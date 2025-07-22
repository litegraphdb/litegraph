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

            Console.WriteLine("=== LiteGraph Vector Search Test ===");
            Console.WriteLine($"Creating {_NodeCount} nodes with {_VectorDimensionality}-dimensional vectors");
            Console.WriteLine();

            // Initialize client
            var initStopwatch = Stopwatch.StartNew();
            _Client = new LiteGraphClient(new SqliteGraphRepository("litegraph.db", true));
            _Client.Logging.MinimumSeverity = 0;
            _Client.Logging.Logger = null;
            _Client.InitializeRepository();
            initStopwatch.Stop();
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

                var graphStopwatch = Stopwatch.StartNew();
                graph = CreateGraph(tenant.GUID);
                graphStopwatch.Stop();

                step1Stopwatch.Stop();

                Console.WriteLine($"Created tenant: {tenant.GUID} in {tenantStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Created graph: {graph.GUID} in {graphStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Step 1 total time: {step1Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                // Step 2: Load graph with nodes
                Console.WriteLine($"Step 2: Creating {_NodeCount} nodes with embeddings...");
                var step2Stopwatch = Stopwatch.StartNew();
                nodes = CreateNodesWithEmbeddings(tenant.GUID, graph.GUID, _NodeCount);
                step2Stopwatch.Stop();
                Console.WriteLine($"Created {nodes.Count} nodes in {step2Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Average: {step2Stopwatch.ElapsedMilliseconds / (double)nodes.Count:F2}ms per node");
                Console.WriteLine($"Step 2 total time: {step2Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                // Step 3: Perform vector searches
                Console.WriteLine($"Step 3: Performing {_SearchCount} cosine similarity searches (minimum score 0.1)...");
                var step3Stopwatch = Stopwatch.StartNew();
                PerformVectorSearches(tenant.GUID, graph.GUID);
                step3Stopwatch.Stop();
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
                Console.WriteLine($"Step 4 total time: {step4Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine("Cleanup completed successfully!");

                totalStopwatch.Stop();
                Console.WriteLine();
                Console.WriteLine("=== Test Summary ===");
                Console.WriteLine($"Initialization: {initStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Step 1 (Create tenant/graph): {step1Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Step 2 (Create nodes): {step2Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Step 3 (Vector searches): {step3Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Step 4 (Cleanup): {step4Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Total runtime: {totalStopwatch.ElapsedMilliseconds}ms ({totalStopwatch.Elapsed.TotalSeconds:F2} seconds)");
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
            var batchStopwatch = new Stopwatch();
            var totalNodeCreationTime = 0L;
            var totalEmbeddingTime = 0L;

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
                totalNodeCreationTime += nodeStopwatch.ElapsedMilliseconds;

                nodes.Add(node);

                // Progress indicator with batch timing
                if ((i + 1) % 100 == 0)
                {
                    if (batchStopwatch.IsRunning)
                    {
                        batchStopwatch.Stop();
                        Console.Write($"\rProgress: {i + 1}/{count} ({(i + 1) * 100.0 / count:F1}%) - Last 100 nodes: {batchStopwatch.ElapsedMilliseconds}ms      ");
                    }
                    else
                    {
                        Console.Write($"\rProgress: {i + 1}/{count} ({(i + 1) * 100.0 / count:F1}%)");
                    }
                    batchStopwatch.Restart();
                }
            }

            Console.WriteLine($"\rProgress: {count}/{count} (100.0%)");
            Console.WriteLine($"Total embedding generation time: {totalEmbeddingTime}ms (avg: {totalEmbeddingTime / (double)count:F2}ms)");
            Console.WriteLine($"Total node creation time: {totalNodeCreationTime}ms (avg: {totalNodeCreationTime / (double)count:F2}ms)");

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

            for (int searchNum = 1; searchNum <= _SearchCount; searchNum++)
            {
                Console.WriteLine($"\n--- Search {searchNum} ---");

                // Generate random query embeddings with timing
                var queryGenStopwatch = Stopwatch.StartNew();
                List<float> queryEmbeddings = GenerateRandomEmbeddings(_VectorDimensionality);
                queryGenStopwatch.Stop();

                Console.WriteLine($"Generated random query vector in {queryGenStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"First 10 values: [{string.Join(", ", queryEmbeddings.Take(10).Select(f => f.ToString("F3")))}...]");

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
                List<VectorSearchResult> results = _Client.Vector.Search(searchRequest).ToList();
                searchStopwatch.Stop();

                totalSearchTime += searchStopwatch.ElapsedMilliseconds;
                searchTimes.Add(searchStopwatch.ElapsedMilliseconds);

                Console.WriteLine($"Search completed in {searchStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Total results returned: {results.Count}");

                // Display top results with timing
                var sortStopwatch = Stopwatch.StartNew();
                var topResults = results
                    .OrderByDescending(r => r.Score)
                    .Take(_TopResults)
                    .ToList();
                sortStopwatch.Stop();
                Console.WriteLine($"Sorting results took {sortStopwatch.ElapsedMilliseconds}ms");

                Console.WriteLine($"\nTop {_TopResults} matches by cosine similarity:");
                for (int i = 0; i < topResults.Count; i++)
                {
                    var result = topResults[i];
                    Console.WriteLine($"  {i + 1}. Node: {result.Node.Name}");
                    Console.WriteLine($"     Score: {result.Score:F6}");

                    // Display node data if available
                    if (result.Node.Data != null)
                    {
                        try
                        {
                            var nodeData = _Client.ConvertData<NodeTestData>(result.Node.Data);
                            Console.WriteLine($"     Category: {nodeData.Category}");
                            Console.WriteLine($"     Value: {nodeData.Value:F2}");
                        }
                        catch
                        {
                            // Fallback to raw display
                            Console.WriteLine($"     Data: {result.Node.Data}");
                        }
                    }
                }

                // Add some statistics
                if (results.Count > 0)
                {
                    var scores = results.Select(r => r.Score ?? 0).ToList();
                    Console.WriteLine($"\nScore statistics:");
                    Console.WriteLine($"  Min: {scores.Min():F6}");
                    Console.WriteLine($"  Max: {scores.Max():F6}");
                    Console.WriteLine($"  Avg: {scores.Average():F6}");
                    Console.WriteLine($"  Std Dev: {CalculateStdDev(scores):F6}");
                }
            }

            Console.WriteLine($"\nSearch Performance Summary:");
            Console.WriteLine($"  Total search time: {totalSearchTime}ms");
            Console.WriteLine($"  Average search time: {totalSearchTime / (double)_SearchCount:F2}ms");
            Console.WriteLine($"  Min search time: {searchTimes.Min()}ms");
            Console.WriteLine($"  Max search time: {searchTimes.Max()}ms");
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
            var nodeDeleteStopwatch = Stopwatch.StartNew();

            // Delete nodes
            Console.WriteLine($"Deleting {nodes.Count} nodes...");
            int deletedCount = 0;
            var batchStopwatch = new Stopwatch();

            foreach (var node in nodes)
            {
                try
                {
                    _Client.Node.DeleteByGuid(tenant.GUID, graph.GUID, node.GUID);
                    deletedCount++;

                    if (deletedCount % 100 == 0)
                    {
                        if (batchStopwatch.IsRunning)
                        {
                            batchStopwatch.Stop();
                            Console.Write($"\rDeleted {deletedCount}/{nodes.Count} nodes - Last 100: {batchStopwatch.ElapsedMilliseconds}ms");
                        }
                        else
                        {
                            Console.Write($"\rDeleted {deletedCount}/{nodes.Count} nodes");
                        }
                        batchStopwatch.Restart();
                    }
                }
                catch (Exception ex)
                {
                    // Node might already be deleted
                    Console.WriteLine($"\nWarning: Could not delete node {node.GUID}: {ex.Message}");
                }
            }
            nodeDeleteStopwatch.Stop();
            Console.WriteLine($"\rDeleted {deletedCount}/{nodes.Count} nodes in {nodeDeleteStopwatch.ElapsedMilliseconds}ms");

            // Delete graph
            var graphDeleteStopwatch = Stopwatch.StartNew();
            Console.WriteLine("Deleting graph...");
            _Client.Graph.DeleteByGuid(tenant.GUID, graph.GUID, force: true);
            graphDeleteStopwatch.Stop();
            Console.WriteLine($"Graph deleted in {graphDeleteStopwatch.ElapsedMilliseconds}ms");

            // Delete tenant
            var tenantDeleteStopwatch = Stopwatch.StartNew();
            Console.WriteLine("Deleting tenant...");
            _Client.Tenant.DeleteByGuid(tenant.GUID, force: true);
            tenantDeleteStopwatch.Stop();
            Console.WriteLine($"Tenant deleted in {tenantDeleteStopwatch.ElapsedMilliseconds}ms");

            cleanupStopwatch.Stop();
            Console.WriteLine($"Total cleanup time: {cleanupStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Node deletion: {nodeDeleteStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Graph deletion: {graphDeleteStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Tenant deletion: {tenantDeleteStopwatch.ElapsedMilliseconds}ms");
        }
    }
}