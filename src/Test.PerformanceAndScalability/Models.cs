namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using LiteGraph;

    internal sealed class BenchmarkContext
    {
        public BenchmarkOptions Options { get; init; } = null!;
        public DatabaseSettings DatabaseSettings { get; init; } = null!;
        public LiteGraphClient Client { get; init; } = null!;
        public DatasetState Dataset { get; set; } = null!;
        public TelemetryCollector Telemetry { get; init; } = null!;
        public ProcessMetricsCollector ProcessMetrics { get; init; } = null!;
        public List<StorageMetricSample> StorageMetrics { get; init; } = new List<StorageMetricSample>();
        public ConcurrentBag<QueryProfileSample> QueryProfiles { get; init; } = new ConcurrentBag<QueryProfileSample>();
    }

    internal sealed class DatasetState
    {
        public List<GraphDataset> Graphs { get; } = new List<GraphDataset>();
        public DatasetMetadata Metadata { get; set; } = new DatasetMetadata();

        public GraphDataset PickGraph(Random random)
        {
            return Graphs[random.Next(Graphs.Count)];
        }
    }

    internal sealed class GraphDataset
    {
        public TenantMetadata Tenant { get; init; } = null!;
        public Graph Graph { get; init; } = null!;
        public List<Node> Nodes { get; } = new List<Node>();
        public List<Edge> Edges { get; } = new List<Edge>();
        public List<VectorMetadata> Vectors { get; } = new List<VectorMetadata>();
        public List<Guid> HotNodeGuids { get; } = new List<Guid>();
        public List<RouteFixture> RouteFixtures { get; } = new List<RouteFixture>();

        public Node PickNode(Random random)
        {
            return Nodes[random.Next(Nodes.Count)];
        }

        public Edge? PickEdge(Random random)
        {
            if (Edges.Count < 1) return null;
            return Edges[random.Next(Edges.Count)];
        }

        public VectorMetadata? PickVector(Random random)
        {
            if (Vectors.Count < 1) return null;
            return Vectors[random.Next(Vectors.Count)];
        }

        public Guid PickHotOrRandomNodeGuid(Random random)
        {
            if (HotNodeGuids.Count > 0 && random.NextDouble() < 0.8) return HotNodeGuids[random.Next(HotNodeGuids.Count)];
            return PickNode(random).GUID;
        }

        public RouteFixture? PickRouteFixture(Random random)
        {
            if (RouteFixtures.Count < 1) return null;
            return RouteFixtures[random.Next(RouteFixtures.Count)];
        }
    }

    internal sealed class RouteFixture
    {
        public List<Guid> NodeGuids { get; } = new List<Guid>();
        public List<Guid> EdgeGuids { get; } = new List<Guid>();

        public Guid FromNodeGuid
        {
            get { return NodeGuids.Count == 0 ? Guid.Empty : NodeGuids[0]; }
        }

        public Guid ToNodeGuid
        {
            get { return NodeGuids.Count == 0 ? Guid.Empty : NodeGuids[NodeGuids.Count - 1]; }
        }
    }

    internal sealed class DatasetMetadata
    {
        public int Tenants { get; set; }
        public int Graphs { get; set; }
        public int Nodes { get; set; }
        public int Edges { get; set; }
        public int Vectors { get; set; }
        public int LabelsPerNode { get; set; }
        public int TagsPerNode { get; set; }
        public string PayloadSize { get; set; } = string.Empty;
        public string Topology { get; set; } = string.Empty;
        public int VectorDimensions { get; set; }
        public double AverageDegree { get; set; }
        public int MaxDegree { get; set; }
        public int Seed { get; set; }
        public double GenerationDurationMs { get; set; }
    }

    internal sealed class BenchmarkScenario
    {
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool RunOnce { get; init; }
        public Func<BenchmarkContext, WorkerContext, System.Threading.CancellationToken, System.Threading.Tasks.Task<OperationOutcome>> Operation { get; init; } = null!;
    }

    internal sealed class WorkerContext
    {
        public int WorkerId { get; init; }
        public Random Random { get; init; } = null!;
        public long OperationIndex { get; set; }
    }

    internal sealed class OperationOutcome
    {
        public int Items { get; init; } = 1;
        public int ResultCount { get; init; }
        public bool Correct { get; init; } = true;
        public string? Error { get; init; }

        public static OperationOutcome Success(int items = 1, int resultCount = 0)
        {
            return new OperationOutcome
            {
                Items = items,
                ResultCount = resultCount,
                Correct = true
            };
        }

        public static OperationOutcome Incorrect(int items = 1, int resultCount = 0, string? error = null)
        {
            return new OperationOutcome
            {
                Items = items,
                ResultCount = resultCount,
                Correct = false,
                Error = error
            };
        }
    }

    internal sealed class ScenarioResult
    {
        public string Scenario { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Iteration { get; set; }
        public int Concurrency { get; set; }
        public bool OpenLoop { get; set; }
        public double? TargetRate { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime CompletedUtc { get; set; }
        public double DurationMs { get; set; }
        public long Attempted { get; set; }
        public long Completed { get; set; }
        public long Failed { get; set; }
        public long TimedOut { get; set; }
        public long Canceled { get; set; }
        public long Incorrect { get; set; }
        public long Items { get; set; }
        public long ResultCount { get; set; }
        public double OperationsPerSecond { get; set; }
        public double ItemsPerSecond { get; set; }
        public LatencyStats Latency { get; set; } = new LatencyStats();
        public ProcessMetricDelta ProcessDelta { get; set; } = new ProcessMetricDelta();
        public StorageMetricSample? StorageBefore { get; set; }
        public StorageMetricSample? StorageAfter { get; set; }
        public List<RepositoryTelemetrySummary> RepositoryTelemetry { get; set; } = new List<RepositoryTelemetrySummary>();
        public List<VectorTelemetrySummary> VectorTelemetry { get; set; } = new List<VectorTelemetrySummary>();
        public List<QueryProfileSample> QueryProfiles { get; set; } = new List<QueryProfileSample>();
        public string? ErrorSample { get; set; }
    }

    internal sealed class LatencyStats
    {
        public long Count { get; set; }
        public double MinMs { get; set; }
        public double MeanMs { get; set; }
        public double StdDevMs { get; set; }
        public double P50Ms { get; set; }
        public double P90Ms { get; set; }
        public double P95Ms { get; set; }
        public double P99Ms { get; set; }
        public double P999Ms { get; set; }
        public double MaxMs { get; set; }

        public static LatencyStats From(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return new LatencyStats();

            List<double> sorted = new List<double>(values);
            sorted.Sort();

            double sum = 0;
            for (int i = 0; i < sorted.Count; i++) sum += sorted[i];
            double mean = sum / sorted.Count;

            double variance = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                double delta = sorted[i] - mean;
                variance += delta * delta;
            }

            return new LatencyStats
            {
                Count = sorted.Count,
                MinMs = sorted[0],
                MeanMs = mean,
                StdDevMs = Math.Sqrt(variance / sorted.Count),
                P50Ms = Percentile(sorted, 0.50),
                P90Ms = Percentile(sorted, 0.90),
                P95Ms = Percentile(sorted, 0.95),
                P99Ms = Percentile(sorted, 0.99),
                P999Ms = Percentile(sorted, 0.999),
                MaxMs = sorted[sorted.Count - 1]
            };
        }

        private static double Percentile(List<double> sorted, double percentile)
        {
            if (sorted.Count == 0) return 0;
            double position = (sorted.Count - 1) * percentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            if (lower == upper) return sorted[lower];
            double weight = position - lower;
            return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
        }
    }

    internal sealed class RepositoryTelemetrySummary
    {
        public string Scenario { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Transactional { get; set; }
        public long Count { get; set; }
        public long StatementCount { get; set; }
        public long RowCount { get; set; }
        public double TotalDurationMs { get; set; }
        public double AverageDurationMs { get; set; }
    }

    internal sealed class VectorTelemetrySummary
    {
        public string Scenario { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public bool Success { get; set; }
        public long Count { get; set; }
        public long ResultCount { get; set; }
        public double TotalDurationMs { get; set; }
        public double AverageDurationMs { get; set; }
    }

    internal sealed class QueryProfileSample
    {
        public string Scenario { get; set; } = string.Empty;
        public double ParseTimeMs { get; set; }
        public double PlanTimeMs { get; set; }
        public double ExecuteTimeMs { get; set; }
        public double RepositoryTimeMs { get; set; }
        public int RepositoryOperationCount { get; set; }
        public double VectorSearchTimeMs { get; set; }
        public int VectorSearchCount { get; set; }
        public double TransactionTimeMs { get; set; }
        public double TotalTimeMs { get; set; }
        public int RowCount { get; set; }
    }

    internal sealed class ProcessMetricSample
    {
        public DateTime TimestampUtc { get; set; }
        public string Scenario { get; set; } = string.Empty;
        public double CpuPercent { get; set; }
        public long WorkingSetBytes { get; set; }
        public long PrivateBytes { get; set; }
        public long ManagedHeapBytes { get; set; }
        public long AllocatedBytes { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
    }

    internal sealed class ProcessMetricDelta
    {
        public long AllocatedBytes { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long WorkingSetDeltaBytes { get; set; }
    }

    internal sealed class StorageMetricSample
    {
        public DateTime TimestampUtc { get; set; }
        public string Scenario { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? DatabaseFile { get; set; }
        public long DatabaseBytes { get; set; }
        public long WalBytes { get; set; }
        public long ShmBytes { get; set; }
        public long IndexBytes { get; set; }
    }

    internal sealed class EnvironmentSnapshot
    {
        public string? GitSha { get; set; }
        public string? GitStatus { get; set; }
        public string LiteGraphAssemblyVersion { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public string OSDescription { get; set; } = string.Empty;
        public string ProcessArchitecture { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public bool IsServerGc { get; set; }
        public string GcLatencyMode { get; set; } = string.Empty;
        public DateTime CapturedUtc { get; set; }
    }

    internal sealed class BenchmarkRunSummary
    {
        public string RunId { get; set; } = string.Empty;
        public DateTime StartedUtc { get; set; }
        public DateTime CompletedUtc { get; set; }
        public object Configuration { get; set; } = null!;
        public EnvironmentSnapshot Environment { get; set; } = null!;
        public DatasetMetadata Dataset { get; set; } = null!;
        public List<ScenarioResult> Results { get; set; } = new List<ScenarioResult>();
        public List<string> Anomalies { get; set; } = new List<string>();
    }
}
