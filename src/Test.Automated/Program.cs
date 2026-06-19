namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;
    using Touchstone.Core;

    internal static class Program
    {
        private static readonly HashSet<string> TransactionConcurrencyCaseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Transactions.Client.ConcurrentQueue",
            "Transactions.Sqlite.ConcurrentWriteContention",
            "Transactions.Concurrent.SameGraphMixedObjects",
            "Transactions.Concurrent.DifferentGraphs",
            "Transactions.Concurrent.CommitRollbackIsolation",
            "Transactions.Concurrent.AttachDetachMetadata",
            "Transactions.Concurrent.UpsertWaves",
            "Transactions.Concurrent.VectorCommitRollback",
            "Transactions.Concurrent.MixedTransactionalNonTransactionalWrites",
            "Transactions.Concurrent.MixedVectorIndexChanges",
            "Transactions.ProviderMatrix.PostgresqlConcurrency",
            "Transactions.Client.IsolatedParallelExecution",
            "Transactions.Query.IsolatedParallelExecution",
            "Transactions.Authorization.Boundary"
        };

        private static async Task<int> Main(string[] args)
        {
            AutomatedOptions options;
            try
            {
                options = AutomatedOptions.Parse(args);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                WriteHelp();
                return 1;
            }

            if (options.Help)
            {
                WriteHelp();
                return 0;
            }

            IReadOnlyList<TestSuiteDescriptor> suites = LiteGraphTouchstoneSuites.All;
            if (options.List)
            {
                WriteSuiteList(suites);
                return 0;
            }

            suites = FilterSuites(suites, options);
            if (suites.Count < 1)
            {
                Console.Error.WriteLine("No Test.Automated suites matched the requested filters.");
                return 1;
            }

            int exitCode = await ConsoleRunner.RunAsync(
                suites,
                resultsPath: options.ResultsPath).ConfigureAwait(false);
            if (options.TransactionConcurrencyOnly) WriteTransactionConcurrencySummary(suites, exitCode);
            return exitCode;
        }

        private static IReadOnlyList<TestSuiteDescriptor> FilterSuites(IReadOnlyList<TestSuiteDescriptor> suites, AutomatedOptions options)
        {
            if (!options.HasFilters) return suites;

            List<TestSuiteDescriptor> filtered = new List<TestSuiteDescriptor>();
            foreach (TestSuiteDescriptor suite in suites)
            {
                if (options.SuiteIds.Count > 0 && !options.SuiteIds.Contains(suite.SuiteId)) continue;

                List<TestCaseDescriptor> cases = suite.Cases.Where(c => MatchesCase(c, options)).ToList();
                if (cases.Count < 1) continue;

                filtered.Add(new TestSuiteDescriptor(
                    suiteId: suite.SuiteId,
                    displayName: suite.DisplayName,
                    cases: cases,
                    beforeSuiteAsync: suite.BeforeSuiteAsync,
                    afterSuiteAsync: suite.AfterSuiteAsync));
            }

            return filtered;
        }

        private static bool MatchesCase(TestCaseDescriptor testCase, AutomatedOptions options)
        {
            if (options.TransactionConcurrencyOnly && !TransactionConcurrencyCaseIds.Contains(testCase.CaseId)) return false;
            if (options.TransactionsOnly && !testCase.CaseId.StartsWith("Transactions.", StringComparison.OrdinalIgnoreCase)) return false;

            if (options.CaseIds.Count > 0)
            {
                return options.CaseIds.Contains(testCase.CaseId)
                    || options.CaseIds.Contains(testCase.TestId);
            }

            return true;
        }

        private static void WriteSuiteList(IReadOnlyList<TestSuiteDescriptor> suites)
        {
            foreach (TestSuiteDescriptor suite in suites)
            {
                Console.WriteLine(suite.SuiteId + " - " + suite.DisplayName);
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    Console.WriteLine("  " + testCase.TestId + " - " + testCase.DisplayName);
                }
            }
        }

        private static void WriteHelp()
        {
            Console.WriteLine("Usage: Test.Automated [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --results <path>               Write Touchstone JSON results.");
            Console.WriteLine("  --suite <id[,id]>              Run only matching suite IDs.");
            Console.WriteLine("  --case <id[,id]>               Run only matching case IDs or full test IDs.");
            Console.WriteLine("  --transactions                 Run transaction cases only.");
            Console.WriteLine("  --transaction-concurrency      Run isolated parallel transaction cases only.");
            Console.WriteLine("  --list                         List suite and case IDs.");
            Console.WriteLine("  --help                         Show this help.");
        }

        private static void WriteTransactionConcurrencySummary(IReadOnlyList<TestSuiteDescriptor> suites, int exitCode)
        {
            int caseCount = suites.Sum(s => s.Cases.Count);
            Console.WriteLine();
            Console.WriteLine("Transaction concurrency correctness: " + (exitCode == 0 ? "PASS" : "FAIL") + " (" + caseCount + " selected cases)");
        }

        private sealed class AutomatedOptions
        {
            public string? ResultsPath { get; private set; }
            public bool List { get; private set; }
            public bool Help { get; private set; }
            public bool TransactionsOnly { get; private set; }
            public bool TransactionConcurrencyOnly { get; private set; }
            public HashSet<string> SuiteIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> CaseIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool HasFilters
            {
                get
                {
                    return TransactionsOnly
                        || TransactionConcurrencyOnly
                        || SuiteIds.Count > 0
                        || CaseIds.Count > 0;
                }
            }

            public static AutomatedOptions Parse(string[] args)
            {
                AutomatedOptions options = new AutomatedOptions();
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    switch (arg)
                    {
                        case "--results":
                            options.ResultsPath = RequireValue(args, ref i, arg);
                            break;
                        case "--suite":
                            AddCsv(options.SuiteIds, RequireValue(args, ref i, arg));
                            break;
                        case "--case":
                            AddCsv(options.CaseIds, RequireValue(args, ref i, arg));
                            break;
                        case "--transactions":
                            options.TransactionsOnly = true;
                            break;
                        case "--transaction-concurrency":
                            options.TransactionConcurrencyOnly = true;
                            options.TransactionsOnly = true;
                            break;
                        case "--list":
                            options.List = true;
                            break;
                        case "--help":
                        case "-h":
                        case "/?":
                            options.Help = true;
                            break;
                        default:
                            throw new ArgumentException("Unknown argument '" + arg + "'.");
                    }
                }

                return options;
            }

            private static string RequireValue(string[] args, ref int index, string name)
            {
                if (index + 1 >= args.Length) throw new ArgumentException("Missing value for " + name + ".");
                string value = args[++index];
                if (String.IsNullOrWhiteSpace(value) || value.StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Missing value for " + name + ".");
                return value;
            }

            private static void AddCsv(HashSet<string> values, string csv)
            {
                foreach (string value in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = value.Trim();
                    if (!String.IsNullOrWhiteSpace(trimmed)) values.Add(trimmed);
                }
            }
        }
    }
}
