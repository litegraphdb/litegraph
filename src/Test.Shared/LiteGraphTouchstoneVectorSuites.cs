namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        private static readonly string _VectorArtifactDirectory = Path.Combine(
            Path.GetTempPath(),
            "LiteGraph.Touchstone",
            "Vector",
            Guid.NewGuid().ToString("N"));

        private static TestSuiteDescriptor CreateVectorSearchSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Vector.Search",
                displayName: "Deterministic vector search coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Vector.Search",
                        caseId: "CosineSimilarity",
                        displayName: "Cosine similarity returns the exact matching node first",
                        executeAsync: ExecuteVectorSearchSuiteAsync)
                });
        }

        private static TestSuiteDescriptor CreateVectorIndexImplementationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Vector.Index.Implementation",
                displayName: "Vector index configuration and lifecycle coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "Configuration",
                        displayName: "Graph configuration round-trips through VectorIndexConfiguration",
                        executeAsync: ExecuteVectorIndexConfigurationSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "Metadata",
                        displayName: "HNSW index entries carry LiteGraph object metadata",
                        executeAsync: ExecuteVectorIndexMetadataSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "SqlitePersistence",
                        displayName: "HNSW SQLite index persists searchable graph topology across reload",
                        executeAsync: ExecuteVectorIndexPersistenceSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "LegacySqliteArtifact",
                        displayName: "Legacy HNSW SQLite artifacts are marked dirty and bypassed",
                        executeAsync: ExecuteLegacyVectorIndexArtifactSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "Lifecycle",
                        displayName: "HNSW RAM index supports enable, search, add, and remove flows",
                        executeAsync: ExecuteVectorIndexLifecycleSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "DirtyRepair",
                        displayName: "Vector index dirty state protects search and clears after rebuild",
                        executeAsync: ExecuteVectorIndexDirtyRepairSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "TransactionRollbackStaging",
                        displayName: "Graph transaction rollback discards staged vector index mutations",
                        executeAsync: ExecuteVectorIndexTransactionRollbackDirtySuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "ConcurrentTransactionStaging",
                        displayName: "Concurrent graph transactions preserve staged vector index correctness",
                        executeAsync: ExecuteVectorIndexConcurrentTransactionSuiteAsync)
                });
        }

        private static TestSuiteDescriptor CreateVectorIndexSearchSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Vector.Index.Search",
                displayName: "HNSW RAM and SQLite search parity coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Search",
                        caseId: "RamVsSqlite",
                        displayName: "HNSW RAM and SQLite indices both return deterministic matches",
                        executeAsync: ExecuteVectorIndexSearchSuiteAsync)
                });
        }

        private static async Task ExecuteVectorSearchSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 16;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-search.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Search Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Search Graph"
                }, cancellationToken).ConfigureAwait(false);

                List<Node> nodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: 12,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                List<VectorSearchResult> results = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 5,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(results, nodes[0].GUID, "Cosine similarity");
                AssertTrue(results.Count <= 5, "Cosine similarity should respect TopK");
                AssertTrue(results[0].Score.HasValue, "Cosine similarity top result score");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static Task ExecuteVectorIndexConfigurationSuiteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Graph graphWithoutIndex = new Graph
            {
                GUID = Guid.NewGuid(),
                TenantGUID = Guid.NewGuid(),
                Name = "Graph Without Indexing",
                VectorIndexType = null,
                VectorIndexFile = null,
                VectorIndexThreshold = null,
                VectorDimensionality = null,
                VectorIndexM = null,
                VectorIndexEf = null,
                VectorIndexEfConstruction = null
            };

            VectorIndexConfiguration graphConfig = new VectorIndexConfiguration(graphWithoutIndex);
            AssertEqual(VectorIndexTypeEnum.None, graphConfig.VectorIndexType, "Graph constructor should map missing index type to None");
            AssertEqual(null, graphConfig.VectorIndexFile, "Graph constructor should preserve null index file");
            AssertEqual(null, graphConfig.VectorIndexThreshold, "Graph constructor should preserve null threshold");
            AssertEqual(null, graphConfig.VectorDimensionality, "Graph constructor should preserve null dimensionality");
            AssertEqual(16, graphConfig.VectorIndexM, "Graph constructor should default VectorIndexM");
            AssertEqual(50, graphConfig.VectorIndexEf, "Graph constructor should default VectorIndexEf");
            AssertEqual(200, graphConfig.VectorIndexEfConstruction, "Graph constructor should default VectorIndexEfConstruction");

            string json = @"{
  ""VectorIndexType"": ""HnswSqlite"",
  ""VectorIndexFile"": ""graph-index.sqlite"",
  ""VectorIndexThreshold"": 4,
  ""VectorDimensionality"": 32,
  ""VectorIndexM"": 12,
  ""VectorIndexEf"": 48,
  ""VectorIndexEfConstruction"": 96
}";

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
            };

            VectorIndexConfiguration? deserialized = JsonSerializer.Deserialize<VectorIndexConfiguration>(json, options);
            AssertNotNull(deserialized, "JSON configuration");
            AssertEqual(VectorIndexTypeEnum.HnswSqlite, deserialized!.VectorIndexType, "JSON should deserialize HnswSqlite");
            AssertEqual("graph-index.sqlite", deserialized.VectorIndexFile, "JSON should preserve VectorIndexFile");
            AssertEqual(4, deserialized.VectorIndexThreshold, "JSON should preserve VectorIndexThreshold");
            AssertEqual(32, deserialized.VectorDimensionality, "JSON should preserve VectorDimensionality");
            AssertEqual(12, deserialized.VectorIndexM, "JSON should preserve VectorIndexM");
            AssertEqual(48, deserialized.VectorIndexEf, "JSON should preserve VectorIndexEf");
            AssertEqual(96, deserialized.VectorIndexEfConstruction, "JSON should preserve VectorIndexEfConstruction");

            return Task.CompletedTask;
        }

        private static async Task ExecuteVectorIndexMetadataSuiteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Guid tenantGuid = Guid.NewGuid();
            Guid graphGuid = Guid.NewGuid();
            Guid nodeGuid = Guid.NewGuid();
            Guid edgeGuid = Guid.NewGuid();
            Guid toNodeGuid = Guid.NewGuid();

            Graph graph = new Graph
            {
                TenantGUID = tenantGuid,
                GUID = graphGuid,
                Name = "Metadata Graph",
                Labels = new List<string> { "graph-label" },
                Tags = new NameValueCollection { { "graph-key", "graph-value" } },
                VectorIndexType = VectorIndexTypeEnum.HnswRam,
                VectorDimensionality = 3,
                VectorIndexM = 8,
                VectorIndexEf = 16,
                VectorIndexEfConstruction = 32
            };

            Node node = new Node
            {
                TenantGUID = tenantGuid,
                GUID = nodeGuid,
                GraphGUID = graphGuid,
                Name = "Metadata Node",
                Labels = new List<string> { "node-label", "shared-label" },
                Tags = new NameValueCollection { { "node-key", "node-value" } }
            };

            Edge edge = new Edge
            {
                TenantGUID = tenantGuid,
                GUID = edgeGuid,
                GraphGUID = graphGuid,
                Name = "Metadata Edge",
                From = nodeGuid,
                To = toNodeGuid,
                Labels = new List<string> { "edge-label" },
                Tags = new NameValueCollection { { "edge-key", "edge-value" } }
            };

            VectorMetadata graphVector = CreateVectorMetadata(tenantGuid, graphGuid, null, null, "graph vector", 0);
            VectorMetadata nodeVector = CreateVectorMetadata(tenantGuid, graphGuid, nodeGuid, null, "node vector", 1);
            VectorMetadata edgeVector = CreateVectorMetadata(tenantGuid, graphGuid, null, edgeGuid, "edge vector", 2);

            VectorIndexEntry graphEntry = VectorIndexEntry.FromVectorMetadata(graphVector, graph);
            VectorIndexEntry nodeEntry = VectorIndexEntry.FromVectorMetadata(nodeVector, graph, node);
            VectorIndexEntry edgeEntry = VectorIndexEntry.FromVectorMetadata(edgeVector, graph, null, edge);

            AssertEqual(graphGuid, graphEntry.Id, "Graph vector index ID");
            AssertEqual("Metadata Graph", graphEntry.Name, "Graph vector index name");
            AssertLabelsContain(graphEntry.Labels, "graph-label", "Graph vector labels");
            AssertTagEquals(graphEntry.Tags, "graph-key", "graph-value", "Graph vector object tag");
            AssertTagEquals(graphEntry.Tags, "litegraph.domain", "Graph", "Graph vector domain tag");

            AssertEqual(nodeGuid, nodeEntry.Id, "Node vector index ID");
            AssertEqual("Metadata Node", nodeEntry.Name, "Node vector index name");
            AssertLabelsContain(nodeEntry.Labels, "node-label", "Node vector labels");
            AssertTagEquals(nodeEntry.Tags, "node-key", "node-value", "Node vector object tag");
            AssertTagEquals(nodeEntry.Tags, "litegraph.domain", "Node", "Node vector domain tag");
            AssertTagEquals(nodeEntry.Tags, "litegraph.graph_name", "Metadata Graph", "Node vector graph name tag");
            AssertTagEquals(nodeEntry.Tags, "litegraph.node_guid", nodeGuid.ToString("D"), "Node vector node GUID tag");
            AssertTagEquals(nodeEntry.Tags, "litegraph.vector_guid", nodeVector.GUID.ToString("D"), "Node vector vector GUID tag");

            AssertEqual(edgeGuid, edgeEntry.Id, "Edge vector index ID");
            AssertEqual("Metadata Edge", edgeEntry.Name, "Edge vector index name");
            AssertLabelsContain(edgeEntry.Labels, "edge-label", "Edge vector labels");
            AssertTagEquals(edgeEntry.Tags, "edge-key", "edge-value", "Edge vector object tag");
            AssertTagEquals(edgeEntry.Tags, "litegraph.domain", "Edge", "Edge vector domain tag");
            AssertTagEquals(edgeEntry.Tags, "litegraph.edge_from", nodeGuid.ToString("D"), "Edge vector from tag");
            AssertTagEquals(edgeEntry.Tags, "litegraph.edge_to", toNodeGuid.ToString("D"), "Edge vector to tag");

            HnswLiteVectorIndex ramIndex = new HnswLiteVectorIndex();
            try
            {
                await ramIndex.InitializeAsync(graph, cancellationToken).ConfigureAwait(false);
                await ramIndex.AddAsync(nodeEntry, cancellationToken).ConfigureAwait(false);

                object indexedNode = await GetIndexedNodeAsync(ramIndex, nodeGuid, cancellationToken).ConfigureAwait(false);
                AssertEqual("Metadata Node", GetIndexedNodeProperty<string>(indexedNode, "Name"), "RAM HNSW node metadata name");
                AssertLabelsContain(GetIndexedNodeProperty<List<string>>(indexedNode, "Labels"), "node-label", "RAM HNSW node metadata labels");
                AssertTagEquals(GetIndexedNodeProperty<Dictionary<string, object>>(indexedNode, "Tags"), "node-key", "node-value", "RAM HNSW node metadata tags");

                Guid batchNodeGuid = Guid.NewGuid();
                Node batchNode = new Node
                {
                    TenantGUID = tenantGuid,
                    GUID = batchNodeGuid,
                    GraphGUID = graphGuid,
                    Name = "Metadata Batch Node",
                    Labels = new List<string> { "batch-label" },
                    Tags = new NameValueCollection { { "batch-key", "batch-value" } }
                };
                VectorMetadata batchVector = CreateVectorMetadata(tenantGuid, graphGuid, batchNodeGuid, null, "batch vector", 2);
                VectorIndexEntry batchEntry = VectorIndexEntry.FromVectorMetadata(batchVector, graph, batchNode);

                await ramIndex.AddBatchAsync(new List<VectorIndexEntry> { batchEntry }, cancellationToken).ConfigureAwait(false);
                object indexedBatchNode = await GetIndexedNodeAsync(ramIndex, batchNodeGuid, cancellationToken).ConfigureAwait(false);
                AssertEqual("Metadata Batch Node", GetIndexedNodeProperty<string>(indexedBatchNode, "Name"), "RAM HNSW batch metadata name");
                AssertLabelsContain(GetIndexedNodeProperty<List<string>>(indexedBatchNode, "Labels"), "batch-label", "RAM HNSW batch metadata labels");
                AssertTagEquals(GetIndexedNodeProperty<Dictionary<string, object>>(indexedBatchNode, "Tags"), "batch-key", "batch-value", "RAM HNSW batch metadata tags");
            }
            finally
            {
                ramIndex.Dispose();
            }

            string sqliteIndexPath = BuildVectorArtifactPath("touchstone-vector-index-metadata.sqlite");
            CleanupSqliteArtifacts(sqliteIndexPath, sqliteIndexPath + ".layers");

            HnswLiteVectorIndex sqliteIndex = new HnswLiteVectorIndex();
            try
            {
                Graph sqliteGraph = new Graph
                {
                    TenantGUID = tenantGuid,
                    GUID = graphGuid,
                    Name = "Metadata Graph",
                    VectorIndexType = VectorIndexTypeEnum.HnswSqlite,
                    VectorIndexFile = sqliteIndexPath,
                    VectorDimensionality = 3,
                    VectorIndexM = 8,
                    VectorIndexEf = 16,
                    VectorIndexEfConstruction = 32
                };

                await sqliteIndex.InitializeAsync(sqliteGraph, cancellationToken).ConfigureAwait(false);
                await sqliteIndex.AddAsync(nodeEntry, cancellationToken).ConfigureAwait(false);
                await sqliteIndex.SaveAsync(cancellationToken).ConfigureAwait(false);
                sqliteIndex.Dispose();

                string indexJson = File.ReadAllText(sqliteIndexPath);
                HnswIndexState state = JsonSerializer.Deserialize<HnswIndexState>(indexJson)!;
                HnswNodeState savedNode = state.Node.Single(saved => saved.Id == nodeGuid);

                AssertEqual("Metadata Node", savedNode.Name, "SQLite HNSW metadata name");
                AssertLabelsContain(savedNode.Labels, "node-label", "SQLite HNSW metadata labels");
                AssertTagEquals(savedNode.Tags, "node-key", "node-value", "SQLite HNSW metadata owner tag");
                AssertTagEquals(savedNode.Tags, "litegraph.domain", "Node", "SQLite HNSW metadata domain tag");
            }
            finally
            {
                sqliteIndex.Dispose();
                CleanupSqliteArtifacts(sqliteIndexPath, sqliteIndexPath + ".layers");
            }
        }

        private static async Task ExecuteVectorIndexPersistenceSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 16;
            const int vectorCount = 12;
            string sqliteIndexPath = BuildVectorArtifactPath("touchstone-vector-index-persistence.sqlite");

            CleanupSqliteArtifacts(sqliteIndexPath, sqliteIndexPath + ".layers");

            Guid tenantGuid = Guid.NewGuid();
            Graph graph = new Graph
            {
                TenantGUID = tenantGuid,
                GUID = Guid.NewGuid(),
                Name = "Persistent SQLite HNSW Graph",
                VectorIndexType = VectorIndexTypeEnum.HnswSqlite,
                VectorIndexFile = sqliteIndexPath,
                VectorDimensionality = dimensionality,
                VectorIndexM = 8,
                VectorIndexEf = 32,
                VectorIndexEfConstruction = 64
            };

            List<VectorIndexEntry> entries = new List<VectorIndexEntry>();
            for (int i = 0; i < vectorCount; i++)
            {
                Guid nodeGuid = Guid.NewGuid();
                entries.Add(new VectorIndexEntry
                {
                    Id = nodeGuid,
                    Vector = BuildDeterministicVector(i, dimensionality),
                    Name = "Persistent Vector Node " + i,
                    Labels = new List<string> { "persistent", "node-" + i },
                    Tags = new Dictionary<string, object>
                    {
                        { "litegraph.domain", "Node" },
                        { "litegraph.node_guid", nodeGuid.ToString("D") }
                    }
                });
            }

            Guid expectedTopId = entries[0].Id;
            HnswLiteVectorIndex writer = new HnswLiteVectorIndex();
            HnswLiteVectorIndex reader = new HnswLiteVectorIndex();

            try
            {
                await writer.InitializeAsync(graph, cancellationToken).ConfigureAwait(false);
                await writer.AddBatchAsync(entries, cancellationToken).ConfigureAwait(false);
                await writer.SaveAsync(cancellationToken).ConfigureAwait(false);

                List<VectorDistanceResult> preReloadResults = await writer.SearchAsync(
                    BuildDeterministicVector(0, dimensionality),
                    3,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                AssertTrue(preReloadResults.Count > 0, "SQLite HNSW pre-reload search should return results");
                AssertEqual(expectedTopId, preReloadResults[0].Id, "SQLite HNSW pre-reload top result");
                writer.Dispose();

                AssertTrue(File.Exists(sqliteIndexPath), "SQLite HNSW state file should exist");

                HnswIndexState state = JsonSerializer.Deserialize<HnswIndexState>(File.ReadAllText(sqliteIndexPath))!;
                AssertEqual(HnswIndexState.CurrentFormatVersion, state.FormatVersion, "SQLite HNSW persisted format version");
                AssertEqual("2.0.1", state.HnswLiteVersion, "SQLite HNSW persisted HnswLite version");
                AssertTrue(
                    state.Node.Sum(nodeState => nodeState.Connections?.Values.Sum(neighbors => neighbors?.Count ?? 0) ?? 0) > 0,
                    "SQLite HNSW state should persist neighbor connections");

                await reader.InitializeAsync(graph, cancellationToken).ConfigureAwait(false);
                List<VectorDistanceResult> postReloadResults = await reader.SearchAsync(
                    BuildDeterministicVector(0, dimensionality),
                    3,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                AssertTrue(postReloadResults.Count > 0, "SQLite HNSW post-reload search should return results");
                AssertEqual(expectedTopId, postReloadResults[0].Id, "SQLite HNSW post-reload top result");
            }
            finally
            {
                writer.Dispose();
                reader.Dispose();
                CleanupSqliteArtifacts(sqliteIndexPath, sqliteIndexPath + ".layers");
            }
        }

        private static async Task ExecuteLegacyVectorIndexArtifactSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 8;
            const int nodeCount = 6;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-index-legacy-artifact.db");
            string sqliteIndexPath = BuildVectorArtifactPath("touchstone-vector-index-legacy-artifact.sqlite");

            CleanupSqliteArtifacts(databasePath, sqliteIndexPath, sqliteIndexPath + ".layers");

            LiteGraphClient? writer = null;
            LiteGraphClient? reader = null;

            try
            {
                writer = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                writer.InitializeRepository();

                TenantMetadata tenant = await writer.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Legacy HNSW Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await writer.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Legacy HNSW Graph"
                }, cancellationToken).ConfigureAwait(false);

                List<Node> nodes = await CreateDeterministicNodesWithVectorsAsync(
                    writer,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: nodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                await writer.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    new VectorIndexConfiguration
                    {
                        VectorIndexType = VectorIndexTypeEnum.HnswSqlite,
                        VectorIndexFile = sqliteIndexPath,
                        VectorDimensionality = dimensionality,
                        VectorIndexThreshold = 1,
                        VectorIndexM = 8,
                        VectorIndexEf = 32,
                        VectorIndexEfConstruction = 64
                    },
                    cancellationToken).ConfigureAwait(false);

                writer.Dispose();
                writer = null;

                RewriteHnswStateAsLegacyArtifact(sqliteIndexPath);

                reader = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                reader.InitializeRepository();

                List<VectorSearchResult> results = await SearchVectorsAsync(
                    reader,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(results, nodes[0].GUID, "Legacy HNSW artifact fallback search");

                Graph dirtyGraph = await reader.Graph.ReadByGuid(tenant.GUID, graph.GUID, token: cancellationToken).ConfigureAwait(false);
                AssertTrue(dirtyGraph.VectorIndexDirty, "Legacy HNSW artifact should mark graph dirty");
                AssertTrue(
                    dirtyGraph.VectorIndexDirtyReason != null
                    && dirtyGraph.VectorIndexDirtyReason.Contains("HnswLite 2.0.1", StringComparison.Ordinal),
                    "Legacy HNSW dirty reason should name HnswLite 2.0.1");

                VectorIndexStatistics? dirtyStats = await reader.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(dirtyStats, "Legacy HNSW dirty statistics");
                AssertTrue(dirtyStats!.IsDirty, "Legacy HNSW statistics should expose dirty state");
            }
            finally
            {
                writer?.Dispose();
                reader?.Dispose();
                CleanupSqliteArtifacts(databasePath, sqliteIndexPath, sqliteIndexPath + ".layers");
            }
        }

        private static async Task ExecuteVectorIndexLifecycleSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 64;
            const int initialNodeCount = 24;
            const int additionalNodeCount = 12;
            const int removalCount = 6;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-index-lifecycle.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Index Lifecycle Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Index Lifecycle Graph"
                }, cancellationToken).ConfigureAwait(false);

                List<Node> initialNodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: initialNodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                VectorIndexConfiguration configuration = new VectorIndexConfiguration
                {
                    VectorIndexType = VectorIndexTypeEnum.HnswRam,
                    VectorDimensionality = dimensionality,
                    VectorIndexThreshold = 1,
                    VectorIndexM = 8,
                    VectorIndexEf = 32,
                    VectorIndexEfConstruction = 64
                };

                await client.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    configuration,
                    cancellationToken).ConfigureAwait(false);

                VectorIndexStatistics? initialStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(initialStats, "Initial vector index statistics");
                AssertEqual(VectorIndexTypeEnum.HnswRam, initialStats!.IndexType, "Initial vector index type");
                AssertEqual(initialNodeCount, initialStats.VectorCount, "Initial indexed vector count");
                AssertEqual(dimensionality, initialStats.Dimensions, "Initial vector dimensionality");
                AssertTrue(initialStats.IsLoaded, "Initial HNSW RAM index should be loaded");

                List<VectorSearchResult> cosineResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(cosineResults, initialNodes[0].GUID, "HNSW RAM cosine similarity");

                List<Node> additionalNodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: initialNodeCount,
                    count: additionalNodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                client.Flush();

                VectorIndexStatistics? postInsertStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(postInsertStats, "Post-insert vector index statistics");
                AssertEqual(initialNodeCount + additionalNodeCount, postInsertStats!.VectorCount, "Indexed vector count after insert");

                List<VectorSearchResult> dotProductResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(initialNodeCount, dimensionality),
                    VectorSearchTypeEnum.DotProduct,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(dotProductResults, additionalNodes[0].GUID, "HNSW RAM dot product");

                for (int i = 0; i < removalCount; i++)
                {
                    await client.Node.DeleteByGuid(
                        tenant.GUID,
                        graph.GUID,
                        initialNodes[i].GUID,
                        cancellationToken).ConfigureAwait(false);
                }

                client.Flush();

                VectorIndexStatistics? postDeleteStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(postDeleteStats, "Post-delete vector index statistics");
                AssertEqual(initialNodeCount + additionalNodeCount - removalCount, postDeleteStats!.VectorCount, "Indexed vector count after delete");

                List<VectorSearchResult> euclidianResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(initialNodeCount + 1, dimensionality),
                    VectorSearchTypeEnum.EuclidianDistance,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(euclidianResults, additionalNodes[1].GUID, "HNSW RAM euclidian distance");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static async Task ExecuteVectorIndexDirtyRepairSuiteAsync(CancellationToken cancellationToken)
        {
            const int indexDimensionality = 3;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-index-dirty-repair.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Index Dirty Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Index Dirty Graph"
                }, cancellationToken).ConfigureAwait(false);

                Node node = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Dirty Index Node"
                }, cancellationToken).ConfigureAwait(false);

                await client.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    new VectorIndexConfiguration
                    {
                        VectorIndexType = VectorIndexTypeEnum.HnswRam,
                        VectorDimensionality = indexDimensionality,
                        VectorIndexThreshold = 1,
                        VectorIndexM = 8,
                        VectorIndexEf = 16,
                        VectorIndexEfConstruction = 32
                    },
                    cancellationToken).ConfigureAwait(false);

                VectorMetadata dirtyVector = await client.Vector.Create(new VectorMetadata
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    NodeGUID = node.GUID,
                    Model = "dirty-repair-model",
                    Dimensionality = 2,
                    Content = "Dirty vector",
                    Vectors = new List<float> { 1.0f, 0.0f }
                }, cancellationToken).ConfigureAwait(false);

                Graph dirtyGraph = await client.Graph.ReadByGuid(
                    tenant.GUID,
                    graph.GUID,
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(dirtyGraph.VectorIndexDirty, "Graph should be marked dirty after post-commit index update failure");
                AssertNotNull(dirtyGraph.VectorIndexDirtyUtc, "Dirty graph should record dirty timestamp");
                AssertTrue(
                    dirtyGraph.VectorIndexDirtyReason != null
                    && dirtyGraph.VectorIndexDirtyReason.Contains("Vector create index update failed", StringComparison.Ordinal),
                    "Dirty graph should record vector create failure reason");

                VectorIndexStatistics? dirtyStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(dirtyStats, "Dirty vector index statistics");
                AssertTrue(dirtyStats!.IsDirty, "Vector index statistics should expose dirty state");
                AssertNotNull(dirtyStats.DirtySinceUtc, "Vector index statistics should expose dirty timestamp");
                AssertTrue(
                    dirtyStats.DirtyReason != null
                    && dirtyStats.DirtyReason.Contains("Vector create index update failed", StringComparison.Ordinal),
                    "Vector index statistics should expose dirty reason");

                List<VectorSearchResult> dirtySearchResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    new List<float> { 1.0f, 0.0f },
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(dirtySearchResults, node.GUID, "Dirty index fallback search");

                dirtyVector.Dimensionality = indexDimensionality;
                dirtyVector.Vectors = new List<float> { 1.0f, 0.0f, 0.0f };
                dirtyVector.Content = "Repaired vector";

                await client.Vector.Update(dirtyVector, cancellationToken).ConfigureAwait(false);

                VectorIndexStatistics? preRepairStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(preRepairStats, "Pre-repair vector index statistics");
                AssertTrue(preRepairStats!.IsDirty, "Incremental vector update should not clear dirty state without rebuild");

                await client.Graph.RebuildVectorIndex(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                Graph repairedGraph = await client.Graph.ReadByGuid(
                    tenant.GUID,
                    graph.GUID,
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(!repairedGraph.VectorIndexDirty, "Graph dirty state should clear after rebuild");
                AssertEqual(null, repairedGraph.VectorIndexDirtyUtc, "Graph dirty timestamp should clear after rebuild");
                AssertEqual(null, repairedGraph.VectorIndexDirtyReason, "Graph dirty reason should clear after rebuild");

                VectorIndexStatistics? repairedStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(repairedStats, "Repaired vector index statistics");
                AssertTrue(!repairedStats!.IsDirty, "Vector index statistics dirty state should clear after rebuild");
                AssertEqual(1, repairedStats.VectorCount, "Rebuilt vector index should contain the repaired vector");

                List<VectorSearchResult> repairedSearchResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    new List<float> { 1.0f, 0.0f, 0.0f },
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(repairedSearchResults, node.GUID, "Repaired index search");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static async Task ExecuteVectorIndexTransactionRollbackDirtySuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 3;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-index-transaction-rollback.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Index Transaction Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Index Transaction Graph"
                }, cancellationToken).ConfigureAwait(false);

                Node node = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Transaction Rollback Node"
                }, cancellationToken).ConfigureAwait(false);

                await client.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    new VectorIndexConfiguration
                    {
                        VectorIndexType = VectorIndexTypeEnum.HnswRam,
                        VectorDimensionality = dimensionality,
                        VectorIndexThreshold = 1,
                        VectorIndexM = 8,
                        VectorIndexEf = 16,
                        VectorIndexEfConstruction = 32
                    },
                    cancellationToken).ConfigureAwait(false);

                Guid duplicateVectorGuid = Guid.NewGuid();
                TransactionRequest request = new TransactionRequest
                {
                    Operations = new List<TransactionOperation>
                    {
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Vector,
                            Payload = new VectorMetadata
                            {
                                GUID = duplicateVectorGuid,
                                NodeGUID = node.GUID,
                                Model = "transaction-dirty-model",
                                Dimensionality = dimensionality,
                                Content = "First vector",
                                Vectors = new List<float> { 0.0f, 1.0f, 0.0f }
                            }
                        },
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Vector,
                            Payload = new VectorMetadata
                            {
                                GUID = duplicateVectorGuid,
                                NodeGUID = node.GUID,
                                Model = "transaction-dirty-model",
                                Dimensionality = dimensionality,
                                Content = "Duplicate vector",
                                Vectors = new List<float> { 0.0f, 1.0f, 0.0f }
                            }
                        }
                    }
                };

                TransactionResult transactionResult = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    request,
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(!transactionResult.Success, "Duplicate vector transaction should fail");
                AssertTrue(transactionResult.RolledBack, "Duplicate vector transaction should roll back");
                AssertEqual(1, transactionResult.FailedOperationIndex, "Duplicate vector transaction failure index");
                AssertTrue(
                    !await client.Vector.ExistsByGuid(tenant.GUID, duplicateVectorGuid, cancellationToken).ConfigureAwait(false),
                    "Rolled-back vector should not remain in the database");

                Graph dirtyGraph = await client.Graph.ReadByGuid(
                    tenant.GUID,
                    graph.GUID,
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(!dirtyGraph.VectorIndexDirty, "Transaction rollback should not dirty index when staged mutations are discarded");

                VectorIndexStatistics? rollbackStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(rollbackStats, "Transaction rollback vector index statistics");
                AssertTrue(!rollbackStats!.IsDirty, "Transaction rollback should leave vector index clean");
                AssertEqual(0, rollbackStats.VectorCount, "Transaction rollback should not apply staged vector index mutation");

                Guid committedVectorGuid = Guid.NewGuid();
                TransactionResult commitResult = await client.Transaction.Execute(
                    tenant.GUID,
                    graph.GUID,
                    new TransactionRequest
                    {
                        Operations = new List<TransactionOperation>
                        {
                            new TransactionOperation
                            {
                                OperationType = TransactionOperationTypeEnum.Create,
                                ObjectType = TransactionObjectTypeEnum.Vector,
                                Payload = new VectorMetadata
                                {
                                    GUID = committedVectorGuid,
                                    NodeGUID = node.GUID,
                                    Model = "transaction-staged-model",
                                    Dimensionality = dimensionality,
                                    Content = "Committed vector",
                                    Vectors = new List<float> { 1.0f, 0.0f, 0.0f }
                                }
                            }
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(commitResult.Success, "Committed vector transaction should succeed");
                AssertTrue(
                    await client.Vector.ExistsByGuid(tenant.GUID, committedVectorGuid, cancellationToken).ConfigureAwait(false),
                    "Committed vector should remain in the database");

                VectorIndexStatistics? commitStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(commitStats, "Committed transaction vector index statistics");
                AssertTrue(!commitStats!.IsDirty, "Committed staged vector index mutation should leave index clean");
                AssertEqual(1, commitStats.VectorCount, "Committed staged vector index mutation should apply after commit");

                List<VectorSearchResult> committedResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    new List<float> { 1.0f, 0.0f, 0.0f },
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(committedResults, node.GUID, "Committed staged vector index search");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static async Task ExecuteVectorIndexConcurrentTransactionSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 4;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-index-concurrent-transactions.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Index Concurrent Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Index Concurrent Graph"
                }, cancellationToken).ConfigureAwait(false);

                Node firstNode = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Concurrent Transaction Node 1"
                }, cancellationToken).ConfigureAwait(false);

                Node secondNode = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Concurrent Transaction Node 2"
                }, cancellationToken).ConfigureAwait(false);

                await client.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    new VectorIndexConfiguration
                    {
                        VectorIndexType = VectorIndexTypeEnum.HnswRam,
                        VectorDimensionality = dimensionality,
                        VectorIndexThreshold = 1,
                        VectorIndexM = 8,
                        VectorIndexEf = 16,
                        VectorIndexEfConstruction = 32
                    },
                    cancellationToken).ConfigureAwait(false);

                TransactionRequest firstRequest = CreateSingleVectorTransactionRequest(
                    firstNode.GUID,
                    "concurrent-vector-1",
                    dimensionality,
                    BuildDeterministicVector(0, dimensionality));

                TransactionRequest secondRequest = CreateSingleVectorTransactionRequest(
                    secondNode.GUID,
                    "concurrent-vector-2",
                    dimensionality,
                    BuildDeterministicVector(1, dimensionality));

                Task<TransactionResult> firstTask = client.Transaction.Execute(tenant.GUID, graph.GUID, firstRequest, cancellationToken);
                Task<TransactionResult> secondTask = client.Transaction.Execute(tenant.GUID, graph.GUID, secondRequest, cancellationToken);

                TransactionResult[] results = await Task.WhenAll(firstTask, secondTask).ConfigureAwait(false);

                AssertTrue(results[0].Success, "First concurrent vector transaction succeeds");
                AssertTrue(results[1].Success, "Second concurrent vector transaction succeeds");

                VectorIndexStatistics? stats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(stats, "Concurrent vector transaction index stats");
                AssertTrue(!stats!.IsDirty, "Concurrent vector transactions leave index clean");
                AssertEqual(2, stats.VectorCount, "Concurrent vector transactions apply both staged index mutations");

                List<VectorSearchResult> firstResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                List<VectorSearchResult> secondResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(1, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(firstResults, firstNode.GUID, "First concurrent vector transaction search");
                AssertTopNode(secondResults, secondNode.GUID, "Second concurrent vector transaction search");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static async Task ExecuteVectorIndexSearchSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 32;
            const int nodeCount = 18;
            string ramDatabasePath = BuildVectorArtifactPath("touchstone-vector-index-search-ram.db");
            string sqliteDatabasePath = BuildVectorArtifactPath("touchstone-vector-index-search-sqlite.db");
            string sqliteIndexPath = BuildVectorArtifactPath("touchstone-vector-index-search-index.sqlite");

            CleanupSqliteArtifacts(ramDatabasePath, sqliteDatabasePath, sqliteIndexPath, sqliteIndexPath + ".layers");

            VectorIndexStatistics ramStats = await ExecuteIndexedSearchVariantAsync(
                databasePath: ramDatabasePath,
                indexType: VectorIndexTypeEnum.HnswRam,
                indexFile: null,
                dimensionality: dimensionality,
                nodeCount: nodeCount,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            VectorIndexStatistics sqliteStats = await ExecuteIndexedSearchVariantAsync(
                databasePath: sqliteDatabasePath,
                indexType: VectorIndexTypeEnum.HnswSqlite,
                indexFile: sqliteIndexPath,
                dimensionality: dimensionality,
                nodeCount: nodeCount,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            try
            {
                AssertEqual(VectorIndexTypeEnum.HnswRam, ramStats.IndexType, "RAM index type");
                AssertEqual(VectorIndexTypeEnum.HnswSqlite, sqliteStats.IndexType, "SQLite index type");
                AssertEqual(nodeCount, ramStats.VectorCount, "RAM vector count");
                AssertEqual(nodeCount, sqliteStats.VectorCount, "SQLite vector count");
                AssertEqual(dimensionality, ramStats.Dimensions, "RAM dimensionality");
                AssertEqual(dimensionality, sqliteStats.Dimensions, "SQLite dimensionality");
                AssertEqual(sqliteIndexPath, sqliteStats.IndexFile, "SQLite index file path");
                AssertTrue(File.Exists(sqliteIndexPath), "SQLite index file should exist");
            }
            finally
            {
                CleanupSqliteArtifacts(ramDatabasePath, sqliteDatabasePath, sqliteIndexPath, sqliteIndexPath + ".layers");
            }
        }

        private static async Task<VectorIndexStatistics> ExecuteIndexedSearchVariantAsync(
            string databasePath,
            VectorIndexTypeEnum indexType,
            string? indexFile,
            int dimensionality,
            int nodeCount,
            CancellationToken cancellationToken)
        {
            CleanupSqliteArtifacts(databasePath, indexFile);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone " + indexType + " Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone " + indexType + " Graph",
                    VectorIndexType = indexType,
                    VectorIndexFile = indexFile,
                    VectorDimensionality = dimensionality,
                    VectorIndexThreshold = 1,
                    VectorIndexM = 8,
                    VectorIndexEf = 32,
                    VectorIndexEfConstruction = 64
                }, cancellationToken).ConfigureAwait(false);

                List<Node> nodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: nodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                client.Flush();

                VectorIndexStatistics? stats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(stats, indexType + " vector index statistics");

                List<VectorSearchResult> results = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(results, nodes[0].GUID, indexType + " cosine similarity");
                return stats!;
            }
            finally
            {
                client?.Dispose();
            }
        }

        private static async Task<List<Node>> CreateDeterministicNodesWithVectorsAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            int startIndex,
            int count,
            int dimensionality,
            CancellationToken cancellationToken)
        {
            List<Node> nodes = new List<Node>(count);

            for (int i = 0; i < count; i++)
            {
                int vectorIndex = startIndex + i;
                Guid nodeGuid = Guid.NewGuid();

                nodes.Add(new Node
                {
                    GUID = nodeGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "Vector Node " + vectorIndex,
                    Data = new { Index = vectorIndex },
                    Vectors = new List<VectorMetadata>
                    {
                        new VectorMetadata
                        {
                            GUID = Guid.NewGuid(),
                            TenantGUID = tenantGuid,
                            GraphGUID = graphGuid,
                            NodeGUID = nodeGuid,
                            Model = "touchstone-deterministic",
                            Dimensionality = dimensionality,
                            Content = "Vector node " + vectorIndex,
                            Vectors = BuildDeterministicVector(vectorIndex, dimensionality)
                        }
                    }
                });
            }

            return await client.Node.CreateMany(
                tenantGuid,
                graphGuid,
                nodes,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<List<VectorSearchResult>> SearchVectorsAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            List<float> embeddings,
            VectorSearchTypeEnum searchType,
            int topK,
            CancellationToken cancellationToken)
        {
            VectorSearchRequest request = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = searchType,
                Embeddings = embeddings,
                TopK = topK
            };

            if (searchType == VectorSearchTypeEnum.EuclidianDistance)
            {
                request.MaximumDistance = 0.01f;
            }
            else if (searchType == VectorSearchTypeEnum.CosineSimilarity)
            {
                request.MinimumScore = 0.0f;
            }
            else if (searchType == VectorSearchTypeEnum.DotProduct)
            {
                request.MinimumInnerProduct = 0.0f;
            }

            List<VectorSearchResult> results = new List<VectorSearchResult>();

            await foreach (VectorSearchResult result in client.Vector.Search(request, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                results.Add(result);
            }

            return results;
        }

        private static VectorMetadata CreateVectorMetadata(
            Guid tenantGuid,
            Guid graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string content,
            int vectorIndex)
        {
            return new VectorMetadata
            {
                GUID = Guid.NewGuid(),
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                NodeGUID = nodeGuid,
                EdgeGUID = edgeGuid,
                Model = "touchstone-model",
                Dimensionality = 3,
                Content = content,
                Vectors = BuildDeterministicVector(vectorIndex, 3),
                CreatedUtc = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                LastUpdateUtc = new DateTime(2025, 1, 2, 3, 5, 5, DateTimeKind.Utc)
            };
        }

        private static TransactionRequest CreateSingleVectorTransactionRequest(
            Guid nodeGuid,
            string content,
            int dimensionality,
            List<float> vector)
        {
            return new TransactionRequest
            {
                Operations = new List<TransactionOperation>
                {
                    new TransactionOperation
                    {
                        OperationType = TransactionOperationTypeEnum.Create,
                        ObjectType = TransactionObjectTypeEnum.Vector,
                        Payload = new VectorMetadata
                        {
                            GUID = Guid.NewGuid(),
                            NodeGUID = nodeGuid,
                            Model = "transaction-staged-model",
                            Dimensionality = dimensionality,
                            Content = content,
                            Vectors = vector
                        }
                    }
                }
            };
        }

        private static async Task<object> GetIndexedNodeAsync(
            HnswLiteVectorIndex index,
            Guid id,
            CancellationToken cancellationToken)
        {
            FieldInfo? storageField = typeof(HnswLiteVectorIndex).GetField("_Storage", BindingFlags.Instance | BindingFlags.NonPublic);
            AssertNotNull(storageField, "HNSW index storage field");

            object? storage = storageField!.GetValue(index);
            AssertNotNull(storage, "HNSW index storage");

            MethodInfo? getNodeAsync = storage!.GetType().GetMethod("GetNodeAsync");
            AssertNotNull(getNodeAsync, "HNSW storage GetNodeAsync method");

            object? taskObject = getNodeAsync!.Invoke(storage, new object[] { id, cancellationToken });
            AssertNotNull(taskObject, "HNSW storage GetNodeAsync task");

            await ((Task)taskObject!).ConfigureAwait(false);
            PropertyInfo? resultProperty = taskObject.GetType().GetProperty("Result");
            AssertNotNull(resultProperty, "HNSW storage GetNodeAsync result property");

            object? node = resultProperty!.GetValue(taskObject);
            AssertNotNull(node, "HNSW indexed node");
            return node!;
        }

        private static T GetIndexedNodeProperty<T>(object node, string propertyName)
        {
            PropertyInfo? property = node.GetType().GetProperty(propertyName);
            AssertNotNull(property, "HNSW indexed node " + propertyName + " property");

            object? value = property!.GetValue(node);
            AssertNotNull(value, "HNSW indexed node " + propertyName + " value");
            return (T)value!;
        }

        private static void AssertLabelsContain(List<string> labels, string expected, string context)
        {
            AssertNotNull(labels, context);
            AssertTrue(labels!.Contains(expected), context + " should contain " + expected);
        }

        private static void AssertTagEquals(Dictionary<string, object> tags, string key, string expected, string context)
        {
            AssertNotNull(tags, context);
            AssertTrue(tags!.ContainsKey(key), context + " should contain tag " + key);
            AssertEqual(expected, tags[key]?.ToString(), context);
        }

        private static void AssertTopNode(
            IReadOnlyList<VectorSearchResult> results,
            Guid expectedNodeGuid,
            string context)
        {
            AssertTrue(results.Count > 0, context + " should return at least one result");
            AssertNotNull(results[0].Node, context + " top result node");
            AssertEqual(expectedNodeGuid, results[0].Node!.GUID, context + " should return the exact match first");
        }

        private static List<float> BuildDeterministicVector(int index, int dimensionality)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));
            if (index >= dimensionality) throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the dimensionality for deterministic one-hot vectors.");

            List<float> vector = Enumerable.Repeat(0.0f, dimensionality).ToList();
            vector[index] = 1.0f;
            return vector;
        }

        private static string BuildVectorArtifactPath(string filename)
        {
            Directory.CreateDirectory(_VectorArtifactDirectory);
            return Path.Combine(_VectorArtifactDirectory, filename);
        }

        private static void RewriteHnswStateAsLegacyArtifact(string indexPath)
        {
            JsonObject root = JsonNode.Parse(File.ReadAllText(indexPath))!.AsObject();
            root.Remove(nameof(HnswIndexState.FormatVersion));
            root.Remove(nameof(HnswIndexState.HnswLiteVersion));

            if (root[nameof(HnswIndexState.Node)] is JsonArray nodeStates)
            {
                foreach (JsonNode? nodeState in nodeStates)
                {
                    nodeState?.AsObject().Remove(nameof(HnswNodeState.Connections));
                }
            }

            File.WriteAllText(indexPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void CleanupSqliteArtifacts(params string?[] paths)
        {
            foreach (string? path in paths)
            {
                if (String.IsNullOrEmpty(path)) continue;

                DeleteIfExists(path);
                DeleteIfExists(path + "-shm");
                DeleteIfExists(path + "-wal");
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (!File.Exists(path)) return;

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
