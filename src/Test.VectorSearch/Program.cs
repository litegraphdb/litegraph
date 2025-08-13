namespace Test.VectorSearch
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using GetSomeInput;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Serialization;

    class Program
    {
        static LiteGraphClient _Client = null;
        static Serializer _Serializer = new Serializer();
        static Random _Random = new Random();

        // Constants for the test
        static int _NodeCount = 1000;
        static int _VectorDimensionality = 384;
        static int _SearchCount = 5;
        static int _TopResults = 10;

        // Performance tracking
        static class PerformanceMetrics
        {
            public static long InitializationTime { get; set; }
            public static long TenantCreationTime { get; set; }
            public static long GraphCreationTime { get; set; }
            public static long TotalNodeCreationTime { get; set; }
            public static long TotalEmbeddingGenerationTime { get; set; }
            public static List<long> IndividualSearchTimes { get; set; } = new List<long>();
            public static Dictionary<int, long> SearchTimePerTestCase { get; set; } = new Dictionary<int, long>();
            public static long TotalSearchTime { get; set; }
            public static long QueryGenerationTime { get; set; }
            public static long NodeDeletionTime { get; set; }
            public static long GraphDeletionTime { get; set; }
            public static long TenantDeletionTime { get; set; }
            public static long TotalCleanupTime { get; set; }
            public static int NodesCreated { get; set; }
            public static int NodesDeleted { get; set; }
            public static List<int> SearchResultCounts { get; set; } = new List<int>();
            public static List<double> SearchScoreRanges { get; set; } = new List<double>();
            public static long Step1Time { get; set; }
            public static long Step2Time { get; set; }
            public static long Step3Time { get; set; }
            public static long Step4Time { get; set; }

            // Node insertion performance tracking
            public static List<long> IndividualNodeInsertionTimes { get; set; } = new List<long>();
            public static Dictionary<int, double> BatchAverageInsertionTimes { get; set; } = new Dictionary<int, double>();
            public static double FirstBatchAvgTime { get; set; }
            public static double LastBatchAvgTime { get; set; }
            public static double InsertionDegradationRate { get; set; }
            public static long MinInsertionTime { get; set; } = long.MaxValue;
            public static long MaxInsertionTime { get; set; } = 0;
            public static int MinInsertionIndex { get; set; }
            public static int MaxInsertionIndex { get; set; }
        }

        // Node data class
        class NodeTestData
        {
            public int Index { get; set; }
            public string Category { get; set; }
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
        }

        static void Main(string[] args)
        {
            var totalStopwatch = Stopwatch.StartNew();
            DateTime testStartTime = DateTime.Now;

            Console.WriteLine("=== LiteGraph Vector Search Test ===");
            Console.WriteLine($"Test started at: {testStartTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Creating {_NodeCount} nodes with {_VectorDimensionality}-dimensional vectors");
            Console.WriteLine();

            // Initialize client
            var initStopwatch = Stopwatch.StartNew();
            _Client = new LiteGraphClient(new SqliteGraphRepository("litegraph.db", true));
            _Client.Logging.MinimumSeverity = 0;
            _Client.Logging.Logger = null;
            _Client.InitializeRepository();
            initStopwatch.Stop();
            PerformanceMetrics.InitializationTime = initStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Client initialization completed in {initStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();

            TenantMetadata tenant = null;
            Graph graph = null;
            List<Node> nodes = new List<Node>();

            try
            {
                // Step 1: Create tenant and graph
                Console.WriteLine("Step 1: Creating tenant and graph...");
                var step1Stopwatch = Stopwatch.StartNew();

                var tenantStopwatch = Stopwatch.StartNew();
                tenant = CreateTenant();
                tenantStopwatch.Stop();
                PerformanceMetrics.TenantCreationTime = tenantStopwatch.ElapsedMilliseconds;

                var graphStopwatch = Stopwatch.StartNew();
                graph = CreateGraph(tenant.GUID);
                graphStopwatch.Stop();
                PerformanceMetrics.GraphCreationTime = graphStopwatch.ElapsedMilliseconds;

                step1Stopwatch.Stop();
                PerformanceMetrics.Step1Time = step1Stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"Created tenant: {tenant.GUID} in {tenantStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Created graph: {graph.GUID} in {graphStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Step 1 total time: {step1Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                // Step 2: Load graph with nodes
                Console.WriteLine($"Step 2: Creating {_NodeCount} nodes with embeddings...");
                var step2Stopwatch = Stopwatch.StartNew();
                nodes = CreateNodesWithEmbeddings(tenant.GUID, graph.GUID, _NodeCount);
                step2Stopwatch.Stop();
                PerformanceMetrics.Step2Time = step2Stopwatch.ElapsedMilliseconds;
                PerformanceMetrics.NodesCreated = nodes.Count;
                Console.WriteLine($"Created {nodes.Count} nodes in {step2Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Average: {step2Stopwatch.ElapsedMilliseconds / (double)nodes.Count:F2}ms per node");
                Console.WriteLine($"Step 2 total time: {step2Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                // Step 3: Perform vector searches
                Console.WriteLine($"Step 3: Performing {_SearchCount} cosine similarity searches (minimum score 0.1)...");
                var step3Stopwatch = Stopwatch.StartNew();
                PerformVectorSearches(tenant.GUID, graph.GUID);
                step3Stopwatch.Stop();
                PerformanceMetrics.Step3Time = step3Stopwatch.ElapsedMilliseconds;
                Console.WriteLine($"\nStep 3 total time: {step3Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                Console.WriteLine("Press ENTER to continue");
                Console.ReadLine();
                Console.WriteLine("");

                // Step 4: Cleanup
                Console.WriteLine("Step 4: Cleaning up resources...");
                Console.Write("Press any key to delete all test data...");
                Console.ReadKey();
                Console.WriteLine();

                var step4Stopwatch = Stopwatch.StartNew();
                Cleanup(tenant, graph, nodes);
                step4Stopwatch.Stop();
                PerformanceMetrics.Step4Time = step4Stopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Step 4 total time: {step4Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine("Cleanup completed successfully!");

                totalStopwatch.Stop();
                DateTime testEndTime = DateTime.Now;

                // Print comprehensive summary
                PrintComprehensiveSummary(totalStopwatch.ElapsedMilliseconds, testStartTime, testEndTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Attempt cleanup on error
                if (tenant != null && graph != null)
                {
                    Console.WriteLine("Attempting cleanup after error...");
                    try
                    {
                        var cleanupStopwatch = Stopwatch.StartNew();
                        Cleanup(tenant, graph, nodes);
                        cleanupStopwatch.Stop();
                        Console.WriteLine($"Error cleanup completed in {cleanupStopwatch.ElapsedMilliseconds}ms");
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"Cleanup failed: {cleanupEx.Message}");
                    }
                }

                totalStopwatch.Stop();
                Console.WriteLine($"Total runtime before error: {totalStopwatch.ElapsedMilliseconds}ms");
            }

            Console.WriteLine();
            Console.WriteLine("Test completed. Press any key to exit...");
            Console.ReadKey();
        }

        static void PrintComprehensiveSummary(long totalElapsedMs, DateTime startTime, DateTime endTime)
        {
            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine("                              TEST SUMMARY                                   ");
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine();

            // Test Configuration
            Console.WriteLine("TEST CONFIGURATION:");
            Console.WriteLine($"  • Node Count:           {_NodeCount:N0}");
            Console.WriteLine($"  • Vector Dimensions:    {_VectorDimensionality:N0}");
            Console.WriteLine($"  • Search Iterations:    {_SearchCount:N0}");
            Console.WriteLine();

            // Execution Timeline
            Console.WriteLine("EXECUTION TIMELINE:");
            Console.WriteLine($"  • Total Duration:       {totalElapsedMs:N0}ms ({totalElapsedMs / 1000.0:F2}s)");
            Console.WriteLine();

            // Node Operations
            Console.WriteLine("NODE OPERATIONS:");
            Console.WriteLine($"  • Nodes Created:            {PerformanceMetrics.NodesCreated,8:N0}");
            Console.WriteLine($"  • Total Creation Time:      {PerformanceMetrics.TotalNodeCreationTime,8:N0}ms");
            Console.WriteLine($"  • Avg Creation Time/Node:   {(PerformanceMetrics.TotalNodeCreationTime / (double)PerformanceMetrics.NodesCreated),8:F2}ms");
            Console.WriteLine();

            // Node Insertion Performance Degradation
            Console.WriteLine("NODE INSERTION PERFORMANCE DEGRADATION:");
            Console.WriteLine($"  • First Batch Avg Time:     {PerformanceMetrics.FirstBatchAvgTime,8:F2}ms/node");
            Console.WriteLine($"  • Last Batch Avg Time:      {PerformanceMetrics.LastBatchAvgTime,8:F2}ms/node");
            Console.WriteLine($"  • Degradation Rate:         {PerformanceMetrics.InsertionDegradationRate,8:F2}%");
            Console.WriteLine($"  • Min Insertion Time:       {PerformanceMetrics.MinInsertionTime,8:N0}ms (node #{PerformanceMetrics.MinInsertionIndex})");
            Console.WriteLine($"  • Max Insertion Time:       {PerformanceMetrics.MaxInsertionTime,8:N0}ms (node #{PerformanceMetrics.MaxInsertionIndex})");

            if (PerformanceMetrics.IndividualNodeInsertionTimes.Count > 0)
            {
                // Calculate percentiles
                var sortedTimes = PerformanceMetrics.IndividualNodeInsertionTimes.OrderBy(t => t).ToList();
                int p50Index = (int)(sortedTimes.Count * 0.50);
                int p90Index = (int)(sortedTimes.Count * 0.90);
                int p95Index = (int)(sortedTimes.Count * 0.95);
                int p99Index = (int)(sortedTimes.Count * 0.99);

                Console.WriteLine($"  • P50 (Median):             {sortedTimes[p50Index],8:N0}ms");
                Console.WriteLine($"  • P90:                      {sortedTimes[p90Index],8:N0}ms");
                Console.WriteLine($"  • P95:                      {sortedTimes[p95Index],8:N0}ms");
                Console.WriteLine($"  • P99:                      {sortedTimes[p99Index],8:N0}ms");
            }
            Console.WriteLine();

            // Vector Search Performance
            Console.WriteLine("VECTOR SEARCH PERFORMANCE:");
            if (PerformanceMetrics.IndividualSearchTimes.Count > 0)
            {
                Console.WriteLine($"  • Total Searches:           {PerformanceMetrics.IndividualSearchTimes.Count,8:N0}");
                Console.WriteLine($"  • Total Search Time:        {PerformanceMetrics.TotalSearchTime,8:N0}ms");
                Console.WriteLine($"  • Average Search Time:      {PerformanceMetrics.IndividualSearchTimes.Average(),8:F2}ms");
                Console.WriteLine($"  • Min Search Time:          {PerformanceMetrics.IndividualSearchTimes.Min(),8:N0}ms");
                Console.WriteLine($"  • Max Search Time:          {PerformanceMetrics.IndividualSearchTimes.Max(),8:N0}ms");
            }
            Console.WriteLine();

            // Per-Test Step Runtime
            Console.WriteLine("PER-TEST STEP RUNTIME:");
            Console.WriteLine($"  • Step 1 (Setup):           {PerformanceMetrics.Step1Time,8:N0}ms");
            Console.WriteLine($"  • Step 2 (Node Creation):   {PerformanceMetrics.Step2Time,8:N0}ms");
            Console.WriteLine($"  • Step 3 (Vector Search):   {PerformanceMetrics.Step3Time,8:N0}ms");
            Console.WriteLine($"  • Step 4 (Cleanup):         {PerformanceMetrics.Step4Time,8:N0}ms");
            Console.WriteLine();

            // Individual Search Test Case Times
            Console.WriteLine("SEARCH TIME PER TEST CASE:");
            foreach (var kvp in PerformanceMetrics.SearchTimePerTestCase.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  • Search Test {kvp.Key}:            {kvp.Value,8:N0}ms");
            }
            Console.WriteLine();

            Console.WriteLine("=" + new string('=', 78));
        }

        static void Logger(SeverityEnum sev, string msg)
        {
            if (!String.IsNullOrEmpty(msg))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {sev}: {msg}");
            }
        }

        static TenantMetadata CreateTenant()
        {
            return _Client.Tenant.Create(new TenantMetadata
            {
                Name = $"Vector Search Test Tenant {DateTime.Now:yyyyMMdd-HHmmss}"
            });
        }

        static Graph CreateGraph(Guid tenantGuid)
        {
            return _Client.Graph.Create(new Graph
            {
                TenantGUID = tenantGuid,
                Name = $"Vector Search Test Graph {DateTime.Now:yyyyMMdd-HHmmss}",
                Labels = new List<string> { "vector-test", "performance-test" }
            });
        }

        static List<Node> CreateNodesWithEmbeddings(Guid tenantGuid, Guid graphGuid, int count)
        {
            List<Node> nodes = new List<Node>();
            var totalNodeCreationTime = 0L;
            var totalEmbeddingTime = 0L;
            List<long> batchTimes = new List<long>();
            int batchSize = 100;
            int currentBatch = 1;

            Console.WriteLine($"Creating {count} nodes with {_VectorDimensionality}-dimensional vectors...");
            Console.WriteLine("Batch | Nodes    | Batch Time | Avg/Node | Cumulative Avg");
            Console.WriteLine("------|----------|------------|----------|---------------");

            for (int i = 0; i < count; i++)
            {
                // Generate random embeddings with timing
                var embeddingStopwatch = Stopwatch.StartNew();
                List<float> embeddings = GenerateRandomEmbeddings(_VectorDimensionality);
                embeddingStopwatch.Stop();
                totalEmbeddingTime += embeddingStopwatch.ElapsedMilliseconds;

                // Create node with embeddings
                Guid nodeGuid = Guid.NewGuid();
                Node node = new Node
                {
                    GUID = nodeGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = $"Node-{i:D5}",
                    Labels = new List<string> { "test-node", i % 2 == 0 ? "even" : "odd" },
                    Data = new NodeTestData
                    {
                        Index = i,
                        Category = $"Category-{i % 10}",
                        Value = _Random.NextDouble() * 1000,
                        Timestamp = DateTime.UtcNow
                    },
                    Vectors = new List<VectorMetadata>
                    {
                        new VectorMetadata
                        {
                            TenantGUID = tenantGuid,
                            GraphGUID = graphGuid,
                            NodeGUID = nodeGuid,
                            Model = "test-embeddings",
                            Dimensionality = _VectorDimensionality,
                            Content = $"Test content for node {i}",
                            Vectors = embeddings
                        }
                    }
                };

                var nodeStopwatch = Stopwatch.StartNew();
                node = _Client.Node.Create(node);
                nodeStopwatch.Stop();

                long insertionTime = nodeStopwatch.ElapsedMilliseconds;
                totalNodeCreationTime += insertionTime;
                batchTimes.Add(insertionTime);

                // Track individual insertion times for degradation analysis
                PerformanceMetrics.IndividualNodeInsertionTimes.Add(insertionTime);

                // Track min/max insertion times
                if (insertionTime < PerformanceMetrics.MinInsertionTime)
                {
                    PerformanceMetrics.MinInsertionTime = insertionTime;
                    PerformanceMetrics.MinInsertionIndex = i;
                }
                if (insertionTime > PerformanceMetrics.MaxInsertionTime)
                {
                    PerformanceMetrics.MaxInsertionTime = insertionTime;
                    PerformanceMetrics.MaxInsertionIndex = i;
                }

                nodes.Add(node);

                // Report batch statistics on new lines
                if ((i + 1) % batchSize == 0)
                {
                    double batchAvg = batchTimes.Average();
                    double batchTotal = batchTimes.Sum();
                    double cumulativeAvg = totalNodeCreationTime / (double)(i + 1);
                    PerformanceMetrics.BatchAverageInsertionTimes[currentBatch] = batchAvg;

                    if (currentBatch == 1)
                    {
                        PerformanceMetrics.FirstBatchAvgTime = batchAvg;
                    }

                    Console.WriteLine($"{currentBatch,5} | {i - batchSize + 2,4}-{i + 1,4} | {batchTotal,8:F0}ms | {batchAvg,7:F2}ms | {cumulativeAvg,7:F2}ms");

                    batchTimes.Clear();
                    currentBatch++;
                }
            }

            // Handle remaining nodes in the last partial batch
            if (batchTimes.Count > 0)
            {
                double lastBatchAvg = batchTimes.Average();
                double lastBatchTotal = batchTimes.Sum();
                double finalCumulativeAvg = totalNodeCreationTime / (double)count;
                PerformanceMetrics.BatchAverageInsertionTimes[currentBatch] = lastBatchAvg;
                PerformanceMetrics.LastBatchAvgTime = lastBatchAvg;

                int startNode = ((currentBatch - 1) * batchSize) + 1;
                Console.WriteLine($"{currentBatch,5} | {startNode,4}-{count,4} | {lastBatchTotal,8:F0}ms | {lastBatchAvg,7:F2}ms | {finalCumulativeAvg,7:F2}ms");
            }
            else if (currentBatch > 1)
            {
                // Use the last complete batch average
                PerformanceMetrics.LastBatchAvgTime = PerformanceMetrics.BatchAverageInsertionTimes[currentBatch - 1];
            }

            // Calculate degradation rate
            if (PerformanceMetrics.FirstBatchAvgTime > 0 && PerformanceMetrics.LastBatchAvgTime > 0)
            {
                PerformanceMetrics.InsertionDegradationRate =
                    ((PerformanceMetrics.LastBatchAvgTime - PerformanceMetrics.FirstBatchAvgTime) / PerformanceMetrics.FirstBatchAvgTime) * 100;
            }

            Console.WriteLine($"\nInsertion Summary:");
            Console.WriteLine($"  First batch avg: {PerformanceMetrics.FirstBatchAvgTime:F2}ms/node");
            Console.WriteLine($"  Last batch avg: {PerformanceMetrics.LastBatchAvgTime:F2}ms/node");
            Console.WriteLine($"  Degradation: {PerformanceMetrics.InsertionDegradationRate:+0.0;-0.0;0}%");

            // Store metrics
            PerformanceMetrics.TotalEmbeddingGenerationTime = totalEmbeddingTime;
            PerformanceMetrics.TotalNodeCreationTime = totalNodeCreationTime;

            return nodes;
        }

        static List<float> GenerateRandomEmbeddings(int dimensionality)
        {
            List<float> embeddings = new List<float>();

            for (int i = 0; i < dimensionality; i++)
            {
                // Generate random floats between -1 and 1
                embeddings.Add((float)(_Random.NextDouble() * 2 - 1));
            }

            // Normalize the vector (common practice for embeddings)
            float magnitude = (float)Math.Sqrt(embeddings.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embeddings.Count; i++)
                {
                    embeddings[i] /= magnitude;
                }
            }

            return embeddings;
        }

        static void PerformVectorSearches(Guid tenantGuid, Guid graphGuid)
        {
            var totalSearchTime = 0L;
            var searchTimes = new List<long>();
            var totalQueryGenTime = 0L;

            Console.WriteLine($"\nPerforming {_SearchCount} vector searches...");

            for (int searchNum = 1; searchNum <= _SearchCount; searchNum++)
            {
                // Generate random query embeddings
                var queryGenStopwatch = Stopwatch.StartNew();
                List<float> queryEmbeddings = GenerateRandomEmbeddings(_VectorDimensionality);
                queryGenStopwatch.Stop();
                totalQueryGenTime += queryGenStopwatch.ElapsedMilliseconds;

                // Create search request
                VectorSearchRequest searchRequest = new VectorSearchRequest
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Domain = VectorSearchDomainEnum.Node,
                    SearchType = VectorSearchTypeEnum.CosineSimilarity,
                    Embeddings = queryEmbeddings,
                    MinimumScore = 0.1f
                };

                // Perform search and measure time
                var searchStopwatch = Stopwatch.StartNew();
                List<VectorSearchResult> results = null;

                try
                {
                    // Add timeout monitoring
                    var searchTask = System.Threading.Tasks.Task.Run(() => _Client.Vector.Search(searchRequest).ToList());
                    int timeoutSeconds = 30;

                    if (searchTask.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        results = searchTask.Result;
                        searchStopwatch.Stop();
                    }
                    else
                    {
                        searchStopwatch.Stop();
                        Console.WriteLine($"  Search {searchNum}: TIMEOUT after {timeoutSeconds}s");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    searchStopwatch.Stop();
                    Console.WriteLine($"  Search {searchNum}: ERROR - {ex.Message}");
                    break;
                }

                if (results == null)
                {
                    Console.WriteLine($"  Search {searchNum}: Failed");
                    continue;
                }

                totalSearchTime += searchStopwatch.ElapsedMilliseconds;
                searchTimes.Add(searchStopwatch.ElapsedMilliseconds);
                PerformanceMetrics.SearchTimePerTestCase[searchNum] = searchStopwatch.ElapsedMilliseconds;
                PerformanceMetrics.SearchResultCounts.Add(results.Count);

                Console.WriteLine($"  Search {searchNum}: {searchStopwatch.ElapsedMilliseconds,6}ms | {results.Count,4} results");
            }

            // Store metrics
            PerformanceMetrics.IndividualSearchTimes = searchTimes;
            PerformanceMetrics.TotalSearchTime = totalSearchTime;
            PerformanceMetrics.QueryGenerationTime = totalQueryGenTime;

            if (searchTimes.Count > 0)
            {
                Console.WriteLine($"\nSearch Summary:");
                Console.WriteLine($"  Average: {searchTimes.Average():F2}ms");
                Console.WriteLine($"  Min: {searchTimes.Min()}ms, Max: {searchTimes.Max()}ms");
            }
        }

        static double CalculateStdDev(List<float> values)
        {
            if (values.Count <= 1) return 0;

            double avg = values.Average();
            double sumOfSquares = values.Sum(val => Math.Pow(val - avg, 2));
            return Math.Sqrt(sumOfSquares / (values.Count - 1));
        }

        static void Cleanup(TenantMetadata tenant, Graph graph, List<Node> nodes)
        {
            var cleanupStopwatch = Stopwatch.StartNew();

            // Delete nodes
            Console.WriteLine($"\nCleaning up {nodes.Count} nodes...");
            int deletedCount = 0;

            foreach (var node in nodes)
            {
                try
                {
                    _Client.Node.DeleteByGuid(tenant.GUID, graph.GUID, node.GUID);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    // Node might already be deleted - silent fail
                }
            }

            PerformanceMetrics.NodesDeleted = deletedCount;
            PerformanceMetrics.NodeDeletionTime = cleanupStopwatch.ElapsedMilliseconds;

            // Delete graph
            _Client.Graph.DeleteByGuid(tenant.GUID, graph.GUID, force: true);
            PerformanceMetrics.GraphDeletionTime = cleanupStopwatch.ElapsedMilliseconds - PerformanceMetrics.NodeDeletionTime;

            // Delete tenant
            _Client.Tenant.DeleteByGuid(tenant.GUID, force: true);
            PerformanceMetrics.TenantDeletionTime = cleanupStopwatch.ElapsedMilliseconds - PerformanceMetrics.NodeDeletionTime - PerformanceMetrics.GraphDeletionTime;

            cleanupStopwatch.Stop();
            PerformanceMetrics.TotalCleanupTime = cleanupStopwatch.ElapsedMilliseconds;

            Console.WriteLine($"Cleanup completed in {cleanupStopwatch.ElapsedMilliseconds}ms");
        }
    }
}