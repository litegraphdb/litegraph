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
