namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Query;
    using LiteGraph.Query.Ast;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using LiteGraph.Storage;
    using Microsoft.Data.Sqlite;
    using SyslogLogging;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        private const string PostgresqlTestConnectionStringEnvironmentVariable = "LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING";
        private const string MysqlTestConnectionStringEnvironmentVariable = "LITEGRAPH_TEST_MYSQL_CONNECTION_STRING";
        private const string SqlServerTestConnectionStringEnvironmentVariable = "LITEGRAPH_TEST_SQLSERVER_CONNECTION_STRING";

        private static TestSuiteDescriptor CreateImprovementFoundationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Improvements.Foundation",
                displayName: "Improvement plan foundation coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Factory.Sqlite",
                        displayName: "Storage factory creates SQLite repositories",
                        executeAsync: TestStorageFactorySqlite),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Postgresql.SqlTranslation",
                        displayName: "PostgreSQL provider translates SQLite-shaped provider SQL",
                        executeAsync: TestPostgresqlSqlTranslation),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Postgresql.TransactionClonePool",
                        displayName: "PostgreSQL transaction clones share the repository connection pool",
                        executeAsync: TestPostgresqlTransactionClonePool),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Migration.SqliteRoundTrip",
                        displayName: "Storage migration copies and verifies repository data",
                        executeAsync: TestStorageMigrationSqliteRoundTrip),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "RequestLifecycle.TimeoutSettings",
                        displayName: "Request lifecycle timeout settings and API errors are stable",
                        executeAsync: TestRequestLifecycleTimeoutSettings),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Provider.Postgresql",
                        displayName: "PostgreSQL provider suite",
                        executeAsync: ct => TestProviderBackendSuite(DatabaseTypeEnum.Postgresql, "PostgreSQL", PostgresqlTestConnectionStringEnvironmentVariable, ct),
                        skip: ShouldSkipProviderSuite(PostgresqlTestConnectionStringEnvironmentVariable),
                        skipReason: ProviderSuiteSkipReason("PostgreSQL", PostgresqlTestConnectionStringEnvironmentVariable)),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Provider.PostgresqlParity",
                        displayName: "PostgreSQL provider matches SQLite parity coverage",
                        executeAsync: ct => TestPostgresqlProviderParity(PostgresqlTestConnectionStringEnvironmentVariable, ct),
                        skip: ShouldSkipProviderSuite(PostgresqlTestConnectionStringEnvironmentVariable),
                        skipReason: ProviderSuiteSkipReason("PostgreSQL parity", PostgresqlTestConnectionStringEnvironmentVariable)),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.ProviderMatrix.PostgresqlConcurrency",
                        displayName: "PostgreSQL graph transaction concurrency matrix passes",
                        executeAsync: ct => TestPostgresqlTransactionConcurrencyMatrix(PostgresqlTestConnectionStringEnvironmentVariable, ct),
                        skip: ShouldSkipProviderSuite(PostgresqlTestConnectionStringEnvironmentVariable),
                        skipReason: ProviderSuiteSkipReason("PostgreSQL transaction concurrency", PostgresqlTestConnectionStringEnvironmentVariable)),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Provider.Mysql",
                        displayName: "MySQL provider suite",
                        executeAsync: ct => TestProviderBackendSuite(DatabaseTypeEnum.Mysql, "MySQL", MysqlTestConnectionStringEnvironmentVariable, ct),
                        skip: ShouldSkipProviderSuite(MysqlTestConnectionStringEnvironmentVariable),
                        skipReason: ProviderSuiteSkipReason("MySQL", MysqlTestConnectionStringEnvironmentVariable)),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Provider.SqlServer",
                        displayName: "SQL Server provider suite",
                        executeAsync: ct => TestProviderBackendSuite(DatabaseTypeEnum.SqlServer, "SQL Server", SqlServerTestConnectionStringEnvironmentVariable, ct),
                        skip: ShouldSkipProviderSuite(SqlServerTestConnectionStringEnvironmentVariable),
                        skipReason: ProviderSuiteSkipReason("SQL Server", SqlServerTestConnectionStringEnvironmentVariable)),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Sqlite.CommitRollback",
                        displayName: "SQLite graph transactions commit and roll back graph child mutations",
                        executeAsync: TestSqliteGraphTransactionCommitRollback),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Storage.Sqlite.PersistentConnectionFallback",
                        displayName: "SQLite repository falls back to the persistent WAL connection after transient open failures",
                        executeAsync: TestSqlitePersistentConnectionFallback),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Sqlite.PersistentConnectionFallback",
                        displayName: "SQLite graph transactions can fall back to the persistent WAL connection after transient open failures",
                        executeAsync: TestSqliteGraphTransactionPersistentConnectionFallback),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.Execute",
                        displayName: "Graph transaction client commits and rolls back operation lists",
                        executeAsync: TestGraphTransactionClientExecute),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.Builder",
                        displayName: "Graph transaction client builder creates executable requests",
                        executeAsync: TestGraphTransactionClientBuilder),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.AttachDetachUpsert",
                        displayName: "Graph transaction client attaches, detaches, and upserts graph child objects",
                        executeAsync: TestGraphTransactionClientAttachDetachUpsert),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.OperationMatrix",
                        displayName: "Graph transaction client creates, updates, deletes, and verifies graph child objects",
                        executeAsync: TestGraphTransactionClientOperationMatrix),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.MixedRollbackAndLimits",
                        displayName: "Graph transaction client enforces limits and rolls back mixed operations",
                        executeAsync: TestGraphTransactionClientMixedRollbackAndLimits),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.Cancellation",
                        displayName: "Graph transaction client honors cancellation before writes",
                        executeAsync: TestGraphTransactionClientCancellation),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.Timeout",
                        displayName: "Graph transaction client times out long-running operations and rolls back",
                        executeAsync: TestGraphTransactionClientTimeout),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Diagnostics.ProviderErrors",
                        displayName: "Graph transaction diagnostics classify provider concurrency errors",
                        executeAsync: TestGraphTransactionProviderErrorDiagnostics),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.ActiveGuard",
                        displayName: "Graph query mutations reject active ambient repository transactions",
                        executeAsync: TestGraphTransactionClientActiveGuard),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.ConcurrentQueue",
                        displayName: "Graph transaction client commits concurrent operation lists",
                        executeAsync: TestGraphTransactionClientConcurrentQueue),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Concurrent.SameGraphMixedObjects",
                        displayName: "Concurrent graph transactions commit mixed objects on the same graph",
                        executeAsync: TestGraphTransactionConcurrentSameGraphMixedObjects),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Concurrent.DifferentGraphs",
                        displayName: "Concurrent graph transactions commit independently across graphs",
                        executeAsync: TestGraphTransactionConcurrentDifferentGraphs),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Concurrent.CommitRollbackIsolation",
                        displayName: "Concurrent graph transaction rollbacks do not affect commits",
                        executeAsync: TestGraphTransactionConcurrentCommitRollbackIsolation),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Concurrent.AttachDetachMetadata",
                        displayName: "Concurrent graph transactions attach and detach metadata safely",
                        executeAsync: TestGraphTransactionConcurrentAttachDetachMetadata),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Concurrent.UpsertWaves",
                        displayName: "Concurrent graph transactions upsert object waves safely",
                        executeAsync: TestGraphTransactionConcurrentUpsertWaves),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Concurrent.VectorCommitRollback",
                        displayName: "Concurrent graph vector transactions isolate commit and rollback",
                        executeAsync: TestGraphTransactionConcurrentVectorCommitRollback),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Client.IsolatedParallelExecution",
                        displayName: "Graph transaction client runs isolated provider transactions in parallel",
                        executeAsync: TestGraphTransactionClientIsolatedParallelExecution),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Transactions.Query.IsolatedParallelExecution",
                        displayName: "Graph query mutations run isolated provider transactions in parallel",
                        executeAsync: TestGraphQueryIsolatedParallelExecution),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.Scoped.Persistence",
                        displayName: "Credential scopes and graph allow-lists persist",
                        executeAsync: TestScopedCredentialPersistence),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.QueryScopeClassification",
                        displayName: "Query authorization classifies read and mutation queries",
                        executeAsync: TestGraphQueryScopeClassification),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationService",
                        displayName: "Authorization service evaluates scoped credential policies",
                        executeAsync: TestAuthorizationServiceCredentialPolicies),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationPolicyDefinitions",
                        displayName: "Authorization policy definitions codify built-in roles and migration defaults",
                        executeAsync: TestAuthorizationPolicyDefinitions),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationPermissionMatrix",
                        displayName: "Authorization request, role, and assignment permission matrix is stable",
                        executeAsync: TestAuthorizationPermissionMatrix),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationRoleStorage",
                        displayName: "Authorization roles and assignments persist, search, update, page, and delete",
                        executeAsync: TestAuthorizationRoleStorage),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationMigrationCompatibility",
                        displayName: "Authorization migration preserves legacy credential access",
                        executeAsync: TestAuthorizationMigrationCompatibility),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationRoleEffectiveAccess",
                        displayName: "Authorization service evaluates stored role and credential-scope assignments",
                        executeAsync: TestAuthorizationRoleEffectiveAccess),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationCacheInvalidation",
                        displayName: "Authorization effective-permission cache invalidates on role and scope changes",
                        executeAsync: TestAuthorizationCacheInvalidation),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationRoles.RestManagement",
                        displayName: "REST and MCP authorization role and assignment management enforces RBAC",
                        executeAsync: TestAuthorizationRoleRestManagement),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationMcpBoundary",
                        displayName: "MCP tools honor REST RBAC decisions for scoped credentials",
                        executeAsync: TestAuthorizationMcpBoundary),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationAudit.Persistence",
                        displayName: "Authorization audit persists, searches, pages, and deletes denial records",
                        executeAsync: TestAuthorizationAuditPersistence),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Credentials.AuthorizationAudit.RestDeniedQuery",
                        displayName: "Denied REST graph query writes an authorization audit record",
                        executeAsync: TestAuthorizationAuditRestDeniedQuery),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.CreateAndMatch",
                        displayName: "Native graph query creates and matches nodes",
                        executeAsync: TestNativeQueryCreateAndMatch),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.MultiHopMatch",
                        displayName: "Native graph query matches fixed directed multi-hop paths",
                        executeAsync: TestNativeQueryMultiHopMatch),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.DataFilters",
                        displayName: "Native graph query filters node and edge data fields",
                        executeAsync: TestNativeQueryDataFilters),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.Lexer",
                        displayName: "Native graph query lexer tokenizes operators and reports source errors",
                        executeAsync: TestNativeQueryLexer),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.Parser",
                        displayName: "Native graph query parser reports supported syntax and errors",
                        executeAsync: TestNativeQueryParser),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.ParserExamples",
                        displayName: "Native graph query parser accepts documented examples and rejects unsupported syntax",
                        executeAsync: TestNativeQueryParserExamples),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.ParameterErrors",
                        displayName: "Native graph query reports parameter errors and honors cancellation",
                        executeAsync: TestNativeQueryParameterErrors),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.Planner",
                        displayName: "Native graph query planner classifies seeds, warnings, and mutations",
                        executeAsync: TestNativeQueryPlanner),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.CreateObjectsAndVectorSearch",
                        displayName: "Native graph query creates child objects and searches supplied vectors",
                        executeAsync: TestNativeQueryCreateObjectsAndVectorSearch),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.UpdateAndDelete",
                        displayName: "Native graph query updates and deletes matched nodes and edges",
                        executeAsync: TestNativeQueryUpdateAndDelete),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Query.UpdateAndDeleteMetadata",
                        displayName: "Native graph query updates and deletes matched labels, tags, and vectors",
                        executeAsync: TestNativeQueryUpdateAndDeleteMetadata),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.Prometheus",
                        displayName: "Observability renders Prometheus metrics",
                        executeAsync: TestObservabilityPrometheus),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.MetricsEndpoint",
                        displayName: "Metrics endpoint is reachable without authentication",
                        executeAsync: TestMetricsEndpoint),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.GrafanaDashboard",
                        displayName: "Grafana dashboard template covers exported metrics",
                        executeAsync: TestGrafanaDashboardTemplate),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.Tracing",
                        displayName: "Observability emits OpenTelemetry-compatible activities",
                        executeAsync: TestObservabilityTracing),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.QueryPhaseTracing",
                        displayName: "Native graph query emits phase, vector search, and vector index activities",
                        executeAsync: TestObservabilityQueryPhaseTracing),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.OtlpSettings",
                        displayName: "Observability configures built-in OTLP exporters",
                        executeAsync: TestObservabilityOtlpSettings),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.QueryProfileTimings",
                        displayName: "Native graph query profiles repository, vector, and transaction timings",
                        executeAsync: TestObservabilityQueryProfileTimings),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.RestQueryProfile",
                        displayName: "REST graph query profiles authorization and serialization timings",
                        executeAsync: TestObservabilityRestQueryProfile),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.RequestHistoryCorrelation",
                        displayName: "Request history persists request, correlation, and trace identifiers",
                        executeAsync: TestRequestHistoryCorrelation),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.RequestHistoryRedaction",
                        displayName: "Request history redacts sensitive headers and truncates bodies",
                        executeAsync: TestRequestHistoryRedaction),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.OperationalLogRedaction",
                        displayName: "Operational logging redacts sensitive request values",
                        executeAsync: TestOperationalLogRedaction),
                    new TestCaseDescriptor(
                        suiteId: "Improvements.Foundation",
                        caseId: "Observability.JsonOperationalLogs",
                        displayName: "Operational request logging can emit JSON records",
                        executeAsync: TestOperationalJsonLogFormatting)
                });
        }

        private static Task TestStorageFactorySqlite(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string filename = "test-improvements-storage.db";
            DeleteFileIfExists(filename);

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Type = DatabaseTypeEnum.Sqlite,
                Filename = filename
            }))
            {
                AssertTrue(repo is SqliteGraphRepository, "Factory returns SQLite repository");
                repo.InitializeRepository();
            }

            using (GraphRepositoryBase postgresqlRepo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Type = DatabaseTypeEnum.Postgresql,
                ConnectionString = "Host=localhost;Database=litegraph;Username=litegraph;Password=litegraph"
            }))
            {
                AssertTrue(postgresqlRepo is PostgresqlGraphRepository, "Factory returns PostgreSQL repository");
            }

            AssertUnsupportedProvider(DatabaseTypeEnum.Mysql, "MySQL");
            AssertUnsupportedProvider(DatabaseTypeEnum.SqlServer, "SQL Server");

            DeleteFileIfExists(filename);
            return Task.CompletedTask;
        }

        private static Task TestPostgresqlSqlTranslation(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string tableSql = PostgresqlSqlTranslator.Translate(
                "CREATE TABLE IF NOT EXISTS 'vectors' (guid VARCHAR(64) NOT NULL UNIQUE, embeddings BLOB);",
                "litegraph");
            AssertTrue(tableSql.Contains("CREATE TABLE IF NOT EXISTS \"litegraph\".\"vectors\"", StringComparison.Ordinal), "PostgreSQL schema-qualifies quoted table names");
            AssertTrue(tableSql.Contains("guid VARCHAR(64) PRIMARY KEY", StringComparison.Ordinal), "PostgreSQL promotes GUID to primary key for grouped reads");
            AssertTrue(tableSql.Contains("embeddings BYTEA", StringComparison.Ordinal), "PostgreSQL translates BLOB columns to BYTEA");

            string querySql = PostgresqlSqlTranslator.Translate(
                "SELECT * FROM 'nodes' WHERE json_extract(nodes.data, '$.age') >= 30;",
                "litegraph");
            AssertTrue(querySql.Contains("FROM \"litegraph\".\"nodes\"", StringComparison.Ordinal), "PostgreSQL schema-qualifies read tables");
            AssertTrue(querySql.Contains("((nodes.data::jsonb #>> '{age}')::DOUBLE PRECISION) >= 30", StringComparison.Ordinal), "PostgreSQL translates numeric JSON data field filters");

            string blobSql = PostgresqlSqlTranslator.Translate(
                "INSERT INTO 'vectors' VALUES (X'0A0B');",
                "litegraph");
            AssertTrue(blobSql.Contains("decode('0A0B', 'hex')", StringComparison.Ordinal), "PostgreSQL translates SQLite blob literals");

            return Task.CompletedTask;
        }

        private static Task TestPostgresqlTransactionClonePool(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (PostgresqlGraphRepository repo = new PostgresqlGraphRepository(new DatabaseSettings
            {
                Type = DatabaseTypeEnum.Postgresql,
                ConnectionString = "Host=localhost;Database=litegraph;Username=litegraph;Password=litegraph",
                Schema = "clone_pool_test",
                MaxConnections = 7
            }))
            using (GraphRepositoryBase clone = repo.CreateIsolatedTransactionRepository())
            {
                AssertTrue(clone is PostgresqlGraphRepository, "PostgreSQL transaction clone type");
                AssertEqual(repo.Schema, ((PostgresqlGraphRepository)clone).Schema, "PostgreSQL transaction clone preserves schema");

                FieldInfo? dataSourceField = typeof(PostgresqlGraphRepository).GetField("_DataSource", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo? ownsDataSourceField = typeof(PostgresqlGraphRepository).GetField("_OwnsDataSource", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertNotNull(dataSourceField, "PostgreSQL data source field exists");
                AssertNotNull(ownsDataSourceField, "PostgreSQL data source ownership field exists");
                if (dataSourceField == null || ownsDataSourceField == null) throw new InvalidOperationException("PostgreSQL clone pool fields were not found.");

                object? parentDataSource = dataSourceField.GetValue(repo);
                object? cloneDataSource = dataSourceField.GetValue(clone);
                object? parentOwnsDataSource = ownsDataSourceField.GetValue(repo);
                object? cloneOwnsDataSource = ownsDataSourceField.GetValue(clone);
                AssertNotNull(parentDataSource, "PostgreSQL parent data source exists");
                AssertNotNull(cloneDataSource, "PostgreSQL clone data source exists");
                AssertTrue(Object.ReferenceEquals(parentDataSource, cloneDataSource), "PostgreSQL transaction clone shares parent data source");
                AssertTrue(parentOwnsDataSource is bool parentOwns && parentOwns, "PostgreSQL parent owns data source");
                AssertTrue(cloneOwnsDataSource is bool cloneOwns && !cloneOwns, "PostgreSQL transaction clone does not own data source");
            }

            return Task.CompletedTask;
        }

        private static async Task TestStorageMigrationSqliteRoundTrip(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string suffix = Guid.NewGuid().ToString("N");
            string sourceFilename = "test-storage-migration-source-" + suffix + ".db";
            string destinationFilename = "test-storage-migration-destination-" + suffix + ".db";
            DeleteFileIfExists(sourceFilename);
            DeleteFileIfExists(destinationFilename);

            try
            {
                {
                    await using GraphRepositoryBase source = GraphRepositoryFactory.Create(new DatabaseSettings
                    {
                        Type = DatabaseTypeEnum.Sqlite,
                        Filename = sourceFilename
                    });

                    await using GraphRepositoryBase destination = GraphRepositoryFactory.Create(new DatabaseSettings
                    {
                        Type = DatabaseTypeEnum.Sqlite,
                        Filename = destinationFilename
                    });

                    await source.InitializeRepositoryAsync(cancellationToken).ConfigureAwait(false);
                    await destination.InitializeRepositoryAsync(cancellationToken).ConfigureAwait(false);

                    Guid tenantGuid;
                    Guid graphGuid;
                    Guid fromGuid;
                    Guid edgeGuid;
                    Guid labelGuid;
                    Guid tagGuid;
                    Guid vectorGuid;
                    Guid userGuid;
                    Guid credentialGuid;

                    using (LiteGraphClient client = new LiteGraphClient(source, null, null, null, false))
                    {
                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Migration Tenant " + suffix }, cancellationToken).ConfigureAwait(false);
                    tenantGuid = tenant.GUID;

                    UserMaster user = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "Migration",
                        LastName = "User",
                        Email = "migration-" + suffix + "@example.com",
                        Password = "password"
                    }, cancellationToken).ConfigureAwait(false);
                    userGuid = user.GUID;

                    Credential credential = await client.Credential.Create(new Credential
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        Name = "Migration Credential " + suffix,
                        Scopes = new List<string> { "read", "write" }
                    }, cancellationToken).ConfigureAwait(false);
                    credentialGuid = credential.GUID;

                    Graph graph = await client.Graph.Create(new Graph
                    {
                        TenantGUID = tenant.GUID,
                        Name = "Migration Graph " + suffix,
                        Data = new Dictionary<string, object> { ["kind"] = "migration" }
                    }, cancellationToken).ConfigureAwait(false);
                    graphGuid = graph.GUID;

                    Node from = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "from-" + suffix,
                        Data = new Dictionary<string, object> { ["name"] = "Ada", ["age"] = 42 }
                    }, cancellationToken).ConfigureAwait(false);
                    fromGuid = from.GUID;

                    Node to = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "to-" + suffix,
                        Data = new Dictionary<string, object> { ["name"] = "Grace", ["age"] = 37 }
                    }, cancellationToken).ConfigureAwait(false);

                    Edge edge = await client.Edge.Create(new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        From = from.GUID,
                        To = to.GUID,
                        Name = "edge-" + suffix,
                        Cost = 7,
                        Data = new Dictionary<string, object> { ["relationship"] = "knows" }
                    }, cancellationToken).ConfigureAwait(false);
                    edgeGuid = edge.GUID;

                    LabelMetadata label = await client.Label.Create(new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = from.GUID,
                        Label = "Person"
                    }, cancellationToken).ConfigureAwait(false);
                    labelGuid = label.GUID;

                    TagMetadata tag = await client.Tag.Create(new TagMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        EdgeGUID = edge.GUID,
                        Key = "source",
                        Value = "migration"
                    }, cancellationToken).ConfigureAwait(false);
                    tagGuid = tag.GUID;

                    VectorMetadata vector = await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = from.GUID,
                        Model = "test",
                        Dimensionality = 3,
                        Content = "migration vector",
                        Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
                    }, cancellationToken).ConfigureAwait(false);
                    vectorGuid = vector.GUID;

                    AuthorizationRole role = await client.AuthorizationRoles.CreateRole(new AuthorizationRole
                    {
                        TenantGUID = tenant.GUID,
                        Name = "migration-role-" + suffix,
                        DisplayName = "Migration Role",
                        Description = "Migration role",
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Read, AuthorizationPermissionEnum.Write },
                        ResourceTypes = new List<AuthorizationResourceTypeEnum> { AuthorizationResourceTypeEnum.Graph, AuthorizationResourceTypeEnum.Node }
                    }, cancellationToken).ConfigureAwait(false);

                    await client.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        RoleGUID = role.GUID,
                        RoleName = role.Name,
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        GraphGUID = graph.GUID
                    }, cancellationToken).ConfigureAwait(false);

                    await client.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                    {
                        TenantGUID = tenant.GUID,
                        CredentialGUID = credential.GUID,
                        RoleGUID = role.GUID,
                        RoleName = role.Name,
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        GraphGUID = graph.GUID,
                        Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Read },
                        ResourceTypes = new List<AuthorizationResourceTypeEnum> { AuthorizationResourceTypeEnum.Graph, AuthorizationResourceTypeEnum.Node }
                    }, cancellationToken).ConfigureAwait(false);
                }

                    StorageMigrationResult migration = await StorageMigrationManager.MigrateAsync(source, destination, true, 10, cancellationToken).ConfigureAwait(false);
                AssertTrue(migration.Succeeded, "Storage migration succeeds");
                AssertTrue(migration.Verification.Succeeded, "Storage migration verification succeeds");
                AssertEqual(1, migration.Migrated.Tenants, "Storage migration copies tenants");
                AssertEqual(1, migration.Migrated.Users, "Storage migration copies users");
                AssertEqual(1, migration.Migrated.Credentials, "Storage migration copies credentials");
                AssertEqual(1, migration.Migrated.Graphs, "Storage migration copies graphs");
                AssertEqual(2, migration.Migrated.Nodes, "Storage migration copies nodes");
                AssertEqual(1, migration.Migrated.Edges, "Storage migration copies edges");
                AssertEqual(1, migration.Migrated.Labels, "Storage migration copies labels");
                AssertEqual(1, migration.Migrated.Tags, "Storage migration copies tags");
                AssertEqual(1, migration.Migrated.Vectors, "Storage migration copies vectors");
                AssertEqual(1, migration.Migrated.AuthorizationRoles, "Storage migration copies custom authorization roles");
                AssertEqual(1, migration.Migrated.UserRoleAssignments, "Storage migration copies user role assignments");
                AssertEqual(1, migration.Migrated.CredentialScopeAssignments, "Storage migration copies credential scope assignments");

                AssertNotNull(await destination.Tenant.ReadByGuid(tenantGuid, cancellationToken).ConfigureAwait(false), "Migrated tenant can be read");
                AssertNotNull(await destination.User.ReadByGuid(tenantGuid, userGuid, cancellationToken).ConfigureAwait(false), "Migrated user can be read");
                AssertNotNull(await destination.Credential.ReadByGuid(tenantGuid, credentialGuid, cancellationToken).ConfigureAwait(false), "Migrated credential can be read");
                AssertNotNull(await destination.Graph.ReadByGuid(tenantGuid, graphGuid, cancellationToken).ConfigureAwait(false), "Migrated graph can be read");
                Node migratedNode = await destination.Node.ReadByGuid(tenantGuid, fromGuid, cancellationToken).ConfigureAwait(false);
                AssertNotNull(migratedNode, "Migrated node can be read");
                if (migratedNode == null) throw new InvalidOperationException("Migrated node was not found.");
                    string migratedNodeData = migratedNode.Data?.ToString() ?? String.Empty;
                    AssertTrue(migratedNodeData.Contains("Ada", StringComparison.Ordinal), "Migrated node data is preserved");
                AssertNotNull(await destination.Edge.ReadByGuid(tenantGuid, edgeGuid, cancellationToken).ConfigureAwait(false), "Migrated edge can be read");
                AssertNotNull(await destination.Label.ReadByGuid(tenantGuid, labelGuid, cancellationToken).ConfigureAwait(false), "Migrated label can be read");
                AssertNotNull(await destination.Tag.ReadByGuid(tenantGuid, tagGuid, cancellationToken).ConfigureAwait(false), "Migrated tag can be read");
                VectorMetadata migratedVector = await destination.Vector.ReadByGuid(tenantGuid, vectorGuid, cancellationToken).ConfigureAwait(false);
                AssertNotNull(migratedVector, "Migrated vector can be read");
                if (migratedVector == null) throw new InvalidOperationException("Migrated vector was not found.");
                AssertEqual(3, migratedVector.Vectors.Count, "Migrated vector embedding dimensionality is preserved");

                StorageVerificationResult verification = await StorageMigrationManager.VerifyAsync(source, destination, 10, cancellationToken).ConfigureAwait(false);
                AssertTrue(verification.Succeeded, "Storage verification can be run independently");

                    await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                DeleteFileIfExists(sourceFilename);
                DeleteFileIfExists(destinationFilename);
            }
        }

        private static Task TestRequestLifecycleTimeoutSettings(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Settings settings = new Settings();
            AssertEqual(60, settings.RequestTimeoutSeconds, "Default request timeout");

            settings.RequestTimeoutSeconds = 1;
            AssertEqual(1, settings.RequestTimeoutSeconds, "Minimum request timeout");

            settings.RequestTimeoutSeconds = 3600;
            AssertEqual(3600, settings.RequestTimeoutSeconds, "Maximum request timeout");

            bool rejectedZero = false;
            try
            {
                settings.RequestTimeoutSeconds = 0;
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedZero = true;
            }

            AssertTrue(rejectedZero, "Request timeout rejects zero");

            bool rejectedTooLarge = false;
            try
            {
                settings.RequestTimeoutSeconds = 3601;
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedTooLarge = true;
            }

            AssertTrue(rejectedTooLarge, "Request timeout rejects values above one hour");

            ApiErrorResponse timeout = new ApiErrorResponse(ApiErrorEnum.RequestTimeout);
            AssertEqual(408, timeout.StatusCode, "Request timeout HTTP status");
            AssertTrue(timeout.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase), "Request timeout message");

            AssertEqual(1000, settings.LiteGraph.Transactions.MaxOperations, "Default transaction max operations");
            AssertEqual(60, settings.LiteGraph.Transactions.MaxTimeoutSeconds, "Default transaction max timeout");

            settings.LiteGraph.Transactions.MaxOperations = 1;
            AssertEqual(1, settings.LiteGraph.Transactions.MaxOperations, "Minimum transaction max operations");

            settings.LiteGraph.Transactions.MaxOperations = 10000;
            AssertEqual(10000, settings.LiteGraph.Transactions.MaxOperations, "Maximum transaction max operations");

            settings.LiteGraph.Transactions.MaxTimeoutSeconds = 1;
            AssertEqual(1, settings.LiteGraph.Transactions.MaxTimeoutSeconds, "Minimum transaction max timeout");

            settings.LiteGraph.Transactions.MaxTimeoutSeconds = 3600;
            AssertEqual(3600, settings.LiteGraph.Transactions.MaxTimeoutSeconds, "Maximum transaction max timeout");

            bool rejectedTransactionOperationZero = false;
            try
            {
                settings.LiteGraph.Transactions.MaxOperations = 0;
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedTransactionOperationZero = true;
            }

            AssertTrue(rejectedTransactionOperationZero, "Transaction max operations rejects zero");

            bool rejectedTransactionTimeoutTooLarge = false;
            try
            {
                settings.LiteGraph.Transactions.MaxTimeoutSeconds = 3601;
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedTransactionTimeoutTooLarge = true;
            }

            AssertTrue(rejectedTransactionTimeoutTooLarge, "Transaction max timeout rejects values above one hour");

            Type? constantsType = typeof(Settings).Assembly.GetType("LiteGraph.Server.Classes.Constants");
            AssertNotNull(constantsType, "Server constants type exists");
            if (constantsType == null) throw new InvalidOperationException("Server constants type was not found.");

            FieldInfo? envField = constantsType.GetField("RequestTimeoutEnvironmentVariable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(envField, "Request timeout environment variable constant exists");
            if (envField == null) throw new InvalidOperationException("Request timeout environment variable constant was not found.");

            object? envValue = envField.GetValue(null);
            AssertEqual("LITEGRAPH_REQUEST_TIMEOUT_SECONDS", envValue as string, "Request timeout environment variable name");

            FieldInfo? transactionMaxOperationsEnvField = constantsType.GetField("TransactionMaxOperationsEnvironmentVariable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(transactionMaxOperationsEnvField, "Transaction max operations environment variable constant exists");
            if (transactionMaxOperationsEnvField == null) throw new InvalidOperationException("Transaction max operations environment variable constant was not found.");
            AssertEqual("LITEGRAPH_TRANSACTION_MAX_OPERATIONS", transactionMaxOperationsEnvField.GetValue(null) as string, "Transaction max operations environment variable name");

            FieldInfo? transactionMaxTimeoutEnvField = constantsType.GetField("TransactionMaxTimeoutEnvironmentVariable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(transactionMaxTimeoutEnvField, "Transaction max timeout environment variable constant exists");
            if (transactionMaxTimeoutEnvField == null) throw new InvalidOperationException("Transaction max timeout environment variable constant was not found.");
            AssertEqual("LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS", transactionMaxTimeoutEnvField.GetValue(null) as string, "Transaction max timeout environment variable name");

            FieldInfo? createDefaultRecordsEnvField = constantsType.GetField("CreateDefaultRecordsEnvironmentVariable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(createDefaultRecordsEnvField, "Create default records environment variable constant exists");
            if (createDefaultRecordsEnvField == null) throw new InvalidOperationException("Create default records environment variable constant was not found.");
            AssertEqual("LITEGRAPH_CREATE_DEFAULT_RECORDS", createDefaultRecordsEnvField.GetValue(null) as string, "Create default records environment variable name");

            FieldInfo? initOnlyEnvField = constantsType.GetField("InitOnlyEnvironmentVariable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(initOnlyEnvField, "Init-only environment variable constant exists");
            if (initOnlyEnvField == null) throw new InvalidOperationException("Init-only environment variable constant was not found.");
            AssertEqual("LITEGRAPH_INIT_ONLY", initOnlyEnvField.GetValue(null) as string, "Init-only environment variable name");

            Type? handlerType = typeof(Settings).Assembly.GetType("LiteGraph.Server.API.REST.RestServiceHandler");
            AssertNotNull(handlerType, "REST service handler type exists");
            if (handlerType == null) throw new InvalidOperationException("REST service handler type was not found.");

            MethodInfo? limitMethod = handlerType.GetMethod("ApplyTransactionServerLimits", BindingFlags.Static | BindingFlags.NonPublic);
            AssertNotNull(limitMethod, "Transaction server limit helper exists");
            if (limitMethod == null) throw new InvalidOperationException("Transaction server limit helper was not found.");

            TransactionRequest transactionRequest = new TransactionRequest
            {
                MaxOperations = 100,
                TimeoutSeconds = 120
            };
            limitMethod.Invoke(null, new object[]
            {
                transactionRequest,
                new TransactionSettings
                {
                    MaxOperations = 10,
                    MaxTimeoutSeconds = 30
                }
            });
            AssertEqual(10, transactionRequest.MaxOperations, "Transaction max operations is capped by server settings");
            AssertEqual(30, transactionRequest.TimeoutSeconds, "Transaction timeout is capped by server settings");

            string restHandler = File.ReadAllText(ResolveRepositoryFile("src", "LiteGraph.Server", "API", "REST", "RestServiceHandler.cs"));
            string authorizationHandler = File.ReadAllText(ResolveRepositoryFile("src", "LiteGraph.Server", "API", "REST", "RestServiceHandler.Authorization.cs"));
            AssertFalse(restHandler.Contains("CancellationToken.None", StringComparison.Ordinal), "REST service handler passes request cancellation tokens");
            AssertFalse(authorizationHandler.Contains("CancellationToken.None", StringComparison.Ordinal), "REST authorization handler passes request cancellation tokens");

            return Task.CompletedTask;
        }

        private static async Task TestProviderBackendSuite(DatabaseTypeEnum type, string providerName, string connectionStringEnvironmentVariable, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? connectionString = Environment.GetEnvironmentVariable(connectionStringEnvironmentVariable);
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(providerName + " provider suite requires " + connectionStringEnvironmentVariable + ".");

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Type = type,
                ConnectionString = connectionString,
                Schema = "litegraph"
            }))
            {
                AssertFalse(repo is UnsupportedGraphRepository, providerName + " provider implementation is required when the provider suite is enabled.");
                repo.InitializeRepository();

                if (type == DatabaseTypeEnum.Postgresql)
                    await RunPostgresqlCoreStorageSmoke(repo, connectionString, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task RunPostgresqlCoreStorageSmoke(GraphRepositoryBase repo, string connectionString, CancellationToken cancellationToken)
        {
            using (LiteGraphClient client = new LiteGraphClient(repo, null, null, null, false))
            {
                string suffix = Guid.NewGuid().ToString("N");
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "PostgreSQL Test " + suffix }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "PostgreSQL Graph " + suffix }, cancellationToken).ConfigureAwait(false);

                Node from = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "from-" + suffix,
                    Data = new Dictionary<string, object> { ["kind"] = "person", ["age"] = 42, ["active"] = true }
                }, cancellationToken).ConfigureAwait(false);
                Node to = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "to-" + suffix,
                    Data = new Dictionary<string, object> { ["kind"] = "person", ["age"] = 21, ["active"] = false }
                }, cancellationToken).ConfigureAwait(false);
                Edge edge = await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = from.GUID, To = to.GUID, Name = "edge-" + suffix, Cost = 3 }, cancellationToken).ConfigureAwait(false);
                LabelMetadata label = await client.Label.Create(new LabelMetadata { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, NodeGUID = from.GUID, Label = "postgresql-smoke" }, cancellationToken).ConfigureAwait(false);
                TagMetadata tag = await client.Tag.Create(new TagMetadata { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, EdgeGUID = edge.GUID, Key = "provider", Value = "postgresql" }, cancellationToken).ConfigureAwait(false);
                VectorMetadata vector = await client.Vector.Create(new VectorMetadata { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, NodeGUID = from.GUID, Model = "test", Dimensionality = 3, Content = "postgresql", Vectors = new List<float> { 0.1f, 0.2f, 0.3f } }, cancellationToken).ConfigureAwait(false);

                await VerifyBatchExistenceEmptySiblingFiltersAsync(
                    from.GUID,
                    edge.GUID,
                    vector.GUID,
                    from.GUID,
                    to.GUID,
                    request => client.Batch.Existence(tenant.GUID, graph.GUID, request, cancellationToken)).ConfigureAwait(false);
                await VerifyBatchExistenceLargePayloadAsync(
                    from.GUID,
                    edge.GUID,
                    vector.GUID,
                    from.GUID,
                    to.GUID,
                    request => client.Batch.Existence(tenant.GUID, graph.GUID, request, cancellationToken)).ConfigureAwait(false);

                AssertNotNull(await client.Tenant.ReadByGuid(tenant.GUID, cancellationToken).ConfigureAwait(false), "PostgreSQL reads tenant by GUID");
                AssertNotNull(await client.Graph.ReadByGuid(tenant.GUID, graph.GUID, token: cancellationToken).ConfigureAwait(false), "PostgreSQL reads graph by GUID");
                AssertNotNull(await client.Node.ReadByGuid(tenant.GUID, graph.GUID, from.GUID, token: cancellationToken).ConfigureAwait(false), "PostgreSQL reads node by GUID");
                AssertNotNull(await client.Edge.ReadByGuid(tenant.GUID, graph.GUID, edge.GUID, token: cancellationToken).ConfigureAwait(false), "PostgreSQL reads edge by GUID");
                AssertNotNull(await client.Label.ReadByGuid(tenant.GUID, label.GUID, cancellationToken).ConfigureAwait(false), "PostgreSQL reads label by GUID");
                AssertNotNull(await client.Tag.ReadByGuid(tenant.GUID, tag.GUID, cancellationToken).ConfigureAwait(false), "PostgreSQL reads tag by GUID");
                AssertNotNull(await client.Vector.ReadByGuid(tenant.GUID, vector.GUID, cancellationToken).ConfigureAwait(false), "PostgreSQL reads vector by GUID");

                List<Node> numericFiltered = new List<Node>();
                await foreach (Node node in client.Node.ReadMany(
                    tenant.GUID,
                    graph.GUID,
                    nodeFilter: new Expr("age", OperatorEnum.GreaterThanOrEqualTo, 40),
                    token: cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    numericFiltered.Add(node);
                }
                AssertTrue(numericFiltered.Any(node => node.GUID == from.GUID), "PostgreSQL filters JSON numeric data fields");
                AssertFalse(numericFiltered.Any(node => node.GUID == to.GUID), "PostgreSQL JSON numeric filters exclude non-matching nodes");

                List<Node> booleanFiltered = new List<Node>();
                await foreach (Node node in client.Node.ReadMany(
                    tenant.GUID,
                    graph.GUID,
                    nodeFilter: new Expr("active", OperatorEnum.Equals, true),
                    token: cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    booleanFiltered.Add(node);
                }
                AssertTrue(booleanFiltered.Any(node => node.GUID == from.GUID), "PostgreSQL filters JSON boolean data fields");
                AssertFalse(booleanFiltered.Any(node => node.GUID == to.GUID), "PostgreSQL JSON boolean filters exclude non-matching nodes");

                GraphQueryResult postgresqlQuery = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n) WHERE n.data.age >= 40 RETURN n LIMIT 5"
                    },
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(postgresqlQuery.Nodes.Any(node => node.GUID == from.GUID), "PostgreSQL native query filters JSON numeric data fields");
                AssertFalse(postgresqlQuery.Nodes.Any(node => node.GUID == to.GUID), "PostgreSQL native query excludes non-matching JSON numeric data fields");

                GraphQueryResult postgresqlPathQuery = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a)-[path*1..1]->(b) WHERE a.guid = $from RETURN a, path, b LIMIT 5",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "from", from.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(1, postgresqlPathQuery.RowCount, "PostgreSQL native query executes bounded variable-length path");
                AssertEqual(edge.GUID, ((IEnumerable<Edge>)postgresqlPathQuery.Rows[0]["path"]).First().GUID, "PostgreSQL native query returns path edge");

                List<Task<Node>> concurrentCreates = Enumerable.Range(0, 8)
                    .Select(index => client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "concurrent-" + index + "-" + suffix,
                        Data = new Dictionary<string, object> { ["batch"] = "postgresql-concurrency", ["index"] = index }
                    }, cancellationToken))
                    .ToList();
                Node[] concurrentNodes = await Task.WhenAll(concurrentCreates).ConfigureAwait(false);
                foreach (Node node in concurrentNodes)
                    AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node.GUID, cancellationToken).ConfigureAwait(false), "PostgreSQL concurrent writer node exists");

                Guid rolledBackNodeGuid = Guid.NewGuid();
                await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                await repo.Node.Create(new Node { GUID = rolledBackNodeGuid, TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "rollback-" + suffix }, cancellationToken).ConfigureAwait(false);
                await repo.RollbackGraphTransaction(cancellationToken).ConfigureAwait(false);
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, rolledBackNodeGuid, cancellationToken).ConfigureAwait(false), "PostgreSQL graph transaction rolls back node create");

                Guid committedNodeGuid = Guid.NewGuid();
                await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                await repo.Node.Create(new Node { GUID = committedNodeGuid, TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "commit-" + suffix }, cancellationToken).ConfigureAwait(false);
                await repo.CommitGraphTransaction(cancellationToken).ConfigureAwait(false);
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, committedNodeGuid, cancellationToken).ConfigureAwait(false), "PostgreSQL graph transaction commits node create");

                Guid concurrentTransactionNode1Guid = Guid.NewGuid();
                Guid concurrentTransactionNode2Guid = Guid.NewGuid();
                using (GraphRepositoryBase repo1 = GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = "litegraph"
                }))
                using (GraphRepositoryBase repo2 = GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = "litegraph"
                }))
                using (LiteGraphClient client1 = new LiteGraphClient(repo1, null, null, null, false))
                using (LiteGraphClient client2 = new LiteGraphClient(repo2, null, null, null, false))
                {
                    repo1.InitializeRepository();
                    repo2.InitializeRepository();

                    Task<TransactionResult> transaction1 = client1.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new TransactionRequest
                        {
                            Operations = new List<TransactionOperation>
                            {
                                new TransactionOperation
                                {
                                    OperationType = TransactionOperationTypeEnum.Create,
                                    ObjectType = TransactionObjectTypeEnum.Node,
                                    Payload = new Node { GUID = concurrentTransactionNode1Guid, Name = "concurrent-transaction-1-" + suffix }
                                }
                            }
                        },
                        cancellationToken);

                    Task<TransactionResult> transaction2 = client2.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new TransactionRequest
                        {
                            Operations = new List<TransactionOperation>
                            {
                                new TransactionOperation
                                {
                                    OperationType = TransactionOperationTypeEnum.Create,
                                    ObjectType = TransactionObjectTypeEnum.Node,
                                    Payload = new Node { GUID = concurrentTransactionNode2Guid, Name = "concurrent-transaction-2-" + suffix }
                                }
                            }
                        },
                        cancellationToken);

                    TransactionResult[] concurrentTransactions = await Task.WhenAll(transaction1, transaction2).ConfigureAwait(false);
                    AssertTrue(concurrentTransactions[0].Success, "PostgreSQL concurrent transaction 1 commits");
                    AssertTrue(concurrentTransactions[1].Success, "PostgreSQL concurrent transaction 2 commits");
                    AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, concurrentTransactionNode1Guid, cancellationToken).ConfigureAwait(false), "PostgreSQL concurrent transaction node 1 exists");
                    AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, concurrentTransactionNode2Guid, cancellationToken).ConfigureAwait(false), "PostgreSQL concurrent transaction node 2 exists");
                }

                await client.Tenant.DeleteByGuid(tenant.GUID, true, cancellationToken).ConfigureAwait(false);
                AssertFalse(await client.Tenant.ExistsByGuid(tenant.GUID, cancellationToken).ConfigureAwait(false), "PostgreSQL deletes smoke-test tenant");
            }
        }

        private static async Task TestPostgresqlProviderParity(string connectionStringEnvironmentVariable, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? connectionString = Environment.GetEnvironmentVariable(connectionStringEnvironmentVariable);
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("PostgreSQL parity suite requires " + connectionStringEnvironmentVariable + ".");

            string suffix = Guid.NewGuid().ToString("N");
            string sqliteFilename = "test-postgresql-parity-" + suffix + ".db";
            DeleteFileIfExists(sqliteFilename);

            try
            {
                using (GraphRepositoryBase sqliteRepo = GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Sqlite,
                    Filename = sqliteFilename
                }))
                using (GraphRepositoryBase postgresqlRepo = GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = "litegraph"
                }))
                {
                    AssertFalse(sqliteRepo is UnsupportedGraphRepository, "SQLite parity repository is supported");
                    AssertFalse(postgresqlRepo is UnsupportedGraphRepository, "PostgreSQL parity repository is supported");

                    sqliteRepo.InitializeRepository();
                    postgresqlRepo.InitializeRepository();

                    ProviderParitySnapshot sqliteSnapshot = await BuildProviderParitySnapshot(sqliteRepo, "SQLite", suffix, cancellationToken).ConfigureAwait(false);
                    ProviderParitySnapshot postgresqlSnapshot = await BuildProviderParitySnapshot(postgresqlRepo, "PostgreSQL", suffix, cancellationToken).ConfigureAwait(false);

                    AssertProviderParity(sqliteSnapshot, postgresqlSnapshot);
                }
            }
            finally
            {
                DeleteFileIfExists(sqliteFilename);
            }
        }

        private static async Task TestPostgresqlTransactionConcurrencyMatrix(string connectionStringEnvironmentVariable, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? connectionString = Environment.GetEnvironmentVariable(connectionStringEnvironmentVariable);
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("PostgreSQL transaction concurrency suite requires " + connectionStringEnvironmentVariable + ".");

            string schema = "litegraph_txn_matrix_" + Guid.NewGuid().ToString("N");
            string indexDirectory = Path.Combine(".", "indexes", "postgresql", schema);

            try
            {
                using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = schema
                }))
                using (LiteGraphClient client = new LiteGraphClient(repo, null, null, null, false))
                {
                    repo.InitializeRepository();
                    await RunProviderTransactionConcurrencyMatrix(client, "PostgreSQL", cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await DropPostgresqlSchemaAsync(connectionString, schema, cancellationToken).ConfigureAwait(false);
                try
                {
                    if (Directory.Exists(indexDirectory)) Directory.Delete(indexDirectory, true);
                }
                catch
                {
                }
            }
        }

        private static async Task RunProviderTransactionConcurrencyMatrix(LiteGraphClient client, string providerName, CancellationToken cancellationToken)
        {
            string suffix = Guid.NewGuid().ToString("N");
            TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = providerName + " Transaction Matrix Tenant " + suffix }, cancellationToken).ConfigureAwait(false);
            Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = providerName + " Transaction Matrix Graph " + suffix }, cancellationToken).ConfigureAwait(false);

            var mixedSpecs = Enumerable.Range(0, 4)
                .Select(index => new
                {
                    Index = index,
                    NodeA = Guid.NewGuid(),
                    NodeB = Guid.NewGuid(),
                    Edge = Guid.NewGuid(),
                    Label = Guid.NewGuid(),
                    Tag = Guid.NewGuid(),
                    Vector = Guid.NewGuid()
                })
                .ToArray();

            TransactionResult[] mixedResults = await Task.WhenAll(mixedSpecs.Select(spec => client.Transaction.Execute(
                tenant.GUID,
                graph.GUID,
                client.Transaction
                    .CreateRequestBuilder()
                    .CreateNode(new Node { GUID = spec.NodeA, Name = providerName + " Same Graph Node A " + spec.Index })
                    .CreateNode(new Node { GUID = spec.NodeB, Name = providerName + " Same Graph Node B " + spec.Index })
                    .CreateEdge(new Edge { GUID = spec.Edge, Name = providerName + " Same Graph Edge " + spec.Index, From = spec.NodeA, To = spec.NodeB })
                    .CreateLabel(new LabelMetadata { GUID = spec.Label, NodeGUID = spec.NodeA, Label = providerName + "SameGraph" + spec.Index })
                    .CreateTag(new TagMetadata { GUID = spec.Tag, NodeGUID = spec.NodeA, Key = "provider", Value = providerName + spec.Index })
                    .CreateVector(new VectorMetadata
                    {
                        GUID = spec.Vector,
                        NodeGUID = spec.NodeA,
                        Model = "provider-matrix",
                        Dimensionality = 4,
                        Content = providerName + " same graph vector " + spec.Index,
                        Vectors = BuildDeterministicVector(spec.Index % 4, 4)
                    })
                    .Build(),
                cancellationToken))).ConfigureAwait(false);

            for (int i = 0; i < mixedSpecs.Length; i++)
            {
                AssertTrue(mixedResults[i].Success, providerName + " same-graph mixed transaction " + i + " committed: " + mixedResults[i].Error);
                AssertEqual("Committed", mixedResults[i].State, providerName + " same-graph mixed transaction " + i + " state");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, mixedSpecs[i].NodeA, cancellationToken).ConfigureAwait(false), providerName + " same-graph mixed node A " + i + " exists");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, mixedSpecs[i].NodeB, cancellationToken).ConfigureAwait(false), providerName + " same-graph mixed node B " + i + " exists");
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, mixedSpecs[i].Edge, cancellationToken).ConfigureAwait(false), providerName + " same-graph mixed edge " + i + " exists");
                AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, mixedSpecs[i].Vector, cancellationToken).ConfigureAwait(false), providerName + " same-graph mixed vector " + i + " exists");
            }

            List<(int Index, Graph Graph, Guid NodeA, Guid NodeB, Guid Edge)> graphSpecs = new List<(int Index, Graph Graph, Guid NodeA, Guid NodeB, Guid Edge)>();
            for (int i = 0; i < 3; i++)
            {
                Graph nextGraph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = providerName + " Matrix Parallel Graph " + i + " " + suffix }, cancellationToken).ConfigureAwait(false);
                graphSpecs.Add((i, nextGraph, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
            }

            TransactionResult[] graphResults = await Task.WhenAll(graphSpecs.Select(spec => client.Transaction.Execute(
                tenant.GUID,
                spec.Graph.GUID,
                client.Transaction
                    .CreateRequestBuilder()
                    .CreateNode(new Node { GUID = spec.NodeA, Name = providerName + " Multi Graph Node A " + spec.Index })
                    .CreateNode(new Node { GUID = spec.NodeB, Name = providerName + " Multi Graph Node B " + spec.Index })
                    .CreateEdge(new Edge { GUID = spec.Edge, Name = providerName + " Multi Graph Edge " + spec.Index, From = spec.NodeA, To = spec.NodeB })
                    .Build(),
                cancellationToken))).ConfigureAwait(false);

            for (int i = 0; i < graphSpecs.Count; i++)
            {
                AssertTrue(graphResults[i].Success, providerName + " different-graph transaction " + i + " committed: " + graphResults[i].Error);
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graphSpecs[i].Graph.GUID, graphSpecs[i].Edge, cancellationToken).ConfigureAwait(false), providerName + " different-graph edge " + i + " exists");
            }

            var rollbackSpecs = Enumerable.Range(0, 6)
                .Select(index => new
                {
                    Index = index,
                    ShouldCommit = index % 2 == 0,
                    Node = Guid.NewGuid(),
                    DuplicateNode = Guid.NewGuid()
                })
                .ToArray();

            TransactionResult[] rollbackResults = await Task.WhenAll(rollbackSpecs.Select(spec =>
            {
                TransactionRequestBuilder builder = client.Transaction.CreateRequestBuilder();
                if (spec.ShouldCommit)
                {
                    builder.CreateNode(new Node { GUID = spec.Node, Name = providerName + " Commit Node " + spec.Index });
                }
                else
                {
                    builder
                        .CreateNode(new Node { GUID = spec.DuplicateNode, Name = providerName + " Rollback Node " + spec.Index })
                        .CreateNode(new Node { GUID = spec.DuplicateNode, Name = providerName + " Rollback Duplicate " + spec.Index });
                }

                return client.Transaction.Execute(tenant.GUID, graph.GUID, builder.Build(), cancellationToken);
            })).ConfigureAwait(false);

            for (int i = 0; i < rollbackSpecs.Length; i++)
            {
                if (rollbackSpecs[i].ShouldCommit)
                {
                    AssertTrue(rollbackResults[i].Success, providerName + " concurrent commit " + i + " succeeded: " + rollbackResults[i].Error);
                    AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, rollbackSpecs[i].Node, cancellationToken).ConfigureAwait(false), providerName + " concurrent commit node " + i + " exists");
                }
                else
                {
                    AssertFalse(rollbackResults[i].Success, providerName + " concurrent rollback " + i + " failed as expected");
                    AssertEqual("RolledBack", rollbackResults[i].State, providerName + " concurrent rollback " + i + " state");
                    AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, rollbackSpecs[i].DuplicateNode, cancellationToken).ConfigureAwait(false), providerName + " concurrent rollback node " + i + " absent");
                }
            }

            List<(int Index, Guid Node, Guid Label, Guid Tag)> metadataSpecs = new List<(int Index, Guid Node, Guid Label, Guid Tag)>();
            for (int i = 0; i < 4; i++)
            {
                Guid nodeGuid = Guid.NewGuid();
                await client.Node.Create(new Node
                {
                    GUID = nodeGuid,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = providerName + " Metadata Target " + i
                }, cancellationToken).ConfigureAwait(false);
                metadataSpecs.Add((i, nodeGuid, Guid.NewGuid(), Guid.NewGuid()));
            }

            TransactionResult[] attachResults = await Task.WhenAll(metadataSpecs.Select(spec => client.Transaction.Execute(
                tenant.GUID,
                graph.GUID,
                client.Transaction
                    .CreateRequestBuilder()
                    .AttachLabel(new LabelMetadata { GUID = spec.Label, NodeGUID = spec.Node, Label = providerName + "Attach" + spec.Index })
                    .AttachTag(new TagMetadata { GUID = spec.Tag, NodeGUID = spec.Node, Key = "attach", Value = spec.Index.ToString() })
                    .Build(),
                cancellationToken))).ConfigureAwait(false);

            TransactionResult[] detachResults = await Task.WhenAll(metadataSpecs.Select(spec => client.Transaction.Execute(
                tenant.GUID,
                graph.GUID,
                client.Transaction
                    .CreateRequestBuilder()
                    .DetachLabel(spec.Label)
                    .DetachTag(spec.Tag)
                    .Build(),
                cancellationToken))).ConfigureAwait(false);

            for (int i = 0; i < metadataSpecs.Count; i++)
            {
                AssertTrue(attachResults[i].Success, providerName + " metadata attach " + i + " committed: " + attachResults[i].Error);
                AssertTrue(detachResults[i].Success, providerName + " metadata detach " + i + " committed: " + detachResults[i].Error);
                AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, metadataSpecs[i].Label, cancellationToken).ConfigureAwait(false), providerName + " metadata label " + i + " detached");
                AssertFalse(await client.Tag.ExistsByGuid(tenant.GUID, metadataSpecs[i].Tag, cancellationToken).ConfigureAwait(false), providerName + " metadata tag " + i + " detached");
            }

            List<(int Index, bool ShouldCommit, Guid Node, Guid Vector)> vectorSpecs = new List<(int Index, bool ShouldCommit, Guid Node, Guid Vector)>();
            for (int i = 0; i < 6; i++)
            {
                Guid nodeGuid = Guid.NewGuid();
                await client.Node.Create(new Node
                {
                    GUID = nodeGuid,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = providerName + " Vector Target " + i
                }, cancellationToken).ConfigureAwait(false);
                vectorSpecs.Add((i, i % 2 == 0, nodeGuid, Guid.NewGuid()));
            }

            TransactionResult[] vectorResults = await Task.WhenAll(vectorSpecs.Select(spec =>
            {
                TransactionRequestBuilder builder = client.Transaction
                    .CreateRequestBuilder()
                    .CreateVector(new VectorMetadata
                    {
                        GUID = spec.Vector,
                        NodeGUID = spec.Node,
                        Model = "provider-matrix",
                        Dimensionality = 4,
                        Content = providerName + " vector " + spec.Index,
                        Vectors = BuildDeterministicVector(spec.Index % 4, 4)
                    });

                if (!spec.ShouldCommit)
                {
                    builder.CreateVector(new VectorMetadata
                    {
                        GUID = spec.Vector,
                        NodeGUID = spec.Node,
                        Model = "provider-matrix",
                        Dimensionality = 4,
                        Content = providerName + " duplicate vector " + spec.Index,
                        Vectors = BuildDeterministicVector((spec.Index + 1) % 4, 4)
                    });
                }

                return client.Transaction.Execute(tenant.GUID, graph.GUID, builder.Build(), cancellationToken);
            })).ConfigureAwait(false);

            for (int i = 0; i < vectorSpecs.Count; i++)
            {
                if (vectorSpecs[i].ShouldCommit)
                {
                    AssertTrue(vectorResults[i].Success, providerName + " vector commit " + i + " succeeded: " + vectorResults[i].Error);
                    AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, vectorSpecs[i].Vector, cancellationToken).ConfigureAwait(false), providerName + " vector commit " + i + " exists");
                }
                else
                {
                    AssertFalse(vectorResults[i].Success, providerName + " vector rollback " + i + " failed as expected");
                    AssertEqual("RolledBack", vectorResults[i].State, providerName + " vector rollback " + i + " state");
                    AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, vectorSpecs[i].Vector, cancellationToken).ConfigureAwait(false), providerName + " vector rollback " + i + " absent");
                }
            }

            await client.Tenant.DeleteByGuid(tenant.GUID, true, cancellationToken).ConfigureAwait(false);
            AssertFalse(await client.Tenant.ExistsByGuid(tenant.GUID, cancellationToken).ConfigureAwait(false), providerName + " transaction matrix tenant cleanup");
        }

        private static async Task<ProviderParitySnapshot> BuildProviderParitySnapshot(GraphRepositoryBase repo, string providerName, string suffix, CancellationToken cancellationToken)
        {
            Guid tenantGuid = Guid.Empty;
            string requestId = "parity-request-" + providerName.ToLowerInvariant() + "-" + suffix;
            string auditRequestId = "parity-audit-" + providerName.ToLowerInvariant() + "-" + suffix;

            using (LiteGraphClient client = new LiteGraphClient(repo, null, null, null, false))
            {
                try
                {
                    string providerKey = providerName.ToLowerInvariant();
                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                    {
                        Name = providerName + " Parity Tenant " + suffix
                    }, cancellationToken).ConfigureAwait(false);
                    tenantGuid = tenant.GUID;

                    UserMaster user = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = providerName,
                        LastName = "Parity",
                        Email = "parity-" + providerKey + "-" + suffix + "@example.com",
                        Password = "password"
                    }, cancellationToken).ConfigureAwait(false);

                    Credential credential = await client.Credential.Create(new Credential
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        Name = providerName + " Parity Credential " + suffix,
                        BearerToken = "parity-" + providerKey + "-" + suffix,
                        Scopes = new List<string> { "read", "write" }
                    }, cancellationToken).ConfigureAwait(false);

                    Graph graph = await client.Graph.Create(new Graph
                    {
                        TenantGUID = tenant.GUID,
                        Name = providerName + " Parity Graph " + suffix
                    }, cancellationToken).ConfigureAwait(false);

                    credential.GraphGUIDs = new List<Guid> { graph.GUID };
                    await client.Credential.Update(credential, cancellationToken).ConfigureAwait(false);

                    Node from = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "from-" + providerKey + "-" + suffix,
                        Data = new Dictionary<string, object>
                        {
                            ["kind"] = "person",
                            ["age"] = 42,
                            ["active"] = true
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    Node to = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "to-" + providerKey + "-" + suffix,
                        Data = new Dictionary<string, object>
                        {
                            ["kind"] = "person",
                            ["age"] = 21,
                            ["active"] = false
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    Edge edge = await client.Edge.Create(new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        From = from.GUID,
                        To = to.GUID,
                        Name = "edge-" + providerKey + "-" + suffix,
                        Cost = 7,
                        Data = new Dictionary<string, object>
                        {
                            ["relationship"] = "knows",
                            ["strength"] = 9,
                            ["active"] = true
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    await client.Label.Create(new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = from.GUID,
                        Label = "provider-parity"
                    }, cancellationToken).ConfigureAwait(false);

                    await client.Tag.Create(new TagMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = from.GUID,
                        Key = "provider",
                        Value = providerKey
                    }, cancellationToken).ConfigureAwait(false);

                    await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = from.GUID,
                        Model = "provider-parity",
                        Dimensionality = 3,
                        Content = providerName + " parity vector",
                        Vectors = new List<float> { 0.15f, 0.35f, 0.55f }
                    }, cancellationToken).ConfigureAwait(false);

                    List<Node> numericFiltered = await MaterializeAsync(client.Node.ReadMany(
                        tenant.GUID,
                        graph.GUID,
                        nodeFilter: new Expr("age", OperatorEnum.GreaterThanOrEqualTo, 40),
                        token: cancellationToken), cancellationToken).ConfigureAwait(false);

                    List<Node> booleanFiltered = await MaterializeAsync(client.Node.ReadMany(
                        tenant.GUID,
                        graph.GUID,
                        nodeFilter: new Expr("active", OperatorEnum.Equals, true),
                        token: cancellationToken), cancellationToken).ConfigureAwait(false);

                    GraphQueryResult numericQuery = await client.Query.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new GraphQueryRequest
                        {
                            Query = "MATCH (n) WHERE n.data.age >= 40 RETURN n LIMIT 5"
                        },
                        cancellationToken).ConfigureAwait(false);

                    GraphQueryResult pathQuery = await client.Query.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new GraphQueryRequest
                        {
                            Query = "MATCH (a)-[path*1..1]->(b) WHERE a.guid = $from RETURN a, path, b LIMIT 5",
                            Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "from", from.GUID }
                            }
                        },
                        cancellationToken).ConfigureAwait(false);

                    Guid rolledBackNodeGuid = Guid.NewGuid();
                    await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                    await repo.Node.Create(new Node
                    {
                        GUID = rolledBackNodeGuid,
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "rolled-back-" + providerKey + "-" + suffix
                    }, cancellationToken).ConfigureAwait(false);
                    await repo.RollbackGraphTransaction(cancellationToken).ConfigureAwait(false);

                    Guid committedNodeGuid = Guid.NewGuid();
                    await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                    await repo.Node.Create(new Node
                    {
                        GUID = committedNodeGuid,
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        Name = "committed-" + providerKey + "-" + suffix
                    }, cancellationToken).ConfigureAwait(false);
                    await repo.CommitGraphTransaction(cancellationToken).ConfigureAwait(false);

                    AuthorizationRole role = await repo.AuthorizationRoles.CreateRole(new AuthorizationRole
                    {
                        TenantGUID = tenant.GUID,
                        Name = "provider-parity-" + providerKey + "-" + suffix,
                        DisplayName = providerName + " Provider Parity",
                        Description = "Provider parity test role",
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        Permissions = new List<AuthorizationPermissionEnum>
                        {
                            AuthorizationPermissionEnum.Read,
                            AuthorizationPermissionEnum.Write
                        },
                        ResourceTypes = new List<AuthorizationResourceTypeEnum>
                        {
                            AuthorizationResourceTypeEnum.Graph,
                            AuthorizationResourceTypeEnum.Node,
                            AuthorizationResourceTypeEnum.Edge,
                            AuthorizationResourceTypeEnum.Query
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    UserRoleAssignment userRole = await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        RoleGUID = role.GUID,
                        RoleName = role.Name,
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        GraphGUID = graph.GUID
                    }, cancellationToken).ConfigureAwait(false);

                    CredentialScopeAssignment credentialScope = await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                    {
                        TenantGUID = tenant.GUID,
                        CredentialGUID = credential.GUID,
                        RoleGUID = role.GUID,
                        RoleName = role.Name,
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        GraphGUID = graph.GUID,
                        Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Read },
                        ResourceTypes = new List<AuthorizationResourceTypeEnum>
                        {
                            AuthorizationResourceTypeEnum.Graph,
                            AuthorizationResourceTypeEnum.Node,
                            AuthorizationResourceTypeEnum.Query
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    DateTime requestCreatedUtc = DateTime.UtcNow.AddMinutes(-1);
                    await repo.RequestHistory.Insert(new RequestHistoryDetail
                    {
                        RequestId = requestId,
                        CorrelationId = "parity-correlation-" + suffix,
                        TraceId = "0123456789abcdef0123456789abcdef",
                        CreatedUtc = requestCreatedUtc,
                        CompletedUtc = requestCreatedUtc.AddMilliseconds(12),
                        Method = "GET",
                        Path = "/v1.0/provider-parity",
                        Url = "/v1.0/provider-parity?provider=" + providerKey,
                        SourceIp = "127.0.0.1",
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        StatusCode = 200,
                        Success = true,
                        ProcessingTimeMs = 12,
                        RequestHeaders = new Dictionary<string, string> { ["x-request-id"] = requestId },
                        ResponseHeaders = new Dictionary<string, string> { ["x-request-id"] = requestId },
                        RequestBody = "{\"provider\":\"" + providerKey + "\"}",
                        ResponseBody = "{\"ok\":true}"
                    }, cancellationToken).ConfigureAwait(false);

                    await repo.AuthorizationAudit.Insert(new AuthorizationAuditEntry
                    {
                        RequestId = auditRequestId,
                        CorrelationId = "parity-audit-correlation-" + suffix,
                        TraceId = "fedcba9876543210fedcba9876543210",
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        UserGUID = user.GUID,
                        CredentialGUID = credential.GUID,
                        RequestType = "GraphQuery",
                        Method = "POST",
                        Path = "/v1.0/provider-parity/query",
                        SourceIp = "127.0.0.1",
                        AuthenticationResult = "Authenticated",
                        AuthorizationResult = "Denied",
                        Reason = "MissingPermission",
                        RequiredScope = "query",
                        StatusCode = 403,
                        Description = providerName + " provider parity audit"
                    }, cancellationToken).ConfigureAwait(false);

                    AuthorizationRoleSearchResult roleSearch = await repo.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest
                    {
                        TenantGUID = tenant.GUID,
                        Name = role.Name
                    }, cancellationToken).ConfigureAwait(false);

                    UserRoleAssignmentSearchResult userRoleSearch = await repo.AuthorizationRoles.SearchUserRoles(new UserRoleAssignmentSearchRequest
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        GraphGUID = graph.GUID
                    }, cancellationToken).ConfigureAwait(false);

                    CredentialScopeAssignmentSearchResult credentialScopeSearch = await repo.AuthorizationRoles.SearchCredentialScopes(new CredentialScopeAssignmentSearchRequest
                    {
                        TenantGUID = tenant.GUID,
                        CredentialGUID = credential.GUID,
                        GraphGUID = graph.GUID,
                        Permission = AuthorizationPermissionEnum.Read,
                        ResourceType = AuthorizationResourceTypeEnum.Query
                    }, cancellationToken).ConfigureAwait(false);

                    RequestHistorySearchResult requestSearch = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                    {
                        TenantGUID = tenant.GUID,
                        RequestId = requestId
                    }, cancellationToken).ConfigureAwait(false);

                    RequestHistorySummary requestSummary = await repo.RequestHistory.GetSummary(
                        tenant.GUID,
                        "minute",
                        requestCreatedUtc.AddMinutes(-1),
                        requestCreatedUtc.AddMinutes(2),
                        cancellationToken).ConfigureAwait(false);

                    AuthorizationAuditSearchResult auditSearch = await repo.AuthorizationAudit.Search(new AuthorizationAuditSearchRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        RequestId = auditRequestId
                    }, cancellationToken).ConfigureAwait(false);

                    return new ProviderParitySnapshot
                    {
                        TenantExists = await client.Tenant.ExistsByGuid(tenant.GUID, cancellationToken).ConfigureAwait(false),
                        GraphExists = await client.Graph.ExistsByGuid(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false),
                        UserExists = await client.User.ExistsByGuid(tenant.GUID, user.GUID, cancellationToken).ConfigureAwait(false),
                        CredentialExists = await client.Credential.ExistsByGuid(tenant.GUID, credential.GUID, cancellationToken).ConfigureAwait(false),
                        NodeCount = await CountAsync(client.Node.ReadAllInGraph(tenant.GUID, graph.GUID, token: cancellationToken), cancellationToken).ConfigureAwait(false),
                        EdgeCount = await CountAsync(client.Edge.ReadAllInGraph(tenant.GUID, graph.GUID, token: cancellationToken), cancellationToken).ConfigureAwait(false),
                        LabelCount = await CountAsync(client.Label.ReadAllInGraph(tenant.GUID, graph.GUID, token: cancellationToken), cancellationToken).ConfigureAwait(false),
                        TagCount = await CountAsync(client.Tag.ReadAllInGraph(tenant.GUID, graph.GUID, token: cancellationToken), cancellationToken).ConfigureAwait(false),
                        VectorCount = await CountAsync(client.Vector.ReadAllInGraph(tenant.GUID, graph.GUID, token: cancellationToken), cancellationToken).ConfigureAwait(false),
                        NumericFilterCount = numericFiltered.Count,
                        BooleanFilterCount = booleanFiltered.Count,
                        NumericQueryRows = numericQuery.RowCount,
                        PathQueryRows = pathQuery.RowCount,
                        RolledBackNodeExists = await client.Node.ExistsByGuid(tenant.GUID, rolledBackNodeGuid, cancellationToken).ConfigureAwait(false),
                        CommittedNodeExists = await client.Node.ExistsByGuid(tenant.GUID, committedNodeGuid, cancellationToken).ConfigureAwait(false),
                        RoleSearchCount = roleSearch.TotalCount,
                        UserRoleSearchCount = userRoleSearch.TotalCount,
                        CredentialScopeSearchCount = credentialScopeSearch.TotalCount,
                        RoleReadMatches = (await repo.AuthorizationRoles.ReadRoleByGuid(role.GUID, cancellationToken).ConfigureAwait(false))?.Name == role.Name,
                        UserRoleReadMatches = (await repo.AuthorizationRoles.ReadUserRoleByGuid(userRole.GUID, cancellationToken).ConfigureAwait(false))?.UserGUID == user.GUID,
                        CredentialScopeReadMatches = (await repo.AuthorizationRoles.ReadCredentialScopeByGuid(credentialScope.GUID, cancellationToken).ConfigureAwait(false))?.CredentialGUID == credential.GUID,
                        RequestHistorySearchCount = requestSearch.TotalCount,
                        RequestHistorySummaryTotal = requestSummary.TotalRequests,
                        AuthorizationAuditSearchCount = auditSearch.TotalCount
                    };
                }
                finally
                {
                    if (tenantGuid != Guid.Empty)
                    {
                        try
                        {
                            await repo.RequestHistory.DeleteMany(new RequestHistorySearchRequest
                            {
                                TenantGUID = tenantGuid,
                                RequestId = requestId
                            }, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                        }

                        try
                        {
                            await repo.AuthorizationAudit.DeleteMany(new AuthorizationAuditSearchRequest
                            {
                                TenantGUID = tenantGuid,
                                RequestId = auditRequestId
                            }, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                        }

                        try
                        {
                            await client.Tenant.DeleteByGuid(tenantGuid, true, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private static void AssertProviderParity(ProviderParitySnapshot sqlite, ProviderParitySnapshot postgresql)
        {
            AssertEqual(sqlite.TenantExists, postgresql.TenantExists, "Provider parity tenant existence");
            AssertEqual(sqlite.GraphExists, postgresql.GraphExists, "Provider parity graph existence");
            AssertEqual(sqlite.UserExists, postgresql.UserExists, "Provider parity user existence");
            AssertEqual(sqlite.CredentialExists, postgresql.CredentialExists, "Provider parity credential existence");
            AssertEqual(sqlite.NodeCount, postgresql.NodeCount, "Provider parity node count");
            AssertEqual(sqlite.EdgeCount, postgresql.EdgeCount, "Provider parity edge count");
            AssertEqual(sqlite.LabelCount, postgresql.LabelCount, "Provider parity label count");
            AssertEqual(sqlite.TagCount, postgresql.TagCount, "Provider parity tag count");
            AssertEqual(sqlite.VectorCount, postgresql.VectorCount, "Provider parity vector count");
            AssertEqual(sqlite.NumericFilterCount, postgresql.NumericFilterCount, "Provider parity numeric JSON filter count");
            AssertEqual(sqlite.BooleanFilterCount, postgresql.BooleanFilterCount, "Provider parity boolean JSON filter count");
            AssertEqual(sqlite.NumericQueryRows, postgresql.NumericQueryRows, "Provider parity native query row count");
            AssertEqual(sqlite.PathQueryRows, postgresql.PathQueryRows, "Provider parity path query row count");
            AssertEqual(sqlite.RolledBackNodeExists, postgresql.RolledBackNodeExists, "Provider parity rollback visibility");
            AssertEqual(sqlite.CommittedNodeExists, postgresql.CommittedNodeExists, "Provider parity commit visibility");
            AssertEqual(sqlite.RoleSearchCount, postgresql.RoleSearchCount, "Provider parity authorization role search count");
            AssertEqual(sqlite.UserRoleSearchCount, postgresql.UserRoleSearchCount, "Provider parity user role search count");
            AssertEqual(sqlite.CredentialScopeSearchCount, postgresql.CredentialScopeSearchCount, "Provider parity credential scope search count");
            AssertEqual(sqlite.RoleReadMatches, postgresql.RoleReadMatches, "Provider parity authorization role read");
            AssertEqual(sqlite.UserRoleReadMatches, postgresql.UserRoleReadMatches, "Provider parity user role read");
            AssertEqual(sqlite.CredentialScopeReadMatches, postgresql.CredentialScopeReadMatches, "Provider parity credential scope read");
            AssertEqual(sqlite.RequestHistorySearchCount, postgresql.RequestHistorySearchCount, "Provider parity request history search count");
            AssertEqual(sqlite.RequestHistorySummaryTotal, postgresql.RequestHistorySummaryTotal, "Provider parity request history summary total");
            AssertEqual(sqlite.AuthorizationAuditSearchCount, postgresql.AuthorizationAuditSearchCount, "Provider parity authorization audit search count");

            AssertTrue(sqlite.TenantExists, "SQLite parity tenant exists");
            AssertTrue(sqlite.GraphExists, "SQLite parity graph exists");
            AssertTrue(sqlite.UserExists, "SQLite parity user exists");
            AssertTrue(sqlite.CredentialExists, "SQLite parity credential exists");
            AssertEqual(3, sqlite.NodeCount, "SQLite parity node count includes committed transaction node");
            AssertEqual(1, sqlite.EdgeCount, "SQLite parity edge count");
            AssertEqual(1, sqlite.LabelCount, "SQLite parity label count");
            AssertEqual(1, sqlite.TagCount, "SQLite parity tag count");
            AssertEqual(1, sqlite.VectorCount, "SQLite parity vector count");
            AssertEqual(1, sqlite.NumericFilterCount, "SQLite parity numeric JSON filter count");
            AssertEqual(1, sqlite.BooleanFilterCount, "SQLite parity boolean JSON filter count");
            AssertEqual(1, sqlite.NumericQueryRows, "SQLite parity native query row count");
            AssertEqual(1, sqlite.PathQueryRows, "SQLite parity path query row count");
            AssertFalse(sqlite.RolledBackNodeExists, "SQLite parity rollback hides node");
            AssertTrue(sqlite.CommittedNodeExists, "SQLite parity commit persists node");
            AssertEqual(1L, sqlite.RoleSearchCount, "SQLite parity authorization role search count");
            AssertEqual(1L, sqlite.UserRoleSearchCount, "SQLite parity user role search count");
            AssertEqual(1L, sqlite.CredentialScopeSearchCount, "SQLite parity credential scope search count");
            AssertTrue(sqlite.RoleReadMatches, "SQLite parity authorization role read");
            AssertTrue(sqlite.UserRoleReadMatches, "SQLite parity user role read");
            AssertTrue(sqlite.CredentialScopeReadMatches, "SQLite parity credential scope read");
            AssertEqual(1L, sqlite.RequestHistorySearchCount, "SQLite parity request history search count");
            AssertEqual(1L, sqlite.RequestHistorySummaryTotal, "SQLite parity request history summary total");
            AssertEqual(1L, sqlite.AuthorizationAuditSearchCount, "SQLite parity authorization audit search count");
        }

        private static async Task<List<T>> MaterializeAsync<T>(IAsyncEnumerable<T> values, CancellationToken cancellationToken)
        {
            List<T> ret = new List<T>();
            await foreach (T value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ret.Add(value);
            }

            return ret;
        }

        private static async Task<int> CountAsync<T>(IAsyncEnumerable<T> values, CancellationToken cancellationToken)
        {
            int count = 0;
            await foreach (T _ in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                count++;
            }

            return count;
        }

        private sealed class ProviderParitySnapshot
        {
            public bool TenantExists { get; set; }
            public bool GraphExists { get; set; }
            public bool UserExists { get; set; }
            public bool CredentialExists { get; set; }
            public int NodeCount { get; set; }
            public int EdgeCount { get; set; }
            public int LabelCount { get; set; }
            public int TagCount { get; set; }
            public int VectorCount { get; set; }
            public int NumericFilterCount { get; set; }
            public int BooleanFilterCount { get; set; }
            public int NumericQueryRows { get; set; }
            public int PathQueryRows { get; set; }
            public bool RolledBackNodeExists { get; set; }
            public bool CommittedNodeExists { get; set; }
            public long RoleSearchCount { get; set; }
            public long UserRoleSearchCount { get; set; }
            public long CredentialScopeSearchCount { get; set; }
            public bool RoleReadMatches { get; set; }
            public bool UserRoleReadMatches { get; set; }
            public bool CredentialScopeReadMatches { get; set; }
            public long RequestHistorySearchCount { get; set; }
            public long RequestHistorySummaryTotal { get; set; }
            public long AuthorizationAuditSearchCount { get; set; }
        }

        private static bool ShouldSkipProviderSuite(string connectionStringEnvironmentVariable)
        {
            return String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(connectionStringEnvironmentVariable));
        }

        private static string ProviderSuiteSkipReason(string providerName, string connectionStringEnvironmentVariable)
        {
            return providerName + " provider tests require a dedicated test database. Set " + connectionStringEnvironmentVariable + " to run this suite.";
        }

        private static void AssertUnsupportedProvider(DatabaseTypeEnum type, string providerName)
        {
            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Type = type
            }))
            {
                AssertTrue(repo is UnsupportedGraphRepository, providerName + " repository placeholder");

                try
                {
                    repo.InitializeRepository();
                    throw new InvalidOperationException(providerName + " placeholder should not initialize.");
                }
                catch (NotSupportedException e)
                {
                    AssertTrue(e.Message.Contains(providerName), providerName + " unsupported initialization names provider");
                    AssertTrue(e.Message.Contains("InitializeRepository"), providerName + " unsupported initialization names operation");
                }

                try
                {
                    _ = repo.Graph;
                    throw new InvalidOperationException(providerName + " placeholder should not expose graph methods.");
                }
                catch (NotSupportedException e)
                {
                    AssertTrue(e.Message.Contains(providerName), providerName + " unsupported operation names provider");
                    AssertTrue(e.Message.Contains("Graph"), providerName + " unsupported operation names accessor");
                }
            }
        }

        private static async Task TestSqliteGraphTransactionCommitRollback(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transactions.db";
            DeleteFileIfExists(filename);

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings { Filename = filename }))
            using (LiteGraphClient client = new LiteGraphClient(repo, null, null, null, false))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Graph" }, cancellationToken).ConfigureAwait(false);

                Guid rolledBackNodeGuid = Guid.NewGuid();
                await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                await repo.Node.Create(new Node
                {
                    GUID = rolledBackNodeGuid,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Rolled Back"
                }, cancellationToken).ConfigureAwait(false);
                await repo.RollbackGraphTransaction(cancellationToken).ConfigureAwait(false);

                bool existsAfterRollback = await repo.Node.ExistsByGuid(tenant.GUID, rolledBackNodeGuid, cancellationToken).ConfigureAwait(false);
                AssertFalse(existsAfterRollback, "Rolled back node does not exist");

                Guid committedNodeGuid = Guid.NewGuid();
                await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                await repo.Node.Create(new Node
                {
                    GUID = committedNodeGuid,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Committed"
                }, cancellationToken).ConfigureAwait(false);
                await repo.CommitGraphTransaction(cancellationToken).ConfigureAwait(false);

                bool existsAfterCommit = await repo.Node.ExistsByGuid(tenant.GUID, committedNodeGuid, cancellationToken).ConfigureAwait(false);
                AssertTrue(existsAfterCommit, "Committed node exists");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestSqlitePersistentConnectionFallback(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-sqlite-fallback.db";
            DeleteFileIfExists(filename);

            using (GraphRepositoryBase repoBase = GraphRepositoryFactory.Create(new DatabaseSettings { Filename = filename }))
            using (LiteGraphClient client = new LiteGraphClient(repoBase, null, null, null, false))
            {
                client.InitializeRepository();
                SqliteGraphRepository repo = repoBase as SqliteGraphRepository
                    ?? throw new InvalidOperationException("Expected SQLite graph repository.");

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Fallback Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Fallback Graph" }, cancellationToken).ConfigureAwait(false);

                ForceSqliteTransientConnectionFailure(repo);

                Graph reloaded = await repo.Graph.ReadByGuid(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                AssertNotNull(reloaded, "Graph read succeeds through persistent connection fallback");
                AssertEqual(graph.GUID, reloaded.GUID, "Fallback graph GUID matches");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestSqliteGraphTransactionPersistentConnectionFallback(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-sqlite-transaction-fallback.db";
            DeleteFileIfExists(filename);

            using (GraphRepositoryBase repoBase = GraphRepositoryFactory.Create(new DatabaseSettings { Filename = filename }))
            using (LiteGraphClient client = new LiteGraphClient(repoBase, null, null, null, false))
            {
                client.InitializeRepository();
                SqliteGraphRepository repo = repoBase as SqliteGraphRepository
                    ?? throw new InvalidOperationException("Expected SQLite graph repository.");

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Fallback Transaction Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Fallback Transaction Graph" }, cancellationToken).ConfigureAwait(false);

                ForceSqliteTransientConnectionFailure(repo);

                Guid nodeGuid = Guid.NewGuid();
                await repo.BeginGraphTransaction(tenant.GUID, graph.GUID, cancellationToken).ConfigureAwait(false);
                await repo.Node.Create(new Node
                {
                    GUID = nodeGuid,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Fallback Transaction Node"
                }, cancellationToken).ConfigureAwait(false);
                await repo.CommitGraphTransaction(cancellationToken).ConfigureAwait(false);

                bool exists = await repo.Node.ExistsByGuid(tenant.GUID, nodeGuid, cancellationToken).ConfigureAwait(false);
                AssertTrue(exists, "Graph transaction succeeds through persistent connection fallback");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestScopedCredentialPersistence(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-scoped-credentials.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Scoped Credential Tenant" }, cancellationToken).ConfigureAwait(false);
                UserMaster user = await client.User.Create(new UserMaster
                {
                    TenantGUID = tenant.GUID,
                    FirstName = "Scoped",
                    LastName = "User",
                    Email = "scoped@example.com",
                    Password = "password"
                }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Scoped Credential Graph" }, cancellationToken).ConfigureAwait(false);

                Credential created = await client.Credential.Create(new Credential
                {
                    TenantGUID = tenant.GUID,
                    UserGUID = user.GUID,
                    Name = "Scoped Credential",
                    BearerToken = Guid.NewGuid().ToString(),
                    Scopes = new List<string> { "read", "write" },
                    GraphGUIDs = new List<Guid> { graph.GUID }
                }, cancellationToken).ConfigureAwait(false);

                AssertNotNull(created.Scopes, "Created credential scopes");
                AssertNotNull(created.GraphGUIDs, "Created credential graph GUIDs");
                AssertTrue(created.HasScope("read"), "Created credential has read scope");
                AssertTrue(created.HasScope("write"), "Created credential has write scope");
                AssertFalse(created.HasScope("admin"), "Created credential does not have admin scope");
                AssertTrue(created.CanAccessGraph(graph.GUID), "Created credential can access graph");
                AssertFalse(created.CanAccessGraph(Guid.NewGuid()), "Created credential cannot access arbitrary graph");

                Credential read = await client.Credential.ReadByBearerToken(created.BearerToken, cancellationToken).ConfigureAwait(false);
                AssertNotNull(read, "Credential read by bearer token");
                AssertEqual(2, read.Scopes.Count, "Persisted scope count");
                AssertEqual(1, read.GraphGUIDs.Count, "Persisted graph allow-list count");
                AssertTrue(read.HasScope("write"), "Persisted credential has write scope");
                AssertTrue(read.CanAccessGraph(graph.GUID), "Persisted credential can access graph");

                read.Scopes = new List<string> { "read" };
                Credential updated = await client.Credential.Update(read, cancellationToken).ConfigureAwait(false);
                AssertTrue(updated.HasScope("read"), "Updated credential has read scope");
                AssertFalse(updated.HasScope("write"), "Updated credential write scope removed");
                AssertEqual(1, updated.GraphGUIDs.Count, "Updated graph allow-list count");
            }

            DeleteFileIfExists(filename);
        }

        private static Task TestGraphQueryScopeClassification(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Type? handlerType = typeof(Settings).Assembly.GetType("LiteGraph.Server.API.REST.RestServiceHandler");
            if (handlerType == null) throw new InvalidOperationException("Unable to locate REST service handler.");

            MethodInfo? method = handlerType.GetMethod("GraphQueryRequiredScope", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Unable to locate query scope classifier.");

            AssertEqual("read", InvokeGraphQueryRequiredScope(method, "MATCH (n) RETURN n"), "Read match query requires read scope");
            AssertEqual("read", InvokeGraphQueryRequiredScope(method, "CALL litegraph.vector.searchNodes($v) YIELD node, score RETURN node, score"), "Vector search query requires read scope");
            AssertEqual("write", InvokeGraphQueryRequiredScope(method, "CREATE (n:Person { name: 'Ada' }) RETURN n"), "Create query requires write scope");
            AssertEqual("write", InvokeGraphQueryRequiredScope(method, "MATCH (n:Person) WHERE n.guid = $id SET n.name = 'Ada' RETURN n"), "MATCH SET query requires write scope");
            AssertEqual("write", InvokeGraphQueryRequiredScope(method, "MATCH (n:Person) WHERE n.guid = $id DELETE n RETURN n"), "MATCH DELETE query requires write scope");

            return Task.CompletedTask;
        }

        private static string InvokeGraphQueryRequiredScope(MethodInfo method, string query)
        {
            object? result = method.Invoke(null, new object[]
            {
                new GraphQueryRequest
                {
                    Query = query
                }
            });

            if (result is string scope) return scope;
            throw new InvalidOperationException("Query scope classifier did not return a scope.");
        }

        private static Task TestAuthorizationServiceCredentialPolicies(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AuthorizationService service = new AuthorizationService(new LoggingModule());
            Guid graphGuid = Guid.NewGuid();
            Guid otherGraphGuid = Guid.NewGuid();
            Credential readOnly = new Credential
            {
                Scopes = new List<string> { "read" },
                GraphGUIDs = new List<Guid> { graphGuid }
            };

            AuthorizationDecision readDecision = service.EvaluateCredential(readOnly, graphGuid, RequestTypeEnum.NodeRead);
            AssertEqual(AuthorizationResultEnum.Permitted, readDecision.Result, "Read-scoped credential can read allowed graph");
            AssertEqual("read", readDecision.RequiredScope, "Read request required scope");
            AssertEqual(AuthorizationDecisionReason.Permitted, readDecision.Reason, "Read request decision reason");

            AuthorizationDecision writeDecision = service.EvaluateCredential(readOnly, graphGuid, RequestTypeEnum.NodeCreate);
            AssertEqual(AuthorizationResultEnum.Denied, writeDecision.Result, "Read-scoped credential cannot write");
            AssertEqual("write", writeDecision.RequiredScope, "Write request required scope");
            AssertEqual(AuthorizationDecisionReason.MissingScope, writeDecision.Reason, "Write denial reason");

            Credential writer = new Credential
            {
                Scopes = new List<string> { "write" },
                GraphGUIDs = new List<Guid> { graphGuid }
            };

            AuthorizationDecision graphDecision = service.EvaluateCredential(writer, otherGraphGuid, RequestTypeEnum.NodeCreate);
            AssertEqual(AuthorizationResultEnum.Denied, graphDecision.Result, "Credential cannot access graph outside allow-list");
            AssertEqual(AuthorizationDecisionReason.GraphDenied, graphDecision.Reason, "Graph denial reason");

            AuthorizationDecision unrestrictedDecision = service.EvaluateCredential(new Credential(), otherGraphGuid, RequestTypeEnum.NodeDelete);
            AssertEqual(AuthorizationResultEnum.Permitted, unrestrictedDecision.Result, "Unscoped credential keeps backward-compatible full access");
            AssertEqual("write", unrestrictedDecision.RequiredScope, "Unrestricted write request required scope");

            Guid tenantGuid = Guid.NewGuid();
            AuthorizationDecision sameTenant = service.EvaluateTenantAccess(false, tenantGuid, tenantGuid);
            AssertEqual(AuthorizationResultEnum.Permitted, sameTenant.Result, "Authenticated tenant can access itself");

            AuthorizationDecision otherTenant = service.EvaluateTenantAccess(false, Guid.NewGuid(), tenantGuid);
            AssertEqual(AuthorizationResultEnum.Denied, otherTenant.Result, "Non-admin cannot access another tenant");
            AssertEqual(AuthorizationDecisionReason.TenantDenied, otherTenant.Reason, "Tenant denial reason");

            AuthorizationDecision adminTenant = service.EvaluateTenantAccess(true, Guid.NewGuid(), tenantGuid);
            AssertEqual(AuthorizationResultEnum.Permitted, adminTenant.Result, "Admin can access any tenant");

            Type? handlerType = typeof(Settings).Assembly.GetType("LiteGraph.Server.API.REST.RestServiceHandler");
            if (handlerType == null) throw new InvalidOperationException("Unable to locate REST service handler.");

            MethodInfo? responseMethod = handlerType.GetMethod("AuthorizationFailedResponse", BindingFlags.NonPublic | BindingFlags.Static);
            if (responseMethod == null) throw new InvalidOperationException("Unable to locate authorization failure response helper.");

            object? invoked = responseMethod.Invoke(null, new object?[]
            {
                null,
                "MissingScope",
                "write",
                "Missing write scope."
            });
            if (invoked is not ApiErrorResponse response) throw new InvalidOperationException("Authorization helper did not return an API error response.");
            AssertEqual(ApiErrorEnum.AuthorizationFailed, response.Error, "Authorization helper returns authorization error");
            AssertEqual("Missing write scope.", response.Description, "Authorization helper preserves description");
            if (response.Context is not Dictionary<string, string> context) throw new InvalidOperationException("Authorization helper did not return context.");
            AssertEqual("MissingScope", context["reason"], "Authorization helper includes denial reason");
            AssertEqual("write", context["requiredScope"], "Authorization helper includes required scope");

            return Task.CompletedTask;
        }

        private static Task TestAuthorizationPolicyDefinitions(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<RoleDefinition> roles = AuthorizationPolicyDefinitions.BuiltInRoles;
            AssertEqual(5, roles.Count, "Built-in role count");
            AssertEqual(5, roles.Select(role => role.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(), "Built-in role names are unique");

            foreach (BuiltInRoleEnum role in Enum.GetValues(typeof(BuiltInRoleEnum)))
            {
                RoleDefinition definition = AuthorizationPolicyDefinitions.GetBuiltInRole(role);
                AssertNotNull(definition, "Built-in role exists: " + role);
                AssertEqual(role, definition.BuiltInRole, "Built-in role enum round trip: " + role);
                AssertTrue(definition.BuiltIn, "Built-in role flag: " + role);
            }

            RoleDefinition tenantAdmin = AuthorizationPolicyDefinitions.GetBuiltInRole(AuthorizationPolicyDefinitions.TenantAdminRoleName);
            AssertEqual(BuiltInRoleEnum.TenantAdmin, tenantAdmin.BuiltInRole, "TenantAdmin lookup by name");
            AssertEqual(AuthorizationResourceScopeEnum.Tenant, tenantAdmin.ResourceScope, "TenantAdmin scope");
            AssertTrue(tenantAdmin.InheritsToGraphs, "TenantAdmin inherits to graphs");
            AssertRoleHasPermissions(tenantAdmin, AuthorizationPermissionEnum.Read, AuthorizationPermissionEnum.Write, AuthorizationPermissionEnum.Delete, AuthorizationPermissionEnum.Admin);
            AssertRoleAppliesTo(tenantAdmin, AuthorizationResourceTypeEnum.Admin, AuthorizationResourceTypeEnum.Graph, AuthorizationResourceTypeEnum.Node, AuthorizationResourceTypeEnum.Edge, AuthorizationResourceTypeEnum.Label, AuthorizationResourceTypeEnum.Tag, AuthorizationResourceTypeEnum.Vector, AuthorizationResourceTypeEnum.Query, AuthorizationResourceTypeEnum.Transaction);

            RoleDefinition graphAdmin = AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.GraphAdmin);
            AssertEqual(AuthorizationResourceScopeEnum.Graph, graphAdmin.ResourceScope, "GraphAdmin scope");
            AssertFalse(graphAdmin.InheritsToGraphs, "GraphAdmin does not inherit beyond assigned graph");
            AssertRoleHasPermissions(graphAdmin, AuthorizationPermissionEnum.Read, AuthorizationPermissionEnum.Write, AuthorizationPermissionEnum.Delete, AuthorizationPermissionEnum.Admin);
            AssertRoleAppliesTo(graphAdmin, AuthorizationResourceTypeEnum.Graph, AuthorizationResourceTypeEnum.Node, AuthorizationResourceTypeEnum.Edge, AuthorizationResourceTypeEnum.Label, AuthorizationResourceTypeEnum.Tag, AuthorizationResourceTypeEnum.Vector, AuthorizationResourceTypeEnum.Query, AuthorizationResourceTypeEnum.Transaction);
            AssertFalse(graphAdmin.AppliesTo(AuthorizationResourceTypeEnum.Admin), "GraphAdmin does not apply to tenant/server admin resources");

            RoleDefinition editor = AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Editor);
            AssertEqual(AuthorizationResourceScopeEnum.Graph, editor.ResourceScope, "Editor scope");
            AssertRoleHasPermissions(editor, AuthorizationPermissionEnum.Read, AuthorizationPermissionEnum.Write, AuthorizationPermissionEnum.Delete);
            AssertFalse(editor.HasPermission(AuthorizationPermissionEnum.Admin), "Editor does not grant admin");
            AssertRoleAppliesTo(editor, AuthorizationResourceTypeEnum.Graph, AuthorizationResourceTypeEnum.Node, AuthorizationResourceTypeEnum.Edge, AuthorizationResourceTypeEnum.Label, AuthorizationResourceTypeEnum.Tag, AuthorizationResourceTypeEnum.Vector, AuthorizationResourceTypeEnum.Query, AuthorizationResourceTypeEnum.Transaction);

            RoleDefinition viewer = AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Viewer);
            AssertEqual(AuthorizationResourceScopeEnum.Graph, viewer.ResourceScope, "Viewer scope");
            AssertRoleHasPermissions(viewer, AuthorizationPermissionEnum.Read);
            AssertFalse(viewer.HasPermission(AuthorizationPermissionEnum.Write), "Viewer does not grant write");
            AssertFalse(viewer.HasPermission(AuthorizationPermissionEnum.Delete), "Viewer does not grant delete");
            AssertFalse(viewer.HasPermission(AuthorizationPermissionEnum.Admin), "Viewer does not grant admin");
            AssertRoleAppliesTo(viewer, AuthorizationResourceTypeEnum.Graph, AuthorizationResourceTypeEnum.Node, AuthorizationResourceTypeEnum.Edge, AuthorizationResourceTypeEnum.Label, AuthorizationResourceTypeEnum.Tag, AuthorizationResourceTypeEnum.Vector, AuthorizationResourceTypeEnum.Query);
            AssertFalse(viewer.AppliesTo(AuthorizationResourceTypeEnum.Transaction), "Viewer does not apply to mutating transactions");
            AssertFalse(viewer.AppliesTo(AuthorizationResourceTypeEnum.Admin), "Viewer does not apply to tenant/server admin resources");

            RoleDefinition custom = AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Custom);
            AssertEqual(AuthorizationResourceScopeEnum.Graph, custom.ResourceScope, "Custom default scope");
            AssertEqual(0, custom.Permissions.Count, "Custom built-in template has no predefined permissions");
            AssertEqual(0, custom.ResourceTypes.Count, "Custom built-in template has no predefined resource types");

            RoleDefinition cloned = AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Viewer);
            cloned.Permissions.Add(AuthorizationPermissionEnum.Admin);
            RoleDefinition fresh = AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Viewer);
            AssertFalse(fresh.HasPermission(AuthorizationPermissionEnum.Admin), "Built-in role definitions are clone-protected");

            AssertTrue(AuthorizationPolicyDefinitions.ExistingUsersReceiveTenantAdminEquivalentAccess, "Existing user migration default");
            AssertTrue(AuthorizationPolicyDefinitions.EmptyCredentialScopesPreserveFullAccess, "Empty credential scopes preserve access");
            AssertTrue(AuthorizationPolicyDefinitions.EmptyCredentialGraphAllowListsPreserveTenantGraphAccess, "Empty credential graph allow-lists preserve graph access");
            AssertTrue(AuthorizationPolicyDefinitions.AdminBearerTokenRemainsAdmin, "Admin bearer token migration default");
            AssertTrue(AuthorizationPolicyDefinitions.ExternalIdentityMappingIsOutOfScope, "External identity boundary default");

            AssertEqual(4, Enum.GetValues(typeof(AuthorizationPermissionEnum)).Length, "Permission enum count");
            AssertEqual(2, Enum.GetValues(typeof(AuthorizationResourceScopeEnum)).Length, "Resource scope enum count");
            AssertEqual(9, Enum.GetValues(typeof(AuthorizationResourceTypeEnum)).Length, "Resource type enum count");

            return Task.CompletedTask;
        }

        private static async Task TestAuthorizationPermissionMatrix(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (RequestTypeEnum requestType in Enum.GetValues(typeof(RequestTypeEnum)))
            {
                if (!AuthorizationService.IsAdministrativeRequest(requestType)) continue;
                AssertRequestAuthorization(requestType, "admin", AuthorizationPermissionEnum.Admin, AuthorizationResourceTypeEnum.Admin);
            }

            (RequestTypeEnum RequestType, string Scope, AuthorizationPermissionEnum Permission, AuthorizationResourceTypeEnum ResourceType)[] requestExpectations =
            {
                (RequestTypeEnum.TenantUpdate, "admin", AuthorizationPermissionEnum.Admin, AuthorizationResourceTypeEnum.Admin),
                (RequestTypeEnum.AuthorizationRoleReadAll, "admin", AuthorizationPermissionEnum.Admin, AuthorizationResourceTypeEnum.Admin),
                (RequestTypeEnum.UserRoleAssignmentCreate, "admin", AuthorizationPermissionEnum.Admin, AuthorizationResourceTypeEnum.Admin),
                (RequestTypeEnum.CredentialScopeAssignmentDelete, "admin", AuthorizationPermissionEnum.Admin, AuthorizationResourceTypeEnum.Admin),
                (RequestTypeEnum.UserEffectivePermissionsRead, "admin", AuthorizationPermissionEnum.Admin, AuthorizationResourceTypeEnum.Admin),
                (RequestTypeEnum.GraphRead, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Graph),
                (RequestTypeEnum.GraphCreate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Graph),
                (RequestTypeEnum.GraphUpdate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Graph),
                (RequestTypeEnum.GraphDelete, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Graph),
                (RequestTypeEnum.GraphExistence, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Graph),
                (RequestTypeEnum.GraphQuery, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Query),
                (RequestTypeEnum.GraphTransaction, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Transaction),
                (RequestTypeEnum.NodeRead, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeReadAll, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeReadAllInTenant, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeReadAllInGraph, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeEnumerate, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeReadFirst, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeSearch, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeParents, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeChildren, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeNeighbors, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeReadMostConnected, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeReadLeastConnected, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.GetRoutes, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeCreate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeCreateMany, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeUpdate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeDelete, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeDeleteAll, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeDeleteAllInTenant, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.NodeDeleteMany, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Node),
                (RequestTypeEnum.EdgeRead, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeReadAll, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeReadAllInTenant, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeReadAllInGraph, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeEnumerate, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeReadMany, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeSearch, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeBetween, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeExists, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgesFromNode, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgesToNode, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.AllEdgesToNode, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeCreate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeCreateMany, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeUpdate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeDelete, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeDeleteAll, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeDeleteAllInTenant, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeDeleteMany, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeDeleteNodeEdges, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.EdgeDeleteNodeEdgesMany, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Edge),
                (RequestTypeEnum.LabelRead, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelReadAll, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelReadAllInTenant, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelReadAllInGraph, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelEnumerate, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelReadManyGraph, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelReadManyNode, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelReadManyEdge, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelExists, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelCreate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelCreateMany, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelUpdate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDelete, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDeleteMany, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDeleteAllInTenant, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDeleteAllInGraph, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDeleteGraphLabels, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDeleteNodeLabels, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.LabelDeleteEdgeLabels, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Label),
                (RequestTypeEnum.TagRead, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Tag),
                (RequestTypeEnum.TagCreate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Tag),
                (RequestTypeEnum.TagUpdate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Tag),
                (RequestTypeEnum.TagDelete, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Tag),
                (RequestTypeEnum.VectorRead, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Vector),
                (RequestTypeEnum.VectorSearch, "read", AuthorizationPermissionEnum.Read, AuthorizationResourceTypeEnum.Vector),
                (RequestTypeEnum.VectorCreate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Vector),
                (RequestTypeEnum.VectorUpdate, "write", AuthorizationPermissionEnum.Write, AuthorizationResourceTypeEnum.Vector),
                (RequestTypeEnum.VectorDelete, "write", AuthorizationPermissionEnum.Delete, AuthorizationResourceTypeEnum.Vector)
            };

            foreach ((RequestTypeEnum RequestType, string Scope, AuthorizationPermissionEnum Permission, AuthorizationResourceTypeEnum ResourceType) expectation in requestExpectations)
            {
                AssertRequestAuthorization(expectation.RequestType, expectation.Scope, expectation.Permission, expectation.ResourceType);
            }

            AuthorizationPermissionEnum[] allPermissions = Enum.GetValues(typeof(AuthorizationPermissionEnum)).Cast<AuthorizationPermissionEnum>().ToArray();
            AuthorizationResourceTypeEnum[] allResourceTypes = Enum.GetValues(typeof(AuthorizationResourceTypeEnum)).Cast<AuthorizationResourceTypeEnum>().ToArray();
            AuthorizationResourceTypeEnum[] graphResourceTypes = allResourceTypes
                .Where(resourceType => resourceType != AuthorizationResourceTypeEnum.Admin)
                .ToArray();
            AuthorizationResourceTypeEnum[] readOnlyGraphResourceTypes = graphResourceTypes
                .Where(resourceType => resourceType != AuthorizationResourceTypeEnum.Transaction)
                .ToArray();

            AssertRoleMatrix(
                AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.TenantAdmin),
                true,
                allPermissions,
                allResourceTypes);
            AssertRoleMatrix(
                AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.GraphAdmin),
                false,
                allPermissions,
                graphResourceTypes);
            AssertRoleMatrix(
                AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Editor),
                false,
                new[]
                {
                    AuthorizationPermissionEnum.Read,
                    AuthorizationPermissionEnum.Write,
                    AuthorizationPermissionEnum.Delete
                },
                graphResourceTypes);
            AssertRoleMatrix(
                AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Viewer),
                false,
                new[]
                {
                    AuthorizationPermissionEnum.Read
                },
                readOnlyGraphResourceTypes);
            AssertRoleMatrix(
                AuthorizationPolicyDefinitions.GetBuiltInRole(BuiltInRoleEnum.Custom),
                false,
                Array.Empty<AuthorizationPermissionEnum>(),
                Array.Empty<AuthorizationResourceTypeEnum>());

            string filename = "test-improvements-authorization-permission-matrix.db";
            DeleteFileIfExists(filename);

            Guid tenantGuid = Guid.NewGuid();
            Guid graphA = Guid.NewGuid();
            Guid graphB = Guid.NewGuid();
            Guid tenantAdminUser = Guid.NewGuid();
            Guid viewerUser = Guid.NewGuid();
            Guid editorUser = Guid.NewGuid();
            Guid graphAdminUser = Guid.NewGuid();
            Guid unassignedUser = Guid.NewGuid();

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            }))
            {
                repo.InitializeRepository();
                AuthorizationService service = new AuthorizationService(new LoggingModule(), repo);

                await AssertUserAccess(service, tenantGuid, unassignedUser, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.NoCredential, "Unassigned user keeps read compatibility access", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, unassignedUser, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.NoCredential, "Unassigned user keeps write compatibility access", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, unassignedUser, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Unassigned user cannot manage authorization roles", cancellationToken).ConfigureAwait(false);

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = tenantAdminUser,
                    RoleName = AuthorizationPolicyDefinitions.TenantAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Tenant
                }, cancellationToken).ConfigureAwait(false);

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = viewerUser,
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = editorUser,
                    RoleName = AuthorizationPolicyDefinitions.EditorRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = graphAdminUser,
                    RoleName = AuthorizationPolicyDefinitions.GraphAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, tenantAdminUser, null, RequestTypeEnum.TenantUpdate, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "TenantAdmin can update tenant metadata", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, tenantAdminUser, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "TenantAdmin can manage authorization roles", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, tenantAdminUser, graphB, RequestTypeEnum.NodeDelete, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "TenantAdmin inherits delete access to tenant graphs", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, tenantAdminUser, graphB, RequestTypeEnum.GraphTransaction, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "TenantAdmin can run graph transactions", cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, viewerUser, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Viewer can read nodes", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, viewerUser, graphA, RequestTypeEnum.GraphQuery, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Viewer can execute read query requests", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, viewerUser, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Viewer cannot create nodes", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, viewerUser, graphA, RequestTypeEnum.GraphTransaction, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Viewer cannot run graph transactions", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, viewerUser, graphB, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.GraphDenied, "Viewer cannot read another graph", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, viewerUser, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Viewer cannot manage authorization roles", cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, editorUser, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Editor can create nodes", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, editorUser, graphA, RequestTypeEnum.NodeDelete, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Editor can delete nodes", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, editorUser, graphA, RequestTypeEnum.GraphTransaction, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Editor can run graph transactions", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, editorUser, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Editor cannot manage authorization roles", cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, graphAdminUser, graphA, RequestTypeEnum.GraphTransaction, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "GraphAdmin can run graph transactions", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, graphAdminUser, graphA, RequestTypeEnum.NodeDelete, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "GraphAdmin can delete graph data", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, graphAdminUser, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "GraphAdmin cannot manage authorization roles", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, graphAdminUser, null, RequestTypeEnum.TenantUpdate, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "GraphAdmin cannot update tenant metadata", cancellationToken).ConfigureAwait(false);

                Credential viewerCredential = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid()
                };

                await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    TenantGUID = tenantGuid,
                    CredentialGUID = viewerCredential.GUID,
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                await AssertCredentialAccess(service, viewerCredential, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Viewer credential can read nodes", cancellationToken).ConfigureAwait(false);
                await AssertCredentialAccess(service, viewerCredential, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Viewer credential cannot create nodes", cancellationToken).ConfigureAwait(false);

                Credential directQueryWriter = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid(),
                    Scopes = new List<string>
                    {
                        "read",
                        "write"
                    }
                };

                await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    TenantGUID = tenantGuid,
                    CredentialGUID = directQueryWriter.GUID,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Query
                    }
                }, cancellationToken).ConfigureAwait(false);

                await AssertCredentialAccess(service, directQueryWriter, graphA, "write", AuthorizationResourceTypeEnum.Query, RequestTypeEnum.GraphQuery, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Direct credential permission can run mutation query", cancellationToken).ConfigureAwait(false);
                await AssertCredentialAccess(service, directQueryWriter, graphA, RequestTypeEnum.GraphTransaction, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Direct query writer cannot run transactions", cancellationToken).ConfigureAwait(false);
            }

            DeleteFileIfExists(filename);
        }

        private static void AssertRequestAuthorization(
            RequestTypeEnum requestType,
            string expectedScope,
            AuthorizationPermissionEnum expectedPermission,
            AuthorizationResourceTypeEnum expectedResourceType)
        {
            AssertEqual(expectedScope, AuthorizationService.RequiredScope(requestType), requestType + " required scope");
            AssertEqual(expectedPermission, AuthorizationService.RequiredPermission(requestType), requestType + " required permission");
            AssertEqual(expectedResourceType, AuthorizationService.RequiredResourceType(requestType), requestType + " required resource type");
        }

        private static void AssertRoleMatrix(
            RoleDefinition role,
            bool expectedInheritsToGraphs,
            IEnumerable<AuthorizationPermissionEnum> expectedPermissions,
            IEnumerable<AuthorizationResourceTypeEnum> expectedResourceTypes)
        {
            AssertNotNull(role, "Role matrix definition");

            HashSet<AuthorizationPermissionEnum> permissions = new HashSet<AuthorizationPermissionEnum>(expectedPermissions);
            HashSet<AuthorizationResourceTypeEnum> resourceTypes = new HashSet<AuthorizationResourceTypeEnum>(expectedResourceTypes);

            AssertEqual(expectedInheritsToGraphs, role.InheritsToGraphs, role.Name + " inheritance flag");
            AssertEqual(permissions.Count, role.Permissions.Count, role.Name + " permission count");
            AssertEqual(resourceTypes.Count, role.ResourceTypes.Count, role.Name + " resource type count");

            foreach (AuthorizationPermissionEnum permission in Enum.GetValues(typeof(AuthorizationPermissionEnum)))
            {
                AssertEqual(permissions.Contains(permission), role.HasPermission(permission), role.Name + " permission " + permission);
            }

            foreach (AuthorizationResourceTypeEnum resourceType in Enum.GetValues(typeof(AuthorizationResourceTypeEnum)))
            {
                AssertEqual(resourceTypes.Contains(resourceType), role.AppliesTo(resourceType), role.Name + " resource type " + resourceType);
            }
        }

        private static async Task AssertUserAccess(
            AuthorizationService service,
            Guid tenantGuid,
            Guid userGuid,
            Guid? graphGuid,
            RequestTypeEnum requestType,
            AuthorizationResultEnum expectedResult,
            AuthorizationDecisionReason expectedReason,
            string message,
            CancellationToken cancellationToken)
        {
            AuthorizationDecision decision = await service.EvaluateUserEffectiveAccess(
                tenantGuid,
                userGuid,
                graphGuid,
                requestType,
                cancellationToken).ConfigureAwait(false);

            AssertEqual(expectedResult, decision.Result, message);
            AssertEqual(expectedReason, decision.Reason, message + " reason");
        }

        private static async Task AssertCredentialAccess(
            AuthorizationService service,
            Credential credential,
            Guid? graphGuid,
            RequestTypeEnum requestType,
            AuthorizationResultEnum expectedResult,
            AuthorizationDecisionReason expectedReason,
            string message,
            CancellationToken cancellationToken)
        {
            AuthorizationDecision decision = await service.EvaluateCredentialEffectiveAccess(
                credential,
                graphGuid,
                requestType,
                cancellationToken).ConfigureAwait(false);

            AssertEqual(expectedResult, decision.Result, message);
            AssertEqual(expectedReason, decision.Reason, message + " reason");
        }

        private static async Task AssertCredentialAccess(
            AuthorizationService service,
            Credential credential,
            Guid? graphGuid,
            string requiredScope,
            AuthorizationResourceTypeEnum resourceType,
            RequestTypeEnum requestType,
            AuthorizationResultEnum expectedResult,
            AuthorizationDecisionReason expectedReason,
            string message,
            CancellationToken cancellationToken)
        {
            AuthorizationDecision decision = await service.EvaluateCredentialEffectiveAccess(
                credential,
                graphGuid,
                requiredScope,
                resourceType,
                requestType,
                cancellationToken).ConfigureAwait(false);

            AssertEqual(expectedResult, decision.Result, message);
            AssertEqual(expectedReason, decision.Reason, message + " reason");
        }

        private static void AssertRoleHasPermissions(RoleDefinition role, params AuthorizationPermissionEnum[] permissions)
        {
            foreach (AuthorizationPermissionEnum permission in permissions)
            {
                AssertTrue(role.HasPermission(permission), role.Name + " has permission " + permission);
            }
        }

        private static void AssertRoleAppliesTo(RoleDefinition role, params AuthorizationResourceTypeEnum[] resourceTypes)
        {
            foreach (AuthorizationResourceTypeEnum resourceType in resourceTypes)
            {
                AssertTrue(role.AppliesTo(resourceType), role.Name + " applies to " + resourceType);
            }
        }

        private static async Task TestAuthorizationRoleStorage(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-authorization-roles.db";
            DeleteFileIfExists(filename);

            Guid tenantA = Guid.NewGuid();
            Guid tenantB = Guid.NewGuid();
            Guid graphA = Guid.NewGuid();
            Guid graphB = Guid.NewGuid();
            Guid userA = Guid.NewGuid();
            Guid userB = Guid.NewGuid();
            Guid userC = Guid.NewGuid();
            Guid credentialA = Guid.NewGuid();
            Guid credentialB = Guid.NewGuid();
            Guid credentialC = Guid.NewGuid();
            DateTime baseTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();

                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = true }, 5L, "Seeded built-in role count", cancellationToken).ConfigureAwait(false);

                AuthorizationRole createdEditor = await client.AuthorizationRoles.ReadRoleByName(null, AuthorizationPolicyDefinitions.EditorRoleName, cancellationToken).ConfigureAwait(false);
                AssertNotNull(createdEditor, "Seeded Editor role");

                AuthorizationRole custom = new AuthorizationRole
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantA,
                    Name = "DataCurator",
                    DisplayName = "Data Curator",
                    Description = "Can read and write selected graph data.",
                    BuiltIn = false,
                    BuiltInRole = BuiltInRoleEnum.Custom,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read,
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Node,
                        AuthorizationResourceTypeEnum.Tag
                    },
                    InheritsToGraphs = false,
                    CreatedUtc = baseTime.AddMinutes(2),
                    LastUpdateUtc = baseTime.AddMinutes(2)
                };

                AuthorizationRole auditor = new AuthorizationRole
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantB,
                    Name = "AuditViewer",
                    DisplayName = "Audit Viewer",
                    Description = "Can inspect graph query surfaces.",
                    BuiltIn = false,
                    BuiltInRole = BuiltInRoleEnum.Custom,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Graph,
                        AuthorizationResourceTypeEnum.Query
                    },
                    CreatedUtc = baseTime.AddMinutes(3),
                    LastUpdateUtc = baseTime.AddMinutes(3)
                };

                AuthorizationRole createdCustom = await client.AuthorizationRoles.CreateRole(custom, cancellationToken).ConfigureAwait(false);
                AuthorizationRole createdAuditor = await client.AuthorizationRoles.CreateRole(auditor, cancellationToken).ConfigureAwait(false);

                AuthorizationRole readEditor = await client.AuthorizationRoles.ReadRoleByGuid(createdEditor.GUID, cancellationToken).ConfigureAwait(false);
                AssertNotNull(readEditor, "Role read by GUID");
                AssertEqual(createdEditor.GUID, readEditor.GUID, "Role GUID round trip");
                AssertEqual((Guid?)null, readEditor.TenantGUID, "Global role tenant round trip");
                AssertEqual(AuthorizationPolicyDefinitions.EditorRoleName, readEditor.Name, "Role name round trip");
                AssertEqual(BuiltInRoleEnum.Editor, readEditor.BuiltInRole, "Built-in role enum round trip");
                AssertTrue(readEditor.BuiltIn, "Built-in role flag round trip");
                AssertTrue(readEditor.HasPermission(AuthorizationPermissionEnum.Delete), "Stored role permission helper");
                AssertTrue(readEditor.AppliesTo(AuthorizationResourceTypeEnum.Transaction), "Stored role resource helper");

                AuthorizationRole readCustomByName = await client.AuthorizationRoles.ReadRoleByName(tenantA, "DataCurator", cancellationToken).ConfigureAwait(false);
                AssertNotNull(readCustomByName, "Tenant role read by name");
                AssertEqual(createdCustom.GUID, readCustomByName.GUID, "Tenant role name lookup GUID");
                AuthorizationRole readGlobalByName = await client.AuthorizationRoles.ReadRoleByName(null, AuthorizationPolicyDefinitions.EditorRoleName, cancellationToken).ConfigureAwait(false);
                AssertNotNull(readGlobalByName, "Global role read by name");
                AssertEqual(createdEditor.GUID, readGlobalByName.GUID, "Global role name lookup GUID");

                AuthorizationRoleSearchResult rolePage = await client.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest
                {
                    BuiltIn = false,
                    PageSize = 2
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(2L, rolePage.TotalCount, "Custom role total count");
                AssertEqual(1, rolePage.TotalPages, "Custom role total pages");
                AssertEqual(2, rolePage.Objects.Count, "Role first page size");
                AssertEqual(createdAuditor.GUID, rolePage.Objects[0].GUID, "Role ordering newest first");

                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { TenantGUID = tenantA }, 1L, "Role tenant filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { Name = "DataCurator" }, 1L, "Role name filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = true }, 5L, "Role built-in filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltInRole = BuiltInRoleEnum.Editor }, 1L, "Role built-in enum filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false, ResourceScope = AuthorizationResourceScopeEnum.Graph }, 2L, "Role resource scope filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false, Permission = AuthorizationPermissionEnum.Write }, 1L, "Role permission filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false, ResourceType = AuthorizationResourceTypeEnum.Tag }, 1L, "Role resource type filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false, FromUtc = baseTime.AddMinutes(2), ToUtc = baseTime.AddMinutes(4) }, 2L, "Role time-window filter", cancellationToken).ConfigureAwait(false);

                createdCustom.DisplayName = "Data Curator Updated";
                createdCustom.Description = "Can read, write, and delete selected graph data.";
                createdCustom.Permissions.Add(AuthorizationPermissionEnum.Delete);
                createdCustom.ResourceTypes.Add(AuthorizationResourceTypeEnum.Edge);
                AuthorizationRole updatedCustom = await client.AuthorizationRoles.UpdateRole(createdCustom, cancellationToken).ConfigureAwait(false);
                AssertEqual("Data Curator Updated", updatedCustom.DisplayName, "Role update display name");
                AssertTrue(updatedCustom.HasPermission(AuthorizationPermissionEnum.Delete), "Role update permissions");
                AssertTrue(updatedCustom.AppliesTo(AuthorizationResourceTypeEnum.Edge), "Role update resource types");
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false, Permission = AuthorizationPermissionEnum.Delete }, 1L, "Role updated permission filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false, ResourceType = AuthorizationResourceTypeEnum.Edge }, 1L, "Role updated resource type filter", cancellationToken).ConfigureAwait(false);

                UserRoleAssignment userCurator = await client.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantA,
                    UserGUID = userA,
                    RoleGUID = updatedCustom.GUID,
                    RoleName = updatedCustom.Name,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA,
                    CreatedUtc = baseTime.AddMinutes(10),
                    LastUpdateUtc = baseTime.AddMinutes(10)
                }, cancellationToken).ConfigureAwait(false);

                UserRoleAssignment userEditor = await client.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantA,
                    UserGUID = userB,
                    RoleGUID = createdEditor.GUID,
                    RoleName = createdEditor.Name,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphB,
                    CreatedUtc = baseTime.AddMinutes(11),
                    LastUpdateUtc = baseTime.AddMinutes(11)
                }, cancellationToken).ConfigureAwait(false);

                UserRoleAssignment userTenantAdmin = await client.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantB,
                    UserGUID = userC,
                    RoleGUID = null,
                    RoleName = AuthorizationPolicyDefinitions.TenantAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Tenant,
                    GraphGUID = null,
                    CreatedUtc = baseTime.AddMinutes(12),
                    LastUpdateUtc = baseTime.AddMinutes(12)
                }, cancellationToken).ConfigureAwait(false);

                UserRoleAssignment readUserRole = await client.AuthorizationRoles.ReadUserRoleByGuid(userCurator.GUID, cancellationToken).ConfigureAwait(false);
                AssertNotNull(readUserRole, "User role read by GUID");
                AssertEqual(userCurator.TenantGUID, readUserRole.TenantGUID, "User role tenant round trip");
                AssertEqual(userCurator.UserGUID, readUserRole.UserGUID, "User role user round trip");
                AssertEqual(userCurator.RoleGUID, readUserRole.RoleGUID, "User role role GUID round trip");
                AssertEqual(userCurator.RoleName, readUserRole.RoleName, "User role name round trip");
                AssertEqual(userCurator.ResourceScope, readUserRole.ResourceScope, "User role scope round trip");
                AssertEqual(userCurator.GraphGUID, readUserRole.GraphGUID, "User role graph round trip");

                UserRoleAssignmentSearchResult userRolePage = await client.AuthorizationRoles.SearchUserRoles(new UserRoleAssignmentSearchRequest
                {
                    PageSize = 2
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(3L, userRolePage.TotalCount, "User role total count");
                AssertEqual(2, userRolePage.TotalPages, "User role total pages");
                AssertEqual(2, userRolePage.Objects.Count, "User role first page size");
                AssertEqual(userTenantAdmin.GUID, userRolePage.Objects[0].GUID, "User role ordering newest first");

                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { TenantGUID = tenantA }, 2L, "User role tenant filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { UserGUID = userA }, 1L, "User role user filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { RoleGUID = updatedCustom.GUID }, 1L, "User role role GUID filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { RoleName = createdEditor.Name }, 1L, "User role role name filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { ResourceScope = AuthorizationResourceScopeEnum.Graph }, 2L, "User role graph scope filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { GraphGUID = graphB }, 1L, "User role graph filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { FromUtc = baseTime.AddMinutes(10), ToUtc = baseTime.AddMinutes(12) }, 2L, "User role time-window filter", cancellationToken).ConfigureAwait(false);

                userCurator.GraphGUID = graphB;
                userCurator.RoleName = "DataCuratorUpdated";
                UserRoleAssignment updatedUserRole = await client.AuthorizationRoles.UpdateUserRole(userCurator, cancellationToken).ConfigureAwait(false);
                AssertEqual((Guid?)graphB, updatedUserRole.GraphGUID, "User role update graph");
                AssertEqual("DataCuratorUpdated", updatedUserRole.RoleName, "User role update role name");
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { GraphGUID = graphB }, 2L, "User role updated graph filter", cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { RoleName = "DataCuratorUpdated" }, 1L, "User role updated name filter", cancellationToken).ConfigureAwait(false);

                CredentialScopeAssignment credentialCurator = await client.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantA,
                    CredentialGUID = credentialA,
                    RoleGUID = updatedCustom.GUID,
                    RoleName = updatedCustom.Name,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read,
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Node,
                        AuthorizationResourceTypeEnum.Tag
                    },
                    CreatedUtc = baseTime.AddMinutes(20),
                    LastUpdateUtc = baseTime.AddMinutes(20)
                }, cancellationToken).ConfigureAwait(false);

                CredentialScopeAssignment credentialEditor = await client.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantA,
                    CredentialGUID = credentialB,
                    RoleGUID = createdEditor.GUID,
                    RoleName = createdEditor.Name,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphB,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Graph,
                        AuthorizationResourceTypeEnum.Query
                    },
                    CreatedUtc = baseTime.AddMinutes(21),
                    LastUpdateUtc = baseTime.AddMinutes(21)
                }, cancellationToken).ConfigureAwait(false);

                CredentialScopeAssignment credentialTenantAdmin = await client.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantB,
                    CredentialGUID = credentialC,
                    RoleGUID = null,
                    RoleName = AuthorizationPolicyDefinitions.TenantAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Tenant,
                    GraphGUID = null,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read,
                        AuthorizationPermissionEnum.Write,
                        AuthorizationPermissionEnum.Delete,
                        AuthorizationPermissionEnum.Admin
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Admin,
                        AuthorizationResourceTypeEnum.Graph
                    },
                    CreatedUtc = baseTime.AddMinutes(22),
                    LastUpdateUtc = baseTime.AddMinutes(22)
                }, cancellationToken).ConfigureAwait(false);

                CredentialScopeAssignment readCredentialScope = await client.AuthorizationRoles.ReadCredentialScopeByGuid(credentialCurator.GUID, cancellationToken).ConfigureAwait(false);
                AssertNotNull(readCredentialScope, "Credential scope read by GUID");
                AssertEqual(credentialCurator.TenantGUID, readCredentialScope.TenantGUID, "Credential scope tenant round trip");
                AssertEqual(credentialCurator.CredentialGUID, readCredentialScope.CredentialGUID, "Credential scope credential round trip");
                AssertEqual(credentialCurator.RoleGUID, readCredentialScope.RoleGUID, "Credential scope role GUID round trip");
                AssertEqual(credentialCurator.RoleName, readCredentialScope.RoleName, "Credential scope role name round trip");
                AssertEqual(credentialCurator.ResourceScope, readCredentialScope.ResourceScope, "Credential scope resource scope round trip");
                AssertEqual(credentialCurator.GraphGUID, readCredentialScope.GraphGUID, "Credential scope graph round trip");
                AssertEqual(2, readCredentialScope.Permissions.Count, "Credential scope permissions round trip");
                AssertEqual(2, readCredentialScope.ResourceTypes.Count, "Credential scope resource types round trip");

                CredentialScopeAssignmentSearchResult credentialScopePage = await client.AuthorizationRoles.SearchCredentialScopes(new CredentialScopeAssignmentSearchRequest
                {
                    PageSize = 2
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(3L, credentialScopePage.TotalCount, "Credential scope total count");
                AssertEqual(2, credentialScopePage.TotalPages, "Credential scope total pages");
                AssertEqual(2, credentialScopePage.Objects.Count, "Credential scope first page size");
                AssertEqual(credentialTenantAdmin.GUID, credentialScopePage.Objects[0].GUID, "Credential scope ordering newest first");

                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { TenantGUID = tenantA }, 2L, "Credential scope tenant filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { CredentialGUID = credentialA }, 1L, "Credential scope credential filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { RoleGUID = updatedCustom.GUID }, 1L, "Credential scope role GUID filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { RoleName = createdEditor.Name }, 1L, "Credential scope role name filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { ResourceScope = AuthorizationResourceScopeEnum.Graph }, 2L, "Credential scope graph scope filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { GraphGUID = graphB }, 1L, "Credential scope graph filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { Permission = AuthorizationPermissionEnum.Write }, 2L, "Credential scope permission filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { ResourceType = AuthorizationResourceTypeEnum.Tag }, 1L, "Credential scope resource type filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { FromUtc = baseTime.AddMinutes(20), ToUtc = baseTime.AddMinutes(22) }, 2L, "Credential scope time-window filter", cancellationToken).ConfigureAwait(false);

                credentialCurator.GraphGUID = graphB;
                credentialCurator.RoleName = "DataCuratorUpdated";
                credentialCurator.Permissions.Add(AuthorizationPermissionEnum.Delete);
                credentialCurator.ResourceTypes.Add(AuthorizationResourceTypeEnum.Edge);
                CredentialScopeAssignment updatedCredentialScope = await client.AuthorizationRoles.UpdateCredentialScope(credentialCurator, cancellationToken).ConfigureAwait(false);
                AssertEqual((Guid?)graphB, updatedCredentialScope.GraphGUID, "Credential scope update graph");
                AssertEqual("DataCuratorUpdated", updatedCredentialScope.RoleName, "Credential scope update role name");
                AssertTrue(updatedCredentialScope.Permissions.Contains(AuthorizationPermissionEnum.Delete), "Credential scope update permissions");
                AssertTrue(updatedCredentialScope.ResourceTypes.Contains(AuthorizationResourceTypeEnum.Edge), "Credential scope update resource types");
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { GraphGUID = graphB }, 2L, "Credential scope updated graph filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { Permission = AuthorizationPermissionEnum.Delete }, 2L, "Credential scope updated permission filter", cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { ResourceType = AuthorizationResourceTypeEnum.Edge }, 1L, "Credential scope updated resource type filter", cancellationToken).ConfigureAwait(false);

                await client.AuthorizationRoles.DeleteUserRoleByGuid(userEditor.GUID, cancellationToken).ConfigureAwait(false);
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest { TenantGUID = tenantA }, 1L, "User role delete by GUID", cancellationToken).ConfigureAwait(false);
                int userTenantDeleted = await client.AuthorizationRoles.DeleteUserRoles(new UserRoleAssignmentSearchRequest { TenantGUID = tenantB }, cancellationToken).ConfigureAwait(false);
                AssertEqual(1, userTenantDeleted, "User role delete many count");
                int userRemainingDeleted = await client.AuthorizationRoles.DeleteUserRoles(new UserRoleAssignmentSearchRequest { TenantGUID = tenantA }, cancellationToken).ConfigureAwait(false);
                AssertEqual(1, userRemainingDeleted, "User role delete remaining count");
                await AssertUserRoleCount(client, new UserRoleAssignmentSearchRequest(), 0L, "User role count after deletes", cancellationToken).ConfigureAwait(false);

                await client.AuthorizationRoles.DeleteCredentialScopeByGuid(credentialEditor.GUID, cancellationToken).ConfigureAwait(false);
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest { TenantGUID = tenantA }, 1L, "Credential scope delete by GUID", cancellationToken).ConfigureAwait(false);
                int credentialTenantDeleted = await client.AuthorizationRoles.DeleteCredentialScopes(new CredentialScopeAssignmentSearchRequest { TenantGUID = tenantB }, cancellationToken).ConfigureAwait(false);
                AssertEqual(1, credentialTenantDeleted, "Credential scope delete many count");
                int credentialRemainingDeleted = await client.AuthorizationRoles.DeleteCredentialScopes(new CredentialScopeAssignmentSearchRequest { TenantGUID = tenantA }, cancellationToken).ConfigureAwait(false);
                AssertEqual(1, credentialRemainingDeleted, "Credential scope delete remaining count");
                await AssertCredentialScopeCount(client, new CredentialScopeAssignmentSearchRequest(), 0L, "Credential scope count after deletes", cancellationToken).ConfigureAwait(false);

                await client.AuthorizationRoles.DeleteRoleByGuid(createdAuditor.GUID, cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false }, 1L, "Role delete by GUID", cancellationToken).ConfigureAwait(false);
                await client.AuthorizationRoles.DeleteRoleByGuid(updatedCustom.GUID, cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = false }, 0L, "Custom role count after deletes", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationRoleCount(client, new AuthorizationRoleSearchRequest { BuiltIn = true }, 5L, "Built-in roles remain after custom deletes", cancellationToken).ConfigureAwait(false);
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestAuthorizationMigrationCompatibility(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-authorization-migration.db";
            DeleteFileIfExists(filename);

            Guid tenantGuid = Guid.NewGuid();
            Guid userGuid = Guid.NewGuid();
            Guid credentialGuid = Guid.NewGuid();
            Guid graphGuid = Guid.NewGuid();
            string bearerToken = "legacy-" + Guid.NewGuid().ToString("N");
            DateTime timestamp = DateTime.UtcNow;

            CreateLegacyCredentialDatabase(filename, tenantGuid, userGuid, credentialGuid, bearerToken, timestamp);
            AssertTrue(!SqliteColumnExists(filename, "creds", "scopes"), "Legacy credential table starts without scopes column");
            AssertTrue(!SqliteColumnExists(filename, "creds", "graphguids"), "Legacy credential table starts without graph allow-list column");
            AssertTrue(!SqliteTableExists(filename, "authorizationroles"), "Legacy database starts without authorization role table");

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            }))
            {
                repo.InitializeRepository();

                AssertTrue(SqliteColumnExists(filename, "creds", "scopes"), "Migration adds credential scopes column");
                AssertTrue(SqliteColumnExists(filename, "creds", "graphguids"), "Migration adds credential graph allow-list column");
                AssertTrue(SqliteTableExists(filename, "authorizationroles"), "Migration creates authorization role table");
                AssertTrue(SqliteTableExists(filename, "userroleassignments"), "Migration creates user-role assignment table");
                AssertTrue(SqliteTableExists(filename, "credentialscopeassignments"), "Migration creates credential-scope assignment table");
                AssertTrue(SqliteTableExists(filename, "authorizationaudit"), "Migration creates authorization audit table");

                Credential migrated = await repo.Credential.ReadByBearerToken(bearerToken, cancellationToken).ConfigureAwait(false);
                AssertNotNull(migrated, "Legacy credential remains readable after migration");
                AssertEqual(credentialGuid, migrated.GUID, "Legacy credential GUID is preserved");
                AssertTrue(migrated.Scopes == null || migrated.Scopes.Count == 0, "Legacy credential has no explicit scopes after migration");
                AssertTrue(migrated.GraphGUIDs == null || migrated.GraphGUIDs.Count == 0, "Legacy credential has no graph allow-list after migration");
                AssertTrue(migrated.HasScope("write"), "Legacy credential keeps write compatibility scope");
                AssertTrue(migrated.CanAccessGraph(graphGuid), "Legacy credential keeps graph compatibility access");

                AuthorizationRoleSearchResult builtIns = await repo.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest
                {
                    BuiltIn = true,
                    PageSize = 1000
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(5L, builtIns.TotalCount, "Migration seeds built-in roles");
                AssertEqual(5, builtIns.Objects.Select(role => role.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(), "Migration seeds unique built-in roles");

                AuthorizationService service = new AuthorizationService(new LoggingModule(), repo);
                AuthorizationDecision writeDecision = await service.EvaluateCredentialEffectiveAccess(
                    migrated,
                    graphGuid,
                    RequestTypeEnum.NodeDelete,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, writeDecision.Result, "Migrated legacy credential keeps unrestricted write access without stored assignments");
                AssertEqual(AuthorizationDecisionReason.Permitted, writeDecision.Reason, "Migrated legacy credential write compatibility reason");

                repo.InitializeRepository();
                AuthorizationRoleSearchResult builtInsAfterSecondInitialize = await repo.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest
                {
                    BuiltIn = true,
                    PageSize = 1000
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(5L, builtInsAfterSecondInitialize.TotalCount, "Authorization migration is idempotent");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task AssertAuthorizationRoleCount(
            LiteGraphClient client,
            AuthorizationRoleSearchRequest search,
            long expected,
            string message,
            CancellationToken cancellationToken)
        {
            AuthorizationRoleSearchResult result = await client.AuthorizationRoles.SearchRoles(search, cancellationToken).ConfigureAwait(false);
            AssertEqual(expected, result.TotalCount, message);
        }

        private static async Task AssertUserRoleCount(
            LiteGraphClient client,
            UserRoleAssignmentSearchRequest search,
            long expected,
            string message,
            CancellationToken cancellationToken)
        {
            UserRoleAssignmentSearchResult result = await client.AuthorizationRoles.SearchUserRoles(search, cancellationToken).ConfigureAwait(false);
            AssertEqual(expected, result.TotalCount, message);
        }

        private static async Task AssertCredentialScopeCount(
            LiteGraphClient client,
            CredentialScopeAssignmentSearchRequest search,
            long expected,
            string message,
            CancellationToken cancellationToken)
        {
            CredentialScopeAssignmentSearchResult result = await client.AuthorizationRoles.SearchCredentialScopes(search, cancellationToken).ConfigureAwait(false);
            AssertEqual(expected, result.TotalCount, message);
        }

        private static async Task TestAuthorizationRoleEffectiveAccess(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-authorization-effective-access.db";
            DeleteFileIfExists(filename);

            Guid tenantGuid = Guid.NewGuid();
            Guid graphA = Guid.NewGuid();
            Guid graphB = Guid.NewGuid();
            Guid userViewer = Guid.NewGuid();
            Guid userTenantAdmin = Guid.NewGuid();
            Guid userGraphAdminTenant = Guid.NewGuid();
            Guid userUnassigned = Guid.NewGuid();

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            }))
            {
                repo.InitializeRepository();
                AuthorizationService service = new AuthorizationService(new LoggingModule(), repo);

                AssertEqual(AuthorizationPermissionEnum.Delete, AuthorizationService.RequiredPermission(RequestTypeEnum.NodeDelete), "Node delete requires delete permission");
                AssertEqual(AuthorizationPermissionEnum.Admin, AuthorizationService.RequiredPermission(RequestTypeEnum.TenantUpdate), "Tenant update requires admin permission");
                AssertEqual(AuthorizationResourceTypeEnum.Transaction, AuthorizationService.RequiredResourceType(RequestTypeEnum.GraphTransaction), "Graph transaction resource type");
                AssertEqual(AuthorizationResourceTypeEnum.Query, AuthorizationService.RequiredResourceType(RequestTypeEnum.GraphQuery), "Graph query resource type");

                Credential unrestrictedCredential = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid()
                };

                AuthorizationDecision unrestricted = await service.EvaluateCredentialEffectiveAccess(
                    unrestrictedCredential,
                    graphB,
                    RequestTypeEnum.NodeCreate,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, unrestricted.Result, "Credential without assignments keeps compatibility access");
                AssertEqual(AuthorizationDecisionReason.Permitted, unrestricted.Reason, "Credential without assignments compatibility reason");

                Credential viewerCredential = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid()
                };

                await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    TenantGUID = tenantGuid,
                    CredentialGUID = viewerCredential.GUID,
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationDecision viewerRead = await service.EvaluateCredentialEffectiveAccess(
                    viewerCredential,
                    graphA,
                    RequestTypeEnum.NodeRead,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, viewerRead.Result, "Viewer credential assignment permits node read");

                AuthorizationDecision viewerWrite = await service.EvaluateCredentialEffectiveAccess(
                    viewerCredential,
                    graphA,
                    RequestTypeEnum.NodeCreate,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Denied, viewerWrite.Result, "Viewer credential assignment denies node write");
                AssertEqual(AuthorizationDecisionReason.MissingScope, viewerWrite.Reason, "Viewer credential write denial reason");

                AuthorizationDecision viewerOtherGraph = await service.EvaluateCredentialEffectiveAccess(
                    viewerCredential,
                    graphB,
                    RequestTypeEnum.NodeRead,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Denied, viewerOtherGraph.Result, "Viewer credential assignment denies other graph");
                AssertEqual(AuthorizationDecisionReason.GraphDenied, viewerOtherGraph.Reason, "Viewer credential graph denial reason");

                Credential queryWriterCredential = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid()
                };

                await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    TenantGUID = tenantGuid,
                    CredentialGUID = queryWriterCredential.GUID,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Query
                    }
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationDecision queryWrite = await service.EvaluateCredentialEffectiveAccess(
                    queryWriterCredential,
                    graphA,
                    "write",
                    AuthorizationResourceTypeEnum.Query,
                    RequestTypeEnum.GraphQuery,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, queryWrite.Result, "Direct credential scope assignment permits mutation query");

                Credential legacyReadOnly = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid(),
                    Scopes = new List<string>
                    {
                        "read"
                    }
                };

                await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    TenantGUID = tenantGuid,
                    CredentialGUID = legacyReadOnly.GUID,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Query
                    }
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationDecision legacyStillRestricts = await service.EvaluateCredentialEffectiveAccess(
                    legacyReadOnly,
                    graphA,
                    "write",
                    AuthorizationResourceTypeEnum.Query,
                    RequestTypeEnum.GraphQuery,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Denied, legacyStillRestricts.Result, "Legacy credential scope remains an upper bound");
                AssertEqual(AuthorizationDecisionReason.MissingScope, legacyStillRestricts.Reason, "Legacy credential upper-bound denial reason");

                AuthorizationDecision unassignedUser = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userUnassigned,
                    graphA,
                    RequestTypeEnum.NodeDelete,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, unassignedUser.Result, "User without assignments keeps compatibility access");

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = userViewer,
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationDecision userViewerRead = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userViewer,
                    graphA,
                    RequestTypeEnum.NodeRead,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, userViewerRead.Result, "Viewer user assignment permits node read");

                AuthorizationDecision userViewerWrite = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userViewer,
                    graphA,
                    RequestTypeEnum.NodeCreate,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Denied, userViewerWrite.Result, "Viewer user assignment denies node write");
                AssertEqual(AuthorizationDecisionReason.MissingScope, userViewerWrite.Reason, "Viewer user write denial reason");

                AuthorizationDecision userViewerOtherGraph = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userViewer,
                    graphB,
                    RequestTypeEnum.NodeRead,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Denied, userViewerOtherGraph.Result, "Viewer user assignment denies other graph");
                AssertEqual(AuthorizationDecisionReason.GraphDenied, userViewerOtherGraph.Reason, "Viewer user graph denial reason");

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = userTenantAdmin,
                    RoleName = AuthorizationPolicyDefinitions.TenantAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Tenant
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationDecision tenantAdminDeletesGraphChild = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userTenantAdmin,
                    graphB,
                    RequestTypeEnum.NodeDelete,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, tenantAdminDeletesGraphChild.Result, "TenantAdmin user assignment inherits to graphs");

                AuthorizationDecision tenantAdminManagesTenant = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userTenantAdmin,
                    null,
                    RequestTypeEnum.TenantUpdate,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Permitted, tenantAdminManagesTenant.Result, "TenantAdmin user assignment permits tenant admin operation");

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = userGraphAdminTenant,
                    RoleName = AuthorizationPolicyDefinitions.GraphAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Tenant
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationDecision graphAdminDoesNotInherit = await service.EvaluateUserEffectiveAccess(
                    tenantGuid,
                    userGraphAdminTenant,
                    graphA,
                    RequestTypeEnum.NodeRead,
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(AuthorizationResultEnum.Denied, graphAdminDoesNotInherit.Result, "Non-inheriting tenant assignment does not grant graph access");
                AssertEqual(AuthorizationDecisionReason.GraphDenied, graphAdminDoesNotInherit.Reason, "Non-inheriting tenant assignment graph denial reason");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestAuthorizationCacheInvalidation(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-authorization-cache.db";
            DeleteFileIfExists(filename);

            Guid tenantGuid = Guid.NewGuid();
            Guid graphA = Guid.NewGuid();
            Guid graphB = Guid.NewGuid();
            Guid userCustom = Guid.NewGuid();
            Guid userPromoted = Guid.NewGuid();

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            }))
            {
                repo.InitializeRepository();

                AuthorizationRole customRole = await repo.AuthorizationRoles.CreateRole(new AuthorizationRole
                {
                    TenantGUID = tenantGuid,
                    Name = "CacheNodeRole",
                    DisplayName = "Cache node role",
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Node
                    }
                }, cancellationToken).ConfigureAwait(false);

                await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = userCustom,
                    RoleGUID = customRole.GUID,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                AuthorizationService service = new AuthorizationService(new LoggingModule(), repo);

                await AssertUserAccess(service, tenantGuid, userCustom, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Initial custom role read is permitted", cancellationToken).ConfigureAwait(false);
                AuthorizationCacheStatistics afterFirstRead = service.GetCacheStatistics();
                AssertEqual(1L, afterFirstRead.PolicyCacheMisses, "First user policy evaluation misses cache");
                AssertEqual(0L, afterFirstRead.PolicyCacheHits, "First user policy evaluation has no cache hit");
                AssertEqual(1, afterFirstRead.EffectivePolicyCacheEntries, "First user policy evaluation caches one subject");
                AssertTrue(afterFirstRead.RoleCacheEntries >= 1, "First user policy evaluation caches resolved role");

                await AssertUserAccess(service, tenantGuid, userCustom, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Second custom role read is permitted from cache", cancellationToken).ConfigureAwait(false);
                AuthorizationCacheStatistics afterSecondRead = service.GetCacheStatistics();
                AssertTrue(afterSecondRead.PolicyCacheHits > afterFirstRead.PolicyCacheHits, "Second user policy evaluation hits cache");
                AssertEqual(afterFirstRead.PolicyCacheMisses, afterSecondRead.PolicyCacheMisses, "Second user policy evaluation does not miss cache");

                customRole.Permissions = new List<AuthorizationPermissionEnum>
                {
                    AuthorizationPermissionEnum.Write
                };
                customRole.LastUpdateUtc = DateTime.UtcNow;
                await repo.AuthorizationRoles.UpdateRole(customRole, cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, userCustom, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Updated custom role invalidates cached read grant", cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, userCustom, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Updated custom role grants write after invalidation", cancellationToken).ConfigureAwait(false);
                AuthorizationCacheStatistics afterRoleUpdate = service.GetCacheStatistics();
                AssertTrue(afterRoleUpdate.PolicyCacheInvalidations > afterSecondRead.PolicyCacheInvalidations, "Role update invalidates effective policy cache");
                AssertTrue(afterRoleUpdate.PolicyCacheMisses > afterSecondRead.PolicyCacheMisses, "Role update forces policy reload");

                await repo.AuthorizationRoles.DeleteRoleByGuid(customRole.GUID, cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, userCustom, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Deleted custom role invalidates cached write grant", cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, userPromoted, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Unassigned user admin denial is cached", cancellationToken).ConfigureAwait(false);
                UserRoleAssignment promotedAssignment = await repo.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                {
                    TenantGUID = tenantGuid,
                    UserGUID = userPromoted,
                    RoleName = AuthorizationPolicyDefinitions.TenantAdminRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Tenant
                }, cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, userPromoted, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "User role creation invalidates cached admin denial", cancellationToken).ConfigureAwait(false);
                promotedAssignment.ResourceScope = AuthorizationResourceScopeEnum.Graph;
                promotedAssignment.GraphGUID = graphA;
                promotedAssignment.LastUpdateUtc = DateTime.UtcNow;
                await repo.AuthorizationRoles.UpdateUserRole(promotedAssignment, cancellationToken).ConfigureAwait(false);

                await AssertUserAccess(service, tenantGuid, userPromoted, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "User role update invalidates cached tenant admin grant", cancellationToken).ConfigureAwait(false);
                await repo.AuthorizationRoles.DeleteUserRoleByGuid(promotedAssignment.GUID, cancellationToken).ConfigureAwait(false);
                await AssertUserAccess(service, tenantGuid, userPromoted, null, RequestTypeEnum.AuthorizationRoleReadAll, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "User role deletion keeps admin denial after invalidation", cancellationToken).ConfigureAwait(false);

                Credential credential = new Credential
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    UserGUID = Guid.NewGuid(),
                    Scopes = new List<string>
                    {
                        "read",
                        "write"
                    }
                };

                await AssertCredentialAccess(service, credential, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Credential without assignments keeps compatibility write access before cache invalidation", cancellationToken).ConfigureAwait(false);
                CredentialScopeAssignment credentialScope = await repo.AuthorizationRoles.CreateCredentialScope(new CredentialScopeAssignment
                {
                    TenantGUID = tenantGuid,
                    CredentialGUID = credential.GUID,
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA
                }, cancellationToken).ConfigureAwait(false);

                await AssertCredentialAccess(service, credential, graphA, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Credential scope creation invalidates compatibility write grant", cancellationToken).ConfigureAwait(false);
                await AssertCredentialAccess(service, credential, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Credential scope read grant is cached", cancellationToken).ConfigureAwait(false);
                AuthorizationCacheStatistics afterCredentialRead = service.GetCacheStatistics();

                await AssertCredentialAccess(service, credential, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Second credential read uses cached scope", cancellationToken).ConfigureAwait(false);
                AuthorizationCacheStatistics afterCredentialHit = service.GetCacheStatistics();
                AssertTrue(afterCredentialHit.PolicyCacheHits > afterCredentialRead.PolicyCacheHits, "Second credential policy evaluation hits cache");

                credentialScope.GraphGUID = graphB;
                credentialScope.LastUpdateUtc = DateTime.UtcNow;
                await repo.AuthorizationRoles.UpdateCredentialScope(credentialScope, cancellationToken).ConfigureAwait(false);

                await AssertCredentialAccess(service, credential, graphA, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.GraphDenied, "Credential scope update invalidates cached graphA grant", cancellationToken).ConfigureAwait(false);
                await AssertCredentialAccess(service, credential, graphB, RequestTypeEnum.NodeRead, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Credential scope update grants graphB after invalidation", cancellationToken).ConfigureAwait(false);
                await AssertCredentialAccess(service, credential, graphB, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Denied, AuthorizationDecisionReason.MissingScope, "Viewer credential scope denies writes before deletion", cancellationToken).ConfigureAwait(false);

                await repo.AuthorizationRoles.DeleteCredentialScopeByGuid(credentialScope.GUID, cancellationToken).ConfigureAwait(false);
                await AssertCredentialAccess(service, credential, graphB, RequestTypeEnum.NodeCreate, AuthorizationResultEnum.Permitted, AuthorizationDecisionReason.Permitted, "Credential scope deletion invalidates cached viewer restriction", cancellationToken).ConfigureAwait(false);

                AuthorizationCacheStatistics finalStats = service.GetCacheStatistics();
                AssertTrue(finalStats.PolicyCacheInvalidations >= 7, "All role and scope mutations invalidated cache");
                AssertTrue(finalStats.PolicyCacheHits >= 3, "Repeated user and credential evaluations used cache");
                AssertTrue(finalStats.PolicyCacheMisses >= 8, "Invalidations forced effective policy reloads");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestAuthorizationRoleRestManagement(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");
                if (_McpClient == null) throw new InvalidOperationException("MCP client was not initialized.");

                TenantMetadata tenant;
                Graph graph;
                UserMaster targetUser;
                UserMaster tenantAdminUser;
                UserMaster viewerUser;
                UserMaster unassignedUser;
                Credential credential;
                AuthorizationRole editorRole;

                string password = "password";
                string roleName = "RestCurator" + Guid.NewGuid().ToString("N");
                string endpointBase;

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = _McpEnvironment.DatabasePath
                })))
                {
                    client.InitializeRepository();

                    tenant = await client.Tenant.Create(new TenantMetadata { Name = "REST RBAC Tenant" }, cancellationToken).ConfigureAwait(false);
                    graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "REST RBAC Graph" }, cancellationToken).ConfigureAwait(false);
                    targetUser = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "Target",
                        LastName = "User",
                        Email = "target-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = password
                    }, cancellationToken).ConfigureAwait(false);
                    tenantAdminUser = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "Tenant",
                        LastName = "Admin",
                        Email = "tenant-admin-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = password
                    }, cancellationToken).ConfigureAwait(false);
                    viewerUser = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "Viewer",
                        LastName = "User",
                        Email = "viewer-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = password
                    }, cancellationToken).ConfigureAwait(false);
                    unassignedUser = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "Unassigned",
                        LastName = "User",
                        Email = "unassigned-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = password
                    }, cancellationToken).ConfigureAwait(false);
                    credential = await client.Credential.Create(new Credential
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = targetUser.GUID,
                        Name = "REST RBAC Credential",
                        BearerToken = "rest-rbac-" + Guid.NewGuid().ToString("N"),
                        Scopes = new List<string> { "read", "write", "admin" },
                        GraphGUIDs = new List<Guid> { graph.GUID }
                    }, cancellationToken).ConfigureAwait(false);

                    editorRole = await client.AuthorizationRoles.ReadRoleByName(null, AuthorizationPolicyDefinitions.EditorRoleName, cancellationToken).ConfigureAwait(false);
                    await client.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = tenantAdminUser.GUID,
                        RoleName = AuthorizationPolicyDefinitions.TenantAdminRoleName,
                        ResourceScope = AuthorizationResourceScopeEnum.Tenant
                    }, cancellationToken).ConfigureAwait(false);
                    await client.AuthorizationRoles.CreateUserRole(new UserRoleAssignment
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = viewerUser.GUID,
                        RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                        ResourceScope = AuthorizationResourceScopeEnum.Graph,
                        GraphGUID = graph.GUID
                    }, cancellationToken).ConfigureAwait(false);
                }

                endpointBase = _McpEnvironment.LiteGraphEndpoint + "/v1.0/tenants/" + tenant.GUID;

                async Task<(int Status, string Body)> SendAdminAsync(HttpMethod method, string url, object? body = null)
                {
                    using HttpRequestMessage request = new HttpRequestMessage(method, url);
                    request.Headers.Add("Authorization", "Bearer litegraphadmin");
                    if (body != null)
                    {
                        request.Content = new StringContent(_McpSerializer.SerializeJson(body, false), Encoding.UTF8, "application/json");
                    }

                    using HttpResponseMessage response = await _ReadinessClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                    return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                }

                async Task<(int Status, string Body)> SendUserAsync(UserMaster user, HttpMethod method, string url, object? body = null)
                {
                    using HttpRequestMessage request = new HttpRequestMessage(method, url);
                    request.Headers.Add("x-email", user.Email);
                    request.Headers.Add("x-password", password);
                    request.Headers.Add("x-tenant-guid", tenant.GUID.ToString());
                    if (body != null)
                    {
                        request.Content = new StringContent(_McpSerializer.SerializeJson(body, false), Encoding.UTF8, "application/json");
                    }

                    using HttpResponseMessage response = await _ReadinessClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                    return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                }

                (int listRolesStatus, string listRolesBody) = await SendAdminAsync(HttpMethod.Get, endpointBase + "/roles?includeBuiltIns=true&pageSize=20").ConfigureAwait(false);
                AssertEqual(200, listRolesStatus, "REST role list status");
                AuthorizationRoleSearchResult listedRoles = _McpSerializer.DeserializeJson<AuthorizationRoleSearchResult>(listRolesBody);
                AssertTrue(listedRoles.Objects.Any(role => role.Name == AuthorizationPolicyDefinitions.TenantAdminRoleName), "REST role list includes built-ins");

                AuthorizationRole customRole = new AuthorizationRole
                {
                    Name = roleName,
                    DisplayName = "REST Curator",
                    Description = "Created through REST",
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read,
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Node,
                        AuthorizationResourceTypeEnum.Query
                    }
                };

                (int createRoleStatus, string createRoleBody) = await SendAdminAsync(HttpMethod.Put, endpointBase + "/roles", customRole).ConfigureAwait(false);
                AssertEqual(201, createRoleStatus, "REST role create status");
                AuthorizationRole createdRole = _McpSerializer.DeserializeJson<AuthorizationRole>(createRoleBody);
                AssertEqual(tenant.GUID, createdRole.TenantGUID.GetValueOrDefault(), "REST role create tenant");
                AssertFalse(createdRole.BuiltIn, "REST role create forces custom role");

                (int readRoleStatus, string readRoleBody) = await SendAdminAsync(HttpMethod.Get, endpointBase + "/roles/" + createdRole.GUID).ConfigureAwait(false);
                AssertEqual(200, readRoleStatus, "REST role read status");
                AuthorizationRole readRole = _McpSerializer.DeserializeJson<AuthorizationRole>(readRoleBody);
                AssertEqual(createdRole.GUID, readRole.GUID, "REST role read GUID");

                createdRole.DisplayName = "REST Curator Updated";
                createdRole.Permissions.Add(AuthorizationPermissionEnum.Delete);
                createdRole.ResourceTypes.Add(AuthorizationResourceTypeEnum.Edge);
                (int updateRoleStatus, string updateRoleBody) = await SendAdminAsync(HttpMethod.Put, endpointBase + "/roles/" + createdRole.GUID, createdRole).ConfigureAwait(false);
                AssertEqual(200, updateRoleStatus, "REST role update status");
                AuthorizationRole updatedRole = _McpSerializer.DeserializeJson<AuthorizationRole>(updateRoleBody);
                AssertTrue(updatedRole.Permissions.Contains(AuthorizationPermissionEnum.Delete), "REST role update permissions");
                AssertTrue(updatedRole.ResourceTypes.Contains(AuthorizationResourceTypeEnum.Edge), "REST role update resource types");

                (int filterRoleStatus, string filterRoleBody) = await SendAdminAsync(HttpMethod.Get, endpointBase + "/roles?includeBuiltIns=false&name=" + roleName + "&permission=Delete&resourceType=Edge").ConfigureAwait(false);
                AssertEqual(200, filterRoleStatus, "REST role filtered list status");
                AuthorizationRoleSearchResult filteredRoles = _McpSerializer.DeserializeJson<AuthorizationRoleSearchResult>(filterRoleBody);
                AssertEqual(1L, filteredRoles.TotalCount, "REST role filtered count");

                (int builtInUpdateStatus, _) = await SendAdminAsync(HttpMethod.Put, endpointBase + "/roles/" + editorRole.GUID, editorRole).ConfigureAwait(false);
                AssertEqual(409, builtInUpdateStatus, "REST built-in role update is rejected");
                (int builtInDeleteStatus, _) = await SendAdminAsync(HttpMethod.Delete, endpointBase + "/roles/" + editorRole.GUID).ConfigureAwait(false);
                AssertEqual(409, builtInDeleteStatus, "REST built-in role delete is rejected");

                UserRoleAssignment userAssignment = new UserRoleAssignment
                {
                    RoleGUID = updatedRole.GUID,
                    RoleName = updatedRole.Name,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graph.GUID
                };

                string userRolesEndpoint = endpointBase + "/users/" + targetUser.GUID + "/roles";
                (int createUserRoleStatus, string createUserRoleBody) = await SendAdminAsync(HttpMethod.Put, userRolesEndpoint, userAssignment).ConfigureAwait(false);
                AssertEqual(201, createUserRoleStatus, "REST user role assignment create status");
                UserRoleAssignment createdUserRole = _McpSerializer.DeserializeJson<UserRoleAssignment>(createUserRoleBody);
                AssertEqual(targetUser.GUID, createdUserRole.UserGUID, "REST user role assignment route user");
                AssertEqual(graph.GUID, createdUserRole.GraphGUID.GetValueOrDefault(), "REST user role assignment graph");

                (int listUserRoleStatus, string listUserRoleBody) = await SendAdminAsync(HttpMethod.Get, userRolesEndpoint + "?roleName=" + roleName + "&graphGuid=" + graph.GUID).ConfigureAwait(false);
                AssertEqual(200, listUserRoleStatus, "REST user role assignment list status");
                UserRoleAssignmentSearchResult listedUserRoles = _McpSerializer.DeserializeJson<UserRoleAssignmentSearchResult>(listUserRoleBody);
                AssertEqual(1L, listedUserRoles.TotalCount, "REST user role assignment filtered count");

                createdUserRole.GraphGUID = null;
                (int updateUserRoleStatus, string updateUserRoleBody) = await SendAdminAsync(HttpMethod.Put, userRolesEndpoint + "/" + createdUserRole.GUID, createdUserRole).ConfigureAwait(false);
                AssertEqual(200, updateUserRoleStatus, "REST user role assignment update status");
                UserRoleAssignment updatedUserRole = _McpSerializer.DeserializeJson<UserRoleAssignment>(updateUserRoleBody);
                AssertTrue(updatedUserRole.GraphGUID == null, "REST user role assignment update graph");

                (int userEffectiveStatus, string userEffectiveBody) = await SendAdminAsync(HttpMethod.Get, endpointBase + "/users/" + targetUser.GUID + "/permissions?graphGuid=" + graph.GUID).ConfigureAwait(false);
                AssertEqual(200, userEffectiveStatus, "REST user effective permissions status");
                AuthorizationEffectivePermissionsResult userEffective = _McpSerializer.DeserializeJson<AuthorizationEffectivePermissionsResult>(userEffectiveBody);
                AssertEqual(1, userEffective.Grants.Count, "REST user effective grant count");
                AssertTrue(userEffective.Grants[0].Permissions.Contains(AuthorizationPermissionEnum.Delete), "REST user effective grants include role permissions");

                CredentialScopeAssignment credentialScope = new CredentialScopeAssignment
                {
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graph.GUID,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Query
                    }
                };

                string credentialScopesEndpoint = endpointBase + "/credentials/" + credential.GUID + "/scopes";
                (int createScopeStatus, string createScopeBody) = await SendAdminAsync(HttpMethod.Put, credentialScopesEndpoint, credentialScope).ConfigureAwait(false);
                AssertEqual(201, createScopeStatus, "REST credential scope create status");
                CredentialScopeAssignment createdScope = _McpSerializer.DeserializeJson<CredentialScopeAssignment>(createScopeBody);
                AssertEqual(credential.GUID, createdScope.CredentialGUID, "REST credential scope route credential");

                (int listScopeStatus, string listScopeBody) = await SendAdminAsync(HttpMethod.Get, credentialScopesEndpoint + "?permission=Write&resourceType=Query&graphGuid=" + graph.GUID).ConfigureAwait(false);
                AssertEqual(200, listScopeStatus, "REST credential scope list status");
                CredentialScopeAssignmentSearchResult listedScopes = _McpSerializer.DeserializeJson<CredentialScopeAssignmentSearchResult>(listScopeBody);
                AssertEqual(1L, listedScopes.TotalCount, "REST credential scope filtered count");

                createdScope.Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Delete };
                createdScope.ResourceTypes = new List<AuthorizationResourceTypeEnum> { AuthorizationResourceTypeEnum.Edge };
                (int updateScopeStatus, string updateScopeBody) = await SendAdminAsync(HttpMethod.Put, credentialScopesEndpoint + "/" + createdScope.GUID, createdScope).ConfigureAwait(false);
                AssertEqual(200, updateScopeStatus, "REST credential scope update status");
                CredentialScopeAssignment updatedScope = _McpSerializer.DeserializeJson<CredentialScopeAssignment>(updateScopeBody);
                AssertTrue(updatedScope.Permissions.Contains(AuthorizationPermissionEnum.Delete), "REST credential scope update permission");
                AssertTrue(updatedScope.ResourceTypes.Contains(AuthorizationResourceTypeEnum.Edge), "REST credential scope update resource type");

                (int credentialEffectiveStatus, string credentialEffectiveBody) = await SendAdminAsync(HttpMethod.Get, endpointBase + "/credentials/" + credential.GUID + "/permissions?graphGuid=" + graph.GUID).ConfigureAwait(false);
                AssertEqual(200, credentialEffectiveStatus, "REST credential effective permissions status");
                AuthorizationEffectivePermissionsResult credentialEffective = _McpSerializer.DeserializeJson<AuthorizationEffectivePermissionsResult>(credentialEffectiveBody);
                AssertEqual(1, credentialEffective.Grants.Count, "REST credential effective grant count");
                AssertTrue(credentialEffective.Grants[0].Permissions.Contains(AuthorizationPermissionEnum.Delete), "REST credential effective grants include direct permissions");
                AssertTrue(credentialEffective.Roles.Any(role => role.Name == AuthorizationPolicyDefinitions.ViewerRoleName), "REST credential effective grants resolve roles");

                (int tenantAdminStatus, _) = await SendUserAsync(tenantAdminUser, HttpMethod.Get, endpointBase + "/roles?includeBuiltIns=false").ConfigureAwait(false);
                AssertEqual(200, tenantAdminStatus, "REST TenantAdmin user can manage authorization endpoints");
                (int viewerStatus, string viewerBody) = await SendUserAsync(viewerUser, HttpMethod.Get, endpointBase + "/roles?includeBuiltIns=false").ConfigureAwait(false);
                AssertEqual(401, viewerStatus, "REST Viewer user cannot manage authorization endpoints");
                AssertTrue(viewerBody.Contains("AuthorizationFailed"), "REST Viewer denial body");
                (int unassignedStatus, _) = await SendUserAsync(unassignedUser, HttpMethod.Get, endpointBase + "/roles?includeBuiltIns=false").ConfigureAwait(false);
                AssertEqual(401, unassignedStatus, "REST unassigned user cannot use admin fallback for authorization endpoints");

                (int deleteScopeStatus, _) = await SendAdminAsync(HttpMethod.Delete, credentialScopesEndpoint + "/" + updatedScope.GUID).ConfigureAwait(false);
                AssertEqual(204, deleteScopeStatus, "REST credential scope delete status");
                (int deleteUserRoleStatus, _) = await SendAdminAsync(HttpMethod.Delete, userRolesEndpoint + "/" + updatedUserRole.GUID).ConfigureAwait(false);
                AssertEqual(204, deleteUserRoleStatus, "REST user role assignment delete status");
                (int deleteRoleStatus, _) = await SendAdminAsync(HttpMethod.Delete, endpointBase + "/roles/" + updatedRole.GUID).ConfigureAwait(false);
                AssertEqual(204, deleteRoleStatus, "REST role delete status");

                (int deletedRoleReadStatus, _) = await SendAdminAsync(HttpMethod.Get, endpointBase + "/roles/" + updatedRole.GUID).ConfigureAwait(false);
                AssertEqual(404, deletedRoleReadStatus, "REST role read after delete status");

                string mcpRoleName = "McpCurator" + Guid.NewGuid().ToString("N");
                AuthorizationRole mcpRole = new AuthorizationRole
                {
                    Name = mcpRoleName,
                    DisplayName = "MCP Curator",
                    Description = "Created through MCP",
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Read,
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Node,
                        AuthorizationResourceTypeEnum.Query
                    }
                };

                string mcpCreateRoleBody = await _McpClient.CallAsync<string>("authorization/role/create", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    role = _McpSerializer.SerializeJson(mcpRole, false)
                }).ConfigureAwait(false);
                AuthorizationRole mcpCreatedRole = _McpSerializer.DeserializeJson<AuthorizationRole>(mcpCreateRoleBody);
                AssertEqual(tenant.GUID, mcpCreatedRole.TenantGUID.GetValueOrDefault(), "MCP role create tenant");
                AssertFalse(mcpCreatedRole.BuiltIn, "MCP role create forces custom role");

                string mcpReadRoleBody = await _McpClient.CallAsync<string>("authorization/role/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    roleGuid = mcpCreatedRole.GUID.ToString()
                }).ConfigureAwait(false);
                AuthorizationRole mcpReadRole = _McpSerializer.DeserializeJson<AuthorizationRole>(mcpReadRoleBody);
                AssertEqual(mcpCreatedRole.GUID, mcpReadRole.GUID, "MCP role read GUID");

                mcpCreatedRole.DisplayName = "MCP Curator Updated";
                mcpCreatedRole.Permissions.Add(AuthorizationPermissionEnum.Delete);
                mcpCreatedRole.ResourceTypes.Add(AuthorizationResourceTypeEnum.Edge);
                string mcpUpdateRoleBody = await _McpClient.CallAsync<string>("authorization/role/update", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    roleGuid = mcpCreatedRole.GUID.ToString(),
                    role = _McpSerializer.SerializeJson(mcpCreatedRole, false)
                }).ConfigureAwait(false);
                AuthorizationRole mcpUpdatedRole = _McpSerializer.DeserializeJson<AuthorizationRole>(mcpUpdateRoleBody);
                AssertTrue(mcpUpdatedRole.Permissions.Contains(AuthorizationPermissionEnum.Delete), "MCP role update permissions");
                AssertTrue(mcpUpdatedRole.ResourceTypes.Contains(AuthorizationResourceTypeEnum.Edge), "MCP role update resource types");

                string mcpRoleListBody = await _McpClient.CallAsync<string>("authorization/role/all", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    includeBuiltIns = false,
                    name = mcpRoleName,
                    permission = AuthorizationPermissionEnum.Delete.ToString(),
                    resourceType = AuthorizationResourceTypeEnum.Edge.ToString(),
                    pageSize = 20
                }).ConfigureAwait(false);
                AuthorizationRoleSearchResult mcpRoleList = _McpSerializer.DeserializeJson<AuthorizationRoleSearchResult>(mcpRoleListBody);
                AssertEqual(1L, mcpRoleList.TotalCount, "MCP role filtered count");

                UserRoleAssignment mcpUserAssignment = new UserRoleAssignment
                {
                    RoleGUID = mcpUpdatedRole.GUID,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graph.GUID
                };
                string mcpCreateUserRoleBody = await _McpClient.CallAsync<string>("authorization/userrole/create", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    userGuid = targetUser.GUID.ToString(),
                    assignment = _McpSerializer.SerializeJson(mcpUserAssignment, false)
                }).ConfigureAwait(false);
                UserRoleAssignment mcpCreatedUserRole = _McpSerializer.DeserializeJson<UserRoleAssignment>(mcpCreateUserRoleBody);
                AssertEqual(targetUser.GUID, mcpCreatedUserRole.UserGUID, "MCP user role create route user");

                string mcpReadUserRoleBody = await _McpClient.CallAsync<string>("authorization/userrole/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    userGuid = targetUser.GUID.ToString(),
                    assignmentGuid = mcpCreatedUserRole.GUID.ToString()
                }).ConfigureAwait(false);
                UserRoleAssignment mcpReadUserRole = _McpSerializer.DeserializeJson<UserRoleAssignment>(mcpReadUserRoleBody);
                AssertEqual(mcpCreatedUserRole.GUID, mcpReadUserRole.GUID, "MCP user role read GUID");

                mcpCreatedUserRole.GraphGUID = null;
                string mcpUpdateUserRoleBody = await _McpClient.CallAsync<string>("authorization/userrole/update", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    userGuid = targetUser.GUID.ToString(),
                    assignmentGuid = mcpCreatedUserRole.GUID.ToString(),
                    assignment = _McpSerializer.SerializeJson(mcpCreatedUserRole, false)
                }).ConfigureAwait(false);
                UserRoleAssignment mcpUpdatedUserRole = _McpSerializer.DeserializeJson<UserRoleAssignment>(mcpUpdateUserRoleBody);
                AssertTrue(mcpUpdatedUserRole.GraphGUID == null, "MCP user role update graph");

                string mcpUserRoleListBody = await _McpClient.CallAsync<string>("authorization/userrole/all", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    userGuid = targetUser.GUID.ToString(),
                    roleGuid = mcpUpdatedRole.GUID.ToString(),
                    pageSize = 20
                }).ConfigureAwait(false);
                UserRoleAssignmentSearchResult mcpUserRoleList = _McpSerializer.DeserializeJson<UserRoleAssignmentSearchResult>(mcpUserRoleListBody);
                AssertEqual(1L, mcpUserRoleList.TotalCount, "MCP user role filtered count");

                string mcpUserEffectiveBody = await _McpClient.CallAsync<string>("authorization/user/permissions", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    userGuid = targetUser.GUID.ToString(),
                    graphGuid = graph.GUID.ToString()
                }).ConfigureAwait(false);
                AuthorizationEffectivePermissionsResult mcpUserEffective = _McpSerializer.DeserializeJson<AuthorizationEffectivePermissionsResult>(mcpUserEffectiveBody);
                AssertEqual(1, mcpUserEffective.Grants.Count, "MCP user effective grant count");
                AssertTrue(mcpUserEffective.Grants[0].Permissions.Contains(AuthorizationPermissionEnum.Delete), "MCP user effective grants include role permissions");

                CredentialScopeAssignment mcpCredentialScope = new CredentialScopeAssignment
                {
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graph.GUID,
                    Permissions = new List<AuthorizationPermissionEnum>
                    {
                        AuthorizationPermissionEnum.Write
                    },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum>
                    {
                        AuthorizationResourceTypeEnum.Query
                    }
                };
                string mcpCreateCredentialScopeBody = await _McpClient.CallAsync<string>("authorization/credentialscope/create", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    credentialGuid = credential.GUID.ToString(),
                    assignment = _McpSerializer.SerializeJson(mcpCredentialScope, false)
                }).ConfigureAwait(false);
                CredentialScopeAssignment mcpCreatedCredentialScope = _McpSerializer.DeserializeJson<CredentialScopeAssignment>(mcpCreateCredentialScopeBody);
                AssertEqual(credential.GUID, mcpCreatedCredentialScope.CredentialGUID, "MCP credential scope route credential");

                string mcpReadCredentialScopeBody = await _McpClient.CallAsync<string>("authorization/credentialscope/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    credentialGuid = credential.GUID.ToString(),
                    assignmentGuid = mcpCreatedCredentialScope.GUID.ToString()
                }).ConfigureAwait(false);
                CredentialScopeAssignment mcpReadCredentialScope = _McpSerializer.DeserializeJson<CredentialScopeAssignment>(mcpReadCredentialScopeBody);
                AssertEqual(mcpCreatedCredentialScope.GUID, mcpReadCredentialScope.GUID, "MCP credential scope read GUID");

                mcpCreatedCredentialScope.Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Delete };
                mcpCreatedCredentialScope.ResourceTypes = new List<AuthorizationResourceTypeEnum> { AuthorizationResourceTypeEnum.Edge };
                string mcpUpdateCredentialScopeBody = await _McpClient.CallAsync<string>("authorization/credentialscope/update", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    credentialGuid = credential.GUID.ToString(),
                    assignmentGuid = mcpCreatedCredentialScope.GUID.ToString(),
                    assignment = _McpSerializer.SerializeJson(mcpCreatedCredentialScope, false)
                }).ConfigureAwait(false);
                CredentialScopeAssignment mcpUpdatedCredentialScope = _McpSerializer.DeserializeJson<CredentialScopeAssignment>(mcpUpdateCredentialScopeBody);
                AssertTrue(mcpUpdatedCredentialScope.Permissions.Contains(AuthorizationPermissionEnum.Delete), "MCP credential scope update permission");
                AssertTrue(mcpUpdatedCredentialScope.ResourceTypes.Contains(AuthorizationResourceTypeEnum.Edge), "MCP credential scope update resource type");

                string mcpCredentialScopeListBody = await _McpClient.CallAsync<string>("authorization/credentialscope/all", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    credentialGuid = credential.GUID.ToString(),
                    permission = AuthorizationPermissionEnum.Delete.ToString(),
                    resourceType = AuthorizationResourceTypeEnum.Edge.ToString(),
                    graphGuid = graph.GUID.ToString(),
                    pageSize = 20
                }).ConfigureAwait(false);
                CredentialScopeAssignmentSearchResult mcpCredentialScopeList = _McpSerializer.DeserializeJson<CredentialScopeAssignmentSearchResult>(mcpCredentialScopeListBody);
                AssertEqual(1L, mcpCredentialScopeList.TotalCount, "MCP credential scope filtered count");

                string mcpCredentialEffectiveBody = await _McpClient.CallAsync<string>("authorization/credential/permissions", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    credentialGuid = credential.GUID.ToString(),
                    graphGuid = graph.GUID.ToString()
                }).ConfigureAwait(false);
                AuthorizationEffectivePermissionsResult mcpCredentialEffective = _McpSerializer.DeserializeJson<AuthorizationEffectivePermissionsResult>(mcpCredentialEffectiveBody);
                AssertEqual(1, mcpCredentialEffective.Grants.Count, "MCP credential effective grant count");
                AssertTrue(mcpCredentialEffective.Grants[0].Permissions.Contains(AuthorizationPermissionEnum.Delete), "MCP credential effective grants include direct permissions");
                AssertTrue(mcpCredentialEffective.Roles.Any(role => role.Name == AuthorizationPolicyDefinitions.ViewerRoleName), "MCP credential effective grants resolve roles");

                bool mcpDeleteCredentialScope = await _McpClient.CallAsync<bool>("authorization/credentialscope/delete", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    credentialGuid = credential.GUID.ToString(),
                    assignmentGuid = mcpUpdatedCredentialScope.GUID.ToString()
                }).ConfigureAwait(false);
                AssertTrue(mcpDeleteCredentialScope, "MCP credential scope delete result");

                bool mcpDeleteUserRole = await _McpClient.CallAsync<bool>("authorization/userrole/delete", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    userGuid = targetUser.GUID.ToString(),
                    assignmentGuid = mcpUpdatedUserRole.GUID.ToString()
                }).ConfigureAwait(false);
                AssertTrue(mcpDeleteUserRole, "MCP user role delete result");

                bool mcpDeleteRole = await _McpClient.CallAsync<bool>("authorization/role/delete", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    roleGuid = mcpUpdatedRole.GUID.ToString()
                }).ConfigureAwait(false);
                AssertTrue(mcpDeleteRole, "MCP role delete result");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestAuthorizationMcpBoundary(CancellationToken cancellationToken)
        {
            string bearerToken = "mcp-rbac-" + Guid.NewGuid().ToString("N");
            await EnsureMcpEnvironmentAsync(cancellationToken, bearerToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");
                if (_McpClient == null) throw new InvalidOperationException("MCP client was not initialized.");

                TenantMetadata tenant;
                TenantMetadata targetTenant;
                Graph graphA;
                Graph graphB;
                Graph graphC;
                UserMaster user;
                UserMaster targetUser;
                Node nodeA;
                Node nodeA2;
                Node nodeA3;
                Node nodeB;
                Node nodeB2;
                Edge edgeA;
                Edge edgeB;
                LabelMetadata labelA;
                LabelMetadata graphLabelA;
                LabelMetadata edgeLabelA;
                LabelMetadata labelB;
                TagMetadata tagA;
                TagMetadata graphTagA;
                TagMetadata edgeTagA;
                TagMetadata tagB;
                VectorMetadata vectorA;
                VectorMetadata graphVectorA;
                VectorMetadata edgeVectorA;
                VectorMetadata vectorB;
                Credential credential;
                Credential targetCredential;

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = _McpEnvironment.DatabasePath
                })))
                {
                    client.InitializeRepository();

                    tenant = await client.Tenant.Create(new TenantMetadata { Name = "MCP RBAC Tenant" }, cancellationToken).ConfigureAwait(false);
                    targetTenant = await client.Tenant.Create(new TenantMetadata { Name = "MCP RBAC Admin Boundary Tenant" }, cancellationToken).ConfigureAwait(false);
                    user = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "MCP",
                        LastName = "Viewer",
                        Email = "mcp-rbac-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = "password"
                    }, cancellationToken).ConfigureAwait(false);
                    targetUser = await client.User.Create(new UserMaster
                    {
                        TenantGUID = targetTenant.GUID,
                        FirstName = "MCP",
                        LastName = "AdminBoundary",
                        Email = "mcp-admin-boundary-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = "password"
                    }, cancellationToken).ConfigureAwait(false);
                    graphA = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "MCP RBAC Allowed Graph" }, cancellationToken).ConfigureAwait(false);
                    graphB = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "MCP RBAC Denied Graph" }, cancellationToken).ConfigureAwait(false);
                    graphC = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "MCP RBAC Allowed Empty Graph" }, cancellationToken).ConfigureAwait(false);
                    nodeA = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Name = "Readable MCP Node"
                    }, cancellationToken).ConfigureAwait(false);
                    nodeA2 = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Name = "Readable MCP Node 2"
                    }, cancellationToken).ConfigureAwait(false);
                    nodeA3 = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Name = "Writable MCP Boundary Node"
                    }, cancellationToken).ConfigureAwait(false);
                    nodeB = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphB.GUID,
                        Name = "Denied MCP Node"
                    }, cancellationToken).ConfigureAwait(false);
                    nodeB2 = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphB.GUID,
                        Name = "Denied MCP Node 2"
                    }, cancellationToken).ConfigureAwait(false);
                    edgeA = await client.Edge.Create(new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        From = nodeA.GUID,
                        To = nodeA2.GUID,
                        Name = "Readable MCP Edge"
                    }, cancellationToken).ConfigureAwait(false);
                    edgeB = await client.Edge.Create(new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphB.GUID,
                        From = nodeB.GUID,
                        To = nodeB2.GUID,
                        Name = "Denied MCP Edge"
                    }, cancellationToken).ConfigureAwait(false);
                    labelA = await client.Label.Create(new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        NodeGUID = nodeA.GUID,
                        Label = "ReadableMcpLabel"
                    }, cancellationToken).ConfigureAwait(false);
                    graphLabelA = await client.Label.Create(new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Label = "ReadableMcpGraphLabel"
                    }, cancellationToken).ConfigureAwait(false);
                    edgeLabelA = await client.Label.Create(new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        EdgeGUID = edgeA.GUID,
                        Label = "ReadableMcpEdgeLabel"
                    }, cancellationToken).ConfigureAwait(false);
                    tagA = await client.Tag.Create(new TagMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        NodeGUID = nodeA.GUID,
                        Key = "readable",
                        Value = "mcp-tag"
                    }, cancellationToken).ConfigureAwait(false);
                    graphTagA = await client.Tag.Create(new TagMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Key = "readable-graph",
                        Value = "mcp-graph-tag"
                    }, cancellationToken).ConfigureAwait(false);
                    edgeTagA = await client.Tag.Create(new TagMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        EdgeGUID = edgeA.GUID,
                        Key = "readable-edge",
                        Value = "mcp-edge-tag"
                    }, cancellationToken).ConfigureAwait(false);
                    vectorA = await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        NodeGUID = nodeA.GUID,
                        Model = "mcp-rbac",
                        Content = "readable mcp vector",
                        Dimensionality = 4,
                        Vectors = BuildDeterministicVector(0, 4)
                    }, cancellationToken).ConfigureAwait(false);
                    graphVectorA = await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Model = "mcp-rbac",
                        Content = "readable graph mcp vector",
                        Dimensionality = 4,
                        Vectors = BuildDeterministicVector(1, 4)
                    }, cancellationToken).ConfigureAwait(false);
                    edgeVectorA = await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        EdgeGUID = edgeA.GUID,
                        Model = "mcp-rbac",
                        Content = "readable edge mcp vector",
                        Dimensionality = 4,
                        Vectors = BuildDeterministicVector(2, 4)
                    }, cancellationToken).ConfigureAwait(false);
                    labelB = await client.Label.Create(new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphB.GUID,
                        NodeGUID = nodeB.GUID,
                        Label = "DeniedMcpLabel"
                    }, cancellationToken).ConfigureAwait(false);
                    tagB = await client.Tag.Create(new TagMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphB.GUID,
                        NodeGUID = nodeB.GUID,
                        Key = "denied",
                        Value = "mcp-tag"
                    }, cancellationToken).ConfigureAwait(false);
                    vectorB = await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphB.GUID,
                        NodeGUID = nodeB.GUID,
                        Model = "mcp-rbac",
                        Content = "denied mcp vector",
                        Dimensionality = 4,
                        Vectors = BuildDeterministicVector(3, 4)
                    }, cancellationToken).ConfigureAwait(false);

                    credential = await client.Credential.Create(new Credential
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        Name = "MCP Read Credential",
                        BearerToken = bearerToken,
                        Scopes = new List<string> { "read" },
                        GraphGUIDs = new List<Guid> { graphA.GUID, graphC.GUID }
                    }, cancellationToken).ConfigureAwait(false);
                    targetCredential = await client.Credential.Create(new Credential
                    {
                        TenantGUID = targetTenant.GUID,
                        UserGUID = targetUser.GUID,
                        Name = "MCP Admin Boundary Target Credential",
                        BearerToken = "mcp-admin-boundary-target-" + Guid.NewGuid().ToString("N"),
                        Scopes = new List<string> { "read" }
                    }, cancellationToken).ConfigureAwait(false);
                }

                string nodeReadBody = await _McpClient.CallAsync<string>("node/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                Node readNode = _McpSerializer.DeserializeJson<Node>(nodeReadBody);
                AssertEqual(nodeA.GUID, readNode.GUID, "MCP read-scoped credential can read allowed node");

                string edgeReadBody = await _McpClient.CallAsync<string>("edge/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    edgeGuid = edgeA.GUID.ToString()
                }).ConfigureAwait(false);
                Edge readEdge = _McpSerializer.DeserializeJson<Edge>(edgeReadBody);
                AssertEqual(edgeA.GUID, readEdge.GUID, "MCP read-scoped credential can read allowed edge");

                string edgeAllBody = await _McpClient.CallAsync<string>("edge/all", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> edgeAll = _McpSerializer.DeserializeJson<List<Edge>>(edgeAllBody);
                AssertTrue(edgeAll.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can list allowed graph edges");

                string edgeReadAllInGraphBody = await _McpClient.CallAsync<string>("edge/readallingraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> graphEdges = _McpSerializer.DeserializeJson<List<Edge>>(edgeReadAllInGraphBody);
                AssertTrue(graphEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can read all edges in allowed graph");

                string edgeReadAllInTenantBody = await _McpClient.CallAsync<string>("edge/readallintenant", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> tenantEdges = _McpSerializer.DeserializeJson<List<Edge>>(edgeReadAllInTenantBody);
                AssertTrue(tenantEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can invoke tenant edge listing");

                string edgeGetManyBody = await _McpClient.CallAsync<string>("edge/getmany", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    edgeGuids = new[] { edgeA.GUID.ToString() }
                }).ConfigureAwait(false);
                List<Edge> manyEdges = _McpSerializer.DeserializeJson<List<Edge>>(edgeGetManyBody);
                AssertEqual(1, manyEdges.Count, "MCP read-scoped credential can read many allowed edges");

                string edgeExistsBody = await _McpClient.CallAsync<string>("edge/exists", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    edgeGuid = edgeA.GUID.ToString()
                }).ConfigureAwait(false);
                AssertEqual("true", edgeExistsBody, "MCP read-scoped credential can check allowed edge existence");

                string nodeEdgesBody = await _McpClient.CallAsync<string>("edge/nodeedges", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> nodeEdges = _McpSerializer.DeserializeJson<List<Edge>>(nodeEdgesBody);
                AssertTrue(nodeEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can read allowed node edges");

                string filteredNodeEdgesBody = await _McpClient.CallAsync<string>("edge/nodeedges", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString(),
                    labels = _McpSerializer.SerializeJson(new List<string> { edgeLabelA.Label }, false)
                }).ConfigureAwait(false);
                List<Edge> filteredNodeEdges = _McpSerializer.DeserializeJson<List<Edge>>(filteredNodeEdgesBody);
                AssertTrue(filteredNodeEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can read filtered allowed node edges");

                string fromNodeEdgesBody = await _McpClient.CallAsync<string>("edge/fromnode", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> fromNodeEdges = _McpSerializer.DeserializeJson<List<Edge>>(fromNodeEdgesBody);
                AssertTrue(fromNodeEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can read allowed outgoing edges");

                string toNodeEdgesBody = await _McpClient.CallAsync<string>("edge/tonode", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA2.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> toNodeEdges = _McpSerializer.DeserializeJson<List<Edge>>(toNodeEdgesBody);
                AssertTrue(toNodeEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can read allowed incoming edges");

                string betweenNodeEdgesBody = await _McpClient.CallAsync<string>("edge/betweennodes", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    fromNodeGuid = nodeA.GUID.ToString(),
                    toNodeGuid = nodeA2.GUID.ToString()
                }).ConfigureAwait(false);
                List<Edge> betweenNodeEdges = _McpSerializer.DeserializeJson<List<Edge>>(betweenNodeEdgesBody);
                AssertTrue(betweenNodeEdges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can read allowed edges between nodes");

                SearchRequest edgeSearchRequest = new SearchRequest
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    Name = edgeA.Name
                };
                string edgeSearchBody = await _McpClient.CallAsync<string>("edge/search", new
                {
                    request = _McpSerializer.SerializeJson(edgeSearchRequest, false)
                }).ConfigureAwait(false);
                SearchResult edgeSearch = _McpSerializer.DeserializeJson<SearchResult>(edgeSearchBody);
                AssertTrue(edgeSearch.Edges.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can search allowed edges");

                string edgeReadFirstBody = await _McpClient.CallAsync<string>("edge/readfirst", new
                {
                    request = _McpSerializer.SerializeJson(edgeSearchRequest, false)
                }).ConfigureAwait(false);
                Edge firstEdge = _McpSerializer.DeserializeJson<Edge>(edgeReadFirstBody);
                AssertEqual(edgeA.GUID, firstEdge.GUID, "MCP read-scoped credential can read first allowed edge");

                string edgeEnumerateBody = await _McpClient.CallAsync<string>("edge/enumerate", new
                {
                    query = _McpSerializer.SerializeJson(new EnumerationRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        MaxResults = 10
                    }, false)
                }).ConfigureAwait(false);
                EnumerationResult<Edge> edgeEnumeration = _McpSerializer.DeserializeJson<EnumerationResult<Edge>>(edgeEnumerateBody);
                AssertTrue(edgeEnumeration.Objects.Any(edge => edge.GUID == edgeA.GUID), "MCP read-scoped credential can enumerate allowed graph edges");

                string labelReadBody = await _McpClient.CallAsync<string>("label/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    labelGuid = labelA.GUID.ToString()
                }).ConfigureAwait(false);
                LabelMetadata readLabel = _McpSerializer.DeserializeJson<LabelMetadata>(labelReadBody);
                AssertEqual(labelA.GUID, readLabel.GUID, "MCP read-scoped credential can read allowed label");

                string labelAllBody = await _McpClient.CallAsync<string>("label/all", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<LabelMetadata> labelAll = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelAllBody);
                AssertTrue(labelAll.Any(label => label.GUID == labelA.GUID), "MCP read-scoped credential can list labels");

                string labelReadAllInTenantBody = await _McpClient.CallAsync<string>("label/readallintenant", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<LabelMetadata> tenantLabels = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelReadAllInTenantBody);
                AssertTrue(tenantLabels.Any(label => label.GUID == labelA.GUID), "MCP read-scoped credential can invoke tenant label listing");

                string labelReadAllInGraphBody = await _McpClient.CallAsync<string>("label/readallingraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<LabelMetadata> graphLabels = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelReadAllInGraphBody);
                AssertTrue(graphLabels.Any(label => label.GUID == labelA.GUID), "MCP read-scoped credential can read all labels in allowed graph");

                string labelReadManyGraphBody = await _McpClient.CallAsync<string>("label/readmanygraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<LabelMetadata> graphScopedLabels = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelReadManyGraphBody);
                AssertTrue(graphScopedLabels.Any(label => label.GUID == graphLabelA.GUID), "MCP read-scoped credential can read graph labels");

                string labelReadManyNodeBody = await _McpClient.CallAsync<string>("label/readmanynode", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<LabelMetadata> nodeLabels = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelReadManyNodeBody);
                AssertTrue(nodeLabels.Any(label => label.GUID == labelA.GUID), "MCP read-scoped credential can read node labels");

                string labelReadManyEdgeBody = await _McpClient.CallAsync<string>("label/readmanyedge", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    edgeGuid = edgeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<LabelMetadata> edgeLabels = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelReadManyEdgeBody);
                AssertTrue(edgeLabels.Any(label => label.GUID == edgeLabelA.GUID), "MCP read-scoped credential can read edge labels");

                string labelGetManyBody = await _McpClient.CallAsync<string>("label/getmany", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    labelGuids = new[] { labelA.GUID.ToString(), edgeLabelA.GUID.ToString() }
                }).ConfigureAwait(false);
                List<LabelMetadata> manyLabels = _McpSerializer.DeserializeJson<List<LabelMetadata>>(labelGetManyBody);
                AssertEqual(2, manyLabels.Count, "MCP read-scoped credential can read many allowed labels");

                string labelExistsBody = await _McpClient.CallAsync<string>("label/exists", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    labelGuid = labelA.GUID.ToString()
                }).ConfigureAwait(false);
                AssertEqual("true", labelExistsBody, "MCP read-scoped credential can check allowed label existence");

                string labelEnumerateBody = await _McpClient.CallAsync<string>("label/enumerate", new
                {
                    query = _McpSerializer.SerializeJson(new EnumerationRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        MaxResults = 10
                    }, false)
                }).ConfigureAwait(false);
                EnumerationResult<LabelMetadata> labelEnumeration = _McpSerializer.DeserializeJson<EnumerationResult<LabelMetadata>>(labelEnumerateBody);
                AssertTrue(labelEnumeration.Objects.Any(label => label.GUID == labelA.GUID), "MCP read-scoped credential can enumerate allowed graph labels");

                string tagReadBody = await _McpClient.CallAsync<string>("tag/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    tagGuid = tagA.GUID.ToString()
                }).ConfigureAwait(false);
                TagMetadata readTag = _McpSerializer.DeserializeJson<TagMetadata>(tagReadBody);
                AssertEqual(tagA.GUID, readTag.GUID, "MCP read-scoped credential can read allowed tag");

                string tagReadManyBody = await _McpClient.CallAsync<string>("tag/readmany", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<TagMetadata> readManyTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagReadManyBody);
                AssertTrue(readManyTags.Any(tag => tag.GUID == tagA.GUID), "MCP read-scoped credential can list allowed graph tags through readmany");

                string tagReadAllInTenantBody = await _McpClient.CallAsync<string>("tag/readallintenant", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<TagMetadata> tenantTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagReadAllInTenantBody);
                AssertTrue(tenantTags.Any(tag => tag.GUID == tagA.GUID), "MCP read-scoped credential can invoke tenant tag listing");

                string tagReadAllInGraphBody = await _McpClient.CallAsync<string>("tag/readallingraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<TagMetadata> graphTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagReadAllInGraphBody);
                AssertTrue(graphTags.Any(tag => tag.GUID == tagA.GUID), "MCP read-scoped credential can read all tags in allowed graph");

                string tagReadManyGraphBody = await _McpClient.CallAsync<string>("tag/readmanygraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<TagMetadata> graphScopedTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagReadManyGraphBody);
                AssertTrue(graphScopedTags.Any(tag => tag.GUID == graphTagA.GUID), "MCP read-scoped credential can read graph tags");

                string tagReadManyNodeBody = await _McpClient.CallAsync<string>("tag/readmanynode", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<TagMetadata> nodeTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagReadManyNodeBody);
                AssertTrue(nodeTags.Any(tag => tag.GUID == tagA.GUID), "MCP read-scoped credential can read node tags");

                string tagReadManyEdgeBody = await _McpClient.CallAsync<string>("tag/readmanyedge", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    edgeGuid = edgeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<TagMetadata> edgeTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagReadManyEdgeBody);
                AssertTrue(edgeTags.Any(tag => tag.GUID == edgeTagA.GUID), "MCP read-scoped credential can read edge tags");

                string tagGetManyBody = await _McpClient.CallAsync<string>("tag/getmany", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    tagGuids = new[] { tagA.GUID.ToString(), edgeTagA.GUID.ToString() }
                }).ConfigureAwait(false);
                List<TagMetadata> manyTags = _McpSerializer.DeserializeJson<List<TagMetadata>>(tagGetManyBody);
                AssertEqual(2, manyTags.Count, "MCP read-scoped credential can read many allowed tags");

                string tagExistsBody = await _McpClient.CallAsync<string>("tag/exists", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    tagGuid = tagA.GUID.ToString()
                }).ConfigureAwait(false);
                AssertEqual("true", tagExistsBody, "MCP read-scoped credential can check allowed tag existence");

                string tagEnumerateBody = await _McpClient.CallAsync<string>("tag/enumerate", new
                {
                    query = _McpSerializer.SerializeJson(new EnumerationRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        MaxResults = 10
                    }, false)
                }).ConfigureAwait(false);
                EnumerationResult<TagMetadata> tagEnumeration = _McpSerializer.DeserializeJson<EnumerationResult<TagMetadata>>(tagEnumerateBody);
                AssertTrue(tagEnumeration.Objects.Any(tag => tag.GUID == tagA.GUID), "MCP read-scoped credential can enumerate allowed graph tags");

                string vectorReadBody = await _McpClient.CallAsync<string>("vector/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    vectorGuid = vectorA.GUID.ToString()
                }).ConfigureAwait(false);
                VectorMetadata readVector = _McpSerializer.DeserializeJson<VectorMetadata>(vectorReadBody);
                AssertEqual(vectorA.GUID, readVector.GUID, "MCP read-scoped credential can read allowed vector");

                string vectorAllBody = await _McpClient.CallAsync<string>("vector/all", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<VectorMetadata> allVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorAllBody);
                AssertTrue(allVectors.Any(vector => vector.GUID == vectorA.GUID), "MCP read-scoped credential can list vectors");

                string vectorReadAllInTenantBody = await _McpClient.CallAsync<string>("vector/readallintenant", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<VectorMetadata> tenantVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorReadAllInTenantBody);
                AssertTrue(tenantVectors.Any(vector => vector.GUID == vectorA.GUID), "MCP read-scoped credential can invoke tenant vector listing");

                string vectorReadAllInGraphBody = await _McpClient.CallAsync<string>("vector/readallingraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<VectorMetadata> graphVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorReadAllInGraphBody);
                AssertTrue(graphVectors.Any(vector => vector.GUID == vectorA.GUID), "MCP read-scoped credential can read all vectors in allowed graph");

                string vectorReadManyGraphBody = await _McpClient.CallAsync<string>("vector/readmanygraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<VectorMetadata> graphScopedVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorReadManyGraphBody);
                AssertTrue(graphScopedVectors.Any(vector => vector.GUID == graphVectorA.GUID), "MCP read-scoped credential can read graph vectors");

                string vectorReadManyNodeBody = await _McpClient.CallAsync<string>("vector/readmanynode", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<VectorMetadata> nodeVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorReadManyNodeBody);
                AssertTrue(nodeVectors.Any(vector => vector.GUID == vectorA.GUID), "MCP read-scoped credential can read node vectors");

                string vectorReadManyEdgeBody = await _McpClient.CallAsync<string>("vector/readmanyedge", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    edgeGuid = edgeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<VectorMetadata> edgeVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorReadManyEdgeBody);
                AssertTrue(edgeVectors.Any(vector => vector.GUID == edgeVectorA.GUID), "MCP read-scoped credential can read edge vectors");

                string vectorGetManyBody = await _McpClient.CallAsync<string>("vector/getmany", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    vectorGuids = new[] { vectorA.GUID.ToString(), edgeVectorA.GUID.ToString() }
                }).ConfigureAwait(false);
                List<VectorMetadata> manyVectors = _McpSerializer.DeserializeJson<List<VectorMetadata>>(vectorGetManyBody);
                AssertEqual(2, manyVectors.Count, "MCP read-scoped credential can read many allowed vectors");

                string vectorExistsBody = await _McpClient.CallAsync<string>("vector/exists", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    vectorGuid = vectorA.GUID.ToString()
                }).ConfigureAwait(false);
                AssertEqual("true", vectorExistsBody, "MCP read-scoped credential can check allowed vector existence");

                string vectorEnumerateBody = await _McpClient.CallAsync<string>("vector/enumerate", new
                {
                    query = _McpSerializer.SerializeJson(new EnumerationRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        MaxResults = 10
                    }, false)
                }).ConfigureAwait(false);
                EnumerationResult<VectorMetadata> vectorEnumeration = _McpSerializer.DeserializeJson<EnumerationResult<VectorMetadata>>(vectorEnumerateBody);
                AssertTrue(vectorEnumeration.Objects.Any(vector => vector.GUID == vectorA.GUID), "MCP read-scoped credential can enumerate allowed graph vectors");

                string vectorSearchBody = await _McpClient.CallAsync<string>("vector/search", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    searchRequest = _McpSerializer.SerializeJson(new VectorSearchRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Embeddings = BuildDeterministicVector(0, 4),
                        TopK = 5
                    }, false)
                }).ConfigureAwait(false);
                List<VectorSearchResult> vectorSearchResults = _McpSerializer.DeserializeJson<List<VectorSearchResult>>(vectorSearchBody);
                AssertTrue(vectorSearchResults.Any(result => result.Node != null && result.Node.GUID == nodeA.GUID), "MCP read-scoped credential can search allowed graph vectors");

                string queryBody = await _McpClient.CallAsync<string>("graph/query", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    query = "MATCH (n) RETURN n LIMIT 10"
                }).ConfigureAwait(false);
                GraphQueryResult queryResult = _McpSerializer.DeserializeJson<GraphQueryResult>(queryBody);
                AssertFalse(queryResult.Mutated, "MCP read query does not mutate");
                AssertTrue(queryResult.RowCount >= 1, "MCP read query returns rows");

                string graphReadBody = await _McpClient.CallAsync<string>("graph/get", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                Graph readGraph = _McpSerializer.DeserializeJson<Graph>(graphReadBody);
                AssertEqual(graphA.GUID, readGraph.GUID, "MCP read-scoped credential can read allowed graph");

                string batchExistenceBody = await _McpClient.CallAsync<string>("batch/existence", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodes = new[] { nodeA.GUID.ToString() },
                    edges = new[] { edgeA.GUID.ToString() },
                    vectors = new[] { vectorA.GUID.ToString() }
                }).ConfigureAwait(false);
                ExistenceResult batchExistence = _McpSerializer.DeserializeJson<ExistenceResult>(batchExistenceBody);
                AssertNotNull(batchExistence.ExistingNodes, "MCP batch existence node result");
                AssertNotNull(batchExistence.ExistingEdges, "MCP batch existence edge result");
                AssertNotNull(batchExistence.ExistingVectors, "MCP batch existence vector result");
                AssertTrue(batchExistence.ExistingNodes.Contains(nodeA.GUID), "MCP read-scoped credential can run allowed batch node existence");
                AssertTrue(batchExistence.ExistingEdges.Contains(edgeA.GUID), "MCP read-scoped credential can run allowed batch edge existence");
                AssertTrue(batchExistence.ExistingVectors.Contains(vectorA.GUID), "MCP read-scoped credential can run allowed batch vector existence");

                string nodeAllBody = await _McpClient.CallAsync<string>("node/all", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> nodeAll = _McpSerializer.DeserializeJson<List<Node>>(nodeAllBody);
                AssertTrue(nodeAll.Any(node => node.GUID == nodeA.GUID), "MCP read-scoped credential can list allowed graph nodes");

                string nodeReadAllInTenantBody = await _McpClient.CallAsync<string>("node/readallintenant", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> tenantNodes = _McpSerializer.DeserializeJson<List<Node>>(nodeReadAllInTenantBody);
                AssertTrue(tenantNodes.Any(node => node.GUID == nodeA.GUID), "MCP read-scoped credential can invoke tenant node listing");

                string nodeReadAllInGraphBody = await _McpClient.CallAsync<string>("node/readallingraph", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> graphNodes = _McpSerializer.DeserializeJson<List<Node>>(nodeReadAllInGraphBody);
                AssertTrue(graphNodes.Any(node => node.GUID == nodeA.GUID), "MCP read-scoped credential can read all nodes in allowed graph");

                string mostConnectedBody = await _McpClient.CallAsync<string>("node/readmostconnected", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> mostConnectedNodes = _McpSerializer.DeserializeJson<List<Node>>(mostConnectedBody);
                AssertNotNull(mostConnectedNodes, "MCP read-scoped credential can read most-connected nodes in allowed graph");

                string leastConnectedBody = await _McpClient.CallAsync<string>("node/readleastconnected", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> leastConnectedNodes = _McpSerializer.DeserializeJson<List<Node>>(leastConnectedBody);
                AssertNotNull(leastConnectedNodes, "MCP read-scoped credential can read least-connected nodes in allowed graph");

                string nodeGetManyBody = await _McpClient.CallAsync<string>("node/getmany", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuids = new[] { nodeA.GUID.ToString(), nodeA2.GUID.ToString() }
                }).ConfigureAwait(false);
                List<Node> manyNodes = _McpSerializer.DeserializeJson<List<Node>>(nodeGetManyBody);
                AssertEqual(2, manyNodes.Count, "MCP read-scoped credential can read many allowed nodes");

                string nodeExistsBody = await _McpClient.CallAsync<string>("node/exists", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                AssertEqual("true", nodeExistsBody, "MCP read-scoped credential can check allowed node existence");

                SearchRequest nodeSearchRequest = new SearchRequest
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    Name = nodeA3.Name
                };
                string nodeSearchBody = await _McpClient.CallAsync<string>("node/search", new
                {
                    searchRequest = _McpSerializer.SerializeJson(nodeSearchRequest, false)
                }).ConfigureAwait(false);
                SearchResult nodeSearch = _McpSerializer.DeserializeJson<SearchResult>(nodeSearchBody);
                AssertTrue(nodeSearch.Nodes.Any(node => node.GUID == nodeA3.GUID), "MCP read-scoped credential can search allowed nodes");

                string nodeReadFirstBody = await _McpClient.CallAsync<string>("node/readfirst", new
                {
                    searchRequest = _McpSerializer.SerializeJson(nodeSearchRequest, false)
                }).ConfigureAwait(false);
                Node firstNode = _McpSerializer.DeserializeJson<Node>(nodeReadFirstBody);
                AssertEqual(nodeA3.GUID, firstNode.GUID, "MCP read-scoped credential can read first allowed node");

                string nodeEnumerateBody = await _McpClient.CallAsync<string>("node/enumerate", new
                {
                    query = _McpSerializer.SerializeJson(new EnumerationRequest
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        MaxResults = 10
                    }, false)
                }).ConfigureAwait(false);
                EnumerationResult<Node> nodeEnumeration = _McpSerializer.DeserializeJson<EnumerationResult<Node>>(nodeEnumerateBody);
                AssertTrue(nodeEnumeration.Objects.Any(node => node.GUID == nodeA.GUID), "MCP read-scoped credential can enumerate allowed graph nodes");

                string nodeParentsBody = await _McpClient.CallAsync<string>("node/parents", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA2.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> parentNodes = _McpSerializer.DeserializeJson<List<Node>>(nodeParentsBody);
                AssertTrue(parentNodes.Any(node => node.GUID == nodeA.GUID), "MCP read-scoped credential can read allowed node parents");

                string nodeChildrenBody = await _McpClient.CallAsync<string>("node/children", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> childNodes = _McpSerializer.DeserializeJson<List<Node>>(nodeChildrenBody);
                AssertTrue(childNodes.Any(node => node.GUID == nodeA2.GUID), "MCP read-scoped credential can read allowed node children");

                string nodeNeighborsBody = await _McpClient.CallAsync<string>("node/neighbors", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    nodeGuid = nodeA.GUID.ToString()
                }).ConfigureAwait(false);
                List<Node> neighborNodes = _McpSerializer.DeserializeJson<List<Node>>(nodeNeighborsBody);
                AssertTrue(neighborNodes.Any(node => node.GUID == nodeA2.GUID), "MCP read-scoped credential can read allowed node neighbors");

                string nodeTraverseBody = await _McpClient.CallAsync<string>("node/traverse", new
                {
                    tenantGuid = tenant.GUID.ToString(),
                    graphGuid = graphA.GUID.ToString(),
                    fromNodeGuid = nodeA.GUID.ToString(),
                    toNodeGuid = nodeA2.GUID.ToString(),
                    searchType = SearchTypeEnum.DepthFirstSearch.ToString()
                }).ConfigureAwait(false);
                List<RouteDetail> routes = _McpSerializer.DeserializeJson<List<RouteDetail>>(nodeTraverseBody);
                AssertTrue(routes.Count > 0, "MCP read-scoped credential can traverse allowed graph nodes");

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/create", new
                    {
                        name = "Blocked MCP Tenant"
                    }),
                    "MCP read-scoped credential cannot create tenants").ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/get", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot read tenant admin metadata").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TenantRead,
                    "AdminRequired",
                    "admin",
                    "MCP tenant read denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/all", new { }),
                    "MCP read-scoped credential cannot list tenants").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    null,
                    null,
                    RequestTypeEnum.TenantReadAll,
                    "AdminRequired",
                    "admin",
                    "MCP tenant list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest { MaxResults = 10 }, false)
                    }),
                    "MCP read-scoped credential cannot enumerate tenants").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    null,
                    null,
                    RequestTypeEnum.TenantEnumerate,
                    "AdminRequired",
                    "admin",
                    "MCP tenant enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/statisticsall", new { }),
                    "MCP read-scoped credential cannot read all tenant statistics").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    null,
                    null,
                    RequestTypeEnum.TenantStatistics,
                    "AdminRequired",
                    "admin",
                    "MCP tenant all-statistics denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot check tenant existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TenantExists,
                    "AdminRequired",
                    "admin",
                    "MCP tenant exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                AuthorizationRole blockedMcpRole = new AuthorizationRole
                {
                    Name = "BlockedMcpRole" + Guid.NewGuid().ToString("N"),
                    DisplayName = "Blocked MCP Role",
                    Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Read },
                    ResourceTypes = new List<AuthorizationResourceTypeEnum> { AuthorizationResourceTypeEnum.Node }
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("authorization/role/create", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        role = _McpSerializer.SerializeJson(blockedMcpRole, false)
                    }),
                    "MCP read-scoped credential cannot create authorization roles").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.AuthorizationRoleCreate,
                    "MissingScope",
                    "admin",
                    "MCP authorization role create denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("authorization/role/all", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        includeBuiltIns = false
                    }),
                    "MCP read-scoped credential cannot list authorization roles").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.AuthorizationRoleReadAll,
                    "MissingScope",
                    "admin",
                    "MCP authorization role list denial audit",
                    cancellationToken).ConfigureAwait(false);

                UserRoleAssignment blockedUserRole = new UserRoleAssignment
                {
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA.GUID
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("authorization/userrole/create", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        userGuid = user.GUID.ToString(),
                        assignment = _McpSerializer.SerializeJson(blockedUserRole, false)
                    }),
                    "MCP read-scoped credential cannot assign user roles").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserRoleAssignmentCreate,
                    "MissingScope",
                    "admin",
                    "MCP user role assignment denial audit",
                    cancellationToken).ConfigureAwait(false);

                CredentialScopeAssignment blockedCredentialScope = new CredentialScopeAssignment
                {
                    RoleName = AuthorizationPolicyDefinitions.ViewerRoleName,
                    ResourceScope = AuthorizationResourceScopeEnum.Graph,
                    GraphGUID = graphA.GUID
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("authorization/credentialscope/create", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        credentialGuid = credential.GUID.ToString(),
                        assignment = _McpSerializer.SerializeJson(blockedCredentialScope, false)
                    }),
                    "MCP read-scoped credential cannot assign credential scopes").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialScopeAssignmentCreate,
                    "MissingScope",
                    "admin",
                    "MCP credential scope assignment denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("authorization/user/permissions", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        userGuid = user.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot read user effective permissions").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserEffectivePermissionsRead,
                    "MissingScope",
                    "admin",
                    "MCP user effective permissions denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("authorization/credential/permissions", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        credentialGuid = credential.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot read credential effective permissions").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialEffectivePermissionsRead,
                    "MissingScope",
                    "admin",
                    "MCP credential effective permissions denial audit",
                    cancellationToken).ConfigureAwait(false);

                string tenantStatisticsBody = await _McpClient.CallAsync<string>("tenant/statistics", new
                {
                    tenantGuid = tenant.GUID.ToString()
                }).ConfigureAwait(false);
                TenantStatistics tenantStatistics = _McpSerializer.DeserializeJson<TenantStatistics>(tenantStatisticsBody);
                AssertNotNull(tenantStatistics, "MCP read-scoped credential can read same-tenant statistics");

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/getmany", new
                    {
                        tenantGuids = new[] { tenant.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot read many tenants").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TenantRead,
                    "AdminRequired",
                    "admin",
                    "MCP tenant getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                TenantMetadata tenantUpdate = new TenantMetadata
                {
                    GUID = targetTenant.GUID,
                    Name = "Blocked MCP Tenant Update"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/update", new
                    {
                        tenant = _McpSerializer.SerializeJson(tenantUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update tenants").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.TenantUpdate,
                    "TenantDenied",
                    null,
                    "MCP tenant update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tenant/delete", new
                    {
                        tenantGuid = targetTenant.GUID.ToString(),
                        force = true
                    }),
                    "MCP read-scoped credential cannot delete tenants").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.TenantDelete,
                    "TenantDenied",
                    null,
                    "MCP tenant delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                UserMaster userCreate = new UserMaster
                {
                    TenantGUID = tenant.GUID,
                    FirstName = "Blocked",
                    LastName = "MCP",
                    Email = "blocked-mcp-user-" + Guid.NewGuid().ToString("N") + "@example.com",
                    Password = "password"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/create", new
                    {
                        user = _McpSerializer.SerializeJson(userCreate, false)
                    }),
                    "MCP read-scoped credential cannot create users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserCreate,
                    "MissingScope",
                    "admin",
                    "MCP user create denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        userGuid = user.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot read users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserRead,
                    "AdminRequired",
                    "admin",
                    "MCP user read denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/all", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot list users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserReadAll,
                    "AdminRequired",
                    "admin",
                    "MCP user list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP read-scoped credential cannot enumerate users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserEnumerate,
                    "AdminRequired",
                    "admin",
                    "MCP user enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        userGuid = user.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot check user existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserExists,
                    "AdminRequired",
                    "admin",
                    "MCP user exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        userGuids = new[] { user.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot read many users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.UserRead,
                    "AdminRequired",
                    "admin",
                    "MCP user getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                UserMaster userUpdate = new UserMaster
                {
                    GUID = targetUser.GUID,
                    TenantGUID = targetTenant.GUID,
                    FirstName = "Blocked",
                    LastName = "MCP Update",
                    Email = targetUser.Email,
                    Password = "password"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/update", new
                    {
                        user = _McpSerializer.SerializeJson(userUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.UserUpdate,
                    "TenantDenied",
                    null,
                    "MCP user update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("user/delete", new
                    {
                        tenantGuid = targetTenant.GUID.ToString(),
                        userGuid = targetUser.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete users").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.UserDelete,
                    "TenantDenied",
                    null,
                    "MCP user delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                Credential credentialCreate = new Credential
                {
                    TenantGUID = tenant.GUID,
                    UserGUID = user.GUID,
                    Name = "Blocked MCP Credential Create",
                    BearerToken = "blocked-mcp-credential-" + Guid.NewGuid().ToString("N"),
                    Scopes = new List<string> { "read" }
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/create", new
                    {
                        credential = _McpSerializer.SerializeJson(credentialCreate, false)
                    }),
                    "MCP read-scoped credential cannot create credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialCreate,
                    "MissingScope",
                    "admin",
                    "MCP credential create denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/get", new
                    {
                        tenantGuid = targetTenant.GUID.ToString(),
                        credentialGuid = targetCredential.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot read credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.CredentialRead,
                    "TenantDenied",
                    null,
                    "MCP credential read denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/all", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot list credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialReadAll,
                    "AdminRequired",
                    "admin",
                    "MCP credential list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP read-scoped credential cannot enumerate credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialEnumerate,
                    "AdminRequired",
                    "admin",
                    "MCP credential enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        credentialGuid = credential.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot check credential existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialExists,
                    "AdminRequired",
                    "admin",
                    "MCP credential exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        credentialGuids = new[] { credential.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot read many credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.CredentialRead,
                    "AdminRequired",
                    "admin",
                    "MCP credential getmany denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/getbybearertoken", new
                    {
                        bearerToken = targetCredential.BearerToken
                    }),
                    "MCP read-scoped credential cannot read credentials by bearer token").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    null,
                    null,
                    RequestTypeEnum.CredentialReadByBearerToken,
                    "AdminRequired",
                    "admin",
                    "MCP credential bearer lookup denial audit",
                    cancellationToken).ConfigureAwait(false);

                Credential credentialUpdate = new Credential
                {
                    GUID = targetCredential.GUID,
                    TenantGUID = targetTenant.GUID,
                    UserGUID = targetUser.GUID,
                    Name = "Blocked MCP Credential Update",
                    BearerToken = targetCredential.BearerToken,
                    Scopes = new List<string> { "read" }
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/update", new
                    {
                        credential = _McpSerializer.SerializeJson(credentialUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.CredentialUpdate,
                    "TenantDenied",
                    null,
                    "MCP credential update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/deletebyuser", new
                    {
                        tenantGuid = targetTenant.GUID.ToString(),
                        userGuid = targetUser.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete credentials by user").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.CredentialDeleteByUser,
                    "TenantDenied",
                    null,
                    "MCP credential delete-by-user denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/deleteallintenant", new
                    {
                        tenantGuid = targetTenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all credentials in a tenant").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.CredentialDeleteAllInTenant,
                    "TenantDenied",
                    null,
                    "MCP credential delete-all denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("credential/delete", new
                    {
                        tenantGuid = targetTenant.GUID.ToString(),
                        credentialGuid = targetCredential.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete credentials").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    targetTenant.GUID,
                    null,
                    RequestTypeEnum.CredentialDelete,
                    "TenantDenied",
                    null,
                    "MCP credential delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("graph/create", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        name = "Blocked MCP Graph"
                    }),
                    "MCP read-scoped credential cannot create graphs").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.GraphCreate,
                    "MissingScope",
                    "write",
                    "MCP graph create denial audit",
                    cancellationToken).ConfigureAwait(false);

                Graph graphUpdate = new Graph
                {
                    TenantGUID = tenant.GUID,
                    GUID = graphC.GUID,
                    Name = "Blocked MCP Graph Update"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("graph/update", new
                    {
                        graph = _McpSerializer.SerializeJson(graphUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update graphs").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphC.GUID,
                    RequestTypeEnum.GraphUpdate,
                    "MissingScope",
                    "write",
                    "MCP graph update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("graph/delete", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphC.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete graphs").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphC.GUID,
                    RequestTypeEnum.GraphDelete,
                    "MissingScope",
                    "write",
                    "MCP graph delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/create", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        name = "Blocked MCP Node"
                    }),
                    "MCP read-scoped credential cannot create nodes").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.NodeCreate,
                    "MissingScope",
                    "write",
                    "MCP node create denial audit",
                    cancellationToken).ConfigureAwait(false);

                Node nodeUpdate = new Node
                {
                    GUID = nodeA3.GUID,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    Name = "Blocked MCP Node Update"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/update", new
                    {
                        node = _McpSerializer.SerializeJson(nodeUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update nodes").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.NodeUpdate,
                    "MissingScope",
                    "write",
                    "MCP node update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/delete", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuid = nodeA3.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete nodes").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.NodeDelete,
                    "MissingScope",
                    "write",
                    "MCP node delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                List<Node> blockedCreateManyNodes = new List<Node>
                {
                    new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        Name = "Blocked MCP Node CreateMany"
                    }
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/createmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodes = _McpSerializer.SerializeJson(blockedCreateManyNodes, false)
                    }),
                    "MCP read-scoped credential cannot bulk-create nodes").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.NodeCreateMany,
                    "MissingScope",
                    "write",
                    "MCP node create-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/deletemany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuids = new[] { nodeA3.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot bulk-delete nodes").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.NodeDeleteMany,
                    "MissingScope",
                    "write",
                    "MCP node delete-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/deleteall", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all nodes in a graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.NodeDeleteAll,
                    "MissingScope",
                    "write",
                    "MCP node delete-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/deleteallintenant", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all nodes in a tenant").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.NodeDeleteAllInTenant,
                    "MissingScope",
                    "write",
                    "MCP node delete-all-in-tenant denial audit",
                    cancellationToken).ConfigureAwait(false);

                Edge edgeCreate = new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    From = nodeA.GUID,
                    To = nodeA2.GUID,
                    Name = "Blocked MCP Edge Create"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/create", new
                    {
                        edge = _McpSerializer.SerializeJson(edgeCreate, false)
                    }),
                    "MCP read-scoped credential cannot create edges").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeCreate,
                    "MissingScope",
                    "write",
                    "MCP edge create denial audit",
                    cancellationToken).ConfigureAwait(false);

                Edge edgeUpdate = new Edge
                {
                    GUID = edgeA.GUID,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    From = nodeA.GUID,
                    To = nodeA2.GUID,
                    Name = "Blocked MCP Edge Update"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/update", new
                    {
                        edge = _McpSerializer.SerializeJson(edgeUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update edges").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeUpdate,
                    "MissingScope",
                    "write",
                    "MCP edge update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/delete", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        edgeGuid = edgeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete edges").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeDelete,
                    "MissingScope",
                    "write",
                    "MCP edge delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                List<Edge> blockedCreateManyEdges = new List<Edge>
                {
                    new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        From = nodeA.GUID,
                        To = nodeA2.GUID,
                        Name = "Blocked MCP Edge CreateMany"
                    }
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/createmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        edges = _McpSerializer.SerializeJson(blockedCreateManyEdges, false)
                    }),
                    "MCP read-scoped credential cannot bulk-create edges").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeCreateMany,
                    "MissingScope",
                    "write",
                    "MCP edge create-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/deletemany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        edgeGuids = new[] { edgeA.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot bulk-delete edges").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeDeleteMany,
                    "MissingScope",
                    "write",
                    "MCP edge delete-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/deleteallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all edges in a graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeDeleteAll,
                    "MissingScope",
                    "write",
                    "MCP edge delete-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/deleteallintenant", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all edges in a tenant").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.EdgeDeleteAllInTenant,
                    "MissingScope",
                    "write",
                    "MCP edge delete-all-in-tenant denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/deletenodeedges", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuid = nodeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete node edges").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeDeleteNodeEdges,
                    "MissingScope",
                    "write",
                    "MCP edge delete-node-edges denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/deletenodeedgesmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuids = new[] { nodeA.GUID.ToString(), nodeA2.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot delete node edges in bulk").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.EdgeDeleteNodeEdgesMany,
                    "MissingScope",
                    "write",
                    "MCP edge delete-node-edges-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                LabelMetadata labelCreate = new LabelMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    NodeGUID = nodeA.GUID,
                    Label = "BlockedMcpLabelCreate"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/create", new
                    {
                        label = _McpSerializer.SerializeJson(labelCreate, false)
                    }),
                    "MCP read-scoped credential cannot create labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.LabelCreate,
                    "MissingScope",
                    "write",
                    "MCP label create denial audit",
                    cancellationToken).ConfigureAwait(false);

                LabelMetadata labelUpdate = new LabelMetadata
                {
                    GUID = labelA.GUID,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    NodeGUID = nodeA.GUID,
                    Label = "BlockedMcpLabelUpdate"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/update", new
                    {
                        label = _McpSerializer.SerializeJson(labelUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.LabelUpdate,
                    "MissingScope",
                    "write",
                    "MCP label update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/delete", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        labelGuid = labelA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.LabelDelete,
                    "MissingScope",
                    "write",
                    "MCP label delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                List<LabelMetadata> blockedCreateManyLabels = new List<LabelMetadata>
                {
                    new LabelMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graphA.GUID,
                        NodeGUID = nodeA.GUID,
                        Label = "BlockedMcpLabelCreateMany"
                    }
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/createmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        labels = _McpSerializer.SerializeJson(blockedCreateManyLabels, false)
                    }),
                    "MCP read-scoped credential cannot bulk-create labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.LabelCreateMany,
                    "MissingScope",
                    "write",
                    "MCP label create-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/deletemany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        labelGuids = new[] { labelA.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot bulk-delete labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.LabelDeleteMany,
                    "MissingScope",
                    "write",
                    "MCP label delete-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/deleteallintenant", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all labels in a tenant").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.LabelDeleteAllInTenant,
                    "MissingScope",
                    "write",
                    "MCP label delete-all-in-tenant denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/deleteallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all labels in a graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.LabelDeleteAllInGraph,
                    "MissingScope",
                    "write",
                    "MCP label delete-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/deletegraphlabels", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete graph labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.LabelDeleteGraphLabels,
                    "MissingScope",
                    "write",
                    "MCP label delete-graph-labels denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/deletenodelabels", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuid = nodeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete node labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.LabelDeleteNodeLabels,
                    "MissingScope",
                    "write",
                    "MCP label delete-node-labels denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/deleteedgelabels", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        edgeGuid = edgeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete edge labels").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.LabelDeleteEdgeLabels,
                    "MissingScope",
                    "write",
                    "MCP label delete-edge-labels denial audit",
                    cancellationToken).ConfigureAwait(false);

                TagMetadata tagCreate = new TagMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    NodeGUID = nodeA.GUID,
                    Key = "blocked",
                    Value = "mcp-tag-create"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/create", new
                    {
                        tag = _McpSerializer.SerializeJson(tagCreate, false)
                    }),
                    "MCP read-scoped credential cannot create tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TagCreate,
                    "MissingScope",
                    "write",
                    "MCP tag create denial audit",
                    cancellationToken).ConfigureAwait(false);

                TagMetadata tagUpdate = new TagMetadata
                {
                    GUID = tagA.GUID,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    NodeGUID = nodeA.GUID,
                    Key = "blocked",
                    Value = "mcp-tag-update"
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/update", new
                    {
                        tag = _McpSerializer.SerializeJson(tagUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TagUpdate,
                    "MissingScope",
                    "write",
                    "MCP tag update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/delete", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        tagGuid = tagA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TagDelete,
                    "MissingScope",
                    "write",
                    "MCP tag delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/createmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        tags = _McpSerializer.SerializeJson(new List<TagMetadata>
                        {
                            new TagMetadata
                            {
                                TenantGUID = tenant.GUID,
                                GraphGUID = graphA.GUID,
                                NodeGUID = nodeA.GUID,
                                Key = "blocked",
                                Value = "mcp-tag-create-many"
                            }
                        }, false)
                    }),
                    "MCP read-scoped credential cannot create many tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TagCreateMany,
                    "MissingScope",
                    "write",
                    "MCP tag create-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/deletemany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        tagGuids = new[] { tagA.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot delete many tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TagDeleteMany,
                    "MissingScope",
                    "write",
                    "MCP tag delete-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/deleteallintenant", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all tags in a tenant").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.TagDeleteAllInTenant,
                    "MissingScope",
                    "write",
                    "MCP tag delete-all-in-tenant denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/deleteallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all tags in a graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.TagDeleteAllInGraph,
                    "MissingScope",
                    "write",
                    "MCP tag delete-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/deletegraphlabels", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete graph tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.TagDeleteGraphTags,
                    "MissingScope",
                    "write",
                    "MCP tag delete-graph-tags denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/deletenodelabels", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuid = nodeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete node tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.TagDeleteNodeTags,
                    "MissingScope",
                    "write",
                    "MCP tag delete-node-tags denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/deleteedgetags", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        edgeGuid = edgeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete edge tags").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.TagDeleteEdgeTags,
                    "MissingScope",
                    "write",
                    "MCP tag delete-edge-tags denial audit",
                    cancellationToken).ConfigureAwait(false);

                VectorMetadata vectorCreate = new VectorMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    NodeGUID = nodeA.GUID,
                    Model = "mcp-rbac",
                    Content = "blocked mcp vector create",
                    Dimensionality = 4,
                    Vectors = BuildDeterministicVector(1, 4)
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/create", new
                    {
                        vector = _McpSerializer.SerializeJson(vectorCreate, false)
                    }),
                    "MCP read-scoped credential cannot create vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.VectorCreate,
                    "MissingScope",
                    "write",
                    "MCP vector create denial audit",
                    cancellationToken).ConfigureAwait(false);

                VectorMetadata vectorUpdate = new VectorMetadata
                {
                    GUID = vectorA.GUID,
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphA.GUID,
                    NodeGUID = nodeA.GUID,
                    Model = "mcp-rbac",
                    Content = "blocked mcp vector update",
                    Dimensionality = 4,
                    Vectors = BuildDeterministicVector(2, 4)
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/update", new
                    {
                        vector = _McpSerializer.SerializeJson(vectorUpdate, false)
                    }),
                    "MCP read-scoped credential cannot update vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.VectorUpdate,
                    "MissingScope",
                    "write",
                    "MCP vector update denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/delete", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        vectorGuid = vectorA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.VectorDelete,
                    "MissingScope",
                    "write",
                    "MCP vector delete denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/createmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        vectors = _McpSerializer.SerializeJson(new List<VectorMetadata>
                        {
                            new VectorMetadata
                            {
                                TenantGUID = tenant.GUID,
                                GraphGUID = graphA.GUID,
                                NodeGUID = nodeA.GUID,
                                Model = "mcp-rbac",
                                Content = "blocked mcp vector create many",
                                Dimensionality = 4,
                                Vectors = BuildDeterministicVector(0, 4)
                            }
                        }, false)
                    }),
                    "MCP read-scoped credential cannot create many vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.VectorCreateMany,
                    "MissingScope",
                    "write",
                    "MCP vector create-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/deletemany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        vectorGuids = new[] { vectorA.GUID.ToString() }
                    }),
                    "MCP read-scoped credential cannot delete many vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.VectorDeleteMany,
                    "MissingScope",
                    "write",
                    "MCP vector delete-many denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/deleteallintenant", new
                    {
                        tenantGuid = tenant.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all vectors in a tenant").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.VectorDeleteAllInTenant,
                    "MissingScope",
                    "write",
                    "MCP vector delete-all-in-tenant denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/deleteallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete all vectors in a graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.VectorDeleteAllInGraph,
                    "MissingScope",
                    "write",
                    "MCP vector delete-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/deletegraphvectors", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete graph vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.VectorDeleteGraphVectors,
                    "MissingScope",
                    "write",
                    "MCP vector delete-graph-vectors denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/deletenodevectors", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        nodeGuid = nodeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete node vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.VectorDeleteNodeVectors,
                    "MissingScope",
                    "write",
                    "MCP vector delete-node-vectors denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/deleteedgevectors", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        edgeGuid = edgeA.GUID.ToString()
                    }),
                    "MCP read-scoped credential cannot delete edge vectors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.VectorDeleteEdgeVectors,
                    "MissingScope",
                    "write",
                    "MCP vector delete-edge-vectors denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("graph/query", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        query = "CREATE (n:Blocked { name: 'Blocked' }) RETURN n"
                    }),
                    "MCP read-scoped credential cannot run mutation queries").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.GraphQuery,
                    "MissingScope",
                    "write",
                    "MCP mutation query denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("graph/transaction", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphA.GUID.ToString(),
                        operations = Array.Empty<object>()
                    }),
                    "MCP read-scoped credential cannot run graph transactions").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphA.GUID,
                    RequestTypeEnum.GraphTransaction,
                    "MissingScope",
                    "write",
                    "MCP graph transaction denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("graph/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph read").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.GraphRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("batch/existence", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodes = new[] { nodeB.GUID.ToString() }
                    }),
                    "MCP graph allow-list denies another graph batch existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.GraphExistence,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list batch existence denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuids = new[] { nodeB.GUID.ToString() }
                    }),
                    "MCP graph allow-list denies another graph node getmany").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/all", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeReadAll,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/readallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node read-all-in-graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeReadAllInGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node read-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/readmostconnected", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph most-connected node list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeReadMostConnected,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node most-connected denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/readleastconnected", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph least-connected node list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeReadLeastConnected,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node least-connected denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graphB.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP graph allow-list denies another graph node enumeration").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeEnumerate,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeExists,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                SearchRequest deniedNodeSearch = new SearchRequest
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphB.GUID,
                    Name = nodeB.Name
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/search", new
                    {
                        searchRequest = _McpSerializer.SerializeJson(deniedNodeSearch, false)
                    }),
                    "MCP graph allow-list denies another graph node search").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeSearch,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node search denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/readfirst", new
                    {
                        searchRequest = _McpSerializer.SerializeJson(deniedNodeSearch, false)
                    }),
                    "MCP graph allow-list denies another graph node readfirst").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeReadFirst,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node readfirst denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/parents", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node parents").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeParents,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node parents denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/children", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node children").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeChildren,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node children denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/neighbors", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node neighbors").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.NodeNeighbors,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node neighbors denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("node/traverse", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        fromNodeGuid = nodeB.GUID.ToString(),
                        toNodeGuid = nodeB.GUID.ToString(),
                        searchType = SearchTypeEnum.DepthFirstSearch.ToString()
                    }),
                    "MCP graph allow-list denies another graph node traversal").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.GetRoutes,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node traversal denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        edgeGuid = edgeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        edgeGuids = new[] { edgeB.GUID.ToString() }
                    }),
                    "MCP graph allow-list denies another graph edge getmany").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/all", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeReadMany,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/readallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge read-all-in-graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeReadAllInGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge read-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graphB.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP graph allow-list denies another graph edge enumeration").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeEnumerate,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        edgeGuid = edgeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeExists,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/nodeedges", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node edge list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.AllEdgesToNode,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node edge list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/fromnode", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph outgoing edge list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgesFromNode,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list outgoing edge denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/tonode", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB2.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph incoming edge list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgesToNode,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list incoming edge denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/betweennodes", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        fromNodeGuid = nodeB.GUID.ToString(),
                        toNodeGuid = nodeB2.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge-between read").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeBetween,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge-between denial audit",
                    cancellationToken).ConfigureAwait(false);

                SearchRequest deniedEdgeSearch = new SearchRequest
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphB.GUID,
                    Name = edgeB.Name
                };
                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/search", new
                    {
                        request = _McpSerializer.SerializeJson(deniedEdgeSearch, false)
                    }),
                    "MCP graph allow-list denies another graph edge search").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeSearch,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge search denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("edge/readfirst", new
                    {
                        request = _McpSerializer.SerializeJson(deniedEdgeSearch, false)
                    }),
                    "MCP graph allow-list denies another graph edge readfirst").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.EdgeReadAll,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge readfirst denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        labelGuid = labelB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph label").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list label denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        labelGuids = new[] { labelB.GUID.ToString() }
                    }),
                    "MCP graph allow-list denies another graph label getmany").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list label getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        labelGuid = labelB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph label existence").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelExists,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list label exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/readallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph label read-all-in-graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelReadAllInGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list label read-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/readmanygraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph graph-label list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelReadManyGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list graph-label list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/readmanynode", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node-label list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelReadManyNode,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node-label list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/readmanyedge", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        edgeGuid = edgeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge-label list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelReadManyEdge,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge-label list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("label/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graphB.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP graph allow-list denies another graph label enumeration").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.LabelEnumerate,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list label enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        tagGuid = tagB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph tag").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list tag denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/get", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        vectorGuid = vectorB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph vector").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list vector denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        tagGuids = new[] { tagB.GUID.ToString() }
                    }),
                    "MCP graph allow-list denies another graph tag getmany").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list tag getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        tagGuid = tagB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph tag exists").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagExists,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list tag exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/readallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph tag read-all-in-graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagReadAllInGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list tag read-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/readmanygraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph graph-tag list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagReadManyGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list graph-tag list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/readmanynode", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node-tag list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagReadManyNode,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node-tag list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/readmanyedge", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        edgeGuid = edgeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge-tag list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagReadManyEdge,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge-tag list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("tag/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graphB.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP graph allow-list denies another graph tag enumeration").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.TagEnumerate,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list tag enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/getmany", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        vectorGuids = new[] { vectorB.GUID.ToString() }
                    }),
                    "MCP graph allow-list denies another graph vector getmany").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorRead,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list vector getmany denial audit",
                    cancellationToken,
                    2).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/exists", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        vectorGuid = vectorB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph vector exists").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorExists,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list vector exists denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/readallingraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph vector read-all-in-graph").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorReadAllInGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list vector read-all-in-graph denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/readmanygraph", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph graph-vector list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorReadManyGraph,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list graph-vector list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/readmanynode", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        nodeGuid = nodeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph node-vector list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorReadManyNode,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list node-vector list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/readmanyedge", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        edgeGuid = edgeB.GUID.ToString()
                    }),
                    "MCP graph allow-list denies another graph edge-vector list").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorReadManyEdge,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list edge-vector list denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/enumerate", new
                    {
                        query = _McpSerializer.SerializeJson(new EnumerationRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graphB.GUID,
                            MaxResults = 10
                        }, false)
                    }),
                    "MCP graph allow-list denies another graph vector enumeration").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorEnumerate,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list vector enumerate denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("vector/search", new
                    {
                        tenantGuid = tenant.GUID.ToString(),
                        graphGuid = graphB.GUID.ToString(),
                        searchRequest = _McpSerializer.SerializeJson(new VectorSearchRequest
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graphB.GUID,
                            Embeddings = BuildDeterministicVector(3, 4),
                            TopK = 5
                        }, false)
                    }),
                    "MCP graph allow-list denies another graph vector search").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    graphB.GUID,
                    RequestTypeEnum.VectorSearch,
                    "GraphDenied",
                    "read",
                    "MCP graph allow-list vector search denial audit",
                    cancellationToken).ConfigureAwait(false);

                await AssertMcpCallDenied(
                    () => _McpClient.CallAsync<string>("admin/flush", new { }),
                    "MCP read-scoped credential cannot use admin tools").ConfigureAwait(false);
                await AssertMcpAuthorizationAudit(
                    credential.GUID,
                    tenant.GUID,
                    null,
                    RequestTypeEnum.FlushDatabase,
                    "MissingScope",
                    "admin",
                    "MCP admin denial audit",
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task AssertMcpAuthorizationAudit(
            Guid credentialGuid,
            Guid? tenantGuid,
            Guid? graphGuid,
            RequestTypeEnum requestType,
            string reason,
            string? requiredScope,
            string message,
            CancellationToken cancellationToken,
            long expectedCount = 1)
        {
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = _McpEnvironment.DatabasePath
            })))
            {
                client.InitializeRepository();
                AuthorizationAuditSearchResult audit = await client.AuthorizationAudit.Search(new AuthorizationAuditSearchRequest
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    CredentialGUID = credentialGuid,
                    RequestType = requestType.ToString(),
                    Reason = reason,
                    RequiredScope = requiredScope
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(expectedCount, audit.TotalCount, message + " count");
            }
        }

        private static async Task AssertMcpCallDenied(Func<Task> call, string message)
        {
            try
            {
                await call().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string text = ex.ToString();
                AssertTrue(
                    text.Contains("RPC Error", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Internal error", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("LiteGraph endpoint returned", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("LiteGraph query endpoint returned", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("LiteGraph transaction endpoint returned", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Authorization", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Denied", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("401", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("403", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("500", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("AuthorizationFailed", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("MissingScope", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("GraphDenied", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("AdminRequired", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Authorization", StringComparison.OrdinalIgnoreCase),
                    message + " denial details: " + TruncateForAssertion(text));
                return;
            }

            throw new InvalidOperationException(message + " should fail.");
        }

        private static string TruncateForAssertion(string value, int maxLength = 500)
        {
            if (String.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
            return value.Substring(0, maxLength) + "...";
        }

        private static async Task TestAuthorizationAuditPersistence(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-authorization-audit.db";
            DeleteFileIfExists(filename);

            Guid tenantA = Guid.NewGuid();
            Guid tenantB = Guid.NewGuid();
            Guid graphA = Guid.NewGuid();
            Guid graphB = Guid.NewGuid();
            Guid userA = Guid.NewGuid();
            Guid credentialA = Guid.NewGuid();
            Guid credentialB = Guid.NewGuid();
            DateTime baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();

                AuthorizationAuditEntry missingScope = new AuthorizationAuditEntry
                {
                    GUID = Guid.NewGuid(),
                    CreatedUtc = baseTime.AddMinutes(1),
                    RequestId = "audit-request-1",
                    CorrelationId = "audit-correlation-1",
                    TraceId = "audit-trace-1",
                    TenantGUID = tenantA,
                    GraphGUID = graphA,
                    UserGUID = userA,
                    CredentialGUID = credentialA,
                    RequestType = RequestTypeEnum.GraphQuery.ToString(),
                    Method = "POST",
                    Path = "/v1.0/tenants/" + tenantA + "/graphs/" + graphA + "/query",
                    SourceIp = "127.0.0.1",
                    AuthenticationResult = AuthenticationResultEnum.Success.ToString(),
                    AuthorizationResult = AuthorizationResultEnum.Denied.ToString(),
                    Reason = "MissingScope",
                    RequiredScope = "write",
                    IsAdmin = false,
                    StatusCode = 401,
                    Description = "Missing write scope."
                };

                AuthorizationAuditEntry graphDenied = new AuthorizationAuditEntry
                {
                    GUID = Guid.NewGuid(),
                    CreatedUtc = baseTime.AddMinutes(2),
                    RequestId = "audit-request-2",
                    CorrelationId = "audit-correlation-2",
                    TraceId = "audit-trace-2",
                    TenantGUID = tenantA,
                    GraphGUID = graphB,
                    UserGUID = userA,
                    CredentialGUID = credentialA,
                    RequestType = RequestTypeEnum.NodeRead.ToString(),
                    Method = "GET",
                    Path = "/v1.0/tenants/" + tenantA + "/graphs/" + graphB + "/nodes",
                    SourceIp = "127.0.0.2",
                    AuthenticationResult = AuthenticationResultEnum.Success.ToString(),
                    AuthorizationResult = AuthorizationResultEnum.Denied.ToString(),
                    Reason = "GraphDenied",
                    RequiredScope = "read",
                    IsAdmin = false,
                    StatusCode = 401,
                    Description = "Graph denied."
                };

                AuthorizationAuditEntry tenantDenied = new AuthorizationAuditEntry
                {
                    GUID = Guid.NewGuid(),
                    CreatedUtc = baseTime.AddMinutes(3),
                    RequestId = "audit-request-3",
                    CorrelationId = "audit-correlation-3",
                    TraceId = "audit-trace-3",
                    TenantGUID = tenantB,
                    GraphGUID = graphA,
                    UserGUID = Guid.NewGuid(),
                    CredentialGUID = credentialB,
                    RequestType = RequestTypeEnum.GraphRead.ToString(),
                    Method = "GET",
                    Path = "/v1.0/tenants/" + tenantB + "/graphs/" + graphA,
                    SourceIp = "127.0.0.3",
                    AuthenticationResult = AuthenticationResultEnum.Success.ToString(),
                    AuthorizationResult = AuthorizationResultEnum.Denied.ToString(),
                    Reason = "TenantDenied",
                    RequiredScope = "read",
                    IsAdmin = false,
                    StatusCode = 401,
                    Description = "Tenant denied."
                };

                await client.AuthorizationAudit.Insert(missingScope, cancellationToken).ConfigureAwait(false);
                await client.AuthorizationAudit.Insert(graphDenied, cancellationToken).ConfigureAwait(false);
                await client.AuthorizationAudit.Insert(tenantDenied, cancellationToken).ConfigureAwait(false);

                AuthorizationAuditEntry read = await client.AuthorizationAudit.ReadByGuid(missingScope.GUID, cancellationToken).ConfigureAwait(false);
                AssertNotNull(read, "Authorization audit read by GUID");
                AssertEqual(missingScope.GUID, read.GUID, "Audit GUID round trip");
                AssertEqual(missingScope.RequestId, read.RequestId, "Audit request ID round trip");
                AssertEqual(missingScope.CorrelationId, read.CorrelationId, "Audit correlation ID round trip");
                AssertEqual(missingScope.TraceId, read.TraceId, "Audit trace ID round trip");
                AssertEqual(missingScope.TenantGUID, read.TenantGUID, "Audit tenant GUID round trip");
                AssertEqual(missingScope.GraphGUID, read.GraphGUID, "Audit graph GUID round trip");
                AssertEqual(missingScope.UserGUID, read.UserGUID, "Audit user GUID round trip");
                AssertEqual(missingScope.CredentialGUID, read.CredentialGUID, "Audit credential GUID round trip");
                AssertEqual(missingScope.RequestType, read.RequestType, "Audit request type round trip");
                AssertEqual(missingScope.Method, read.Method, "Audit method round trip");
                AssertEqual(missingScope.Path, read.Path, "Audit path round trip");
                AssertEqual(missingScope.SourceIp, read.SourceIp, "Audit source IP round trip");
                AssertEqual(missingScope.AuthenticationResult, read.AuthenticationResult, "Audit authentication result round trip");
                AssertEqual(missingScope.AuthorizationResult, read.AuthorizationResult, "Audit authorization result round trip");
                AssertEqual(missingScope.Reason, read.Reason, "Audit reason round trip");
                AssertEqual(missingScope.RequiredScope, read.RequiredScope, "Audit required scope round trip");
                AssertEqual(missingScope.IsAdmin, read.IsAdmin, "Audit admin flag round trip");
                AssertEqual(missingScope.StatusCode, read.StatusCode, "Audit status code round trip");
                AssertEqual(missingScope.Description, read.Description, "Audit description round trip");

                AuthorizationAuditSearchResult all = await client.AuthorizationAudit.Search(new AuthorizationAuditSearchRequest
                {
                    PageSize = 2
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(3L, all.TotalCount, "Audit total count");
                AssertEqual(2, all.TotalPages, "Audit total pages");
                AssertEqual(2, all.Objects.Count, "Audit first page size");
                AssertEqual(tenantDenied.GUID, all.Objects[0].GUID, "Audit ordering newest first");

                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { TenantGUID = tenantA }, 2L, "Audit tenant filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { GraphGUID = graphA }, 2L, "Audit graph filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { UserGUID = userA }, 2L, "Audit user filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { CredentialGUID = credentialA }, 2L, "Audit credential filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { RequestId = "audit-request-1" }, 1L, "Audit request ID filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { CorrelationId = "audit-correlation-2" }, 1L, "Audit correlation ID filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { TraceId = "audit-trace-3" }, 1L, "Audit trace ID filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { RequestType = RequestTypeEnum.GraphQuery.ToString() }, 1L, "Audit request type filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { Reason = "MissingScope" }, 1L, "Audit reason filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { RequiredScope = "read" }, 2L, "Audit required scope filter", cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { FromUtc = baseTime.AddMinutes(1), ToUtc = baseTime.AddMinutes(3) }, 2L, "Audit time-window filter", cancellationToken).ConfigureAwait(false);

                AuthorizationAuditSearchResult secondPage = await client.AuthorizationAudit.Search(new AuthorizationAuditSearchRequest
                {
                    Page = 1,
                    PageSize = 2
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(1, secondPage.Objects.Count, "Audit second page size");
                AssertEqual(missingScope.GUID, secondPage.Objects[0].GUID, "Audit second page object");

                await client.AuthorizationAudit.DeleteByGuid(graphDenied.GUID, cancellationToken).ConfigureAwait(false);
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest { TenantGUID = tenantA }, 1L, "Audit delete by GUID", cancellationToken).ConfigureAwait(false);

                int tenantDeniedDeleted = await client.AuthorizationAudit.DeleteMany(new AuthorizationAuditSearchRequest
                {
                    Reason = "TenantDenied"
                }, cancellationToken).ConfigureAwait(false);
                AssertEqual(1, tenantDeniedDeleted, "Audit delete many count");
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest(), 1L, "Audit count after delete many", cancellationToken).ConfigureAwait(false);

                int olderDeleted = await client.AuthorizationAudit.DeleteOlderThan(baseTime.AddMinutes(5), cancellationToken).ConfigureAwait(false);
                AssertEqual(1, olderDeleted, "Audit delete older than count");
                await AssertAuthorizationAuditCount(client, new AuthorizationAuditSearchRequest(), 0L, "Audit count after delete older than", cancellationToken).ConfigureAwait(false);
            }

            DeleteFileIfExists(filename);
        }

        private static async Task AssertAuthorizationAuditCount(
            LiteGraphClient client,
            AuthorizationAuditSearchRequest search,
            long expected,
            string message,
            CancellationToken cancellationToken)
        {
            AuthorizationAuditSearchResult result = await client.AuthorizationAudit.Search(search, cancellationToken).ConfigureAwait(false);
            AssertEqual(expected, result.TotalCount, message);
        }

        private static async Task TestAuthorizationAuditRestDeniedQuery(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

                string bearerToken = "audit-denied-" + Guid.NewGuid().ToString("N");
                Guid tenantGuid;
                Guid graphGuid;
                Guid credentialGuid;

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = _McpEnvironment.DatabasePath
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Audit Tenant" }, cancellationToken).ConfigureAwait(false);
                    UserMaster user = await client.User.Create(new UserMaster
                    {
                        TenantGUID = tenant.GUID,
                        FirstName = "Audit",
                        LastName = "User",
                        Email = "audit-" + Guid.NewGuid().ToString("N") + "@example.com",
                        Password = "password"
                    }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Audit Graph" }, cancellationToken).ConfigureAwait(false);
                    Credential credential = await client.Credential.Create(new Credential
                    {
                        TenantGUID = tenant.GUID,
                        UserGUID = user.GUID,
                        Name = "Audit Read Credential",
                        BearerToken = bearerToken,
                        Scopes = new List<string> { "read" },
                        GraphGUIDs = new List<Guid> { graph.GUID }
                    }, cancellationToken).ConfigureAwait(false);

                    tenantGuid = tenant.GUID;
                    graphGuid = graph.GUID;
                    credentialGuid = credential.GUID;
                }

                string requestId = "audit-denied-request-" + Guid.NewGuid().ToString("N");
                string endpoint = _McpEnvironment.LiteGraphEndpoint + "/v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/query";
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", "Bearer " + bearerToken);
                request.Headers.Add("x-request-id", requestId);
                request.Content = new StringContent(
                    "{\"Query\":\"CREATE (n:DeniedAudit { name: 'Blocked' }) RETURN n\"}",
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage response = await _ReadinessClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false);

                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                AssertEqual(401, (int)response.StatusCode, "Denied graph query status");
                AssertTrue(body.Contains("AuthorizationFailed"), "Denied graph query authorization error body");
                AssertTrue(body.Contains("MissingScope"), "Denied graph query response reason");
                AssertTrue(body.Contains("requiredScope"), "Denied graph query response required scope");

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = _McpEnvironment.DatabasePath
                })))
                {
                    client.InitializeRepository();

                    AuthorizationAuditSearchResult audit = await client.AuthorizationAudit.Search(new AuthorizationAuditSearchRequest
                    {
                        RequestId = requestId
                    }, cancellationToken).ConfigureAwait(false);

                    AssertEqual(1L, audit.TotalCount, "Denied graph query audit count");
                    AuthorizationAuditEntry entry = audit.Objects[0];
                    AssertEqual(tenantGuid, entry.TenantGUID, "Denied graph query audit tenant");
                    AssertEqual(graphGuid, entry.GraphGUID, "Denied graph query audit graph");
                    AssertEqual(credentialGuid, entry.CredentialGUID, "Denied graph query audit credential");
                    AssertEqual(RequestTypeEnum.GraphQuery.ToString(), entry.RequestType, "Denied graph query audit request type");
                    AssertEqual("POST", entry.Method, "Denied graph query audit method");
                    AssertEqual(AuthenticationResultEnum.Success.ToString(), entry.AuthenticationResult, "Denied graph query audit authentication result");
                    AssertEqual(AuthorizationResultEnum.Denied.ToString(), entry.AuthorizationResult, "Denied graph query audit authorization result");
                    AssertEqual("MissingScope", entry.Reason, "Denied graph query audit reason");
                    AssertEqual("write", entry.RequiredScope, "Denied graph query audit required scope");
                    AssertEqual(401, entry.StatusCode, "Denied graph query audit status code");
                }
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestNativeQueryCreateAndMatch(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Graph" }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult create = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE (n:Person { name: $name, data: $data }) RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "name", "Ada Lovelace" },
                            { "data", new Dictionary<string, object> { { "role", "mathematician" } } }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(create.Mutated, "Create query mutates");
                AssertEqual(1, create.RowCount, "Create query row count");
                AssertTrue(create.ExecutionProfile == null, "Query profile is omitted by default");

                GraphQueryResult match = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 10",
                        IncludeProfile = true,
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "name", "Ada Lovelace" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, match.RowCount, "Match query row count");
                AssertEqual("Ada Lovelace", match.Nodes[0].Name, "Match query node name");
                AssertTrue(match.ExecutionTimeMs >= 0, "Match query execution time");
                AssertNotNull(match.Plan, "Match query plan summary");
                AssertEqual(GraphQueryPlanSeedKindEnum.NodeName, match.Plan.SeedKind, "Match query plan seed");
                AssertNotNull(match.ExecutionProfile, "Match query execution profile");
                AssertTrue(match.ExecutionProfile.ParseTimeMs >= 0, "Match query parse profile time");
                AssertTrue(match.ExecutionProfile.PlanTimeMs >= 0, "Match query plan profile time");
                AssertTrue(match.ExecutionProfile.ExecuteTimeMs >= 0, "Match query execute profile time");
                AssertTrue(match.ExecutionProfile.TotalTimeMs >= match.ExecutionProfile.ExecuteTimeMs, "Match query profile total time");

                Node secondNode = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Charles Babbage"
                }, cancellationToken).ConfigureAwait(false);

                Edge edge = await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Collaborated",
                    From = create.Nodes[0].GUID,
                    To = secondNode.GUID,
                    Labels = new List<string> { "KNOWS" }
                }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult edgeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a)-[e:KNOWS]->(b) WHERE a.guid = $from RETURN a, e, b LIMIT 5",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "from", create.Nodes[0].GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeMatch.RowCount, "Edge match query row count");
                AssertEqual(edge.GUID, edgeMatch.Edges[0].GUID, "Edge match query edge GUID");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestNativeQueryMultiHopMatch(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-multihop.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Multi-Hop Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Multi-Hop Graph" }, cancellationToken).ConfigureAwait(false);

                Node start = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Start",
                    Labels = new List<string> { "Person" }
                }, cancellationToken).ConfigureAwait(false);
                Node middle = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Middle",
                    Labels = new List<string> { "Person" }
                }, cancellationToken).ConfigureAwait(false);
                Node end = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "End",
                    Labels = new List<string> { "Person" }
                }, cancellationToken).ConfigureAwait(false);
                Node deadEnd = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Dead End",
                    Labels = new List<string> { "Person" }
                }, cancellationToken).ConfigureAwait(false);

                Edge firstHop = await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "First Hop",
                    From = start.GUID,
                    To = middle.GUID,
                    Labels = new List<string> { "LINKS" }
                }, cancellationToken).ConfigureAwait(false);
                Edge secondHop = await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Second Hop",
                    From = middle.GUID,
                    To = end.GUID,
                    Labels = new List<string> { "LINKS" }
                }, cancellationToken).ConfigureAwait(false);
                Edge directHop = await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Direct Hop",
                    From = start.GUID,
                    To = end.GUID,
                    Labels = new List<string> { "LINKS" }
                }, cancellationToken).ConfigureAwait(false);
                await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Short Branch",
                    From = start.GUID,
                    To = deadEnd.GUID,
                    Labels = new List<string> { "LINKS" }
                }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult path = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e1:LINKS]->(b:Person)-[e2:LINKS]->(c:Person) WHERE a.guid = $start RETURN a, e1, b, e2, c LIMIT 10",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "start", start.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, path.RowCount, "Multi-hop path row count");
                AssertEqual(3, path.Nodes.Count, "Multi-hop path returned nodes");
                AssertEqual(2, path.Edges.Count, "Multi-hop path returned edges");
                AssertEqual(start.GUID, ((Node)path.Rows[0]["a"]).GUID, "Multi-hop start node");
                AssertEqual(firstHop.GUID, ((Edge)path.Rows[0]["e1"]).GUID, "Multi-hop first edge");
                AssertEqual(middle.GUID, ((Node)path.Rows[0]["b"]).GUID, "Multi-hop middle node");
                AssertEqual(secondHop.GUID, ((Edge)path.Rows[0]["e2"]).GUID, "Multi-hop second edge");
                AssertEqual(end.GUID, ((Node)path.Rows[0]["c"]).GUID, "Multi-hop end node");

                GraphQueryResult variablePath = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[path:LINKS*1..2]->(c:Person) WHERE a.guid = $start AND c.guid = $end RETURN a, path, c LIMIT 10",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "start", start.GUID },
                            { "end", end.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(2, variablePath.RowCount, "Variable-length path row count");
                List<int> variablePathLengths = variablePath.Rows
                    .Select(row => ((IEnumerable<Edge>)row["path"]).Count())
                    .OrderBy(length => length)
                    .ToList();
                AssertEqual(1, variablePathLengths[0], "Variable-length path includes direct path");
                AssertEqual(2, variablePathLengths[1], "Variable-length path includes two-hop path");
                AssertEqual(3, variablePath.Edges.Count, "Variable-length path typed edges include all path edges");

                GraphQueryResult shortestPath = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH SHORTEST (a:Person)-[path:LINKS*1..2]->(c:Person) WHERE a.guid = $start AND c.guid = $end RETURN a, path, c LIMIT 10",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "start", start.GUID },
                            { "end", end.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, shortestPath.RowCount, "Shortest path row count");
                List<Edge> shortestEdges = ((IEnumerable<Edge>)shortestPath.Rows[0]["path"]).ToList();
                AssertEqual(1, shortestEdges.Count, "Shortest path edge count");
                AssertEqual(directHop.GUID, shortestEdges[0].GUID, "Shortest path selects the direct edge");

                GraphQueryResult optionalMissing = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "OPTIONAL MATCH (a:Person)-[e:MISSING]->(b:Person) WHERE a.guid = $start RETURN a, e, b LIMIT 1",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "start", start.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, optionalMissing.RowCount, "OPTIONAL MATCH emits one null row when no edge matches");
                AssertTrue(optionalMissing.Rows[0].ContainsKey("a"), "OPTIONAL MATCH null row contains source variable");
                AssertTrue(optionalMissing.Rows[0]["a"] == null, "OPTIONAL MATCH source variable is null");
                AssertTrue(optionalMissing.Rows[0]["e"] == null, "OPTIONAL MATCH edge variable is null");
                AssertTrue(optionalMissing.Rows[0]["b"] == null, "OPTIONAL MATCH target variable is null");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestNativeQueryDataFilters(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-data-filters.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Data Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Data Graph" }, cancellationToken).ConfigureAwait(false);

                Node ada = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Ada",
                    Labels = new List<string> { "Person" },
                    Data = new Dictionary<string, object>
                    {
                        { "role", "mathematician" },
                        { "profile", new Dictionary<string, object> { { "age", 36 } } }
                    }
                }, cancellationToken).ConfigureAwait(false);

                Node charles = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Charles",
                    Labels = new List<string> { "Person" },
                    Data = new Dictionary<string, object>
                    {
                        { "role", "engineer" }
                    }
                }, cancellationToken).ConfigureAwait(false);

                Edge edge = await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Worked With",
                    From = ada.GUID,
                    To = charles.GUID,
                    Cost = 7,
                    Labels = new List<string> { "KNOWS" },
                    Data = new Dictionary<string, object>
                    {
                        { "kind", "collaboration" }
                    }
                }, cancellationToken).ConfigureAwait(false);

                await client.Tag.Create(new TagMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    NodeGUID = ada.GUID,
                    Key = "field",
                    Value = "math"
                }, cancellationToken).ConfigureAwait(false);

                await client.Tag.Create(new TagMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    NodeGUID = charles.GUID,
                    Key = "field",
                    Value = "mechanical"
                }, cancellationToken).ConfigureAwait(false);

                await client.Tag.Create(new TagMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    EdgeGUID = edge.GUID,
                    Key = "kind",
                    Value = "historical"
                }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult roleMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.role = $role RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "role", "mathematician" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, roleMatch.RowCount, "Node data role row count");
                AssertEqual(ada.GUID, roleMatch.Nodes[0].GUID, "Node data role match");

                GraphQueryResult nestedMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.profile.age = 36 RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, nestedMatch.RowCount, "Nested node data row count");
                AssertEqual(ada.GUID, nestedMatch.Nodes[0].GUID, "Nested node data match");

                GraphQueryResult greaterThanMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.profile.age > $age RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "age", 30 }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, greaterThanMatch.RowCount, "Node data greater-than row count");
                AssertEqual(ada.GUID, greaterThanMatch.Nodes[0].GUID, "Node data greater-than match");

                GraphQueryResult andNodeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.role = $role AND n.data.profile.age >= 36 RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "role", "mathematician" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, andNodeMatch.RowCount, "Node data AND row count");
                AssertEqual(ada.GUID, andNodeMatch.Nodes[0].GUID, "Node data AND match");

                GraphQueryResult orNodeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.role = 'mathematician' OR n.name = 'Charles' RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(2, orNodeMatch.RowCount, "Node OR row count");
                AssertTrue(orNodeMatch.Nodes.Any(node => node.GUID == ada.GUID), "Node OR includes first branch");
                AssertTrue(orNodeMatch.Nodes.Any(node => node.GUID == charles.GUID), "Node OR includes second branch");

                GraphQueryResult notNodeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE NOT n.data.role = 'engineer' RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, notNodeMatch.RowCount, "Node NOT row count");
                AssertEqual(ada.GUID, notNodeMatch.Nodes[0].GUID, "Node NOT match");

                GraphQueryResult literalListMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.name IN ['Ada', 'Missing'] RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, literalListMatch.RowCount, "Node literal IN row count");
                AssertEqual(ada.GUID, literalListMatch.Nodes[0].GUID, "Node literal IN match");

                GraphQueryResult parameterListMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.role IN $roles RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "roles", new List<string> { "mathematician", "engineer" } }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(2, parameterListMatch.RowCount, "Node parameter IN row count");
                AssertTrue(parameterListMatch.Nodes.Any(node => node.GUID == ada.GUID), "Node parameter IN includes Ada");
                AssertTrue(parameterListMatch.Nodes.Any(node => node.GUID == charles.GUID), "Node parameter IN includes Charles");

                GraphQueryResult nodeTagMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.tags.field IN ['math', 'logic'] RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, nodeTagMatch.RowCount, "Node tag IN row count");
                AssertEqual(ada.GUID, nodeTagMatch.Nodes[0].GUID, "Node tag IN match");

                GraphQueryResult nodeAggregate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) RETURN COUNT(*) AS total, COUNT(n.data.profile.age) AS aged, SUM(n.data.profile.age) AS ageSum, AVG(n.data.profile.age) AS averageAge, MIN(n.name) AS firstName, MAX(n.name) AS lastName"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, nodeAggregate.RowCount, "Node aggregate row count");
                Dictionary<string, object> nodeAggregateRow = nodeAggregate.Rows[0];
                AssertEqual(2, Convert.ToInt32(nodeAggregateRow["total"]), "Node aggregate COUNT(*)");
                AssertEqual(1, Convert.ToInt32(nodeAggregateRow["aged"]), "Node aggregate COUNT(field)");
                AssertEqual(36m, Convert.ToDecimal(nodeAggregateRow["ageSum"]), "Node aggregate SUM");
                AssertEqual(36m, Convert.ToDecimal(nodeAggregateRow["averageAge"]), "Node aggregate AVG");
                AssertEqual("Ada", nodeAggregateRow["firstName"]?.ToString(), "Node aggregate MIN");
                AssertEqual("Charles", nodeAggregateRow["lastName"]?.ToString(), "Node aggregate MAX");

                GraphQueryResult tagAggregate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.tags.field IN ['math', 'logic'] RETURN COUNT(n) AS tagged, MAX(n.tags.field) AS field"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, tagAggregate.RowCount, "Tag aggregate row count");
                AssertEqual(1, Convert.ToInt32(tagAggregate.Rows[0]["tagged"]), "Tag aggregate COUNT");
                AssertEqual("math", tagAggregate.Rows[0]["field"]?.ToString(), "Tag aggregate MAX");

                GraphQueryResult parenthesizedBooleanMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE (n.name = 'Ada' OR n.name = 'Charles') AND NOT n.data.role = 'engineer' RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, parenthesizedBooleanMatch.RowCount, "Parenthesized boolean row count");
                AssertEqual(ada.GUID, parenthesizedBooleanMatch.Nodes[0].GUID, "Parenthesized boolean match");

                GraphQueryResult nameContainsMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.name CONTAINS 'da' RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, nameContainsMatch.RowCount, "Node name CONTAINS row count");
                AssertEqual(ada.GUID, nameContainsMatch.Nodes[0].GUID, "Node name CONTAINS match");

                GraphQueryResult dataStartsWithMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.data.role STARTS WITH 'math' RETURN n"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, dataStartsWithMatch.RowCount, "Node data STARTS WITH row count");
                AssertEqual(ada.GUID, dataStartsWithMatch.Nodes[0].GUID, "Node data STARTS WITH match");

                GraphQueryResult orderedNodeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) RETURN n ORDER BY n.name DESC LIMIT 1"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, orderedNodeMatch.RowCount, "ORDER BY row count");
                AssertEqual(charles.GUID, orderedNodeMatch.Nodes[0].GUID, "ORDER BY name DESC match");

                GraphQueryResult edgeDataMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a)-[e:KNOWS]->(b) WHERE e.data.kind = 'collaboration' RETURN e"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeDataMatch.RowCount, "Edge data row count");
                AssertEqual(edge.GUID, edgeDataMatch.Edges[0].GUID, "Edge data match");

                GraphQueryResult edgeNameEndsWithMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a)-[e:KNOWS]->(b) WHERE e.name ENDS WITH 'With' RETURN e"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeNameEndsWithMatch.RowCount, "Edge name ENDS WITH row count");
                AssertEqual(edge.GUID, edgeNameEndsWithMatch.Edges[0].GUID, "Edge name ENDS WITH match");

                GraphQueryResult andEdgeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE e.data.kind = 'collaboration' AND a.data.role = 'mathematician' RETURN a, e, b"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, andEdgeMatch.RowCount, "Edge and node data AND row count");
                AssertEqual(edge.GUID, andEdgeMatch.Edges[0].GUID, "Edge and node data AND edge");

                GraphQueryResult edgeTagMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE e.tags.kind = 'historical' RETURN e"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeTagMatch.RowCount, "Edge tag row count");
                AssertEqual(edge.GUID, edgeTagMatch.Edges[0].GUID, "Edge tag match");

                GraphQueryResult edgeAggregate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE e.tags.kind = 'historical' RETURN COUNT(e) AS totalEdges, SUM(e.cost) AS totalCost, MIN(a.name) AS firstSource, MAX(b.name) AS lastTarget"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeAggregate.RowCount, "Edge aggregate row count");
                AssertEqual(1, Convert.ToInt32(edgeAggregate.Rows[0]["totalEdges"]), "Edge aggregate COUNT");
                AssertEqual(7m, Convert.ToDecimal(edgeAggregate.Rows[0]["totalCost"]), "Edge aggregate SUM");
                AssertEqual("Ada", edgeAggregate.Rows[0]["firstSource"]?.ToString(), "Edge aggregate source MIN");
                AssertEqual("Charles", edgeAggregate.Rows[0]["lastTarget"]?.ToString(), "Edge aggregate target MAX");

                GraphQueryResult sourceTagMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.tags.field = 'math' RETURN a, e, b"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, sourceTagMatch.RowCount, "Edge source tag row count");
                AssertEqual(edge.GUID, sourceTagMatch.Edges[0].GUID, "Edge source tag match");

                GraphQueryResult orEdgeMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.name = 'Nobody' OR b.name = 'Charles' RETURN a, e, b"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, orEdgeMatch.RowCount, "Edge OR row count");
                AssertEqual(edge.GUID, orEdgeMatch.Edges[0].GUID, "Edge OR match");

                GraphQueryResult pathDataMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.data.profile.age <= 36 RETURN a, e, b"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, pathDataMatch.RowCount, "Path node data comparison row count");
                AssertEqual(edge.GUID, pathDataMatch.Edges[0].GUID, "Path node data comparison edge");

                GraphQueryResult pathBooleanMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.name IN ['Missing', 'Ada'] OR NOT b.data.role = 'engineer' RETURN a, e, b"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, pathBooleanMatch.RowCount, "Path boolean/list row count");
                AssertEqual(edge.GUID, pathBooleanMatch.Edges[0].GUID, "Path boolean/list edge");

                GraphQueryResult pathTagMatch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.tags.field = 'math' AND e.tags.kind = 'historical' RETURN a, e, b"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, pathTagMatch.RowCount, "Path tag row count");
                AssertEqual(edge.GUID, pathTagMatch.Edges[0].GUID, "Path tag edge");

                GraphQueryResult pathAggregate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.tags.field = 'math' RETURN COUNT(*) AS paths, MAX(e.tags.kind) AS pathKind"
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, pathAggregate.RowCount, "Path aggregate row count");
                AssertEqual(1, Convert.ToInt32(pathAggregate.Rows[0]["paths"]), "Path aggregate COUNT");
                AssertEqual("historical", pathAggregate.Rows[0]["pathKind"]?.ToString(), "Path aggregate tag MAX");
            }

            DeleteFileIfExists(filename);
        }

        private static Task TestNativeQueryLexer(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<GraphQueryToken> tokens = Lexer.Tokenize("MATCH (a)-[e:KNOWS]->(b) WHERE e.cost >= -1.5 RETURN e;");
            AssertTrue(tokens.Count > 0, "Lexer emits tokens");
            AssertTrue(tokens.Exists(t => t.Type == GraphQueryTokenTypeEnum.Arrow && t.Text == "->"), "Lexer emits arrow token");
            AssertTrue(tokens.Exists(t => t.Type == GraphQueryTokenTypeEnum.GreaterThanOrEquals && t.Text == ">="), "Lexer emits greater-than-or-equals token");
            AssertTrue(tokens.Exists(t => t.Type == GraphQueryTokenTypeEnum.Number && t.Text == "-1.5"), "Lexer emits negative decimal token");
            AssertTrue(tokens.Exists(t => t.Type == GraphQueryTokenTypeEnum.Semicolon), "Lexer emits semicolon token");
            AssertEqual(GraphQueryTokenTypeEnum.End, tokens[tokens.Count - 1].Type, "Lexer emits end token");

            List<GraphQueryToken> parameterTokens = Lexer.Tokenize("CALL litegraph.vector.searchNodes($embedding)");
            AssertTrue(parameterTokens.Exists(t => t.Type == GraphQueryTokenTypeEnum.Parameter && t.Text == "$embedding"), "Lexer emits parameter token");

            List<GraphQueryToken> variablePathTokens = Lexer.Tokenize("MATCH (a)-[path:LINKS*1..3]->(b) RETURN path");
            AssertTrue(variablePathTokens.Exists(t => t.Type == GraphQueryTokenTypeEnum.Star), "Lexer emits variable-length star token");
            AssertTrue(variablePathTokens.Count(t => t.Type == GraphQueryTokenTypeEnum.Dot) >= 2, "Lexer emits variable-length range dot tokens");

            List<GraphQueryToken> multiline = Lexer.Tokenize("MATCH (n)\nWHERE n.name = 'Ada'\nRETURN n");
            GraphQueryToken? where = multiline.Find(t => string.Equals(t.Text, "WHERE", StringComparison.OrdinalIgnoreCase));
            AssertNotNull(where, "Lexer finds WHERE token");
            AssertEqual(2, where!.Line, "Lexer tracks line numbers");
            AssertEqual(1, where.Column, "Lexer tracks column numbers");

            try
            {
                Lexer.Tokenize("MATCH (n) WHERE n.name = $ RETURN n");
                throw new InvalidOperationException("Lexer should reject missing parameter names.");
            }
            catch (GraphQueryParseException e)
            {
                AssertTrue(e.Message.Contains("Parameter name expected"), "Lexer reports missing parameter name");
                AssertTrue(e.Line >= 1, "Missing parameter line");
                AssertTrue(e.Column >= 1, "Missing parameter column");
            }

            try
            {
                Lexer.Tokenize("MATCH (n) WHERE n.name = 'Ada RETURN n");
                throw new InvalidOperationException("Lexer should reject unterminated strings.");
            }
            catch (GraphQueryParseException e)
            {
                AssertTrue(e.Message.Contains("Unterminated string literal"), "Lexer reports unterminated string");
            }

            try
            {
                Lexer.Tokenize("MATCH (n) @ RETURN n");
                throw new InvalidOperationException("Lexer should reject unexpected characters.");
            }
            catch (GraphQueryParseException e)
            {
                AssertTrue(e.Message.Contains("Unexpected character"), "Lexer reports unexpected character");
            }

            return Task.CompletedTask;
        }

        private static Task TestNativeQueryParser(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<GraphQueryToken> tokens = Lexer.Tokenize("MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 5");
            AssertTrue(tokens.Count > 0, "Lexer emits tokens");

            GraphQueryAst ast = Parser.Parse("MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 5");
            AssertEqual(GraphQueryKindEnum.MatchNode, ast.Kind, "Parsed query kind");
            AssertEqual("n", ast.NodeVariable, "Parsed node variable");
            AssertEqual("Person", ast.NodeLabel, "Parsed node label");
            AssertEqual("name", ast.WhereField, "Parsed WHERE field");
            AssertEqual(5, ast.Limit.GetValueOrDefault(), "Parsed LIMIT");

            GraphQueryAst dataFilter = Parser.Parse("MATCH (n:Person) WHERE n.data.profile.age = 36 RETURN n");
            AssertEqual("data.profile.age", dataFilter.WhereField, "Parsed nested data WHERE field");

            GraphQueryAst comparisonFilter = Parser.Parse("MATCH (n:Person) WHERE n.data.profile.age >= 30 RETURN n");
            AssertEqual(">=", comparisonFilter.WhereOperator, "Parsed comparison WHERE operator");

            GraphQueryAst andFilter = Parser.Parse("MATCH (n:Person) WHERE n.data.role = $role AND n.data.profile.age >= 30 RETURN n");
            AssertEqual(2, andFilter.WherePredicates.Count, "Parsed AND predicate count");
            AssertEqual("data.profile.age", andFilter.WherePredicates[1].Field, "Parsed second AND predicate field");
            AssertEqual(GraphQueryPredicateExpressionKindEnum.And, andFilter.WhereExpression.Kind, "Parsed AND predicate expression");

            GraphQueryAst orFilter = Parser.Parse("MATCH (n:Person) WHERE n.name = 'Ada' OR NOT n.data.role IN ['engineer', 'writer'] RETURN n");
            AssertEqual(2, orFilter.WherePredicates.Count, "Parsed OR predicate leaves");
            AssertEqual(GraphQueryPredicateExpressionKindEnum.Or, orFilter.WhereExpression.Kind, "Parsed OR predicate expression");
            AssertEqual(GraphQueryPredicateExpressionKindEnum.Not, orFilter.WhereExpression.Right.Kind, "Parsed NOT predicate expression");
            AssertEqual("IN", orFilter.WherePredicates[1].Operator, "Parsed IN predicate operator");

            GraphQueryAst tagFilter = Parser.Parse("MATCH (n:Person) WHERE n.tags.field = 'math' RETURN n");
            AssertEqual("tags.field", tagFilter.WhereField, "Parsed tag predicate field");

            GraphQueryAst aggregate = Parser.Parse("MATCH (n:Person) RETURN COUNT(*) AS total, AVG(n.data.profile.age) AS averageAge");
            AssertEqual(2, aggregate.ReturnItems.Count, "Parsed aggregate return count");
            AssertEqual(GraphQueryReturnItemKindEnum.Aggregate, aggregate.ReturnItems[0].Kind, "Parsed aggregate return item kind");
            AssertEqual(GraphQueryAggregateFunctionEnum.Count, aggregate.ReturnItems[0].AggregateFunction.GetValueOrDefault(), "Parsed COUNT aggregate");
            AssertTrue(aggregate.ReturnItems[0].AggregateWildcard, "Parsed COUNT wildcard");
            AssertEqual(GraphQueryAggregateFunctionEnum.Avg, aggregate.ReturnItems[1].AggregateFunction.GetValueOrDefault(), "Parsed AVG aggregate");
            AssertEqual("data.profile.age", aggregate.ReturnItems[1].Field, "Parsed aggregate field");
            AssertEqual("averageAge", aggregate.ReturnItems[1].Alias, "Parsed aggregate alias");

            GraphQueryAst stringFilter = Parser.Parse("MATCH (n:Person) WHERE n.name STARTS WITH 'Ad' RETURN n");
            AssertEqual("STARTS WITH", stringFilter.WhereOperator, "Parsed string WHERE operator");

            GraphQueryAst ordered = Parser.Parse("MATCH (n:Person) RETURN n ORDER BY n.name DESC LIMIT 5");
            AssertEqual("n", ordered.OrderVariable, "Parsed ORDER BY variable");
            AssertEqual("name", ordered.OrderField, "Parsed ORDER BY field");
            AssertTrue(ordered.OrderDescending, "Parsed ORDER BY direction");

            Planner planner = new Planner();
            GraphQueryPlan plan = planner.Plan(ast, new GraphQueryRequest { Query = "MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 5" });
            AssertEqual(GraphQueryPlanSeedKindEnum.NodeName, plan.SeedKind, "Planner identifies node name seed");
            AssertEqual(GraphQueryKindEnum.MatchNode, plan.Kind, "Planner preserves query kind");
            AssertTrue(!plan.Mutates, "Planner identifies read query");

            GraphQueryAst update = Parser.Parse("MATCH (n:Person) WHERE n.guid = $node SET n.name = $name RETURN n");
            AssertEqual(GraphQueryKindEnum.UpdateNode, update.Kind, "Parsed update query kind");
            AssertEqual("n", update.SetVariable, "Parsed SET variable");
            AssertTrue(update.SetProperties.ContainsKey("name"), "Parsed SET property");

            GraphQueryAst path = Parser.Parse("MATCH (a:Person)-[e1:LINKS]->(b:Person)-[e2:LINKS]->(c:Person) WHERE a.guid = $start RETURN a, e1, b, e2, c LIMIT 10");
            AssertEqual(GraphQueryKindEnum.MatchPath, path.Kind, "Parsed path query kind");
            AssertEqual(2, path.PathSegments.Count, "Parsed path segment count");
            AssertEqual("e2", path.PathSegments[1].EdgeVariable, "Parsed second edge variable");

            GraphQueryAst delete = Parser.Parse("MATCH ()-[e:KNOWS]->() WHERE e.guid = $edge DELETE e RETURN e");
            AssertEqual(GraphQueryKindEnum.DeleteEdge, delete.Kind, "Parsed delete query kind");
            AssertEqual("e", delete.DeleteVariable, "Parsed DELETE variable");

            GraphQueryAst tagUpdate = Parser.Parse("MATCH TAG t WHERE t.guid = $tag SET t.value = 'updated' RETURN t");
            AssertEqual(GraphQueryKindEnum.UpdateTag, tagUpdate.Kind, "Parsed tag update query kind");
            AssertEqual("t", tagUpdate.ObjectVariable, "Parsed tag object variable");

            GraphQueryAst vectorDelete = Parser.Parse("MATCH VECTOR v WHERE v.guid = $vector DELETE v RETURN v");
            AssertEqual(GraphQueryKindEnum.DeleteVector, vectorDelete.Kind, "Parsed vector delete query kind");
            AssertEqual("v", vectorDelete.DeleteVariable, "Parsed vector DELETE variable");

            GraphQueryAst call = Parser.Parse("CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 1");
            AssertEqual(GraphQueryKindEnum.VectorSearch, call.Kind, "Parsed CALL query kind");
            AssertEqual(VectorSearchDomainEnum.Node, call.VectorDomain.GetValueOrDefault(), "Parsed vector search domain");

            GraphQueryAst variablePath = Parser.Parse("MATCH (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start RETURN a, path, c LIMIT 10");
            AssertEqual(GraphQueryKindEnum.MatchPath, variablePath.Kind, "Parsed variable-length path query kind");
            AssertEqual(1, variablePath.PathSegments.Count, "Parsed variable-length segment count");
            AssertTrue(variablePath.PathSegments[0].IsVariableLength, "Parsed variable-length segment flag");
            AssertEqual(1, variablePath.PathSegments[0].MinHops, "Parsed variable-length minimum");
            AssertEqual(3, variablePath.PathSegments[0].MaxHops, "Parsed variable-length maximum");

            GraphQueryAst shortestPath = Parser.Parse("MATCH SHORTEST (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start RETURN a, path, c LIMIT 10");
            AssertTrue(shortestPath.IsShortestPath, "Parsed MATCH SHORTEST flag");
            AssertEqual(GraphQueryKindEnum.MatchPath, shortestPath.Kind, "Parsed shortest path kind");

            GraphQueryAst optionalMatch = Parser.Parse("OPTIONAL MATCH (n:Person) WHERE n.name = 'Missing' RETURN n LIMIT 1");
            AssertTrue(optionalMatch.IsOptional, "Parsed OPTIONAL MATCH flag");
            AssertEqual(GraphQueryKindEnum.MatchNode, optionalMatch.Kind, "Parsed OPTIONAL MATCH kind");

            try
            {
                Parser.Parse("MATCH (n RETURN n");
                throw new InvalidOperationException("Parser should reject malformed MATCH.");
            }
            catch (GraphQueryParseException e)
            {
                AssertTrue(e.Line >= 1, "Parse exception line");
                AssertTrue(e.Column >= 1, "Parse exception column");
            }

            return Task.CompletedTask;
        }

        private static Task TestNativeQueryParserExamples(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            KeyValuePair<string, GraphQueryKindEnum>[] supported = new[]
            {
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n) RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.guid = $nodeGuid RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.name = 'Ada' RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.data.profile.age >= 30 RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.name = 'Ada' OR n.name = 'Grace' RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE NOT n.data.role = 'engineer' RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.name IN ['Ada', 'Grace'] RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.tags.field = 'math' RETURN n", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) RETURN COUNT(*) AS total, AVG(n.data.profile.age) AS averageAge", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a)-[e]->(b) RETURN a, e, b", GraphQueryKindEnum.MatchEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a)-[e:KNOWS]->(b) WHERE a.guid = $from RETURN a, e, b", GraphQueryKindEnum.MatchEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a)-[e:KNOWS]->(b) WHERE b.guid = $to RETURN a, e, b", GraphQueryKindEnum.MatchEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a)-[e:KNOWS]->(b) WHERE e.guid = $edgeGuid RETURN e", GraphQueryKindEnum.MatchEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a)-[e:KNOWS]->(b) WHERE e.data.kind = 'collaboration' RETURN e", GraphQueryKindEnum.MatchEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a:Person)-[e1:LINKS]->(b:Person)-[e2:LINKS]->(c:Person) WHERE a.guid = $start RETURN a, e1, b, e2, c LIMIT 10", GraphQueryKindEnum.MatchPath),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start RETURN a, path, c LIMIT 10", GraphQueryKindEnum.MatchPath),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH SHORTEST (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start RETURN a, path, c LIMIT 10", GraphQueryKindEnum.MatchPath),
                new KeyValuePair<string, GraphQueryKindEnum>("OPTIONAL MATCH (n:Person) WHERE n.name = 'Missing' RETURN n LIMIT 1", GraphQueryKindEnum.MatchNode),
                new KeyValuePair<string, GraphQueryKindEnum>("CREATE (n:Person { name: $name, data: $data }) RETURN n", GraphQueryKindEnum.CreateNode),
                new KeyValuePair<string, GraphQueryKindEnum>("CREATE ()-[e:KNOWS { from: $from, to: $to, name: $name, data: $data }]->() RETURN e", GraphQueryKindEnum.CreateEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("CREATE LABEL l { nodeGuid: $node, label: 'Scientist' } RETURN l", GraphQueryKindEnum.CreateLabel),
                new KeyValuePair<string, GraphQueryKindEnum>("CREATE TAG t { nodeGuid: $node, key: 'field', value: 'math' } RETURN t", GraphQueryKindEnum.CreateTag),
                new KeyValuePair<string, GraphQueryKindEnum>("CREATE VECTOR v { nodeGuid: $node, model: 'touchstone-query', embeddings: $embedding, content: 'Ada vector' } RETURN v", GraphQueryKindEnum.CreateVector),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.guid = $node SET n.name = $name, n.data = $data RETURN n", GraphQueryKindEnum.UpdateNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH ()-[e:LINKS]->() WHERE e.guid = $edge SET e.name = $name, e.cost = 7 RETURN e", GraphQueryKindEnum.UpdateEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH ()-[e:LINKS]->() WHERE e.guid = $edge DELETE e RETURN e", GraphQueryKindEnum.DeleteEdge),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH (n:Person) WHERE n.guid = $node DELETE n RETURN n", GraphQueryKindEnum.DeleteNode),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH LABEL l WHERE l.guid = $label SET l.label = $value RETURN l", GraphQueryKindEnum.UpdateLabel),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH TAG t WHERE t.guid = $tag SET t.value = $value RETURN t", GraphQueryKindEnum.UpdateTag),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH VECTOR v WHERE v.guid = $vector SET v.content = $content, v.embeddings = $embedding RETURN v", GraphQueryKindEnum.UpdateVector),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH LABEL l WHERE l.guid = $label DELETE l RETURN l", GraphQueryKindEnum.DeleteLabel),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH TAG t WHERE t.guid = $tag DELETE t RETURN t", GraphQueryKindEnum.DeleteTag),
                new KeyValuePair<string, GraphQueryKindEnum>("MATCH VECTOR v WHERE v.guid = $vector DELETE v RETURN v", GraphQueryKindEnum.DeleteVector),
                new KeyValuePair<string, GraphQueryKindEnum>("CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 5", GraphQueryKindEnum.VectorSearch),
                new KeyValuePair<string, GraphQueryKindEnum>("CALL litegraph.vector.searchEdges($embedding) YIELD edge, score RETURN edge, score LIMIT 5", GraphQueryKindEnum.VectorSearch),
                new KeyValuePair<string, GraphQueryKindEnum>("CALL litegraph.vector.searchGraph($embedding) YIELD result RETURN result LIMIT 5", GraphQueryKindEnum.VectorSearch)
            };

            foreach (KeyValuePair<string, GraphQueryKindEnum> example in supported)
            {
                GraphQueryAst ast = Parser.Parse(example.Key);
                AssertEqual(example.Value, ast.Kind, "Documented parser example kind: " + example.Key);
                AssertTrue(ast.ReturnItems.Count > 0, "Documented parser example has RETURN: " + example.Key);
            }

            AssertParseFails("MATCH (n) WHERE n.guid = $node SET n.name = 'Ada', m.name = 'Grace' RETURN n", "SET assignments must target a single variable", "multi-variable SET");
            AssertParseFails("MATCH (n) WHERE n.guid = $node DELETE m RETURN n", "DELETE can only target the matched node variable", "wrong DELETE variable");
            AssertParseFails("MATCH LABEL l WHERE l.guid = $label RETURN l", "requires SET or DELETE", "native object MATCH without mutation");
            AssertParseFails("MATCH (n) RETURN n LIMIT 0", "LIMIT must be a positive integer", "zero LIMIT");
            AssertParseFails("MATCH (n) RETURN n, COUNT(*) AS total", "cannot mix aggregate expressions", "mixed aggregate return");
            AssertParseFails("MATCH (n) RETURN SUM(n)", "requires a variable field path", "aggregate field required");
            AssertParseFails("MATCH (a)-[path:LINKS*]->(b) RETURN a, path, b", "must be bounded", "unbounded variable path");
            AssertParseFails("MATCH SHORTEST (a)-[e:LINKS]->(b) RETURN a, e, b", "requires a bounded variable-length path", "shortest without variable path");
            AssertParseFails("OPTIONAL MATCH (n) SET n.name = 'Ada' RETURN n", "does not support SET", "optional mutation");

            try
            {
                Parser.Parse("  CALL not.supported($embedding) RETURN result");
                throw new InvalidOperationException("Unsupported CALL procedure should fail.");
            }
            catch (GraphQueryParseException e)
            {
                AssertTrue(e.Message.Contains("Unsupported CALL procedure"), "Unsupported CALL error message");
                AssertEqual(1, e.Line, "Unsupported CALL line");
                AssertEqual(8, e.Column, "Unsupported CALL column");
            }

            return Task.CompletedTask;
        }

        private static void AssertParseFails(string query, string expectedMessageFragment, string description)
        {
            try
            {
                Parser.Parse(query);
                throw new InvalidOperationException(description + " should fail.");
            }
            catch (GraphQueryParseException e)
            {
                AssertTrue(e.Message.Contains(expectedMessageFragment), description + " error message");
                AssertTrue(e.Line >= 1, description + " line");
                AssertTrue(e.Column >= 1, description + " column");
            }
        }

        private static async Task TestNativeQueryParameterErrors(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-errors.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Error Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Error Graph" }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult unusedParameter = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n) RETURN n LIMIT 1",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "unused", "ignored" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(0, unusedParameter.RowCount, "Unused query parameters do not fail execution");

                await AssertQueryFails(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n) WHERE n.guid = $missing RETURN n"
                    },
                    "Missing query parameter 'missing'",
                    "Missing parameter",
                    cancellationToken).ConfigureAwait(false);

                await AssertQueryFails(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n) WHERE n.guid = $node RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", "not-a-guid" }
                        }
                    },
                    "is not a GUID",
                    "Invalid GUID parameter",
                    cancellationToken).ConfigureAwait(false);

                await AssertQueryFails(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n) RETURN m"
                    },
                    "MATCH node queries can only RETURN the matched node variable",
                    "Unsupported return variable",
                    cancellationToken).ConfigureAwait(false);

                using (CancellationTokenSource cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    try
                    {
                        await client.Query.Execute(
                            tenant.GUID,
                            graph.GUID,
                            new GraphQueryRequest
                            {
                                Query = "MATCH (n) RETURN n LIMIT 1"
                            },
                            cancelled.Token).ConfigureAwait(false);

                        throw new InvalidOperationException("Cancelled query should fail.");
                    }
                    catch (OperationCanceledException)
                    {
                        AssertTrue(true, "Cancelled query throws OperationCanceledException");
                    }
                }
            }

            DeleteFileIfExists(filename);
        }

        private static async Task AssertQueryFails(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryRequest request,
            string expectedMessageFragment,
            string description,
            CancellationToken cancellationToken)
        {
            try
            {
                await client.Query.Execute(tenantGuid, graphGuid, request, cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException(description + " should fail.");
            }
            catch (ArgumentException e)
            {
                AssertTrue(e.Message.Contains(expectedMessageFragment), description + " error message");
            }
        }

        private static Task TestNativeQueryPlanner(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Planner planner = new Planner();

            GraphQueryPlan nodeGuid = Plan(
                planner,
                "MATCH (n:Person) WHERE n.guid = $node RETURN n LIMIT 1");
            AssertEqual(GraphQueryPlanSeedKindEnum.NodeGuid, nodeGuid.SeedKind, "Planner identifies node GUID seed");
            AssertEqual(10, nodeGuid.EstimatedCost, "Planner node GUID seed cost");

            GraphQueryPlan edgeFrom = Plan(
                planner,
                "MATCH (a)-[e:KNOWS]->(b) WHERE a.guid = $from RETURN a, e, b LIMIT 5");
            AssertEqual(GraphQueryPlanSeedKindEnum.EdgeFromGuid, edgeFrom.SeedKind, "Planner identifies edge from GUID seed");
            AssertEqual("a", edgeFrom.SeedVariable, "Planner edge from seed variable");

            GraphQueryPlan edgeTo = Plan(
                planner,
                "MATCH (a)-[e:KNOWS]->(b) WHERE b.guid = $to RETURN a, e, b LIMIT 5");
            AssertEqual(GraphQueryPlanSeedKindEnum.EdgeToGuid, edgeTo.SeedKind, "Planner identifies edge to GUID seed");

            GraphQueryPlan edgeName = Plan(
                planner,
                "MATCH (a)-[e:KNOWS]->(b) WHERE e.name = $name RETURN e LIMIT 5");
            AssertEqual(GraphQueryPlanSeedKindEnum.EdgeName, edgeName.SeedKind, "Planner identifies edge name seed");

            GraphQueryPlan pathSecondEdge = Plan(
                planner,
                "MATCH (a)-[e1:LINKS]->(b)-[e2:LINKS]->(c) WHERE e2.guid = $edge RETURN a, e1, b, e2, c LIMIT 10");
            AssertEqual(GraphQueryPlanSeedKindEnum.EdgeGuid, pathSecondEdge.SeedKind, "Planner identifies second path edge seed");
            AssertEqual("e2", pathSecondEdge.SeedVariable, "Planner identifies second path edge variable");

            GraphQueryPlan orderedUnbounded = Plan(
                planner,
                "MATCH (n:Person) RETURN n ORDER BY n.name",
                5000);
            AssertTrue(orderedUnbounded.HasOrder, "Planner detects ORDER BY");
            AssertFalse(orderedUnbounded.HasLimit, "Planner detects missing LIMIT");
            AssertTrue(orderedUnbounded.Warnings.Count >= 2, "Planner emits unbounded ORDER BY warnings");

            GraphQueryPlan booleanPredicate = Plan(
                planner,
                "MATCH (n:Person) WHERE n.name = 'Ada' OR NOT n.name IN ['Charles'] RETURN n LIMIT 5");
            AssertEqual(GraphQueryPlanSeedKindEnum.None, booleanPredicate.SeedKind, "Planner avoids unsafe OR/NOT seed");
            AssertTrue(booleanPredicate.Warnings.Any(warning => warning.Contains("OR/NOT")), "Planner warns on OR/NOT scan");

            GraphQueryPlan aggregate = Plan(
                planner,
                "MATCH (n:Person) RETURN COUNT(*) AS total");
            AssertTrue(aggregate.Warnings.Any(warning => warning.Contains("Aggregate")), "Planner warns on aggregate scan");
            AssertEqual(90, aggregate.EstimatedCost, "Planner aggregate cost");

            GraphQueryPlan variablePath = Plan(
                planner,
                "MATCH (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start RETURN a, path, c LIMIT 10");
            AssertEqual(GraphQueryKindEnum.MatchPath, variablePath.Kind, "Planner preserves variable-length path kind");
            AssertTrue(variablePath.Warnings.Any(warning => warning.Contains("Variable-length")), "Planner warns on variable-length path expansion");
            AssertTrue(variablePath.EstimatedCost >= 250, "Planner assigns higher variable-length path cost");

            GraphQueryPlan shortestPath = Plan(
                planner,
                "MATCH SHORTEST (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start RETURN a, path, c LIMIT 10");
            AssertTrue(shortestPath.Warnings.Any(warning => warning.Contains("MATCH SHORTEST")), "Planner warns on shortest path evaluation");

            GraphQueryPlan optionalMatch = Plan(
                planner,
                "OPTIONAL MATCH (n:Person) WHERE n.name = 'Missing' RETURN n LIMIT 1");
            AssertTrue(optionalMatch.Warnings.Any(warning => warning.Contains("OPTIONAL MATCH")), "Planner warns on OPTIONAL MATCH null row behavior");

            GraphQueryPlan update = Plan(
                planner,
                "MATCH (n:Person) WHERE n.guid = $node SET n.name = $name RETURN n");
            AssertTrue(update.Mutates, "Planner classifies update as mutation");

            GraphQueryPlan vector = Plan(
                planner,
                "CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 3");
            AssertTrue(vector.UsesVectorSearch, "Planner detects vector search");
            AssertEqual(GraphQueryPlanSeedKindEnum.VectorIndex, vector.SeedKind, "Planner identifies vector index seed");
            AssertEqual(5, vector.EstimatedCost, "Planner vector index cost");

            return Task.CompletedTask;
        }

        private static GraphQueryPlan Plan(Planner planner, string query, int maxResults = 1000)
        {
            return planner.Plan(
                Parser.Parse(query),
                new GraphQueryRequest
                {
                    Query = query,
                    MaxResults = maxResults
                });
        }

        private static async Task TestNativeQueryCreateObjectsAndVectorSearch(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-objects.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Objects Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Objects Graph" }, cancellationToken).ConfigureAwait(false);

                Node ada = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Ada"
                }, cancellationToken).ConfigureAwait(false);
                Node charles = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Charles"
                }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult edgeCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE ()-[e:COLLABORATED { from: $from, to: $to, name: $name }]->() RETURN e",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "from", ada.GUID },
                            { "to", charles.GUID },
                            { "name", "Analytical Engine" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(edgeCreate.Mutated, "Edge create mutates");
                AssertEqual(1, edgeCreate.Edges.Count, "Edge create result count");
                AssertEqual("Analytical Engine", edgeCreate.Edges[0].Name, "Edge create name");

                GraphQueryResult labelCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE LABEL l { nodeGuid: $node, label: 'Scientist' } RETURN l",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", ada.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, labelCreate.Labels.Count, "Label create result count");
                AssertEqual("Scientist", labelCreate.Labels[0].Label, "Label value");

                GraphQueryResult tagCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE TAG t { nodeGuid: $node, key: 'field', value: 'math' } RETURN t",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", ada.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, tagCreate.Tags.Count, "Tag create result count");
                AssertEqual("field", tagCreate.Tags[0].Key, "Tag key");

                List<float> embedding = BuildDeterministicVector(0, 4);
                GraphQueryResult vectorCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE VECTOR v { nodeGuid: $node, model: 'touchstone-query', embeddings: $embedding, content: 'Ada vector' } RETURN v",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", ada.GUID },
                            { "embedding", embedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, vectorCreate.Vectors.Count, "Vector create result count");
                AssertEqual(4, vectorCreate.Vectors[0].Dimensionality, "Vector dimensionality");

                List<float> charlesEmbedding = BuildDeterministicVector(2, 4);
                GraphQueryResult secondVectorCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE VECTOR v { nodeGuid: $node, model: 'touchstone-query', embeddings: $embedding, content: 'Charles vector' } RETURN v",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", charles.GUID },
                            { "embedding", charlesEmbedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, secondVectorCreate.Vectors.Count, "Second node vector create result count");

                await client.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    new VectorIndexConfiguration
                    {
                        VectorIndexType = VectorIndexTypeEnum.HnswRam,
                        VectorDimensionality = 4,
                        VectorIndexThreshold = 1,
                        VectorIndexM = 8,
                        VectorIndexEf = 16,
                        VectorIndexEfConstruction = 32
                    },
                    cancellationToken).ConfigureAwait(false);

                VectorIndexStatistics? vectorIndexStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(vectorIndexStats, "Query vector index statistics");
                AssertEqual(VectorIndexTypeEnum.HnswRam, vectorIndexStats!.IndexType, "Query vector index type");
                AssertEqual(2, vectorIndexStats.VectorCount, "Query-created node vectors indexed");

                GraphQueryResult vectorSearch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 1",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "embedding", embedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, vectorSearch.RowCount, "Vector search row count");
                AssertEqual(ada.GUID, vectorSearch.Nodes[0].GUID, "Vector search returns node");

                List<float> edgeEmbedding = BuildDeterministicVector(1, 4);
                GraphQueryResult edgeVectorCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE VECTOR v { edgeGuid: $edge, model: 'touchstone-query', embeddings: $embedding, content: 'Edge vector' } RETURN v",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "edge", edgeCreate.Edges[0].GUID },
                            { "embedding", edgeEmbedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeVectorCreate.Vectors.Count, "Edge vector create result count");

                GraphQueryResult edgeVectorSearch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CALL litegraph.vector.searchEdges($embedding) YIELD edge, score RETURN edge, score LIMIT 1",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "embedding", edgeEmbedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, edgeVectorSearch.RowCount, "Edge vector search row count");
                AssertEqual(edgeCreate.Edges[0].GUID, edgeVectorSearch.Edges[0].GUID, "Edge vector search returns edge");

                List<float> graphSearchEmbedding = BuildDeterministicVector(2, 4);
                Graph otherGraph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Other Query Objects Graph" }, cancellationToken).ConfigureAwait(false);
                await client.Vector.Create(new VectorMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = otherGraph.GUID,
                    Model = "touchstone-query",
                    Dimensionality = graphSearchEmbedding.Count,
                    Content = "Other graph exact vector",
                    Vectors = graphSearchEmbedding
                }, cancellationToken).ConfigureAwait(false);

                List<float> graphEmbedding = BuildDeterministicVector(3, 4);
                GraphQueryResult graphVectorCreate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CREATE VECTOR v { model: 'touchstone-query', embeddings: $embedding, content: 'Graph vector' } RETURN v",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "embedding", graphEmbedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, graphVectorCreate.Vectors.Count, "Graph vector create result count");

                GraphQueryResult graphVectorSearch = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "CALL litegraph.vector.searchGraph($embedding) YIELD graph, score, result RETURN graph, score, result LIMIT 1",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "embedding", graphSearchEmbedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, graphVectorSearch.RowCount, "Graph vector search row count");
                AssertEqual(graph.GUID, ((Graph)graphVectorSearch.Rows[0]["graph"]).GUID, "Graph vector search stays scoped to the query graph");
                AssertEqual(graph.GUID, graphVectorSearch.VectorSearchResults[0].Graph.GUID, "Graph vector search result graph");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestNativeQueryUpdateAndDelete(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-mutations.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Mutation Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Mutation Graph" }, cancellationToken).ConfigureAwait(false);

                Node first = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "First",
                    Labels = new List<string> { "Person" }
                }, cancellationToken).ConfigureAwait(false);

                Node second = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Second"
                }, cancellationToken).ConfigureAwait(false);

                Edge edge = await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Original Edge",
                    From = first.GUID,
                    To = second.GUID,
                    Labels = new List<string> { "LINKS" }
                }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult nodeUpdate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.guid = $node SET n.name = $name, n.data = $data RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", first.GUID },
                            { "name", "Updated First" },
                            { "data", new Dictionary<string, object> { { "status", "updated" } } }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(nodeUpdate.Mutated, "Node update mutates");
                AssertEqual(1, nodeUpdate.RowCount, "Node update row count");
                AssertEqual("Updated First", nodeUpdate.Nodes[0].Name, "Node update returned name");

                Node persistedNode = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, first.GUID, token: cancellationToken).ConfigureAwait(false);
                AssertEqual("Updated First", persistedNode.Name, "Node update persisted");

                GraphQueryResult edgeUpdate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH ()-[e:LINKS]->() WHERE e.guid = $edge SET e.name = $name, e.cost = 7 RETURN e",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "edge", edge.GUID },
                            { "name", "Updated Edge" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(edgeUpdate.Mutated, "Edge update mutates");
                AssertEqual(1, edgeUpdate.RowCount, "Edge update row count");
                AssertEqual("Updated Edge", edgeUpdate.Edges[0].Name, "Edge update returned name");
                AssertEqual(7, edgeUpdate.Edges[0].Cost, "Edge update returned cost");

                Edge persistedEdge = await client.Edge.ReadByGuid(tenant.GUID, graph.GUID, edge.GUID, token: cancellationToken).ConfigureAwait(false);
                AssertEqual("Updated Edge", persistedEdge.Name, "Edge update persisted");
                AssertEqual(7, persistedEdge.Cost, "Edge update cost persisted");

                GraphQueryResult edgeDelete = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH ()-[e:LINKS]->() WHERE e.guid = $edge DELETE e RETURN e",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "edge", edge.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(edgeDelete.Mutated, "Edge delete mutates");
                AssertEqual(1, edgeDelete.RowCount, "Edge delete row count");
                AssertEqual(edge.GUID, edgeDelete.Edges[0].GUID, "Edge delete returns deleted edge");
                AssertFalse(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edge.GUID, cancellationToken).ConfigureAwait(false), "Edge delete persisted");

                GraphQueryResult nodeDelete = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH (n:Person) WHERE n.guid = $node DELETE n RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "node", first.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(nodeDelete.Mutated, "Node delete mutates");
                AssertEqual(1, nodeDelete.RowCount, "Node delete row count");
                AssertEqual(first.GUID, nodeDelete.Nodes[0].GUID, "Node delete returns deleted node");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, first.GUID, cancellationToken).ConfigureAwait(false), "Node delete persisted");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestNativeQueryUpdateAndDeleteMetadata(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-metadata-mutations.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Query Metadata Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Query Metadata Graph" }, cancellationToken).ConfigureAwait(false);
                Node node = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Metadata Target"
                }, cancellationToken).ConfigureAwait(false);

                LabelMetadata label = await client.Label.Create(new LabelMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    NodeGUID = node.GUID,
                    Label = "OriginalLabel"
                }, cancellationToken).ConfigureAwait(false);

                TagMetadata tag = await client.Tag.Create(new TagMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    NodeGUID = node.GUID,
                    Key = "kind",
                    Value = "original"
                }, cancellationToken).ConfigureAwait(false);

                VectorMetadata vector = await client.Vector.Create(new VectorMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    NodeGUID = node.GUID,
                    Model = "query-metadata-test",
                    Content = "original vector",
                    Dimensionality = 4,
                    Vectors = BuildDeterministicVector(2, 4)
                }, cancellationToken).ConfigureAwait(false);

                GraphQueryResult labelUpdate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH LABEL l WHERE l.guid = $label SET l.label = $value RETURN l",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "label", label.GUID },
                            { "value", "UpdatedLabel" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(labelUpdate.Mutated, "Label update mutates");
                AssertEqual(1, labelUpdate.RowCount, "Label update row count");
                AssertEqual("UpdatedLabel", labelUpdate.Labels[0].Label, "Label update returned value");

                GraphQueryResult tagUpdate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH TAG t WHERE t.guid = $tag SET t.value = $value RETURN t",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "tag", tag.GUID },
                            { "value", "updated" }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(tagUpdate.Mutated, "Tag update mutates");
                AssertEqual(1, tagUpdate.RowCount, "Tag update row count");
                AssertEqual("updated", tagUpdate.Tags[0].Value, "Tag update returned value");

                List<float> updatedEmbedding = BuildDeterministicVector(3, 4);
                GraphQueryResult vectorUpdate = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH VECTOR v WHERE v.guid = $vector SET v.content = $content, v.embeddings = $embedding RETURN v",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "vector", vector.GUID },
                            { "content", "updated vector" },
                            { "embedding", updatedEmbedding }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(vectorUpdate.Mutated, "Vector update mutates");
                AssertEqual(1, vectorUpdate.RowCount, "Vector update row count");
                AssertEqual("updated vector", vectorUpdate.Vectors[0].Content, "Vector update returned content");
                AssertEqual(updatedEmbedding.Count, vectorUpdate.Vectors[0].Dimensionality, "Vector update dimensionality");

                GraphQueryResult vectorDelete = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH VECTOR v WHERE v.guid = $vector DELETE v RETURN v",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "vector", vector.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, vectorDelete.RowCount, "Vector delete row count");
                AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, vector.GUID, cancellationToken).ConfigureAwait(false), "Vector delete persisted");

                GraphQueryResult tagDelete = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH TAG t WHERE t.guid = $tag DELETE t RETURN t",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "tag", tag.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, tagDelete.RowCount, "Tag delete row count");
                AssertFalse(await client.Tag.ExistsByGuid(tenant.GUID, tag.GUID, cancellationToken).ConfigureAwait(false), "Tag delete persisted");

                GraphQueryResult labelDelete = await client.Query.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new GraphQueryRequest
                    {
                        Query = "MATCH LABEL l WHERE l.guid = $label DELETE l RETURN l",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "label", label.GUID }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertEqual(1, labelDelete.RowCount, "Label delete row count");
                AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, label.GUID, cancellationToken).ConfigureAwait(false), "Label delete persisted");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientExecute(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-client.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Client Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Client Graph" }, cancellationToken).ConfigureAwait(false);

                Guid node1Guid = Guid.NewGuid();
                Guid node2Guid = Guid.NewGuid();
                Guid edgeGuid = Guid.NewGuid();

                TransactionResult committed = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = node1Guid, Name = "Transaction Node 1" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = node2Guid, Name = "Transaction Node 2" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Edge,
                                Payload = new Edge { GUID = edgeGuid, Name = "Transaction Edge", From = node1Guid, To = node2Guid }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(committed.Success, "Transaction committed");
                AssertEqual("Committed", committed.State, "Committed transaction lifecycle state");
                AssertEqual(3, committed.Operations.Count, "Committed operation count");
                AssertEqual(3, committed.OperationCount, "Committed diagnostic operation count");
                AssertFalse(committed.TransactionId == Guid.Empty, "Committed transaction has diagnostic ID");
                AssertTrue(committed.StartedUtc > DateTime.MinValue, "Committed transaction has start timestamp");
                AssertTrue(committed.CompletedUtc >= committed.StartedUtc, "Committed transaction has completion timestamp");
                AssertTrue(committed.DurationMs >= 0, "Committed transaction duration is recorded");
                AssertTrue(committed.CommitDurationMs >= 0, "Committed transaction commit duration is recorded");
                AssertEqual("Sqlite", committed.Provider, "Committed transaction provider");
                AssertEqual("Default", committed.IsolationLevel, "Committed transaction isolation level");
                AssertTrue(committed.IsolatedRepository, "Committed transaction used isolated repository");
                AssertFalse(committed.SerializedByGate, "Committed transaction did not use legacy gate");
                AssertEqual(0, committed.RetryCount, "Committed transaction retry count");
                AssertFalse(committed.Retryable, "Committed transaction is not retryable");
                AssertFalse(committed.ConcurrencyConflict, "Committed transaction is not a concurrency conflict");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Committed node 1 exists");
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Committed edge exists");

                Guid rollbackNodeGuid = Guid.NewGuid();
                TransactionResult rolledBack = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = rollbackNodeGuid, Name = "Rollback Node" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = rollbackNodeGuid, Name = "Duplicate Rollback Node" }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertFalse(rolledBack.Success, "Transaction rolled back");
                AssertEqual("RolledBack", rolledBack.State, "Rolled back transaction lifecycle state");
                AssertTrue(rolledBack.RolledBack, "Rollback flag");
                AssertFalse(rolledBack.TransactionId == Guid.Empty, "Rolled back transaction has diagnostic ID");
                AssertEqual(2, rolledBack.OperationCount, "Rolled back diagnostic operation count");
                AssertTrue(rolledBack.RollbackDurationMs >= 0, "Rolled back transaction rollback duration is recorded");
                AssertEqual("Sqlite", rolledBack.Provider, "Rolled back transaction provider");
                AssertTrue(rolledBack.IsolatedRepository, "Rolled back transaction used isolated repository");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, rollbackNodeGuid, cancellationToken).ConfigureAwait(false), "Rolled back node does not exist");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientBuilder(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-builder.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Builder Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Builder Graph" }, cancellationToken).ConfigureAwait(false);

                Guid node1Guid = Guid.NewGuid();
                Guid node2Guid = Guid.NewGuid();
                Guid edgeGuid = Guid.NewGuid();

                TransactionRequest createRequest = client.Transaction
                    .CreateRequestBuilder()
                    .WithMaxOperations(10)
                    .WithTimeoutSeconds(30)
                    .WithIsolationLevel(TransactionIsolationLevelEnum.Serializable)
                    .CreateNode(new Node { GUID = node1Guid, Name = "Builder Node 1" })
                    .CreateNode(new Node { GUID = node2Guid, Name = "Builder Node 2" })
                    .CreateEdge(new Edge { GUID = edgeGuid, Name = "Builder Edge", From = node1Guid, To = node2Guid })
                    .Build();

                AssertEqual(3, createRequest.Operations.Count, "Builder create operation count");
                AssertEqual(10, createRequest.MaxOperations, "Builder max operations");
                AssertEqual(30, createRequest.TimeoutSeconds, "Builder timeout");
                AssertEqual(TransactionIsolationLevelEnum.Serializable, createRequest.IsolationLevel, "Builder isolation level");

                TransactionResult created = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    createRequest,
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(created.Success, "Builder create transaction committed");
                AssertEqual("Serializable", created.IsolationLevel, "Builder transaction result isolation level");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Builder node 1 exists");
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Builder edge exists");

                TransactionRequest mutationRequest = client.Transaction
                    .CreateRequestBuilder()
                    .UpdateNode(new Node { Name = "Builder Node 1 Updated" }, node1Guid)
                    .DeleteEdge(edgeGuid)
                    .DeleteNode(node2Guid)
                    .Build();

                TransactionResult mutated = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    mutationRequest,
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(mutated.Success, "Builder mutation transaction committed");
                Node updatedNode = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, node1Guid, token: cancellationToken).ConfigureAwait(false);
                AssertEqual("Builder Node 1 Updated", updatedNode.Name, "Builder update persisted");
                AssertFalse(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Builder edge delete persisted");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, node2Guid, cancellationToken).ConfigureAwait(false), "Builder node delete persisted");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientAttachDetachUpsert(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-attach-detach-upsert.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Attach Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Attach Graph" }, cancellationToken).ConfigureAwait(false);

                Guid node1Guid = Guid.NewGuid();
                Guid node2Guid = Guid.NewGuid();
                Guid edgeGuid = Guid.NewGuid();
                Guid labelGuid = Guid.NewGuid();
                Guid tagGuid = Guid.NewGuid();
                Guid vectorGuid = Guid.NewGuid();

                TransactionRequest attachRequest = client.Transaction
                    .CreateRequestBuilder()
                    .UpsertNode(new Node { GUID = node1Guid, Name = "Attach Node 1" })
                    .UpsertNode(new Node { GUID = node2Guid, Name = "Attach Node 2" })
                    .UpsertEdge(new Edge { GUID = edgeGuid, Name = "Attach Edge", From = node1Guid, To = node2Guid, Cost = 3 })
                    .AttachLabel(new LabelMetadata { GUID = labelGuid, NodeGUID = node1Guid, Label = "Attached" })
                    .AttachTag(new TagMetadata { GUID = tagGuid, NodeGUID = node1Guid, Key = "kind", Value = "initial" })
                    .AttachVector(new VectorMetadata
                    {
                        GUID = vectorGuid,
                        NodeGUID = node1Guid,
                        Model = "transaction-upsert",
                        Dimensionality = 4,
                        Content = "initial vector",
                        Vectors = BuildDeterministicVector(0, 4)
                    })
                    .Build();

                AssertEqual(6, attachRequest.Operations.Count, "Attach/upsert builder operation count");
                AssertEqual(TransactionOperationTypeEnum.Upsert, attachRequest.Operations[0].OperationType, "Builder node upsert operation");
                AssertEqual(TransactionOperationTypeEnum.Attach, attachRequest.Operations[3].OperationType, "Builder label attach operation");

                TransactionResult attached = await client.Transaction.Execute(tenant.GUID, graph.GUID, attachRequest, cancellationToken).ConfigureAwait(false);
                AssertTrue(attached.Success, "Attach/upsert transaction committed");
                AssertEqual(6, attached.Operations.Count, "Attach/upsert result operation count");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Upsert created node");
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Upsert created edge");
                AssertTrue(await client.Label.ExistsByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false), "Attach created label");
                AssertTrue(await client.Tag.ExistsByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false), "Attach created tag");
                AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false), "Attach created vector");

                TransactionRequest updateRequest = client.Transaction
                    .CreateRequestBuilder()
                    .UpsertNode(new Node { Name = "Attach Node 1 Updated" }, node1Guid)
                    .UpsertEdge(new Edge { Name = "Attach Edge Updated", From = node1Guid, To = node2Guid, Cost = 9 }, edgeGuid)
                    .UpsertLabel(new LabelMetadata { NodeGUID = node1Guid, Label = "AttachedUpdated" }, labelGuid)
                    .UpsertTag(new TagMetadata { NodeGUID = node1Guid, Key = "kind", Value = "updated" }, tagGuid)
                    .UpsertVector(new VectorMetadata
                    {
                        NodeGUID = node1Guid,
                        Model = "transaction-upsert",
                        Dimensionality = 4,
                        Content = "updated vector",
                        Vectors = BuildDeterministicVector(1, 4)
                    }, vectorGuid)
                    .Build();

                TransactionResult updated = await client.Transaction.Execute(tenant.GUID, graph.GUID, updateRequest, cancellationToken).ConfigureAwait(false);
                AssertTrue(updated.Success, "Upsert update transaction committed");
                Node updatedNode = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, node1Guid, token: cancellationToken).ConfigureAwait(false);
                Edge updatedEdge = await client.Edge.ReadByGuid(tenant.GUID, graph.GUID, edgeGuid, token: cancellationToken).ConfigureAwait(false);
                LabelMetadata updatedLabel = await client.Label.ReadByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false);
                TagMetadata updatedTag = await client.Tag.ReadByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false);
                VectorMetadata updatedVector = await client.Vector.ReadByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false);
                AssertEqual("Attach Node 1 Updated", updatedNode.Name, "Upsert updated node");
                AssertEqual("Attach Edge Updated", updatedEdge.Name, "Upsert updated edge");
                AssertEqual(9, updatedEdge.Cost, "Upsert updated edge cost");
                AssertEqual("AttachedUpdated", updatedLabel.Label, "Upsert updated label");
                AssertEqual("updated", updatedTag.Value, "Upsert updated tag");
                AssertEqual("updated vector", updatedVector.Content, "Upsert updated vector");

                TransactionRequest detachRequest = client.Transaction
                    .CreateRequestBuilder()
                    .DetachLabel(labelGuid)
                    .DetachTag(tagGuid)
                    .DetachVector(vectorGuid)
                    .Build();

                TransactionResult detached = await client.Transaction.Execute(tenant.GUID, graph.GUID, detachRequest, cancellationToken).ConfigureAwait(false);
                AssertTrue(detached.Success, "Detach transaction committed");
                AssertEqual(TransactionOperationTypeEnum.Detach, detached.Operations[0].OperationType, "Detach operation result type");
                AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false), "Detach removed label");
                AssertFalse(await client.Tag.ExistsByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false), "Detach removed tag");
                AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false), "Detach removed vector");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Detach leaves node");
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Detach leaves edge");

                Guid invalidLabelGuid = Guid.NewGuid();
                TransactionResult invalidAttach = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Attach,
                                ObjectType = TransactionObjectTypeEnum.Label,
                                Payload = new LabelMetadata { GUID = invalidLabelGuid, NodeGUID = node1Guid, Label = "ShouldRollback" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Attach,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { Name = "Invalid Attach Node" }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertFalse(invalidAttach.Success, "Invalid attach transaction fails");
                AssertEqual("Faulted", invalidAttach.State, "Invalid attach lifecycle state");
                AssertFalse(invalidAttach.RolledBack, "Invalid attach fails before transaction start");
                AssertTrue(invalidAttach.ValidationFailure, "Invalid attach reports validation failure");
                AssertEqual(1, invalidAttach.FailedOperationIndex.GetValueOrDefault(), "Invalid attach failed index");
                AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, invalidLabelGuid, cancellationToken).ConfigureAwait(false), "Invalid attach did not write label");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientOperationMatrix(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-operation-matrix.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Matrix Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Matrix Graph" }, cancellationToken).ConfigureAwait(false);

                Guid node1Guid = Guid.NewGuid();
                Guid node2Guid = Guid.NewGuid();
                Guid edgeGuid = Guid.NewGuid();
                Guid labelGuid = Guid.NewGuid();
                Guid tagGuid = Guid.NewGuid();
                Guid vectorGuid = Guid.NewGuid();

                TransactionRequest createRequest = client.Transaction
                    .CreateRequestBuilder()
                    .CreateNode(new Node { GUID = node1Guid, Name = "Matrix Node 1" })
                    .CreateNode(new Node { GUID = node2Guid, Name = "Matrix Node 2" })
                    .CreateEdge(new Edge { GUID = edgeGuid, Name = "Matrix Edge", From = node1Guid, To = node2Guid, Cost = 4 })
                    .CreateLabel(new LabelMetadata { GUID = labelGuid, NodeGUID = node1Guid, Label = "MatrixLabel" })
                    .CreateTag(new TagMetadata { GUID = tagGuid, NodeGUID = node1Guid, Key = "matrix", Value = "initial" })
                    .CreateVector(new VectorMetadata
                    {
                        GUID = vectorGuid,
                        NodeGUID = node1Guid,
                        Model = "transaction-matrix",
                        Dimensionality = 4,
                        Content = "matrix vector",
                        Vectors = BuildDeterministicVector(2, 4)
                    })
                    .Build();

                TransactionResult created = await client.Transaction.Execute(tenant.GUID, graph.GUID, createRequest, cancellationToken).ConfigureAwait(false);
                AssertTrue(created.Success, "Operation matrix create transaction committed");
                AssertEqual("Committed", created.State, "Operation matrix create transaction state");
                AssertEqual(6, created.OperationCount, "Operation matrix create operation count");
                AssertEqual(6, created.Operations.Count, "Operation matrix create results");
                AssertTrue(created.Operations.All(operation => operation.Success), "Operation matrix create results succeeded");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Operation matrix node 1 create persisted");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node2Guid, cancellationToken).ConfigureAwait(false), "Operation matrix node 2 create persisted");
                AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Operation matrix edge create persisted");
                AssertTrue(await client.Label.ExistsByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false), "Operation matrix label create persisted");
                AssertTrue(await client.Tag.ExistsByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false), "Operation matrix tag create persisted");
                AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false), "Operation matrix vector create persisted");

                TransactionRequest updateRequest = client.Transaction
                    .CreateRequestBuilder()
                    .UpdateEdge(new Edge { Name = "Matrix Edge Updated", From = node1Guid, To = node2Guid, Cost = 8 }, edgeGuid)
                    .UpdateLabel(new LabelMetadata { NodeGUID = node1Guid, Label = "MatrixLabelUpdated" }, labelGuid)
                    .UpdateTag(new TagMetadata { NodeGUID = node1Guid, Key = "matrix", Value = "updated" }, tagGuid)
                    .UpdateVector(new VectorMetadata
                    {
                        NodeGUID = node1Guid,
                        Model = "transaction-matrix",
                        Dimensionality = 4,
                        Content = "matrix vector updated",
                        Vectors = BuildDeterministicVector(3, 4)
                    }, vectorGuid)
                    .Build();

                TransactionResult updated = await client.Transaction.Execute(tenant.GUID, graph.GUID, updateRequest, cancellationToken).ConfigureAwait(false);
                AssertTrue(updated.Success, "Operation matrix update transaction committed at failed index " + updated.FailedOperationIndex + ": " + updated.Error);
                AssertEqual("Committed", updated.State, "Operation matrix update transaction state");
                AssertEqual(4, updated.OperationCount, "Operation matrix update operation count");

                Edge updatedEdge = await client.Edge.ReadByGuid(tenant.GUID, graph.GUID, edgeGuid, token: cancellationToken).ConfigureAwait(false);
                LabelMetadata updatedLabel = await client.Label.ReadByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false);
                TagMetadata updatedTag = await client.Tag.ReadByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false);
                VectorMetadata updatedVector = await client.Vector.ReadByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false);
                AssertEqual("Matrix Edge Updated", updatedEdge.Name, "Operation matrix edge update persisted");
                AssertEqual(8, updatedEdge.Cost, "Operation matrix edge cost update persisted");
                AssertEqual("MatrixLabelUpdated", updatedLabel.Label, "Operation matrix label update persisted");
                AssertEqual("updated", updatedTag.Value, "Operation matrix tag update persisted");
                AssertEqual("matrix vector updated", updatedVector.Content, "Operation matrix vector update persisted");
                AssertEqual(4, updatedVector.Vectors.Count, "Operation matrix vector embedding persisted");

                TransactionRequest deleteRequest = client.Transaction
                    .CreateRequestBuilder()
                    .DeleteLabel(labelGuid)
                    .DeleteTag(tagGuid)
                    .DeleteVector(vectorGuid)
                    .Build();

                TransactionResult deleted = await client.Transaction.Execute(tenant.GUID, graph.GUID, deleteRequest, cancellationToken).ConfigureAwait(false);
                AssertTrue(deleted.Success, "Operation matrix child delete transaction committed");
                AssertEqual("Committed", deleted.State, "Operation matrix child delete transaction state");
                AssertEqual(3, deleted.OperationCount, "Operation matrix child delete operation count");
                AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false), "Operation matrix label delete persisted");
                AssertFalse(await client.Tag.ExistsByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false), "Operation matrix tag delete persisted");
                AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false), "Operation matrix vector delete persisted");

                TransactionResult parentUpdated = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    client.Transaction
                        .CreateRequestBuilder()
                        .UpdateNode(new Node { Name = "Matrix Node 1 Updated" }, node1Guid)
                        .Build(),
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(parentUpdated.Success, "Operation matrix node update transaction committed: " + parentUpdated.Error);
                Node updatedNode = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, node1Guid, token: cancellationToken).ConfigureAwait(false);
                AssertEqual("Matrix Node 1 Updated", updatedNode.Name, "Operation matrix node update persisted");

                TransactionResult parentDeleted = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    client.Transaction
                        .CreateRequestBuilder()
                        .DeleteEdge(edgeGuid)
                        .DeleteNode(node2Guid)
                        .Build(),
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(parentDeleted.Success, "Operation matrix edge/node delete transaction committed: " + parentDeleted.Error);
                AssertEqual("Committed", parentDeleted.State, "Operation matrix edge/node delete transaction state");
                AssertFalse(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Operation matrix edge delete persisted");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, node2Guid, cancellationToken).ConfigureAwait(false), "Operation matrix node delete persisted");
                AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Operation matrix unaffected node remains");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientMixedRollbackAndLimits(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-client-mixed.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Mixed Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Mixed Graph" }, cancellationToken).ConfigureAwait(false);

                try
                {
                    await client.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new TransactionRequest
                        {
                            MaxOperations = 1,
                            Operations = new List<TransactionOperation>
                            {
                                new TransactionOperation
                                {
                                    OperationType = TransactionOperationTypeEnum.Create,
                                    ObjectType = TransactionObjectTypeEnum.Node,
                                    Payload = new Node { Name = "Over Limit 1" }
                                },
                                new TransactionOperation
                                {
                                    OperationType = TransactionOperationTypeEnum.Create,
                                    ObjectType = TransactionObjectTypeEnum.Node,
                                    Payload = new Node { Name = "Over Limit 2" }
                                }
                            }
                        },
                        cancellationToken).ConfigureAwait(false);

                    throw new InvalidOperationException("Transaction MaxOperations should be enforced.");
                }
                catch (ArgumentException e)
                {
                    AssertTrue(e.Message.Contains("MaxOperations"), "MaxOperations exception identifies limit");
                }

                Guid prevalidationNodeGuid = Guid.NewGuid();
                TransactionResult invalidShape = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = prevalidationNodeGuid, Name = "Should Not Be Written" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Delete,
                                ObjectType = TransactionObjectTypeEnum.Node
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertFalse(invalidShape.Success, "Invalid transaction shape fails");
                AssertEqual("Faulted", invalidShape.State, "Invalid transaction shape lifecycle state");
                AssertFalse(invalidShape.RolledBack, "Invalid transaction shape does not report rollback before transaction start");
                AssertTrue(invalidShape.ValidationFailure, "Invalid transaction shape reports validation failure");
                AssertTrue(invalidShape.FailedOperationIndex != null, "Invalid transaction shape includes failed index");
                AssertEqual(1, invalidShape.FailedOperationIndex.GetValueOrDefault(), "Invalid transaction shape failed index");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, prevalidationNodeGuid, cancellationToken).ConfigureAwait(false), "Prevalidated transaction does not write earlier operations");

                Guid node1Guid = Guid.NewGuid();
                Guid node2Guid = Guid.NewGuid();
                Guid edgeGuid = Guid.NewGuid();
                Guid labelGuid = Guid.NewGuid();
                Guid tagGuid = Guid.NewGuid();
                Guid vectorGuid = Guid.NewGuid();

                TransactionResult rolledBack = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = node1Guid, Name = "Mixed Node 1" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = node2Guid, Name = "Mixed Node 2" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Edge,
                                Payload = new Edge { GUID = edgeGuid, Name = "Mixed Edge", From = node1Guid, To = node2Guid }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Label,
                                Payload = new LabelMetadata { GUID = labelGuid, NodeGUID = node1Guid, Label = "Mixed" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Tag,
                                Payload = new TagMetadata { GUID = tagGuid, NodeGUID = node1Guid, Key = "kind", Value = "mixed" }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Vector,
                                Payload = new VectorMetadata
                                {
                                    GUID = vectorGuid,
                                    NodeGUID = node1Guid,
                                    Model = "transaction-test",
                                    Dimensionality = 4,
                                    Content = "mixed vector",
                                    Vectors = BuildDeterministicVector(1, 4)
                                }
                            },
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = node1Guid, Name = "Duplicate Mixed Node" }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertFalse(rolledBack.Success, "Mixed transaction rolled back");
                AssertEqual("RolledBack", rolledBack.State, "Mixed transaction lifecycle state");
                AssertTrue(rolledBack.RolledBack, "Mixed transaction rollback flag");
                AssertEqual(6, rolledBack.FailedOperationIndex.GetValueOrDefault(), "Mixed transaction failed operation index");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, node1Guid, cancellationToken).ConfigureAwait(false), "Mixed rollback removed node 1");
                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, node2Guid, cancellationToken).ConfigureAwait(false), "Mixed rollback removed node 2");
                AssertFalse(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, edgeGuid, cancellationToken).ConfigureAwait(false), "Mixed rollback removed edge");
                AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, labelGuid, cancellationToken).ConfigureAwait(false), "Mixed rollback removed label");
                AssertFalse(await client.Tag.ExistsByGuid(tenant.GUID, tagGuid, cancellationToken).ConfigureAwait(false), "Mixed rollback removed tag");
                AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, vectorGuid, cancellationToken).ConfigureAwait(false), "Mixed rollback removed vector");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientCancellation(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-client-cancellation.db";
            DeleteFileIfExists(filename);

            using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = filename
            })))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Cancellation Tenant" }, cancellationToken).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Cancellation Graph" }, cancellationToken).ConfigureAwait(false);
                Guid nodeGuid = Guid.NewGuid();

                using (CancellationTokenSource cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    TransactionResult result = await client.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new TransactionRequest
                        {
                            Operations = new List<TransactionOperation>
                            {
                                new TransactionOperation
                                {
                                    OperationType = TransactionOperationTypeEnum.Create,
                                    ObjectType = TransactionObjectTypeEnum.Node,
                                    Payload = new Node { GUID = nodeGuid, Name = "Cancelled Node" }
                                }
                            }
                        },
                        cancelled.Token).ConfigureAwait(false);

                    AssertFalse(result.Success, "Cancelled transaction fails");
                    AssertEqual("Faulted", result.State, "Cancelled transaction lifecycle state");
                    AssertFalse(result.RolledBack, "Cancelled transaction does not report rollback before transaction start");
                    AssertFalse(result.ValidationFailure, "Cancelled transaction is not validation failure");
                    AssertTrue(result.Error.Contains("canceled") || result.Error.Contains("cancelled"), "Cancelled transaction error indicates cancellation");
                }

                AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, nodeGuid, cancellationToken).ConfigureAwait(false), "Cancelled transaction leaves no node");
            }

            DeleteFileIfExists(filename);
        }

        private static async Task TestGraphTransactionClientTimeout(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (TransactionTimeoutRepository repo = new TransactionTimeoutRepository(TimeSpan.FromMilliseconds(1500)))
            using (LiteGraphClient client = new LiteGraphClient(repo, null, null, null, false))
            {
                Guid tenantGuid = Guid.NewGuid();
                Guid graphGuid = Guid.NewGuid();
                Guid nodeGuid = Guid.NewGuid();

                TransactionResult result = await client.Transaction.Execute(
                    tenantGuid,
                    graphGuid,
                    new TransactionRequest
                    {
                        TimeoutSeconds = 1,
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = nodeGuid, Name = "Timeout Node" }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertFalse(result.Success, "Timed-out transaction fails");
                AssertEqual("RolledBack", result.State, "Timed-out transaction lifecycle state");
                AssertTrue(result.RolledBack, "Timed-out transaction reports rollback");
                AssertEqual(0, result.FailedOperationIndex.GetValueOrDefault(), "Timed-out transaction failed operation index");
                AssertTrue(result.Error.Contains("canceled", StringComparison.OrdinalIgnoreCase)
                    || result.Error.Contains("cancelled", StringComparison.OrdinalIgnoreCase), "Timed-out transaction error indicates cancellation");
                AssertTrue(repo.RollbackCalled, "Timed-out transaction calls repository rollback");
                AssertFalse(repo.GraphTransactionActive, "Timed-out transaction clears active state");
                AssertFalse(repo.NodeCreateCompleted, "Timed-out transaction did not finish the long-running node create");
            }
        }

        private static Task TestGraphTransactionProviderErrorDiagnostics(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AssertProviderErrorDiagnostics("40001", true, true, "PostgreSQL serialization failure");
            AssertProviderErrorDiagnostics("40P01", true, true, "PostgreSQL deadlock detected");
            AssertProviderErrorDiagnostics("55P03", true, true, "PostgreSQL lock not available");
            AssertProviderErrorDiagnostics("23505", false, false, "PostgreSQL unique violation");

            return Task.CompletedTask;
        }

        private static void AssertProviderErrorDiagnostics(
            string providerCode,
            bool expectedRetryable,
            bool expectedConcurrencyConflict,
            string context)
        {
            TransactionResult result = new TransactionResult();
            MethodInfo? applyDiagnostics = typeof(LiteGraph.Client.Implementations.TransactionMethods).GetMethod(
                "ApplyProviderErrorDiagnostics",
                BindingFlags.NonPublic | BindingFlags.Static);

            AssertNotNull(applyDiagnostics, "Transaction diagnostics helper");
            applyDiagnostics!.Invoke(null, new object[] { result, new SyntheticProviderException(providerCode) });

            AssertEqual(providerCode, result.ProviderErrorCode, context + " provider error code");
            AssertEqual(expectedRetryable, result.Retryable, context + " retryable flag");
            AssertEqual(expectedConcurrencyConflict, result.ConcurrencyConflict, context + " concurrency-conflict flag");
        }

        private static async Task TestGraphTransactionClientActiveGuard(CancellationToken cancellationToken)
        {
            using (GraphRepositoryBase repo = new TransactionTimeoutRepository(TimeSpan.Zero))
            using (LiteGraphClient client = new LiteGraphClient(repo, null, null, null, false))
            {
                Guid tenantGuid = Guid.NewGuid();
                Guid graphGuid = Guid.NewGuid();

                await repo.BeginGraphTransaction(tenantGuid, graphGuid, cancellationToken).ConfigureAwait(false);

                try
                {
                    try
                    {
                        await client.Query.Execute(
                            tenantGuid,
                            graphGuid,
                            new GraphQueryRequest
                            {
                                Query = "CREATE (n:Blocked { name: $name }) RETURN n",
                                Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    { "name", "Rejected Query Node" }
                                }
                            },
                            cancellationToken).ConfigureAwait(false);

                        throw new InvalidOperationException("Query mutation should not execute while a transaction is active.");
                    }
                    catch (InvalidOperationException e)
                    {
                        AssertTrue(e.Message.Contains("already active"), "Rejected query mutation names active transaction");
                    }

                    AssertTrue(repo.GraphTransactionActive, "Rejected query mutation leaves existing transaction active");
                }
                finally
                {
                    if (repo.GraphTransactionActive)
                        await repo.RollbackGraphTransaction(CancellationToken.None).ConfigureAwait(false);
                }

                Guid subsequentNodeGuid = Guid.NewGuid();
                TransactionResult subsequent = await client.Transaction.Execute(
                    tenantGuid,
                    graphGuid,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = subsequentNodeGuid, Name = "Subsequent Transaction Node" }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(subsequent.Success, "Transaction API works after active guard rollback");
            }
        }

        private static async Task TestGraphTransactionClientConcurrentQueue(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-client-concurrent-queue.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Transaction Queue Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Transaction Queue Graph" }, cancellationToken).ConfigureAwait(false);

                    Guid[] nodeGuids = Enumerable.Range(0, 4)
                        .Select(_ => Guid.NewGuid())
                        .ToArray();

                    Task<TransactionResult>[] transactions = nodeGuids
                        .Select((nodeGuid, index) => client.Transaction.Execute(
                            tenant.GUID,
                            graph.GUID,
                            new TransactionRequest
                            {
                                Operations = new List<TransactionOperation>
                                {
                                    new TransactionOperation
                                    {
                                        OperationType = TransactionOperationTypeEnum.Create,
                                        ObjectType = TransactionObjectTypeEnum.Node,
                                        Payload = new Node { GUID = nodeGuid, Name = "Queued Transaction Node " + index }
                                    }
                                }
                            },
                            cancellationToken))
                        .ToArray();

                    TransactionResult[] results = await Task.WhenAll(transactions).ConfigureAwait(false);

                    for (int i = 0; i < results.Length; i++)
                    {
                        AssertTrue(results[i].Success, "Queued transaction " + i + " committed");
                        AssertEqual("Committed", results[i].State, "Queued transaction " + i + " lifecycle state");
                        AssertFalse(results[i].RolledBack, "Queued transaction " + i + " did not roll back");
                        AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, nodeGuids[i], cancellationToken).ConfigureAwait(false), "Queued transaction node " + i + " exists");
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionConcurrentSameGraphMixedObjects(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-concurrent-same-graph.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Concurrent Same Graph Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Concurrent Same Graph" }, cancellationToken).ConfigureAwait(false);
                    var specs = Enumerable.Range(0, 6)
                        .Select(index => new
                        {
                            Index = index,
                            NodeA = Guid.NewGuid(),
                            NodeB = Guid.NewGuid(),
                            Edge = Guid.NewGuid(),
                            Label = Guid.NewGuid(),
                            Tag = Guid.NewGuid(),
                            Vector = Guid.NewGuid()
                        })
                        .ToArray();

                    Task<TransactionResult>[] transactions = specs
                        .Select(spec => client.Transaction.Execute(
                            tenant.GUID,
                            graph.GUID,
                            client.Transaction
                                .CreateRequestBuilder()
                                .CreateNode(new Node { GUID = spec.NodeA, Name = "Concurrent Mixed Node A " + spec.Index })
                                .CreateNode(new Node { GUID = spec.NodeB, Name = "Concurrent Mixed Node B " + spec.Index })
                                .CreateEdge(new Edge { GUID = spec.Edge, Name = "Concurrent Mixed Edge " + spec.Index, From = spec.NodeA, To = spec.NodeB, Cost = spec.Index + 1 })
                                .CreateLabel(new LabelMetadata { GUID = spec.Label, NodeGUID = spec.NodeA, Label = "ConcurrentMixed" + spec.Index })
                                .CreateTag(new TagMetadata { GUID = spec.Tag, NodeGUID = spec.NodeA, Key = "slot", Value = spec.Index.ToString() })
                                .CreateVector(new VectorMetadata
                                {
                                    GUID = spec.Vector,
                                    NodeGUID = spec.NodeA,
                                    Model = "concurrent-mixed",
                                    Dimensionality = 4,
                                    Content = "concurrent mixed vector " + spec.Index,
                                    Vectors = BuildDeterministicVector(spec.Index % 4, 4)
                                })
                                .Build(),
                            cancellationToken))
                        .ToArray();

                    TransactionResult[] results = await Task.WhenAll(transactions).ConfigureAwait(false);

                    for (int i = 0; i < specs.Length; i++)
                    {
                        AssertTrue(results[i].Success, "Same-graph mixed transaction " + i + " committed: " + results[i].Error);
                        AssertEqual("Committed", results[i].State, "Same-graph mixed transaction " + i + " lifecycle state");
                        AssertEqual(6, results[i].OperationCount, "Same-graph mixed transaction " + i + " operation count");
                        AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].NodeA, cancellationToken).ConfigureAwait(false), "Same-graph mixed node A " + i + " exists");
                        AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].NodeB, cancellationToken).ConfigureAwait(false), "Same-graph mixed node B " + i + " exists");
                        AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, specs[i].Edge, cancellationToken).ConfigureAwait(false), "Same-graph mixed edge " + i + " exists");
                        AssertTrue(await client.Label.ExistsByGuid(tenant.GUID, specs[i].Label, cancellationToken).ConfigureAwait(false), "Same-graph mixed label " + i + " exists");
                        AssertTrue(await client.Tag.ExistsByGuid(tenant.GUID, specs[i].Tag, cancellationToken).ConfigureAwait(false), "Same-graph mixed tag " + i + " exists");
                        AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, specs[i].Vector, cancellationToken).ConfigureAwait(false), "Same-graph mixed vector " + i + " exists");
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionConcurrentDifferentGraphs(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-concurrent-different-graphs.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Concurrent Different Graphs Tenant" }, cancellationToken).ConfigureAwait(false);
                    List<(int Index, Graph Graph, Guid NodeA, Guid NodeB, Guid Edge)> specs = new List<(int Index, Graph Graph, Guid NodeA, Guid NodeB, Guid Edge)>();
                    for (int i = 0; i < 5; i++)
                    {
                        Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Concurrent Graph " + i }, cancellationToken).ConfigureAwait(false);
                        specs.Add((i, graph, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
                    }

                    Task<TransactionResult>[] transactions = specs
                        .Select(spec => client.Transaction.Execute(
                            tenant.GUID,
                            spec.Graph.GUID,
                            client.Transaction
                                .CreateRequestBuilder()
                                .CreateNode(new Node { GUID = spec.NodeA, Name = "Different Graph Node A " + spec.Index })
                                .CreateNode(new Node { GUID = spec.NodeB, Name = "Different Graph Node B " + spec.Index })
                                .CreateEdge(new Edge { GUID = spec.Edge, Name = "Different Graph Edge " + spec.Index, From = spec.NodeA, To = spec.NodeB })
                                .Build(),
                            cancellationToken))
                        .ToArray();

                    TransactionResult[] results = await Task.WhenAll(transactions).ConfigureAwait(false);

                    for (int i = 0; i < specs.Count; i++)
                    {
                        AssertTrue(results[i].Success, "Different-graph transaction " + i + " committed: " + results[i].Error);
                        AssertEqual("Committed", results[i].State, "Different-graph transaction " + i + " lifecycle state");
                        AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].NodeA, cancellationToken).ConfigureAwait(false), "Different-graph node A " + i + " exists");
                        AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].NodeB, cancellationToken).ConfigureAwait(false), "Different-graph node B " + i + " exists");
                        AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, specs[i].Graph.GUID, specs[i].Edge, cancellationToken).ConfigureAwait(false), "Different-graph edge " + i + " exists");
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionConcurrentCommitRollbackIsolation(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-concurrent-commit-rollback.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Concurrent Rollback Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Concurrent Rollback Graph" }, cancellationToken).ConfigureAwait(false);
                    var specs = Enumerable.Range(0, 8)
                        .Select(index => new
                        {
                            Index = index,
                            ShouldCommit = index % 2 == 0,
                            NodeA = Guid.NewGuid(),
                            NodeB = Guid.NewGuid(),
                            Edge = Guid.NewGuid(),
                            RollbackNode = Guid.NewGuid()
                        })
                        .ToArray();

                    Task<TransactionResult>[] transactions = specs
                        .Select(spec =>
                        {
                            TransactionRequestBuilder builder = client.Transaction.CreateRequestBuilder();
                            if (spec.ShouldCommit)
                            {
                                builder
                                    .CreateNode(new Node { GUID = spec.NodeA, Name = "Commit Isolation Node A " + spec.Index })
                                    .CreateNode(new Node { GUID = spec.NodeB, Name = "Commit Isolation Node B " + spec.Index })
                                    .CreateEdge(new Edge { GUID = spec.Edge, Name = "Commit Isolation Edge " + spec.Index, From = spec.NodeA, To = spec.NodeB });
                            }
                            else
                            {
                                builder
                                    .CreateNode(new Node { GUID = spec.RollbackNode, Name = "Rollback Isolation Node " + spec.Index })
                                    .CreateNode(new Node { GUID = spec.RollbackNode, Name = "Rollback Isolation Duplicate " + spec.Index });
                            }

                            return client.Transaction.Execute(tenant.GUID, graph.GUID, builder.Build(), cancellationToken);
                        })
                        .ToArray();

                    TransactionResult[] results = await Task.WhenAll(transactions).ConfigureAwait(false);

                    for (int i = 0; i < specs.Length; i++)
                    {
                        if (specs[i].ShouldCommit)
                        {
                            AssertTrue(results[i].Success, "Concurrent commit " + i + " succeeded: " + results[i].Error);
                            AssertEqual("Committed", results[i].State, "Concurrent commit " + i + " lifecycle state");
                            AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].NodeA, cancellationToken).ConfigureAwait(false), "Concurrent commit node A " + i + " exists");
                            AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].NodeB, cancellationToken).ConfigureAwait(false), "Concurrent commit node B " + i + " exists");
                            AssertTrue(await client.Edge.ExistsByGuid(tenant.GUID, graph.GUID, specs[i].Edge, cancellationToken).ConfigureAwait(false), "Concurrent commit edge " + i + " exists");
                        }
                        else
                        {
                            AssertFalse(results[i].Success, "Concurrent rollback " + i + " failed as expected");
                            AssertEqual("RolledBack", results[i].State, "Concurrent rollback " + i + " lifecycle state");
                            AssertTrue(results[i].RolledBack, "Concurrent rollback " + i + " reports rollback");
                            AssertFalse(await client.Node.ExistsByGuid(tenant.GUID, specs[i].RollbackNode, cancellationToken).ConfigureAwait(false), "Concurrent rollback node " + i + " is absent");
                        }
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionConcurrentAttachDetachMetadata(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-concurrent-attach-detach.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Concurrent Attach Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Concurrent Attach Graph" }, cancellationToken).ConfigureAwait(false);
                    List<(int Index, Guid Node, Guid Label, Guid Tag, Guid Vector)> specs = new List<(int Index, Guid Node, Guid Label, Guid Tag, Guid Vector)>();
                    for (int i = 0; i < 6; i++)
                    {
                        Guid nodeGuid = Guid.NewGuid();
                        Node node = await client.Node.Create(new Node
                        {
                            GUID = nodeGuid,
                            TenantGUID = tenant.GUID,
                            GraphGUID = graph.GUID,
                            Name = "Attach Target Node " + i
                        }, cancellationToken).ConfigureAwait(false);

                        specs.Add((i, node.GUID, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
                    }

                    TransactionResult[] attached = await Task.WhenAll(specs.Select(spec => client.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        client.Transaction
                            .CreateRequestBuilder()
                            .AttachLabel(new LabelMetadata { GUID = spec.Label, NodeGUID = spec.Node, Label = "ConcurrentAttach" + spec.Index })
                            .AttachTag(new TagMetadata { GUID = spec.Tag, NodeGUID = spec.Node, Key = "attach", Value = spec.Index.ToString() })
                            .AttachVector(new VectorMetadata
                            {
                                GUID = spec.Vector,
                                NodeGUID = spec.Node,
                                Model = "concurrent-attach",
                                Dimensionality = 4,
                                Content = "concurrent attach vector " + spec.Index,
                                Vectors = BuildDeterministicVector(spec.Index % 4, 4)
                            })
                            .Build(),
                        cancellationToken))).ConfigureAwait(false);

                    for (int i = 0; i < specs.Count; i++)
                    {
                        AssertTrue(attached[i].Success, "Concurrent attach transaction " + i + " committed: " + attached[i].Error);
                        AssertEqual("Committed", attached[i].State, "Concurrent attach transaction " + i + " lifecycle state");
                        AssertTrue(await client.Label.ExistsByGuid(tenant.GUID, specs[i].Label, cancellationToken).ConfigureAwait(false), "Concurrent attach label " + i + " exists");
                        AssertTrue(await client.Tag.ExistsByGuid(tenant.GUID, specs[i].Tag, cancellationToken).ConfigureAwait(false), "Concurrent attach tag " + i + " exists");
                        AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, specs[i].Vector, cancellationToken).ConfigureAwait(false), "Concurrent attach vector " + i + " exists");
                    }

                    TransactionResult[] detached = await Task.WhenAll(specs.Select(spec => client.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        client.Transaction
                            .CreateRequestBuilder()
                            .DetachLabel(spec.Label)
                            .DetachTag(spec.Tag)
                            .DetachVector(spec.Vector)
                            .Build(),
                        cancellationToken))).ConfigureAwait(false);

                    for (int i = 0; i < specs.Count; i++)
                    {
                        AssertTrue(detached[i].Success, "Concurrent detach transaction " + i + " committed: " + detached[i].Error);
                        AssertEqual("Committed", detached[i].State, "Concurrent detach transaction " + i + " lifecycle state");
                        AssertFalse(await client.Label.ExistsByGuid(tenant.GUID, specs[i].Label, cancellationToken).ConfigureAwait(false), "Concurrent detach label " + i + " removed");
                        AssertFalse(await client.Tag.ExistsByGuid(tenant.GUID, specs[i].Tag, cancellationToken).ConfigureAwait(false), "Concurrent detach tag " + i + " removed");
                        AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, specs[i].Vector, cancellationToken).ConfigureAwait(false), "Concurrent detach vector " + i + " removed");
                        AssertTrue(await client.Node.ExistsByGuid(tenant.GUID, specs[i].Node, cancellationToken).ConfigureAwait(false), "Concurrent detach leaves node " + i);
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionConcurrentUpsertWaves(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-concurrent-upsert-waves.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Concurrent Upsert Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Concurrent Upsert Graph" }, cancellationToken).ConfigureAwait(false);
                    var specs = Enumerable.Range(0, 6)
                        .Select(index => new
                        {
                            Index = index,
                            NodeA = Guid.NewGuid(),
                            NodeB = Guid.NewGuid(),
                            Edge = Guid.NewGuid()
                        })
                        .ToArray();

                    TransactionResult[] created = await Task.WhenAll(specs.Select(spec => client.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        client.Transaction
                            .CreateRequestBuilder()
                            .UpsertNode(new Node { GUID = spec.NodeA, Name = "Upsert Wave Node A " + spec.Index })
                            .UpsertNode(new Node { GUID = spec.NodeB, Name = "Upsert Wave Node B " + spec.Index })
                            .UpsertEdge(new Edge { GUID = spec.Edge, Name = "Upsert Wave Edge " + spec.Index, From = spec.NodeA, To = spec.NodeB, Cost = spec.Index + 1 })
                            .Build(),
                        cancellationToken))).ConfigureAwait(false);

                    for (int i = 0; i < specs.Length; i++)
                    {
                        AssertTrue(created[i].Success, "Concurrent upsert create wave " + i + " committed: " + created[i].Error);
                        AssertEqual("Committed", created[i].State, "Concurrent upsert create wave " + i + " lifecycle state");
                    }

                    TransactionResult[] updated = await Task.WhenAll(specs.Select(spec => client.Transaction.Execute(
                        tenant.GUID,
                        graph.GUID,
                        client.Transaction
                            .CreateRequestBuilder()
                            .UpsertNode(new Node { Name = "Upsert Wave Node A Updated " + spec.Index }, spec.NodeA)
                            .UpsertNode(new Node { Name = "Upsert Wave Node B Updated " + spec.Index }, spec.NodeB)
                            .UpsertEdge(new Edge { Name = "Upsert Wave Edge Updated " + spec.Index, From = spec.NodeA, To = spec.NodeB, Cost = spec.Index + 10 }, spec.Edge)
                            .Build(),
                        cancellationToken))).ConfigureAwait(false);

                    for (int i = 0; i < specs.Length; i++)
                    {
                        AssertTrue(updated[i].Success, "Concurrent upsert update wave " + i + " committed: " + updated[i].Error);
                        AssertEqual("Committed", updated[i].State, "Concurrent upsert update wave " + i + " lifecycle state");
                        Node nodeA = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, specs[i].NodeA, token: cancellationToken).ConfigureAwait(false);
                        Node nodeB = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, specs[i].NodeB, token: cancellationToken).ConfigureAwait(false);
                        Edge edge = await client.Edge.ReadByGuid(tenant.GUID, graph.GUID, specs[i].Edge, token: cancellationToken).ConfigureAwait(false);
                        AssertEqual("Upsert Wave Node A Updated " + i, nodeA.Name, "Concurrent upsert node A " + i + " updated");
                        AssertEqual("Upsert Wave Node B Updated " + i, nodeB.Name, "Concurrent upsert node B " + i + " updated");
                        AssertEqual("Upsert Wave Edge Updated " + i, edge.Name, "Concurrent upsert edge " + i + " updated");
                        AssertEqual(i + 10, edge.Cost, "Concurrent upsert edge " + i + " cost updated");
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionConcurrentVectorCommitRollback(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-transaction-concurrent-vector-rollback.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Concurrent Vector Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Concurrent Vector Graph" }, cancellationToken).ConfigureAwait(false);
                    List<(int Index, bool ShouldCommit, Guid Node, Guid Vector)> specs = new List<(int Index, bool ShouldCommit, Guid Node, Guid Vector)>();
                    for (int i = 0; i < 8; i++)
                    {
                        Guid nodeGuid = Guid.NewGuid();
                        await client.Node.Create(new Node
                        {
                            GUID = nodeGuid,
                            TenantGUID = tenant.GUID,
                            GraphGUID = graph.GUID,
                            Name = "Vector Target Node " + i
                        }, cancellationToken).ConfigureAwait(false);

                        specs.Add((i, i % 2 == 0, nodeGuid, Guid.NewGuid()));
                    }

                    TransactionResult[] results = await Task.WhenAll(specs.Select(spec =>
                    {
                        VectorMetadata vector = new VectorMetadata
                        {
                            GUID = spec.Vector,
                            NodeGUID = spec.Node,
                            Model = "concurrent-vector",
                            Dimensionality = 4,
                            Content = "concurrent vector " + spec.Index,
                            Vectors = BuildDeterministicVector(spec.Index % 4, 4)
                        };

                        TransactionRequestBuilder builder = client.Transaction
                            .CreateRequestBuilder()
                            .CreateVector(vector);

                        if (!spec.ShouldCommit)
                        {
                            builder.CreateVector(new VectorMetadata
                            {
                                GUID = spec.Vector,
                                NodeGUID = spec.Node,
                                Model = "concurrent-vector",
                                Dimensionality = 4,
                                Content = "concurrent duplicate vector " + spec.Index,
                                Vectors = BuildDeterministicVector((spec.Index + 1) % 4, 4)
                            });
                        }

                        return client.Transaction.Execute(tenant.GUID, graph.GUID, builder.Build(), cancellationToken);
                    })).ConfigureAwait(false);

                    for (int i = 0; i < specs.Count; i++)
                    {
                        if (specs[i].ShouldCommit)
                        {
                            AssertTrue(results[i].Success, "Concurrent vector commit " + i + " succeeded: " + results[i].Error);
                            AssertEqual("Committed", results[i].State, "Concurrent vector commit " + i + " lifecycle state");
                            AssertTrue(await client.Vector.ExistsByGuid(tenant.GUID, specs[i].Vector, cancellationToken).ConfigureAwait(false), "Concurrent vector commit " + i + " exists");
                        }
                        else
                        {
                            AssertFalse(results[i].Success, "Concurrent vector rollback " + i + " failed as expected");
                            AssertEqual("RolledBack", results[i].State, "Concurrent vector rollback " + i + " lifecycle state");
                            AssertTrue(results[i].RolledBack, "Concurrent vector rollback " + i + " reports rollback");
                            AssertFalse(await client.Vector.ExistsByGuid(tenant.GUID, specs[i].Vector, cancellationToken).ConfigureAwait(false), "Concurrent vector rollback " + i + " absent");
                        }
                    }
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestGraphTransactionClientIsolatedParallelExecution(CancellationToken cancellationToken)
        {
            ParallelTransactionState state = new ParallelTransactionState();
            using (LiteGraphClient client = new LiteGraphClient(new ParallelTransactionRepository(state, TimeSpan.FromMilliseconds(250), false)))
            {
                Guid tenantGuid = Guid.NewGuid();
                Guid graphGuid = Guid.NewGuid();
                Guid[] nodeGuids = Enumerable.Range(0, 4)
                    .Select(_ => Guid.NewGuid())
                    .ToArray();

                Stopwatch sw = Stopwatch.StartNew();
                TransactionResult[] results = await Task.WhenAll(nodeGuids.Select((nodeGuid, index) => client.Transaction.Execute(
                    tenantGuid,
                    graphGuid,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Node,
                                Payload = new Node { GUID = nodeGuid, Name = "Parallel Transaction Node " + index }
                            }
                        }
                    },
                    cancellationToken))).ConfigureAwait(false);
                sw.Stop();

                for (int i = 0; i < results.Length; i++)
                {
                    AssertTrue(results[i].Success, "Parallel transaction " + i + " committed");
                    AssertEqual("Committed", results[i].State, "Parallel transaction " + i + " lifecycle state");
                    AssertFalse(results[i].RolledBack, "Parallel transaction " + i + " did not roll back");
                    AssertFalse(results[i].TransactionId == Guid.Empty, "Parallel transaction " + i + " has diagnostic ID");
                    AssertEqual(1, results[i].OperationCount, "Parallel transaction " + i + " diagnostic operation count");
                    AssertTrue(results[i].IsolatedRepository, "Parallel transaction " + i + " used isolated repository");
                    AssertFalse(results[i].SerializedByGate, "Parallel transaction " + i + " did not use legacy gate");
                    AssertTrue(results[i].CommitDurationMs >= 0, "Parallel transaction " + i + " commit duration is recorded");
                }

                AssertEqual(nodeGuids.Length, state.BeginCount, "Each parallel transaction started on an isolated repository");
                AssertEqual(nodeGuids.Length, state.CommitCount, "Each parallel transaction committed");
                AssertEqual(nodeGuids.Length, state.DisposeCount, "Each isolated transaction repository was disposed");
                AssertEqual(nodeGuids.Length, state.CreatedNodeGuids.Count, "Each parallel transaction created one node");
                AssertTrue(state.MaxConcurrentCreates > 1, "Isolated transaction repositories execute node creates concurrently");
                AssertTrue(sw.Elapsed < TimeSpan.FromMilliseconds(900), "Parallel transactions do not run behind the legacy serialized gate");
            }
        }

        private static async Task TestGraphQueryIsolatedParallelExecution(CancellationToken cancellationToken)
        {
            ParallelTransactionState state = new ParallelTransactionState();
            using (LiteGraphClient client = new LiteGraphClient(new ParallelTransactionRepository(state, TimeSpan.FromMilliseconds(250), false)))
            {
                Guid tenantGuid = Guid.NewGuid();
                Guid graphGuid = Guid.NewGuid();

                Stopwatch sw = Stopwatch.StartNew();
                GraphQueryResult[] results = await Task.WhenAll(Enumerable.Range(0, 4).Select(index => client.Query.Execute(
                    tenantGuid,
                    graphGuid,
                    new GraphQueryRequest
                    {
                        Query = "CREATE (n:Parallel { name: $name }) RETURN n",
                        Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "name", "Parallel Query Node " + index }
                        }
                    },
                    cancellationToken))).ConfigureAwait(false);
                sw.Stop();

                for (int i = 0; i < results.Length; i++)
                {
                    AssertTrue(results[i].Mutated, "Parallel query mutation " + i + " reports mutation");
                }

                AssertEqual(4, state.BeginCount, "Each parallel query mutation started on an isolated repository");
                AssertEqual(4, state.CommitCount, "Each parallel query mutation committed");
                AssertEqual(4, state.DisposeCount, "Each isolated query transaction repository was disposed");
                AssertEqual(4, state.CreatedNodeGuids.Count, "Each parallel query mutation created one node");
                AssertTrue(state.MaxConcurrentCreates > 1, "Isolated query transaction repositories execute node creates concurrently");
                AssertTrue(sw.Elapsed < TimeSpan.FromMilliseconds(900), "Parallel query mutations do not run behind an ambient repository transaction");
            }
        }

        private sealed class ParallelTransactionState
        {
            internal int BeginCount;
            internal int CommitCount;
            internal int RollbackCount;
            internal int DisposeCount;
            internal int ActiveCreates;
            internal int MaxConcurrentCreates;
            internal List<Guid> CreatedNodeGuids { get; } = new List<Guid>();
            internal object SyncRoot { get; } = new object();

            internal void TrackCreateStart()
            {
                int active = Interlocked.Increment(ref ActiveCreates);
                int current;
                do
                {
                    current = MaxConcurrentCreates;
                    if (active <= current) break;
                }
                while (Interlocked.CompareExchange(ref MaxConcurrentCreates, active, current) != current);
            }

            internal void TrackCreateStop()
            {
                Interlocked.Decrement(ref ActiveCreates);
            }
        }

        private sealed class SyntheticProviderException : Exception
        {
            public SyntheticProviderException(string sqlState)
                : base("Synthetic provider exception " + sqlState)
            {
                SqlState = sqlState;
            }

            public string SqlState { get; }
        }

        private sealed class ParallelTransactionRepository : GraphRepositoryBase
        {
            public override IAdminMethods Admin { get { return Unsupported<IAdminMethods>(); } }
            public override ITenantMethods Tenant { get { return Unsupported<ITenantMethods>(); } }
            public override IUserMethods User { get { return Unsupported<IUserMethods>(); } }
            public override ICredentialMethods Credential { get { return Unsupported<ICredentialMethods>(); } }
            public override ILabelMethods Label { get { return Unsupported<ILabelMethods>(); } }
            public override ITagMethods Tag { get { return Unsupported<ITagMethods>(); } }
            public override IVectorMethods Vector { get { return Unsupported<IVectorMethods>(); } }
            public override IGraphMethods Graph { get { return Unsupported<IGraphMethods>(); } }
            public override INodeMethods Node { get { return _Node; } }
            public override IEdgeMethods Edge { get { return Unsupported<IEdgeMethods>(); } }
            public override IBatchMethods Batch { get { return Unsupported<IBatchMethods>(); } }
            public override IVectorIndexMethods VectorIndex { get { return Unsupported<IVectorIndexMethods>(); } }
            public override IRequestHistoryMethods RequestHistory { get { return Unsupported<IRequestHistoryMethods>(); } }
            public override IAuthorizationAuditMethods AuthorizationAudit { get { return Unsupported<IAuthorizationAuditMethods>(); } }
            public override IAuthorizationRoleMethods AuthorizationRoles { get { return Unsupported<IAuthorizationRoleMethods>(); } }
            public override bool GraphTransactionActive { get { return _Active; } }

            private readonly ParallelTransactionState _State;
            private readonly TimeSpan _NodeCreateDelay;
            private readonly bool _IsTransactionRepository;
            private readonly INodeMethods _Node;
            private bool _Active = false;

            internal ParallelTransactionRepository(ParallelTransactionState state, TimeSpan nodeCreateDelay, bool isTransactionRepository)
            {
                _State = state ?? throw new ArgumentNullException(nameof(state));
                _NodeCreateDelay = nodeCreateDelay;
                _IsTransactionRepository = isTransactionRepository;
                _Node = DispatchProxy.Create<INodeMethods, ParallelNodeMethodsProxy>();
                ParallelNodeMethodsProxy proxy = (ParallelNodeMethodsProxy)(object)_Node;
                proxy.State = _State;
                proxy.Delay = _NodeCreateDelay;
            }

            public override GraphRepositoryBase CreateIsolatedTransactionRepository()
            {
                return new ParallelTransactionRepository(_State, _NodeCreateDelay, true);
            }

            public override void InitializeRepository()
            {
            }

            public override void Flush()
            {
            }

            public override Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                if (!_IsTransactionRepository) throw new InvalidOperationException("Parallel transaction test must use an isolated repository.");
                _Active = true;
                Interlocked.Increment(ref _State.BeginCount);
                return Task.CompletedTask;
            }

            public override Task CommitGraphTransaction(CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                _Active = false;
                Interlocked.Increment(ref _State.CommitCount);
                return Task.CompletedTask;
            }

            public override Task RollbackGraphTransaction(CancellationToken token = default)
            {
                _Active = false;
                Interlocked.Increment(ref _State.RollbackCount);
                return Task.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
                if (Disposed) return;

                if (disposing && _IsTransactionRepository)
                    Interlocked.Increment(ref _State.DisposeCount);

                base.Dispose(disposing);
            }

            private static T Unsupported<T>() where T : class
            {
                T proxy = DispatchProxy.Create<T, UnsupportedMethodsProxy>();
                ((UnsupportedMethodsProxy)(object)proxy).InterfaceName = typeof(T).Name;
                return proxy;
            }
        }

        private class ParallelNodeMethodsProxy : DispatchProxy
        {
            internal ParallelTransactionState State { get; set; } = null!;
            internal TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(250);

            protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod != null && targetMethod.Name == nameof(INodeMethods.Create))
                {
                    if (args == null || args.Length < 1 || args[0] is not Node node)
                        throw new ArgumentException("Node create requires a node payload.");

                    CancellationToken token = args.Length > 1 && args[1] is CancellationToken suppliedToken ? suppliedToken : CancellationToken.None;
                    return CreateDelayed(node, token);
                }

                throw new NotSupportedException("Only node create is supported by the parallel transaction test repository.");
            }

            private async Task<Node> CreateDelayed(Node node, CancellationToken token)
            {
                State.TrackCreateStart();
                try
                {
                    await Task.Delay(Delay, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    lock (State.SyncRoot)
                    {
                        State.CreatedNodeGuids.Add(node.GUID);
                    }

                    return node;
                }
                finally
                {
                    State.TrackCreateStop();
                }
            }
        }

        private sealed class TransactionTimeoutRepository : GraphRepositoryBase
        {
            internal bool RollbackCalled { get; private set; }
            internal bool NodeCreateCompleted
            {
                get
                {
                    return ((DelayedNodeMethodsProxy)(object)_Node).CreateCompleted;
                }
            }

            public override IAdminMethods Admin { get { return Unsupported<IAdminMethods>(); } }
            public override ITenantMethods Tenant { get { return Unsupported<ITenantMethods>(); } }
            public override IUserMethods User { get { return Unsupported<IUserMethods>(); } }
            public override ICredentialMethods Credential { get { return Unsupported<ICredentialMethods>(); } }
            public override ILabelMethods Label { get { return Unsupported<ILabelMethods>(); } }
            public override ITagMethods Tag { get { return Unsupported<ITagMethods>(); } }
            public override IVectorMethods Vector { get { return Unsupported<IVectorMethods>(); } }
            public override IGraphMethods Graph { get { return Unsupported<IGraphMethods>(); } }
            public override INodeMethods Node { get { return _Node; } }
            public override IEdgeMethods Edge { get { return Unsupported<IEdgeMethods>(); } }
            public override IBatchMethods Batch { get { return Unsupported<IBatchMethods>(); } }
            public override IVectorIndexMethods VectorIndex { get { return Unsupported<IVectorIndexMethods>(); } }
            public override IRequestHistoryMethods RequestHistory { get { return Unsupported<IRequestHistoryMethods>(); } }
            public override IAuthorizationAuditMethods AuthorizationAudit { get { return Unsupported<IAuthorizationAuditMethods>(); } }
            public override IAuthorizationRoleMethods AuthorizationRoles { get { return Unsupported<IAuthorizationRoleMethods>(); } }
            public override bool GraphTransactionActive { get { return _Active; } }

            private readonly INodeMethods _Node;
            private bool _Active = false;

            internal TransactionTimeoutRepository(TimeSpan nodeCreateDelay)
            {
                _Node = DispatchProxy.Create<INodeMethods, DelayedNodeMethodsProxy>();
                ((DelayedNodeMethodsProxy)(object)_Node).Delay = nodeCreateDelay;
            }

            public override void InitializeRepository()
            {
            }

            public override void Flush()
            {
            }

            public override Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                _Active = true;
                return Task.CompletedTask;
            }

            public override Task CommitGraphTransaction(CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                _Active = false;
                return Task.CompletedTask;
            }

            public override Task RollbackGraphTransaction(CancellationToken token = default)
            {
                RollbackCalled = true;
                _Active = false;
                return Task.CompletedTask;
            }

            private static T Unsupported<T>() where T : class
            {
                T proxy = DispatchProxy.Create<T, UnsupportedMethodsProxy>();
                ((UnsupportedMethodsProxy)(object)proxy).InterfaceName = typeof(T).Name;
                return proxy;
            }
        }

        private class DelayedNodeMethodsProxy : DispatchProxy
        {
            internal TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
            internal bool CreateCompleted { get; private set; }

            protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod != null && targetMethod.Name == nameof(INodeMethods.Create))
                {
                    if (args == null || args.Length < 1 || args[0] is not Node node)
                        throw new ArgumentException("Node create requires a node payload.");

                    CancellationToken token = args.Length > 1 && args[1] is CancellationToken suppliedToken ? suppliedToken : CancellationToken.None;
                    return CreateDelayed(node, token);
                }

                throw new NotSupportedException("Only node create is supported by the transaction timeout test repository.");
            }

            private async Task<Node> CreateDelayed(Node node, CancellationToken token)
            {
                await Task.Delay(Delay, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                CreateCompleted = true;
                return node;
            }
        }

        private class UnsupportedMethodsProxy : DispatchProxy
        {
            internal string InterfaceName { get; set; } = "repository method";

            protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                throw new NotSupportedException(InterfaceName + " is not supported by the transaction timeout test repository.");
            }
        }

        private static Task TestObservabilityPrometheus(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (ObservabilityService observability = new ObservabilityService(new ObservabilitySettings()))
            {
                observability.RecordHttpRequest("GET", "/v1.0/tenants", 200, 12.5);
                observability.RecordGraphQuery(false, true, 3.5);
                observability.RecordVectorSearch("Node", true, 3, 2.5);
                observability.RecordGraphTransaction(false, true, 2, 4.5);
                observability.RecordGraphTransaction(false, false, true, 1, 1.5);
                observability.RecordAuthentication(AuthenticationResultEnum.Success, AuthorizationResultEnum.Permitted);
                observability.RecordRepositoryOperation("Sqlite", "read", true, 1.5);
                observability.RecordStorageBackend("Sqlite", false);
                observability.RecordStorageConnectionPool("Sqlite", 32, 30);
                observability.RecordEntityCount("tenant", "nodes", 7);
                string metrics = observability.RenderPrometheus();
                AssertTrue(metrics.Contains("litegraph_http_requests_total"), "Prometheus request metric exists");
                AssertTrue(metrics.Contains("method=\"GET\""), "Prometheus method label exists");
                AssertTrue(metrics.Contains("status_code=\"200\""), "Prometheus status code label exists");
                AssertTrue(metrics.Contains("# TYPE litegraph_http_request_duration_ms histogram"), "Prometheus request duration histogram type exists");
                AssertTrue(metrics.Contains("litegraph_http_request_duration_ms_bucket{method=\"GET\",path=\"/v1.0/tenants\",status_code=\"200\",le=\"10\"} 0"), "Prometheus request duration lower bucket exists");
                AssertTrue(metrics.Contains("litegraph_http_request_duration_ms_bucket{method=\"GET\",path=\"/v1.0/tenants\",status_code=\"200\",le=\"25\"} 1"), "Prometheus request duration matching bucket exists");
                AssertTrue(metrics.Contains("litegraph_http_request_duration_ms_bucket{method=\"GET\",path=\"/v1.0/tenants\",status_code=\"200\",le=\"+Inf\"} 1"), "Prometheus request duration infinity bucket exists");
                AssertTrue(metrics.Contains("litegraph_graph_queries_total"), "Prometheus graph query metric exists");
                AssertTrue(metrics.Contains("litegraph_vector_searches_total"), "Prometheus vector search metric exists");
                AssertTrue(metrics.Contains("litegraph_vector_search_results_total"), "Prometheus vector search result metric exists");
                AssertTrue(metrics.Contains("domain=\"Node\""), "Prometheus vector search domain label exists");
                AssertTrue(metrics.Contains("litegraph_graph_transactions_total"), "Prometheus graph transaction metric exists");
                AssertTrue(metrics.Contains("validation_failure=\"true\""), "Prometheus graph transaction validation label exists");
                AssertTrue(metrics.Contains("litegraph_authentication_requests_total"), "Prometheus authentication metric exists");
                AssertTrue(metrics.Contains("litegraph_repository_operations_total"), "Prometheus repository operation metric exists");
                AssertTrue(metrics.Contains("operation=\"read\""), "Prometheus repository operation label exists");
                AssertTrue(metrics.Contains("litegraph_storage_backend_info"), "Prometheus storage backend metric exists");
                AssertTrue(metrics.Contains("provider=\"Sqlite\""), "Prometheus storage backend provider label exists");
                AssertTrue(metrics.Contains("litegraph_storage_connection_pool_max{provider=\"Sqlite\"} 32"), "Prometheus storage pool max metric exists");
                AssertTrue(metrics.Contains("litegraph_storage_command_timeout_seconds{provider=\"Sqlite\"} 30"), "Prometheus storage command timeout metric exists");
                AssertTrue(metrics.Contains("litegraph_entity_count{scope=\"tenant\",entity=\"nodes\"} 7"), "Prometheus entity count metric exists");
            }

            return Task.CompletedTask;
        }

        private static async Task TestMetricsEndpoint(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _McpEnvironment.LiteGraphEndpoint + "/metrics");
                using HttpResponseMessage response = await _ReadinessClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false);

                string metrics = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                AssertEqual(200, (int)response.StatusCode, "Metrics endpoint status");
                AssertTrue(
                    response.Content.Headers.ContentType?.MediaType?.Contains("text/plain", StringComparison.OrdinalIgnoreCase) == true,
                    "Metrics endpoint content type");
                AssertTrue(metrics.Contains("litegraph_http_requests_total"), "Metrics endpoint renders HTTP request metric");
                AssertTrue(metrics.Contains("litegraph_storage_backend_info"), "Metrics endpoint renders storage backend metric");
                AssertTrue(metrics.Contains("litegraph_storage_connection_pool_max"), "Metrics endpoint renders storage connection pool metric");
                AssertTrue(metrics.Contains("litegraph_storage_command_timeout_seconds"), "Metrics endpoint renders storage command timeout metric");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static Task TestGrafanaDashboardTemplate(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string dashboardPath = ResolveRepositoryFile("assets", "grafana", "litegraph-observability-dashboard.json");
            string dashboardJson = File.ReadAllText(dashboardPath);
            using JsonDocument document = JsonDocument.Parse(dashboardJson);
            JsonElement root = document.RootElement;

            AssertEqual("LiteGraph Observability", root.GetProperty("title").GetString(), "Grafana dashboard title");
            AssertEqual("litegraph-observability", root.GetProperty("uid").GetString(), "Grafana dashboard UID");
            AssertTrue(root.GetProperty("tags").EnumerateArray().Any(tag => tag.GetString() == "litegraph"), "Grafana dashboard tag");

            JsonElement panels = root.GetProperty("panels");
            AssertTrue(panels.ValueKind == JsonValueKind.Array, "Grafana panels array");
            AssertTrue(panels.GetArrayLength() >= 10, "Grafana dashboard panel count");

            List<string> expressions = new List<string>();
            CollectGrafanaExpressions(root, expressions);

            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_http_requests_total", StringComparison.Ordinal)), "Grafana HTTP request metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_http_request_duration_ms_sum", StringComparison.Ordinal)), "Grafana HTTP duration metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_graph_queries_total", StringComparison.Ordinal)), "Grafana graph query metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_graph_transactions_total", StringComparison.Ordinal)), "Grafana transaction metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_vector_searches_total", StringComparison.Ordinal)), "Grafana vector search metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_repository_operations_total", StringComparison.Ordinal)), "Grafana repository metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_entity_count", StringComparison.Ordinal)), "Grafana entity count metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_storage_backend_info", StringComparison.Ordinal)), "Grafana storage backend metric");
            AssertTrue(expressions.Any(expr => expr.Contains("litegraph_authentication_requests_total", StringComparison.Ordinal)), "Grafana auth metric");

            return Task.CompletedTask;
        }

        private static void CollectGrafanaExpressions(JsonElement element, List<string> expressions)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        if (property.NameEquals("expr") && property.Value.ValueKind == JsonValueKind.String)
                        {
                            string? expression = property.Value.GetString();
                            if (!String.IsNullOrWhiteSpace(expression)) expressions.Add(expression);
                        }
                        else
                        {
                            CollectGrafanaExpressions(property.Value, expressions);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (JsonElement child in element.EnumerateArray())
                    {
                        CollectGrafanaExpressions(child, expressions);
                    }
                    break;
            }
        }

        private static Task TestObservabilityTracing(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AssertEqual(LiteGraphTelemetry.ActivitySourceName, LiteGraphTelemetry.MeterName, "Core telemetry meter and activity source names match");

            ObservabilitySettings settings = new ObservabilitySettings
            {
                ServiceName = "LiteGraph.Server.Tests." + Guid.NewGuid().ToString("N")
            };

            List<Activity> stoppedActivities = new List<Activity>();
            using (ActivityListener listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == settings.ServiceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => stoppedActivities.Add(activity)
            })
            {
                ActivitySource.AddActivityListener(listener);

                using (ObservabilityService observability = new ObservabilityService(settings))
                {
                    string traceparent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";
                    AssertTrue(ObservabilityService.TryParseTraceContext(traceparent, null, out ActivityContext parentContext), "Trace context parses");

                    Activity serverActivity = observability.StartActivity("GET /metrics", ActivityKind.Server, parentContext);
                    AssertNotNull(serverActivity, "Server activity starts");
                    serverActivity.SetTag("litegraph.request.id", "request-id");

                    Activity authActivity = observability.StartActivity("litegraph.auth", ActivityKind.Internal, serverActivity.Context);
                    AssertNotNull(authActivity, "Auth activity starts");
                    authActivity.SetTag("litegraph.authentication.result", AuthenticationResultEnum.Success.ToString());
                    authActivity.Dispose();

                    serverActivity.Dispose();

                    using ObservabilityService disabledObservability = new ObservabilityService(new ObservabilitySettings
                    {
                        ServiceName = settings.ServiceName,
                        EnableOpenTelemetry = false
                    });
                    Activity disabled = disabledObservability.StartActivity("disabled", ActivityKind.Internal);
                    AssertTrue(disabled == null, "Disabled OpenTelemetry does not start activities");
                }
            }

            Activity server = null!;
            Activity auth = null!;
            foreach (Activity activity in stoppedActivities)
            {
                if (activity.DisplayName == "GET /metrics") server = activity;
                else if (activity.DisplayName == "litegraph.auth") auth = activity;
            }

            AssertNotNull(server, "Stopped server activity captured");
            AssertNotNull(auth, "Stopped auth activity captured");
            AssertEqual("0123456789abcdef0123456789abcdef", server!.TraceId.ToString(), "Server activity preserves incoming trace ID");
            AssertEqual(server.TraceId.ToString(), auth!.TraceId.ToString(), "Child activity shares trace ID");
            AssertEqual(server.SpanId.ToString(), auth.ParentSpanId.ToString(), "Child activity parent span is server activity");

            return Task.CompletedTask;
        }

        private static async Task TestObservabilityQueryPhaseTracing(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-phase-tracing.db";
            DeleteFileIfExists(filename);

            try
            {
                Guid tenantGuid;
                Guid graphGuid;
                Guid expectedNodeGuid;
                List<float> embedding = BuildDeterministicVector(0, 4);

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();
                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Telemetry Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Telemetry Graph" }, cancellationToken).ConfigureAwait(false);
                    tenantGuid = tenant.GUID;
                    graphGuid = graph.GUID;

                    Node ada = await client.Node.Create(new Node
                    {
                        TenantGUID = tenantGuid,
                        GraphGUID = graphGuid,
                        Name = "Ada"
                    }, cancellationToken).ConfigureAwait(false);
                    expectedNodeGuid = ada.GUID;

                    Node grace = await client.Node.Create(new Node
                    {
                        TenantGUID = tenantGuid,
                        GraphGUID = graphGuid,
                        Name = "Grace"
                    }, cancellationToken).ConfigureAwait(false);

                    await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenantGuid,
                        GraphGUID = graphGuid,
                        NodeGUID = ada.GUID,
                        Model = "touchstone-telemetry",
                        Content = "Ada vector",
                        Dimensionality = 4,
                        Vectors = embedding
                    }, cancellationToken).ConfigureAwait(false);

                    await client.Vector.Create(new VectorMetadata
                    {
                        TenantGUID = tenantGuid,
                        GraphGUID = graphGuid,
                        NodeGUID = grace.GUID,
                        Model = "touchstone-telemetry",
                        Content = "Grace vector",
                        Dimensionality = 4,
                        Vectors = BuildDeterministicVector(2, 4)
                    }, cancellationToken).ConfigureAwait(false);

                    await client.Graph.EnableVectorIndexing(
                        tenantGuid,
                        graphGuid,
                        new VectorIndexConfiguration
                        {
                            VectorIndexType = VectorIndexTypeEnum.HnswRam,
                            VectorDimensionality = 4,
                            VectorIndexThreshold = 1,
                            VectorIndexM = 8,
                            VectorIndexEf = 16,
                            VectorIndexEfConstruction = 32
                        },
                        cancellationToken).ConfigureAwait(false);

                    List<Activity> stoppedActivities = new List<Activity>();
                    object activityLock = new object();
                    using (ActivityListener listener = new ActivityListener
                    {
                        ShouldListenTo = source => source.Name == LiteGraphTelemetry.ActivitySourceName,
                        Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                        ActivityStopped = activity =>
                        {
                            lock (activityLock)
                            {
                                stoppedActivities.Add(activity);
                            }
                        }
                    })
                    {
                        ActivitySource.AddActivityListener(listener);

                        GraphQueryResult result = await client.Query.Execute(
                            tenantGuid,
                            graphGuid,
                            new GraphQueryRequest
                            {
                                Query = "CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 1",
                                Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    { "embedding", embedding }
                                }
                            },
                            cancellationToken).ConfigureAwait(false);

                        AssertEqual(1, result.RowCount, "Telemetry vector query row count");
                        AssertEqual(expectedNodeGuid, result.Nodes[0].GUID, "Telemetry vector query returns indexed node");
                    }

                    Activity query = FindRequiredActivity(stoppedActivities, LiteGraphTelemetry.QueryActivityName);
                    Activity parse = FindRequiredActivity(stoppedActivities, LiteGraphTelemetry.QueryParseActivityName);
                    Activity plan = FindRequiredActivity(stoppedActivities, LiteGraphTelemetry.QueryPlanActivityName);
                    Activity execute = FindRequiredActivity(stoppedActivities, LiteGraphTelemetry.QueryExecuteActivityName);
                    Activity vectorSearch = FindRequiredActivity(stoppedActivities, LiteGraphTelemetry.VectorSearchActivityName);
                    Activity vectorIndex = FindRequiredActivity(stoppedActivities, LiteGraphTelemetry.VectorIndexSearchActivityName);
                    List<Activity> repositoryActivities = stoppedActivities
                        .Where(activity => activity.DisplayName == LiteGraphTelemetry.RepositoryOperationActivityName)
                        .ToList();

                    AssertEqual(query.SpanId.ToString(), parse.ParentSpanId.ToString(), "Parse activity parent");
                    AssertEqual(query.SpanId.ToString(), plan.ParentSpanId.ToString(), "Plan activity parent");
                    AssertEqual(query.SpanId.ToString(), execute.ParentSpanId.ToString(), "Execute activity parent");
                    AssertEqual(execute.SpanId.ToString(), vectorSearch.ParentSpanId.ToString(), "Vector search activity parent");
                    AssertEqual(vectorSearch.SpanId.ToString(), vectorIndex.ParentSpanId.ToString(), "Vector index activity parent");
                    AssertEqual(GraphQueryKindEnum.VectorSearch.ToString(), GetActivityTag(query, "litegraph.query.kind")?.ToString(), "Query activity kind tag");
                    AssertEqual("true", GetActivityTag(query, "litegraph.query.uses_vector_search")?.ToString()?.ToLowerInvariant(), "Query activity vector-search tag");
                    AssertEqual("Node", GetActivityTag(vectorSearch, "litegraph.vector.domain")?.ToString(), "Vector search domain tag");
                    AssertEqual("true", GetActivityTag(vectorIndex, "litegraph.vector.index.used")?.ToString()?.ToLowerInvariant(), "Vector index used tag");
                    AssertEqual("1", GetActivityTag(vectorIndex, "litegraph.vector.index.results")?.ToString(), "Vector index result count tag");
                    AssertTrue(repositoryActivities.Count > 0, "Repository operation activities captured");
                    AssertTrue(repositoryActivities.All(activity => activity.TraceId == query.TraceId), "Repository operation activities share query trace");
                    AssertTrue(repositoryActivities.Any(activity => String.Equals(GetActivityTag(activity, "litegraph.repository.provider")?.ToString(), "Sqlite", StringComparison.Ordinal)), "Repository activity provider tag");
                    AssertTrue(repositoryActivities.Any(activity => String.Equals(GetActivityTag(activity, "litegraph.repository.success")?.ToString()?.ToLowerInvariant(), "true", StringComparison.Ordinal)), "Repository activity success tag");
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static Activity FindRequiredActivity(List<Activity> activities, string displayName)
        {
            foreach (Activity activity in activities)
            {
                if (activity.DisplayName == displayName) return activity;
            }

            throw new InvalidOperationException("Expected activity '" + displayName + "' was not captured.");
        }

        private static object? GetActivityTag(Activity activity, string name)
        {
            foreach (KeyValuePair<string, object?> tag in activity.TagObjects)
            {
                if (String.Equals(tag.Key, name, StringComparison.Ordinal)) return tag.Value;
            }

            return null;
        }

        private static Task TestObservabilityOtlpSettings(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ObservabilitySettings settings = new ObservabilitySettings();
            AssertFalse(settings.EnableOtlpExporter, "OTLP exporter is opt-in by default");
            AssertEqual("grpc", settings.OtlpProtocol, "Default OTLP protocol");
            AssertEqual(10000, settings.OtlpTimeoutMilliseconds, "Default OTLP timeout");

            settings.OtlpProtocol = "http/protobuf";
            AssertEqual("http/protobuf", settings.OtlpProtocol, "HTTP protobuf OTLP protocol");
            settings.OtlpProtocol = "http-protobuf";
            AssertEqual("http-protobuf", settings.OtlpProtocol, "HTTP protobuf alternate OTLP protocol");
            settings.OtlpTimeoutMilliseconds = 1;
            AssertEqual(1, settings.OtlpTimeoutMilliseconds, "Minimum OTLP timeout");
            settings.OtlpTimeoutMilliseconds = 600000;
            AssertEqual(600000, settings.OtlpTimeoutMilliseconds, "Maximum OTLP timeout");

            bool invalidProtocolRejected = false;
            try
            {
                settings.OtlpProtocol = "zipkin";
            }
            catch (ArgumentException)
            {
                invalidProtocolRejected = true;
            }

            AssertTrue(invalidProtocolRejected, "Invalid OTLP protocol rejected");

            bool invalidTimeoutRejected = false;
            try
            {
                settings.OtlpTimeoutMilliseconds = 0;
            }
            catch (ArgumentOutOfRangeException)
            {
                invalidTimeoutRejected = true;
            }

            AssertTrue(invalidTimeoutRejected, "Invalid OTLP timeout rejected");

            using (ObservabilityService observability = new ObservabilityService(new ObservabilitySettings
            {
                EnableOtlpExporter = true,
                OtlpEndpoint = "http://127.0.0.1:4318",
                OtlpProtocol = "http/protobuf",
                OtlpTimeoutMilliseconds = 1,
                ServiceName = "LiteGraph.Server.Tests." + Guid.NewGuid().ToString("N")
            }))
            {
                FieldInfo? tracerProviderField = typeof(ObservabilityService).GetField("_TracerProvider", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo? meterProviderField = typeof(ObservabilityService).GetField("_MeterProvider", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertNotNull(tracerProviderField?.GetValue(observability), "OTLP tracer provider initialized");
                AssertNotNull(meterProviderField?.GetValue(observability), "OTLP meter provider initialized");
            }

            return Task.CompletedTask;
        }

        private static async Task TestObservabilityQueryProfileTimings(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-query-profile-timings.db";
            DeleteFileIfExists(filename);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Filename = filename
                })))
                {
                    client.InitializeRepository();
                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Profile Tenant" }, cancellationToken).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Profile Graph" }, cancellationToken).ConfigureAwait(false);

                    GraphQueryResult nodeCreate = await client.Query.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new GraphQueryRequest
                        {
                            Query = "CREATE (n:Person { name: $name }) RETURN n",
                            Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "name", "Ada" }
                            },
                            IncludeProfile = true
                        },
                        cancellationToken).ConfigureAwait(false);

                    AssertNotNull(nodeCreate.ExecutionProfile, "Create query profile exists");
                    AssertProfileHasCoreTimings(nodeCreate.ExecutionProfile, "Create query");
                    AssertTrue(nodeCreate.ExecutionProfile.RepositoryOperationCount > 0, "Create query repository operation count");
                    AssertTrue(nodeCreate.ExecutionProfile.RepositoryTimeMs >= 0, "Create query repository time");
                    AssertTrue(nodeCreate.ExecutionProfile.TransactionTimeMs >= 0, "Create query transaction time");
                    AssertEqual(0, nodeCreate.ExecutionProfile.VectorSearchCount, "Create query vector search count");
                    AssertEqual(0, nodeCreate.ExecutionProfile.SerializationTimeMs, "Direct client profile has no serialization timing");
                    AssertEqual(0, nodeCreate.ExecutionProfile.AuthorizationTimeMs, "Direct client profile has no authorization timing");

                    Guid nodeGuid = nodeCreate.Nodes[0].GUID;
                    List<float> embedding = BuildDeterministicVector(0, 4);
                    GraphQueryResult vectorCreate = await client.Query.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new GraphQueryRequest
                        {
                            Query = "CREATE VECTOR v { nodeGuid: $nodeGuid, model: 'profile-model', embeddings: $embedding, content: 'Ada vector' } RETURN v",
                            Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "nodeGuid", nodeGuid },
                                { "embedding", embedding }
                            },
                            IncludeProfile = true
                        },
                        cancellationToken).ConfigureAwait(false);

                    AssertNotNull(vectorCreate.ExecutionProfile, "Vector create profile exists");
                    AssertTrue(vectorCreate.ExecutionProfile.RepositoryOperationCount > 0, "Vector create repository operation count");
                    AssertTrue(vectorCreate.ExecutionProfile.TransactionTimeMs >= 0, "Vector create transaction time");

                    GraphQueryResult vectorSearch = await client.Query.Execute(
                        tenant.GUID,
                        graph.GUID,
                        new GraphQueryRequest
                        {
                            Query = "CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 1",
                            Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "embedding", embedding }
                            },
                            IncludeProfile = true
                        },
                        cancellationToken).ConfigureAwait(false);

                    AssertNotNull(vectorSearch.ExecutionProfile, "Vector search profile exists");
                    AssertProfileHasCoreTimings(vectorSearch.ExecutionProfile, "Vector search query");
                    AssertEqual(1, vectorSearch.ExecutionProfile.VectorSearchCount, "Vector search count");
                    AssertTrue(vectorSearch.ExecutionProfile.VectorSearchTimeMs >= 0, "Vector search time");
                    AssertTrue(vectorSearch.ExecutionProfile.RepositoryOperationCount > 0, "Vector search repository operation count");
                    AssertTrue(vectorSearch.ExecutionProfile.RepositoryTimeMs >= 0, "Vector search repository time");
                    AssertEqual(0, vectorSearch.ExecutionProfile.TransactionTimeMs, "Read vector search has no mutation transaction timing");
                }
            }
            finally
            {
                DeleteFileIfExists(filename);
            }
        }

        private static async Task TestObservabilityRestQueryProfile(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

                string defaultGuid = Guid.Empty.ToString("D");
                string endpoint = _McpEnvironment.LiteGraphEndpoint + "/v1.0/tenants/" + defaultGuid + "/graphs/" + defaultGuid + "/query";
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", "Bearer default");
                request.Content = new StringContent(
                    _McpSerializer.SerializeJson(new GraphQueryRequest
                    {
                        Query = "MATCH (n) RETURN n LIMIT 1",
                        IncludeProfile = true
                    }, false),
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage response = await _ReadinessClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false);

                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                AssertEqual(200, (int)response.StatusCode, "REST query profile status");
                GraphQueryResult result = _McpSerializer.DeserializeJson<GraphQueryResult>(body);
                AssertNotNull(result.ExecutionProfile, "REST query profile exists");
                AssertProfileHasCoreTimings(result.ExecutionProfile, "REST query");
                AssertTrue(result.ExecutionProfile.AuthorizationTimeMs >= 0, "REST query authorization timing");
                AssertTrue(result.ExecutionProfile.SerializationTimeMs > 0, "REST query serialization timing");
                AssertTrue(result.ExecutionProfile.RepositoryOperationCount > 0, "REST query repository operation count");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static void AssertProfileHasCoreTimings(GraphQueryExecutionProfile profile, string label)
        {
            AssertNotNull(profile, label + " profile exists");
            AssertTrue(profile.ParseTimeMs >= 0, label + " parse timing");
            AssertTrue(profile.PlanTimeMs >= 0, label + " plan timing");
            AssertTrue(profile.ExecuteTimeMs >= 0, label + " execute timing");
            AssertTrue(profile.TotalTimeMs >= 0, label + " total timing");
        }

        private static async Task TestRequestHistoryCorrelation(CancellationToken cancellationToken)
        {
            string filename = "test-improvements-request-history-correlation.db";
            DeleteFileIfExists(filename);

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings { Filename = filename }))
            {
                repo.InitializeRepository();

                Guid requestGuid = Guid.NewGuid();
                Guid failureGuid = Guid.NewGuid();
                string requestId = "request-" + requestGuid.ToString("N");
                string correlationId = "correlation-" + Guid.NewGuid().ToString("N");
                string traceId = "0123456789abcdef0123456789abcdef";
                string transactionDiagnosticsJson = "{\"TransactionId\":\"11111111-1111-1111-1111-111111111111\",\"State\":\"RolledBack\",\"OperationCount\":2,\"IsolationLevel\":\"Serializable\",\"RetryCount\":0,\"ProviderErrorCode\":\"40001\"}";

                await repo.RequestHistory.Insert(new RequestHistoryDetail
                {
                    GUID = requestGuid,
                    RequestId = requestId,
                    CorrelationId = correlationId,
                    TraceId = traceId,
                    CreatedUtc = DateTime.UtcNow,
                    CompletedUtc = DateTime.UtcNow,
                    Method = "GET",
                    Path = "/v1.0/tenants",
                    Url = "/v1.0/tenants",
                    SourceIp = "127.0.0.1",
                    StatusCode = 200,
                    Success = true,
                    ProcessingTimeMs = 1.25,
                    TransactionDiagnosticsJson = transactionDiagnosticsJson,
                    RequestHeaders = new Dictionary<string, string>
                    {
                        { "x-request-id", requestId },
                        { "x-correlation-id", correlationId }
                    },
                    ResponseHeaders = new Dictionary<string, string>
                    {
                        { "x-request-id", requestId },
                        { "x-correlation-id", correlationId }
                    }
                }, cancellationToken).ConfigureAwait(false);

                await repo.RequestHistory.Insert(new RequestHistoryDetail
                {
                    GUID = failureGuid,
                    RequestId = "request-" + failureGuid.ToString("N"),
                    CorrelationId = "correlation-" + failureGuid.ToString("N"),
                    TraceId = "fedcba9876543210fedcba9876543210",
                    CreatedUtc = DateTime.UtcNow.AddSeconds(1),
                    CompletedUtc = DateTime.UtcNow.AddSeconds(1),
                    Method = "POST",
                    Path = "/v1.0/fail",
                    Url = "/v1.0/fail",
                    SourceIp = "127.0.0.1",
                    StatusCode = 500,
                    Success = false,
                    ProcessingTimeMs = 2.5
                }, cancellationToken).ConfigureAwait(false);

                RequestHistoryEntry entry = await repo.RequestHistory.ReadByGuid(requestGuid, cancellationToken).ConfigureAwait(false);
                AssertNotNull(entry, "Request history entry exists");
                AssertEqual(requestId, entry.RequestId, "Request history entry request ID");
                AssertEqual(correlationId, entry.CorrelationId, "Request history entry correlation ID");
                AssertEqual(traceId, entry.TraceId, "Request history entry trace ID");
                AssertEqual(transactionDiagnosticsJson, entry.TransactionDiagnosticsJson, "Request history entry transaction diagnostics");

                RequestHistoryDetail detail = await repo.RequestHistory.ReadDetailByGuid(requestGuid, cancellationToken).ConfigureAwait(false);
                AssertNotNull(detail, "Request history detail exists");
                AssertEqual(requestId, detail.RequestId, "Request history detail request ID");
                AssertEqual(correlationId, detail.ResponseHeaders["x-correlation-id"], "Request history response correlation header");
                AssertEqual(transactionDiagnosticsJson, detail.TransactionDiagnosticsJson, "Request history detail transaction diagnostics");

                RequestHistorySearchResult byCorrelation = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    CorrelationId = correlationId
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, byCorrelation.TotalCount, "Request history correlation search count");
                AssertEqual(requestGuid, byCorrelation.Objects[0].GUID, "Request history correlation search GUID");
                AssertEqual(transactionDiagnosticsJson, byCorrelation.Objects[0].TransactionDiagnosticsJson, "Request history search transaction diagnostics");

                RequestHistorySearchResult byTrace = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    TraceId = traceId
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, byTrace.TotalCount, "Request history trace search count");
                AssertEqual(requestGuid, byTrace.Objects[0].GUID, "Request history trace search GUID");

                RequestHistorySearchResult successes = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    Success = true
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, successes.TotalCount, "Request history success search count");
                AssertEqual(requestGuid, successes.Objects[0].GUID, "Request history success search GUID");

                RequestHistorySearchResult transactionRows = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    HasTransactionDiagnostics = true
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, transactionRows.TotalCount, "Request history transaction diagnostics search count");
                AssertEqual(requestGuid, transactionRows.Objects[0].GUID, "Request history transaction diagnostics search GUID");

                RequestHistorySearchResult nonTransactionRows = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    HasTransactionDiagnostics = false
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, nonTransactionRows.TotalCount, "Request history non-transaction diagnostics search count");
                AssertEqual(failureGuid, nonTransactionRows.Objects[0].GUID, "Request history non-transaction diagnostics search GUID");

                RequestHistorySearchResult byTransactionId = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    TransactionId = "11111111-1111"
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, byTransactionId.TotalCount, "Request history transaction ID search count");
                AssertEqual(requestGuid, byTransactionId.Objects[0].GUID, "Request history transaction ID search GUID");

                RequestHistorySearchResult failures = await repo.RequestHistory.Search(new RequestHistorySearchRequest
                {
                    Success = false
                }, cancellationToken).ConfigureAwait(false);

                AssertEqual(1L, failures.TotalCount, "Request history failure search count");
                AssertEqual(failureGuid, failures.Objects[0].GUID, "Request history failure search GUID");
            }

            DeleteFileIfExists(filename);
        }

        private static Task TestRequestHistoryRedaction(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string filename = "test-improvements-request-history-redaction.db";
            DeleteFileIfExists(filename);

            using (GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings { Filename = filename }))
            using (RequestHistoryService service = new RequestHistoryService(new Settings(), new LoggingModule(), repo))
            {
                NameValueCollection headers = new NameValueCollection(StringComparer.OrdinalIgnoreCase)
                {
                    { "Authorization", "Bearer secret" },
                    { "x-password", "password" },
                    { "x-request-id", "request-id" }
                };

                Dictionary<string, string> redacted = service.RedactHeaders(headers);
                AssertEqual(RequestHistoryService.RedactedValue, redacted["Authorization"], "Authorization header redacted");
                AssertEqual(RequestHistoryService.RedactedValue, redacted["x-password"], "Password header redacted");
                AssertEqual("request-id", redacted["x-request-id"], "Request ID header retained");

                byte[] body = Encoding.UTF8.GetBytes("abcdef");
                string captured = service.CaptureBody(body, 3, out bool truncated);
                AssertEqual("abc", captured, "Captured body is truncated to limit");
                AssertTrue(truncated, "Body truncation flag");

                string discarded = service.CaptureBody(body, 0, out bool discardedTruncated);
                AssertTrue(discarded == null, "Body capture with zero limit is discarded");
                AssertTrue(discardedTruncated, "Discarded body truncation flag");
            }

            DeleteFileIfExists(filename);
            return Task.CompletedTask;
        }

        private static Task TestOperationalLogRedaction(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string bearerToken = "secret-bearer-token";
            string password = "secret-password";
            string connectionString = "Host=prod;Username=litegraph;Password=secret";
            string url = "/v1.0/credentials/bearer/" + bearerToken
                + "?password=" + password
                + "&connectionString=" + Uri.EscapeDataString(connectionString)
                + "&embeddings=1,2,3"
                + "&safe=value";

            string redactedUrl = OperationalLogRedactor.RedactUrl(url);
            AssertFalse(redactedUrl.Contains(bearerToken), "Bearer token path segment redacted");
            AssertFalse(redactedUrl.Contains(password), "Password query value redacted");
            AssertFalse(redactedUrl.Contains("Host%3Dprod", StringComparison.OrdinalIgnoreCase), "Connection string query value redacted");
            AssertFalse(redactedUrl.Contains("1,2,3"), "Vector payload query value redacted");
            AssertTrue(redactedUrl.Contains("/v1.0/credentials/bearer/" + OperationalLogRedactor.RedactedValue), "Bearer token redaction marker present");
            AssertTrue(redactedUrl.Contains("password=" + OperationalLogRedactor.RedactedValue), "Password redaction marker present");
            AssertTrue(redactedUrl.Contains("connectionString=" + OperationalLogRedactor.RedactedValue), "Connection string redaction marker present");
            AssertTrue(redactedUrl.Contains("safe=value"), "Non-sensitive query value retained");

            NameValueCollection headers = new NameValueCollection(StringComparer.OrdinalIgnoreCase)
            {
                { "Authorization", "Bearer secret" },
                { "x-password", "password" },
                { "x-token", "token" },
                { "x-api-key", "api-key" },
                { "x-request-id", "request-id" }
            };

            Dictionary<string, string> redactedHeaders = OperationalLogRedactor.RedactHeaders(headers);
            AssertEqual(OperationalLogRedactor.RedactedValue, redactedHeaders["Authorization"], "Authorization header redacted for logs");
            AssertEqual(OperationalLogRedactor.RedactedValue, redactedHeaders["x-password"], "Password header redacted for logs");
            AssertEqual(OperationalLogRedactor.RedactedValue, redactedHeaders["x-token"], "Token header redacted for logs");
            AssertEqual(OperationalLogRedactor.RedactedValue, redactedHeaders["x-api-key"], "API key header redacted for logs");
            AssertEqual("request-id", redactedHeaders["x-request-id"], "Non-sensitive header retained for logs");
            AssertEqual(OperationalLogRedactor.RedactedValue, OperationalLogRedactor.RedactValue("sensitive"), "Scalar sensitive value redacted for logs");

            return Task.CompletedTask;
        }

        private static Task TestOperationalJsonLogFormatting(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string secret = "secret-token";
            string redactedUrl = OperationalLogRedactor.RedactUrl("/v1.0/credentials/bearer/" + secret + "?password=" + secret + "&safe=value");
            string json = OperationalLogFormatter.FormatRequestCompletion(
                "[LiteGraphServer] ",
                "GET",
                redactedUrl,
                401,
                12.5,
                "request-id",
                "correlation-id",
                "trace-id",
                "{\"error\":\"AuthorizationFailed\"}",
                true,
                true);

            AssertFalse(json.Contains(secret), "JSON operational log does not contain secret");
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                AssertEqual("http_request_completed", root.GetProperty("event").GetString(), "JSON log event");
                AssertEqual("LiteGraph.Server.REST", root.GetProperty("component").GetString(), "JSON log component");
                AssertEqual("GET", root.GetProperty("method").GetString(), "JSON log method");
                AssertEqual(401, root.GetProperty("statusCode").GetInt32(), "JSON log status");
                AssertEqual("request-id", root.GetProperty("requestId").GetString(), "JSON log request ID");
                AssertEqual("correlation-id", root.GetProperty("correlationId").GetString(), "JSON log correlation ID");
                AssertEqual("trace-id", root.GetProperty("traceId").GetString(), "JSON log trace ID");
                AssertTrue(root.GetProperty("url").GetString()?.Contains(OperationalLogRedactor.RedactedValue) == true, "JSON log uses redacted URL");
                AssertTrue(root.GetProperty("responseBody").GetString()?.Contains("AuthorizationFailed") == true, "JSON log includes response body when requested");
            }

            string jsonWithoutBody = OperationalLogFormatter.FormatRequestCompletion(
                "[LiteGraphServer] ",
                "GET",
                redactedUrl,
                200,
                1.0,
                "request-id",
                "correlation-id",
                null,
                "{\"ok\":true}",
                false,
                true);
            using (JsonDocument document = JsonDocument.Parse(jsonWithoutBody))
            {
                AssertFalse(document.RootElement.TryGetProperty("responseBody", out _), "JSON log omits response body by default");
            }

            string plain = OperationalLogFormatter.FormatRequestCompletion(
                "[LiteGraphServer] ",
                "GET",
                redactedUrl,
                200,
                1.0,
                "request-id",
                "correlation-id",
                "trace-id",
                null,
                false,
                false);
            AssertTrue(plain.Contains("requestId=request-id"), "Plain operational log remains compatible");
            AssertFalse(plain.Contains(secret), "Plain operational log does not contain secret");

            return Task.CompletedTask;
        }

        private static void CreateLegacyCredentialDatabase(
            string filename,
            Guid tenantGuid,
            Guid userGuid,
            Guid credentialGuid,
            string bearerToken,
            DateTime timestamp)
        {
            using (SqliteConnection conn = new SqliteConnection("Data Source=" + filename + ";Mode=ReadWriteCreate;Pooling=False;"))
            {
                conn.Open();

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "CREATE TABLE 'creds' ("
                        + "guid VARCHAR(64) NOT NULL UNIQUE, "
                        + "tenantguid VARCHAR(64) NOT NULL, "
                        + "userguid VARCHAR(64) NOT NULL, "
                        + "name VARCHAR(64), "
                        + "bearertoken VARCHAR(64), "
                        + "active INT, "
                        + "createdutc VARCHAR(64), "
                        + "lastupdateutc VARCHAR(64) "
                        + ");";
                    cmd.ExecuteNonQuery();
                }

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO 'creds' "
                        + "(guid, tenantguid, userguid, name, bearertoken, active, createdutc, lastupdateutc) "
                        + "VALUES (@guid, @tenantguid, @userguid, @name, @bearertoken, @active, @createdutc, @lastupdateutc);";
                    cmd.Parameters.AddWithValue("@guid", credentialGuid.ToString());
                    cmd.Parameters.AddWithValue("@tenantguid", tenantGuid.ToString());
                    cmd.Parameters.AddWithValue("@userguid", userGuid.ToString());
                    cmd.Parameters.AddWithValue("@name", "Legacy Credential");
                    cmd.Parameters.AddWithValue("@bearertoken", bearerToken);
                    cmd.Parameters.AddWithValue("@active", 1);
                    cmd.Parameters.AddWithValue("@createdutc", timestamp.ToUniversalTime().ToString("o"));
                    cmd.Parameters.AddWithValue("@lastupdateutc", timestamp.ToUniversalTime().ToString("o"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool SqliteTableExists(string filename, string tableName)
        {
            using (SqliteConnection conn = new SqliteConnection("Data Source=" + filename + ";Mode=ReadOnly;Pooling=False;"))
            {
                conn.Open();

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;";
                    cmd.Parameters.AddWithValue("@name", tableName);
                    object? scalar = cmd.ExecuteScalar();
                    long count = scalar != null && scalar != DBNull.Value ? Convert.ToInt64(scalar) : 0;
                    return count > 0;
                }
            }
        }

        private static bool SqliteColumnExists(string filename, string tableName, string columnName)
        {
            using (SqliteConnection conn = new SqliteConnection("Data Source=" + filename + ";Mode=ReadOnly;Pooling=False;"))
            {
                conn.Open();

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM pragma_table_info(@tableName);";
                    cmd.Parameters.AddWithValue("@tableName", tableName);

                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (String.Equals(reader.GetString(0), columnName, StringComparison.OrdinalIgnoreCase)) return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void DeleteFileIfExists(string filename)
        {
            SqliteConnection.ClearAllPools();

            DeleteFileWithRetry(filename);
            DeleteFileWithRetry(filename + ".backup");
            DeleteFileWithRetry(filename + "-shm");
            DeleteFileWithRetry(filename + "-wal");

            string indexDirectory = Path.Combine(Path.GetDirectoryName(filename) ?? ".", "indexes");
            if (!Directory.Exists(indexDirectory)) return;

            foreach (string file in Directory.GetFiles(indexDirectory, Path.GetFileNameWithoutExtension(filename) + "*"))
            {
                DeleteFileWithRetry(file);
            }
        }

        private static void DeleteFileWithRetry(string filename)
        {
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    if (File.Exists(filename)) File.Delete(filename);
                    return;
                }
                catch (IOException) when (i < 19)
                {
                    SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException) when (i < 19)
                {
                    SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(50);
                }
            }
        }

        private static void ForceSqliteTransientConnectionFailure(SqliteGraphRepository repo)
        {
            AssertNotNull(repo, "SQLite repository exists");

            FieldInfo? connectionStringField = typeof(SqliteGraphRepository).GetField("_ConnectionString", BindingFlags.Instance | BindingFlags.NonPublic);
            AssertNotNull(connectionStringField, "SQLite connection string field exists");

            string missingDirectory = Path.Combine(Path.GetTempPath(), "litegraph-missing-" + Guid.NewGuid().ToString("N"), "nested");
            string brokenConnectionString = "Data Source=" + Path.Combine(missingDirectory, "litegraph.db") + ";Pooling=true";
            connectionStringField!.SetValue(repo, brokenConnectionString);
        }
    }
}
