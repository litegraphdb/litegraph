namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Node methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class NodeMethods : INodeMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Node methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public NodeMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Node> Create(Node node, CancellationToken token = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            token.ThrowIfCancellationRequested();

            string createQuery = NodeQueries.Insert(node);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            Node created = Converters.NodeFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<Node>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Node> nodes, CancellationToken token = default)
        {
            if (nodes == null || nodes.Count < 1) return new List<Node>();
            token.ThrowIfCancellationRequested();

            foreach (Node node in nodes)
            {
                node.TenantGUID = tenantGuid;
                node.GraphGUID = graphGuid;
            }

            string insertQuery = NodeQueries.InsertMany(tenantGuid, nodes);
            string retrieveQuery = NodeQueries.SelectMany(tenantGuid, nodes.Select(n => n.GUID).ToList());

            DataTable createResult = await _Repo.ExecuteQueryAsync(insertQuery, true, token).ConfigureAwait(false);
            DataTable retrieveResult = await _Repo.ExecuteQueryAsync(retrieveQuery, true, token).ConfigureAwait(false);
            List<Node> created = Converters.NodesFromDataTable(retrieveResult);

            List<VectorMetadata> allVectors = new List<VectorMetadata>();
            foreach (Node node in nodes)
            {
                if (node.Vectors != null && node.Vectors.Count > 0)
                {
                    allVectors.AddRange(node.Vectors);
                }
            }

            if (allVectors.Count > 0)
            {
                await VectorMethodsIndexExtensions.UpdateIndexForCreateManyAsync(_Repo, allVectors).ConfigureAwait(false);
            }

            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectMany(
                    tenantGuid,
                    graphGuid,
                    name,
                    labels,
                    tags,
                    nodeFilter,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<Node> ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectMany(
                tenantGuid,
                graphGuid,
                name,
                labels,
                tags,
                nodeFilter,
                1,
                0,
                order), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.NodeFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectMostConnected(tenantGuid, graphGuid, labels, tags, nodeFilter, _Repo.SelectBatchSize, skip), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectLeastConnected(tenantGuid, graphGuid, labels, tags, nodeFilter, _Repo.SelectBatchSize, skip), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<Node> ReadByGuid(Guid tenantGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectByGuid(tenantGuid, nodeGuid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1)
            {
                Node node = Converters.NodeFromDataRow(result.Rows[0]);
                return node;
            }
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.NodeFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Node>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            Node marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null && query.GraphGUID != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Node> ret = new EnumerationResult<Node>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = await GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, null, token).ConfigureAwait(false);

            if (ret.TotalRecords < 1)
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
                ret.RecordsRemaining = 0;
                ret.Timestamp.End = DateTime.UtcNow;
                return ret;
            }
            else
            {
                DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.Labels,
                    query.Tags,
                    query.Expr,
                    query.MaxResults,
                    query.Skip,
                    query.Ordering,
                    marker), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1)
                {
                    ret.ContinuationToken = null;
                    ret.EndOfResults = true;
                    ret.RecordsRemaining = 0;
                    ret.Timestamp.End = DateTime.UtcNow;
                    return ret;
                }
                else
                {
                    ret.Objects = Converters.NodesFromDataTable(result);

                    Node lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);

                    if (ret.RecordsRemaining > 0)
                    {
                        ret.ContinuationToken = lastItem.GUID;
                        ret.EndOfResults = false;
                        ret.Timestamp.End = DateTime.UtcNow;
                        return ret;
                    }
                    else
                    {
                        ret.ContinuationToken = null;
                        ret.EndOfResults = true;
                        ret.Timestamp.End = DateTime.UtcNow;
                        return ret;
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Node marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            string query = NodeQueries.GetRecordCount(
                tenantGuid,
                graphGuid,
                labels,
                tags,
                filter,
                order,
                marker);

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result != null && result.Rows != null && result.Rows.Count > 0)
            {
                if (result.Columns.Contains("record_count"))
                {
                    int ret = Convert.ToInt32(result.Rows[0]["record_count"]);
                    return ret;
                }
            }

            return 0;
        }

        /// <inheritdoc />
        public async Task<Node> Update(Node node, CancellationToken token = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(NodeQueries.Update(node), true, token).ConfigureAwait(false);
            Node updated = Converters.NodeFromDataRow(result.Rows[0]);    
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            // Remove from database
            await _Repo.ExecuteQueryAsync(NodeQueries.Delete(tenantGuid, graphGuid, nodeGuid), true, token).ConfigureAwait(false);

            // Update vector index if needed
            await VectorMethodsIndexExtensions.UpdateIndexForDeleteAsync(_Repo, tenantGuid, nodeGuid, graphGuid).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(NodeQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(NodeQueries.DeleteAllInGraph(tenantGuid, graphGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            token.ThrowIfCancellationRequested();

            // Remove from database
            await _Repo.ExecuteQueryAsync(NodeQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids), true, token).ConfigureAwait(false);

            // Update vector index if needed
            await VectorMethodsIndexExtensions.UpdateIndexForDeleteManyAsync(_Repo, tenantGuid, nodeGuids, graphGuid).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Node node = await ReadByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
            return (node != null);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, null, null, null, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.To.Equals(nodeGuid))
                    {
                        Node parent = await ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                        if (parent != null) yield return parent;
                        else _Repo.Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                    }

                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadChildren(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, null, null, null, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.From.Equals(nodeGuid))
                    {
                        Node child = await ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                        if (child != null) yield return child;
                        else _Repo.Logging.Log(SeverityEnum.Warn, "node " + edge.To + " referenced in graph " + graphGuid + " but does not exist");
                    }

                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadNeighbors(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            List<Guid> visited = new List<Guid>();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectConnected(
                    tenantGuid,
                    graphGuid,
                    nodeGuid,
                    null,
                    null,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.From.Equals(nodeGuid))
                    {
                        if (visited.Contains(edge.To))
                        {
                            skip++;
                            continue;
                        }
                        else
                        {
                            Node neighbor = await ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                            if (neighbor != null)
                            {
                                visited.Add(edge.To);
                                yield return neighbor;
                            }
                            else _Repo.Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                            skip++;
                        }
                    }
                    if (edge.To.Equals(nodeGuid))
                    {
                        if (visited.Contains(edge.From))
                        {
                            skip++;
                            continue;
                        }
                        else
                        {
                            Node neighbor = await ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                            if (neighbor != null)
                            {
                                visited.Add(edge.From);
                                yield return neighbor;
                            }
                            else _Repo.Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                            skip++;
                        }
                    }
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<RouteDetail> ReadRoutes(
            SearchTypeEnum searchType,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr edgeFilter = null,
            Expr nodeFilter = null,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            #region Retrieve-Objects

            Graph graph = await _Repo.Graph.ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");

            Node fromNode = await ReadByGuid(tenantGuid, fromNodeGuid, token).ConfigureAwait(false);
            if (fromNode == null) throw new ArgumentException("No node with GUID '" + fromNodeGuid + "' exists in graph '" + graphGuid + "'");

            Node toNode = await ReadByGuid(tenantGuid, toNodeGuid, token).ConfigureAwait(false);
            if (toNode == null) throw new ArgumentException("No node with GUID '" + toNodeGuid + "' exists in graph '" + graphGuid + "'");

            #endregion

            #region Perform-Search

            if (searchType == SearchTypeEnum.DepthFirstSearch)
            {
                await foreach (RouteDetail route in GetRoutesDfs(
                    tenantGuid,
                    graph,
                    fromNode,
                    toNode,
                    edgeFilter,
                    nodeFilter,
                    new List<Node> { fromNode },
                    new List<Edge>(),
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    if (route != null) yield return route;
                }
            }
            else
            {
                throw new ArgumentException("Unknown search type '" + searchType.ToString() + ".");
            }

            #endregion
        }

        #endregion

        #region Private-Methods

        private async IAsyncEnumerable<RouteDetail> GetRoutesDfs(
           Guid tenantGuid,
           Graph graph,
           Node start,
           Node end,
           Expr edgeFilter,
           Expr nodeFilter,
           List<Node> visitedNodes,
           List<Edge> visitedEdges,
           [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            #region Get-Edges

            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Repo.Edge.ReadEdgesFromNode(
                tenantGuid,
                graph.GUID,
                start.GUID,
                null,
                null,
                edgeFilter,
                EnumerationOrderEnum.CreatedDescending,
                token: token).WithCancellation(token).ConfigureAwait(false))
            {
                edges.Add(edge);
            }

            #endregion

            #region Process-Each-Edge

            for (int i = 0; i < edges.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                Edge nextEdge = edges[i];

                #region Retrieve-Next-Node

                Node nextNode = await ReadByGuid(tenantGuid, nextEdge.To, token).ConfigureAwait(false);
                if (nextNode == null)
                {
                    _Repo.Logging.Log(SeverityEnum.Warn, "node " + nextEdge.To + " referenced in graph " + graph.GUID + " but does not exist");
                    continue;
                }

                #endregion

                #region Check-for-End

                if (nextNode.GUID.Equals(end.GUID))
                {
                    RouteDetail routeDetail = new RouteDetail();
                    routeDetail.Edges = new List<Edge>(visitedEdges);
                    routeDetail.Edges.Add(nextEdge);
                    yield return routeDetail;
                    continue;
                }

                #endregion

                #region Check-for-Cycles

                if (visitedNodes.Any(n => n.GUID.Equals(nextEdge.To))) continue; // cycle

                #endregion

                #region Recursion-and-Variables

                List<Node> childVisitedNodes = new List<Node>(visitedNodes);
                List<Edge> childVisitedEdges = new List<Edge>(visitedEdges);

                childVisitedNodes.Add(nextNode);
                childVisitedEdges.Add(nextEdge);

                await foreach (RouteDetail route in GetRoutesDfs(tenantGuid, graph, nextNode, end, edgeFilter, nodeFilter, childVisitedNodes, childVisitedEdges, token).WithCancellation(token).ConfigureAwait(false))
                {
                    if (route != null) yield return route;
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}
