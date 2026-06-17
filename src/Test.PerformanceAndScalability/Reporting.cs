namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using LiteGraph;

    internal static class EnvironmentCapture
    {
        public static EnvironmentSnapshot Capture()
        {
            return new EnvironmentSnapshot
            {
                GitSha = RunGit("rev-parse HEAD"),
                GitStatus = RunGit("status --short"),
                LiteGraphAssemblyVersion = typeof(LiteGraphClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? typeof(LiteGraphClient).Assembly.GetName().Version?.ToString()
                    ?? string.Empty,
                DotNetVersion = Environment.Version.ToString(),
                OSDescription = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                IsServerGc = GCSettings.IsServerGC,
                GcLatencyMode = GCSettings.LatencyMode.ToString(),
                CapturedUtc = DateTime.UtcNow
            };
        }

        private static string? RunGit(string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(startInfo)!;
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                return output.Trim();
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class ArtifactWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static async Task WriteAllAsync(
            BenchmarkRunSummary run,
            IReadOnlyList<ProcessMetricSample> processMetrics,
            IReadOnlyList<StorageMetricSample> storageMetrics,
            IReadOnlyList<RepositoryTelemetrySummary> repositoryTelemetry,
            IReadOnlyList<VectorTelemetrySummary> vectorTelemetry)
        {
            string dir = ((BenchmarkOptionsRedacted)run.Configuration).OutputDirectory;
            Directory.CreateDirectory(dir);

            await WriteJsonAsync(Path.Combine(dir, "config.redacted.json"), run.Configuration).ConfigureAwait(false);
            await WriteJsonAsync(Path.Combine(dir, "environment.json"), run.Environment).ConfigureAwait(false);
            await WriteJsonAsync(Path.Combine(dir, "dataset.json"), run.Dataset).ConfigureAwait(false);
            await WriteJsonAsync(Path.Combine(dir, "summary.json"), run).ConfigureAwait(false);
            await WriteJsonAsync(Path.Combine(dir, "anomalies.json"), run.Anomalies).ConfigureAwait(false);

            await WriteScenarioCsvAsync(Path.Combine(dir, "summary.csv"), run.Results).ConfigureAwait(false);
            await WriteScenarioCsvAsync(Path.Combine(dir, "operations.csv"), run.Results).ConfigureAwait(false);
            await WriteRepositoryCsvAsync(Path.Combine(dir, "repository-telemetry.csv"), repositoryTelemetry).ConfigureAwait(false);
            await WriteVectorCsvAsync(Path.Combine(dir, "vector-telemetry.csv"), vectorTelemetry).ConfigureAwait(false);
            await WriteQueryProfilesCsvAsync(Path.Combine(dir, "query-profiles.csv"), run.Results.SelectMany(r => r.QueryProfiles).ToList()).ConfigureAwait(false);
            await WriteProcessMetricsCsvAsync(Path.Combine(dir, "process-metrics.csv"), processMetrics).ConfigureAwait(false);
            await WriteStorageMetricsCsvAsync(Path.Combine(dir, "storage-metrics.csv"), storageMetrics).ConfigureAwait(false);
            await WriteMarkdownReportAsync(Path.Combine(dir, "report.md"), run).ConfigureAwait(false);
        }

        public static BenchmarkOptionsRedacted Redact(BenchmarkOptions options, DatabaseSettings settings)
        {
            return new BenchmarkOptionsRedacted
            {
                OutputDirectory = options.OutputDirectory,
                Value = options.ToRedactedObject(settings)
            };
        }

        public static async Task WriteJsonAsync(string path, object value)
        {
            string json = JsonSerializer.Serialize(value, JsonOptions);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        private static async Task WriteScenarioCsvAsync(string path, IReadOnlyList<ScenarioResult> results)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("scenario,category,iteration,concurrency,open_loop,target_rate,duration_ms,attempted,completed,failed,timed_out,canceled,incorrect,items,result_count,ops_per_sec,items_per_sec,p50_ms,p95_ms,p99_ms,p999_ms,max_ms,allocated_bytes,gen0,gen1,gen2,working_set_delta_bytes,error_sample");
            foreach (ScenarioResult r in results)
            {
                sb.AppendCsv(r.Scenario)
                    .AppendCsv(r.Category)
                    .AppendCsv(r.Iteration)
                    .AppendCsv(r.Concurrency)
                    .AppendCsv(r.OpenLoop)
                    .AppendCsv(r.TargetRate)
                    .AppendCsv(r.DurationMs)
                    .AppendCsv(r.Attempted)
                    .AppendCsv(r.Completed)
                    .AppendCsv(r.Failed)
                    .AppendCsv(r.TimedOut)
                    .AppendCsv(r.Canceled)
                    .AppendCsv(r.Incorrect)
                    .AppendCsv(r.Items)
                    .AppendCsv(r.ResultCount)
                    .AppendCsv(r.OperationsPerSecond)
                    .AppendCsv(r.ItemsPerSecond)
                    .AppendCsv(r.Latency.P50Ms)
                    .AppendCsv(r.Latency.P95Ms)
                    .AppendCsv(r.Latency.P99Ms)
                    .AppendCsv(r.Latency.P999Ms)
                    .AppendCsv(r.Latency.MaxMs)
                    .AppendCsv(r.ProcessDelta.AllocatedBytes)
                    .AppendCsv(r.ProcessDelta.Gen0Collections)
                    .AppendCsv(r.ProcessDelta.Gen1Collections)
                    .AppendCsv(r.ProcessDelta.Gen2Collections)
                    .AppendCsv(r.ProcessDelta.WorkingSetDeltaBytes)
                    .AppendCsvLast(r.ErrorSample);
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static async Task WriteRepositoryCsvAsync(string path, IReadOnlyList<RepositoryTelemetrySummary> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("scenario,provider,operation,success,transactional,count,statement_count,row_count,total_duration_ms,average_duration_ms");
            foreach (RepositoryTelemetrySummary r in rows)
            {
                sb.AppendCsv(r.Scenario)
                    .AppendCsv(r.Provider)
                    .AppendCsv(r.Operation)
                    .AppendCsv(r.Success)
                    .AppendCsv(r.Transactional)
                    .AppendCsv(r.Count)
                    .AppendCsv(r.StatementCount)
                    .AppendCsv(r.RowCount)
                    .AppendCsv(r.TotalDurationMs)
                    .AppendCsvLast(r.AverageDurationMs);
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static async Task WriteVectorCsvAsync(string path, IReadOnlyList<VectorTelemetrySummary> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("scenario,domain,success,count,result_count,total_duration_ms,average_duration_ms");
            foreach (VectorTelemetrySummary r in rows)
            {
                sb.AppendCsv(r.Scenario)
                    .AppendCsv(r.Domain)
                    .AppendCsv(r.Success)
                    .AppendCsv(r.Count)
                    .AppendCsv(r.ResultCount)
                    .AppendCsv(r.TotalDurationMs)
                    .AppendCsvLast(r.AverageDurationMs);
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static async Task WriteQueryProfilesCsvAsync(string path, IReadOnlyList<QueryProfileSample> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("scenario,parse_ms,plan_ms,execute_ms,repository_ms,repository_operations,vector_ms,vector_count,transaction_ms,total_ms,row_count");
            foreach (QueryProfileSample r in rows)
            {
                sb.AppendCsv(r.Scenario)
                    .AppendCsv(r.ParseTimeMs)
                    .AppendCsv(r.PlanTimeMs)
                    .AppendCsv(r.ExecuteTimeMs)
                    .AppendCsv(r.RepositoryTimeMs)
                    .AppendCsv(r.RepositoryOperationCount)
                    .AppendCsv(r.VectorSearchTimeMs)
                    .AppendCsv(r.VectorSearchCount)
                    .AppendCsv(r.TransactionTimeMs)
                    .AppendCsv(r.TotalTimeMs)
                    .AppendCsvLast(r.RowCount);
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static async Task WriteProcessMetricsCsvAsync(string path, IReadOnlyList<ProcessMetricSample> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("timestamp_utc,scenario,cpu_percent,working_set_bytes,private_bytes,managed_heap_bytes,allocated_bytes,gen0,gen1,gen2,thread_count,handle_count");
            foreach (ProcessMetricSample r in rows)
            {
                sb.AppendCsv(r.TimestampUtc.ToString("O", CultureInfo.InvariantCulture))
                    .AppendCsv(r.Scenario)
                    .AppendCsv(r.CpuPercent)
                    .AppendCsv(r.WorkingSetBytes)
                    .AppendCsv(r.PrivateBytes)
                    .AppendCsv(r.ManagedHeapBytes)
                    .AppendCsv(r.AllocatedBytes)
                    .AppendCsv(r.Gen0Collections)
                    .AppendCsv(r.Gen1Collections)
                    .AppendCsv(r.Gen2Collections)
                    .AppendCsv(r.ThreadCount)
                    .AppendCsvLast(r.HandleCount);
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static async Task WriteStorageMetricsCsvAsync(string path, IReadOnlyList<StorageMetricSample> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("timestamp_utc,scenario,provider,database_file,database_bytes,wal_bytes,shm_bytes,index_bytes");
            foreach (StorageMetricSample r in rows)
            {
                sb.AppendCsv(r.TimestampUtc.ToString("O", CultureInfo.InvariantCulture))
                    .AppendCsv(r.Scenario)
                    .AppendCsv(r.Provider)
                    .AppendCsv(r.DatabaseFile)
                    .AppendCsv(r.DatabaseBytes)
                    .AppendCsv(r.WalBytes)
                    .AppendCsv(r.ShmBytes)
                    .AppendCsvLast(r.IndexBytes);
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static async Task WriteMarkdownReportAsync(string path, BenchmarkRunSummary run)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# LiteGraph Performance And Scalability Report");
            sb.AppendLine();
            sb.AppendLine("- Run id: `" + run.RunId + "`");
            sb.AppendLine("- Started UTC: `" + run.StartedUtc.ToString("O", CultureInfo.InvariantCulture) + "`");
            sb.AppendLine("- Completed UTC: `" + run.CompletedUtc.ToString("O", CultureInfo.InvariantCulture) + "`");
            sb.AppendLine("- LiteGraph assembly: `" + run.Environment.LiteGraphAssemblyVersion + "`");
            sb.AppendLine("- Runtime: `.NET " + run.Environment.DotNetVersion + "`");
            sb.AppendLine("- OS: `" + run.Environment.OSDescription + "`");
            sb.AppendLine("- Processors: `" + run.Environment.ProcessorCount + "`");
            sb.AppendLine();
            sb.AppendLine("## Dataset");
            sb.AppendLine();
            sb.AppendLine("| Graphs | Nodes | Edges | Vectors | Topology | Payload | Avg Degree | Max Degree |");
            sb.AppendLine("| ---: | ---: | ---: | ---: | --- | --- | ---: | ---: |");
            sb.AppendLine("| " + run.Dataset.Graphs + " | " + run.Dataset.Nodes + " | " + run.Dataset.Edges + " | " + run.Dataset.Vectors + " | " + run.Dataset.Topology + " | " + run.Dataset.PayloadSize + " | " + run.Dataset.AverageDegree.ToString("F2", CultureInfo.InvariantCulture) + " | " + run.Dataset.MaxDegree + " |");
            sb.AppendLine();
            sb.AppendLine("## Scenario Summary");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Category | C | Ops/sec | P50 ms | P95 ms | P99 ms | Failed | Timeout | Incorrect |");
            sb.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (ScenarioResult r in run.Results.OrderBy(r => r.Category).ThenBy(r => r.Scenario))
            {
                sb.AppendLine("| `" + r.Scenario + "` | " + r.Category + " | " + r.Concurrency + " | " + Format(r.OperationsPerSecond) + " | " + Format(r.Latency.P50Ms) + " | " + Format(r.Latency.P95Ms) + " | " + Format(r.Latency.P99Ms) + " | " + r.Failed + " | " + r.TimedOut + " | " + r.Incorrect + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## Top Repository Time");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Provider | Operation | Count | Statements | Rows | Total ms | Avg ms |");
            sb.AppendLine("| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |");
            foreach (RepositoryTelemetrySummary r in run.Results.SelectMany(r => r.RepositoryTelemetry).OrderByDescending(r => r.TotalDurationMs).Take(20))
            {
                sb.AppendLine("| `" + r.Scenario + "` | " + r.Provider + " | `" + r.Operation + "` | " + r.Count + " | " + r.StatementCount + " | " + r.RowCount + " | " + Format(r.TotalDurationMs) + " | " + Format(r.AverageDurationMs) + " |");
            }

            if (run.Anomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Anomalies");
                sb.AppendLine();
                foreach (string anomaly in run.Anomalies)
                {
                    sb.AppendLine("- " + anomaly);
                }
            }

            await File.WriteAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }

        private static string Format(double value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    internal sealed class BenchmarkOptionsRedacted
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public object Value { get; set; } = null!;
    }

    internal static class RegressionAnalyzer
    {
        public static List<string> Compare(string? baselinePath, IReadOnlyList<ScenarioResult> current)
        {
            List<string> anomalies = new List<string>();
            if (string.IsNullOrWhiteSpace(baselinePath)) return anomalies;
            if (!File.Exists(baselinePath))
            {
                anomalies.Add("Baseline file was not found: " + baselinePath);
                return anomalies;
            }

            BenchmarkRunSummary? baseline = JsonSerializer.Deserialize<BenchmarkRunSummary>(File.ReadAllText(baselinePath));
            if (baseline == null)
            {
                anomalies.Add("Baseline file could not be parsed: " + baselinePath);
                return anomalies;
            }

            Dictionary<string, ScenarioResult> previous = baseline.Results.ToDictionary(r => r.Scenario, StringComparer.OrdinalIgnoreCase);
            foreach (ScenarioResult result in current)
            {
                if (!previous.TryGetValue(result.Scenario, out ScenarioResult? old)) continue;

                if (old.OperationsPerSecond > 0)
                {
                    double throughputDrop = (old.OperationsPerSecond - result.OperationsPerSecond) / old.OperationsPerSecond;
                    if (throughputDrop > 0.10)
                    {
                        anomalies.Add(result.Scenario + " throughput dropped " + (throughputDrop * 100).ToString("F1", CultureInfo.InvariantCulture) + "% from baseline.");
                    }
                }

                if (old.Latency.P99Ms > 0)
                {
                    double p99Increase = (result.Latency.P99Ms - old.Latency.P99Ms) / old.Latency.P99Ms;
                    if (p99Increase > 0.20)
                    {
                        anomalies.Add(result.Scenario + " p99 latency increased " + (p99Increase * 100).ToString("F1", CultureInfo.InvariantCulture) + "% from baseline.");
                    }
                }

                if (result.Failed > old.Failed)
                {
                    anomalies.Add(result.Scenario + " has more failed operations than baseline.");
                }
            }

            return anomalies;
        }
    }

    internal static class CsvExtensions
    {
        public static StringBuilder AppendCsv(this StringBuilder sb, object? value)
        {
            sb.Append(Escape(value));
            sb.Append(',');
            return sb;
        }

        public static void AppendCsvLast(this StringBuilder sb, object? value)
        {
            sb.Append(Escape(value));
            sb.AppendLine();
        }

        private static string Escape(object? value)
        {
            if (value == null) return string.Empty;
            string text = value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString() ?? string.Empty;
            if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\n') && !text.Contains('\r')) return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
