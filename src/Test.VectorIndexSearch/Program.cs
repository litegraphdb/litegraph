namespace Test.VectorIndexSearch
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Implementations;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;

    class Program
    {
        static LiteGraphClient _Client = null;
        static Serializer _Serializer = new Serializer();
        static Random _Random = new Random();

        // Constants for the test
        static int _NodeCount = 500;  // Smaller for quicker testing
        static int _VectorDimensionality = 384;  // 384 dimensions as requested
        static int _SearchCount = 10;
        static int _TopResults = 10;
        static int _BatchSize = 100; // Batch size for node creation
        
        // HNSW Parameters (adjust these for performance tuning)
        // For FAST INGESTION (3-5x faster): M=4-8, EfConstruction=32-64
        // For BALANCED (default): M=16, EfConstruction=200  
        // For HIGH QUALITY (slower): M=32-64, EfConstruction=400-500
        static int _HnswM = 4;  // Connections per node. Lower = faster ingestion, worse recall (range: 4-64)
        static int _HnswEf = 16;  // Search quality. Only affects search, not ingestion (range: 10-500)
        static int _HnswEfConstruction = 32;  // Build quality. Lower = much faster ingestion (range: 16-500)

        // Performance tracking for individual test runs
        class TestRunMetrics
        {
            public string TestName { get; set; }
            public VectorIndexTypeEnum IndexType { get; set; }
            public long InitializationTime { get; set; }
            public long TenantCreationTime { get; set; }
            public long GraphCreationTime { get; set; }
            public long IndexEnablingTime { get; set; }
            public long TotalNodeCreationTime { get; set; }
            public long TotalEmbeddingGenerationTime { get; set; }
            public List<long> IndividualSearchTimes { get; set; } = new List<long>();
            public Dictionary<int, long> SearchTimePerTestCase { get; set; } = new Dictionary<int, long>();
            public long TotalSearchTime { get; set; }
            public long QueryGenerationTime { get; set; }
            public long NodeDeletionTime { get; set; }
            public long GraphDeletionTime { get; set; }
            public long TenantDeletionTime { get; set; }
            public long TotalCleanupTime { get; set; }
            public int NodesCreated { get; set; }
            public int NodesDeleted { get; set; }
            public List<int> SearchResultCounts { get; set; } = new List<int>();
            public List<double> SearchScoreRanges { get; set; } = new List<double>();
            public long Step1Time { get; set; }
            public long Step2Time { get; set; }
            public long Step3Time { get; set; }
            public long Step4Time { get; set; }

            // Batch insertion performance tracking
            public List<long> BatchInsertionTimes { get; set; } = new List<long>();
            public Dictionary<int, double> BatchAverageInsertionTimes { get; set; } = new Dictionary<int, double>();
            public double FirstBatchAvgTime { get; set; }
            public double LastBatchAvgTime { get; set; }
            public double InsertionDegradationRate { get; set; }
            public long MinBatchTime { get; set; } = long.MaxValue;
            public long MaxBatchTime { get; set; } = 0;
            public int MinBatchIndex { get; set; }
            public int MaxBatchIndex { get; set; }
            
            // Index-specific metrics
            public long IndexBuildTime { get; set; }
            public long IndexedSearchAvgTime { get; set; }
            public long BruteForceComparisonTime { get; set; }
            public double SpeedupFactor { get; set; }
            public VectorIndexStatistics IndexStats { get; set; }
            public long TotalTestTime { get; set; }

            public void Reset()
            {
                IndividualSearchTimes.Clear();
                SearchTimePerTestCase.Clear();
                SearchResultCounts.Clear();
                SearchScoreRanges.Clear();
                BatchInsertionTimes.Clear();
                BatchAverageInsertionTimes.Clear();
                MinBatchTime = long.MaxValue;
                MaxBatchTime = 0;
                // Reset all timing values
                InitializationTime = TenantCreationTime = GraphCreationTime = IndexEnablingTime = 0;
                TotalNodeCreationTime = TotalEmbeddingGenerationTime = TotalSearchTime = 0;
                QueryGenerationTime = NodeDeletionTime = GraphDeletionTime = TenantDeletionTime = 0;
                TotalCleanupTime = Step1Time = Step2Time = Step3Time = Step4Time = 0;
                IndexBuildTime = IndexedSearchAvgTime = BruteForceComparisonTime = 0;
                NodesCreated = NodesDeleted = 0;
                FirstBatchAvgTime = LastBatchAvgTime = InsertionDegradationRate = 0;
                MinBatchIndex = MaxBatchIndex = 0;
                SpeedupFactor = 0;
                IndexStats = null;
                TotalTestTime = 0;
            }
        }

        // Node data class
        class NodeTestData
        {
            public int Index { get; set; }
            public string Category { get; set; }
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
        }

        static Task Main(string[] args)
        {
            return MainAsync(args, CancellationToken.None);
        }

        static async Task MainAsync(string[] args, CancellationToken token = default)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            DateTime testStartTime = DateTime.Now;

            Console.WriteLine("=== LiteGraph Vector Index Search Test (HNSW RAM vs SQLite Comparison) ===");
            Console.WriteLine($"Test started at: {testStartTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Testing both RAM and SQLite HNSW index variants");
            Console.WriteLine($"Creating {_NodeCount} nodes with {_VectorDimensionality}-dimensional vectors per test");
            Console.WriteLine($"Batch size: {_BatchSize} nodes");
            Console.WriteLine($"HNSW Parameters: M={_HnswM}, Ef={_HnswEf}, EfConstruction={_HnswEfConstruction}");
            Console.WriteLine("  (Lower M & EfConstruction = faster ingestion, lower recall quality)");
            Console.WriteLine();

            // Store results for both tests
            TestRunMetrics ramMetrics = new TestRunMetrics { TestName = "HNSW RAM", IndexType = VectorIndexTypeEnum.HnswRam };
            TestRunMetrics sqliteMetrics = new TestRunMetrics { TestName = "HNSW SQLite", IndexType = VectorIndexTypeEnum.HnswSqlite };

            try 
            {
                // Run RAM test
                Console.WriteLine("+-------------------------------------------------------------------+");
                Console.WriteLine("|                        HNSW RAM TEST                              |");
                Console.WriteLine("+-------------------------------------------------------------------+");
                Console.WriteLine();
                await RunHnswTest(ramMetrics, "litegraph_ram.db", null, token).ConfigureAwait(false);

                Console.WriteLine();
                Console.WriteLine();

                // Run SQLite test  
                Console.WriteLine("+-------------------------------------------------------------------+");
                Console.WriteLine("|                      HNSW SQLite TEST                             |");
                Console.WriteLine("+-------------------------------------------------------------------+");
                Console.WriteLine();
                await RunHnswTest(sqliteMetrics, "litegraph_sqlite.db", Path.GetFullPath("test_hnsw_index.sqlite"), token).ConfigureAwait(false);

                totalStopwatch.Stop();

                // Comparative Analysis
                Console.WriteLine();
                Console.WriteLine();
                PrintComparativeAnalysis(ramMetrics, sqliteMetrics, totalStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            // Cleanup test assets
            CleanupTestAssets();
            
            Console.WriteLine();
            Console.WriteLine("=== Test Complete ===");
            Console.WriteLine();
        }

        static async Task RunHnswTest(TestRunMetrics metrics, string dbPath, string indexFile, CancellationToken token = default)
        {
            Stopwatch testStopwatch = Stopwatch.StartNew();

            // Initialize client
            Stopwatch initStopwatch = Stopwatch.StartNew();
            _Client = new LiteGraphClient(new SqliteGraphRepository(dbPath, true));
            _Client.Logging.MinimumSeverity = 0;
            _Client.Logging.Logger = null;
            _Client.InitializeRepository();
            initStopwatch.Stop();
            metrics.InitializationTime = initStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Client initialization completed in {initStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();

            TenantMetadata tenant = null;
            Graph graph = null;
            List<Node> nodes = new List<Node>();

            try
            {
                // Step 1: Create tenant and graph with HNSW indexing
                Console.WriteLine("Step 1: Creating tenant and graph with HNSW index configuration...");
                Stopwatch step1Stopwatch = Stopwatch.StartNew();

                // Create tenant
                Stopwatch tenantStopwatch = Stopwatch.StartNew();
                tenant = await _Client.Tenant.Create(new TenantMetadata
                {
                    GUID = Guid.NewGuid(),
                    Name = $"Vector Index Test Tenant ({metrics.TestName})"
                }, token).ConfigureAwait(false);
                tenantStopwatch.Stop();
                metrics.TenantCreationTime = tenantStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"  Tenant created in {tenantStopwatch.ElapsedMilliseconds}ms");

                // Create graph with HNSW configuration
                Stopwatch graphStopwatch = Stopwatch.StartNew();
                graph = await _Client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    GUID = Guid.NewGuid(),
                    Name = $"HNSW Indexed Vector Test Graph ({metrics.TestName})",
                    VectorIndexType = metrics.IndexType,
                    VectorIndexFile = indexFile,  // null for RAM, path for SQLite
                    VectorDimensionality = _VectorDimensionality,
                    VectorIndexThreshold = 100,  // Use index when we have > 100 vectors
                    VectorIndexM = _HnswM,  // HNSW M parameter - connections per node
                    VectorIndexEf = _HnswEf,  // HNSW Ef parameter for search quality
                    VectorIndexEfConstruction = _HnswEfConstruction,  // HNSW EfConstruction - build quality vs speed
                    Data = new 
                    { 
                        TestType = $"HNSW Index Performance ({metrics.TestName})",
                        NodeCount = _NodeCount,
                        Dimensionality = _VectorDimensionality,
                        IndexType = metrics.IndexType.ToString()
                    }
                }, token).ConfigureAwait(false);
                graphStopwatch.Stop();
                metrics.GraphCreationTime = graphStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"  Graph created with {metrics.TestName} index in {graphStopwatch.ElapsedMilliseconds}ms");

                step1Stopwatch.Stop();
                metrics.Step1Time = step1Stopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Step 1 completed in {step1Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                // Step 2: Create nodes in batches
                Console.WriteLine($"Step 2: Creating {_NodeCount} nodes with vectors (batch insertion)...");
                Stopwatch step2Stopwatch = Stopwatch.StartNew();
                Stopwatch embeddingStopwatch = new Stopwatch();
                string[] categories = new[] { "Technology", "Science", "Engineering", "Mathematics", "Research", "Development", "Analysis", "Innovation" };
                int totalBatches = (_NodeCount + _BatchSize - 1) / _BatchSize;

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    token.ThrowIfCancellationRequested();
                    Stopwatch batchTotalStopwatch = Stopwatch.StartNew();
                    List<Node> batchNodes = new List<Node>();
                    int batchStart = batchIndex * _BatchSize;
                    int batchEnd = Math.Min(batchStart + _BatchSize, _NodeCount);
                    int currentBatchSize = batchEnd - batchStart;

                    // Track node preparation time
                    Stopwatch batchPrepStopwatch = Stopwatch.StartNew();
                    for (int i = batchStart; i < batchEnd; i++)
                    {
                        // Generate embeddings
                        embeddingStopwatch.Start();
                        List<float> embeddings = GenerateRandomVector(_VectorDimensionality);
                        embeddingStopwatch.Stop();

                        Guid nodeGuid = Guid.NewGuid();
                        Node node = new Node
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graph.GUID,
                            GUID = nodeGuid,
                            Name = $"Node_{i:D5}",
                            Data = new NodeTestData
                            {
                                Index = i,
                                Category = categories[i % categories.Length],
                                Value = _Random.NextDouble() * 100,
                                Timestamp = DateTime.UtcNow.AddMinutes(-_Random.Next(0, 10000))
                            },
                            Vectors = new List<VectorMetadata>
                            {
                                new VectorMetadata
                                {
                                    GUID = Guid.NewGuid(),
                                    TenantGUID = tenant.GUID,
                                    GraphGUID = graph.GUID,
                                    NodeGUID = nodeGuid,  // This is critical!
                                    Model = "test-model-384",
                                    Dimensionality = _VectorDimensionality,
                                    Content = $"Content for node {i}",
                                    Vectors = embeddings
                                }
                            }
                        };

                        batchNodes.Add(node);
                    }
                    batchPrepStopwatch.Stop();

                    // Batch insert
                    Stopwatch batchInsertStopwatch = Stopwatch.StartNew();
                    List<Node> createdNodes = await _Client.Node.CreateMany(tenant.GUID, graph.GUID, batchNodes, token).ConfigureAwait(false);
                    batchInsertStopwatch.Stop();
                    nodes.AddRange(createdNodes);
                    metrics.NodesCreated += createdNodes.Count;

                    batchTotalStopwatch.Stop();
                    long batchTime = batchTotalStopwatch.ElapsedMilliseconds;
                    long batchPrepTime = batchPrepStopwatch.ElapsedMilliseconds;
                    long batchInsertTime = batchInsertStopwatch.ElapsedMilliseconds;
                    metrics.BatchInsertionTimes.Add(batchTime);

                    // Track min/max batch times
                    if (batchTime < metrics.MinBatchTime)
                    {
                        metrics.MinBatchTime = batchTime;
                        metrics.MinBatchIndex = batchIndex;
                    }
                    if (batchTime > metrics.MaxBatchTime)
                    {
                        metrics.MaxBatchTime = batchTime;
                        metrics.MaxBatchIndex = batchIndex;
                    }

                    // Calculate average time per node for this batch
                    double avgTimePerNode = (double)batchTime / currentBatchSize;
                    metrics.BatchAverageInsertionTimes[batchIndex] = avgTimePerNode;

                    // Progress indicator
                    if ((batchIndex + 1) % 5 == 0 || batchIndex == totalBatches - 1)
                    {
                        double progress = ((batchIndex + 1) * 100.0) / totalBatches;
                        long elapsedTotal = step2Stopwatch.ElapsedMilliseconds;
                        long nodesCompleted = Math.Min((batchIndex + 1) * _BatchSize, _NodeCount);
                        double avgTimePerNodeOverall = (double)elapsedTotal / nodesCompleted;
                        Console.WriteLine($"  Progress: {progress:F1}% - Batch {batchIndex + 1}/{totalBatches}: {batchTime}ms (prep: {batchPrepTime}ms, insert: {batchInsertTime}ms) - avg: {avgTimePerNodeOverall:F2}ms/node");
                    }
                }

                metrics.TotalEmbeddingGenerationTime = embeddingStopwatch.ElapsedMilliseconds;
                step2Stopwatch.Stop();
                metrics.Step2Time = step2Stopwatch.ElapsedMilliseconds;
                metrics.TotalNodeCreationTime = step2Stopwatch.ElapsedMilliseconds;

                // Force flush to ensure data consistency
                try
                {
                    _Client.Flush();
                }
                catch (Exception)
                {
                    // Flush may not be supported by all implementations
                }

                // Calculate insertion performance metrics
                if (metrics.BatchAverageInsertionTimes.Count > 0)
                {
                    metrics.FirstBatchAvgTime = metrics.BatchAverageInsertionTimes[0];
                    metrics.LastBatchAvgTime = metrics.BatchAverageInsertionTimes[totalBatches - 1];
                    metrics.InsertionDegradationRate = 
                        ((metrics.LastBatchAvgTime - metrics.FirstBatchAvgTime) / metrics.FirstBatchAvgTime) * 100;
                }

                Console.WriteLine($"Step 2 completed in {step2Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"  Nodes created: {metrics.NodesCreated}");
                Console.WriteLine($"  Average time per node: {(double)metrics.TotalNodeCreationTime / metrics.NodesCreated:F2}ms");

                // Validate data was created successfully
                Node testNode = await _Client.Node.ReadFirst(tenant.GUID, graph.GUID, null, null, null, null, EnumerationOrderEnum.CreatedDescending, true, true, token).ConfigureAwait(false);
                if (testNode == null)
                {
                    Console.WriteLine("  WARNING: No nodes found after creation!");
                    return;
                }
                Console.WriteLine();

                // Get index statistics
                try
                {
                    SqliteGraphRepository repo = _Client.GetType().GetField("_Repo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_Client) as SqliteGraphRepository;
                    if (repo != null)
                    {
                        metrics.IndexStats = await repo.Graph.GetVectorIndexStatistics(tenant.GUID, graph.GUID, token).ConfigureAwait(false);
                        if (metrics.IndexStats != null)
                        {
                            Console.WriteLine("Index Statistics:");
                            Console.WriteLine($"  Index Type: {metrics.IndexStats.IndexType}");
                            Console.WriteLine($"  Vector Count: {metrics.IndexStats.VectorCount}");
                            Console.WriteLine($"  Dimensions: {metrics.IndexStats.Dimensions}");
                            Console.WriteLine($"  M Parameter: {metrics.IndexStats.M}");
                            Console.WriteLine($"  Ef Parameter: {metrics.IndexStats.DefaultEf}");
                            Console.WriteLine($"  EfConstruction: {metrics.IndexStats.EfConstruction}");
                            Console.WriteLine($"  Estimated Memory: {metrics.IndexStats.EstimatedMemoryBytes / (1024.0 * 1024.0):F2} MB");
                            if (metrics.IndexType == VectorIndexTypeEnum.HnswSqlite && metrics.IndexStats.IndexFileSizeBytes > 0)
                            {
                                Console.WriteLine($"  Index File Size: {metrics.IndexStats.IndexFileSizeBytes / (1024.0 * 1024.0):F2} MB");
                            }
                            Console.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not retrieve index statistics: {ex.Message}");
                }

                // Step 3: Perform vector searches
                Console.WriteLine($"Step 3: Performing {_SearchCount} vector searches using {metrics.TestName} index...");
                
                Stopwatch step3Stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < _SearchCount; i++)
                {
                    Stopwatch queryStopwatch = Stopwatch.StartNew();
                    List<float> queryVector = GenerateRandomVector(_VectorDimensionality);
                    queryStopwatch.Stop();
                    metrics.QueryGenerationTime += queryStopwatch.ElapsedMilliseconds;

                    Stopwatch searchStopwatch = Stopwatch.StartNew();
                    List<VectorSearchResult> results = new List<VectorSearchResult>();


                    // Check if HNSW indexing is enabled and use appropriate search method
                    if (graph.VectorIndexType.HasValue && graph.VectorIndexType != VectorIndexTypeEnum.None)
                    {
                        // Use HNSW index search
                        try
                        {
                            SqliteGraphRepository repo = _Client.GetType().GetField("_Repo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_Client) as SqliteGraphRepository;
                            if (repo != null)
                            {
                                List<VectorScoreResult> indexResults = await VectorMethodsIndexExtensions.SearchWithIndexAsync(
                                    repo,
                                    VectorSearchTypeEnum.CosineSimilarity,
                                    queryVector,
                                    graph,
                                    _TopResults,
                                    _HnswEf);

                                if (indexResults != null && indexResults.Count > 0)
                                {
                                    // Convert HNSW results to VectorSearchResult format
                                    foreach (VectorScoreResult indexResult in indexResults)
                                    {
                                        // In HNSW indexing, the vector ID corresponds to the node GUID
                                        Node node = await _Client.Node.ReadByGuid(tenant.GUID, graph.GUID, indexResult.Id, includeData: true, includeSubordinates: true, token).ConfigureAwait(false);
                                        
                                        results.Add(new VectorSearchResult
                                        {
                                            Score = indexResult.Score,
                                            Distance = 1.0f - indexResult.Score, // Convert similarity to distance
                                            Graph = graph,
                                            Node = node
                                        });
                                    }
                                }
                                else
                                {
                                    // Fallback to brute-force search
                                    VectorSearchRequest searchRequest = new VectorSearchRequest
                                    {
                                        TenantGUID = tenant.GUID,
                                        GraphGUID = graph.GUID,
                                        Domain = VectorSearchDomainEnum.Node,
                                        SearchType = VectorSearchTypeEnum.CosineSimilarity,
                                        Embeddings = queryVector,
                                        MinimumScore = 0.0f,
                                        TopK = _TopResults
                                    };
                                    results = new List<VectorSearchResult>();
                                    await foreach (VectorSearchResult result in _Client.Vector.Search(searchRequest, token).WithCancellation(token).ConfigureAwait(false))
                                    {
                                        results.Add(result);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Fallback to brute-force search
                            VectorSearchRequest searchRequest = new VectorSearchRequest
                            {
                                TenantGUID = tenant.GUID,
                                GraphGUID = graph.GUID,
                                Domain = VectorSearchDomainEnum.Node,
                                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                                Embeddings = queryVector,
                                MinimumScore = 0.0f,
                                TopK = _TopResults
                            };
                            results = new List<VectorSearchResult>();
                            await foreach (VectorSearchResult result in _Client.Vector.Search(searchRequest, token).WithCancellation(token).ConfigureAwait(false))
                            {
                                results.Add(result);
                            }
                        }
                    }
                    else
                    {
                        // Use brute-force search for non-indexed graphs
                        VectorSearchRequest searchRequest = new VectorSearchRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graph.GUID,
                            Domain = VectorSearchDomainEnum.Node,
                            SearchType = VectorSearchTypeEnum.CosineSimilarity,
                            Embeddings = queryVector,
                            MinimumScore = 0.0f,
                            TopK = _TopResults
                        };
                        results = new List<VectorSearchResult>();
                        await foreach (VectorSearchResult result in _Client.Vector.Search(searchRequest, token).WithCancellation(token).ConfigureAwait(false))
                        {
                            results.Add(result);
                        }
                    }

                    searchStopwatch.Stop();

                    long searchTime = searchStopwatch.ElapsedMilliseconds;
                    metrics.IndividualSearchTimes.Add(searchTime);
                    metrics.SearchTimePerTestCase[i] = searchTime;
                    metrics.SearchResultCounts.Add(results.Count);

                    if (results.Count > 0)
                    {
                        List<float> scores = results.Where(r => r.Score.HasValue).Select(r => r.Score.Value).ToList();
                        if (scores.Count > 0)
                        {
                            double scoreRange = scores.Max() - scores.Min();
                            metrics.SearchScoreRanges.Add(scoreRange);
                        }
                    }

                    Console.WriteLine($"  Search {i + 1}: Found {results.Count} results in {searchTime}ms");
                    if (results.Count > 0 && results[0].Score.HasValue)
                    {
                        Console.WriteLine($"    Top result score: {results[0].Score.Value:F4}");
                    }
                }

                step3Stopwatch.Stop();
                metrics.TotalSearchTime = step3Stopwatch.ElapsedMilliseconds;
                metrics.Step3Time = step3Stopwatch.ElapsedMilliseconds;

                if (metrics.IndividualSearchTimes.Count > 0)
                {
                    metrics.IndexedSearchAvgTime = (long)metrics.IndividualSearchTimes.Average();
                }

                Console.WriteLine($"Step 3 completed in {step3Stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"  Average search time: {metrics.IndexedSearchAvgTime}ms");
                Console.WriteLine();

                // Step 4: Cleanup
                Console.WriteLine("Step 4: Cleaning up test data...");
                Stopwatch step4Stopwatch = Stopwatch.StartNew();

                // Delete nodes
                Stopwatch nodeDeletionStopwatch = Stopwatch.StartNew();
                foreach (Node node in nodes)
                {
                    token.ThrowIfCancellationRequested();
                    await _Client.Node.DeleteByGuid(tenant.GUID, graph.GUID, node.GUID, token).ConfigureAwait(false);
                    metrics.NodesDeleted++;
                }
                nodeDeletionStopwatch.Stop();
                metrics.NodeDeletionTime = nodeDeletionStopwatch.ElapsedMilliseconds;

                // Delete graph
                Stopwatch graphDeletionStopwatch = Stopwatch.StartNew();
                await _Client.Graph.DeleteByGuid(tenant.GUID, graph.GUID, true, token).ConfigureAwait(false);
                graphDeletionStopwatch.Stop();
                metrics.GraphDeletionTime = graphDeletionStopwatch.ElapsedMilliseconds;

                // Delete tenant
                Stopwatch tenantDeletionStopwatch = Stopwatch.StartNew();
                await _Client.Tenant.DeleteByGuid(tenant.GUID, true, token).ConfigureAwait(false);
                tenantDeletionStopwatch.Stop();
                metrics.TenantDeletionTime = tenantDeletionStopwatch.ElapsedMilliseconds;

                step4Stopwatch.Stop();
                metrics.TotalCleanupTime = step4Stopwatch.ElapsedMilliseconds;
                metrics.Step4Time = step4Stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"Step 4 completed in {step4Stopwatch.ElapsedMilliseconds}ms");
                
                testStopwatch.Stop();
                metrics.TotalTestTime = testStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                testStopwatch.Stop();
                metrics.TotalTestTime = testStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static void PrintComparativeAnalysis(TestRunMetrics ramMetrics, TestRunMetrics sqliteMetrics, long totalTime)
        {
            Console.WriteLine("===================================================================");
            Console.WriteLine("                    COMPARATIVE ANALYSIS                        ");
            Console.WriteLine("===================================================================");
            Console.WriteLine();

            // Test Configuration Summary
            Console.WriteLine("TEST CONFIGURATION:");
            Console.WriteLine($"  • Node Count:           {_NodeCount:N0}");
            Console.WriteLine($"  • Vector Dimensions:    {_VectorDimensionality:N0}");
            Console.WriteLine($"  • Search Iterations:    {_SearchCount:N0}");
            Console.WriteLine($"  • Batch Size:           {_BatchSize:N0}");
            Console.WriteLine($"  • HNSW Parameters:      M={_HnswM}, Ef={_HnswEf}, EfConstruction={_HnswEfConstruction}");
            Console.WriteLine($"  • Total Test Duration:  {totalTime:N0}ms ({totalTime / 1000.0:F2}s)");
            Console.WriteLine();

            // Performance Comparison Table
            Console.WriteLine("PERFORMANCE COMPARISON:");
            Console.WriteLine("+-----------------------------+-------------+-------------+-------------+");
            Console.WriteLine("| Metric                      | HNSW RAM    | HNSW SQLite | Difference  |");
            Console.WriteLine("+-----------------------------+-------------+-------------+-------------+");

            PrintComparisonRow("Total Test Time", $"{ramMetrics.TotalTestTime}ms", $"{sqliteMetrics.TotalTestTime}ms", 
                CalculatePercentageDifference(ramMetrics.TotalTestTime, sqliteMetrics.TotalTestTime));

            PrintComparisonRow("Node Creation Time", $"{ramMetrics.TotalNodeCreationTime}ms", $"{sqliteMetrics.TotalNodeCreationTime}ms", 
                CalculatePercentageDifference(ramMetrics.TotalNodeCreationTime, sqliteMetrics.TotalNodeCreationTime));

            PrintComparisonRow("Avg Search Time", $"{ramMetrics.IndexedSearchAvgTime}ms", $"{sqliteMetrics.IndexedSearchAvgTime}ms", 
                CalculatePercentageDifference(ramMetrics.IndexedSearchAvgTime, sqliteMetrics.IndexedSearchAvgTime));

            PrintComparisonRow("First Batch Avg", $"{ramMetrics.FirstBatchAvgTime:F2}ms", $"{sqliteMetrics.FirstBatchAvgTime:F2}ms", 
                CalculatePercentageDifference(ramMetrics.FirstBatchAvgTime, sqliteMetrics.FirstBatchAvgTime));

            PrintComparisonRow("Last Batch Avg", $"{ramMetrics.LastBatchAvgTime:F2}ms", $"{sqliteMetrics.LastBatchAvgTime:F2}ms", 
                CalculatePercentageDifference(ramMetrics.LastBatchAvgTime, sqliteMetrics.LastBatchAvgTime));

            if (ramMetrics.IndexStats != null && sqliteMetrics.IndexStats != null)
            {
                PrintComparisonRow("Est. Memory Usage", 
                    $"{ramMetrics.IndexStats.EstimatedMemoryBytes / (1024.0 * 1024.0):F2}MB", 
                    $"{sqliteMetrics.IndexStats.EstimatedMemoryBytes / (1024.0 * 1024.0):F2}MB", 
                    CalculatePercentageDifference(ramMetrics.IndexStats.EstimatedMemoryBytes, sqliteMetrics.IndexStats.EstimatedMemoryBytes));
                
                if (sqliteMetrics.IndexStats.IndexFileSizeBytes > 0)
                {
                    PrintComparisonRow("Index File Size", "N/A (RAM)", 
                        $"{sqliteMetrics.IndexStats.IndexFileSizeBytes / (1024.0 * 1024.0):F2}MB", "N/A");
                }
            }

            Console.WriteLine("+-----------------------------+-------------+-------------+-------------+");
            Console.WriteLine();

            // Detailed Analysis
            Console.WriteLine("DETAILED ANALYSIS:");
            Console.WriteLine();

            Console.WriteLine(">> PERFORMANCE INSIGHTS:");
            
            if (ramMetrics.TotalNodeCreationTime < sqliteMetrics.TotalNodeCreationTime)
            {
                double speedup = (double)sqliteMetrics.TotalNodeCreationTime / ramMetrics.TotalNodeCreationTime;
                Console.WriteLine($"  • HNSW RAM ingestion is {speedup:F2}x faster than SQLite");
            }
            else
            {
                double speedup = (double)ramMetrics.TotalNodeCreationTime / sqliteMetrics.TotalNodeCreationTime;
                Console.WriteLine($"  • HNSW SQLite ingestion is {speedup:F2}x faster than RAM");
            }

            if (ramMetrics.IndexedSearchAvgTime < sqliteMetrics.IndexedSearchAvgTime)
            {
                double speedup = (double)sqliteMetrics.IndexedSearchAvgTime / ramMetrics.IndexedSearchAvgTime;
                Console.WriteLine($"  • HNSW RAM search is {speedup:F2}x faster than SQLite");
            }
            else
            {
                double speedup = (double)ramMetrics.IndexedSearchAvgTime / sqliteMetrics.IndexedSearchAvgTime;
                Console.WriteLine($"  • HNSW SQLite search is {speedup:F2}x faster than RAM");
            }

            Console.WriteLine();
            Console.WriteLine(">> PERFORMANCE ANALYSIS:");
            
            // Analyze ingestion performance
            if (ramMetrics.TotalNodeCreationTime < sqliteMetrics.TotalNodeCreationTime)
            {
                double ingestSpeedup = (double)sqliteMetrics.TotalNodeCreationTime / ramMetrics.TotalNodeCreationTime;
                Console.WriteLine($"  • RAM shows {ingestSpeedup:F2}x faster ingestion ({ramMetrics.TotalNodeCreationTime:N0}ms vs {sqliteMetrics.TotalNodeCreationTime:N0}ms)");
            }
            else
            {
                double ingestSpeedup = (double)ramMetrics.TotalNodeCreationTime / sqliteMetrics.TotalNodeCreationTime;
                Console.WriteLine($"  • SQLite shows {ingestSpeedup:F2}x faster ingestion ({sqliteMetrics.TotalNodeCreationTime:N0}ms vs {ramMetrics.TotalNodeCreationTime:N0}ms)");
            }

            // Analyze search performance
            if (ramMetrics.IndexedSearchAvgTime < sqliteMetrics.IndexedSearchAvgTime)
            {
                double searchSpeedup = (double)sqliteMetrics.IndexedSearchAvgTime / ramMetrics.IndexedSearchAvgTime;
                Console.WriteLine($"  • RAM shows {searchSpeedup:F2}x faster search ({ramMetrics.IndexedSearchAvgTime:N0}ms vs {sqliteMetrics.IndexedSearchAvgTime:N0}ms)");
            }
            else
            {
                double searchSpeedup = (double)ramMetrics.IndexedSearchAvgTime / sqliteMetrics.IndexedSearchAvgTime;
                Console.WriteLine($"  • SQLite shows {searchSpeedup:F2}x faster search ({sqliteMetrics.IndexedSearchAvgTime:N0}ms vs {ramMetrics.IndexedSearchAvgTime:N0}ms)");
            }

            // Storage analysis
            Console.WriteLine();
            Console.WriteLine(">> STORAGE ANALYSIS:");
            Console.WriteLine($"  • RAM: {ramMetrics.IndexStats?.EstimatedMemoryBytes / (1024.0 * 1024.0):F2}MB memory usage, no persistence");
            Console.WriteLine($"  • SQLite: {sqliteMetrics.IndexStats?.EstimatedMemoryBytes / (1024.0 * 1024.0):F2}MB memory usage, persistent storage");
            
            if (sqliteMetrics.IndexStats?.IndexFileSizeBytes > 0)
            {
                Console.WriteLine($"  • SQLite index file: {sqliteMetrics.IndexStats.IndexFileSizeBytes / (1024.0 * 1024.0):F2}MB on disk");
            }
            else
            {
                Console.WriteLine($"  • SQLite index file size: Not available (may be integrated with database)");
            }

            Console.WriteLine();
            Console.WriteLine(">> DATA-DRIVEN RECOMMENDATIONS:");
            
            // Base recommendations on actual performance data
            if (Math.Abs(ramMetrics.IndexedSearchAvgTime - sqliteMetrics.IndexedSearchAvgTime) < 50) // Within 50ms
            {
                Console.WriteLine($"  • Search performance is comparable (difference: {Math.Abs(ramMetrics.IndexedSearchAvgTime - sqliteMetrics.IndexedSearchAvgTime):F0}ms)");
                Console.WriteLine($"  • Choose based on persistence needs: SQLite for durability, RAM for volatility");
            }
            else if (ramMetrics.IndexedSearchAvgTime < sqliteMetrics.IndexedSearchAvgTime)
            {
                Console.WriteLine($"  • RAM variant shows better search performance - choose for latency-critical applications");
            }
            else
            {
                Console.WriteLine($"  • SQLite variant shows better search performance with added persistence benefits");
            }
            
            Console.WriteLine($"  • Current HNSW parameters: M={_HnswM}, EfConstruction={_HnswEfConstruction}, Ef={_HnswEf}");
            
            Console.WriteLine();

            // Individual Test Summaries
            Console.WriteLine("INDIVIDUAL TEST SUMMARIES:");
            Console.WriteLine();
            PrintTestSummary(ramMetrics);
            Console.WriteLine();
            PrintTestSummary(sqliteMetrics);
        }

        static void PrintComparisonRow(string metric, string ramValue, string sqliteValue, string difference)
        {
            Console.WriteLine($"| {metric,-27} | {ramValue,11} | {sqliteValue,11} | {difference,11} |");
        }

        static string CalculatePercentageDifference(double value1, double value2)
        {
            if (value1 == 0 && value2 == 0) return "0%";
            if (value1 == 0) return "+∞%";
            
            double diff = ((value2 - value1) / value1) * 100;
            string prefix = diff > 0 ? "+" : "";
            return $"{prefix}{diff:F1}%";
        }

        static void PrintTestSummary(TestRunMetrics metrics)
        {
            Console.WriteLine($"{metrics.TestName} Summary:");
            Console.WriteLine($"  • Total Duration:       {metrics.TotalTestTime:N0}ms ({metrics.TotalTestTime / 1000.0:F2}s)");
            Console.WriteLine($"  • Nodes Created:        {metrics.NodesCreated:N0}");
            Console.WriteLine($"  • Creation Throughput:  {metrics.NodesCreated / (metrics.TotalNodeCreationTime / 1000.0):F1} nodes/sec");
            Console.WriteLine($"  • Avg Search Time:      {metrics.IndexedSearchAvgTime:N0}ms");
            if (metrics.IndividualSearchTimes.Count > 0)
            {
                Console.WriteLine($"  • Search Range:         {metrics.IndividualSearchTimes.Min()}ms - {metrics.IndividualSearchTimes.Max()}ms");
            }
            Console.WriteLine($"  • Batch Degradation:    {metrics.InsertionDegradationRate:F1}%");
        }

        static List<float> GenerateRandomVector(int dimensions)
        {
            List<float> vector = new List<float>(dimensions);
            for (int i = 0; i < dimensions; i++)
            {
                // Generate random values between -1 and 1
                vector.Add((float)(_Random.NextDouble() * 2 - 1));
            }
            
            // Normalize the vector to unit length for better cosine similarity
            float magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < vector.Count; i++)
                {
                    vector[i] /= magnitude;
                }
            }
            
            return vector;
        }
        
        static void CleanupTestAssets()
        {
            Console.WriteLine("\n[CLEANUP] Cleaning up test assets...");
            
            try
            {
                // Dispose the client if it exists
                _Client?.Dispose();
                
                // Clean up all database and index files created during tests
                string[] testFiles = {
                    "litegraph_ram.db",
                    "litegraph_ram.db-shm",
                    "litegraph_ram.db-wal",
                    "litegraph_sqlite.db", 
                    "litegraph_sqlite.db-shm",
                    "litegraph_sqlite.db-wal",
                    "test_hnsw_index.sqlite",
                    "test_hnsw_index.sqlite-shm",
                    "test_hnsw_index.sqlite-wal"
                };
                
                int filesDeleted = 0;
                foreach (string file in testFiles)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                            Console.WriteLine($"[OK] Deleted {file}");
                            filesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARNING] Could not delete {file}: {ex.Message}");
                        }
                    }
                }
                
                if (filesDeleted == 0)
                {
                    Console.WriteLine("[OK] No test files found to clean up");
                }
                else
                {
                    Console.WriteLine($"[OK] Cleaned up {filesDeleted} test files");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Error during cleanup: {ex.Message}");
            }
            
            Console.WriteLine("[OK] Cleanup completed");
        }
    }
}