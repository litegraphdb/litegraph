namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Coordination;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Helpers;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Server.Classes;
    using Npgsql;
    using Touchstone.Core;

    /// <summary>
    /// Coverage for the 10.0 scale-out work: security token expiry, lock providers, cluster settings validation,
    /// caching disabled, SQLite index rebuild after restart, and pgvector storage, search, migration, and index lifecycle.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        private static readonly string _ScaleOutArtifactDirectory = Path.Combine(
            Path.GetTempPath(),
            "LiteGraph.Touchstone",
            "ScaleOut",
            Guid.NewGuid().ToString("N"));

        private static TestSuiteDescriptor CreateScaleOutSuite()
        {
            const string suiteId = "ScaleOut";
            string pg = PostgresqlTestConnectionStringEnvironmentVariable;

            return new TestSuiteDescriptor(
                suiteId: suiteId,
                displayName: "Scale-out: stateless nodes, locks, pgvector, and related fixes",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(suiteId, "TokenExpiry", "Security tokens are valid until their expiry and rejected after it", ExecuteScaleOutTokenExpiryAsync),
                    new TestCaseDescriptor(suiteId, "LocalLocks", "Local lock provider grants exclusively per key, times out, and signals release", ExecuteScaleOutLocalLocksAsync),
                    new TestCaseDescriptor(suiteId, "ClusterSettings", "Cluster and Clutch settings accept valid values and reject invalid ones", ExecuteScaleOutClusterSettingsAsync),
                    new TestCaseDescriptor(suiteId, "CachingDisabled", "A client with caching disabled completes a full create, read, update, delete pass", ExecuteScaleOutCachingDisabledAsync),
                    new TestCaseDescriptor(suiteId, "SqliteIndexRebuild", "An in-memory HnswLite index is rebuilt from SQLite after a restart", ExecuteScaleOutSqliteIndexRebuildAsync),
                    new TestCaseDescriptor(suiteId, "ClientAddress", "Forwarded client addresses are honored only from trusted proxies", ExecuteScaleOutClientAddressAsync),
                    new TestCaseDescriptor(suiteId, "RequestHistoryNode", "Request history records the handling node and filters by it", ExecuteScaleOutRequestHistoryNodeAsync),
                    new TestCaseDescriptor(suiteId, "SettingsFile", "Settings are read from the file and saves keep environment-supplied values out of it", ExecuteScaleOutSettingsFileAsync),
                    new TestCaseDescriptor(
                        suiteId: suiteId,
                        caseId: "ClusterRegistry",
                        displayName: "Nodes register in Redis, report state, and notice settings changes and restart requests",
                        executeAsync: ExecuteScaleOutClusterRegistryAsync,
                        skip: ShouldSkipProviderSuite(RedisTestConnectionStringEnvironmentVariable),
                        skipReason: "Redis registry tests require a Redis server. Set " + RedisTestConnectionStringEnvironmentVariable + " to run this case."),
                    new TestCaseDescriptor(
                        suiteId: suiteId,
                        caseId: "NodeRestart",
                        displayName: "A single node's restart reaches only that node, and a stopped node can be removed from the registry",
                        executeAsync: ExecuteScaleOutNodeRestartAsync,
                        skip: ShouldSkipProviderSuite(RedisTestConnectionStringEnvironmentVariable),
                        skipReason: "Node restart tests require a Redis server. Set " + RedisTestConnectionStringEnvironmentVariable + " to run this case."),
                    new TestCaseDescriptor(
                        suiteId: suiteId,
                        caseId: "RollingRestart",
                        displayName: "A cluster restart restarts nodes one at a time, each waiting for the previous one to return",
                        executeAsync: ExecuteScaleOutRollingRestartAsync,
                        skip: ShouldSkipProviderSuite(RedisTestConnectionStringEnvironmentVariable),
                        skipReason: "Rolling restart tests require a Redis server. Set " + RedisTestConnectionStringEnvironmentVariable + " to run this case."),
                    new TestCaseDescriptor(
                        suiteId: suiteId,
                        caseId: "PgvectorSearchParity",
                        displayName: "pgvector search matches exact results for every search type, indexed, unindexed, and filtered",
                        executeAsync: ExecuteScaleOutPgvectorSearchParityAsync,
                        skip: ShouldSkipProviderSuite(pg),
                        skipReason: ProviderSuiteSkipReason("pgvector search parity", pg)),
                    new TestCaseDescriptor(
                        suiteId: suiteId,
                        caseId: "PgvectorLegacyMigration",
                        displayName: "Legacy BYTEA embeddings convert to pgvector once and remain searchable",
                        executeAsync: ExecuteScaleOutPgvectorLegacyMigrationAsync,
                        skip: ShouldSkipProviderSuite(pg),
                        skipReason: ProviderSuiteSkipReason("pgvector legacy migration", pg)),
                    new TestCaseDescriptor(
                        suiteId: suiteId,
                        caseId: "PgvectorIndexLifecycle",
                        displayName: "pgvector index is created, shared per dimensionality, reported, and dropped when unused",
                        executeAsync: ExecuteScaleOutPgvectorIndexLifecycleAsync,
                        skip: ShouldSkipProviderSuite(pg),
                        skipReason: ProviderSuiteSkipReason("pgvector index lifecycle", pg))
                });
        }

        private static Task ExecuteScaleOutTokenExpiryAsync(CancellationToken token)
        {
            LiteGraph.Server.Classes.AuthenticationToken valid = new LiteGraph.Server.Classes.AuthenticationToken
            {
                TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
                ExpirationUtc = DateTime.UtcNow.AddMinutes(5)
            };
            AssertTrue(!valid.IsExpired, "Token within its lifetime is not expired");

            LiteGraph.Server.Classes.AuthenticationToken expired = new LiteGraph.Server.Classes.AuthenticationToken
            {
                TimestampUtc = DateTime.UtcNow.AddHours(-2),
                ExpirationUtc = DateTime.UtcNow.AddHours(-1)
            };
            AssertTrue(expired.IsExpired, "Token past its expiry is expired even though it was issued before expiring");
            return Task.CompletedTask;
        }

        private static async Task ExecuteScaleOutLocalLocksAsync(CancellationToken token)
        {
            using (LocalLockProvider provider = new LocalLockProvider())
            {
                AssertTrue(!provider.IsDistributed && provider.IsAvailable, "Local provider is in-process and available");

                ILockHandle first = await provider.AcquireAsync("schema", LockModeEnum.Exclusive, LockAcquireOptions.WaitUpTo(1000), token).ConfigureAwait(false);
                AssertTrue(first.IsHeld && !first.LostToken.IsCancellationRequested, "First acquire holds the lock");

                ILockHandle? denied = await provider.TryAcquireAsync("schema", LockModeEnum.Exclusive, token).ConfigureAwait(false);
                AssertTrue(denied == null, "TryAcquire on a held key returns null");

                ILockHandle? other = await provider.TryAcquireAsync("job/other", LockModeEnum.Exclusive, token).ConfigureAwait(false);
                AssertTrue(other != null && other.IsHeld, "A different key is not blocked");
                if (other != null) await other.DisposeAsync().ConfigureAwait(false);

                await AssertThrowsAsync<LockNotAcquiredException>(
                    async () => await provider.AcquireAsync("schema", LockModeEnum.Exclusive, LockAcquireOptions.WaitUpTo(200), token).ConfigureAwait(false),
                    "Waiting past the timeout throws LockNotAcquiredException");

                await AssertThrowsAsync<LockNotAcquiredException>(
                    async () => await provider.AcquireAsync("schema", LockModeEnum.Exclusive, LockAcquireOptions.FailFast(), token).ConfigureAwait(false),
                    "Fail-fast acquire on a held key throws LockNotAcquiredException");

                Task<ILockHandle> waiter = provider.AcquireAsync("schema", LockModeEnum.Exclusive, LockAcquireOptions.WaitUpTo(5000), token);
                await Task.Delay(100, token).ConfigureAwait(false);
                AssertTrue(!waiter.IsCompleted, "A waiter blocks while the lock is held");

                CancellationToken lost = first.LostToken;
                await first.DisposeAsync().ConfigureAwait(false);
                AssertTrue(!first.IsHeld && lost.IsCancellationRequested, "Release clears IsHeld and cancels LostToken");
                await first.DisposeAsync().ConfigureAwait(false);

                ILockHandle second = await waiter.ConfigureAwait(false);
                AssertTrue(second.IsHeld, "The waiter acquires after release");
                await second.DisposeAsync().ConfigureAwait(false);

                await AssertThrowsAsync<ArgumentNullException>(
                    async () => await provider.AcquireAsync("", LockModeEnum.Exclusive, null, token).ConfigureAwait(false),
                    "An empty key is rejected");
            }
        }

        private static Task ExecuteScaleOutClusterSettingsAsync(CancellationToken token)
        {
            ClusterSettings cluster = new ClusterSettings();
            AssertTrue(!cluster.Enable, "Cluster mode is off by default");
            AssertEqual("litegraph", cluster.ClusterName, "Default cluster name");
            AssertEqual(Environment.MachineName, cluster.ResolveNodeId(), "Node identifier defaults to the host name");
            cluster.NodeId = " litegraph-2 ";
            AssertEqual("litegraph-2", cluster.ResolveNodeId(), "Configured node identifier is trimmed");

            cluster.ClusterName = "prod-east-1";
            AssertEqual("prod-east-1", cluster.ClusterName, "Valid cluster name is accepted");
            AssertSyncThrows<ArgumentException>(() => cluster.ClusterName = "Prod East", "Cluster name with uppercase and spaces is rejected");
            AssertSyncThrows<ArgumentException>(() => cluster.ClusterName = new string('a', 65), "Cluster name over 64 characters is rejected");
            AssertSyncThrows<ArgumentOutOfRangeException>(() => cluster.EndpointResyncIntervalMs = 1000, "Resync interval below 5000 is rejected");

            ClutchSettings clutch = cluster.Clutch;
            AssertEqual("http://127.0.0.1:8090", clutch.Endpoint, "Default Clutch endpoint uses 127.0.0.1");
            clutch.Endpoint = "http://clutch-lb:8090/";
            AssertEqual("http://clutch-lb:8090", clutch.Endpoint, "Trailing slash is removed from the Clutch endpoint");
            AssertSyncThrows<ArgumentException>(() => clutch.Endpoint = "ftp://clutch", "Non-HTTP Clutch endpoint is rejected");
            AssertSyncThrows<ArgumentOutOfRangeException>(() => clutch.LeaseMs = 1000, "Lease below 5000 ms is rejected");
            AssertSyncThrows<ArgumentOutOfRangeException>(() => clutch.LeaseMs = 400000, "Lease above 300000 ms is rejected");

            cluster.Clutch = null;
            AssertNotNull(cluster.Clutch, "Null Clutch settings are replaced with defaults");
            return Task.CompletedTask;
        }

        private static async Task ExecuteScaleOutCachingDisabledAsync(CancellationToken token)
        {
            Directory.CreateDirectory(_ScaleOutArtifactDirectory);
            string databasePath = Path.Combine(_ScaleOutArtifactDirectory, "caching-disabled-" + Guid.NewGuid().ToString("N") + ".db");

            using (LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false), null, new CachingSettings { Enable = false }))
            {
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "No Cache Tenant" }, token).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "No Cache Graph" }, token).ConfigureAwait(false);
                Node a = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "a" }, token).ConfigureAwait(false);
                Node b = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "b" }, token).ConfigureAwait(false);
                Edge edge = await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = a.GUID, To = b.GUID, Name = "a-b" }, token).ConfigureAwait(false);

                a.Name = "a2";
                Node updated = await client.Node.Update(a, token).ConfigureAwait(false);
                AssertEqual("a2", updated.Name, "Update works with caching disabled");

                await client.Edge.DeleteByGuid(tenant.GUID, graph.GUID, edge.GUID, token).ConfigureAwait(false);
                await client.Node.DeleteByGuid(tenant.GUID, graph.GUID, b.GUID, token).ConfigureAwait(false);
                AssertTrue(!await client.Node.ExistsByGuid(tenant.GUID, b.GUID, token).ConfigureAwait(false), "Deleted node is gone");

                await AssertThrowsAsync<Exception>(
                    async () => await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = a.GUID, To = b.GUID, Name = "dangling" }, token).ConfigureAwait(false),
                    "Creating an edge to a deleted node is rejected");

                await client.Graph.DeleteByGuid(tenant.GUID, graph.GUID, true, token).ConfigureAwait(false);
                await client.Tenant.DeleteByGuid(tenant.GUID, true, token).ConfigureAwait(false);
            }
        }

        private static async Task ExecuteScaleOutSqliteIndexRebuildAsync(CancellationToken token)
        {
            Directory.CreateDirectory(_ScaleOutArtifactDirectory);
            string databasePath = Path.Combine(_ScaleOutArtifactDirectory, "index-rebuild-" + Guid.NewGuid().ToString("N") + ".db");
            List<List<float>> vectors = ScaleOutVectors(40, 8, 11, false);
            Guid tenantGuid;
            Guid graphGuid;

            using (LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false)))
            {
                client.InitializeRepository();
                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Rebuild Tenant" }, token).ConfigureAwait(false);
                Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Rebuild Graph" }, token).ConfigureAwait(false);
                tenantGuid = tenant.GUID;
                graphGuid = graph.GUID;

                await client.VectorIndex.EnableVectorIndex(tenantGuid, graphGuid, new VectorIndexConfiguration
                {
                    VectorIndexType = VectorIndexTypeEnum.HnswRam,
                    VectorDimensionality = 8
                }, token).ConfigureAwait(false);

                await ScaleOutCreateNodesAsync(client, tenantGuid, graphGuid, vectors, token).ConfigureAwait(false);
                List<string?> before = await ScaleOutSearchNamesAsync(client, tenantGuid, graphGuid, VectorSearchTypeEnum.CosineSimilarity, vectors[5], null, token).ConfigureAwait(false);
                AssertEqual("n5", before.FirstOrDefault(), "Exact match first before restart");
            }

            using (LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false)))
            {
                client.InitializeRepository();
                List<string?> after = await ScaleOutSearchNamesAsync(client, tenantGuid, graphGuid, VectorSearchTypeEnum.CosineSimilarity, vectors[5], null, token).ConfigureAwait(false);
                AssertTrue(after.Count > 0, "Indexed search after restart returns results instead of an empty list");
                AssertEqual("n5", after.FirstOrDefault(), "Exact match first after restart");
            }
        }

        private static async Task ExecuteScaleOutPgvectorSearchParityAsync(CancellationToken token)
        {
            string connectionString = Environment.GetEnvironmentVariable(PostgresqlTestConnectionStringEnvironmentVariable)!;
            string schema = "lg_parity_" + Guid.NewGuid().ToString("N").Substring(0, 12);

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = schema
                })))
                {
                    client.InitializeRepository();
                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Parity Tenant" }, token).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "Parity Graph" }, token).ConfigureAwait(false);

                    // Positive components keep every cosine similarity and dot product above zero, so default thresholds keep all rows.
                    List<List<float>> vectors = ScaleOutVectors(60, 8, 23, true);
                    await ScaleOutCreateNodesAsync(client, tenant.GUID, graph.GUID, vectors, token).ConfigureAwait(false);
                    List<float> query = vectors[17];

                    VectorSearchTypeEnum[] types =
                    {
                        VectorSearchTypeEnum.CosineSimilarity,
                        VectorSearchTypeEnum.CosineDistance,
                        VectorSearchTypeEnum.EuclidianSimilarity,
                        VectorSearchTypeEnum.EuclidianDistance,
                        VectorSearchTypeEnum.DotProduct
                    };

                    foreach (bool indexed in new[] { false, true })
                    {
                        if (indexed)
                        {
                            await client.VectorIndex.EnableVectorIndex(tenant.GUID, graph.GUID, new VectorIndexConfiguration
                            {
                                VectorIndexType = VectorIndexTypeEnum.HnswRam,
                                VectorDimensionality = 8
                            }, token).ConfigureAwait(false);
                        }

                        foreach (VectorSearchTypeEnum type in types)
                        {
                            List<VectorSearchResult> actual = await ScaleOutSearchAsync(client, tenant.GUID, graph.GUID, type, query, null, token).ConfigureAwait(false);
                            List<KeyValuePair<string, float>> expected = ScaleOutExpected(vectors, query, type, null).Take(5).ToList();
                            ScaleOutAssertParity(expected, actual, type, (indexed ? "indexed " : "unindexed ") + type);
                        }

                        List<VectorSearchResult> filtered = await ScaleOutSearchAsync(client, tenant.GUID, graph.GUID, VectorSearchTypeEnum.CosineSimilarity, query, new List<string> { "even" }, token).ConfigureAwait(false);
                        List<KeyValuePair<string, float>> expectedFiltered = ScaleOutExpected(vectors, query, VectorSearchTypeEnum.CosineSimilarity, "even").Take(5).ToList();
                        ScaleOutAssertParity(expectedFiltered, filtered, VectorSearchTypeEnum.CosineSimilarity, (indexed ? "indexed" : "unindexed") + " label-filtered CosineSimilarity");
                        AssertTrue(filtered.All(r => ScaleOutIndex(r.Node.Name) % 2 == 0), "Label filter returns only labeled nodes");
                    }

                    VectorIndexStatistics stats = await client.VectorIndex.GetStatistics(tenant.GUID, graph.GUID, token).ConfigureAwait(false);
                    AssertTrue(stats.IsLoaded && stats.IndexFile == "idx_vectors_hnsw_cosine_8" && stats.VectorCount == 60, "Statistics report the pgvector index and vector count");

                    List<VectorSearchResult> wrongDims = await ScaleOutSearchAsync(client, tenant.GUID, graph.GUID, VectorSearchTypeEnum.CosineSimilarity, new List<float> { 0.1f, 0.2f, 0.3f }, null, token).ConfigureAwait(false);
                    AssertEqual(0, wrongDims.Count, "A query with a different dimensionality matches nothing and does not fail");
                }
            }
            finally
            {
                await ScaleOutDropSchemaAsync(connectionString, schema, token).ConfigureAwait(false);
            }
        }

        private static async Task ExecuteScaleOutPgvectorLegacyMigrationAsync(CancellationToken token)
        {
            string connectionString = Environment.GetEnvironmentVariable(PostgresqlTestConnectionStringEnvironmentVariable)!;
            string schema = "lg_legacy_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            Guid tenantGuid = Guid.NewGuid();
            Guid graphGuid = Guid.NewGuid();
            List<List<float>> vectors = ScaleOutVectors(25, 6, 5, true);

            try
            {
                // Build the 9.x shape: vectors.embeddings as BYTEA holding little-endian float32 values.
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync(token).ConfigureAwait(false);
                    StringBuilder sql = new StringBuilder();
                    sql.Append("CREATE SCHEMA \"" + schema + "\"; ");
                    sql.Append("CREATE TABLE \"" + schema + "\".vectors (guid VARCHAR(64) PRIMARY KEY, tenantguid VARCHAR(64) NOT NULL, graphguid VARCHAR(64), nodeguid VARCHAR(64), edgeguid VARCHAR(64), model VARCHAR(256), dimensionality INT, content TEXT, embeddings BYTEA, createdutc VARCHAR(64), lastupdateutc VARCHAR(64)); ");
                    sql.Append("CREATE TABLE \"" + schema + "\".tenants (guid VARCHAR(64) PRIMARY KEY, name VARCHAR(128), active INT NOT NULL DEFAULT 1, createdutc VARCHAR(64), lastupdateutc VARCHAR(64)); ");
                    sql.Append("CREATE TABLE \"" + schema + "\".graphs (guid VARCHAR(64) PRIMARY KEY, tenantguid VARCHAR(64) NOT NULL, name VARCHAR(128), vectorindextype VARCHAR(16), vectorindexfile VARCHAR(256), vectorindexthreshold INT DEFAULT NULL, vectordimensionality INT DEFAULT NULL, vectorindexm INT DEFAULT NULL, vectorindexef INT DEFAULT NULL, vectorindexefconstruction INT DEFAULT NULL, vectorindexdirty INT NOT NULL DEFAULT 0, vectorindexdirtyutc VARCHAR(64), vectorindexdirtyreason TEXT, data TEXT, createdutc VARCHAR(64), lastupdateutc VARCHAR(64)); ");
                    sql.Append("CREATE TABLE \"" + schema + "\".nodes (guid VARCHAR(64) PRIMARY KEY, tenantguid VARCHAR(64) NOT NULL, graphguid VARCHAR(64) NOT NULL, name VARCHAR(128), data TEXT, createdutc VARCHAR(64), lastupdateutc VARCHAR(64)); ");
                    string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
                    sql.Append("INSERT INTO \"" + schema + "\".tenants (guid, name, createdutc, lastupdateutc) VALUES ('" + tenantGuid + "', 'Legacy Tenant', '" + now + "', '" + now + "'); ");
                    sql.Append("INSERT INTO \"" + schema + "\".graphs (guid, tenantguid, name, createdutc, lastupdateutc) VALUES ('" + graphGuid + "', '" + tenantGuid + "', 'Legacy Graph', '" + now + "', '" + now + "'); ");
                    for (int i = 0; i < vectors.Count; i++)
                    {
                        Guid nodeGuid = ScaleOutNodeGuid(i);
                        byte[] bytes = new byte[vectors[i].Count * 4];
                        for (int d = 0; d < vectors[i].Count; d++) Buffer.BlockCopy(BitConverter.GetBytes(vectors[i][d]), 0, bytes, d * 4, 4);
                        string hex = "'\\x" + Convert.ToHexString(bytes) + "'::bytea";
                        sql.Append("INSERT INTO \"" + schema + "\".nodes (guid, tenantguid, graphguid, name, createdutc, lastupdateutc) VALUES ('" + nodeGuid + "', '" + tenantGuid + "', '" + graphGuid + "', 'n" + i + "', '" + now + "', '" + now + "'); ");
                        sql.Append("INSERT INTO \"" + schema + "\".vectors (guid, tenantguid, graphguid, nodeguid, model, dimensionality, content, embeddings, createdutc, lastupdateutc) VALUES ('" + Guid.NewGuid() + "', '" + tenantGuid + "', '" + graphGuid + "', '" + nodeGuid + "', 'legacy', 6, 'n" + i + "', " + hex + ", '" + now + "', '" + now + "'); ");
                    }
                    sql.Append("INSERT INTO \"" + schema + "\".vectors (guid, tenantguid, graphguid, nodeguid, model, dimensionality, content, embeddings, createdutc, lastupdateutc) VALUES ('" + Guid.NewGuid() + "', '" + tenantGuid + "', '" + graphGuid + "', NULL, 'legacy', 6, 'corrupt', '\\x0102'::bytea, '" + now + "', '" + now + "'); ");

                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql.ToString(), conn))
                    {
                        await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                    }
                }

                DatabaseSettings settings = new DatabaseSettings { Type = DatabaseTypeEnum.Postgresql, ConnectionString = connectionString, Schema = schema };

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(settings)))
                {
                    client.InitializeRepository();

                    string? type = await ScaleOutScalarAsync(connectionString, "SELECT udt_name FROM information_schema.columns WHERE table_schema = '" + schema + "' AND table_name = 'vectors' AND column_name = 'embeddings'", token).ConfigureAwait(false);
                    AssertEqual("vector", type, "embeddings column converted to pgvector");

                    string? converted = await ScaleOutScalarAsync(connectionString, "SELECT COUNT(embeddings) FROM \"" + schema + "\".vectors", token).ConfigureAwait(false);
                    AssertEqual("25", converted, "All valid legacy vectors converted; the corrupt row is left empty");

                    string? migrations = await ScaleOutScalarAsync(connectionString, "SELECT string_agg(version::text, ',' ORDER BY version) FROM \"" + schema + "\".schemamigrations", token).ConfigureAwait(false);
                    AssertEqual("1,2", migrations, "Both migrations recorded");

                    VectorMetadata? sample = null;
                    await foreach (VectorMetadata v in client.Vector.ReadAllInGraph(tenantGuid, graphGuid, token: token).ConfigureAwait(false))
                    {
                        if (v.Content == "n3") sample = v;
                    }
                    AssertNotNull(sample, "Converted vector is readable");
                    AssertTrue(sample!.Vectors.Count == 6 && Math.Abs(sample.Vectors[2] - vectors[3][2]) < 1e-6, "Converted values are bit-exact floats");

                    List<string?> names = await ScaleOutSearchNamesAsync(client, tenantGuid, graphGuid, VectorSearchTypeEnum.CosineSimilarity, vectors[9], null, token).ConfigureAwait(false);
                    AssertEqual("n9", names.FirstOrDefault(), "Converted vectors are searchable");
                }

                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(settings)))
                {
                    client.InitializeRepository();
                    string? migrations = await ScaleOutScalarAsync(connectionString, "SELECT COUNT(*) FROM \"" + schema + "\".schemamigrations", token).ConfigureAwait(false);
                    AssertEqual("2", migrations, "A second start does not re-run migrations");
                }
            }
            finally
            {
                await ScaleOutDropSchemaAsync(connectionString, schema, token).ConfigureAwait(false);
            }
        }

        private static async Task ExecuteScaleOutPgvectorIndexLifecycleAsync(CancellationToken token)
        {
            string connectionString = Environment.GetEnvironmentVariable(PostgresqlTestConnectionStringEnvironmentVariable)!;
            string schema = "lg_idx_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            string indexExists = "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = '" + schema + "' AND indexname = 'idx_vectors_hnsw_cosine_12'";

            try
            {
                using (LiteGraphClient client = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = schema
                })))
                {
                    client.InitializeRepository();
                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "Index Tenant" }, token).ConfigureAwait(false);
                    Graph a = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "A" }, token).ConfigureAwait(false);
                    Graph b = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "B" }, token).ConfigureAwait(false);
                    VectorIndexConfiguration config = new VectorIndexConfiguration { VectorIndexType = VectorIndexTypeEnum.HnswSqlite, VectorDimensionality = 12 };

                    AssertEqual("0", await ScaleOutScalarAsync(connectionString, indexExists, token).ConfigureAwait(false), "No index before enabling");

                    await client.VectorIndex.EnableVectorIndex(tenant.GUID, a.GUID, config, token).ConfigureAwait(false);
                    await client.VectorIndex.EnableVectorIndex(tenant.GUID, b.GUID, config, token).ConfigureAwait(false);
                    AssertEqual("1", await ScaleOutScalarAsync(connectionString, indexExists, token).ConfigureAwait(false), "Two graphs with the same dimensionality share one index");

                    Graph readBack = await client.Graph.ReadByGuid(tenant.GUID, a.GUID, token: token).ConfigureAwait(false);
                    AssertTrue(readBack.VectorIndexFile == null, "Vector index file does not apply on PostgreSQL");

                    await client.VectorIndex.RebuildVectorIndex(tenant.GUID, a.GUID, token).ConfigureAwait(false);
                    VectorIndexStatistics stats = await client.VectorIndex.GetStatistics(tenant.GUID, a.GUID, token).ConfigureAwait(false);
                    AssertTrue(stats.IsLoaded, "Index is valid after a concurrent rebuild");

                    await client.VectorIndex.DeleteVectorIndex(tenant.GUID, a.GUID, false, token).ConfigureAwait(false);
                    AssertEqual("1", await ScaleOutScalarAsync(connectionString, indexExists, token).ConfigureAwait(false), "Index kept while another graph still uses it");

                    await client.VectorIndex.DeleteVectorIndex(tenant.GUID, b.GUID, false, token).ConfigureAwait(false);
                    AssertEqual("0", await ScaleOutScalarAsync(connectionString, indexExists, token).ConfigureAwait(false), "Index dropped when no graph uses it");

                    await AssertThrowsAsync<InvalidOperationException>(
                        async () => await client.VectorIndex.RebuildVectorIndex(tenant.GUID, a.GUID, token).ConfigureAwait(false),
                        "Rebuilding a graph without an index is rejected");
                }
            }
            finally
            {
                await ScaleOutDropSchemaAsync(connectionString, schema, token).ConfigureAwait(false);
            }
        }

        private static List<List<float>> ScaleOutVectors(int count, int dims, int seed, bool positive)
        {
            Random random = new Random(seed);
            List<List<float>> ret = new List<List<float>>();
            for (int i = 0; i < count; i++)
            {
                List<float> v = new List<float>();
                for (int d = 0; d < dims; d++)
                {
                    double x = random.NextDouble();
                    v.Add((float)Math.Round(positive ? x + 0.01 : (x * 2) - 1, 5));
                }
                ret.Add(v);
            }
            return ret;
        }

        private static Guid ScaleOutNodeGuid(int i)
        {
            return Guid.Parse("00000000-0000-0000-0000-" + (i + 1).ToString("D12", CultureInfo.InvariantCulture));
        }

        private static int ScaleOutIndex(string name)
        {
            return Int32.Parse(name.Substring(1), CultureInfo.InvariantCulture);
        }

        private static async Task ScaleOutCreateNodesAsync(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, List<List<float>> vectors, CancellationToken token)
        {
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < vectors.Count; i++)
            {
                nodes.Add(new Node
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "n" + i,
                    Labels = i % 2 == 0 ? new List<string> { "even" } : new List<string>(),
                    Vectors = new List<VectorMetadata>
                    {
                        new VectorMetadata
                        {
                            TenantGUID = tenantGuid,
                            GraphGUID = graphGuid,
                            Model = "test",
                            Dimensionality = vectors[i].Count,
                            Content = "n" + i,
                            Vectors = vectors[i]
                        }
                    }
                });
            }

            await client.Node.CreateMany(tenantGuid, graphGuid, nodes, token).ConfigureAwait(false);
        }

        private static async Task<List<VectorSearchResult>> ScaleOutSearchAsync(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, VectorSearchTypeEnum type, List<float> query, List<string>? labels, CancellationToken token)
        {
            VectorSearchRequest req = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = type,
                TopK = 5,
                MinimumScore = 0,
                MaximumDistance = 1000,
                MinimumInnerProduct = 0,
                Embeddings = query
            };
            if (labels != null) req.Labels = labels;

            List<VectorSearchResult> ret = new List<VectorSearchResult>();
            await foreach (VectorSearchResult r in client.Vector.Search(req, token).ConfigureAwait(false)) ret.Add(r);
            return ret;
        }

        private static async Task<List<string?>> ScaleOutSearchNamesAsync(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, VectorSearchTypeEnum type, List<float> query, List<string>? labels, CancellationToken token)
        {
            List<VectorSearchResult> results = await ScaleOutSearchAsync(client, tenantGuid, graphGuid, type, query, labels, token).ConfigureAwait(false);
            return results.Select(r => r.Node?.Name).ToList();
        }

        private static IEnumerable<KeyValuePair<string, float>> ScaleOutExpected(List<List<float>> vectors, List<float> query, VectorSearchTypeEnum type, string? label)
        {
            List<KeyValuePair<string, float>> rows = new List<KeyValuePair<string, float>>();
            for (int i = 0; i < vectors.Count; i++)
            {
                if (label == "even" && i % 2 != 0) continue;
                float value;
                switch (type)
                {
                    case VectorSearchTypeEnum.CosineSimilarity: value = VectorHelper.CalculateCosineSimilarity(query, vectors[i]); break;
                    case VectorSearchTypeEnum.CosineDistance: value = VectorHelper.CalculateCosineDistance(query, vectors[i]); break;
                    case VectorSearchTypeEnum.EuclidianSimilarity: value = VectorHelper.CalculateEuclidianSimilarity(query, vectors[i]); break;
                    case VectorSearchTypeEnum.EuclidianDistance: value = VectorHelper.CalculateEuclidianDistance(query, vectors[i]); break;
                    default: value = VectorHelper.CalculateInnerProduct(query, vectors[i]); break;
                }
                rows.Add(new KeyValuePair<string, float>("n" + i, value));
            }

            bool ascending = type == VectorSearchTypeEnum.CosineDistance || type == VectorSearchTypeEnum.EuclidianDistance;
            return ascending ? rows.OrderBy(r => r.Value) : rows.OrderByDescending(r => r.Value);
        }

        private static void ScaleOutAssertParity(List<KeyValuePair<string, float>> expected, List<VectorSearchResult> actual, VectorSearchTypeEnum type, string label)
        {
            AssertEqual(expected.Count, actual.Count, label + ": result count");
            for (int i = 0; i < expected.Count; i++)
            {
                VectorSearchResult r = actual[i];
                float value = (r.Score ?? r.Distance ?? r.InnerProduct) ?? Single.NaN;
                AssertEqual(expected[i].Key, r.Node?.Name, label + ": rank " + (i + 1) + " object");
                AssertTrue(Math.Abs(expected[i].Value - value) < 1e-4, label + ": rank " + (i + 1) + " value " + value.ToString("R", CultureInfo.InvariantCulture) + " vs expected " + expected[i].Value.ToString("R", CultureInfo.InvariantCulture));
            }

            switch (type)
            {
                case VectorSearchTypeEnum.CosineSimilarity:
                case VectorSearchTypeEnum.EuclidianSimilarity:
                    AssertTrue(actual.All(r => r.Score != null && r.Distance == null && r.InnerProduct == null), label + ": only Score is populated");
                    break;
                case VectorSearchTypeEnum.CosineDistance:
                case VectorSearchTypeEnum.EuclidianDistance:
                    AssertTrue(actual.All(r => r.Distance != null && r.Score == null && r.InnerProduct == null), label + ": only Distance is populated");
                    break;
                default:
                    AssertTrue(actual.All(r => r.InnerProduct != null && r.Score == null && r.Distance == null), label + ": only InnerProduct is populated");
                    break;
            }
        }

        private static async Task<string?> ScaleOutScalarAsync(string connectionString, string sql, CancellationToken token)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    object? result = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
                    return result == null || result == DBNull.Value ? null : Convert.ToString(result, CultureInfo.InvariantCulture);
                }
            }
        }

        private static async Task ScaleOutDropSchemaAsync(string connectionString, string schema, CancellationToken token)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync(token).ConfigureAwait(false);
                    using (NpgsqlCommand cmd = new NpgsqlCommand("DROP SCHEMA IF EXISTS \"" + schema + "\" CASCADE;", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                    }
                }
            }
            catch (NpgsqlException)
            {
            }
        }

        private static void AssertSyncThrows<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(message + " (expected " + typeof(TException).Name + " but got " + e.GetType().Name + ": " + e.Message + ")");
            }

            throw new InvalidOperationException(message + " (expected " + typeof(TException).Name + " but no exception was thrown)");
        }
    }
}
