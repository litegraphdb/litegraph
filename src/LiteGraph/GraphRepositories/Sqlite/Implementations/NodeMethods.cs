namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

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
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            List<Node> created = Converters.NodesFromDataTable(retrieveResult);
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
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(NodeQueries.SelectMany(tenantGuid, graphGuid, labels, tags, nodeFilter, _Repo.SelectBatchSize, skip, order));
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
        public Node ReadByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            DataTable result = _Repo.ExecuteQuery(NodeQueries.Select(tenantGuid, graphGuid, nodeGuid));
            if (result != null && result.Rows.Count == 1)
            {
                Node node = Converters.NodeFromDataRow(result.Rows[0]);
                return node;
            }
            return null;
        }

        /// <inheritdoc />
        public Node Update(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Node updated = Converters.NodeFromDataRow(_Repo.ExecuteQuery(NodeQueries.Update(node), true).Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Repo.ExecuteQuery(NodeQueries.Delete(tenantGuid, graphGuid, nodeGuid), true);
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
            _Repo.ExecuteQuery(NodeQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return (ReadByGuid(tenantGuid, graphGuid, nodeGuid) != null);
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
                        Node parent = ReadByGuid(tenantGuid, graphGuid, edge.From);
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
                        Node child = ReadByGuid(tenantGuid, graphGuid, edge.To);
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
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, null, null, null, _Repo.SelectBatchSize, skip, order));
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
                            Node neighbor = ReadByGuid(tenantGuid, graphGuid, edge.To);
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
                            Node neighbor = ReadByGuid(tenantGuid, graphGuid, edge.From);
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

            Node fromNode = ReadByGuid(tenantGuid, graphGuid, fromNodeGuid);
            if (fromNode == null) throw new ArgumentException("No node with GUID '" + fromNodeGuid + "' exists in graph '" + graphGuid + "'");

            Node toNode = ReadByGuid(tenantGuid, graphGuid, toNodeGuid);
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

                Node nextNode = ReadByGuid(tenantGuid, graph.GUID, nextEdge.To);
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
