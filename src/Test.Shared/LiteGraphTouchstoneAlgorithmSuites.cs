namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Algorithms;

    /// <summary>
    /// Touchstone test cases for graph algorithms and projection export/import.
    /// These run against the shared test graph populated by the core suite.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        #region Private-Methods

        private static async Task RunScoredAlgorithm(GraphAlgorithmTypeEnum type, string label)
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = type }).ConfigureAwait(false);

            AssertTrue(result.NodeCount >= 3, label + " node count");
            AssertTrue(result.Nodes.Count == result.NodeCount, label + " node list matches count");
        }

        private static async Task TestAlgorithmCloseness()
        {
            await RunScoredAlgorithm(GraphAlgorithmTypeEnum.ClosenessCentrality, "Closeness").ConfigureAwait(false);
        }

        private static async Task TestAlgorithmEigenvector()
        {
            await RunScoredAlgorithm(GraphAlgorithmTypeEnum.EigenvectorCentrality, "Eigenvector").ConfigureAwait(false);
        }

        private static async Task TestAlgorithmBetweenness()
        {
            await RunScoredAlgorithm(GraphAlgorithmTypeEnum.BetweennessCentrality, "Betweenness").ConfigureAwait(false);
        }

        private static async Task TestAlgorithmClustering()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.ClusteringCoefficient }).ConfigureAwait(false);

            AssertTrue(result.Nodes.Count == result.NodeCount, "Clustering node list matches count");
            foreach (GraphAlgorithmNodeResult node in result.Nodes)
                AssertTrue(node.Score >= 0d && node.Score <= 1d, "Clustering coefficient in [0,1]");
        }

        private static async Task TestAlgorithmKCore()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.KCore }).ConfigureAwait(false);

            AssertTrue(result.Nodes.Count == result.NodeCount, "k-core node list matches count");
            foreach (GraphAlgorithmNodeResult node in result.Nodes)
                AssertTrue(node.Score >= 0d, "k-core number is non-negative");
        }

        private static async Task TestAlgorithmStronglyConnectedComponents()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.StronglyConnectedComponents }).ConfigureAwait(false);

            AssertTrue(result.CommunityCount != null && result.CommunityCount.Value >= 1, "At least one strongly connected component");
        }

        private static async Task TestAlgorithmLouvain()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.Louvain }).ConfigureAwait(false);

            AssertTrue(result.CommunityCount != null && result.CommunityCount.Value >= 1, "At least one Louvain community");
        }

        private static async Task TestAlgorithmDslCall()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphQueryResult result = await _Client.Query.Execute(
                _TenantGuid,
                _GraphGuid,
                new GraphQueryRequest { Query = "CALL litegraph.algo.pagerank() RETURN guid, score" }).ConfigureAwait(false);

            AssertTrue(result.Rows.Count >= 3, "DSL algorithm returns a row per node");
            AssertTrue(result.Rows[0].ContainsKey("guid") && result.Rows[0].ContainsKey("score"), "DSL algorithm rows carry guid and score");

            double sum = 0d;
            foreach (Dictionary<string, object> row in result.Rows)
                if (row.TryGetValue("score", out object? score) && score is double value) sum += value;
            AssertTrue(Math.Abs(sum - 1.0) < 0.0001, "DSL PageRank scores sum to 1");
        }

        private static async Task TestAlgorithmResultCache()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            TenantMetadata tenant = await _Client.Tenant.Create(new TenantMetadata { Name = "AlgoCacheTenant" }).ConfigureAwait(false);
            Graph graph = await _Client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "AlgoCacheGraph" }).ConfigureAwait(false);
            Node a = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "A" }).ConfigureAwait(false);
            Node b = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "B" }).ConfigureAwait(false);
            await _Client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = a.GUID, To = b.GUID }).ConfigureAwait(false);
            await _Client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = b.GUID, To = a.GUID }).ConfigureAwait(false);

            GraphAlgorithmResult first = await _Client.Algorithm.Run(tenant.GUID, graph.GUID,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.PageRank, UseCache = true }).ConfigureAwait(false);
            AssertTrue(!first.FromCache, "First cached run is computed");

            GraphAlgorithmResult second = await _Client.Algorithm.Run(tenant.GUID, graph.GUID,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.PageRank, UseCache = true }).ConfigureAwait(false);
            AssertTrue(second.FromCache, "Second run served from cache");

            await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "C" }).ConfigureAwait(false);
            GraphAlgorithmResult third = await _Client.Algorithm.Run(tenant.GUID, graph.GUID,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.PageRank, UseCache = true }).ConfigureAwait(false);
            AssertTrue(!third.FromCache && third.NodeCount == 3, "Cache invalidated on node-count change");

            int removed = _Client.Algorithm.InvalidateCache(graph.GUID);
            AssertTrue(removed >= 0, "Explicit invalidate returns a count");

            await _Client.Tenant.DeleteByGuid(tenant.GUID, force: true).ConfigureAwait(false);
        }

        private static async Task TestMcpAlgorithmRun()
        {
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");
            if (_McpTestGraphGuid == Guid.Empty) throw new InvalidOperationException("MCP test graph not initialized");

            string json = await _McpClient.CallAsync<string>("algorithm/run", new
            {
                tenantGuid = _McpTestTenantGuid,
                graphGuid = _McpTestGraphGuid,
                algorithmType = "PageRank"
            }).ConfigureAwait(false);

            AssertTrue(!String.IsNullOrEmpty(json), "MCP algorithm/run returns a body");
            AssertTrue(json.Contains("Nodes") && json.Contains("PageRank"), "MCP algorithm/run returns a PageRank result");
        }

        private static async Task TestMcpAlgorithmExport()
        {
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");
            if (_McpTestGraphGuid == Guid.Empty) throw new InvalidOperationException("MCP test graph not initialized");

            string text = await _McpClient.CallAsync<string>("algorithm/export", new
            {
                tenantGuid = _McpTestTenantGuid,
                graphGuid = _McpTestGraphGuid,
                format = "NodeLinkJson",
                attributes = "Meta"
            }).ConfigureAwait(false);

            AssertTrue(text.Contains("\"nodes\"") && text.Contains("\"links\""), "MCP algorithm/export returns node-link JSON");
        }

        private static async Task TestMcpAlgorithmImport()
        {
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");
            if (_McpTestNode1Guid == Guid.Empty) throw new InvalidOperationException("MCP test node not initialized");

            Dictionary<string, Dictionary<string, double>> values = new Dictionary<string, Dictionary<string, double>>
            {
                [_McpTestNode1Guid.ToString()] = new Dictionary<string, double> { ["mcp_import"] = 0.5d }
            };

            string json = await _McpClient.CallAsync<string>("algorithm/import", new
            {
                tenantGuid = _McpTestTenantGuid,
                graphGuid = _McpTestGraphGuid,
                request = new { Values = values }
            }).ConfigureAwait(false);

            AssertTrue(!String.IsNullOrEmpty(json) && json.Contains("NodesUpdated"), "MCP algorithm/import returns an update count");
        }

        private static async Task TestAlgorithmDegreeCentrality()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.DegreeCentrality }).ConfigureAwait(false);

            AssertTrue(result.NodeCount >= 3, "Degree node count");
            AssertTrue(result.Nodes.Count == result.NodeCount, "Degree node list matches count");
        }

        private static async Task TestAlgorithmPageRank()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.PageRank }).ConfigureAwait(false);

            double sum = 0d;
            foreach (GraphAlgorithmNodeResult node in result.Nodes) sum += node.Score;
            AssertTrue(result.NodeCount == 0 || Math.Abs(sum - 1.0) < 0.0001, "PageRank scores sum to 1");
        }

        private static async Task TestAlgorithmComponents()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmResult result = await _Client.Algorithm.Run(
                _TenantGuid,
                _GraphGuid,
                new GraphAlgorithmRequest { AlgorithmType = GraphAlgorithmTypeEnum.WeaklyConnectedComponents }).ConfigureAwait(false);

            AssertTrue(result.CommunityCount != null && result.CommunityCount.Value >= 1, "At least one component");
        }

        private static async Task TestAlgorithmExportProjection()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            using (MemoryStream stream = new MemoryStream())
            {
                await _Client.Algorithm.ExportGraph(
                    _TenantGuid,
                    _GraphGuid,
                    GraphExportFormatEnum.NodeLinkJson,
                    GraphExportAttributeLevelEnum.Meta,
                    stream).ConfigureAwait(false);

                string json = Encoding.UTF8.GetString(stream.ToArray());
                AssertTrue(json.Contains("\"nodes\""), "Export contains nodes array");
                AssertTrue(json.Contains("\"links\""), "Export contains links array");
            }
        }

        private static async Task TestAlgorithmImportResults()
        {
            if (_Client == null) throw new InvalidOperationException("Client is null");

            GraphAlgorithmImportRequest request = new GraphAlgorithmImportRequest();
            request.Values[_Node1Guid] = new Dictionary<string, double> { ["imported_score"] = 0.42d };

            int updated = await _Client.Algorithm.ImportResults(_TenantGuid, _GraphGuid, request).ConfigureAwait(false);
            AssertTrue(updated >= 1, "Import updated at least one node");

            Node? node = await _Client.Node.ReadByGuid(_TenantGuid, _GraphGuid, _Node1Guid, true, false).ConfigureAwait(false);
            string data = node != null && node.Data != null ? node.Data.ToString() ?? String.Empty : String.Empty;
            AssertTrue(data.Contains("imported_score"), "Imported value present on node data");
        }

        #endregion
    }
}
