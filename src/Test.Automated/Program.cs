namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;

    /// <summary>
    /// Automated test program for LiteGraph.
    /// </summary>
    class Program
    {
        #region Private-Members

        private static LiteGraphClient? _Client = null;
        private static Guid _TenantGuid = Guid.Empty;
        private static Guid _UserGuid = Guid.Empty;
        private static Guid _CredentialGuid = Guid.Empty;
        private static Guid _GraphGuid = Guid.Empty;
        private static Guid _Node1Guid = Guid.Empty;
        private static Guid _Node2Guid = Guid.Empty;
        private static Guid _Node3Guid = Guid.Empty;
        private static Guid _EdgeGuid = Guid.Empty;
        private static Guid _LabelGuid = Guid.Empty;
        private static Guid _TagGuid = Guid.Empty;
        private static Guid _VectorGuid = Guid.Empty;
        private static List<TestResult> _TestResults = new List<TestResult>();

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Task.</returns>
        static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("==============================================");
                Console.WriteLine("  LiteGraph Automated Test Suite");
                Console.WriteLine("==============================================");
                Console.WriteLine("");

                // Run tests with in-memory database
                Console.WriteLine("Testing with In-Memory Database");
                Console.WriteLine("==============================================");
                Console.WriteLine("");
                _Client = new LiteGraphClient(new SqliteGraphRepository("test-automated-memory.db", true));
                _Client.InitializeRepository();
                await RunAllTests().ConfigureAwait(false);
                Cleanup();

                // Store in-memory results
                List<TestResult> inMemoryResults = new List<TestResult>(_TestResults);
                _TestResults.Clear();
                ResetTestState();

                // Run tests with on-disk database
                Console.WriteLine("");
                Console.WriteLine("Testing with On-Disk Database");
                Console.WriteLine("==============================================");
                Console.WriteLine("");
                _Client = new LiteGraphClient(new SqliteGraphRepository("test-automated-disk.db", false));
                _Client.InitializeRepository();
                await RunAllTests().ConfigureAwait(false);
                Cleanup();

                // Store on-disk results
                List<TestResult> onDiskResults = new List<TestResult>(_TestResults);

                // Print combined summary
                PrintCombinedSummary(inMemoryResults, onDiskResults);

                // Return exit code based on test results
                int failureCount = inMemoryResults.Count(r => !r.Passed) + onDiskResults.Count(r => !r.Passed);
                return failureCount > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("FATAL ERROR: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        #endregion

        #region Private-Methods

        private static async Task RunAllTests()
        {
            Console.WriteLine("Running tests...");
            Console.WriteLine("");

            // Tenant Tests
            await RunTest("Tenant.Create", TestTenantCreate).ConfigureAwait(false);
            await RunTest("Tenant.ReadByGuid", TestTenantReadByGuid).ConfigureAwait(false);
            await RunTest("Tenant.ExistsByGuid", TestTenantExistsByGuid).ConfigureAwait(false);
            await RunTest("Tenant.Update", TestTenantUpdate).ConfigureAwait(false);
            await RunTest("Tenant.ReadMany", TestTenantReadMany).ConfigureAwait(false);
            await RunTest("Tenant.ReadByGuids", TestTenantReadByGuids).ConfigureAwait(false);
            await RunTest("Tenant.Enumerate", TestTenantEnumerate).ConfigureAwait(false);
            await RunTest("Tenant.GetStatistics", TestTenantGetStatistics).ConfigureAwait(false);

            // User Tests
            await RunTest("User.Create", TestUserCreate).ConfigureAwait(false);
            await RunTest("User.ReadByGuid", TestUserReadByGuid).ConfigureAwait(false);
            await RunTest("User.ReadByEmail", TestUserReadByEmail).ConfigureAwait(false);
            await RunTest("User.ExistsByGuid", TestUserExistsByGuid).ConfigureAwait(false);
            await RunTest("User.ExistsByEmail", TestUserExistsByEmail).ConfigureAwait(false);
            await RunTest("User.Update", TestUserUpdate).ConfigureAwait(false);
            await RunTest("User.ReadAllInTenant", TestUserReadAllInTenant).ConfigureAwait(false);
            await RunTest("User.ReadMany", TestUserReadMany).ConfigureAwait(false);
            await RunTest("User.ReadByGuids", TestUserReadByGuids).ConfigureAwait(false);
            await RunTest("User.Enumerate", TestUserEnumerate).ConfigureAwait(false);

            // Credential Tests
            await RunTest("Credential.Create", TestCredentialCreate).ConfigureAwait(false);
            await RunTest("Credential.ReadByGuid", TestCredentialReadByGuid).ConfigureAwait(false);
            await RunTest("Credential.ReadByBearerToken", TestCredentialReadByBearerToken).ConfigureAwait(false);
            await RunTest("Credential.ExistsByGuid", TestCredentialExistsByGuid).ConfigureAwait(false);
            await RunTest("Credential.Update", TestCredentialUpdate).ConfigureAwait(false);
            await RunTest("Credential.ReadAllInTenant", TestCredentialReadAllInTenant).ConfigureAwait(false);
            await RunTest("Credential.ReadMany", TestCredentialReadMany).ConfigureAwait(false);
            await RunTest("Credential.ReadByGuids", TestCredentialReadByGuids).ConfigureAwait(false);
            await RunTest("Credential.Enumerate", TestCredentialEnumerate).ConfigureAwait(false);

            // Graph Tests
            await RunTest("Graph.Create", TestGraphCreate).ConfigureAwait(false);
            await RunTest("Graph.ReadByGuid", TestGraphReadByGuid).ConfigureAwait(false);
            await RunTest("Graph.ExistsByGuid", TestGraphExistsByGuid).ConfigureAwait(false);
            await RunTest("Graph.Update", TestGraphUpdate).ConfigureAwait(false);
            await RunTest("Graph.ReadAllInTenant", TestGraphReadAllInTenant).ConfigureAwait(false);
            await RunTest("Graph.ReadMany", TestGraphReadMany).ConfigureAwait(false);
            await RunTest("Graph.ReadFirst", TestGraphReadFirst).ConfigureAwait(false);
            await RunTest("Graph.ReadByGuids", TestGraphReadByGuids).ConfigureAwait(false);
            await RunTest("Graph.Enumerate", TestGraphEnumerate).ConfigureAwait(false);
            await RunTest("Graph.GetStatistics", TestGraphGetStatistics).ConfigureAwait(false);

            // Node Tests
            await RunTest("Node.Create", TestNodeCreate).ConfigureAwait(false);
            await RunTest("Node.CreateMany", TestNodeCreateMany).ConfigureAwait(false);
            await RunTest("Node.ReadByGuid", TestNodeReadByGuid).ConfigureAwait(false);
            await RunTest("Node.ExistsByGuid", TestNodeExistsByGuid).ConfigureAwait(false);
            await RunTest("Node.Update", TestNodeUpdate).ConfigureAwait(false);
            await RunTest("Node.ReadAllInTenant", TestNodeReadAllInTenant).ConfigureAwait(false);
            await RunTest("Node.ReadAllInGraph", TestNodeReadAllInGraph).ConfigureAwait(false);
            await RunTest("Node.ReadMany", TestNodeReadMany).ConfigureAwait(false);
            await RunTest("Node.ReadFirst", TestNodeReadFirst).ConfigureAwait(false);
            await RunTest("Node.ReadByGuids", TestNodeReadByGuids).ConfigureAwait(false);
            await RunTest("Node.Enumerate", TestNodeEnumerate).ConfigureAwait(false);
            await RunTest("Node.ReadMostConnected", TestNodeReadMostConnected).ConfigureAwait(false);
            await RunTest("Node.ReadLeastConnected", TestNodeReadLeastConnected).ConfigureAwait(false);

            // Edge Tests
            await RunTest("Edge.Create", TestEdgeCreate).ConfigureAwait(false);
            await RunTest("Edge.CreateMany", TestEdgeCreateMany).ConfigureAwait(false);
            await RunTest("Edge.ReadByGuid", TestEdgeReadByGuid).ConfigureAwait(false);
            await RunTest("Edge.ExistsByGuid", TestEdgeExistsByGuid).ConfigureAwait(false);
            await RunTest("Edge.Update", TestEdgeUpdate).ConfigureAwait(false);
            await RunTest("Edge.ReadAllInTenant", TestEdgeReadAllInTenant).ConfigureAwait(false);
            await RunTest("Edge.ReadAllInGraph", TestEdgeReadAllInGraph).ConfigureAwait(false);
            await RunTest("Edge.ReadMany", TestEdgeReadMany).ConfigureAwait(false);
            await RunTest("Edge.ReadFirst", TestEdgeReadFirst).ConfigureAwait(false);
            await RunTest("Edge.ReadByGuids", TestEdgeReadByGuids).ConfigureAwait(false);
            await RunTest("Edge.Enumerate", TestEdgeEnumerate).ConfigureAwait(false);
            await RunTest("Edge.ReadNodeEdges", TestEdgeReadNodeEdges).ConfigureAwait(false);
            await RunTest("Edge.ReadEdgesFromNode", TestEdgeReadEdgesFromNode).ConfigureAwait(false);
            await RunTest("Edge.ReadEdgesToNode", TestEdgeReadEdgesToNode).ConfigureAwait(false);
            await RunTest("Edge.ReadEdgesBetweenNodes", TestEdgeReadEdgesBetweenNodes).ConfigureAwait(false);

            // Node relationship tests
            await RunTest("Node.ReadParents", TestNodeReadParents).ConfigureAwait(false);
            await RunTest("Node.ReadChildren", TestNodeReadChildren).ConfigureAwait(false);
            await RunTest("Node.ReadNeighbors", TestNodeReadNeighbors).ConfigureAwait(false);

            // Label Tests
            await RunTest("Label.Create", TestLabelCreate).ConfigureAwait(false);
            await RunTest("Label.CreateMany", TestLabelCreateMany).ConfigureAwait(false);
            await RunTest("Label.ReadByGuid", TestLabelReadByGuid).ConfigureAwait(false);
            await RunTest("Label.ExistsByGuid", TestLabelExistsByGuid).ConfigureAwait(false);
            await RunTest("Label.Update", TestLabelUpdate).ConfigureAwait(false);
            await RunTest("Label.ReadAllInTenant", TestLabelReadAllInTenant).ConfigureAwait(false);
            await RunTest("Label.ReadAllInGraph", TestLabelReadAllInGraph).ConfigureAwait(false);
            await RunTest("Label.ReadMany", TestLabelReadMany).ConfigureAwait(false);
            await RunTest("Label.ReadManyGraph", TestLabelReadManyGraph).ConfigureAwait(false);
            await RunTest("Label.ReadManyNode", TestLabelReadManyNode).ConfigureAwait(false);
            await RunTest("Label.ReadManyEdge", TestLabelReadManyEdge).ConfigureAwait(false);
            await RunTest("Label.ReadByGuids", TestLabelReadByGuids).ConfigureAwait(false);
            await RunTest("Label.Enumerate", TestLabelEnumerate).ConfigureAwait(false);

            // Tag Tests
            await RunTest("Tag.Create", TestTagCreate).ConfigureAwait(false);
            await RunTest("Tag.CreateMany", TestTagCreateMany).ConfigureAwait(false);
            await RunTest("Tag.ReadByGuid", TestTagReadByGuid).ConfigureAwait(false);
            await RunTest("Tag.ExistsByGuid", TestTagExistsByGuid).ConfigureAwait(false);
            await RunTest("Tag.Update", TestTagUpdate).ConfigureAwait(false);
            await RunTest("Tag.ReadAllInTenant", TestTagReadAllInTenant).ConfigureAwait(false);
            await RunTest("Tag.ReadAllInGraph", TestTagReadAllInGraph).ConfigureAwait(false);
            await RunTest("Tag.ReadMany", TestTagReadMany).ConfigureAwait(false);
            await RunTest("Tag.ReadManyGraph", TestTagReadManyGraph).ConfigureAwait(false);
            await RunTest("Tag.ReadManyNode", TestTagReadManyNode).ConfigureAwait(false);
            await RunTest("Tag.ReadManyEdge", TestTagReadManyEdge).ConfigureAwait(false);
            await RunTest("Tag.ReadByGuids", TestTagReadByGuids).ConfigureAwait(false);
            await RunTest("Tag.Enumerate", TestTagEnumerate).ConfigureAwait(false);

            // Vector Tests
            await RunTest("Vector.Create", TestVectorCreate).ConfigureAwait(false);
            await RunTest("Vector.CreateMany", TestVectorCreateMany).ConfigureAwait(false);
            await RunTest("Vector.ReadByGuid", TestVectorReadByGuid).ConfigureAwait(false);
            await RunTest("Vector.ExistsByGuid", TestVectorExistsByGuid).ConfigureAwait(false);
            await RunTest("Vector.Update", TestVectorUpdate).ConfigureAwait(false);
            await RunTest("Vector.ReadAllInTenant", TestVectorReadAllInTenant).ConfigureAwait(false);
            await RunTest("Vector.ReadAllInGraph", TestVectorReadAllInGraph).ConfigureAwait(false);
            await RunTest("Vector.ReadMany", TestVectorReadMany).ConfigureAwait(false);
            await RunTest("Vector.ReadManyGraph", TestVectorReadManyGraph).ConfigureAwait(false);
            await RunTest("Vector.ReadManyNode", TestVectorReadManyNode).ConfigureAwait(false);
            await RunTest("Vector.ReadManyEdge", TestVectorReadManyEdge).ConfigureAwait(false);
            await RunTest("Vector.ReadByGuids", TestVectorReadByGuids).ConfigureAwait(false);
            await RunTest("Vector.Enumerate", TestVectorEnumerate).ConfigureAwait(false);
            await RunTest("Vector.Search", TestVectorSearch).ConfigureAwait(false);

            // Enumeration and Pagination Tests
            await RunTest("Enumeration.Tenants.Skip", TestEnumerationTenantsSkip).ConfigureAwait(false);
            await RunTest("Enumeration.Tenants.ContinuationToken", TestEnumerationTenantsContinuationToken).ConfigureAwait(false);
            await RunTest("Enumeration.Graphs.Paginated", TestEnumerationGraphsPaginated).ConfigureAwait(false);
            await RunTest("Enumeration.Nodes.Paginated", TestEnumerationNodesPaginated).ConfigureAwait(false);
        }

        private static async Task RunTest(string testName, Func<Task> testFunc)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool passed = false;
            string? errorMessage = null;

            try
            {
                await testFunc().ConfigureAwait(false);
                passed = true;
            }
            catch (Exception ex)
            {
                passed = false;
                errorMessage = ex.Message;
            }

            stopwatch.Stop();

            TestResult result = new TestResult
            {
                Name = testName,
                Passed = passed,
                RuntimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = errorMessage
            };

            _TestResults.Add(result);

            // Print result for every test
            string status = passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{status}] {testName,-50} {stopwatch.ElapsedMilliseconds,6}ms");

            if (!passed)
            {
                Console.WriteLine($"       Error: {errorMessage}");
            }
        }

        private static void PrintSummary()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("==============================================");
            Console.WriteLine("  Test Summary");
            Console.WriteLine("==============================================");
            Console.WriteLine("");

            int passCount = _TestResults.Count(r => r.Passed);
            int failCount = _TestResults.Count(r => !r.Passed);
            long totalTime = _TestResults.Sum(r => r.RuntimeMs);
            double avgTime = _TestResults.Average(r => r.RuntimeMs);

            // Print all test results
            foreach (TestResult result in _TestResults)
            {
                string status = result.Passed ? "PASS" : "FAIL";
                Console.WriteLine($"[{status}] {result.Name,-50} {result.RuntimeMs,6}ms");
            }

            Console.WriteLine("");
            Console.WriteLine("==============================================");
            Console.WriteLine($"Total Tests:    {_TestResults.Count}");
            Console.WriteLine($"Passed:         {passCount}");
            Console.WriteLine($"Failed:         {failCount}");
            Console.WriteLine($"Total Time:     {totalTime}ms");
            Console.WriteLine($"Average Time:   {avgTime:F2}ms");
            Console.WriteLine("==============================================");

            if (failCount > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Failed Tests:");
                foreach (TestResult result in _TestResults.Where(r => !r.Passed))
                {
                    Console.WriteLine($"  - {result.Name}: {result.ErrorMessage}");
                }
            }

            Console.WriteLine("");
            string overallResult = failCount == 0 ? "PASS" : "FAIL";
            Console.WriteLine($"Overall Result: {overallResult}");
            Console.WriteLine("");
        }

        private static void PrintCombinedSummary(List<TestResult> inMemoryResults, List<TestResult> onDiskResults)
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("==============================================");
            Console.WriteLine("  Combined Test Summary");
            Console.WriteLine("==============================================");
            Console.WriteLine("");

            // Print detailed results for each test
            Console.WriteLine("Detailed Test Results:");
            Console.WriteLine("");
            Console.WriteLine($"{"Test Name",-52} {"In-Memory",-15} {"On-Disk",-15}");
            Console.WriteLine(new string('-', 82));

            for (int i = 0; i < inMemoryResults.Count; i++)
            {
                string memStatus = inMemoryResults[i].Passed ? $"PASS ({inMemoryResults[i].RuntimeMs}ms)" : $"FAIL ({inMemoryResults[i].RuntimeMs}ms)";
                string diskStatus = onDiskResults[i].Passed ? $"PASS ({onDiskResults[i].RuntimeMs}ms)" : $"FAIL ({onDiskResults[i].RuntimeMs}ms)";
                Console.WriteLine($"{inMemoryResults[i].Name,-52} {memStatus,-15} {diskStatus,-15}");
            }

            Console.WriteLine("");
            Console.WriteLine("==============================================");

            // In-Memory Results
            int inMemoryPassCount = inMemoryResults.Count(r => r.Passed);
            int inMemoryFailCount = inMemoryResults.Count(r => !r.Passed);
            long inMemoryTotalTime = inMemoryResults.Sum(r => r.RuntimeMs);
            double inMemoryAvgTime = inMemoryResults.Average(r => r.RuntimeMs);

            Console.WriteLine("In-Memory Database:");
            Console.WriteLine($"  Total Tests:    {inMemoryResults.Count}");
            Console.WriteLine($"  Passed:         {inMemoryPassCount}");
            Console.WriteLine($"  Failed:         {inMemoryFailCount}");
            Console.WriteLine($"  Total Time:     {inMemoryTotalTime}ms");
            Console.WriteLine($"  Average Time:   {inMemoryAvgTime:F2}ms");
            Console.WriteLine("");

            // On-Disk Results
            int onDiskPassCount = onDiskResults.Count(r => r.Passed);
            int onDiskFailCount = onDiskResults.Count(r => !r.Passed);
            long onDiskTotalTime = onDiskResults.Sum(r => r.RuntimeMs);
            double onDiskAvgTime = onDiskResults.Average(r => r.RuntimeMs);

            Console.WriteLine("On-Disk Database:");
            Console.WriteLine($"  Total Tests:    {onDiskResults.Count}");
            Console.WriteLine($"  Passed:         {onDiskPassCount}");
            Console.WriteLine($"  Failed:         {onDiskFailCount}");
            Console.WriteLine($"  Total Time:     {onDiskTotalTime}ms");
            Console.WriteLine($"  Average Time:   {onDiskAvgTime:F2}ms");
            Console.WriteLine("");

            // Combined Results
            Console.WriteLine("==============================================");
            Console.WriteLine($"Total Tests:    {inMemoryResults.Count + onDiskResults.Count}");
            Console.WriteLine($"Passed:         {inMemoryPassCount + onDiskPassCount}");
            Console.WriteLine($"Failed:         {inMemoryFailCount + onDiskFailCount}");
            Console.WriteLine($"Total Time:     {inMemoryTotalTime + onDiskTotalTime}ms");
            Console.WriteLine("==============================================");

            // Print failed tests if any
            List<TestResult> allFailedTests = new List<TestResult>();
            allFailedTests.AddRange(inMemoryResults.Where(r => !r.Passed).Select(r => new TestResult { Name = r.Name + " (In-Memory)", Passed = r.Passed, RuntimeMs = r.RuntimeMs, ErrorMessage = r.ErrorMessage }));
            allFailedTests.AddRange(onDiskResults.Where(r => !r.Passed).Select(r => new TestResult { Name = r.Name + " (On-Disk)", Passed = r.Passed, RuntimeMs = r.RuntimeMs, ErrorMessage = r.ErrorMessage }));

            if (allFailedTests.Count > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Failed Tests:");
                foreach (TestResult result in allFailedTests)
                {
                    Console.WriteLine($"  - {result.Name}: {result.ErrorMessage}");
                }
            }

            Console.WriteLine("");
            int totalFailCount = inMemoryFailCount + onDiskFailCount;
            string overallResult = totalFailCount == 0 ? "PASS" : "FAIL";
            Console.WriteLine($"Overall Result: {overallResult}");
            Console.WriteLine("");
        }

        private static void ResetTestState()
        {
            _TenantGuid = Guid.Empty;
            _UserGuid = Guid.Empty;
            _CredentialGuid = Guid.Empty;
            _GraphGuid = Guid.Empty;
            _Node1Guid = Guid.Empty;
            _Node2Guid = Guid.Empty;
            _Node3Guid = Guid.Empty;
            _EdgeGuid = Guid.Empty;
            _LabelGuid = Guid.Empty;
            _TagGuid = Guid.Empty;
            _VectorGuid = Guid.Empty;
        }

        private static void Cleanup()
        {
            try
            {
                _Client?.Dispose();

                if (File.Exists("test-automated-memory.db"))
                {
                    File.Delete("test-automated-memory.db");
                }

                if (File.Exists("test-automated-disk.db"))
                {
                    File.Delete("test-automated-disk.db");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Cleanup error: {ex.Message}");
            }
        }

        // ========================================
        // Tenant Tests
        // ========================================

        private static async Task TestTenantCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TenantMetadata tenant = new TenantMetadata
            {
                Name = "Test Tenant",
                // No email field in TenantMetadata
            };

            TenantMetadata? created = await _Client.Tenant.Create(tenant).ConfigureAwait(false);

            AssertNotNull(created, "Created tenant");
            AssertNotEmpty(created.GUID, "Tenant GUID");
            AssertEqual(tenant.Name, created.Name, "Tenant Name");
            // ContactEmail not available
            AssertNotNull(created.CreatedUtc, "Tenant CreatedUtc");

            _TenantGuid = created.GUID;
        }

        private static async Task TestTenantReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TenantMetadata? tenant = await _Client.Tenant.ReadByGuid(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(tenant, "Tenant");
            AssertEqual(_TenantGuid, tenant.GUID, "Tenant GUID");
            AssertEqual("Test Tenant", tenant.Name, "Tenant Name");
        }

        private static async Task TestTenantExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Tenant.ExistsByGuid(_TenantGuid).ConfigureAwait(false);
            AssertTrue(exists, "Tenant exists");

            bool notExists = await _Client.Tenant.ExistsByGuid(Guid.NewGuid()).ConfigureAwait(false);
            AssertFalse(notExists, "Non-existent tenant");
        }

        private static async Task TestTenantUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TenantMetadata? tenant = await _Client.Tenant.ReadByGuid(_TenantGuid).ConfigureAwait(false);
            AssertNotNull(tenant, "Tenant");

            tenant.Name = "Updated Tenant";
            TenantMetadata? updated = await _Client.Tenant.Update(tenant).ConfigureAwait(false);

            AssertNotNull(updated, "Updated tenant");
            AssertEqual("Updated Tenant", updated.Name, "Updated Name");
        }

        private static async Task TestTenantReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TenantMetadata> tenants = new List<TenantMetadata>();
            await foreach (TenantMetadata tenant in _Client.Tenant.ReadMany())
            {
                tenants.Add(tenant);
            }

            AssertTrue(tenants.Count > 0, "Tenants count");
            AssertNotNull(tenants[0].GUID, "First tenant GUID");
        }

        private static async Task TestTenantReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _TenantGuid };
            List<TenantMetadata> tenants = new List<TenantMetadata>();

            await foreach (TenantMetadata tenant in _Client.Tenant.ReadByGuids(guids))
            {
                tenants.Add(tenant);
            }

            AssertEqual(1, tenants.Count, "Tenants count");
            AssertEqual(_TenantGuid, tenants[0].GUID, "Tenant GUID");
        }

        private static async Task TestTenantEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                MaxResults = 10
            };

            EnumerationResult<TenantMetadata>? result = await _Client.Tenant.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
            AssertTrue(result.Objects.Count > 0, "Results count");
        }

        private static async Task TestTenantGetStatistics()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TenantStatistics? stats = await _Client.Tenant.GetStatistics(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(stats, "Statistics");
            // TenantStatistics doesn't have a TenantGUID property, just verify it's not null
        }

        // ========================================
        // User Tests
        // ========================================

        private static async Task TestUserCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            UserMaster user = new UserMaster
            {
                TenantGUID = _TenantGuid,
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User"
            };

            UserMaster? created = await _Client.User.Create(user).ConfigureAwait(false);

            AssertNotNull(created, "Created user");
            AssertNotEmpty(created.GUID, "User GUID");
            AssertEqual(user.Email, created.Email, "User Email");
            AssertEqual(user.FirstName, created.FirstName, "User FirstName");
            AssertEqual(user.LastName, created.LastName, "User LastName");

            _UserGuid = created.GUID;
        }

        private static async Task TestUserReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            UserMaster? user = await _Client.User.ReadByGuid(_TenantGuid, _UserGuid).ConfigureAwait(false);

            AssertNotNull(user, "User");
            AssertEqual(_UserGuid, user.GUID, "User GUID");
            AssertEqual("user@example.com", user.Email, "User Email");
        }

        private static async Task TestUserReadByEmail()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            UserMaster? user = await _Client.User.ReadByEmail(_TenantGuid, "user@example.com").ConfigureAwait(false);

            AssertNotNull(user, "User");
            AssertEqual(_UserGuid, user.GUID, "User GUID");
        }

        private static async Task TestUserExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.User.ExistsByGuid(_TenantGuid, _UserGuid).ConfigureAwait(false);
            AssertTrue(exists, "User exists");

            bool notExists = await _Client.User.ExistsByGuid(_TenantGuid, Guid.NewGuid()).ConfigureAwait(false);
            AssertFalse(notExists, "Non-existent user");
        }

        private static async Task TestUserExistsByEmail()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.User.ExistsByEmail(_TenantGuid, "user@example.com").ConfigureAwait(false);
            AssertTrue(exists, "User exists by email");

            bool notExists = await _Client.User.ExistsByEmail(_TenantGuid, "nonexistent@example.com").ConfigureAwait(false);
            AssertFalse(notExists, "Non-existent user by email");
        }

        private static async Task TestUserUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            UserMaster? user = await _Client.User.ReadByGuid(_TenantGuid, _UserGuid).ConfigureAwait(false);
            AssertNotNull(user, "User");

            user.FirstName = "Updated";
            UserMaster? updated = await _Client.User.Update(user).ConfigureAwait(false);

            AssertNotNull(updated, "Updated user");
            AssertEqual("Updated", updated.FirstName, "Updated FirstName");
        }

        private static async Task TestUserReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<UserMaster> users = new List<UserMaster>();
            await foreach (UserMaster user in _Client.User.ReadAllInTenant(_TenantGuid))
            {
                users.Add(user);
            }

            AssertTrue(users.Count > 0, "Users count");
        }

        private static async Task TestUserReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<UserMaster> users = new List<UserMaster>();
            await foreach (UserMaster user in _Client.User.ReadMany(_TenantGuid, "user@example.com"))
            {
                users.Add(user);
            }

            AssertTrue(users.Count > 0, "Users count");
        }

        private static async Task TestUserReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _UserGuid };
            List<UserMaster> users = new List<UserMaster>();

            await foreach (UserMaster user in _Client.User.ReadByGuids(_TenantGuid, guids))
            {
                users.Add(user);
            }

            AssertEqual(1, users.Count, "Users count");
        }

        private static async Task TestUserEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                MaxResults = 10
            };

            EnumerationResult<UserMaster>? result = await _Client.User.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        // ========================================
        // Credential Tests
        // ========================================

        private static async Task TestCredentialCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Credential credential = new Credential
            {
                TenantGUID = _TenantGuid,
                UserGUID = _UserGuid,
                BearerToken = Guid.NewGuid().ToString(),
                Active = true
            };

            Credential? created = await _Client.Credential.Create(credential).ConfigureAwait(false);

            AssertNotNull(created, "Created credential");
            AssertNotEmpty(created.GUID, "Credential GUID");
            AssertEqual(credential.BearerToken, created.BearerToken, "Credential BearerToken");
            AssertEqual(credential.Active, created.Active, "Credential Active");

            _CredentialGuid = created.GUID;
        }

        private static async Task TestCredentialReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Credential? credential = await _Client.Credential.ReadByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);

            AssertNotNull(credential, "Credential");
            AssertEqual(_CredentialGuid, credential.GUID, "Credential GUID");
        }

        private static async Task TestCredentialReadByBearerToken()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Credential? original = await _Client.Credential.ReadByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);
            AssertNotNull(original, "Original credential");

            Credential? credential = await _Client.Credential.ReadByBearerToken(original.BearerToken).ConfigureAwait(false);

            AssertNotNull(credential, "Credential by bearer token");
            AssertEqual(_CredentialGuid, credential.GUID, "Credential GUID");
        }

        private static async Task TestCredentialExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Credential.ExistsByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);
            AssertTrue(exists, "Credential exists");
        }

        private static async Task TestCredentialUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Credential? credential = await _Client.Credential.ReadByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);
            AssertNotNull(credential, "Credential");

            credential.Active = false;
            Credential? updated = await _Client.Credential.Update(credential).ConfigureAwait(false);

            AssertNotNull(updated, "Updated credential");
            AssertFalse(updated.Active, "Updated Active status");
        }

        private static async Task TestCredentialReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Credential> credentials = new List<Credential>();
            await foreach (Credential credential in _Client.Credential.ReadAllInTenant(_TenantGuid))
            {
                credentials.Add(credential);
            }

            AssertTrue(credentials.Count > 0, "Credentials count");
        }

        private static async Task TestCredentialReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Credential> credentials = new List<Credential>();
            await foreach (Credential credential in _Client.Credential.ReadMany(_TenantGuid, _UserGuid, null))
            {
                credentials.Add(credential);
            }

            AssertTrue(credentials.Count > 0, "Credentials count");
        }

        private static async Task TestCredentialReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _CredentialGuid };
            List<Credential> credentials = new List<Credential>();

            await foreach (Credential credential in _Client.Credential.ReadByGuids(_TenantGuid, guids))
            {
                credentials.Add(credential);
            }

            AssertEqual(1, credentials.Count, "Credentials count");
        }

        private static async Task TestCredentialEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                MaxResults = 10
            };

            EnumerationResult<Credential>? result = await _Client.Credential.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        // ========================================
        // Graph Tests
        // ========================================

        private static async Task TestGraphCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Graph graph = new Graph
            {
                TenantGUID = _TenantGuid,
                Name = "Test Graph",
                Data = "{ \"description\": \"Test graph data\" }"
            };

            Graph? created = await _Client.Graph.Create(graph).ConfigureAwait(false);

            AssertNotNull(created, "Created graph");
            AssertNotEmpty(created.GUID, "Graph GUID");
            AssertEqual(graph.Name, created.Name, "Graph Name");
            AssertNotNull(created.CreatedUtc, "Graph CreatedUtc");

            _GraphGuid = created.GUID;
        }

        private static async Task TestGraphReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Graph? graph = await _Client.Graph.ReadByGuid(_TenantGuid, _GraphGuid, true, false).ConfigureAwait(false);

            AssertNotNull(graph, "Graph");
            AssertEqual(_GraphGuid, graph.GUID, "Graph GUID");
            AssertEqual("Test Graph", graph.Name, "Graph Name");
            AssertNotNull(graph.Data, "Graph Data");
        }

        private static async Task TestGraphExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Graph.ExistsByGuid(_TenantGuid, _GraphGuid).ConfigureAwait(false);
            AssertTrue(exists, "Graph exists");
        }

        private static async Task TestGraphUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Graph? graph = await _Client.Graph.ReadByGuid(_TenantGuid, _GraphGuid).ConfigureAwait(false);
            AssertNotNull(graph, "Graph");

            graph.Name = "Updated Graph";
            Graph? updated = await _Client.Graph.Update(graph).ConfigureAwait(false);

            AssertNotNull(updated, "Updated graph");
            AssertEqual("Updated Graph", updated.Name, "Updated Name");
        }

        private static async Task TestGraphReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Graph> graphs = new List<Graph>();
            await foreach (Graph graph in _Client.Graph.ReadAllInTenant(_TenantGuid))
            {
                graphs.Add(graph);
            }

            AssertTrue(graphs.Count > 0, "Graphs count");
        }

        private static async Task TestGraphReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Graph> graphs = new List<Graph>();
            await foreach (Graph graph in _Client.Graph.ReadMany(_TenantGuid))
            {
                graphs.Add(graph);
            }

            AssertTrue(graphs.Count > 0, "Graphs count");
        }

        private static async Task TestGraphReadFirst()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Graph? graph = await _Client.Graph.ReadFirst(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(graph, "First graph");
        }

        private static async Task TestGraphReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _GraphGuid };
            List<Graph> graphs = new List<Graph>();

            await foreach (Graph graph in _Client.Graph.ReadByGuids(_TenantGuid, guids))
            {
                graphs.Add(graph);
            }

            AssertEqual(1, graphs.Count, "Graphs count");
        }

        private static async Task TestGraphEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                MaxResults = 10
            };

            EnumerationResult<Graph>? result = await _Client.Graph.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        private static async Task TestGraphGetStatistics()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphStatistics? stats = await _Client.Graph.GetStatistics(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(stats, "Statistics");
        }

        // ========================================
        // Node Tests
        // ========================================

        private static async Task TestNodeCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Node node = new Node
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Name = "Test Node",
                Data = "{ \"type\": \"test\" }"
            };

            Node? created = await _Client.Node.Create(node).ConfigureAwait(false);

            AssertNotNull(created, "Created node");
            AssertNotEmpty(created.GUID, "Node GUID");
            AssertEqual(node.Name, created.Name, "Node Name");

            _Node1Guid = created.GUID;
        }

        private static async Task TestNodeCreateMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> nodes = new List<Node>
            {
                new Node
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Name = "Node 2",
                    Data = "{ \"type\": \"test2\" }"
                },
                new Node
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Name = "Node 3",
                    Data = "{ \"type\": \"test3\" }"
                }
            };

            List<Node>? created = await _Client.Node.CreateMany(_TenantGuid, _GraphGuid, nodes).ConfigureAwait(false);

            AssertNotNull(created, "Created nodes");
            AssertEqual(2, created.Count, "Nodes count");
            AssertNotEmpty(created[0].GUID, "Node1 GUID");
            AssertNotEmpty(created[1].GUID, "Node2 GUID");

            _Node2Guid = created[0].GUID;
            _Node3Guid = created[1].GUID;
        }

        private static async Task TestNodeReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Node? node = await _Client.Node.ReadByGuid(_TenantGuid, _GraphGuid, _Node1Guid, true, false).ConfigureAwait(false);

            AssertNotNull(node, "Node");
            AssertEqual(_Node1Guid, node.GUID, "Node GUID");
            AssertEqual("Test Node", node.Name, "Node Name");
            AssertNotNull(node.Data, "Node Data");
        }

        private static async Task TestNodeExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Node.ExistsByGuid(_TenantGuid, _Node1Guid).ConfigureAwait(false);
            AssertTrue(exists, "Node exists");
        }

        private static async Task TestNodeUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Node? node = await _Client.Node.ReadByGuid(_TenantGuid, _GraphGuid, _Node1Guid).ConfigureAwait(false);
            AssertNotNull(node, "Node");

            node.Name = "Updated Node";
            Node? updated = await _Client.Node.Update(node).ConfigureAwait(false);

            AssertNotNull(updated, "Updated node");
            AssertEqual("Updated Node", updated.Name, "Updated Name");
        }

        private static async Task TestNodeReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> nodes = new List<Node>();
            await foreach (Node node in _Client.Node.ReadAllInTenant(_TenantGuid))
            {
                nodes.Add(node);
            }

            AssertTrue(nodes.Count >= 3, "Nodes count");
        }

        private static async Task TestNodeReadAllInGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> nodes = new List<Node>();
            await foreach (Node node in _Client.Node.ReadAllInGraph(_TenantGuid, _GraphGuid))
            {
                nodes.Add(node);
            }

            AssertTrue(nodes.Count >= 3, "Nodes count");
        }

        private static async Task TestNodeReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> nodes = new List<Node>();
            await foreach (Node node in _Client.Node.ReadMany(_TenantGuid, _GraphGuid))
            {
                nodes.Add(node);
            }

            AssertTrue(nodes.Count >= 3, "Nodes count");
        }

        private static async Task TestNodeReadFirst()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Node? node = await _Client.Node.ReadFirst(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(node, "First node");
        }

        private static async Task TestNodeReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _Node1Guid, _Node2Guid };
            List<Node> nodes = new List<Node>();

            await foreach (Node node in _Client.Node.ReadByGuids(_TenantGuid, guids))
            {
                nodes.Add(node);
            }

            AssertEqual(2, nodes.Count, "Nodes count");
        }

        private static async Task TestNodeEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 10
            };

            EnumerationResult<Node>? result = await _Client.Node.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        private static async Task TestNodeReadMostConnected()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> nodes = new List<Node>();
            await foreach (Node node in _Client.Node.ReadMostConnected(_TenantGuid, _GraphGuid))
            {
                nodes.Add(node);
                if (nodes.Count >= 5) break; // Limit results
            }

            // Should not throw
            AssertTrue(true, "Read most connected nodes");
        }

        private static async Task TestNodeReadLeastConnected()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> nodes = new List<Node>();
            await foreach (Node node in _Client.Node.ReadLeastConnected(_TenantGuid, _GraphGuid))
            {
                nodes.Add(node);
                if (nodes.Count >= 5) break; // Limit results
            }

            // Should not throw
            AssertTrue(true, "Read least connected nodes");
        }

        // ========================================
        // Edge Tests
        // ========================================

        private static async Task TestEdgeCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Edge edge = new Edge
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                From = _Node1Guid,
                To = _Node2Guid,
                Name = "Test Edge",
                Cost = 1,
                Data = "{ \"type\": \"connection\" }"
            };

            Edge? created = await _Client.Edge.Create(edge).ConfigureAwait(false);

            AssertNotNull(created, "Created edge");
            AssertNotEmpty(created.GUID, "Edge GUID");
            AssertEqual(edge.Name, created.Name, "Edge Name");
            AssertEqual(edge.Cost, created.Cost, "Edge Cost");

            _EdgeGuid = created.GUID;
        }

        private static async Task TestEdgeCreateMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>
            {
                new Edge
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    From = _Node2Guid,
                    To = _Node3Guid,
                    Name = "Edge 2-3",
                    Cost = 2
                },
                new Edge
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    From = _Node3Guid,
                    To = _Node1Guid,
                    Name = "Edge 3-1",
                    Cost = 3
                }
            };

            List<Edge>? created = await _Client.Edge.CreateMany(_TenantGuid, _GraphGuid, edges).ConfigureAwait(false);

            AssertNotNull(created, "Created edges");
            AssertEqual(2, created.Count, "Edges count");
        }

        private static async Task TestEdgeReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Edge? edge = await _Client.Edge.ReadByGuid(_TenantGuid, _GraphGuid, _EdgeGuid, true, false).ConfigureAwait(false);

            AssertNotNull(edge, "Edge");
            AssertEqual(_EdgeGuid, edge.GUID, "Edge GUID");
            AssertEqual("Test Edge", edge.Name, "Edge Name");
        }

        private static async Task TestEdgeExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Edge.ExistsByGuid(_TenantGuid, _GraphGuid, _EdgeGuid).ConfigureAwait(false);
            AssertTrue(exists, "Edge exists");
        }

        private static async Task TestEdgeUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Edge? edge = await _Client.Edge.ReadByGuid(_TenantGuid, _GraphGuid, _EdgeGuid).ConfigureAwait(false);
            AssertNotNull(edge, "Edge");

            edge.Name = "Updated Edge";
            Edge? updated = await _Client.Edge.Update(edge).ConfigureAwait(false);

            AssertNotNull(updated, "Updated edge");
            AssertEqual("Updated Edge", updated.Name, "Updated Name");
        }

        private static async Task TestEdgeReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadAllInTenant(_TenantGuid))
            {
                edges.Add(edge);
            }

            AssertTrue(edges.Count >= 3, "Edges count");
        }

        private static async Task TestEdgeReadAllInGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadAllInGraph(_TenantGuid, _GraphGuid))
            {
                edges.Add(edge);
            }

            AssertTrue(edges.Count >= 3, "Edges count");
        }

        private static async Task TestEdgeReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadMany(_TenantGuid, _GraphGuid))
            {
                edges.Add(edge);
            }

            AssertTrue(edges.Count >= 3, "Edges count");
        }

        private static async Task TestEdgeReadFirst()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            Edge? edge = await _Client.Edge.ReadFirst(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(edge, "First edge");
        }

        private static async Task TestEdgeReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _EdgeGuid };
            List<Edge> edges = new List<Edge>();

            await foreach (Edge edge in _Client.Edge.ReadByGuids(_TenantGuid, guids))
            {
                edges.Add(edge);
            }

            AssertEqual(1, edges.Count, "Edges count");
        }

        private static async Task TestEdgeEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 10
            };

            EnumerationResult<Edge>? result = await _Client.Edge.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        private static async Task TestEdgeReadNodeEdges()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadNodeEdges(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                edges.Add(edge);
            }

            AssertTrue(edges.Count >= 1, "Node edges count");
        }

        private static async Task TestEdgeReadEdgesFromNode()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadEdgesFromNode(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                edges.Add(edge);
            }

            // Should not throw
            AssertTrue(true, "Read edges from node");
        }

        private static async Task TestEdgeReadEdgesToNode()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadEdgesToNode(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                edges.Add(edge);
            }

            // Should not throw
            AssertTrue(true, "Read edges to node");
        }

        private static async Task TestEdgeReadEdgesBetweenNodes()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadEdgesBetweenNodes(_TenantGuid, _GraphGuid, _Node1Guid, _Node2Guid))
            {
                edges.Add(edge);
            }

            AssertTrue(edges.Count >= 1, "Edges between nodes");
        }

        // ========================================
        // Node Relationship Tests
        // ========================================

        private static async Task TestNodeReadParents()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> parents = new List<Node>();
            await foreach (Node node in _Client.Node.ReadParents(_TenantGuid, _GraphGuid, _Node2Guid))
            {
                parents.Add(node);
            }

            // Should not throw
            AssertTrue(true, "Read parent nodes");
        }

        private static async Task TestNodeReadChildren()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> children = new List<Node>();
            await foreach (Node node in _Client.Node.ReadChildren(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                children.Add(node);
            }

            // Should not throw
            AssertTrue(true, "Read child nodes");
        }

        private static async Task TestNodeReadNeighbors()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Node> neighbors = new List<Node>();
            await foreach (Node node in _Client.Node.ReadNeighbors(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                neighbors.Add(node);
            }

            // Should not throw
            AssertTrue(true, "Read neighbor nodes");
        }

        // ========================================
        // Label Tests
        // ========================================

        private static async Task TestLabelCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            LabelMetadata label = new LabelMetadata
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                NodeGUID = _Node1Guid,
                Label = "TestLabel"
            };

            LabelMetadata? created = await _Client.Label.Create(label).ConfigureAwait(false);

            AssertNotNull(created, "Created label");
            AssertNotEmpty(created.GUID, "Label GUID");
            AssertEqual(label.Label, created.Label, "Label value");

            _LabelGuid = created.GUID;
        }

        private static async Task TestLabelCreateMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>
            {
                new LabelMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    NodeGUID = _Node2Guid,
                    Label = "Label2"
                },
                new LabelMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    EdgeGUID = _EdgeGuid,
                    Label = "EdgeLabel"
                }
            };

            List<LabelMetadata>? created = await _Client.Label.CreateMany(_TenantGuid, labels).ConfigureAwait(false);

            AssertNotNull(created, "Created labels");
            AssertEqual(2, created.Count, "Labels count");
        }

        private static async Task TestLabelReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            LabelMetadata? label = await _Client.Label.ReadByGuid(_TenantGuid, _LabelGuid).ConfigureAwait(false);

            AssertNotNull(label, "Label");
            AssertEqual(_LabelGuid, label.GUID, "Label GUID");
        }

        private static async Task TestLabelExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Label.ExistsByGuid(_TenantGuid, _LabelGuid).ConfigureAwait(false);
            AssertTrue(exists, "Label exists");
        }

        private static async Task TestLabelUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            LabelMetadata? label = await _Client.Label.ReadByGuid(_TenantGuid, _LabelGuid).ConfigureAwait(false);
            AssertNotNull(label, "Label");

            label.Label = "UpdatedLabel";
            LabelMetadata? updated = await _Client.Label.Update(label).ConfigureAwait(false);

            AssertNotNull(updated, "Updated label");
            AssertEqual("UpdatedLabel", updated.Label, "Updated Label");
        }

        private static async Task TestLabelReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Client.Label.ReadAllInTenant(_TenantGuid))
            {
                labels.Add(label);
            }

            AssertTrue(labels.Count >= 3, "Labels count");
        }

        private static async Task TestLabelReadAllInGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Client.Label.ReadAllInGraph(_TenantGuid, _GraphGuid))
            {
                labels.Add(label);
            }

            AssertTrue(labels.Count >= 3, "Labels count");
        }

        private static async Task TestLabelReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Client.Label.ReadMany(_TenantGuid, _GraphGuid, null, null, null))
            {
                labels.Add(label);
            }

            AssertTrue(labels.Count >= 3, "Labels count");
        }

        private static async Task TestLabelReadManyGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Client.Label.ReadManyGraph(_TenantGuid, _GraphGuid))
            {
                labels.Add(label);
            }

            // Should not throw
            AssertTrue(true, "Read graph labels");
        }

        private static async Task TestLabelReadManyNode()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Client.Label.ReadManyNode(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                labels.Add(label);
            }

            AssertTrue(labels.Count >= 1, "Node labels count");
        }

        private static async Task TestLabelReadManyEdge()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<LabelMetadata> labels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Client.Label.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuid))
            {
                labels.Add(label);
            }

            AssertTrue(labels.Count >= 1, "Edge labels count");
        }

        private static async Task TestLabelReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _LabelGuid };
            List<LabelMetadata> labels = new List<LabelMetadata>();

            await foreach (LabelMetadata label in _Client.Label.ReadByGuids(_TenantGuid, guids))
            {
                labels.Add(label);
            }

            AssertEqual(1, labels.Count, "Labels count");
        }

        private static async Task TestLabelEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 10
            };

            EnumerationResult<LabelMetadata>? result = await _Client.Label.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        // ========================================
        // Tag Tests
        // ========================================

        private static async Task TestTagCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TagMetadata tag = new TagMetadata
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                NodeGUID = _Node1Guid,
                Key = "TestKey",
                Value = "TestValue"
            };

            TagMetadata? created = await _Client.Tag.Create(tag).ConfigureAwait(false);

            AssertNotNull(created, "Created tag");
            AssertNotEmpty(created.GUID, "Tag GUID");
            AssertEqual(tag.Key, created.Key, "Tag Key");
            AssertEqual(tag.Value, created.Value, "Tag Value");

            _TagGuid = created.GUID;
        }

        private static async Task TestTagCreateMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>
            {
                new TagMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    NodeGUID = _Node2Guid,
                    Key = "Key2",
                    Value = "Value2"
                },
                new TagMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    EdgeGUID = _EdgeGuid,
                    Key = "EdgeKey",
                    Value = "EdgeValue"
                }
            };

            List<TagMetadata>? created = await _Client.Tag.CreateMany(_TenantGuid, tags).ConfigureAwait(false);

            AssertNotNull(created, "Created tags");
            AssertEqual(2, created.Count, "Tags count");
        }

        private static async Task TestTagReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TagMetadata? tag = await _Client.Tag.ReadByGuid(_TenantGuid, _TagGuid).ConfigureAwait(false);

            AssertNotNull(tag, "Tag");
            AssertEqual(_TagGuid, tag.GUID, "Tag GUID");
        }

        private static async Task TestTagExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Tag.ExistsByGuid(_TenantGuid, _TagGuid).ConfigureAwait(false);
            AssertTrue(exists, "Tag exists");
        }

        private static async Task TestTagUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TagMetadata? tag = await _Client.Tag.ReadByGuid(_TenantGuid, _TagGuid).ConfigureAwait(false);
            AssertNotNull(tag, "Tag");

            tag.Value = "UpdatedValue";
            TagMetadata? updated = await _Client.Tag.Update(tag).ConfigureAwait(false);

            AssertNotNull(updated, "Updated tag");
            AssertEqual("UpdatedValue", updated.Value, "Updated Value");
        }

        private static async Task TestTagReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Client.Tag.ReadAllInTenant(_TenantGuid))
            {
                tags.Add(tag);
            }

            AssertTrue(tags.Count >= 3, "Tags count");
        }

        private static async Task TestTagReadAllInGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Client.Tag.ReadAllInGraph(_TenantGuid, _GraphGuid))
            {
                tags.Add(tag);
            }

            AssertTrue(tags.Count >= 3, "Tags count");
        }

        private static async Task TestTagReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Client.Tag.ReadMany(_TenantGuid, _GraphGuid, null, null, null, null))
            {
                tags.Add(tag);
            }

            AssertTrue(tags.Count >= 3, "Tags count");
        }

        private static async Task TestTagReadManyGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Client.Tag.ReadManyGraph(_TenantGuid, _GraphGuid))
            {
                tags.Add(tag);
            }

            // Should not throw
            AssertTrue(true, "Read graph tags");
        }

        private static async Task TestTagReadManyNode()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Client.Tag.ReadManyNode(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                tags.Add(tag);
            }

            AssertTrue(tags.Count >= 1, "Node tags count");
        }

        private static async Task TestTagReadManyEdge()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Client.Tag.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuid))
            {
                tags.Add(tag);
            }

            AssertTrue(tags.Count >= 1, "Edge tags count");
        }

        private static async Task TestTagReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _TagGuid };
            List<TagMetadata> tags = new List<TagMetadata>();

            await foreach (TagMetadata tag in _Client.Tag.ReadByGuids(_TenantGuid, guids))
            {
                tags.Add(tag);
            }

            AssertEqual(1, tags.Count, "Tags count");
        }

        private static async Task TestTagEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 10
            };

            EnumerationResult<TagMetadata>? result = await _Client.Tag.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        // ========================================
        // Vector Tests
        // ========================================

        private static async Task TestVectorCreate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            VectorMetadata vector = new VectorMetadata
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                NodeGUID = _Node1Guid,
                Model = "test-model",
                Dimensionality = 3,
                Content = "test content",
                Vectors = new List<float> { 1.0f, 2.0f, 3.0f }
            };

            VectorMetadata? created = await _Client.Vector.Create(vector).ConfigureAwait(false);

            AssertNotNull(created, "Created vector");
            AssertNotEmpty(created.GUID, "Vector GUID");
            AssertEqual(vector.Model, created.Model, "Vector Model");
            AssertEqual(vector.Dimensionality, created.Dimensionality, "Vector Dimensionality");
            AssertNotNull(created.Vectors, "Vector Vectors");
            AssertEqual(3, created.Vectors!.Count, "Vector Vectors count");

            _VectorGuid = created.GUID;
        }

        private static async Task TestVectorCreateMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>
            {
                new VectorMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    NodeGUID = _Node2Guid,
                    Model = "test-model",
                    Dimensionality = 3,
                    Content = "test content 2",
                    Vectors = new List<float> { 4.0f, 5.0f, 6.0f }
                },
                new VectorMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    EdgeGUID = _EdgeGuid,
                    Model = "test-model",
                    Dimensionality = 3,
                    Content = "edge content",
                    Vectors = new List<float> { 7.0f, 8.0f, 9.0f }
                }
            };

            List<VectorMetadata>? created = await _Client.Vector.CreateMany(_TenantGuid, vectors).ConfigureAwait(false);

            AssertNotNull(created, "Created vectors");
            AssertEqual(2, created.Count, "Vectors count");
        }

        private static async Task TestVectorReadByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            VectorMetadata? vector = await _Client.Vector.ReadByGuid(_TenantGuid, _VectorGuid).ConfigureAwait(false);

            AssertNotNull(vector, "Vector");
            AssertEqual(_VectorGuid, vector.GUID, "Vector GUID");
            AssertNotNull(vector.Vectors, "Vector Vectors");
        }

        private static async Task TestVectorExistsByGuid()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            bool exists = await _Client.Vector.ExistsByGuid(_TenantGuid, _VectorGuid).ConfigureAwait(false);
            AssertTrue(exists, "Vector exists");
        }

        private static async Task TestVectorUpdate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            VectorMetadata? vector = await _Client.Vector.ReadByGuid(_TenantGuid, _VectorGuid).ConfigureAwait(false);
            AssertNotNull(vector, "Vector");

            vector.Content = "Updated content";
            VectorMetadata? updated = await _Client.Vector.Update(vector).ConfigureAwait(false);

            AssertNotNull(updated, "Updated vector");
            AssertEqual("Updated content", updated.Content, "Updated Content");
        }

        private static async Task TestVectorReadAllInTenant()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Client.Vector.ReadAllInTenant(_TenantGuid))
            {
                vectors.Add(vector);
            }

            AssertTrue(vectors.Count >= 3, "Vectors count");
        }

        private static async Task TestVectorReadAllInGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Client.Vector.ReadAllInGraph(_TenantGuid, _GraphGuid))
            {
                vectors.Add(vector);
            }

            AssertTrue(vectors.Count >= 3, "Vectors count");
        }

        private static async Task TestVectorReadMany()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Client.Vector.ReadMany(_TenantGuid, _GraphGuid, null, null))
            {
                vectors.Add(vector);
            }

            AssertTrue(vectors.Count >= 3, "Vectors count");
        }

        private static async Task TestVectorReadManyGraph()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Client.Vector.ReadManyGraph(_TenantGuid, _GraphGuid))
            {
                vectors.Add(vector);
            }

            // Should not throw
            AssertTrue(true, "Read graph vectors");
        }

        private static async Task TestVectorReadManyNode()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Client.Vector.ReadManyNode(_TenantGuid, _GraphGuid, _Node1Guid))
            {
                vectors.Add(vector);
            }

            AssertTrue(vectors.Count >= 1, "Node vectors count");
        }

        private static async Task TestVectorReadManyEdge()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Client.Vector.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuid))
            {
                vectors.Add(vector);
            }

            AssertTrue(vectors.Count >= 1, "Edge vectors count");
        }

        private static async Task TestVectorReadByGuids()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            List<Guid> guids = new List<Guid> { _VectorGuid };
            List<VectorMetadata> vectors = new List<VectorMetadata>();

            await foreach (VectorMetadata vector in _Client.Vector.ReadByGuids(_TenantGuid, guids))
            {
                vectors.Add(vector);
            }

            AssertEqual(1, vectors.Count, "Vectors count");
        }

        private static async Task TestVectorEnumerate()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 10
            };

            EnumerationResult<VectorMetadata>? result = await _Client.Vector.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Enumeration result");
            AssertNotNull(result.Objects, "Results");
        }

        private static async Task TestVectorSearch()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            VectorSearchRequest request = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Embeddings = new List<float> { 1.0f, 2.0f, 3.0f },
                TopK = 5
            };

            List<VectorSearchResult> results = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _Client.Vector.Search(request))
            {
                results.Add(result);
            }

            // Should not throw
            AssertTrue(true, "Vector search");
        }

        // ========================================
        // Enumeration and Pagination Tests
        // ========================================

        private static async Task TestEnumerationTenantsSkip()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            // Create large set of tenants (50 total including existing)
            for (int i = 0; i < 50; i++)
            {
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = $"Enum Tenant {i}"
                };
                await _Client.Tenant.Create(tenant).ConfigureAwait(false);
            }

            int pageSize = 7;
            int pageNumber = 0;
            long totalRetrieved = 0;
            long expectedTotal = 0;

            EnumerationRequest request = new EnumerationRequest
            {
                MaxResults = pageSize
            };

            // Full enumeration with pagination
            while (true)
            {
                EnumerationResult<TenantMetadata>? result = await _Client.Tenant.Enumerate(request).ConfigureAwait(false);

                // Validate EVERY property
                AssertNotNull(result, $"Page {pageNumber} result");
                AssertTrue(result.Success, $"Page {pageNumber} Success");
                AssertNotNull(result.Timestamp, $"Page {pageNumber} Timestamp");
                AssertEqual(pageSize, result.MaxResults, $"Page {pageNumber} MaxResults");
                AssertNotNull(result.Objects, $"Page {pageNumber} Objects");

                // Store total on first page
                if (pageNumber == 0)
                {
                    expectedTotal = result.TotalRecords;
                    AssertTrue(expectedTotal > 50, "Total records should be > 50");
                }
                else
                {
                    AssertEqual(expectedTotal, result.TotalRecords, $"Page {pageNumber} TotalRecords consistency");
                }

                // Validate page size
                if (!result.EndOfResults)
                {
                    AssertEqual(pageSize, result.Objects.Count, $"Page {pageNumber} should have {pageSize} records");
                    AssertNotNull(result.ContinuationToken, $"Page {pageNumber} should have continuation token");
                }
                else
                {
                    AssertTrue(result.Objects.Count <= pageSize, $"Last page should have <= {pageSize} records");
                    AssertTrue(result.ContinuationToken == null, $"Last page should not have continuation token");
                }

                // Validate RecordsRemaining: TotalRecords = (totalRetrieved + currentPage + remaining)
                long expectedRemaining = expectedTotal - totalRetrieved - result.Objects.Count;
                AssertEqual(expectedRemaining, result.RecordsRemaining, $"Page {pageNumber} RecordsRemaining");

                // Cross-validate: totalRetrieved + current page + remaining should equal total
                long calculatedTotal = totalRetrieved + result.Objects.Count + result.RecordsRemaining;
                AssertEqual(expectedTotal, calculatedTotal, $"Page {pageNumber} total consistency check");

                // Validate EndOfResults
                if (expectedRemaining == 0)
                {
                    AssertTrue(result.EndOfResults, $"Page {pageNumber} should be end of results");
                }
                else
                {
                    AssertFalse(result.EndOfResults, $"Page {pageNumber} should not be end of results");
                }

                totalRetrieved += result.Objects.Count;
                pageNumber++;

                if (result.EndOfResults) break;
                if (pageNumber > 100) throw new Exception("Safety limit exceeded");

                request.ContinuationToken = result.ContinuationToken;
            }

            AssertEqual(expectedTotal, totalRetrieved, "Total retrieved should match TotalRecords");
        }

        private static async Task TestEnumerationTenantsContinuationToken()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            int pageSize = 3;
            int pageNumber = 0;
            long totalRetrieved = 0;
            long expectedTotal = 0;
            Guid? continuationToken = null;

            // Full enumeration using continuation token
            do
            {
                EnumerationRequest request = new EnumerationRequest
                {
                    MaxResults = pageSize,
                    ContinuationToken = continuationToken
                };

                EnumerationResult<TenantMetadata>? result = await _Client.Tenant.Enumerate(request).ConfigureAwait(false);

                // Validate EVERY property
                AssertNotNull(result, $"Page {pageNumber} result");
                AssertTrue(result.Success, $"Page {pageNumber} Success");
                AssertNotNull(result.Timestamp, $"Page {pageNumber} Timestamp");
                AssertEqual(pageSize, result.MaxResults, $"Page {pageNumber} MaxResults");
                AssertNotNull(result.Objects, $"Page {pageNumber} Objects");

                // Store total on first page
                if (pageNumber == 0)
                {
                    expectedTotal = result.TotalRecords;
                }
                else
                {
                    AssertEqual(expectedTotal, result.TotalRecords, $"Page {pageNumber} TotalRecords consistency");
                }

                // Validate page size
                if (!result.EndOfResults)
                {
                    AssertEqual(pageSize, result.Objects.Count, $"Page {pageNumber} count");
                    AssertNotNull(result.ContinuationToken, $"Page {pageNumber} continuation token");
                }
                else
                {
                    AssertTrue(result.Objects.Count <= pageSize, $"Last page count");
                    AssertTrue(result.ContinuationToken == null, $"Last page continuation token");
                }

                // Validate RecordsRemaining: TotalRecords = (totalRetrieved + currentPage + remaining)
                long expectedRemaining = expectedTotal - totalRetrieved - result.Objects.Count;
                AssertEqual(expectedRemaining, result.RecordsRemaining, $"Page {pageNumber} RecordsRemaining");

                // Cross-validate: totalRetrieved + current page + remaining should equal total
                long calculatedTotal = totalRetrieved + result.Objects.Count + result.RecordsRemaining;
                AssertEqual(expectedTotal, calculatedTotal, $"Page {pageNumber} total consistency check");

                // Validate EndOfResults
                AssertEqual(expectedRemaining == 0, result.EndOfResults, $"Page {pageNumber} EndOfResults");

                totalRetrieved += result.Objects.Count;
                continuationToken = result.ContinuationToken;
                pageNumber++;

                if (pageNumber > 100) throw new Exception("Safety limit exceeded");
            }
            while (continuationToken.HasValue);

            AssertEqual(expectedTotal, totalRetrieved, "Total retrieved matches TotalRecords");
        }

        private static async Task TestEnumerationGraphsPaginated()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            // Create large set of graphs (75 additional)
            for (int i = 0; i < 75; i++)
            {
                Graph graph = new Graph
                {
                    TenantGUID = _TenantGuid,
                    Name = $"Enum Graph {i}"
                };
                await _Client.Graph.Create(graph).ConfigureAwait(false);
            }

            int pageSize = 10;
            int pageNumber = 0;
            long totalRetrieved = 0;
            long expectedTotal = 0;

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                MaxResults = pageSize
            };

            // Full enumeration with pagination
            while (true)
            {
                EnumerationResult<Graph>? result = await _Client.Graph.Enumerate(request).ConfigureAwait(false);

                // Validate EVERY property
                AssertNotNull(result, $"Page {pageNumber} result");
                AssertTrue(result.Success, $"Page {pageNumber} Success");
                AssertNotNull(result.Timestamp, $"Page {pageNumber} Timestamp");
                AssertEqual(pageSize, result.MaxResults, $"Page {pageNumber} MaxResults");
                AssertNotNull(result.Objects, $"Page {pageNumber} Objects");

                // Store total on first page
                if (pageNumber == 0)
                {
                    expectedTotal = result.TotalRecords;
                    AssertTrue(expectedTotal >= 75, "Total records should be >= 75");
                }
                else
                {
                    AssertEqual(expectedTotal, result.TotalRecords, $"Page {pageNumber} TotalRecords consistency");
                }

                // Validate page size
                if (!result.EndOfResults)
                {
                    AssertEqual(pageSize, result.Objects.Count, $"Page {pageNumber} count");
                    AssertNotNull(result.ContinuationToken, $"Page {pageNumber} continuation token");
                }
                else
                {
                    AssertTrue(result.Objects.Count <= pageSize, $"Last page count");
                    AssertTrue(result.ContinuationToken == null, $"Last page continuation token");
                }

                // Validate RecordsRemaining: TotalRecords = (totalRetrieved + currentPage + remaining)
                long expectedRemaining = expectedTotal - totalRetrieved - result.Objects.Count;
                AssertEqual(expectedRemaining, result.RecordsRemaining, $"Page {pageNumber} RecordsRemaining");

                // Cross-validate: totalRetrieved + current page + remaining should equal total
                long calculatedTotal = totalRetrieved + result.Objects.Count + result.RecordsRemaining;
                AssertEqual(expectedTotal, calculatedTotal, $"Page {pageNumber} total consistency check");

                // Validate EndOfResults
                AssertEqual(expectedRemaining == 0, result.EndOfResults, $"Page {pageNumber} EndOfResults");

                // Validate each graph object
                foreach (Graph graph in result.Objects)
                {
                    AssertNotEmpty(graph.GUID, "Graph GUID");
                    AssertNotNull(graph.Name, "Graph Name");
                    AssertEqual(_TenantGuid, graph.TenantGUID, "Graph TenantGUID");
                }

                totalRetrieved += result.Objects.Count;
                pageNumber++;

                if (result.EndOfResults) break;
                if (pageNumber > 100) throw new Exception("Safety limit exceeded");

                request.ContinuationToken = result.ContinuationToken;
            }

            AssertEqual(expectedTotal, totalRetrieved, "Total retrieved matches TotalRecords");
        }

        private static async Task TestEnumerationNodesPaginated()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            // Create large set of nodes (100 additional)
            for (int i = 0; i < 100; i++)
            {
                Node node = new Node
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Name = $"Enum Node {i}"
                };
                await _Client.Node.Create(node).ConfigureAwait(false);
            }

            int pageSize = 8;
            int pageNumber = 0;
            long totalRetrieved = 0;
            long expectedTotal = 0;
            Guid? continuationToken = null;

            // Full enumeration using continuation token
            do
            {
                EnumerationRequest request = new EnumerationRequest
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    MaxResults = pageSize,
                    ContinuationToken = continuationToken
                };

                EnumerationResult<Node>? result = await _Client.Node.Enumerate(request).ConfigureAwait(false);

                // Validate EVERY property
                AssertNotNull(result, $"Page {pageNumber} result");
                AssertTrue(result.Success, $"Page {pageNumber} Success");
                AssertNotNull(result.Timestamp, $"Page {pageNumber} Timestamp");
                AssertEqual(pageSize, result.MaxResults, $"Page {pageNumber} MaxResults");
                AssertNotNull(result.Objects, $"Page {pageNumber} Objects");

                // Store total on first page
                if (pageNumber == 0)
                {
                    expectedTotal = result.TotalRecords;
                    AssertTrue(expectedTotal >= 100, "Total records should be >= 100");
                }
                else
                {
                    AssertEqual(expectedTotal, result.TotalRecords, $"Page {pageNumber} TotalRecords consistency");
                }

                // Validate page size
                if (!result.EndOfResults)
                {
                    AssertEqual(pageSize, result.Objects.Count, $"Page {pageNumber} count");
                    AssertNotNull(result.ContinuationToken, $"Page {pageNumber} continuation token");
                }
                else
                {
                    AssertTrue(result.Objects.Count <= pageSize, $"Last page count");
                    AssertTrue(result.ContinuationToken == null, $"Last page continuation token");
                }

                // Validate RecordsRemaining: TotalRecords = (totalRetrieved + currentPage + remaining)
                long expectedRemaining = expectedTotal - totalRetrieved - result.Objects.Count;
                AssertEqual(expectedRemaining, result.RecordsRemaining, $"Page {pageNumber} RecordsRemaining");

                // Cross-validate: totalRetrieved + current page + remaining should equal total
                long calculatedTotal = totalRetrieved + result.Objects.Count + result.RecordsRemaining;
                AssertEqual(expectedTotal, calculatedTotal, $"Page {pageNumber} total consistency check");

                // Validate EndOfResults
                AssertEqual(expectedRemaining == 0, result.EndOfResults, $"Page {pageNumber} EndOfResults");

                // Validate each node object
                foreach (Node node in result.Objects)
                {
                    AssertNotEmpty(node.GUID, "Node GUID");
                    AssertNotNull(node.Name, "Node Name");
                    AssertEqual(_TenantGuid, node.TenantGUID, "Node TenantGUID");
                    AssertEqual(_GraphGuid, node.GraphGUID, "Node GraphGUID");
                }

                totalRetrieved += result.Objects.Count;
                continuationToken = result.ContinuationToken;
                pageNumber++;

                if (pageNumber > 100) throw new Exception("Safety limit exceeded");
            }
            while (continuationToken.HasValue);

            AssertEqual(expectedTotal, totalRetrieved, "Total retrieved matches TotalRecords");
        }

        // ========================================
        // Assertion Helpers
        // ========================================

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message} (expected true, got false)");
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new Exception($"Assertion failed: {message} (expected false, got true)");
            }
        }

        private static void AssertNotNull(object? obj, string message)
        {
            if (obj == null)
            {
                throw new Exception($"Assertion failed: {message} (expected not null, got null)");
            }
        }

        private static void AssertNotEmpty(Guid guid, string message)
        {
            if (guid == Guid.Empty)
            {
                throw new Exception($"Assertion failed: {message} (expected not empty, got empty GUID)");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"Assertion failed: {message} (expected '{expected}', got '{actual}')");
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
        /// Name of the test.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the test passed.
        /// </summary>
        public bool Passed { get; set; } = false;

        /// <summary>
        /// Runtime in milliseconds.
        /// </summary>
        public long RuntimeMs { get; set; } = 0;

        /// <summary>
        /// Error message if failed.
        /// </summary>
        public string? ErrorMessage { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
