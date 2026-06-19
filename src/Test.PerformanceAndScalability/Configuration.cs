namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using LiteGraph;

    internal sealed class BenchmarkOptions
    {
        public bool Help { get; private set; }
        public bool DryRun { get; private set; }

        public DatabaseTypeEnum DbType { get; private set; } = DatabaseTypeEnum.Sqlite;
        public string? ConnectionString { get; private set; }
        public string? SqliteFile { get; private set; }
        public bool InMemory { get; private set; }
        public string Hostname { get; private set; } = "localhost";
        public int? Port { get; private set; }
        public string DatabaseName { get; private set; } = "litegraph";
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public string Schema { get; private set; } = "litegraph";
        public int MaxConnections { get; private set; } = 32;
        public int CommandTimeoutSeconds { get; private set; } = 30;

        public string Profile { get; private set; } = "smoke";
        public List<string> Workloads { get; private set; } = new List<string> { "all" };
        public string OperationMix { get; private set; } = "balanced";
        public double ScaleFactor { get; private set; } = 1;
        public int Tenants { get; private set; }
        public int GraphsPerTenant { get; private set; }
        public int NodesPerGraph { get; private set; }
        public int EdgesPerGraph { get; private set; }
        public int VectorsPerGraph { get; private set; }
        public int LabelsPerNode { get; private set; }
        public int TagsPerNode { get; private set; }
        public string PayloadSize { get; private set; } = "small";
        public int VectorDimensions { get; private set; }
        public int VectorTopK { get; private set; }
        public string Topology { get; private set; } = "random";

        public List<int> Concurrency { get; private set; } = new List<int> { 1 };
        public TimeSpan Duration { get; private set; }
        public TimeSpan Warmup { get; private set; }
        public TimeSpan Cooldown { get; private set; }
        public int Iterations { get; private set; } = 1;
        public double? TargetRate { get; private set; }
        public bool ClosedLoop { get; private set; } = true;
        public int BatchSize { get; private set; } = 100;
        public int TransactionSize { get; private set; } = 10;
        public TransactionIsolationLevelEnum TransactionIsolation { get; private set; } = TransactionIsolationLevelEnum.Default;
        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(30);
        public int Seed { get; private set; } = 12345;

        public string? Output { get; private set; }
        public string? RunId { get; private set; }
        public bool IncludeQueryProfile { get; private set; }
        public bool SampleCorrectness { get; private set; } = true;
        public double SampleRate { get; private set; } = 0.01;
        public bool CaptureProcessMetrics { get; private set; } = true;
        public bool CaptureRepositoryTelemetry { get; private set; } = true;
        public bool CaptureDbFileMetrics { get; private set; } = true;
        public bool KeepDatabase { get; private set; }
        public bool FailOnRegression { get; private set; }
        public string? Baseline { get; private set; }

        public bool CreatedTemporaryDatabase { get; set; }
        public string? EffectiveSqliteFile { get; set; }
        public string OutputDirectory { get; set; } = string.Empty;

        public static BenchmarkOptions Parse(string[] args)
        {
            BenchmarkOptions options = new BenchmarkOptions();
            Dictionary<string, string?> parsed = ParsePairs(args);

            if (parsed.ContainsKey("help") || parsed.ContainsKey("h") || parsed.ContainsKey("?"))
            {
                options.Help = true;
                return options;
            }

            foreach (KeyValuePair<string, string?> pair in parsed)
            {
                string key = pair.Key;
                string? value = pair.Value;

                switch (key)
                {
                    case "db-type":
                        options.DbType = ParseDatabaseType(RequireValue(key, value));
                        break;
                    case "connection-string":
                        options.ConnectionString = RequireValue(key, value);
                        break;
                    case "sqlite-file":
                        options.SqliteFile = RequireValue(key, value);
                        break;
                    case "in-memory":
                        options.InMemory = ParseBool(key, value);
                        break;
                    case "host":
                    case "hostname":
                        options.Hostname = RequireValue(key, value);
                        break;
                    case "port":
                        options.Port = ParseInt(key, value, 0, 65535);
                        break;
                    case "database":
                    case "database-name":
                        options.DatabaseName = RequireValue(key, value);
                        break;
                    case "username":
                        options.Username = RequireValue(key, value);
                        break;
                    case "password":
                        options.Password = RequireValue(key, value);
                        break;
                    case "schema":
                        options.Schema = RequireValue(key, value);
                        break;
                    case "max-connections":
                        options.MaxConnections = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "command-timeout-seconds":
                        options.CommandTimeoutSeconds = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "profile":
                        options.Profile = RequireValue(key, value).ToLowerInvariant();
                        break;
                    case "workloads":
                        options.Workloads = SplitList(RequireValue(key, value));
                        break;
                    case "operation-mix":
                        options.OperationMix = RequireValue(key, value).ToLowerInvariant();
                        break;
                    case "scale-factor":
                        options.ScaleFactor = ParseDouble(key, value, 0.0001, double.MaxValue);
                        break;
                    case "tenants":
                        options.Tenants = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "graphs-per-tenant":
                        options.GraphsPerTenant = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "nodes-per-graph":
                        options.NodesPerGraph = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "edges-per-graph":
                        options.EdgesPerGraph = ParseInt(key, value, 0, int.MaxValue);
                        break;
                    case "vectors-per-graph":
                        options.VectorsPerGraph = ParseInt(key, value, 0, int.MaxValue);
                        break;
                    case "labels-per-node":
                        options.LabelsPerNode = ParseInt(key, value, 0, int.MaxValue);
                        break;
                    case "tags-per-node":
                        options.TagsPerNode = ParseInt(key, value, 0, int.MaxValue);
                        break;
                    case "payload-size":
                        options.PayloadSize = RequireValue(key, value).ToLowerInvariant();
                        break;
                    case "vector-dimensions":
                        options.VectorDimensions = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "vector-top-k":
                        options.VectorTopK = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "topology":
                        options.Topology = RequireValue(key, value).ToLowerInvariant();
                        break;
                    case "concurrency":
                        options.Concurrency = ParseConcurrency(RequireValue(key, value));
                        break;
                    case "duration":
                        options.Duration = ParseTimeSpan(key, value);
                        break;
                    case "warmup":
                        options.Warmup = ParseTimeSpan(key, value);
                        break;
                    case "cooldown":
                        options.Cooldown = ParseTimeSpan(key, value);
                        break;
                    case "iterations":
                        options.Iterations = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "target-rate":
                        options.TargetRate = ParseDouble(key, value, 0.0001, double.MaxValue);
                        options.ClosedLoop = false;
                        break;
                    case "closed-loop":
                        options.ClosedLoop = ParseBool(key, value);
                        break;
                    case "batch-size":
                        options.BatchSize = ParseInt(key, value, 1, int.MaxValue);
                        break;
                    case "transaction-size":
                        options.TransactionSize = ParseInt(key, value, 1, 1000);
                        break;
                    case "transaction-isolation":
                    case "isolation-level":
                        options.TransactionIsolation = ParseTransactionIsolation(RequireValue(key, value));
                        break;
                    case "timeout":
                        options.Timeout = ParseTimeSpan(key, value);
                        break;
                    case "seed":
                        options.Seed = ParseInt(key, value, int.MinValue, int.MaxValue);
                        break;
                    case "output":
                        options.Output = RequireValue(key, value);
                        break;
                    case "run-id":
                        options.RunId = RequireValue(key, value);
                        break;
                    case "include-query-profile":
                        options.IncludeQueryProfile = ParseBool(key, value);
                        break;
                    case "sample-correctness":
                        options.SampleCorrectness = ParseBool(key, value);
                        break;
                    case "sample-rate":
                        options.SampleRate = ParseDouble(key, value, 0, 1);
                        break;
                    case "capture-process-metrics":
                        options.CaptureProcessMetrics = ParseBool(key, value);
                        break;
                    case "capture-repository-telemetry":
                        options.CaptureRepositoryTelemetry = ParseBool(key, value);
                        break;
                    case "capture-db-file-metrics":
                        options.CaptureDbFileMetrics = ParseBool(key, value);
                        break;
                    case "keep-database":
                        options.KeepDatabase = ParseBool(key, value);
                        break;
                    case "dry-run":
                        options.DryRun = ParseBool(key, value);
                        break;
                    case "fail-on-regression":
                        options.FailOnRegression = ParseBool(key, value);
                        break;
                    case "baseline":
                        options.Baseline = RequireValue(key, value);
                        break;
                    default:
                        throw new ArgumentException("Unknown option '--" + key + "'. Use --help for supported options.");
                }
            }

            options.ApplyProfileDefaults(parsed.Keys);
            options.Validate();
            return options;
        }

        public DatabaseSettings BuildDatabaseSettings()
        {
            DatabaseSettings settings = new DatabaseSettings
            {
                Type = DbType,
                InMemory = InMemory,
                Hostname = Hostname,
                Port = Port,
                DatabaseName = DatabaseName,
                Username = Username,
                Password = Password,
                Schema = Schema,
                ConnectionString = ConnectionString,
                MaxConnections = MaxConnections,
                CommandTimeoutSeconds = CommandTimeoutSeconds
            };

            if (DbType == DatabaseTypeEnum.Sqlite)
            {
                string filename = EffectiveSqliteFile
                    ?? SqliteFile
                    ?? Path.Combine(Path.GetTempPath(), "litegraph-perf", RunId ?? CreateRunId(), "litegraph.db");

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filename))!);
                settings.Filename = filename;
                EffectiveSqliteFile = filename;
                CreatedTemporaryDatabase = SqliteFile == null;
            }

            return settings;
        }

        public void PrepareRun()
        {
            RunId ??= CreateRunId();
            OutputDirectory = string.IsNullOrWhiteSpace(Output)
                ? Path.Combine("artifacts", "perf-scale", RunId)
                : Output;
            Directory.CreateDirectory(OutputDirectory);
        }

        public object ToRedactedObject(DatabaseSettings? settings = null)
        {
            return new
            {
                DbType,
                Database = settings?.ToSafeString(),
                SqliteFile = DbType == DatabaseTypeEnum.Sqlite ? EffectiveSqliteFile ?? SqliteFile : null,
                InMemory,
                Hostname,
                Port,
                DatabaseName,
                Username,
                Password = Password == null ? null : "***",
                Schema,
                ConnectionString = ConnectionString == null ? null : "***",
                MaxConnections,
                CommandTimeoutSeconds,
                Profile,
                Workloads,
                OperationMix,
                ScaleFactor,
                Tenants,
                GraphsPerTenant,
                NodesPerGraph,
                EdgesPerGraph,
                VectorsPerGraph,
                LabelsPerNode,
                TagsPerNode,
                PayloadSize,
                VectorDimensions,
                VectorTopK,
                Topology,
                Concurrency,
                Duration = Duration.ToString(),
                Warmup = Warmup.ToString(),
                Cooldown = Cooldown.ToString(),
                Iterations,
                TargetRate,
                ClosedLoop,
                BatchSize,
                TransactionSize,
                TransactionIsolation,
                Timeout = Timeout.ToString(),
                Seed,
                OutputDirectory,
                RunId,
                IncludeQueryProfile,
                SampleCorrectness,
                SampleRate,
                CaptureProcessMetrics,
                CaptureRepositoryTelemetry,
                CaptureDbFileMetrics,
                KeepDatabase,
                DryRun,
                FailOnRegression,
                Baseline
            };
        }

        public static string CreateRunId()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public static string HelpText()
        {
            return
@"LiteGraph performance and scalability harness

Usage:
  dotnet run --project src/Test.PerformanceAndScalability -- [options]

Defaults:
  With no arguments, the harness creates a temporary SQLite file, runs the smoke profile,
  writes artifacts under artifacts/perf-scale/{run-id}, and deletes the temporary database.

Provider options:
  --db-type sqlite|postgresql
  --connection-string <value>
  --sqlite-file <path>
  --in-memory true|false
  --host <hostname>
  --port <port>
  --database <name>
  --username <name>
  --password <value>
  --schema <schema>
  --max-connections <count>
  --command-timeout-seconds <seconds>

Run options:
  --profile smoke|small|medium|large|soak|custom
  --workloads all|ingest,reads,search,traversal,query,vector,transactions,updates,deletes,maintenance,mixed,stress
  --operation-mix read-heavy|write-heavy|balanced|vector-heavy|transaction-heavy|hotspot|tenant-isolated|graph-isolated
  --topology random|power-law|grid|tree|chain|hub|communities|dense
  --concurrency 1,2,4,8 or 1..64x2
  --duration 00:00:10
  --warmup 00:00:02
  --target-rate <ops-per-second>
  --closed-loop true|false
  --batch-size <count>
  --transaction-size <count>
  --transaction-isolation default|read-committed|repeatable-read|serializable
  --seed <integer>

Output options:
  --output <directory>
  --run-id <id>
  --include-query-profile true|false
  --keep-database true|false
  --baseline <summary.json>
  --fail-on-regression true|false
  --dry-run true|false";
        }

        private void ApplyProfileDefaults(IEnumerable<string> suppliedKeys)
        {
            HashSet<string> supplied = new HashSet<string>(suppliedKeys, StringComparer.OrdinalIgnoreCase);

            ProfileDefaults defaults = ProfileDefaults.For(Profile);

            Tenants = supplied.Contains("tenants") ? Tenants : Scale(defaults.Tenants);
            GraphsPerTenant = supplied.Contains("graphs-per-tenant") ? GraphsPerTenant : Scale(defaults.GraphsPerTenant);
            NodesPerGraph = supplied.Contains("nodes-per-graph") ? NodesPerGraph : Scale(defaults.NodesPerGraph);
            EdgesPerGraph = supplied.Contains("edges-per-graph") ? EdgesPerGraph : Scale(defaults.EdgesPerGraph);
            VectorsPerGraph = supplied.Contains("vectors-per-graph") ? VectorsPerGraph : Scale(defaults.VectorsPerGraph);
            LabelsPerNode = supplied.Contains("labels-per-node") ? LabelsPerNode : defaults.LabelsPerNode;
            TagsPerNode = supplied.Contains("tags-per-node") ? TagsPerNode : defaults.TagsPerNode;
            VectorDimensions = supplied.Contains("vector-dimensions") ? VectorDimensions : defaults.VectorDimensions;
            VectorTopK = supplied.Contains("vector-top-k") ? VectorTopK : defaults.VectorTopK;
            Duration = supplied.Contains("duration") ? Duration : defaults.Duration;
            Warmup = supplied.Contains("warmup") ? Warmup : defaults.Warmup;
            Cooldown = supplied.Contains("cooldown") ? Cooldown : defaults.Cooldown;
        }

        private int Scale(int value)
        {
            return Math.Max(value == 0 ? 0 : 1, (int)Math.Round(value * ScaleFactor, MidpointRounding.AwayFromZero));
        }

        private void Validate()
        {
            if (DbType != DatabaseTypeEnum.Sqlite && InMemory)
                throw new ArgumentException("--in-memory is only valid for SQLite.");

            if (DbType == DatabaseTypeEnum.Sqlite && !string.IsNullOrWhiteSpace(ConnectionString))
                throw new ArgumentException("--connection-string is not used with SQLite. Use --sqlite-file instead.");

            if (Tenants < 1 || GraphsPerTenant < 1 || NodesPerGraph < 1)
                throw new ArgumentException("Tenants, graphs per tenant, and nodes per graph must all be at least 1.");

            if (!KnownPayloadSizes.Contains(PayloadSize))
                throw new ArgumentException("Unsupported payload size '" + PayloadSize + "'. Use small, medium, or large.");

            if (!KnownTopologies.Contains(Topology))
                throw new ArgumentException("Unsupported topology '" + Topology + "'. Use random, power-law, grid, tree, chain, hub, communities, or dense.");

            if (Concurrency.Count < 1 || Concurrency.Any(c => c < 1))
                throw new ArgumentException("At least one positive concurrency value is required.");

            if (Duration <= TimeSpan.Zero)
                throw new ArgumentException("--duration must be greater than zero.");

            if (Warmup < TimeSpan.Zero || Cooldown < TimeSpan.Zero)
                throw new ArgumentException("--warmup and --cooldown cannot be negative.");

            if (!ClosedLoop && TargetRate == null)
                throw new ArgumentException("--target-rate is required when --closed-loop false is used.");
        }

        private static Dictionary<string, string?> ParsePairs(string[] args)
        {
            Dictionary<string, string?> ret = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Unexpected argument '" + arg + "'. Options must start with --.");

                string body = arg.Substring(2);
                string key;
                string? value = null;

                int equals = body.IndexOf('=');
                if (equals >= 0)
                {
                    key = body.Substring(0, equals).Trim().ToLowerInvariant();
                    value = body.Substring(equals + 1);
                }
                else
                {
                    key = body.Trim().ToLowerInvariant();
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        value = args[++i];
                    }
                }

                ret[key] = value;
            }

            return ret;
        }

        private static string RequireValue(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("--" + key + " requires a value.");
            return value;
        }

        private static bool ParseBool(string key, string? value)
        {
            if (value == null) return true;
            if (bool.TryParse(value, out bool ret)) return ret;

            switch (value.Trim().ToLowerInvariant())
            {
                case "1":
                case "yes":
                case "y":
                case "on":
                    return true;
                case "0":
                case "no":
                case "n":
                case "off":
                    return false;
                default:
                    throw new ArgumentException("--" + key + " requires a boolean value.");
            }
        }

        private static int ParseInt(string key, string? value, int min, int max)
        {
            if (!int.TryParse(RequireValue(key, value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ret))
                throw new ArgumentException("--" + key + " requires an integer value.");
            if (ret < min || ret > max)
                throw new ArgumentOutOfRangeException(key, ret, "--" + key + " must be between " + min + " and " + max + ".");
            return ret;
        }

        private static double ParseDouble(string key, string? value, double min, double max)
        {
            if (!double.TryParse(RequireValue(key, value), NumberStyles.Float, CultureInfo.InvariantCulture, out double ret))
                throw new ArgumentException("--" + key + " requires a numeric value.");
            if (ret < min || ret > max)
                throw new ArgumentOutOfRangeException(key, ret, "--" + key + " must be between " + min + " and " + max + ".");
            return ret;
        }

        private static TimeSpan ParseTimeSpan(string key, string? value)
        {
            string val = RequireValue(key, value);
            if (TimeSpan.TryParse(val, CultureInfo.InvariantCulture, out TimeSpan ret)) return ret;
            if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)) return TimeSpan.FromSeconds(seconds);
            throw new ArgumentException("--" + key + " requires a TimeSpan or seconds value.");
        }

        private static List<string> SplitList(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => v.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<int> ParseConcurrency(string value)
        {
            if (!value.Contains("..", StringComparison.Ordinal))
            {
                return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(v => int.Parse(v, CultureInfo.InvariantCulture))
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList();
            }

            string[] rangeAndStep = value.Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string[] bounds = rangeAndStep[0].Split("..", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (bounds.Length != 2) throw new ArgumentException("Invalid concurrency range '" + value + "'.");

            int start = int.Parse(bounds[0], CultureInfo.InvariantCulture);
            int end = int.Parse(bounds[1], CultureInfo.InvariantCulture);
            int multiplier = rangeAndStep.Length == 2 ? int.Parse(rangeAndStep[1], CultureInfo.InvariantCulture) : 1;

            List<int> ret = new List<int>();
            if (multiplier > 1)
            {
                for (int current = start; current <= end; current *= multiplier)
                {
                    ret.Add(current);
                    if (current > int.MaxValue / multiplier) break;
                }
            }
            else
            {
                for (int current = start; current <= end; current++) ret.Add(current);
            }

            return ret.Distinct().OrderBy(v => v).ToList();
        }

        private static DatabaseTypeEnum ParseDatabaseType(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "sqlite":
                    return DatabaseTypeEnum.Sqlite;
                case "postgres":
                case "postgresql":
                    return DatabaseTypeEnum.Postgresql;
                default:
                    throw new ArgumentException("Unsupported database type '" + value + "'.");
            }
        }

        private static TransactionIsolationLevelEnum ParseTransactionIsolation(string value)
        {
            string normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty);
            if (Enum.TryParse(normalized, true, out TransactionIsolationLevelEnum ret)) return ret;
            throw new ArgumentException("Unsupported transaction isolation level '" + value + "'. Use default, read-committed, repeatable-read, or serializable.");
        }

        private static readonly HashSet<string> KnownPayloadSizes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "small",
            "medium",
            "large"
        };

        private static readonly HashSet<string> KnownTopologies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "random",
            "power-law",
            "powerlaw",
            "grid",
            "tree",
            "chain",
            "hub",
            "communities",
            "dense"
        };
    }

    internal sealed class ProfileDefaults
    {
        public int Tenants { get; init; }
        public int GraphsPerTenant { get; init; }
        public int NodesPerGraph { get; init; }
        public int EdgesPerGraph { get; init; }
        public int VectorsPerGraph { get; init; }
        public int LabelsPerNode { get; init; }
        public int TagsPerNode { get; init; }
        public int VectorDimensions { get; init; }
        public int VectorTopK { get; init; }
        public TimeSpan Duration { get; init; }
        public TimeSpan Warmup { get; init; }
        public TimeSpan Cooldown { get; init; }

        public static ProfileDefaults For(string profile)
        {
            switch (profile.ToLowerInvariant())
            {
                case "smoke":
                    return new ProfileDefaults
                    {
                        Tenants = 1,
                        GraphsPerTenant = 1,
                        NodesPerGraph = 100,
                        EdgesPerGraph = 200,
                        VectorsPerGraph = 40,
                        LabelsPerNode = 2,
                        TagsPerNode = 2,
                        VectorDimensions = 128,
                        VectorTopK = 10,
                        Duration = TimeSpan.FromSeconds(1),
                        Warmup = TimeSpan.FromMilliseconds(250),
                        Cooldown = TimeSpan.Zero
                    };
                case "small":
                    return new ProfileDefaults
                    {
                        Tenants = 1,
                        GraphsPerTenant = 3,
                        NodesPerGraph = 1000,
                        EdgesPerGraph = 2500,
                        VectorsPerGraph = 300,
                        LabelsPerNode = 3,
                        TagsPerNode = 3,
                        VectorDimensions = 384,
                        VectorTopK = 20,
                        Duration = TimeSpan.FromSeconds(10),
                        Warmup = TimeSpan.FromSeconds(2),
                        Cooldown = TimeSpan.FromSeconds(1)
                    };
                case "medium":
                    return new ProfileDefaults
                    {
                        Tenants = 2,
                        GraphsPerTenant = 5,
                        NodesPerGraph = 10000,
                        EdgesPerGraph = 30000,
                        VectorsPerGraph = 3000,
                        LabelsPerNode = 4,
                        TagsPerNode = 4,
                        VectorDimensions = 768,
                        VectorTopK = 25,
                        Duration = TimeSpan.FromSeconds(30),
                        Warmup = TimeSpan.FromSeconds(5),
                        Cooldown = TimeSpan.FromSeconds(2)
                    };
                case "large":
                    return new ProfileDefaults
                    {
                        Tenants = 4,
                        GraphsPerTenant = 10,
                        NodesPerGraph = 100000,
                        EdgesPerGraph = 300000,
                        VectorsPerGraph = 25000,
                        LabelsPerNode = 5,
                        TagsPerNode = 5,
                        VectorDimensions = 1536,
                        VectorTopK = 50,
                        Duration = TimeSpan.FromMinutes(2),
                        Warmup = TimeSpan.FromSeconds(15),
                        Cooldown = TimeSpan.FromSeconds(5)
                    };
                case "soak":
                    return new ProfileDefaults
                    {
                        Tenants = 2,
                        GraphsPerTenant = 4,
                        NodesPerGraph = 5000,
                        EdgesPerGraph = 15000,
                        VectorsPerGraph = 1000,
                        LabelsPerNode = 4,
                        TagsPerNode = 4,
                        VectorDimensions = 768,
                        VectorTopK = 25,
                        Duration = TimeSpan.FromHours(1),
                        Warmup = TimeSpan.FromSeconds(30),
                        Cooldown = TimeSpan.FromSeconds(5)
                    };
                case "custom":
                    return new ProfileDefaults
                    {
                        Tenants = 1,
                        GraphsPerTenant = 1,
                        NodesPerGraph = 100,
                        EdgesPerGraph = 200,
                        VectorsPerGraph = 40,
                        LabelsPerNode = 2,
                        TagsPerNode = 2,
                        VectorDimensions = 128,
                        VectorTopK = 10,
                        Duration = TimeSpan.FromSeconds(10),
                        Warmup = TimeSpan.FromSeconds(1),
                        Cooldown = TimeSpan.Zero
                    };
                default:
                    throw new ArgumentException("Unsupported profile '" + profile + "'.");
            }
        }
    }
}
