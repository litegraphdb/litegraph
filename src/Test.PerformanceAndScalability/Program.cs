namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;

    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            BenchmarkOptions options;
            try
            {
                options = BenchmarkOptions.Parse(args);
            }
            catch (Exception e)
            {
                ConsoleOutput.WriteError(e.Message);
                Console.WriteLine();
                Console.WriteLine(BenchmarkOptions.HelpText());
                return 2;
            }

            if (options.Help)
            {
                Console.WriteLine(BenchmarkOptions.HelpText());
                return 0;
            }

            options.PrepareRun();
            DatabaseSettings settings;
            try
            {
                settings = options.BuildDatabaseSettings();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 2;
            }

            if (options.DryRun)
            {
                Console.WriteLine(JsonSerializer.Serialize(ArtifactWriter.Redact(options, settings), new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            using CancellationTokenSource runCts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                runCts.Cancel();
                ConsoleOutput.WriteCancellation();
            };

            DateTime startedUtc = DateTime.UtcNow;
            TelemetryCollector telemetry = new TelemetryCollector();
            ProcessMetricsCollector processMetrics = new ProcessMetricsCollector();
            List<StorageMetricSample> storageMetrics = new List<StorageMetricSample>();
            BenchmarkRunSummary? summary = null;
            int exitCode = 0;

            try
            {
                if (options.CaptureProcessMetrics) processMetrics.Start();

                ConsoleOutput.WriteRunHeader(options, settings);

                await using GraphRepositoryBase repository = GraphRepositoryFactory.Create(settings);
                await using LiteGraphClient client = new LiteGraphClient(repository, disposeRepository: false, logging: null, caching: null, storage: null);
                await client.InitializeRepositoryAsync(runCts.Token).ConfigureAwait(false);

                BenchmarkContext context = new BenchmarkContext
                {
                    Options = options,
                    DatabaseSettings = settings,
                    Client = client,
                    Telemetry = telemetry,
                    ProcessMetrics = processMetrics,
                    StorageMetrics = storageMetrics
                };

                ConsoleOutput.WriteDatasetStart();
                context.Dataset = await DatasetGenerator.GenerateAsync(context, runCts.Token).ConfigureAwait(false);
                ConsoleOutput.WriteDatasetSummary(context.Dataset.Metadata);

                List<BenchmarkScenario> scenarios = WorkloadCatalog.Build(options);
                if (scenarios.Count == 0)
                {
                    ConsoleOutput.WriteError("No scenarios matched the selected workloads.");
                    return 2;
                }

                LoadEngine engine = new LoadEngine();
                List<ScenarioResult> results = new List<ScenarioResult>();
                ConsoleOutput.WriteScenarioHeader(scenarios.Count, options.Iterations, options.Concurrency.Count);

                for (int iteration = 1; iteration <= options.Iterations; iteration++)
                {
                    foreach (int concurrency in options.Concurrency)
                    {
                        foreach (BenchmarkScenario scenario in scenarios)
                        {
                            if (runCts.IsCancellationRequested) break;

                            string label = scenario.Name + " iteration " + iteration + " concurrency " + concurrency;
                            processMetrics.SetScenario(label);

                            ScenarioResult result = await engine.RunAsync(context, scenario, concurrency, iteration, runCts.Token).ConfigureAwait(false);
                            results.Add(result);
                            ConsoleOutput.WriteScenarioResult(result);

                            if (options.Cooldown > TimeSpan.Zero)
                            {
                                await Task.Delay(options.Cooldown, runCts.Token).ConfigureAwait(false);
                            }
                        }
                    }
                }

                List<string> anomalies = RegressionAnalyzer.Compare(options.Baseline, results);
                anomalies.AddRange(DetectAnomalies(results));

                summary = new BenchmarkRunSummary
                {
                    RunId = options.RunId ?? string.Empty,
                    StartedUtc = startedUtc,
                    CompletedUtc = DateTime.UtcNow,
                    Configuration = ArtifactWriter.Redact(options, settings),
                    Environment = EnvironmentCapture.Capture(),
                    Dataset = context.Dataset.Metadata,
                    Results = results,
                    Anomalies = anomalies
                };

                if (options.FailOnRegression && anomalies.Count > 0)
                {
                    exitCode = 3;
                }
            }
            catch (OperationCanceledException)
            {
                exitCode = 130;
                summary ??= new BenchmarkRunSummary
                {
                    RunId = options.RunId ?? string.Empty,
                    StartedUtc = startedUtc,
                    CompletedUtc = DateTime.UtcNow,
                    Configuration = ArtifactWriter.Redact(options, settings),
                    Environment = EnvironmentCapture.Capture(),
                    Dataset = new DatasetMetadata(),
                    Results = new List<ScenarioResult>(),
                    Anomalies = new List<string> { "Run was canceled." }
                };
            }
            catch (Exception e)
            {
                exitCode = 1;
                summary ??= new BenchmarkRunSummary
                {
                    RunId = options.RunId ?? string.Empty,
                    StartedUtc = startedUtc,
                    CompletedUtc = DateTime.UtcNow,
                    Configuration = ArtifactWriter.Redact(options, settings),
                    Environment = EnvironmentCapture.Capture(),
                    Dataset = new DatasetMetadata(),
                    Results = new List<ScenarioResult>(),
                    Anomalies = new List<string> { e.GetType().Name + ": " + e.Message }
                };
                ConsoleOutput.WriteError(e.ToString());
            }
            finally
            {
                await processMetrics.StopAsync().ConfigureAwait(false);

                if (summary != null)
                {
                    await ArtifactWriter.WriteAllAsync(
                        summary,
                        processMetrics.Samples,
                        storageMetrics,
                        telemetry.GetAllRepositorySummaries(),
                        telemetry.GetAllVectorSummaries()).ConfigureAwait(false);

                    ConsoleOutput.WriteArtifacts(options.OutputDirectory);
                    ConsoleOutput.WriteAnomalies(summary.Anomalies);
                    ConsoleOutput.WriteLegend();
                }

                telemetry.Dispose();
                CleanupTemporarySqlite(options, settings);
            }

            return exitCode;
        }

        private static List<string> DetectAnomalies(IReadOnlyList<ScenarioResult> results)
        {
            List<string> anomalies = new List<string>();
            foreach (ScenarioResult result in results)
            {
                if (result.Failed > 0) anomalies.Add(result.Scenario + " recorded " + result.Failed + " failed operations.");
                if (result.TimedOut > 0) anomalies.Add(result.Scenario + " recorded " + result.TimedOut + " timed-out operations.");
                if (result.Incorrect > 0) anomalies.Add(result.Scenario + " recorded " + result.Incorrect + " incorrect sampled operations.");
                if (result.Completed == 0 && result.Canceled == 0 && result.TimedOut == 0)
                    anomalies.Add(result.Scenario + " completed no operations.");
            }

            return anomalies.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void CleanupTemporarySqlite(BenchmarkOptions options, DatabaseSettings settings)
        {
            if (settings.Type != DatabaseTypeEnum.Sqlite) return;
            if (!options.CreatedTemporaryDatabase || options.KeepDatabase) return;
            if (string.IsNullOrWhiteSpace(settings.Filename)) return;

            try
            {
                Type? sqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
                sqliteConnectionType?.GetMethod("ClearAllPools", Type.EmptyTypes)?.Invoke(null, null);
            }
            catch
            {
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (string path in new[] { settings.Filename, settings.Filename + "-wal", settings.Filename + "-shm" })
            {
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    try
                    {
                        if (!File.Exists(path)) break;
                        File.Delete(path);
                        break;
                    }
                    catch when (attempt < 5)
                    {
                        Thread.Sleep(100 * attempt);
                    }
                    catch (Exception e)
                    {
                    ConsoleOutput.WriteError("Unable to delete temporary file '" + path + "': " + e.Message);
                }
            }
        }
        }
    }
}
