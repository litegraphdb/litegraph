namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        #region ImportExport-Members

        private static readonly Guid _IeNodeA = Guid.Parse("a0000000-0000-0000-0000-0000000000a1");
        private static readonly Guid _IeNodeB = Guid.Parse("a0000000-0000-0000-0000-0000000000b2");
        private static readonly Guid _IeNodeC = Guid.Parse("a0000000-0000-0000-0000-0000000000c3");
        private static readonly Guid _IeNodeD = Guid.Parse("a0000000-0000-0000-0000-0000000000d4");
        private static readonly Guid _IeNodeE = Guid.Parse("a0000000-0000-0000-0000-0000000000e5");
        private static readonly Guid _IeNodeF = Guid.Parse("a0000000-0000-0000-0000-0000000000f6");

        #endregion

        #region ImportExport-Suite

        private static TestSuiteDescriptor CreateImportExportSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "ImportExport",
                displayName: "Subgraph extraction and JSONL import/export",
                cases: new List<TestCaseDescriptor>
                {
                    Ie("Extract.DepthZero", "Depth 0 returns only start node", TestExtractDepthZero),
                    Ie("Extract.DepthOne", "Depth 1 returns immediate neighbors", TestExtractDepthOne),
                    Ie("Extract.Directions", "Outbound, inbound, and both differ", TestExtractDirections),
                    Ie("Extract.EdgeLabelFilter", "Edge label filter prunes traversal", TestExtractEdgeLabelFilter),
                    Ie("Extract.MaxEdgeCost", "Max edge cost prunes traversal", TestExtractMaxEdgeCost),
                    Ie("Extract.NodeLabelFilter", "Node label filter excludes neighbors", TestExtractNodeLabelFilter),
                    Ie("Extract.MaxNodes", "Max nodes caps the result", TestExtractMaxNodes),
                    Ie("Extract.BothEndpointsInvariant", "Every edge has both endpoints included", TestExtractBothEndpointsInvariant),
                    Ie("Extract.NoStartNodeThrows", "Empty start node list throws", TestExtractNoStartNodeThrows),
                    Ie("Extract.MissingStartNodeThrows", "Missing start node throws", TestExtractMissingStartNodeThrows),
                    Ie("Extract.NegativeDepthThrows", "Negative max depth throws", TestExtractNegativeDepthThrows),
                    Ie("Jsonl.RoundTripWholeGraph", "Whole-graph export and re-import match", TestRoundTripWholeGraph),
                    Ie("Jsonl.CommentLinesIgnored", "Comment lines are ignored on import", TestCommentLinesIgnored),
                    Ie("Import.CreateNewPreservesNodeGuids", "Create-new preserves node GUIDs", TestImportCreateNewPreserve),
                    Ie("Import.MergeRegenerate", "Merge with regenerate adds a disjoint copy", TestImportMergeRegenerate),
                    Ie("Import.MergeSkipIdempotent", "Merge with skip is idempotent", TestImportMergeSkipIdempotent),
                    Ie("Import.MergeOverwrite", "Merge with overwrite updates existing", TestImportMergeOverwrite),
                    Ie("Import.PreserveCollisionThrows", "Preserve collision throws and rolls back", TestImportPreserveCollisionThrows),
                    Ie("Import.MalformedAbortThrows", "Malformed line aborts", TestImportMalformedAbortThrows),
                    Ie("Import.MalformedSkip", "Malformed line skipped under skip policy", TestImportMalformedSkip),
                    Ie("Import.MergeMissingTargetThrows", "Merge without target GUID throws", TestImportMergeMissingTargetThrows),
                    Ie("Import.DanglingEdgeDropped", "Edge with unresolved endpoint is dropped", TestImportDanglingEdgeDropped),
                    Ie("Import.EmptyBodyCreatesEmptyGraph", "Empty body creates an empty graph", TestImportEmptyBodyCreatesEmptyGraph)
                });
        }

        private static TestCaseDescriptor Ie(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: "ImportExport", caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region ImportExport-Cases

        private static async Task TestExtractDepthZero(CancellationToken token)
        {
            string db = IeDbName("depth0");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(new SubgraphExtractionRequest
                {
                    TenantGUID = tenant,
                    GraphGUID = graph,
                    StartNodeGUIDs = new List<Guid> { _IeNodeA },
                    MaxDepth = 0,
                    Direction = GraphTraversalDirectionEnum.Both
                }, token).ConfigureAwait(false);

                AssertEqual(1, result.Nodes.Count, "Depth 0 returns exactly the start node");
                AssertTrue(result.Nodes.Any(n => n.GUID == _IeNodeA), "Depth 0 result contains the start node");
                AssertEqual(0, result.Edges.Count, "Depth 0 returns no edges from a single start node");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractDepthOne(CancellationToken token)
        {
            string db = IeDbName("depth1");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(new SubgraphExtractionRequest
                {
                    TenantGUID = tenant,
                    GraphGUID = graph,
                    StartNodeGUIDs = new List<Guid> { _IeNodeA },
                    MaxDepth = 1,
                    Direction = GraphTraversalDirectionEnum.Both
                }, token).ConfigureAwait(false);

                HashSet<Guid> nodes = result.Nodes.Select(n => n.GUID).ToHashSet();
                AssertTrue(nodes.SetEquals(new[] { _IeNodeA, _IeNodeB, _IeNodeC, _IeNodeF }), "Depth 1 both-direction neighbors of A are B, C, F");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractDirections(CancellationToken token)
        {
            string db = IeDbName("dir");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult outbound = await client.ExtractSubgraph(IeReq(tenant, graph, _IeNodeA, 1, GraphTraversalDirectionEnum.Outbound), token).ConfigureAwait(false);
                SearchResult inbound = await client.ExtractSubgraph(IeReq(tenant, graph, _IeNodeA, 1, GraphTraversalDirectionEnum.Inbound), token).ConfigureAwait(false);

                HashSet<Guid> outNodes = outbound.Nodes.Select(n => n.GUID).ToHashSet();
                HashSet<Guid> inNodes = inbound.Nodes.Select(n => n.GUID).ToHashSet();

                AssertTrue(outNodes.SetEquals(new[] { _IeNodeA, _IeNodeB, _IeNodeC }), "Outbound depth 1 from A reaches B and C");
                AssertTrue(inNodes.SetEquals(new[] { _IeNodeA, _IeNodeF }), "Inbound depth 1 from A reaches F only");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractEdgeLabelFilter(CancellationToken token)
        {
            string db = IeDbName("edgelabel");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(new SubgraphExtractionRequest
                {
                    TenantGUID = tenant,
                    GraphGUID = graph,
                    StartNodeGUIDs = new List<Guid> { _IeNodeA },
                    MaxDepth = 2,
                    Direction = GraphTraversalDirectionEnum.Both,
                    EdgeLabels = new List<string> { "near" }
                }, token).ConfigureAwait(false);

                HashSet<Guid> nodes = result.Nodes.Select(n => n.GUID).ToHashSet();
                AssertTrue(nodes.Contains(_IeNodeB), "Near-only walk reaches B");
                AssertTrue(nodes.Contains(_IeNodeD), "Near-only walk reaches D");
                AssertFalse(nodes.Contains(_IeNodeC), "Near-only walk does not reach C via the far edge");
                AssertFalse(nodes.Contains(_IeNodeE), "Near-only walk does not reach E");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractMaxEdgeCost(CancellationToken token)
        {
            string db = IeDbName("maxcost");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(new SubgraphExtractionRequest
                {
                    TenantGUID = tenant,
                    GraphGUID = graph,
                    StartNodeGUIDs = new List<Guid> { _IeNodeA },
                    MaxDepth = 2,
                    Direction = GraphTraversalDirectionEnum.Both,
                    MaxEdgeCost = 1
                }, token).ConfigureAwait(false);

                HashSet<Guid> nodes = result.Nodes.Select(n => n.GUID).ToHashSet();
                AssertTrue(nodes.Contains(_IeNodeB), "Cost<=1 walk reaches B (cost 1)");
                AssertFalse(nodes.Contains(_IeNodeC), "Cost<=1 walk excludes C (edge cost 5)");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractNodeLabelFilter(CancellationToken token)
        {
            string db = IeDbName("nodelabel");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(new SubgraphExtractionRequest
                {
                    TenantGUID = tenant,
                    GraphGUID = graph,
                    StartNodeGUIDs = new List<Guid> { _IeNodeA },
                    MaxDepth = 2,
                    Direction = GraphTraversalDirectionEnum.Both,
                    NodeLabels = new List<string> { "keep" }
                }, token).ConfigureAwait(false);

                HashSet<Guid> nodes = result.Nodes.Select(n => n.GUID).ToHashSet();
                AssertTrue(nodes.Contains(_IeNodeA), "Start node A is exempt from node filter");
                AssertTrue(nodes.Contains(_IeNodeB), "Kept node B is included");
                AssertFalse(nodes.Contains(_IeNodeD), "Dropped node D is excluded");
                AssertFalse(nodes.Contains(_IeNodeF), "Unlabeled node F is excluded");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractMaxNodes(CancellationToken token)
        {
            string db = IeDbName("maxnodes");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(new SubgraphExtractionRequest
                {
                    TenantGUID = tenant,
                    GraphGUID = graph,
                    StartNodeGUIDs = new List<Guid> { _IeNodeA },
                    MaxDepth = 3,
                    Direction = GraphTraversalDirectionEnum.Both,
                    MaxNodes = 3
                }, token).ConfigureAwait(false);

                AssertEqual(3, result.Nodes.Count, "Max nodes caps included node count at 3");
            }
            IeCleanup(db);
        }

        private static async Task TestExtractBothEndpointsInvariant(CancellationToken token)
        {
            string db = IeDbName("invariant");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                SearchResult result = await client.ExtractSubgraph(IeReq(tenant, graph, _IeNodeA, 3, GraphTraversalDirectionEnum.Both), token).ConfigureAwait(false);

                HashSet<Guid> nodes = result.Nodes.Select(n => n.GUID).ToHashSet();
                foreach (Edge edge in result.Edges)
                {
                    AssertTrue(nodes.Contains(edge.From), "Edge From endpoint is in the node set");
                    AssertTrue(nodes.Contains(edge.To), "Edge To endpoint is in the node set");
                }
            }
            IeCleanup(db);
        }

        private static async Task TestExtractNoStartNodeThrows(CancellationToken token)
        {
            string db = IeDbName("nostart");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                await IeAssertThrows<ArgumentException>(async () =>
                {
                    await client.ExtractSubgraph(new SubgraphExtractionRequest
                    {
                        TenantGUID = tenant,
                        GraphGUID = graph,
                        StartNodeGUIDs = new List<Guid>()
                    }, token).ConfigureAwait(false);
                }, "Empty start node list throws ArgumentException").ConfigureAwait(false);
            }
            IeCleanup(db);
        }

        private static async Task TestExtractMissingStartNodeThrows(CancellationToken token)
        {
            string db = IeDbName("missingstart");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                await IeAssertThrows<ArgumentException>(async () =>
                {
                    await client.ExtractSubgraph(IeReq(tenant, graph, Guid.NewGuid(), 2, GraphTraversalDirectionEnum.Both), token).ConfigureAwait(false);
                }, "Missing start node throws ArgumentException").ConfigureAwait(false);
            }
            IeCleanup(db);
        }

        private static async Task TestExtractNegativeDepthThrows(CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            IeAssertThrowsSync<ArgumentOutOfRangeException>(() =>
            {
                SubgraphExtractionRequest request = new SubgraphExtractionRequest();
                request.MaxDepth = -1;
            }, "Negative max depth throws ArgumentOutOfRangeException");
        }

        private static async Task TestRoundTripWholeGraph(CancellationToken token)
        {
            string db = IeDbName("roundtrip");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                string jsonl = await client.RenderGraphAsJsonl(tenant, graph, true, true, token).ConfigureAwait(false);
                AssertTrue(jsonl.Contains("# litegraph-jsonl"), "Export includes a metadata header");

                GraphImportResult import = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Regenerate
                }, token).ConfigureAwait(false);

                AssertTrue(import.Success, "Round-trip import succeeds");
                AssertEqual(1, import.GraphsCreated, "One graph created");

                HashSet<string> before = await IeCanonicalEdges(client, tenant, graph, token).ConfigureAwait(false);
                HashSet<string> after = await IeCanonicalEdges(client, tenant, import.GraphGUID, token).ConfigureAwait(false);
                AssertTrue(before.SetEquals(after), "Edge structure is preserved across the round trip");

                AssertEqual(await IeNodeCount(client, tenant, graph, token).ConfigureAwait(false),
                    await IeNodeCount(client, tenant, import.GraphGUID, token).ConfigureAwait(false),
                    "Node count is preserved across the round trip");
            }
            IeCleanup(db);
        }

        private static async Task TestCommentLinesIgnored(CancellationToken token)
        {
            string db = IeDbName("comments");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(client, tenant).ConfigureAwait(false);

                string jsonl = await client.RenderGraphAsJsonl(tenant, graph, false, false, token).ConfigureAwait(false);
                GraphImportResult import = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Regenerate
                }, token).ConfigureAwait(false);

                AssertTrue(import.LinesIgnored > 0, "At least one comment line is ignored");
                AssertTrue(import.Success, "Import with comment header succeeds");
            }
            IeCleanup(db);
        }

        private static async Task TestImportCreateNewPreserve(CancellationToken token)
        {
            // GUIDs are globally unique in the store, so Preserve is exercised across two databases (the backup/restore case).
            string srcDb = IeDbName("createnew-src");
            string dstDb = IeDbName("createnew-dst");
            string jsonl;
            using (LiteGraphClient src = IeNewClient(srcDb))
            {
                Guid tenant = await IeSeedTenant(src).ConfigureAwait(false);
                Guid graph = await IeSeedFixture(src, tenant).ConfigureAwait(false);
                jsonl = await src.RenderGraphAsJsonl(tenant, graph, true, true, token).ConfigureAwait(false);
            }

            using (LiteGraphClient dst = IeNewClient(dstDb))
            {
                Guid tenant = await IeSeedTenant(dst).ConfigureAwait(false);
                GraphImportResult import = await dst.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Preserve,
                    NewGraph = new Graph { GUID = Guid.NewGuid(), TenantGUID = tenant, Name = "Preserved copy" }
                }, token).ConfigureAwait(false);

                AssertTrue(import.Success, "Create-new preserve import succeeds");
                Node preserved = await dst.Node.ReadByGuid(tenant, import.GraphGUID, _IeNodeA, false, false, token).ConfigureAwait(false);
                AssertNotNull(preserved, "Original node GUID is preserved in the new database");
            }
            IeCleanup(srcDb);
            IeCleanup(dstDb);
        }

        private static async Task TestImportMergeRegenerate(CancellationToken token)
        {
            string db = IeDbName("mergeregen");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid source = await IeSeedFixture(client, tenant).ConfigureAwait(false);
                Guid target = await IeSeedTargetGraph(client, tenant).ConfigureAwait(false);

                int targetBefore = await IeNodeCount(client, tenant, target, token).ConfigureAwait(false);
                int sourceCount = await IeNodeCount(client, tenant, source, token).ConfigureAwait(false);

                string jsonl = await client.RenderGraphAsJsonl(tenant, source, true, true, token).ConfigureAwait(false);

                GraphImportResult import = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.MergeIntoExisting,
                    GuidStrategy = GraphImportGuidStrategyEnum.Regenerate,
                    TargetGraphGUID = target
                }, token).ConfigureAwait(false);

                AssertTrue(import.Success, "Merge regenerate succeeds");
                AssertTrue(import.GuidMap.Count > 0, "Regenerate populates the GUID map");
                AssertEqual(targetBefore + sourceCount, await IeNodeCount(client, tenant, target, token).ConfigureAwait(false),
                    "Target node count grows by the imported node count");
            }
            IeCleanup(db);
        }

        private static async Task TestImportMergeSkipIdempotent(CancellationToken token)
        {
            string dstDb = IeDbName("mergeskip");
            string jsonl = await IeFixtureJsonl(IeDbName("mergeskip-src"), token).ConfigureAwait(false);
            using (LiteGraphClient client = IeNewClient(dstDb))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);

                // Seed a graph carrying the source GUIDs via a preserve create-new (fresh database).
                GraphImportResult seeded = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Preserve,
                    NewGraph = new Graph { GUID = Guid.NewGuid(), TenantGUID = tenant, Name = "Skip target" }
                }, token).ConfigureAwait(false);

                int before = await IeNodeCount(client, tenant, seeded.GraphGUID, token).ConfigureAwait(false);

                GraphImportResult skip = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.MergeIntoExisting,
                    GuidStrategy = GraphImportGuidStrategyEnum.Skip,
                    TargetGraphGUID = seeded.GraphGUID
                }, token).ConfigureAwait(false);

                AssertTrue(skip.Success, "Skip merge succeeds");
                AssertEqual(0, skip.NodesCreated, "Skip merge creates no nodes on a full collision");
                AssertTrue(skip.NodesSkipped > 0, "Skip merge reports skipped nodes");
                AssertEqual(before, await IeNodeCount(client, tenant, seeded.GraphGUID, token).ConfigureAwait(false),
                    "Skip merge leaves the node count unchanged");
            }
            IeCleanup(dstDb);
        }

        private static async Task TestImportMergeOverwrite(CancellationToken token)
        {
            string dstDb = IeDbName("mergeoverwrite");
            string jsonl = await IeFixtureJsonl(IeDbName("mergeoverwrite-src"), token).ConfigureAwait(false);
            using (LiteGraphClient client = IeNewClient(dstDb))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);

                GraphImportResult seeded = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Preserve,
                    NewGraph = new Graph { GUID = Guid.NewGuid(), TenantGUID = tenant, Name = "Overwrite target" }
                }, token).ConfigureAwait(false);

                GraphImportResult overwrite = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.MergeIntoExisting,
                    GuidStrategy = GraphImportGuidStrategyEnum.Overwrite,
                    TargetGraphGUID = seeded.GraphGUID
                }, token).ConfigureAwait(false);

                AssertTrue(overwrite.Success, "Overwrite merge succeeds");
                AssertEqual(0, overwrite.NodesCreated, "Overwrite merge creates no new nodes on a full collision");
                AssertTrue(overwrite.NodesUpdated > 0, "Overwrite merge updates existing nodes");
            }
            IeCleanup(dstDb);
        }

        private static async Task TestImportPreserveCollisionThrows(CancellationToken token)
        {
            string dstDb = IeDbName("preservecollision");
            string jsonl = await IeFixtureJsonl(IeDbName("preservecollision-src"), token).ConfigureAwait(false);
            using (LiteGraphClient client = IeNewClient(dstDb))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);

                GraphImportResult seeded = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Preserve,
                    NewGraph = new Graph { GUID = Guid.NewGuid(), TenantGUID = tenant, Name = "Collision target" }
                }, token).ConfigureAwait(false);

                int before = await IeNodeCount(client, tenant, seeded.GraphGUID, token).ConfigureAwait(false);

                await IeAssertThrows<InvalidOperationException>(async () =>
                {
                    await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                    {
                        Mode = GraphImportModeEnum.MergeIntoExisting,
                        GuidStrategy = GraphImportGuidStrategyEnum.Preserve,
                        TargetGraphGUID = seeded.GraphGUID
                    }, token).ConfigureAwait(false);
                }, "Preserve collision throws InvalidOperationException").ConfigureAwait(false);

                AssertEqual(before, await IeNodeCount(client, tenant, seeded.GraphGUID, token).ConfigureAwait(false),
                    "Preserve collision leaves the target unchanged");
            }
            IeCleanup(dstDb);
        }

        private static async Task TestImportMalformedAbortThrows(CancellationToken token)
        {
            string db = IeDbName("malformedabort");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                string jsonl = "# header\n{\"Type\":\"Node\",\"Object\":{\"GUID\":\"" + Guid.NewGuid() + "\",\"Name\":\"ok\"}}\nthis is not json\n";

                await IeAssertThrows<JsonlFormatException>(async () =>
                {
                    await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                    {
                        Mode = GraphImportModeEnum.CreateNew,
                        GuidStrategy = GraphImportGuidStrategyEnum.Regenerate,
                        OnError = GraphImportErrorPolicyEnum.Abort
                    }, token).ConfigureAwait(false);
                }, "Malformed line under abort throws JsonlFormatException").ConfigureAwait(false);
            }
            IeCleanup(db);
        }

        private static async Task TestImportMalformedSkip(CancellationToken token)
        {
            string db = IeDbName("malformedskip");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid nodeGuid = Guid.NewGuid();
                string jsonl = "# header\n{\"Type\":\"Node\",\"Object\":{\"GUID\":\"" + nodeGuid + "\",\"Name\":\"ok\"}}\nnot json at all\n";

                GraphImportResult import = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Regenerate,
                    OnError = GraphImportErrorPolicyEnum.Skip
                }, token).ConfigureAwait(false);

                AssertTrue(import.Success, "Skip policy import succeeds despite a bad line");
                AssertEqual(1, import.NodesCreated, "The single good node is imported");
                AssertTrue(import.Warnings.Any(w => w.Contains("Skipped malformed")), "A warning records the skipped line");
            }
            IeCleanup(db);
        }

        private static async Task TestImportMergeMissingTargetThrows(CancellationToken token)
        {
            string db = IeDbName("missingtarget");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                string jsonl = "{\"Type\":\"Node\",\"Object\":{\"GUID\":\"" + Guid.NewGuid() + "\",\"Name\":\"n\"}}\n";

                await IeAssertThrows<ArgumentException>(async () =>
                {
                    await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                    {
                        Mode = GraphImportModeEnum.MergeIntoExisting,
                        TargetGraphGUID = null
                    }, token).ConfigureAwait(false);
                }, "Merge without a target GUID throws ArgumentException").ConfigureAwait(false);
            }
            IeCleanup(db);
        }

        private static async Task TestImportDanglingEdgeDropped(CancellationToken token)
        {
            string db = IeDbName("dangling");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                Guid nodeGuid = Guid.NewGuid();
                Guid missing = Guid.NewGuid();
                StringBuilder sb = new StringBuilder();
                sb.Append("{\"Type\":\"Node\",\"Object\":{\"GUID\":\"" + nodeGuid + "\",\"Name\":\"present\"}}\n");
                sb.Append("{\"Type\":\"Edge\",\"Object\":{\"GUID\":\"" + Guid.NewGuid() + "\",\"From\":\"" + nodeGuid + "\",\"To\":\"" + missing + "\",\"Cost\":0}}\n");

                GraphImportResult import = await client.ImportGraphFromJsonl(tenant, sb.ToString(), new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Preserve,
                    NewGraph = new Graph { GUID = Guid.NewGuid(), TenantGUID = tenant, Name = "Dangling" }
                }, token).ConfigureAwait(false);

                AssertTrue(import.Success, "Import with a dangling edge still succeeds");
                AssertEqual(1, import.NodesCreated, "The present node is imported");
                AssertEqual(0, import.EdgesCreated, "The dangling edge is not written");
                AssertTrue(import.Warnings.Any(w => w.Contains("Dropped edge")), "A warning records the dropped edge");
            }
            IeCleanup(db);
        }

        private static async Task TestImportEmptyBodyCreatesEmptyGraph(CancellationToken token)
        {
            string db = IeDbName("emptybody");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                string jsonl = "# only comments\n# nothing else\n";

                GraphImportResult import = await client.ImportGraphFromJsonl(tenant, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Regenerate,
                    NewGraph = new Graph { GUID = Guid.NewGuid(), TenantGUID = tenant, Name = "Empty" }
                }, token).ConfigureAwait(false);

                AssertTrue(import.Success, "Empty-body create-new succeeds");
                AssertEqual(1, import.GraphsCreated, "An empty graph is created");
                AssertEqual(0, import.NodesCreated, "No nodes are created from an empty body");
                AssertEqual(0, await IeNodeCount(client, tenant, import.GraphGUID, token).ConfigureAwait(false), "The created graph is empty");
            }
            IeCleanup(db);
        }

        #endregion

        #region ImportExport-Helpers

        private static async Task<string> IeFixtureJsonl(string srcDb, CancellationToken token)
        {
            try
            {
                using (LiteGraphClient src = IeNewClient(srcDb))
                {
                    Guid tenant = await IeSeedTenant(src).ConfigureAwait(false);
                    Guid graph = await IeSeedFixture(src, tenant).ConfigureAwait(false);
                    return await src.RenderGraphAsJsonl(tenant, graph, true, true, token).ConfigureAwait(false);
                }
            }
            finally
            {
                IeCleanup(srcDb);
            }
        }

        private static SubgraphExtractionRequest IeReq(Guid tenant, Guid graph, Guid start, int depth, GraphTraversalDirectionEnum direction)
        {
            return new SubgraphExtractionRequest
            {
                TenantGUID = tenant,
                GraphGUID = graph,
                StartNodeGUIDs = new List<Guid> { start },
                MaxDepth = depth,
                Direction = direction,
                IncludeData = true,
                IncludeSubordinates = true
            };
        }

        private static string IeDbName(string suffix)
        {
            return "test-importexport-" + suffix + ".db";
        }

        private static LiteGraphClient IeNewClient(string filename)
        {
            DeleteFileIfExists(filename);
            GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Type = DatabaseTypeEnum.Sqlite,
                Filename = filename
            });
            repo.InitializeRepository();
            return new LiteGraphClient(repo, null, null, null, true);
        }

        private static void IeCleanup(string filename)
        {
            DeleteFileIfExists(filename);
        }

        private static async Task<Guid> IeSeedTenant(LiteGraphClient client)
        {
            Guid tenantGuid = Guid.NewGuid();
            await client.Tenant.Create(new TenantMetadata { GUID = tenantGuid, Name = "ImportExport tenant" }).ConfigureAwait(false);
            return tenantGuid;
        }

        private static async Task<Guid> IeSeedFixture(LiteGraphClient client, Guid tenantGuid)
        {
            Guid graphGuid = Guid.NewGuid();
            await client.Graph.Create(new Graph { GUID = graphGuid, TenantGUID = tenantGuid, Name = "Fixture" }).ConfigureAwait(false);

            await client.Node.Create(IeNode(tenantGuid, graphGuid, _IeNodeA, "A", "root", null, 0)).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, _IeNodeB, "B", "keep", "1", 10)).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, _IeNodeC, "C", "keep", "2", 5)).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, _IeNodeD, "D", "drop", "1", 1)).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, _IeNodeE, "E", "keep", "1", 2)).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, _IeNodeF, "F", null, null, 0)).ConfigureAwait(false);

            await client.Edge.Create(IeEdge(tenantGuid, graphGuid, _IeNodeA, _IeNodeB, 1, "near")).ConfigureAwait(false);
            await client.Edge.Create(IeEdge(tenantGuid, graphGuid, _IeNodeA, _IeNodeC, 5, "far")).ConfigureAwait(false);
            await client.Edge.Create(IeEdge(tenantGuid, graphGuid, _IeNodeB, _IeNodeD, 1, "near")).ConfigureAwait(false);
            await client.Edge.Create(IeEdge(tenantGuid, graphGuid, _IeNodeC, _IeNodeE, 1, "near")).ConfigureAwait(false);
            await client.Edge.Create(IeEdge(tenantGuid, graphGuid, _IeNodeB, _IeNodeC, 2, "far")).ConfigureAwait(false);
            await client.Edge.Create(IeEdge(tenantGuid, graphGuid, _IeNodeF, _IeNodeA, 1, "link")).ConfigureAwait(false);

            return graphGuid;
        }

        private static async Task<Guid> IeSeedTargetGraph(LiteGraphClient client, Guid tenantGuid)
        {
            Guid graphGuid = Guid.NewGuid();
            await client.Graph.Create(new Graph { GUID = graphGuid, TenantGUID = tenantGuid, Name = "Target" }).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, Guid.NewGuid(), "T1", "existing", null, 0)).ConfigureAwait(false);
            await client.Node.Create(IeNode(tenantGuid, graphGuid, Guid.NewGuid(), "T2", "existing", null, 0)).ConfigureAwait(false);
            return graphGuid;
        }

        private static Node IeNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, string name, string? label, string? tier, int score)
        {
            Node node = new Node
            {
                GUID = nodeGuid,
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Name = name,
                Data = new Dictionary<string, object> { { "score", score } }
            };
            if (!String.IsNullOrEmpty(label)) node.Labels = new List<string> { label };
            if (!String.IsNullOrEmpty(tier))
            {
                node.Tags = new NameValueCollection();
                node.Tags.Add("tier", tier);
            }
            return node;
        }

        private static Edge IeEdge(Guid tenantGuid, Guid graphGuid, Guid from, Guid to, int cost, string label)
        {
            return new Edge
            {
                GUID = Guid.NewGuid(),
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                From = from,
                To = to,
                Cost = cost,
                Labels = new List<string> { label }
            };
        }

        private static async Task<int> IeNodeCount(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, CancellationToken token)
        {
            int count = 0;
            await foreach (Node node in client.Node.ReadAllInGraph(tenantGuid, graphGuid, EnumerationOrderEnum.CreatedDescending, 0, false, false, token).ConfigureAwait(false))
            {
                count++;
            }
            return count;
        }

        private static async Task<HashSet<string>> IeCanonicalEdges(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, CancellationToken token)
        {
            Dictionary<Guid, string> names = new Dictionary<Guid, string>();
            await foreach (Node node in client.Node.ReadAllInGraph(tenantGuid, graphGuid, EnumerationOrderEnum.CreatedDescending, 0, false, false, token).ConfigureAwait(false))
            {
                names[node.GUID] = node.Name ?? node.GUID.ToString();
            }

            HashSet<string> edges = new HashSet<string>();
            await foreach (Edge edge in client.Edge.ReadAllInGraph(tenantGuid, graphGuid, EnumerationOrderEnum.CreatedDescending, 0, false, false, token).ConfigureAwait(false))
            {
                string from = names.TryGetValue(edge.From, out string? f) && f != null ? f : edge.From.ToString();
                string to = names.TryGetValue(edge.To, out string? t) && t != null ? t : edge.To.ToString();
                edges.Add(from + "->" + to + ":" + edge.Cost);
            }
            return edges;
        }

        private static async Task IeAssertThrows<TException>(Func<Task> action, string message) where TException : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TException)
            {
                return;
            }
            throw new Exception("Expected " + typeof(TException).Name + ": " + message);
        }

        private static void IeAssertThrowsSync<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new Exception("Expected " + typeof(TException).Name + ": " + message);
        }

        #endregion
    }
}
