namespace Test.Enumeration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;

    /// <summary>
    /// Test program for enumeration APIs.
    /// </summary>
    class Program
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static LiteGraphClient _Client = null;
        private static List<TestResult> _TestResults = new List<TestResult>();
        private static Guid _TenantGuid = Guid.Empty;
        private static Guid _GraphGuid = Guid.Empty;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static Task Main(string[] args)
        {
            return MainAsync(args, CancellationToken.None);
        }

        static async Task MainAsync(string[] args, CancellationToken token = default)
        {
            Console.WriteLine("Test.Enumeration - LiteGraph Enumeration API Test Suite");
            Console.WriteLine("========================================================");
            Console.WriteLine("");

            await InitializeClient(token).ConfigureAwait(false);

            // Run tests for each object type
            await TestTenants(token).ConfigureAwait(false);
            await TestUsers(token).ConfigureAwait(false);
            await TestCredentials(token).ConfigureAwait(false);
            await TestGraphs(token).ConfigureAwait(false);
            await TestNodes(token).ConfigureAwait(false);
            await TestEdges(token).ConfigureAwait(false);
            await TestLabels(token).ConfigureAwait(false);
            await TestTags(token).ConfigureAwait(false);
            await TestVectors(token).ConfigureAwait(false);

            // Print summary
            PrintSummary();

            // Cleanup
            await Cleanup(token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static Task InitializeClient(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Console.WriteLine("Initializing LiteGraphClient...");

            // Delete existing database file to ensure clean state
            string dbPath = "litegraph.db";
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                Console.WriteLine($"Deleted existing database: {dbPath}");
            }

            _Client = new LiteGraphClient(new SqliteGraphRepository(dbPath, true));
            _Client.InitializeRepository();
            Console.WriteLine("Client initialized with clean database.");
            Console.WriteLine("");

            return Task.CompletedTask;
        }

        private static Task Cleanup(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Console.WriteLine("");
            Console.WriteLine("Cleaning up...");

            try
            {
                _Client.Flush();
                _Client.Dispose();

                // Delete database file
                string dbPath = "litegraph.db";
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    Console.WriteLine($"Deleted database file: {dbPath}");
                }

                Console.WriteLine("Cleanup complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup failed: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private static async Task TestTenants(CancellationToken token = default)
        {
            string testName = "Tenants";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 tenants
                List<TenantMetadata> tenants = new List<TenantMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    TenantMetadata tenant = new TenantMetadata
                    {
                        Name = $"Tenant-{i:D3}"
                    };
                    tenants.Add(await _Client.Tenant.Create(tenant, token).ConfigureAwait(false));
                }

                // Store first tenant for later tests
                _TenantGuid = tenants[0].GUID;

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<TenantMetadata>(
                    testName + "-AllAtOnce",
                    () => _Client.Tenant.Enumerate(new EnumerationRequest { MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<TenantMetadata>(
                    testName + "-WithSkip",
                    (skip) => _Client.Tenant.Enumerate(new EnumerationRequest { MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<TenantMetadata>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Tenant.Enumerate(new EnumerationRequest { MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestCredentials(CancellationToken token = default)
        {
            string testName = "Credentials";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Get existing users (created by TestUsers which runs first)
                List<UserMaster> existingUsers = new List<UserMaster>();
                await foreach (UserMaster user in _Client.User.ReadAllInTenant(_TenantGuid, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    existingUsers.Add(user);
                    if (existingUsers.Count >= 100) break;
                }
                if (existingUsers.Count < 100)
                {
                    throw new Exception($"Expected 100 users to exist, but found only {existingUsers.Count}");
                }

                // Create 100 credentials (1 per user)
                List<Credential> credentials = new List<Credential>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Credential credential = new Credential
                    {
                        TenantGUID = _TenantGuid,
                        UserGUID = existingUsers[i].GUID,
                        BearerToken = $"token-{i:D3}",
                        Active = true
                    };
                    credentials.Add(await _Client.Credential.Create(credential, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<Credential>(
                    testName + "-AllAtOnce",
                    () => _Client.Credential.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<Credential>(
                    testName + "-WithSkip",
                    (skip) => _Client.Credential.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<Credential>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Credential.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestUsers(CancellationToken token = default)
        {
            string testName = "Users";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 users
                List<UserMaster> users = new List<UserMaster>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    UserMaster user = new UserMaster
                    {
                        TenantGUID = _TenantGuid,
                        Email = $"user{i:D3}@example.com",
                        Password = "password123",
                        FirstName = $"User{i}",
                        LastName = "Test"
                    };
                    users.Add(await _Client.User.Create(user, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<UserMaster>(
                    testName + "-AllAtOnce",
                    () => _Client.User.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<UserMaster>(
                    testName + "-WithSkip",
                    (skip) => _Client.User.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<UserMaster>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.User.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestGraphs(CancellationToken token = default)
        {
            string testName = "Graphs";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 graphs
                List<Graph> graphs = new List<Graph>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Graph graph = new Graph
                    {
                        TenantGUID = _TenantGuid,
                        Name = $"Graph-{i:D3}",
                        Data = null
                    };
                    graphs.Add(await _Client.Graph.Create(graph, token).ConfigureAwait(false));
                }

                // Store first graph for later tests
                _GraphGuid = graphs[0].GUID;

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<Graph>(
                    testName + "-AllAtOnce",
                    () => _Client.Graph.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<Graph>(
                    testName + "-WithSkip",
                    (skip) => _Client.Graph.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<Graph>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Graph.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestNodes(CancellationToken token = default)
        {
            string testName = "Nodes";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 nodes
                List<Node> nodes = new List<Node>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Node node = new Node
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Name = $"Node-{i:D3}",
                        Data = null
                    };
                    nodes.Add(await _Client.Node.Create(node, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Node.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Node.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Node.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestEdges(CancellationToken token = default)
        {
            string testName = "Edges";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Get nodes to create edges between
                List<Node> nodes = new List<Node>();
                await foreach (Node node in _Client.Node.ReadAllInGraph(_TenantGuid, _GraphGuid, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    nodes.Add(node);
                }
                if (nodes.Count < 2)
                {
                    throw new Exception("Not enough nodes to create edges");
                }

                // Create 100 edges
                List<Edge> edges = new List<Edge>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Edge edge = new Edge
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        From = nodes[i % nodes.Count].GUID,
                        To = nodes[(i + 1) % nodes.Count].GUID,
                        Name = $"Edge-{i:D3}",
                        Cost = 1,
                        Data = null
                    };
                    edges.Add(await _Client.Edge.Create(edge, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<Edge>(
                    testName + "-AllAtOnce",
                    () => _Client.Edge.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<Edge>(
                    testName + "-WithSkip",
                    (skip) => _Client.Edge.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<Edge>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Edge.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestLabels(CancellationToken token = default)
        {
            string testName = "Labels";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 labels
                List<LabelMetadata> labels = new List<LabelMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    LabelMetadata label = new LabelMetadata
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Label = $"Label-{i:D3}"
                    };
                    labels.Add(await _Client.Label.Create(label, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Label.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Label.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Label.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestTags(CancellationToken token = default)
        {
            string testName = "Tags";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 tags
                List<TagMetadata> tags = new List<TagMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    TagMetadata tag = new TagMetadata
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Key = $"Key-{i:D3}",
                        Value = $"Value-{i:D3}"
                    };
                    tags.Add(await _Client.Tag.Create(tag, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<TagMetadata>(
                    testName + "-AllAtOnce",
                    () => _Client.Tag.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<TagMetadata>(
                    testName + "-WithSkip",
                    (skip) => _Client.Tag.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<TagMetadata>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Tag.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task TestVectors(CancellationToken token = default)
        {
            string testName = "Vectors";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Get a node to attach vectors to
                Node node = await _Client.Node.ReadFirst(_TenantGuid, _GraphGuid, token: token).ConfigureAwait(false);
                if (node == null)
                {
                    throw new Exception("No nodes available to attach vectors");
                }

                // Create 100 vectors
                List<VectorMetadata> vectors = new List<VectorMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata vector = new VectorMetadata
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        NodeGUID = node.GUID,
                        Model = "test-model",
                        Dimensionality = 3,
                        Vectors = new List<float> { i, i + 1, i + 2 }
                    };
                    vectors.Add(await _Client.Vector.Create(vector, token).ConfigureAwait(false));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = await TestEnumerateAllAtOnce<VectorMetadata>(
                    testName + "-AllAtOnce",
                    () => _Client.Vector.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }, token),
                    100).ConfigureAwait(false);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = await TestEnumerateWithSkip<VectorMetadata>(
                    testName + "-WithSkip",
                    (skip) => _Client.Vector.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }, token),
                    100).ConfigureAwait(false);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = await TestEnumerateWithContinuationToken<VectorMetadata>(
                    testName + "-WithContinuationToken",
                    (contToken) => _Client.Vector.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = contToken }, token),
                    100).ConfigureAwait(false);

                if (test1Pass && test2Pass && test3Pass)
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = true });
                    Console.WriteLine($"{testName}: PASS");
                }
                else
                {
                    _TestResults.Add(new TestResult { TestName = testName, Passed = false });
                    Console.WriteLine($"{testName}: FAIL");
                }
            }
            catch (Exception ex)
            {
                _TestResults.Add(new TestResult { TestName = testName, Passed = false, ErrorMessage = ex.Message });
                Console.WriteLine($"{testName}: FAIL - {ex.Message}");
            }

            Console.WriteLine("");
        }

        private static async Task<bool> TestEnumerateAllAtOnce<T>(string testName, Func<Task<EnumerationResult<T>>> enumerateFunc, int expectedCount)
        {
            try
            {
                EnumerationResult<T> result = await enumerateFunc().ConfigureAwait(false);

                // Print diagnostic information
                Console.WriteLine($"  {testName}:");
                Console.WriteLine($"    Total Records in DB: {expectedCount}");
                Console.WriteLine($"    Records Retrieved (Objects.Count): {result.Objects?.Count ?? 0}");
                Console.WriteLine($"    Continuation Token: {(result.ContinuationToken.HasValue ? result.ContinuationToken.Value.ToString() : "null")}");
                Console.WriteLine($"    MaxResults: {result.MaxResults}");
                Console.WriteLine($"    EndOfResults: {result.EndOfResults}");
                Console.WriteLine($"    TotalRecords: {result.TotalRecords}");
                Console.WriteLine($"    RecordsRemaining: {result.RecordsRemaining}");

                // Verify properties
                bool passed = true;
                List<string> errors = new List<string>();

                if (result.MaxResults != 100)
                {
                    passed = false;
                    errors.Add($"MaxResults expected 100, got {result.MaxResults}");
                }

                if (result.Objects == null || result.Objects.Count != expectedCount)
                {
                    passed = false;
                    errors.Add($"Objects.Count expected {expectedCount}, got {result.Objects?.Count ?? 0}");
                }

                if (!result.EndOfResults)
                {
                    passed = false;
                    errors.Add($"EndOfResults expected true, got {result.EndOfResults}");
                }

                if (result.TotalRecords != expectedCount)
                {
                    passed = false;
                    errors.Add($"TotalRecords expected {expectedCount}, got {result.TotalRecords}");
                }

                if (result.RecordsRemaining != 0)
                {
                    passed = false;
                    errors.Add($"RecordsRemaining expected 0, got {result.RecordsRemaining}");
                }

                if (result.ContinuationToken != null)
                {
                    passed = false;
                    errors.Add($"ContinuationToken expected null, got {result.ContinuationToken}");
                }

                if (!passed)
                {
                    Console.WriteLine($"    Status: FAIL");
                    foreach (string error in errors)
                    {
                        Console.WriteLine($"      ERROR: {error}");
                    }
                }
                else
                {
                    Console.WriteLine($"    Status: PASS");
                }

                return passed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {testName}: FAIL - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestEnumerateWithSkip<T>(string testName, Func<int, Task<EnumerationResult<T>>> enumerateFunc, int expectedCount)
        {
            try
            {
                Console.WriteLine($"  {testName}:");
                int totalRetrieved = 0;
                int pageNumber = 0;
                bool allPassed = true;
                List<string> errors = new List<string>();

                while (totalRetrieved < expectedCount)
                {
                    int skip = pageNumber * 10;
                    EnumerationResult<T> result = await enumerateFunc(skip).ConfigureAwait(false);

                    int expectedPageSize = Math.Min(10, expectedCount - totalRetrieved);
                    bool isLastPage = (totalRetrieved + expectedPageSize) >= expectedCount;

                    // Print diagnostic information
                    Console.WriteLine($"    Page {pageNumber} (Skip={skip}):");
                    Console.WriteLine($"      Total Records in DB: {expectedCount}");
                    Console.WriteLine($"      Records Retrieved (Objects.Count): {result.Objects?.Count ?? 0}");
                    Console.WriteLine($"      Continuation Token: {(result.ContinuationToken.HasValue ? result.ContinuationToken.Value.ToString() : "null")}");
                    Console.WriteLine($"      MaxResults: {result.MaxResults}");
                    Console.WriteLine($"      EndOfResults: {result.EndOfResults}");
                    Console.WriteLine($"      TotalRecords: {result.TotalRecords}");
                    Console.WriteLine($"      RecordsRemaining: {result.RecordsRemaining}");

                    // Verify properties
                    bool pagePassed = true;
                    if (result.MaxResults != 10)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: MaxResults expected 10, got {result.MaxResults}");
                    }

                    if (result.Objects == null || result.Objects.Count != expectedPageSize)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: Objects.Count expected {expectedPageSize}, got {result.Objects?.Count ?? 0}");
                    }

                    if (result.EndOfResults != isLastPage)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: EndOfResults expected {isLastPage}, got {result.EndOfResults}");
                    }

                    if (result.TotalRecords != expectedCount)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: TotalRecords expected {expectedCount}, got {result.TotalRecords}");
                    }

                    long expectedRemaining = expectedCount - totalRetrieved - expectedPageSize;
                    if (result.RecordsRemaining != expectedRemaining)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: RecordsRemaining expected {expectedRemaining}, got {result.RecordsRemaining}");
                    }

                    Console.WriteLine($"      Page Status: {(pagePassed ? "PASS" : "FAIL")}");

                    totalRetrieved += result.Objects?.Count ?? 0;
                    pageNumber++;

                    if (result.EndOfResults) break;
                }

                if (totalRetrieved != expectedCount)
                {
                    allPassed = false;
                    errors.Add($"Total retrieved {totalRetrieved} does not match expected {expectedCount}");
                }

                if (!allPassed)
                {
                    Console.WriteLine($"    Overall Status: FAIL");
                    foreach (string error in errors)
                    {
                        Console.WriteLine($"      ERROR: {error}");
                    }
                }
                else
                {
                    Console.WriteLine($"    Overall Status: PASS");
                }

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {testName}: FAIL - {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestEnumerateWithContinuationToken<T>(string testName, Func<Guid?, Task<EnumerationResult<T>>> enumerateFunc, int expectedCount)
        {
            try
            {
                Console.WriteLine($"  {testName}:");
                int totalRetrieved = 0;
                int pageNumber = 0;
                Guid? continuationToken = null;
                bool allPassed = true;
                List<string> errors = new List<string>();

                while (true)
                {
                    EnumerationResult<T> result = await enumerateFunc(continuationToken).ConfigureAwait(false);

                    int expectedPageSize = Math.Min(10, expectedCount - totalRetrieved);
                    bool isLastPage = (totalRetrieved + expectedPageSize) >= expectedCount;

                    // Print diagnostic information
                    Console.WriteLine($"    Page {pageNumber} (Token={(continuationToken.HasValue ? continuationToken.Value.ToString() : "null")}):");
                    Console.WriteLine($"      Total Records in DB: {expectedCount}");
                    Console.WriteLine($"      Records Retrieved (Objects.Count): {result.Objects?.Count ?? 0}");
                    Console.WriteLine($"      Continuation Token: {(result.ContinuationToken.HasValue ? result.ContinuationToken.Value.ToString() : "null")}");
                    Console.WriteLine($"      MaxResults: {result.MaxResults}");
                    Console.WriteLine($"      EndOfResults: {result.EndOfResults}");
                    Console.WriteLine($"      TotalRecords: {result.TotalRecords}");
                    Console.WriteLine($"      RecordsRemaining: {result.RecordsRemaining}");

                    // Verify properties
                    bool pagePassed = true;
                    if (result.MaxResults != 10)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: MaxResults expected 10, got {result.MaxResults}");
                    }

                    if (result.Objects == null || result.Objects.Count != expectedPageSize)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: Objects.Count expected {expectedPageSize}, got {result.Objects?.Count ?? 0}");
                    }

                    if (result.EndOfResults != isLastPage)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: EndOfResults expected {isLastPage}, got {result.EndOfResults}");
                    }

                    if (result.TotalRecords != expectedCount)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: TotalRecords expected {expectedCount}, got {result.TotalRecords}");
                    }

                    long expectedRemaining = expectedCount - totalRetrieved - expectedPageSize;
                    if (result.RecordsRemaining != expectedRemaining)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: RecordsRemaining expected {expectedRemaining}, got {result.RecordsRemaining}");
                    }

                    if (!isLastPage && result.ContinuationToken == null)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: ContinuationToken expected non-null for non-last page, got null");
                    }

                    if (isLastPage && result.ContinuationToken != null)
                    {
                        allPassed = false;
                        pagePassed = false;
                        errors.Add($"Page {pageNumber}: ContinuationToken expected null for last page, got {result.ContinuationToken}");
                    }

                    Console.WriteLine($"      Page Status: {(pagePassed ? "PASS" : "FAIL")}");

                    totalRetrieved += result.Objects?.Count ?? 0;
                    continuationToken = result.ContinuationToken;
                    pageNumber++;

                    if (result.EndOfResults) break;
                }

                if (totalRetrieved != expectedCount)
                {
                    allPassed = false;
                    errors.Add($"Total retrieved {totalRetrieved} does not match expected {expectedCount}");
                }

                if (!allPassed)
                {
                    Console.WriteLine($"    Overall Status: FAIL");
                    foreach (string error in errors)
                    {
                        Console.WriteLine($"      ERROR: {error}");
                    }
                }
                else
                {
                    Console.WriteLine($"    Overall Status: PASS");
                }

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {testName}: FAIL - {ex.Message}");
                return false;
            }
        }

        private static void PrintSummary()
        {
            Console.WriteLine("");
            Console.WriteLine("========================================================");
            Console.WriteLine("Test Summary");
            Console.WriteLine("========================================================");
            Console.WriteLine("");

            int passedCount = 0;
            int failedCount = 0;

            foreach (TestResult result in _TestResults)
            {
                string status = result.Passed ? "PASS" : "FAIL";
                Console.WriteLine($"{result.TestName}: {status}");

                if (!result.Passed && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"  Error: {result.ErrorMessage}");
                }

                if (result.Passed)
                {
                    passedCount++;
                }
                else
                {
                    failedCount++;
                }
            }

            Console.WriteLine("");
            Console.WriteLine($"Total Tests: {_TestResults.Count}");
            Console.WriteLine($"Passed: {passedCount}");
            Console.WriteLine($"Failed: {failedCount}");
            Console.WriteLine("");

            if (failedCount == 0)
            {
                Console.WriteLine("Overall: PASS");
            }
            else
            {
                Console.WriteLine("Overall: FAIL");
            }
        }

        #endregion
    }

    /// <summary>
    /// Test result.
    /// </summary>
    class TestResult
    {
        #region Public-Members

        /// <summary>
        /// Test name.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Indicates if the test passed.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Error message if test failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TestResult()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
