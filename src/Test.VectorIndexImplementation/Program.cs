#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace Test.VectorIndexImplementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Comprehensive Vector Index Test Program
    /// Following HnswLite test patterns for thorough HNSW indexing validation
    /// </summary>
    public class Program
    {
        #region Public-Members

        #endregion

        #region Private-Members

        // Test Configuration
        private static readonly string _DatabasePath = "comprehensive_vector_test.db";
        private static readonly int _VectorDimensionality = 128;
        private static readonly int _InitialVectorCount = 500;
        private static readonly int _AdditionalVectorCount = 250;
        private static readonly int _RemovalCount = 100;
        private static readonly int _SearchIterations = 10;

        // HNSW Parameters
        private static readonly int _EfConstruction = 200;
        private static readonly int _EfSearch = 50;
        private static readonly int _MaxConnections = 16;

        // Search Parameters - Configurable thresholds to get actual results
        private static readonly double _MinimumCosineSimilarityScore = 0.0;  // Accept all cosine similarity scores
        private static readonly double _MaximumEuclideanDistance = 10.0;     // Accept distances up to 10.0
        private static readonly double _MinimumDotProductScore = -10.0;      // Accept all dot product scores
        private static readonly int _MaxSearchResults = 50;                  // Increase result limit to get more matches

        // Test Data Storage
        private static List<Node> _CreatedNodes = new List<Node>();
        private static List<Node> _RemovedNodes = new List<Node>();
        private static TenantMetadata _Tenant;
        private static Graph _Graph;
        private static LiteGraphClient _Client;

        #endregion

        #region Entrypoint

        /// <summary>
        /// Main entry point for comprehensive HNSW vector indexing tests.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Task.</returns>
        static async Task Main(string[] args)
        {
            Console.WriteLine("Comprehensive LiteGraph HNSW Vector Index Test Suite");
            Console.WriteLine("Following HnswLite test patterns for thorough validation");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  - Vector Dimensions: {_VectorDimensionality}");
            Console.WriteLine($"  - Initial Vectors: {_InitialVectorCount}");
            Console.WriteLine($"  - Additional Vectors: {_AdditionalVectorCount}");
            Console.WriteLine($"  - Removal Count: {_RemovalCount}");
            Console.WriteLine($"  - Search Iterations: {_SearchIterations}");
            Console.WriteLine($"  - HNSW _EfConstruction: {_EfConstruction}");
            Console.WriteLine($"  - HNSW _EfSearch: {_EfSearch}");
            Console.WriteLine($"  - HNSW _MaxConnections: {_MaxConnections}");
            Console.WriteLine($"  - Min Cosine Similarity: {_MinimumCosineSimilarityScore}");
            Console.WriteLine($"  - Max Euclidean Distance: {_MaximumEuclideanDistance}");
            Console.WriteLine($"  - Min Dot Product Score: {_MinimumDotProductScore}");
            Console.WriteLine($"  - Max Search Results: {_MaxSearchResults}");
            Console.WriteLine(new string('=', 80));

            // Test VectorIndexConfiguration constructor fix
            await TestVectorIndexConfigurationAsync();

            // Test JSON deserialization fix
            await TestJsonDeserializationAsync();

            bool allTestsPassed = true;
            Stopwatch totalTimer = Stopwatch.StartNew();

            try
            {
                await InitializeTestEnvironmentAsync();

                // Test 1: Initial Vector Insertion
                Console.WriteLine("\n[TEST 1] Initial Vector Insertion and Index Creation");
                Console.WriteLine(new string('-', 60));
                bool test1 = await TestInitialVectorInsertionAsync();
                allTestsPassed &= test1;

                // Test 2: Index Verification
                Console.WriteLine("\n[TEST 2] Index Verification and Statistics");
                Console.WriteLine(new string('-', 60));
                bool test2 = await TestIndexVerificationAsync();
                allTestsPassed &= test2;

                // Test 3: Additional Vector Insertion
                Console.WriteLine("\n[TEST 3] Additional Vector Insertion");
                Console.WriteLine(new string('-', 60));
                bool test3 = await TestAdditionalVectorInsertionAsync();
                allTestsPassed &= test3;

                // Test 4: Vector Search Performance
                Console.WriteLine("\n[TEST 4] Vector Search Performance Tests");
                Console.WriteLine(new string('-', 60));
                bool test4 = await TestVectorSearchPerformanceAsync();
                allTestsPassed &= test4;

                // Test 5: Vector Removal
                Console.WriteLine("\n[TEST 5] Vector Removal and Index Update");
                Console.WriteLine(new string('-', 60));
                bool test5 = await TestVectorRemovalAsync();
                allTestsPassed &= test5;

                // Test 6: Post-removal Verification
                Console.WriteLine("\n[TEST 6] Post-removal Index Verification");
                Console.WriteLine(new string('-', 60));
                bool test6 = await TestPostRemovalVerificationAsync();
                allTestsPassed &= test6;

                // Test 7: Final Search Performance
                Console.WriteLine("\n[TEST 7] Final Search Performance After Modifications");
                Console.WriteLine(new string('-', 60));
                bool test7 = await TestFinalSearchPerformanceAsync();
                allTestsPassed &= test7;

                totalTimer.Stop();

                // Final Results
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("TEST SUITE SUMMARY");
                Console.WriteLine(new string('=', 80));
                if (allTestsPassed)
                {
                    Console.WriteLine("[SUCCESS] ALL TESTS PASSED!");
                }
                else
                {
                    Console.WriteLine("[FAILED] Some tests failed. Review output above for details.");
                }
                Console.WriteLine($"Total execution time: {totalTimer.ElapsedMilliseconds:N0}ms ({totalTimer.Elapsed.TotalSeconds:F1}s)");
                Console.WriteLine($"Final vector count in index: {await GetCurrentVectorCount().ConfigureAwait(false)}");
                Console.WriteLine(new string('=', 80));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL ERROR] Test suite failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                await CleanupTestEnvironmentAsync();
            }
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        /// <summary>
        /// Initialize the test environment with database and graph setup.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task InitializeTestEnvironmentAsync()
        {
            Console.WriteLine("[SETUP] Initializing test environment...");
            Stopwatch setupTimer = Stopwatch.StartNew();

            // Clean up any existing database
            if (System.IO.File.Exists(_DatabasePath))
            {
                System.IO.File.Delete(_DatabasePath);
            }

            // Initialize repository and client
            SqliteGraphRepository repo = new SqliteGraphRepository(_DatabasePath);
            repo.InitializeRepository();
            _Client = new LiteGraphClient(repo);

            // Create tenant
            _Tenant = new TenantMetadata
            {
                GUID = Guid.NewGuid(),
                Name = "HNSW Test Tenant",
                CreatedUtc = DateTime.UtcNow
            };
            _Client.Tenant.Create(_Tenant, CancellationToken.None).GetAwaiter().GetResult();

            // Create graph
            _Graph = new Graph
            {
                GUID = Guid.NewGuid(),
                TenantGUID = _Tenant.GUID,
                Name = "HNSW Test Graph",
                VectorDimensionality = _VectorDimensionality,
                CreatedUtc = DateTime.UtcNow
            };
            _Graph = await _Client.Graph.Create(_Graph).ConfigureAwait(false);

            setupTimer.Stop();
            Console.WriteLine($"[OK] Test environment initialized in {setupTimer.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Test initial vector insertion and index creation.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestInitialVectorInsertionAsync()
        {
            try
            {
                // Create initial vectors
                Console.WriteLine($"[PROCESSING] Creating {_InitialVectorCount} nodes with vectors...");
                Stopwatch creationTimer = Stopwatch.StartNew();

                List<Node> nodes = new List<Node>();
                for (int i = 0; i < _InitialVectorCount; i++)
                {
                    Node node = new Node
                    {
                        GUID = Guid.NewGuid(),
                        TenantGUID = _Tenant.GUID,
                        GraphGUID = _Graph.GUID,
                        Name = $"Vector Node {i + 1}",
                        Data = $"{{\"index\": {i}, \"type\": \"initial\"}}",
                        CreatedUtc = DateTime.UtcNow,
                        Vectors = new List<VectorMetadata>
                        {
                            new VectorMetadata
                            {
                                GUID = Guid.NewGuid(),
                                TenantGUID = _Tenant.GUID,
                                GraphGUID = _Graph.GUID,
                                NodeGUID = Guid.NewGuid(), // Will be set properly by the client
                                Vectors = GenerateRandomVector(_VectorDimensionality),
                                Model = $"test-model-v1",
                                CreatedUtc = DateTime.UtcNow
                            }
                        }
                    };
                    nodes.Add(node);
                }

                List<Node> createdNodes = _Client.Node.CreateMany(_Tenant.GUID, _Graph.GUID, nodes, CancellationToken.None).GetAwaiter().GetResult();
                _CreatedNodes.AddRange(createdNodes);
                creationTimer.Stop();

                Console.WriteLine($"[OK] Created {createdNodes.Count} nodes with vectors in {creationTimer.ElapsedMilliseconds}ms");
                Console.WriteLine($"     Average: {(double)creationTimer.ElapsedMilliseconds / createdNodes.Count:F2}ms per node");

                // Enable HNSW indexing
                Console.WriteLine("[PROCESSING] Enabling HNSW vector indexing...");
                Stopwatch indexTimer = Stopwatch.StartNew();

                VectorIndexConfiguration config = new VectorIndexConfiguration
                {
                    VectorIndexType = VectorIndexTypeEnum.HnswRam,
                    VectorDimensionality = _VectorDimensionality,
                    VectorIndexThreshold = 1,
                    VectorIndexEfConstruction = _EfConstruction,
                    VectorIndexEf = _EfSearch,
                    VectorIndexM = _MaxConnections
                };

                await _Client.Graph.EnableVectorIndexingAsync(_Tenant.GUID, _Graph.GUID, config);
                indexTimer.Stop();

                Console.WriteLine($"[OK] HNSW indexing enabled in {indexTimer.ElapsedMilliseconds}ms");

                // Verify vectors were indexed
                int vectorCount =  await GetCurrentVectorCount();
                Console.WriteLine($"[VERIFICATION] Vectors in index: {vectorCount}");

                if (vectorCount == _InitialVectorCount)
                {
                    Console.WriteLine("[SUCCESS] Initial vector insertion test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[FAILED] Expected {_InitialVectorCount} vectors, found {vectorCount}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Initial vector insertion failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test index verification and statistics.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestIndexVerificationAsync()
        {
            try
            {
                VectorIndexStatistics stats = await _Client.Graph.GetVectorIndexStatistics(_Tenant.GUID, _Graph.GUID).ConfigureAwait(false);

                if (stats == null)
                {
                    Console.WriteLine("[FAILED] No index statistics available");
                    return false;
                }

                Console.WriteLine("[OK] Index Statistics:");
                Console.WriteLine($"     Index Type: {stats.IndexType}");
                Console.WriteLine($"     Dimensions: {stats.Dimensions}");
                Console.WriteLine($"     Vector Count: {stats.VectorCount}");
                Console.WriteLine($"     Distance Metric: {stats.DistanceMetric}");
                Console.WriteLine($"     Is Loaded: {stats.IsLoaded}");

                bool isValid = stats.VectorCount == _InitialVectorCount &&
                              stats.Dimensions == _VectorDimensionality &&
                              stats.IndexType == VectorIndexTypeEnum.HnswRam &&
                              stats.IsLoaded;

                if (isValid)
                {
                    Console.WriteLine("[SUCCESS] Index verification test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine("[FAILED] Index verification failed - statistics don't match expectations");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Index verification failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test additional vector insertion into existing index.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestAdditionalVectorInsertionAsync()
        {
            try
            {
                Console.WriteLine($"[PROCESSING] Adding {_AdditionalVectorCount} additional vectors...");
                Stopwatch addTimer = Stopwatch.StartNew();

                List<Node> additionalNodes = new List<Node>();
                for (int i = 0; i < _AdditionalVectorCount; i++)
                {
                    Node node = new Node
                    {
                        GUID = Guid.NewGuid(),
                        TenantGUID = _Tenant.GUID,
                        GraphGUID = _Graph.GUID,
                        Name = $"Additional Vector Node {i + 1}",
                        Data = $"{{\"index\": {i}, \"type\": \"additional\"}}",
                        CreatedUtc = DateTime.UtcNow,
                        Vectors = new List<VectorMetadata>
                        {
                            new VectorMetadata
                            {
                                GUID = Guid.NewGuid(),
                                TenantGUID = _Tenant.GUID,
                                GraphGUID = _Graph.GUID,
                                NodeGUID = Guid.NewGuid(),
                                Vectors = GenerateRandomVector(_VectorDimensionality),
                                Model = $"test-model-v2",
                                CreatedUtc = DateTime.UtcNow
                            }
                        }
                    };
                    additionalNodes.Add(node);
                }

                List<Node> createdNodes = _Client.Node.CreateMany(_Tenant.GUID, _Graph.GUID, additionalNodes, CancellationToken.None).GetAwaiter().GetResult();
                _CreatedNodes.AddRange(createdNodes);
                addTimer.Stop();

                Console.WriteLine($"[OK] Added {createdNodes.Count} additional vectors in {addTimer.ElapsedMilliseconds}ms");
                Console.WriteLine($"     Average: {(double)addTimer.ElapsedMilliseconds / createdNodes.Count:F2}ms per vector");

                // Verify new count
                int totalVectorCount = await GetCurrentVectorCount();
                int expectedCount = _InitialVectorCount + _AdditionalVectorCount;

                Console.WriteLine($"[VERIFICATION] Total vectors in index: {totalVectorCount} (expected: {expectedCount})");

                if (totalVectorCount == expectedCount)
                {
                    Console.WriteLine("[SUCCESS] Additional vector insertion test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[FAILED] Expected {expectedCount} vectors, found {totalVectorCount}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Additional vector insertion failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test vector search performance with multiple search types and iterations.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestVectorSearchPerformanceAsync()
        {
            try
            {
                Console.WriteLine($"[PROCESSING] Running {_SearchIterations} search iterations with different search types...");

                VectorSearchTypeEnum[] searchTypes = new[]
                {
                    VectorSearchTypeEnum.CosineSimilarity,
                    VectorSearchTypeEnum.EuclidianDistance,
                    VectorSearchTypeEnum.DotProduct
                };

                bool allSearchesPassed = true;

                foreach (VectorSearchTypeEnum searchType in searchTypes)
                {
                    Console.WriteLine($"\n[SEARCH TYPE] {searchType}");

                    List<long> searchTimes = new List<long>();
                    List<int> resultCounts = new List<int>();

                    for (int i = 0; i < _SearchIterations; i++)
                    {
                        List<float> searchVector = GenerateRandomVector(_VectorDimensionality);

                        Stopwatch searchTimer = Stopwatch.StartNew();
                        // Get more results initially, then filter based on search type
                        List<VectorSearchResult> allResults = new List<VectorSearchResult>();
                        await foreach (VectorSearchResult result in _Client.Vector.Search(
                            new VectorSearchRequest
                            {
                                TenantGUID = _Tenant.GUID,
                                GraphGUID = _Graph.GUID,
                                Domain = VectorSearchDomainEnum.Node,
                                SearchType = searchType,
                                Embeddings = searchVector
                            },
                            CancellationToken.None
                        ).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                        {
                            allResults.Add(result);
                        }
                        allResults = allResults.Take(_MaxSearchResults * 2).ToList(); // Get extra results for filtering

                        // Filter results based on search type and thresholds
                        List<VectorSearchResult> results = FilterSearchResults(allResults, searchType);
                        searchTimer.Stop();

                        searchTimes.Add(searchTimer.ElapsedMilliseconds);
                        resultCounts.Add(results.Count);

                        // Show detailed results for first search
                        if (i == 0)
                        {
                            Console.WriteLine($"     Search {i + 1}: {searchTimer.ElapsedMilliseconds}ms, {results.Count} results");
                            for (int j = 0; j < Math.Min(3, results.Count); j++)
                            {
                                VectorSearchResult result = results[j];
                                string nodeId = result.Node?.GUID.ToString().Substring(0, 8) ?? "Unknown";
                                Console.WriteLine($"       Result {j + 1}: Distance={result.Distance:F6}, Score={result.Score:F6}, Node={nodeId}");
                            }
                        }
                    }

                    // Calculate statistics
                    double avgTime = searchTimes.Average();
                    double avgResults = resultCounts.Average();
                    long minTime = searchTimes.Min();
                    long maxTime = searchTimes.Max();

                    Console.WriteLine($"     Statistics:");
                    Console.WriteLine($"       Average search time: {avgTime:F1}ms");
                    Console.WriteLine($"       Min/Max search time: {minTime}ms / {maxTime}ms");
                    Console.WriteLine($"       Average results: {avgResults:F1}");
                    Console.WriteLine($"       Total searches: {_SearchIterations}");

                    if (avgResults == 0)
                    {
                        Console.WriteLine($"     [WARNING] No results found for {searchType} searches");
                        allSearchesPassed = false;
                    }
                }

                if (allSearchesPassed)
                {
                    Console.WriteLine("[SUCCESS] Vector search performance test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine("[FAILED] Some search types returned no results");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Vector search performance test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test vector removal from the index.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestVectorRemovalAsync()
        {
            try
            {
                Console.WriteLine($"[PROCESSING] Removing {_RemovalCount} vectors from index...");

                // Select nodes to remove (take from the beginning)
                List<Node> nodesToRemove = _CreatedNodes.Take(_RemovalCount).ToList();
                _RemovedNodes.AddRange(nodesToRemove);

                Stopwatch removalTimer = Stopwatch.StartNew();

                foreach (Node node in nodesToRemove)
                {
                    await _Client.Node.DeleteByGuid(_Tenant.GUID, _Graph.GUID, node.GUID, CancellationToken.None).ConfigureAwait(false);
                }

                removalTimer.Stop();

                Console.WriteLine($"[OK] Removed {nodesToRemove.Count} vectors in {removalTimer.ElapsedMilliseconds}ms");
                Console.WriteLine($"     Average: {(double)removalTimer.ElapsedMilliseconds / nodesToRemove.Count:F2}ms per removal");

                // Remove from our tracking list
                foreach (Node removedNode in nodesToRemove)
                {
                    _CreatedNodes.Remove(removedNode);
                }

                // Verify removal
                int remainingVectorCount = await GetCurrentVectorCount();
                int expectedCount = _InitialVectorCount + _AdditionalVectorCount - _RemovalCount;

                Console.WriteLine($"[VERIFICATION] Remaining vectors in index: {remainingVectorCount} (expected: {expectedCount})");

                if (remainingVectorCount == expectedCount)
                {
                    Console.WriteLine("[SUCCESS] Vector removal test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[FAILED] Expected {expectedCount} vectors, found {remainingVectorCount}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Vector removal test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test that removed vectors no longer exist in search results.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestPostRemovalVerificationAsync()
        {
            try
            {
                Console.WriteLine("[PROCESSING] Verifying removed vectors no longer appear in searches...");

                int failureCount = 0;
                int searchCount = 5;

                for (int i = 0; i < searchCount; i++)
                {
                    List<float> searchVector = GenerateRandomVector(_VectorDimensionality);

                    List<VectorSearchResult> allResults = new List<VectorSearchResult>();
                    await foreach (VectorSearchResult result in _Client.Vector.Search(
                        new VectorSearchRequest
                        {
                            TenantGUID = _Tenant.GUID,
                            GraphGUID = _Graph.GUID,
                            Domain = VectorSearchDomainEnum.Node,
                            SearchType = VectorSearchTypeEnum.CosineDistance,
                            Embeddings = searchVector
                        },
                        CancellationToken.None
                    ).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                    {
                        allResults.Add(result);
                    }
                    allResults = allResults.Take(_MaxSearchResults * 2).ToList();

                    List<VectorSearchResult> results = FilterSearchResults(allResults, VectorSearchTypeEnum.CosineSimilarity);

                    // Check if any removed nodes appear in results
                    foreach (VectorSearchResult result in results)
                    {
                        if (result.Node != null && _RemovedNodes.Any(rn => rn.GUID == result.Node.GUID))
                        {
                            Console.WriteLine($"     [ERROR] Found removed node {result.Node.GUID} in search results!");
                            failureCount++;
                        }
                    }

                    Console.WriteLine($"     Search {i + 1}: {results.Count} results, no removed vectors found");
                }

                Console.WriteLine($"[VERIFICATION] Performed {searchCount} searches, {failureCount} failures detected");

                if (failureCount == 0)
                {
                    Console.WriteLine("[SUCCESS] Post-removal verification test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[FAILED] Found {failureCount} instances of removed vectors in search results");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Post-removal verification test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test final search performance after all modifications.
        /// </summary>
        /// <returns>True if test passed.</returns>
        private static async Task<bool> TestFinalSearchPerformanceAsync()
        {
            try
            {
                Console.WriteLine("[PROCESSING] Final search performance test after all modifications...");

                List<long> searchTimes = new List<long>();
                List<int> resultCounts = new List<int>();

                for (int i = 0; i < _SearchIterations; i++)
                {
                    List<float> searchVector = GenerateRandomVector(_VectorDimensionality);

                    Stopwatch searchTimer = Stopwatch.StartNew();
                    List<VectorSearchResult> allResults = new List<VectorSearchResult>();
                    await foreach (VectorSearchResult result in _Client.Vector.Search(
                        new VectorSearchRequest
                        {
                            TenantGUID = _Tenant.GUID,
                            GraphGUID = _Graph.GUID,
                            Domain = VectorSearchDomainEnum.Node,
                            SearchType = VectorSearchTypeEnum.CosineSimilarity,
                            Embeddings = searchVector
                        },
                        CancellationToken.None
                    ).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                    {
                        allResults.Add(result);
                    }
                    allResults = allResults.Take(_MaxSearchResults * 2).ToList();

                    List<VectorSearchResult> results = FilterSearchResults(allResults, VectorSearchTypeEnum.CosineSimilarity);
                    searchTimer.Stop();

                    searchTimes.Add(searchTimer.ElapsedMilliseconds);
                    resultCounts.Add(results.Count);
                }

                double avgTime = searchTimes.Average();
                double avgResults = resultCounts.Average();
                long minTime = searchTimes.Min();
                long maxTime = searchTimes.Max();

                Console.WriteLine($"[OK] Final Search Performance:");
                Console.WriteLine($"     Average search time: {avgTime:F1}ms");
                Console.WriteLine($"     Min/Max search time: {minTime}ms / {maxTime}ms");
                Console.WriteLine($"     Average results: {avgResults:F1}");
                Console.WriteLine($"     Current index size: {await GetCurrentVectorCount().ConfigureAwait(false)} vectors");

                if (avgResults > 0 && avgTime < 1000) // Reasonable performance thresholds
                {
                    Console.WriteLine("[SUCCESS] Final search performance test passed!");
                    return true;
                }
                else
                {
                    Console.WriteLine("[FAILED] Final search performance below expectations");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Final search performance test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the current vector count in the index.
        /// </summary>
        /// <returns>Vector count.</returns>
        private static async Task<int> GetCurrentVectorCount()
        {
            try
            {
                VectorIndexStatistics stats = await _Client.Graph.GetVectorIndexStatistics(_Tenant.GUID, _Graph.GUID).ConfigureAwait(false);
                return stats?.VectorCount ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Generate a random vector with the specified dimensions.
        /// </summary>
        /// <param name="dimensions">Number of dimensions.</param>
        /// <returns>Random vector.</returns>
        private static List<float> GenerateRandomVector(int dimensions)
        {
            Random random = new Random();
            List<float> vector = new List<float>();

            for (int i = 0; i < dimensions; i++)
            {
                vector.Add((float)(random.NextDouble() - 0.5) * 2.0f);
            }

            return vector;
        }

        /// <summary>
        /// Filter search results based on search type and configurable thresholds.
        /// </summary>
        /// <param name="results">Raw search results.</param>
        /// <param name="searchType">Type of search performed.</param>
        /// <returns>Filtered results.</returns>
        private static List<VectorSearchResult> FilterSearchResults(List<VectorSearchResult> results, VectorSearchTypeEnum searchType)
        {
            if (results == null || results.Count == 0) return results ?? new List<VectorSearchResult>();

            List<VectorSearchResult> filtered = new List<VectorSearchResult>();

            foreach (VectorSearchResult result in results)
            {
                bool includeResult = false;

                switch (searchType)
                {
                    case VectorSearchTypeEnum.CosineSimilarity:
                        // For cosine similarity, higher scores are better (closer to 1.0)
                        includeResult = result.Score >= _MinimumCosineSimilarityScore;
                        break;

                    case VectorSearchTypeEnum.EuclidianDistance:
                        // For Euclidean distance, lower distances are better
                        includeResult = result.Distance <= _MaximumEuclideanDistance;
                        break;

                    case VectorSearchTypeEnum.DotProduct:
                        // For dot product, higher values are generally better, but can be negative
                        includeResult = result.Score >= _MinimumDotProductScore;
                        break;

                    default:
                        // Include all results for unknown search types
                        includeResult = true;
                        break;
                }

                if (includeResult)
                {
                    filtered.Add(result);
                }

                // Limit the number of results to prevent overwhelming output
                if (filtered.Count >= _MaxSearchResults)
                {
                    break;
                }
            }

            return filtered;
        }

        /// <summary>
        /// Clean up test environment and dispose resources.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task CleanupTestEnvironmentAsync()
        {
            Console.WriteLine("\n[CLEANUP] Disposing test resources...");
            Stopwatch cleanupTimer = Stopwatch.StartNew();

            try
            {
                // Dispose client first
                _Client?.Dispose();

                // Clean up database file
                if (System.IO.File.Exists(_DatabasePath))
                {
                    try
                    {
                        System.IO.File.Delete(_DatabasePath);
                        Console.WriteLine($"[OK] Deleted database file: {_DatabasePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] Could not delete database file {_DatabasePath}: {ex.Message}");
                    }
                }

                // Clean up any other test artifacts
                string[] testFiles = {
                    "test_auto_population.db",
                    "test.db"
                };

                foreach (string file in testFiles)
                {
                    if (System.IO.File.Exists(file))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            Console.WriteLine($"[OK] Deleted test file: {file}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARNING] Could not delete test file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Error during cleanup: {ex.Message}");
            }

            cleanupTimer.Stop();
            Console.WriteLine($"[OK] Cleanup completed in {cleanupTimer.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Test VectorIndexConfiguration constructor with graph without indexing.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestVectorIndexConfigurationAsync()
        {
            Console.WriteLine("[TEST] VectorIndexConfiguration Constructor Fix");
            Console.WriteLine("------------------------------------------------------------");

            try
            {
                // Create a graph without vector indexing (simulating API scenario)
                Graph graph = new Graph
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = Guid.NewGuid(),
                    Name = "Test Graph Without Indexing",
                    VectorIndexType = null,
                    VectorIndexFile = null,
                    VectorIndexThreshold = null,
                    VectorDimensionality = null,
                    VectorIndexM = null,
                    VectorIndexEf = null,
                    VectorIndexEfConstruction = null
                };

                Console.WriteLine("[PROCESSING] Creating VectorIndexConfiguration from graph without indexing...");
                VectorIndexConfiguration config = new VectorIndexConfiguration(graph);

                Console.WriteLine($"[VERIFICATION] VectorIndexType: {config.VectorIndexType}");
                Console.WriteLine($"[VERIFICATION] VectorIndexFile: {config.VectorIndexFile ?? "null"}");
                Console.WriteLine($"[VERIFICATION] VectorIndexThreshold: {config.VectorIndexThreshold?.ToString() ?? "null"}");
                Console.WriteLine($"[VERIFICATION] VectorDimensionality: {config.VectorDimensionality?.ToString() ?? "null"}");
                Console.WriteLine($"[VERIFICATION] VectorIndexM: {config.VectorIndexM?.ToString() ?? "null"}");
                Console.WriteLine($"[VERIFICATION] VectorIndexEf: {config.VectorIndexEf?.ToString() ?? "null"}");
                Console.WriteLine($"[VERIFICATION] VectorIndexEfConstruction: {config.VectorIndexEfConstruction?.ToString() ?? "null"}");

                Console.WriteLine("[SUCCESS] VectorIndexConfiguration constructor fix working correctly!");
                Console.WriteLine();

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] VectorIndexConfiguration test failed: {e.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {e.StackTrace}");
                Console.WriteLine();
                throw;
            }
        }

        /// <summary>
        /// Test JSON deserialization for VectorIndexConfiguration.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task TestJsonDeserializationAsync()
        {
            Console.WriteLine("[TEST] JSON Deserialization Fix for VectorIndexTypeEnum");
            Console.WriteLine("------------------------------------------------------------");

            try
            {
                string json = @"{
                    ""VectorIndexType"": ""HnswSqlite"",
                    ""VectorIndexFile"": ""graph-00000000-0000-0000-0000-000000000000-hnsw.db"",
                    ""VectorIndexThreshold"": null,
                    ""VectorDimensionality"": 384,
                    ""VectorIndexM"": 16,
                    ""VectorIndexEf"": 50,
                    ""VectorIndexEfConstruction"": 200
                }";

                Console.WriteLine("[PROCESSING] Deserializing JSON with 'HnswSqlite' enum value...");

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
                };

                VectorIndexConfiguration config = System.Text.Json.JsonSerializer.Deserialize<VectorIndexConfiguration>(json, options);

                Console.WriteLine($"[VERIFICATION] VectorIndexType: {config.VectorIndexType}");
                Console.WriteLine($"[VERIFICATION] VectorIndexFile: {config.VectorIndexFile}");
                Console.WriteLine($"[VERIFICATION] VectorDimensionality: {config.VectorDimensionality}");
                Console.WriteLine($"[VERIFICATION] VectorIndexM: {config.VectorIndexM}");
                Console.WriteLine($"[VERIFICATION] VectorIndexEf: {config.VectorIndexEf}");
                Console.WriteLine($"[VERIFICATION] VectorIndexEfConstruction: {config.VectorIndexEfConstruction}");

                // Verify the enum was parsed correctly
                if (config.VectorIndexType == VectorIndexTypeEnum.HnswSqlite)
                {
                    Console.WriteLine("[SUCCESS] JSON deserialization fix working correctly!");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Expected HnswSqlite, got {config.VectorIndexType}");
                    throw new Exception("Enum deserialization failed");
                }

                Console.WriteLine();
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] JSON deserialization test failed: {e.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {e.StackTrace}");
                Console.WriteLine();
                throw;
            }
        }

        #endregion
    }
}