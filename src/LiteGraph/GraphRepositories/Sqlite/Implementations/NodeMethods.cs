namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Indexing.Vector;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
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
        public Node Create(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            string createQuery = NodeQueries.Insert(node);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            Node created = Converters.NodeFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public List<Node> CreateMany(Guid tenantGuid, Guid graphGuid, List<Node> nodes)
        {
            if (nodes == null || nodes.Count < 1) return new List<Node>();

            foreach (Node node in nodes)
            {
                node.TenantGUID = tenantGuid;
                node.GraphGUID = graphGuid;
            }

            string insertQuery = NodeQueries.InsertMany(tenantGuid, nodes);
            string retrieveQuery = NodeQueries.SelectMany(tenantGuid, nodes.Select(n => n.GUID).ToList());

            // Execute the entire batch with BEGIN/COMMIT and multi-row INSERTs
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            List<Node> created = Converters.NodesFromDataTable(retrieveResult);

            // Update HNSW index for any vectors that were created with the nodes
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
                // Update vector indices asynchronously
                Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForCreateManyAsync(_Repo, allVectors)).Wait();
            }

            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectMany(
                    tenantGuid,
                    graphGuid,
                    name,
                    labels,
                    tags,
                    nodeFilter,
                    _Repo.SelectBatchSize,
                    skip,
                    order));

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public Node ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectMany(
                tenantGuid,
                graphGuid,
                name,
                labels,
                tags,
                nodeFilter,
                1,
                0,
                order));

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.NodeFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectMostConnected(tenantGuid, graphGuid, labels, tags, nodeFilter, _Repo.SelectBatchSize, skip));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectLeastConnected(tenantGuid, graphGuid, labels, tags, nodeFilter, _Repo.SelectBatchSize, skip));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);
                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public Node ReadByGuid(Guid tenantGuid, Guid nodeGuid)
        {
            DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectByGuid(tenantGuid, nodeGuid));
            if (result != null && result.Rows.Count == 1)
            {
                Node node = Converters.NodeFromDataRow(result.Rows[0]);
                return node;
            }
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectByGuids(tenantGuid, guids));

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                yield return Converters.NodeFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public EnumerationResult<Node> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            Node marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null && query.GraphGUID != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Node> ret = new EnumerationResult<Node>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, query.ContinuationToken);

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
                DataTable result = _Repo.ExecuteQuery(NodeQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.Labels,
                    query.Tags,
                    query.Expr,
                    query.MaxResults,
                    query.Skip,
                    query.Ordering,
                    marker));

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

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID);

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
        public int GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null)
        {
            Node marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
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

            DataTable result = _Repo.ExecuteQuery(query);

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
        public Node Update(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Node updated = Converters.NodeFromDataRow(_Repo.ExecuteQuery(NodeQueries.Update(node), true).Rows[0]);
            // Populate the vectors from the database after the update operation
            updated.Vectors = _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID).ToList();

            // Update HNSW index for any vectors that were updated
            if (updated.Vectors != null && updated.Vectors.Count > 0)
            {
                Task.Run(async () =>
                {
                    Graph graph = _Repo.Graph.ReadByGuid(node.TenantGUID, node.GraphGUID);
                    if (graph != null && graph.VectorIndexType.HasValue && graph.VectorIndexType != VectorIndexTypeEnum.None)
                    {

                        VectorIndexStatistics stats = _Repo.VectorIndexManager.GetStatistics(node.GraphGUID);
                        if (stats != null && stats.VectorCount > 0)
                        {
                            foreach (VectorMetadata vector in updated.Vectors)
                            {
                                if (vector.Vectors != null && vector.Vectors.Count > 0)
                                {
                                    await VectorMethodsIndexExtensions.UpdateIndexForUpdateAsync(_Repo, vector);
                                }
                            }
                        }
                        else
                        {
                            _Repo.Logging.Log(SeverityEnum.Warn, $"Vector index for graph {node.GraphGUID} is empty or invalid, rebuilding...");
                            List<VectorMetadata> allGraphVectors = _Repo.Vector.ReadAllInGraph(node.TenantGUID, node.GraphGUID).ToList();
                            await _Repo.VectorIndexManager.RebuildIndexAsync(graph, allGraphVectors);
                        }
                    }

                }).Wait();
            }
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            // Remove from database
            _Repo.ExecuteQuery(NodeQueries.Delete(tenantGuid, graphGuid, nodeGuid), true);

            // Update vector index if needed
            Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForDeleteAsync(_Repo, tenantGuid, nodeGuid, graphGuid)).Wait();
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(NodeQueries.DeleteAllInTenant(tenantGuid), true);
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(NodeQueries.DeleteAllInGraph(tenantGuid, graphGuid), true);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;

            // Remove from database
            _Repo.ExecuteQuery(NodeQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids), true);

            // Update vector index if needed
            Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForDeleteManyAsync(_Repo, tenantGuid, nodeGuids, graphGuid)).Wait();
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid nodeGuid)
        {
            return (ReadByGuid(tenantGuid, nodeGuid) != null);
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, null, null, null, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.To.Equals(nodeGuid))
                    {
                        Node parent = ReadByGuid(tenantGuid, edge.From);
                        if (parent != null) yield return parent;
                        else _Repo.Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                    }

                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadChildren(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, null, null, null, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.From.Equals(nodeGuid))
                    {
                        Node child = ReadByGuid(tenantGuid, edge.To);
                        if (child != null) yield return child;
                        else _Repo.Logging.Log(SeverityEnum.Warn, "node " + edge.To + " referenced in graph " + graphGuid + " but does not exist");
                    }

                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadNeighbors(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            List<Guid> visited = new List<Guid>();

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectConnected(
                    tenantGuid,
                    graphGuid,
                    nodeGuid,
                    null,
                    null,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order));

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
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
                            Node neighbor = ReadByGuid(tenantGuid, edge.To);
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
                            Node neighbor = ReadByGuid(tenantGuid, edge.From);
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
        public IEnumerable<RouteDetail> ReadRoutes(
            SearchTypeEnum searchType,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr edgeFilter = null,
            Expr nodeFilter = null)
        {
            #region Retrieve-Objects

            Graph graph = _Repo.Graph.ReadByGuid(tenantGuid, graphGuid);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");

            Node fromNode = ReadByGuid(tenantGuid, fromNodeGuid);
            if (fromNode == null) throw new ArgumentException("No node with GUID '" + fromNodeGuid + "' exists in graph '" + graphGuid + "'");

            Node toNode = ReadByGuid(tenantGuid, toNodeGuid);
            if (toNode == null) throw new ArgumentException("No node with GUID '" + toNodeGuid + "' exists in graph '" + graphGuid + "'");

            #endregion

            #region Perform-Search

            RouteDetail routeDetail = new RouteDetail();

            if (searchType == SearchTypeEnum.DepthFirstSearch)
            {
                foreach (RouteDetail route in GetRoutesDfs(
                    tenantGuid,
                    graph,
                    fromNode,
                    toNode,
                    edgeFilter,
                    nodeFilter,
                    new List<Node> { fromNode },
                    new List<Edge>()))
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

        private IEnumerable<RouteDetail> GetRoutesDfs(
           Guid tenantGuid,
           Graph graph,
           Node start,
           Node end,
           Expr edgeFilter,
           Expr nodeFilter,
           List<Node> visitedNodes,
           List<Edge> visitedEdges)
        {
            #region Get-Edges

            List<Edge> edges = _Repo.Edge.ReadEdgesFromNode(
                tenantGuid,
                graph.GUID,
                start.GUID,
                null,
                null,
                edgeFilter,
                EnumerationOrderEnum.CreatedDescending).ToList();

            #endregion

            #region Process-Each-Edge

            for (int i = 0; i < edges.Count; i++)
            {
                Edge nextEdge = edges[i];

                #region Retrieve-Next-Node

                Node nextNode = ReadByGuid(tenantGuid, nextEdge.To);
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

                IEnumerable<RouteDetail> recurse = GetRoutesDfs(tenantGuid, graph, nextNode, end, edgeFilter, nodeFilter, childVisitedNodes, childVisitedEdges);
                foreach (RouteDetail route in recurse)
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
