namespace LiteGraph.Subgraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extracts a filtered, directional subgraph from a graph using a bounded breadth-first walk.
    /// The walk composes the client's filter-aware traversal primitives so that all sanitization and
    /// validation remain on the existing repository path.
    /// </summary>
    public class SubgraphExtractor
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SubgraphExtractor()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Extract a subgraph as a materialized search result.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="request">Subgraph extraction request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result containing the source graph metadata, the included nodes, and the retained edges.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the client or request is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the graph or a start node does not exist, or no start node is supplied.</exception>
        public async Task<SearchResult> Extract(
            LiteGraphClient client,
            SubgraphExtractionRequest request,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.StartNodeGUIDs == null || request.StartNodeGUIDs.Count < 1)
                throw new ArgumentException("At least one start node GUID is required.");

            token.ThrowIfCancellationRequested();

            Graph graph = await client.Graph.ReadByGuid(request.TenantGUID, request.GraphGUID, token: token).ConfigureAwait(false);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + request.GraphGUID + "' was found.");

            Dictionary<Guid, Node> includedNodes = new Dictionary<Guid, Node>();
            Dictionary<Guid, Edge> includedEdges = new Dictionary<Guid, Edge>();

            HashSet<Guid> allowedNodes = await BuildAllowedNodeSet(client, request, token).ConfigureAwait(false);

            List<Guid> frontier = new List<Guid>();

            foreach (Guid startGuid in request.StartNodeGUIDs.Distinct())
            {
                token.ThrowIfCancellationRequested();
                Node startNode = await client.Node.ReadByGuid(
                    request.TenantGUID, request.GraphGUID, startGuid, request.IncludeData, request.IncludeSubordinates, token).ConfigureAwait(false);
                if (startNode == null)
                    throw new ArgumentException("Start node with GUID '" + startGuid + "' was not found in graph '" + request.GraphGUID + "'.");

                if (!includedNodes.ContainsKey(startGuid))
                {
                    includedNodes.Add(startGuid, startNode);
                    frontier.Add(startGuid);
                }
            }

            for (int depth = 0; depth < request.MaxDepth && frontier.Count > 0; depth++)
            {
                token.ThrowIfCancellationRequested();
                List<Guid> nextFrontier = new List<Guid>();

                foreach (Guid nodeGuid in frontier)
                {
                    token.ThrowIfCancellationRequested();

                    await foreach (Edge edge in EnumerateEdges(client, request, nodeGuid, token).ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();

                        if (request.MaxEdgeCost != null && edge.Cost > request.MaxEdgeCost.Value) continue;

                        Guid neighborGuid = edge.From == nodeGuid ? edge.To : edge.From;

                        if (includedNodes.ContainsKey(neighborGuid))
                        {
                            AddEdge(includedEdges, edge, request.MaxEdges);
                            continue;
                        }

                        if (allowedNodes != null && !allowedNodes.Contains(neighborGuid)) continue;
                        if (request.MaxNodes > 0 && includedNodes.Count >= request.MaxNodes) continue;

                        Node neighbor = await client.Node.ReadByGuid(
                            request.TenantGUID, request.GraphGUID, neighborGuid, request.IncludeData, request.IncludeSubordinates, token).ConfigureAwait(false);
                        if (neighbor == null) continue;

                        includedNodes.Add(neighborGuid, neighbor);
                        nextFrontier.Add(neighborGuid);
                        AddEdge(includedEdges, edge, request.MaxEdges);
                    }
                }

                frontier = nextFrontier;
            }

            SearchResult result = new SearchResult();
            result.Graphs = new List<Graph> { graph };
            result.Nodes = includedNodes.Values.ToList();
            result.Edges = includedEdges.Values
                .Where(e => includedNodes.ContainsKey(e.From) && includedNodes.ContainsKey(e.To))
                .ToList();
            return result;
        }

        /// <summary>
        /// Extract a subgraph as a stream of JSONL records: the source graph record, then node records, then edge records.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="request">Subgraph extraction request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of JSONL records.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the client or request is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the graph or a start node does not exist, or no start node is supplied.</exception>
        public async IAsyncEnumerable<JsonlRecord> ExtractAsRecords(
            LiteGraphClient client,
            SubgraphExtractionRequest request,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            SearchResult result = await Extract(client, request, token).ConfigureAwait(false);

            if (result.Graphs != null)
            {
                foreach (Graph graph in result.Graphs)
                {
                    token.ThrowIfCancellationRequested();
                    yield return JsonlRecord.ForGraph(graph);
                }
            }

            if (result.Nodes != null)
            {
                foreach (Node node in result.Nodes)
                {
                    token.ThrowIfCancellationRequested();
                    yield return JsonlRecord.ForNode(node);
                }
            }

            if (result.Edges != null)
            {
                foreach (Edge edge in result.Edges)
                {
                    token.ThrowIfCancellationRequested();
                    yield return JsonlRecord.ForEdge(edge);
                }
            }
        }

        #endregion

        #region Private-Methods

        private async Task<HashSet<Guid>> BuildAllowedNodeSet(
            LiteGraphClient client,
            SubgraphExtractionRequest request,
            CancellationToken token)
        {
            bool hasNodeFilter =
                (request.NodeLabels != null && request.NodeLabels.Count > 0) ||
                (request.NodeTags != null && request.NodeTags.Count > 0) ||
                request.NodeFilter != null;

            if (!hasNodeFilter) return null;

            HashSet<Guid> allowed = new HashSet<Guid>();

            await foreach (Node node in client.Node.ReadMany(
                request.TenantGUID,
                request.GraphGUID,
                null,
                request.NodeLabels,
                request.NodeTags,
                request.NodeFilter,
                EnumerationOrderEnum.CreatedDescending,
                0,
                false,
                false,
                token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                allowed.Add(node.GUID);
            }

            foreach (Guid startGuid in request.StartNodeGUIDs) allowed.Add(startGuid);

            return allowed;
        }

        private IAsyncEnumerable<Edge> EnumerateEdges(
            LiteGraphClient client,
            SubgraphExtractionRequest request,
            Guid nodeGuid,
            CancellationToken token)
        {
            switch (request.Direction)
            {
                case GraphTraversalDirectionEnum.Outbound:
                    return client.Edge.ReadEdgesFromNode(
                        request.TenantGUID, request.GraphGUID, nodeGuid,
                        request.EdgeLabels, request.EdgeTags, request.EdgeFilter,
                        EnumerationOrderEnum.CreatedDescending, 0, request.IncludeData, request.IncludeSubordinates, token);
                case GraphTraversalDirectionEnum.Inbound:
                    return client.Edge.ReadEdgesToNode(
                        request.TenantGUID, request.GraphGUID, nodeGuid,
                        request.EdgeLabels, request.EdgeTags, request.EdgeFilter,
                        EnumerationOrderEnum.CreatedDescending, 0, request.IncludeData, request.IncludeSubordinates, token);
                default:
                    return client.Edge.ReadNodeEdges(
                        request.TenantGUID, request.GraphGUID, nodeGuid,
                        request.EdgeLabels, request.EdgeTags, request.EdgeFilter,
                        EnumerationOrderEnum.CreatedDescending, 0, request.IncludeData, request.IncludeSubordinates, token);
            }
        }

        private void AddEdge(Dictionary<Guid, Edge> edges, Edge edge, int maxEdges)
        {
            if (edges.ContainsKey(edge.GUID)) return;
            if (maxEdges > 0 && edges.Count >= maxEdges) return;
            edges.Add(edge.GUID, edge);
        }

        #endregion
    }
}
