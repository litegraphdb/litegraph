namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    internal sealed class LoadEngine
    {
        public async Task<ScenarioResult> RunAsync(
            BenchmarkContext context,
            BenchmarkScenario scenario,
            int concurrency,
            int iteration,
            CancellationToken token)
        {
            if (context.Options.Warmup > TimeSpan.Zero && !scenario.RunOnce)
            {
                await RunPhaseAsync(context, scenario, concurrency, context.Options.Warmup, false, token).ConfigureAwait(false);
            }

            StorageMetricSample? storageBefore = context.Options.CaptureDbFileMetrics
                ? StorageMetrics.Capture(context.Options, context.DatabaseSettings, scenario.Name + ".before")
                : null;

            ProcessMetricSample processBefore = ProcessMetricsCollector.CapturePoint(scenario.Name, 0);
            string scenarioRunName = scenario.Name + ".c" + concurrency + ".i" + iteration;
            context.Telemetry.BeginScenario(scenarioRunName);

            ScenarioResult result;
            try
            {
                result = await RunPhaseAsync(context, scenario, concurrency, context.Options.Duration, true, token).ConfigureAwait(false);
            }
            finally
            {
                context.Telemetry.EndScenario();
            }

            ProcessMetricSample processAfter = ProcessMetricsCollector.CapturePoint(scenario.Name, 0);
            StorageMetricSample? storageAfter = context.Options.CaptureDbFileMetrics
                ? StorageMetrics.Capture(context.Options, context.DatabaseSettings, scenario.Name + ".after")
                : null;

            result.Scenario = scenarioRunName;
            result.Category = scenario.Category;
            result.Description = scenario.Description;
            result.Iteration = iteration;
            result.Concurrency = concurrency;
            result.OpenLoop = !context.Options.ClosedLoop;
            result.TargetRate = context.Options.TargetRate;
            result.StorageBefore = storageBefore;
            result.StorageAfter = storageAfter;
            result.ProcessDelta = new ProcessMetricDelta
            {
                AllocatedBytes = Math.Max(0, processAfter.AllocatedBytes - processBefore.AllocatedBytes),
                Gen0Collections = Math.Max(0, processAfter.Gen0Collections - processBefore.Gen0Collections),
                Gen1Collections = Math.Max(0, processAfter.Gen1Collections - processBefore.Gen1Collections),
                Gen2Collections = Math.Max(0, processAfter.Gen2Collections - processBefore.Gen2Collections),
                WorkingSetDeltaBytes = processAfter.WorkingSetBytes - processBefore.WorkingSetBytes
            };
            result.RepositoryTelemetry = context.Telemetry.GetRepositorySummaries(scenarioRunName);
            result.VectorTelemetry = context.Telemetry.GetVectorSummaries(scenarioRunName);
            result.QueryProfiles = context.QueryProfiles.Where(q => q.Scenario == scenarioRunName).ToList();

            if (storageBefore != null) context.StorageMetrics.Add(storageBefore);
            if (storageAfter != null) context.StorageMetrics.Add(storageAfter);

            return result;
        }

        private async Task<ScenarioResult> RunPhaseAsync(
            BenchmarkContext context,
            BenchmarkScenario scenario,
            int concurrency,
            TimeSpan duration,
            bool record,
            CancellationToken token)
        {
            if (scenario.RunOnce)
            {
                return await RunOnceAsync(context, scenario, concurrency, record, token).ConfigureAwait(false);
            }

            if (!context.Options.ClosedLoop && context.Options.TargetRate != null)
            {
                return await RunOpenLoopAsync(context, scenario, concurrency, duration, record, token).ConfigureAwait(false);
            }

            return await RunClosedLoopAsync(context, scenario, concurrency, duration, record, token).ConfigureAwait(false);
        }

        private async Task<ScenarioResult> RunOnceAsync(
            BenchmarkContext context,
            BenchmarkScenario scenario,
            int concurrency,
            bool record,
            CancellationToken token)
        {
            ScenarioAccumulator accumulator = new ScenarioAccumulator();
            Stopwatch phase = Stopwatch.StartNew();
            List<Task> workers = new List<Task>();
            long operationIndex = 0;

            for (int i = 0; i < concurrency; i++)
            {
                int workerId = i;
                workers.Add(Task.Run(async () =>
                {
                    WorkerContext worker = new WorkerContext
                    {
                        WorkerId = workerId,
                        OperationIndex = Interlocked.Increment(ref operationIndex),
                        Random = new Random(unchecked(context.Options.Seed + (workerId * 7919)))
                    };

                    await ExecuteOneAsync(context, scenario, worker, accumulator, record, token).ConfigureAwait(false);
                }, token));
            }

            await WaitForWorkersAsync(workers).ConfigureAwait(false);
            phase.Stop();
            return accumulator.ToResult(phase.Elapsed, DateTime.UtcNow - phase.Elapsed);
        }

        private async Task<ScenarioResult> RunClosedLoopAsync(
            BenchmarkContext context,
            BenchmarkScenario scenario,
            int concurrency,
            TimeSpan duration,
            bool record,
            CancellationToken token)
        {
            ScenarioAccumulator accumulator = new ScenarioAccumulator();
            using CancellationTokenSource phaseCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            phaseCts.CancelAfter(duration);

            Stopwatch phase = Stopwatch.StartNew();
            List<Task> workers = new List<Task>();
            long operationIndex = 0;

            for (int i = 0; i < concurrency; i++)
            {
                int workerId = i;
                workers.Add(Task.Run(async () =>
                {
                    WorkerContext worker = new WorkerContext
                    {
                        WorkerId = workerId,
                        Random = new Random(unchecked(context.Options.Seed + (workerId * 7919)))
                    };

                    while (!phaseCts.IsCancellationRequested)
                    {
                        worker.OperationIndex = Interlocked.Increment(ref operationIndex);
                        await ExecuteOneAsync(context, scenario, worker, accumulator, record, phaseCts.Token).ConfigureAwait(false);
                    }
                }, phaseCts.Token));
            }

            await WaitForWorkersAsync(workers).ConfigureAwait(false);
            phase.Stop();
            return accumulator.ToResult(phase.Elapsed, DateTime.UtcNow - phase.Elapsed);
        }

        private async Task<ScenarioResult> RunOpenLoopAsync(
            BenchmarkContext context,
            BenchmarkScenario scenario,
            int concurrency,
            TimeSpan duration,
            bool record,
            CancellationToken token)
        {
            ScenarioAccumulator accumulator = new ScenarioAccumulator();
            using CancellationTokenSource phaseCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            phaseCts.CancelAfter(duration);

            double targetRate = context.Options.TargetRate ?? 1;
            TimeSpan interval = TimeSpan.FromSeconds(1.0 / targetRate);
            SemaphoreSlim concurrencyGate = new SemaphoreSlim(concurrency, concurrency);
            ConcurrentBag<Task> scheduled = new ConcurrentBag<Task>();
            Stopwatch phase = Stopwatch.StartNew();
            long operationIndex = 0;
            long scheduledCount = 0;

            try
            {
                while (!phaseCts.IsCancellationRequested)
                {
                    DateTime due = DateTime.UtcNow + interval;
                    await concurrencyGate.WaitAsync(phaseCts.Token).ConfigureAwait(false);

                    long current = Interlocked.Increment(ref operationIndex);
                    int workerId = (int)(Interlocked.Increment(ref scheduledCount) % concurrency);
                    WorkerContext worker = new WorkerContext
                    {
                        WorkerId = workerId,
                        OperationIndex = current,
                        Random = new Random(unchecked(context.Options.Seed + (int)current))
                    };

                    scheduled.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteOneAsync(context, scenario, worker, accumulator, record, phaseCts.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            concurrencyGate.Release();
                        }
                    }, CancellationToken.None));

                    TimeSpan delay = due - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero) await Task.Delay(delay, phaseCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }

            await WaitForWorkersAsync(scheduled.ToList()).ConfigureAwait(false);
            phase.Stop();
            return accumulator.ToResult(phase.Elapsed, DateTime.UtcNow - phase.Elapsed);
        }

        private static async Task ExecuteOneAsync(
            BenchmarkContext context,
            BenchmarkScenario scenario,
            WorkerContext worker,
            ScenarioAccumulator accumulator,
            bool record,
            CancellationToken phaseToken)
        {
            if (phaseToken.IsCancellationRequested) return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref accumulator.Attempted);

            try
            {
                using CancellationTokenSource opCts = CancellationTokenSource.CreateLinkedTokenSource(phaseToken);
                opCts.CancelAfter(context.Options.Timeout);

                OperationOutcome outcome = await scenario.Operation(context, worker, opCts.Token).ConfigureAwait(false);
                stopwatch.Stop();

                if (!record) return;

                accumulator.RecordLatency(stopwatch.Elapsed.TotalMilliseconds);
                Interlocked.Increment(ref accumulator.Completed);
                Interlocked.Add(ref accumulator.Items, Math.Max(0, outcome.Items));
                Interlocked.Add(ref accumulator.ResultCount, Math.Max(0, outcome.ResultCount));
                if (!outcome.Correct)
                {
                    Interlocked.Increment(ref accumulator.Incorrect);
                    accumulator.RecordIncorrect(outcome.Error);
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                if (!record) return;

                if (phaseToken.IsCancellationRequested) Interlocked.Increment(ref accumulator.Canceled);
                else Interlocked.Increment(ref accumulator.TimedOut);
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                if (!record) return;

                accumulator.RecordLatency(stopwatch.Elapsed.TotalMilliseconds);
                Interlocked.Increment(ref accumulator.Failed);
                accumulator.RecordError(e);
            }
        }

        private static async Task WaitForWorkersAsync(List<Task> workers)
        {
            try
            {
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Individual operation failures are recorded inside ExecuteOneAsync.
            }
        }

        private sealed class ScenarioAccumulator
        {
            public long Attempted;
            public long Completed;
            public long Failed;
            public long TimedOut;
            public long Canceled;
            public long Incorrect;
            public long Items;
            public long ResultCount;

            private readonly object _LatencyLock = new object();
            private readonly List<double> _Latencies = new List<double>();
            private string? _ErrorSample;

            public void RecordLatency(double ms)
            {
                lock (_LatencyLock) _Latencies.Add(ms);
            }

            public void RecordError(Exception e)
            {
                if (_ErrorSample == null)
                    Interlocked.CompareExchange(ref _ErrorSample, FormatExceptionSample(e), null);
            }

            public void RecordIncorrect(string? error)
            {
                if (!String.IsNullOrWhiteSpace(error) && _ErrorSample == null)
                    Interlocked.CompareExchange(ref _ErrorSample, "Incorrect: " + error, null);
            }

            public ScenarioResult ToResult(TimeSpan duration, DateTime startedUtc)
            {
                List<double> latencies;
                lock (_LatencyLock) latencies = new List<double>(_Latencies);

                double seconds = Math.Max(duration.TotalSeconds, 0.000001);
                return new ScenarioResult
                {
                    StartedUtc = startedUtc,
                    CompletedUtc = DateTime.UtcNow,
                    DurationMs = duration.TotalMilliseconds,
                    Attempted = Attempted,
                    Completed = Completed,
                    Failed = Failed,
                    TimedOut = TimedOut,
                    Canceled = Canceled,
                    Incorrect = Incorrect,
                    Items = Items,
                    ResultCount = ResultCount,
                    OperationsPerSecond = Completed / seconds,
                    ItemsPerSecond = Items / seconds,
                    Latency = LatencyStats.From(latencies),
                    ErrorSample = _ErrorSample
                };
            }

            private static string FormatExceptionSample(Exception e)
            {
                if (e == null) return string.Empty;

                string sample = e.GetType().Name + ": " + e.Message;
                if (string.IsNullOrWhiteSpace(e.StackTrace)) return sample;

                string[] stackLines = e.StackTrace
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Take(6)
                    .ToArray();

                if (stackLines.Length == 0) return sample;
                return sample + Environment.NewLine + string.Join(Environment.NewLine, stackLines);
            }
        }
    }

    internal sealed class TelemetryCollector : IDisposable
    {
        private readonly ConcurrentDictionary<string, RepositoryAggregate> _Repository = new ConcurrentDictionary<string, RepositoryAggregate>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, VectorAggregate> _Vector = new ConcurrentDictionary<string, VectorAggregate>(StringComparer.Ordinal);
        private string _CurrentScenario = "unscoped";
        private bool _Disposed;

        public string CurrentScenario
        {
            get { return _CurrentScenario; }
        }

        public TelemetryCollector()
        {
            LiteGraphTelemetry.RepositoryOperationRecorded += RepositoryOperationRecorded;
            LiteGraphTelemetry.VectorSearchRecorded += VectorSearchRecorded;
        }

        public void BeginScenario(string scenario)
        {
            _CurrentScenario = scenario;
        }

        public void EndScenario()
        {
            _CurrentScenario = "unscoped";
        }

        public List<RepositoryTelemetrySummary> GetRepositorySummaries(string scenario)
        {
            return _Repository.Values
                .Where(v => v.Scenario == scenario)
                .Select(v => v.ToSummary())
                .OrderByDescending(v => v.TotalDurationMs)
                .ToList();
        }

        public List<VectorTelemetrySummary> GetVectorSummaries(string scenario)
        {
            return _Vector.Values
                .Where(v => v.Scenario == scenario)
                .Select(v => v.ToSummary())
                .OrderByDescending(v => v.TotalDurationMs)
                .ToList();
        }

        public List<RepositoryTelemetrySummary> GetAllRepositorySummaries()
        {
            return _Repository.Values.Select(v => v.ToSummary()).OrderBy(v => v.Scenario).ThenBy(v => v.Operation).ToList();
        }

        public List<VectorTelemetrySummary> GetAllVectorSummaries()
        {
            return _Vector.Values.Select(v => v.ToSummary()).OrderBy(v => v.Scenario).ThenBy(v => v.Domain).ToList();
        }

        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            LiteGraphTelemetry.RepositoryOperationRecorded -= RepositoryOperationRecorded;
            LiteGraphTelemetry.VectorSearchRecorded -= VectorSearchRecorded;
        }

        private void RepositoryOperationRecorded(object? sender, RepositoryOperationTelemetryEventArgs e)
        {
            string scenario = _CurrentScenario;
            string key = string.Join("|", scenario, e.Provider, e.Operation, e.Success, e.Transactional);
            RepositoryAggregate aggregate = _Repository.GetOrAdd(key, _ => new RepositoryAggregate
            {
                Scenario = scenario,
                Provider = e.Provider,
                Operation = e.Operation,
                Success = e.Success,
                Transactional = e.Transactional
            });

            aggregate.Add(e);
        }

        private void VectorSearchRecorded(object? sender, VectorSearchTelemetryEventArgs e)
        {
            string scenario = _CurrentScenario;
            string key = string.Join("|", scenario, e.Domain, e.Success);
            VectorAggregate aggregate = _Vector.GetOrAdd(key, _ => new VectorAggregate
            {
                Scenario = scenario,
                Domain = e.Domain,
                Success = e.Success
            });

            aggregate.Add(e);
        }

        private sealed class RepositoryAggregate
        {
            public string Scenario = string.Empty;
            public string Provider = string.Empty;
            public string Operation = string.Empty;
            public bool Success;
            public bool Transactional;
            private long _Count;
            private long _StatementCount;
            private long _RowCount;
            private double _TotalDurationMs;

            public void Add(RepositoryOperationTelemetryEventArgs e)
            {
                Interlocked.Increment(ref _Count);
                Interlocked.Add(ref _StatementCount, e.StatementCount);
                Interlocked.Add(ref _RowCount, e.RowCount);
                TelemetryCollector.Add(ref _TotalDurationMs, e.DurationMs);
            }

            public RepositoryTelemetrySummary ToSummary()
            {
                long count = Interlocked.Read(ref _Count);
                double total = Volatile.Read(ref _TotalDurationMs);
                return new RepositoryTelemetrySummary
                {
                    Scenario = Scenario,
                    Provider = Provider,
                    Operation = Operation,
                    Success = Success,
                    Transactional = Transactional,
                    Count = count,
                    StatementCount = Interlocked.Read(ref _StatementCount),
                    RowCount = Interlocked.Read(ref _RowCount),
                    TotalDurationMs = total,
                    AverageDurationMs = count == 0 ? 0 : total / count
                };
            }
        }

        private sealed class VectorAggregate
        {
            public string Scenario = string.Empty;
            public string Domain = string.Empty;
            public bool Success;
            private long _Count;
            private long _ResultCount;
            private double _TotalDurationMs;

            public void Add(VectorSearchTelemetryEventArgs e)
            {
                Interlocked.Increment(ref _Count);
                Interlocked.Add(ref _ResultCount, e.ResultCount);
                TelemetryCollector.Add(ref _TotalDurationMs, e.DurationMs);
            }

            public VectorTelemetrySummary ToSummary()
            {
                long count = Interlocked.Read(ref _Count);
                double total = Volatile.Read(ref _TotalDurationMs);
                return new VectorTelemetrySummary
                {
                    Scenario = Scenario,
                    Domain = Domain,
                    Success = Success,
                    Count = count,
                    ResultCount = Interlocked.Read(ref _ResultCount),
                    TotalDurationMs = total,
                    AverageDurationMs = count == 0 ? 0 : total / count
                };
            }
        }

        private static void Add(ref double location, double value)
        {
            double initial;
            double computed;
            do
            {
                initial = Volatile.Read(ref location);
                computed = initial + value;
            }
            while (Math.Abs(Interlocked.CompareExchange(ref location, computed, initial) - initial) > double.Epsilon);
        }
    }

    internal sealed class ProcessMetricsCollector : IDisposable
    {
        private readonly List<ProcessMetricSample> _Samples = new List<ProcessMetricSample>();
        private readonly object _Lock = new object();
        private CancellationTokenSource? _Cts;
        private Task? _Task;
        private string _Scenario = "run";

        public IReadOnlyList<ProcessMetricSample> Samples
        {
            get
            {
                lock (_Lock) return _Samples.ToList();
            }
        }

        public void SetScenario(string scenario)
        {
            _Scenario = scenario;
        }

        public void Start()
        {
            _Cts = new CancellationTokenSource();
            _Task = Task.Run(() => SampleLoopAsync(_Cts.Token));
        }

        public async Task StopAsync()
        {
            if (_Cts == null || _Task == null) return;
            _Cts.Cancel();
            try
            {
                await _Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            _Cts?.Cancel();
            _Cts?.Dispose();
        }

        public static ProcessMetricSample CapturePoint(string scenario, double cpuPercent)
        {
            using Process process = Process.GetCurrentProcess();
            process.Refresh();
            return new ProcessMetricSample
            {
                TimestampUtc = DateTime.UtcNow,
                Scenario = scenario,
                CpuPercent = cpuPercent,
                WorkingSetBytes = process.WorkingSet64,
                PrivateBytes = process.PrivateMemorySize64,
                ManagedHeapBytes = GC.GetTotalMemory(false),
                AllocatedBytes = GC.GetTotalAllocatedBytes(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount
            };
        }

        private async Task SampleLoopAsync(CancellationToken token)
        {
            using Process process = Process.GetCurrentProcess();
            TimeSpan previousCpu = process.TotalProcessorTime;
            DateTime previousTime = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);

                process.Refresh();
                DateTime now = DateTime.UtcNow;
                TimeSpan cpu = process.TotalProcessorTime;
                double elapsedMs = Math.Max(1, (now - previousTime).TotalMilliseconds);
                double cpuMs = (cpu - previousCpu).TotalMilliseconds;
                double cpuPercent = (cpuMs / elapsedMs) * 100.0 / Math.Max(1, Environment.ProcessorCount);
                previousCpu = cpu;
                previousTime = now;

                ProcessMetricSample sample = new ProcessMetricSample
                {
                    TimestampUtc = now,
                    Scenario = _Scenario,
                    CpuPercent = cpuPercent,
                    WorkingSetBytes = process.WorkingSet64,
                    PrivateBytes = process.PrivateMemorySize64,
                    ManagedHeapBytes = GC.GetTotalMemory(false),
                    AllocatedBytes = GC.GetTotalAllocatedBytes(false),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2),
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount
                };

                lock (_Lock) _Samples.Add(sample);
            }
        }
    }

    internal static class StorageMetrics
    {
        public static StorageMetricSample Capture(BenchmarkOptions options, DatabaseSettings settings, string scenario)
        {
            StorageMetricSample sample = new StorageMetricSample
            {
                TimestampUtc = DateTime.UtcNow,
                Scenario = scenario,
                Provider = settings.Type.ToString()
            };

            if (settings.Type == DatabaseTypeEnum.Sqlite)
            {
                string filename = settings.Filename;
                sample.DatabaseFile = filename;
                sample.DatabaseBytes = FileSize(filename);
                sample.WalBytes = FileSize(filename + "-wal");
                sample.ShmBytes = FileSize(filename + "-shm");
            }

            sample.IndexBytes = DirectorySize(Path.Combine(Environment.CurrentDirectory, "indexes"))
                + DirectorySize(Path.Combine(options.OutputDirectory, "indexes"));

            return sample;
        }

        private static long FileSize(string path)
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }

        private static long DirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long total = 0;
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                }
            }
            return total;
        }
    }

    internal static class AsyncEnumerableHelpers
    {
        public static async Task<int> CountUpToAsync<T>(IAsyncEnumerable<T> source, int max, CancellationToken token)
        {
            int count = 0;
            await foreach (T _ in source.WithCancellation(token).ConfigureAwait(false))
            {
                count++;
                if (count >= max) break;
            }

            return count;
        }

        public static async Task<List<T>> TakeAsync<T>(IAsyncEnumerable<T> source, int max, CancellationToken token)
        {
            List<T> ret = new List<T>();
            await foreach (T item in source.WithCancellation(token).ConfigureAwait(false))
            {
                ret.Add(item);
                if (ret.Count >= max) break;
            }

            return ret;
        }
    }
}
