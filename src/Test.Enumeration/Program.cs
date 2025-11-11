namespace Test.Enumeration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
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
        static void Main(string[] args)
        {
            Console.WriteLine("Test.Enumeration - LiteGraph Enumeration API Test Suite");
            Console.WriteLine("========================================================");
            Console.WriteLine("");

            InitializeClient();

            // Run tests for each object type
            TestTenants();
            TestUsers();
            TestCredentials();
            TestGraphs();
            TestNodes();
            TestEdges();
            TestLabels();
            TestTags();
            TestVectors();

            // Print summary
            PrintSummary();

            // Cleanup
            Cleanup();
        }

        #endregion

        #region Private-Methods

        private static void InitializeClient()
        {
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
        }

        private static void Cleanup()
        {
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
        }

        private static void TestTenants()
        {
            string testName = "Tenants";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 tenants
                List<TenantMetadata> tenants = new List<TenantMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    TenantMetadata tenant = new TenantMetadata
                    {
                        Name = $"Tenant-{i:D3}"
                    };
                    tenants.Add(_Client.Tenant.Create(tenant));
                }

                // Store first tenant for later tests
                _TenantGuid = tenants[0].GUID;

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Tenant.Enumerate(new EnumerationRequest { MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Tenant.Enumerate(new EnumerationRequest { MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Tenant.Enumerate(new EnumerationRequest { MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestCredentials()
        {
            string testName = "Credentials";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Get existing users (created by TestUsers which runs first)
                List<UserMaster> existingUsers = _Client.User.ReadAllInTenant(_TenantGuid).Take(100).ToList();
                if (existingUsers.Count < 100)
                {
                    throw new Exception($"Expected 100 users to exist, but found only {existingUsers.Count}");
                }

                // Create 100 credentials (1 per user)
                List<Credential> credentials = new List<Credential>();
                for (int i = 0; i < 100; i++)
                {
                    Credential credential = new Credential
                    {
                        TenantGUID = _TenantGuid,
                        UserGUID = existingUsers[i].GUID,
                        BearerToken = $"token-{i:D3}",
                        Active = true
                    };
                    credentials.Add(_Client.Credential.Create(credential));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Credential.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Credential.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Credential.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestUsers()
        {
            string testName = "Users";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 users
                List<UserMaster> users = new List<UserMaster>();
                for (int i = 0; i < 100; i++)
                {
                    UserMaster user = new UserMaster
                    {
                        TenantGUID = _TenantGuid,
                        Email = $"user{i:D3}@example.com",
                        Password = "password123",
                        FirstName = $"User{i}",
                        LastName = "Test"
                    };
                    users.Add(_Client.User.Create(user));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.User.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.User.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.User.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestGraphs()
        {
            string testName = "Graphs";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 graphs
                List<Graph> graphs = new List<Graph>();
                for (int i = 0; i < 100; i++)
                {
                    Graph graph = new Graph
                    {
                        TenantGUID = _TenantGuid,
                        Name = $"Graph-{i:D3}",
                        Data = null
                    };
                    graphs.Add(_Client.Graph.Create(graph));
                }

                // Store first graph for later tests
                _GraphGuid = graphs[0].GUID;

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Graph.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Graph.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Graph.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestNodes()
        {
            string testName = "Nodes";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 nodes
                List<Node> nodes = new List<Node>();
                for (int i = 0; i < 100; i++)
                {
                    Node node = new Node
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Name = $"Node-{i:D3}",
                        Data = null
                    };
                    nodes.Add(_Client.Node.Create(node));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Node.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Node.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Node.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestEdges()
        {
            string testName = "Edges";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Get nodes to create edges between
                List<Node> nodes = _Client.Node.ReadAllInGraph(_TenantGuid, _GraphGuid).ToList();
                if (nodes.Count < 2)
                {
                    throw new Exception("Not enough nodes to create edges");
                }

                // Create 100 edges
                List<Edge> edges = new List<Edge>();
                for (int i = 0; i < 100; i++)
                {
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
                    edges.Add(_Client.Edge.Create(edge));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Edge.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Edge.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Edge.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestLabels()
        {
            string testName = "Labels";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 labels
                List<LabelMetadata> labels = new List<LabelMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    LabelMetadata label = new LabelMetadata
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Label = $"Label-{i:D3}"
                    };
                    labels.Add(_Client.Label.Create(label));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Label.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Label.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Label.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestTags()
        {
            string testName = "Tags";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Create 100 tags
                List<TagMetadata> tags = new List<TagMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    TagMetadata tag = new TagMetadata
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Key = $"Key-{i:D3}",
                        Value = $"Value-{i:D3}"
                    };
                    tags.Add(_Client.Tag.Create(tag));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Tag.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Tag.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Tag.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static void TestVectors()
        {
            string testName = "Vectors";
            Console.WriteLine($"Testing {testName} enumeration...");

            try
            {
                // Get a node to attach vectors to
                Node node = _Client.Node.ReadFirst(_TenantGuid, _GraphGuid);
                if (node == null)
                {
                    throw new Exception("No nodes available to attach vectors");
                }

                // Create 100 vectors
                List<VectorMetadata> vectors = new List<VectorMetadata>();
                for (int i = 0; i < 100; i++)
                {
                    VectorMetadata vector = new VectorMetadata
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        NodeGUID = node.GUID,
                        Model = "test-model",
                        Dimensionality = 3,
                        Vectors = new List<float> { i, i + 1, i + 2 }
                    };
                    vectors.Add(_Client.Vector.Create(vector));
                }

                // Test 1: Enumerate all at once (max results = 100)
                bool test1Pass = TestEnumerateAllAtOnce(
                    testName + "-AllAtOnce",
                    () => _Client.Vector.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 100 }),
                    100);

                // Test 2: Enumerate in pages of 10 using skip
                bool test2Pass = TestEnumerateWithSkip(
                    testName + "-WithSkip",
                    (skip) => _Client.Vector.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, Skip = skip }),
                    100);

                // Test 3: Enumerate in pages of 10 using continuation token
                bool test3Pass = TestEnumerateWithContinuationToken(
                    testName + "-WithContinuationToken",
                    (token) => _Client.Vector.Enumerate(new EnumerationRequest { TenantGUID = _TenantGuid, GraphGUID = _GraphGuid, MaxResults = 10, ContinuationToken = token }),
                    100);

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

        private static bool TestEnumerateAllAtOnce<T>(string testName, Func<EnumerationResult<T>> enumerateFunc, int expectedCount)
        {
            try
            {
                EnumerationResult<T> result = enumerateFunc();

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

        private static bool TestEnumerateWithSkip<T>(string testName, Func<int, EnumerationResult<T>> enumerateFunc, int expectedCount)
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
                    EnumerationResult<T> result = enumerateFunc(skip);

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

        private static bool TestEnumerateWithContinuationToken<T>(string testName, Func<Guid?, EnumerationResult<T>> enumerateFunc, int expectedCount)
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
                    EnumerationResult<T> result = enumerateFunc(continuationToken);

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
