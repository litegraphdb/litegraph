namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using LiteGraph;

    internal static class ConsoleOutput
    {
        public static void WriteRunHeader(BenchmarkOptions options, DatabaseSettings settings)
        {
            WriteTitle("LiteGraph Performance And Scalability");
            WriteKeyValueTable(new[]
            {
                ("Run ID", options.RunId ?? string.Empty),
                ("Provider", settings.ToSafeString()),
                ("Profile", options.Profile),
                ("Workloads", string.Join(",", options.Workloads)),
                ("Topology", options.Topology),
                ("Concurrency", string.Join(",", options.Concurrency)),
                ("Duration", options.Duration.ToString()),
                ("Output", Path.GetFullPath(options.OutputDirectory))
            });
            Console.WriteLine();
        }

        public static void WriteDatasetStart()
        {
            WriteSection("Dataset");
            Console.WriteLine("Generating deterministic dataset...");
        }

        public static void WriteDatasetSummary(DatasetMetadata metadata)
        {
            string[] headers =
            {
                "Tenants",
                "Graphs",
                "Nodes",
                "Edges",
                "Vectors",
                "Topology",
                "Payload",
                "AvgDeg",
                "MaxDeg",
                "Gen ms"
            };

            WriteTable(headers, new[]
            {
                new[]
                {
                    metadata.Tenants.ToString(CultureInfo.InvariantCulture),
                    metadata.Graphs.ToString(CultureInfo.InvariantCulture),
                    metadata.Nodes.ToString(CultureInfo.InvariantCulture),
                    metadata.Edges.ToString(CultureInfo.InvariantCulture),
                    metadata.Vectors.ToString(CultureInfo.InvariantCulture),
                    metadata.Topology,
                    metadata.PayloadSize,
                    metadata.AverageDegree.ToString("F2", CultureInfo.InvariantCulture),
                    metadata.MaxDegree.ToString(CultureInfo.InvariantCulture),
                    metadata.GenerationDurationMs.ToString("F0", CultureInfo.InvariantCulture)
                }
            });
            Console.WriteLine();
        }

        public static void WriteScenarioHeader(int scenarioCount, int iterationCount, int concurrencyCount)
        {
            WriteSection("Scenarios");
            Console.WriteLine("Rows are written as scenarios complete. Total scheduled rows: " + (scenarioCount * iterationCount * concurrencyCount).ToString(CultureInfo.InvariantCulture));
            Console.WriteLine();
            Console.WriteLine(ScenarioHeader);
            Console.WriteLine(ScenarioSeparator);
        }

        public static void WriteScenarioResult(ScenarioResult result)
        {
            string status = Status(result);
            ConsoleColor? color = StatusColor(status);
            WriteColor(Pad(status, 7), color);
            Console.Write(" ");
            Console.Write(Pad(TrimScenarioName(result.Scenario), 34));
            Console.Write(" ");
            Console.Write(Pad(result.Category, 12));
            Console.Write(" ");
            Console.Write(PadLeft(result.Iteration.ToString(CultureInfo.InvariantCulture), 4));
            Console.Write(" ");
            Console.Write(PadLeft(result.Concurrency.ToString(CultureInfo.InvariantCulture), 3));
            Console.Write(" ");
            Console.Write(PadLeft(result.Completed.ToString(CultureInfo.InvariantCulture), 8));
            Console.Write(" ");
            Console.Write(PadLeft(result.OperationsPerSecond.ToString("F2", CultureInfo.InvariantCulture), 10));
            Console.Write(" ");
            Console.Write(PadLeft(result.Latency.P50Ms.ToString("F2", CultureInfo.InvariantCulture), 9));
            Console.Write(" ");
            Console.Write(PadLeft(result.Latency.P95Ms.ToString("F2", CultureInfo.InvariantCulture), 9));
            Console.Write(" ");
            Console.Write(PadLeft(result.Latency.P99Ms.ToString("F2", CultureInfo.InvariantCulture), 9));
            Console.Write(" ");
            Console.Write(PadLeft(result.Failed.ToString(CultureInfo.InvariantCulture), 5));
            Console.Write(" ");
            Console.Write(PadLeft(result.TimedOut.ToString(CultureInfo.InvariantCulture), 5));
            Console.Write(" ");
            Console.Write(PadLeft(result.Canceled.ToString(CultureInfo.InvariantCulture), 5));
            Console.Write(" ");
            Console.Write(PadLeft(result.Incorrect.ToString(CultureInfo.InvariantCulture), 5));
            Console.WriteLine();
        }

        public static void WriteArtifacts(string outputDirectory)
        {
            Console.WriteLine();
            WriteSection("Artifacts");
            WriteKeyValueTable(new[]
            {
                ("Directory", Path.GetFullPath(outputDirectory)),
                ("Summary", Path.Combine(Path.GetFullPath(outputDirectory), "summary.csv")),
                ("Report", Path.Combine(Path.GetFullPath(outputDirectory), "report.md"))
            });
        }

        public static void WriteAnomalies(IReadOnlyList<string> anomalies)
        {
            if (anomalies.Count < 1) return;

            Console.WriteLine();
            WriteSection("Anomalies");
            string[] headers = { "#", "Finding" };
            List<string[]> rows = new List<string[]>();
            for (int i = 0; i < anomalies.Count; i++)
            {
                rows.Add(new[] { (i + 1).ToString(CultureInfo.InvariantCulture), anomalies[i] });
            }

            WriteTable(headers, rows);
        }

        public static void WriteLegend()
        {
            Console.WriteLine();
            WriteSection("Legend");
            WriteTable(
                new[] { "Column", "Meaning" },
                new[]
                {
                    new[] { "Status", "Overall scenario result for the row." },
                    new[] { "Scenario", "Workload scenario name." },
                    new[] { "Category", "Workload family." },
                    new[] { "Iter", "Iteration number." },
                    new[] { "C", "Configured concurrency for the row." },
                    new[] { "Ops", "Completed high-level operations." },
                    new[] { "Ops/s", "Completed high-level operations per second." },
                    new[] { "P50 ms", "Median completed-operation latency in milliseconds." },
                    new[] { "P95 ms", "95th percentile completed-operation latency in milliseconds." },
                    new[] { "P99 ms", "99th percentile completed-operation latency in milliseconds." },
                    new[] { "Fail", "Operations that threw unhandled failures." },
                    new[] { "TO", "Operations canceled by the per-operation timeout." },
                    new[] { "Cxl", "Operations canceled because the scenario or run ended." },
                    new[] { "Bad", "Completed operations that failed correctness sampling." }
                });

            Console.WriteLine();
            WriteTable(
                new[] { "Status", "Meaning" },
                new[]
                {
                    new[] { "OK", "At least one operation completed and no failure, timeout, or correctness issue was recorded." },
                    new[] { "FAIL", "One or more operations failed with an exception." },
                    new[] { "BAD", "One or more operations completed but failed correctness sampling." },
                    new[] { "TIMEOUT", "One or more operations exceeded --timeout." },
                    new[] { "CANCEL", "No operation completed before the scenario or run was canceled." },
                    new[] { "EMPTY", "No operations completed and no explicit cancellation, timeout, failure, or correctness issue was recorded." }
                });
        }

        public static void WriteCancellation()
        {
            Console.WriteLine();
            WriteColor("Cancellation requested. Finishing cleanup and artifact writing.", ConsoleColor.Yellow);
            Console.WriteLine();
        }

        public static void WriteError(string message)
        {
            WriteColor(message, ConsoleColor.Red);
            Console.WriteLine();
        }

        private static void WriteTitle(string title)
        {
            Console.WriteLine(title);
            Console.WriteLine(new string('=', title.Length));
            Console.WriteLine();
        }

        private static void WriteSection(string title)
        {
            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
        }

        private static void WriteKeyValueTable(IEnumerable<(string Key, string Value)> rows)
        {
            int keyWidth = Math.Max(8, rows.Max(r => r.Key.Length));
            foreach ((string Key, string Value) row in rows)
            {
                Console.Write(Pad(row.Key, keyWidth));
                Console.Write(" : ");
                Console.WriteLine(row.Value);
            }
        }

        private static void WriteTable(string[] headers, IEnumerable<string[]> rows)
        {
            List<string[]> rowList = rows.ToList();
            int[] widths = new int[headers.Length];

            for (int i = 0; i < headers.Length; i++)
            {
                widths[i] = headers[i].Length;
            }

            foreach (string[] row in rowList)
            {
                for (int i = 0; i < headers.Length && i < row.Length; i++)
                {
                    widths[i] = Math.Max(widths[i], row[i]?.Length ?? 0);
                }
            }

            WriteTableRow(headers, widths);
            Console.WriteLine(string.Join(" ", widths.Select(w => new string('-', w))));
            foreach (string[] row in rowList)
            {
                WriteTableRow(row, widths);
            }
        }

        private static void WriteTableRow(string[] row, int[] widths)
        {
            for (int i = 0; i < widths.Length; i++)
            {
                if (i > 0) Console.Write(" ");
                string value = i < row.Length ? row[i] ?? string.Empty : string.Empty;
                Console.Write(Pad(value, widths[i]));
            }
            Console.WriteLine();
        }

        private static string Status(ScenarioResult result)
        {
            if (result.Failed > 0) return "FAIL";
            if (result.Incorrect > 0) return "BAD";
            if (result.TimedOut > 0) return "TIMEOUT";
            if (result.Canceled > 0 && result.Completed == 0) return "CANCEL";
            if (result.Completed == 0) return "EMPTY";
            return "OK";
        }

        private static ConsoleColor? StatusColor(string status)
        {
            switch (status)
            {
                case "OK":
                    return ConsoleColor.Green;
                case "CANCEL":
                case "TIMEOUT":
                    return ConsoleColor.Yellow;
                case "FAIL":
                case "BAD":
                case "EMPTY":
                    return ConsoleColor.Red;
                default:
                    return null;
            }
        }

        private static void WriteColor(string value, ConsoleColor? color)
        {
            if (color == null || Console.IsOutputRedirected)
            {
                Console.Write(value);
                return;
            }

            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.Write(value);
            Console.ForegroundColor = previous;
        }

        private static string TrimScenarioName(string value)
        {
            int concurrencySuffix = value.LastIndexOf(".c", StringComparison.Ordinal);
            if (concurrencySuffix > 0) value = value.Substring(0, concurrencySuffix);
            return value;
        }

        private static string Pad(string value, int width)
        {
            value ??= string.Empty;
            if (value.Length > width) return value.Substring(0, Math.Max(0, width - 1)) + ".";
            return value.PadRight(width);
        }

        private static string PadLeft(string value, int width)
        {
            value ??= string.Empty;
            if (value.Length > width) return value.Substring(0, width);
            return value.PadLeft(width);
        }

        private const string ScenarioHeader =
            "Status  Scenario                           Category     Iter   C      Ops      Ops/s    P50 ms    P95 ms    P99 ms  Fail    TO   Cxl   Bad";

        private const string ScenarioSeparator =
            "------- ---------------------------------- ------------ ---- --- -------- ---------- --------- --------- --------- ----- ----- ----- -----";
    }
}
