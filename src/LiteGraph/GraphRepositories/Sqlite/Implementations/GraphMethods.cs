namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Graph methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class GraphMethods : IGraphMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Graph methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public GraphMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public Graph Create(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            string createQuery = GraphQueries.Insert(graph);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            Graph created = Converters.GraphFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);
                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectMany(
                    tenantGuid,
                    name,
                    labels,
                    tags,
                    graphFilter,
                    _Repo.SelectBatchSize,
                    skip,
                    order));

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);
                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public Graph ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectMany(
                tenantGuid,
                name,
                labels,
                tags,
                graphFilter,
                1,
                0,
                order));

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.GraphFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public Graph ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectByGuid(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1)
            {
                Graph graph = Converters.GraphFromDataRow(result.Rows[0]);
                return graph;
            }
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectByGuids(tenantGuid, guids));

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                yield return Converters.GraphFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public EnumerationResult<Graph> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            Graph marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Graph> ret = new EnumerationResult<Graph>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.Labels, query.Tags, query.Expr, query.Ordering, query.ContinuationToken);

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
                DataTable result = _Repo.ExecuteQuery(GraphQueries.GetRecordPage(
                    query.TenantGUID,
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
                    ret.Objects = Converters.GraphsFromDataTable(result);

                    Graph lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID);

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
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null)
        {
            Graph marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(GraphQueries.GetRecordCount(
                tenantGuid,
                labels,
                tags,
                filter,
                order,
                marker));

            if (result != null && result.Rows != null && result.Rows.Count > 0)
            {
                if (result.Columns.Contains("record_count"))
                {
                    return Convert.ToInt32(result.Rows[0]["record_count"]);
                }
            }
            return 0;
        }

        /// <inheritdoc />
        public Graph Update(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            Graph updated = Converters.GraphFromDataRow(_Repo.ExecuteQuery(GraphQueries.Update(graph), true).Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(GraphQueries.DeleteAllInTenant(tenantGuid), true);
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(GraphQueries.Delete(tenantGuid, graphGuid), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid graphGuid)
        {
            return (ReadByGuid(tenantGuid, graphGuid) != null);
        }

        /// <inheritdoc />
        public Dictionary<Guid, GraphStatistics> GetStatistics(Guid tenantGuid)
        {
            Dictionary<Guid, GraphStatistics> ret = new Dictionary<Guid, GraphStatistics>();
            DataTable table = _Repo.ExecuteQuery(GraphQueries.GetStatistics(tenantGuid, null), true);
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    Guid graphGuid = Guid.Parse(row["guid"].ToString());

                    GraphStatistics stats = new GraphStatistics
                    {
                        Nodes = Convert.ToInt32(row["nodes"]),
                        Edges = Convert.ToInt32(row["edges"]),
                        Labels = Convert.ToInt32(row["labels"]),
                        Tags = Convert.ToInt32(row["tags"]),
                        Vectors = Convert.ToInt32(row["vectors"])
                    };

                    ret[graphGuid] = stats;
                }
            }
            return ret;
        }

        /// <inheritdoc />
        public GraphStatistics GetStatistics(Guid tenantGuid, Guid guid)
        {
            DataTable table = _Repo.ExecuteQuery(GraphQueries.GetStatistics(tenantGuid, guid), true);
            if (table != null && table.Rows.Count > 0) return Converters.GraphStatisticsFromDataRow(table.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async Task EnableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            VectorIndexConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            Graph graph = ReadByGuid(tenantGuid, graphGuid);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            // Apply configuration to the graph object
            configuration.ApplyToGraph(graph);

            // Get all existing vectors in the graph before enabling indexing
            List<VectorMetadata> existingVectors = _Repo.Vector.ReadAllInGraph(tenantGuid, graphGuid).ToList();

            // Enable indexing using the index manager
            await _Repo.VectorIndexManager.EnableIndexingAsync(graph, configuration.VectorIndexType, configuration.VectorIndexFile);

            // If there are existing vectors, populate the index
            if (existingVectors.Count > 0)
            {
                await _Repo.VectorIndexManager.RebuildIndexAsync(graph, existingVectors);
            }

            // Update the graph in the database with all configuration values
            _Repo.ExecuteQuery(GraphQueries.Update(graph), true);
        }

        /// <inheritdoc />
        public async Task DisableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            bool deleteIndexFile = false)
        {
            Graph graph = ReadByGuid(tenantGuid, graphGuid);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            // Disable indexing using the index manager
            await _Repo.VectorIndexManager.DisableIndexingAsync(graphGuid, deleteIndexFile);

            // Apply disabled configuration to clear all vector index settings
            VectorIndexConfiguration disabledConfig = VectorIndexConfiguration.CreateDisabled();
            disabledConfig.ApplyToGraph(graph);

            // Update graph in database
            _Repo.ExecuteQuery(GraphQueries.Update(graph), true);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndexAsync(
            Guid tenantGuid,
            Guid graphGuid)
        {
            Graph graph = ReadByGuid(tenantGuid, graphGuid);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                throw new InvalidOperationException("Graph does not have indexing enabled.");

            // Get all vectors for the graph (including nodes and edges)
            IEnumerable<VectorMetadata> vectors = _Repo.Vector.ReadAllInGraph(tenantGuid, graphGuid);

            // Rebuild the index
            await _Repo.VectorIndexManager.RebuildIndexAsync(graph, vectors);
        }

        /// <inheritdoc />
        public VectorIndexStatistics GetVectorIndexStatistics(
            Guid tenantGuid,
            Guid graphGuid)
        {
            Graph graph = ReadByGuid(tenantGuid, graphGuid);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            return _Repo.VectorIndexManager.GetStatistics(graphGuid);
        }

        /// <inheritdoc />
        public SearchResult GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));

            SearchResult result = new SearchResult
            {
                Nodes = new List<Node>(),
                Edges = new List<Edge>(),
                Graphs = new List<Graph>()
            };

            Graph graph = ReadByGuid(tenantGuid, graphGuid);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");
            result.Graphs.Add(graph);

            Node startingNode = _Repo.Node.ReadByGuid(tenantGuid, nodeGuid);
            if (startingNode == null) throw new ArgumentException("No node with GUID '" + nodeGuid + "' exists in graph '" + graphGuid + "'");
            if (startingNode.GraphGUID != graphGuid) throw new ArgumentException("Node '" + nodeGuid + "' does not belong to graph '" + graphGuid + "'");

            if (maxDepth == 0)
            {
                result.Nodes.Add(startingNode);
                return result;
            }

            HashSet<Guid> visitedNodes = new HashSet<Guid>();
            HashSet<Guid> visitedEdges = new HashSet<Guid>();
            Dictionary<Guid, int> pendingNeighborDepths = new Dictionary<Guid, int>();
            Queue<(Node node, int depth)> nodeQueue = new Queue<(Node, int)>();

            nodeQueue.Enqueue((startingNode, 0));
            visitedNodes.Add(startingNode.GUID);
            result.Nodes.Add(startingNode);

            bool nodesThresholdReached = (maxNodes > 0 && result.Nodes.Count >= maxNodes);
            bool edgesThresholdReached = (maxEdges > 0 && result.Edges.Count >= maxEdges);

            while ((nodeQueue.Count > 0 || pendingNeighborDepths.Count > 0) && !nodesThresholdReached && !edgesThresholdReached)
            {
                if (pendingNeighborDepths.Count > 0 && (nodeQueue.Count == 0 || pendingNeighborDepths.Count >= 10))
                {
                    List<Guid> neighborGuidsToLoad = pendingNeighborDepths.Keys.Where(guid => !visitedNodes.Contains(guid)).ToList();
                    if (neighborGuidsToLoad.Count > 0)
                    {
                        Dictionary<Guid, Node> loadedNodes = _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad)
                            .Where(n => n.GraphGUID == graphGuid)
                            .ToDictionary(n => n.GUID, n => n);

                        foreach (Guid neighborGuid in neighborGuidsToLoad)
                        {
                            if (loadedNodes.TryGetValue(neighborGuid, out Node neighbor))
                            {
                                visitedNodes.Add(neighborGuid);
                                result.Nodes.Add(neighbor);

                                if (maxNodes > 0 && result.Nodes.Count >= maxNodes)
                                {
                                    nodesThresholdReached = true;
                                    break;
                                }

                                if (!nodesThresholdReached && pendingNeighborDepths.TryGetValue(neighborGuid, out int neighborDepth))
                                    if (neighborDepth <= maxDepth)
                                        nodeQueue.Enqueue((neighbor, neighborDepth));
                            }
                            else
                                _Repo.Logging.Log(SeverityEnum.Warn, "node " + neighborGuid + " referenced in graph " + graphGuid + " but does not exist");
                        }
                        pendingNeighborDepths.Clear();
                    }
                }

                if (nodesThresholdReached || edgesThresholdReached) break;
                if (nodeQueue.Count == 0) continue;

                (Node currentNode, int currentDepth) = nodeQueue.Dequeue();
                if (currentDepth >= maxDepth) continue;

                IEnumerable<Edge> connectedEdges = _Repo.Edge.ReadNodeEdges(
                    tenantGuid,
                    graphGuid,
                    currentNode.GUID);

                foreach (Edge edge in connectedEdges)
                {
                    if (maxEdges > 0 && result.Edges.Count >= maxEdges)
                    {
                        edgesThresholdReached = true;
                        break;
                    }

                    if (visitedEdges.Contains(edge.GUID)) continue;

                    Guid neighborGuid;
                    if (edge.From.Equals(currentNode.GUID))
                        neighborGuid = edge.To;
                    else
                        neighborGuid = edge.From;

                    bool needNewNode = !visitedNodes.Contains(neighborGuid);
                    int neighborDepth = currentDepth + 1;

                    if (needNewNode && neighborDepth > maxDepth)
                        continue;

                    if (needNewNode && maxNodes > 0 && result.Nodes.Count >= maxNodes)
                    {
                        nodesThresholdReached = true;
                        continue;
                    }

                    visitedEdges.Add(edge.GUID);
                    result.Edges.Add(edge);

                    if (needNewNode && neighborDepth <= maxDepth && !pendingNeighborDepths.ContainsKey(neighborGuid))
                        pendingNeighborDepths[neighborGuid] = neighborDepth;
                }
            }

            if (pendingNeighborDepths.Count > 0 && !nodesThresholdReached)
            {
                List<Guid> neighborGuidsToLoad = pendingNeighborDepths.Keys.Where(guid => !visitedNodes.Contains(guid)).ToList();
                if (neighborGuidsToLoad.Count > 0)
                {
                    Dictionary<Guid, Node> loadedNodes = _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad)
                        .Where(n => n.GraphGUID == graphGuid)
                        .ToDictionary(n => n.GUID, n => n);

                    foreach (Guid neighborGuid in neighborGuidsToLoad)
                    {
                        if (loadedNodes.TryGetValue(neighborGuid, out Node neighbor))
                        {
                            visitedNodes.Add(neighborGuid);
                            result.Nodes.Add(neighbor);
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public GraphStatistics GetSubgraphStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));

            Node startingNode = _Repo.Node.ReadByGuid(tenantGuid, nodeGuid);
            if (startingNode == null) throw new ArgumentException("No node with GUID '" + nodeGuid + "' exists in graph '" + graphGuid + "'");
            if (startingNode.GraphGUID != graphGuid) throw new ArgumentException("Node '" + nodeGuid + "' does not belong to graph '" + graphGuid + "'");

            HashSet<Guid> visitedNodes = new HashSet<Guid>();
            HashSet<Guid> visitedEdges = new HashSet<Guid>();
            Dictionary<Guid, int> pendingNeighborDepths = new Dictionary<Guid, int>();

            Queue<(Guid nodeGuid, int depth)> nodeQueue = new Queue<(Guid, int)>();
            nodeQueue.Enqueue((nodeGuid, 0));
            visitedNodes.Add(nodeGuid);

            bool nodesThresholdReached = (maxNodes > 0 && visitedNodes.Count >= maxNodes);
            bool edgesThresholdReached = (maxEdges > 0 && visitedEdges.Count >= maxEdges);

            while ((nodeQueue.Count > 0 || pendingNeighborDepths.Count > 0) && !nodesThresholdReached && !edgesThresholdReached)
            {
                if (pendingNeighborDepths.Count > 0 && (nodeQueue.Count == 0 || pendingNeighborDepths.Count >= 10))
                {
                    List<Guid> neighborGuidsToLoad = pendingNeighborDepths.Keys.Where(guid => !visitedNodes.Contains(guid)).ToList();
                    if (neighborGuidsToLoad.Count > 0)
                    {
                        Dictionary<Guid, Node> loadedNodes = _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad)
                            .Where(n => n.GraphGUID == graphGuid)
                            .ToDictionary(n => n.GUID, n => n);

                        foreach (Guid neighborGuid in neighborGuidsToLoad)
                        {
                            if (loadedNodes.TryGetValue(neighborGuid, out Node neighbor))
                            {
                                visitedNodes.Add(neighborGuid);

                                if (maxNodes > 0 && visitedNodes.Count >= maxNodes)
                                {
                                    nodesThresholdReached = true;
                                    break;
                                }

                                if (!nodesThresholdReached && pendingNeighborDepths.TryGetValue(neighborGuid, out int neighborDepth))
                                    if (neighborDepth <= maxDepth)
                                        nodeQueue.Enqueue((neighborGuid, neighborDepth));
                            }
                        }
                        pendingNeighborDepths.Clear();
                    }
                }

                if (nodesThresholdReached || edgesThresholdReached) break;
                if (nodeQueue.Count == 0) continue;

                (Guid currentNodeGuid, int currentDepth) = nodeQueue.Dequeue();
                if (currentDepth >= maxDepth) continue;

                IEnumerable<Edge> connectedEdges = _Repo.Edge.ReadNodeEdges(tenantGuid, graphGuid, currentNodeGuid);

                foreach (Edge edge in connectedEdges)
                {
                    if (maxEdges > 0 && visitedEdges.Count >= maxEdges)
                    {
                        edgesThresholdReached = true;
                        break;
                    }

                    if (visitedEdges.Contains(edge.GUID)) continue;

                    Guid neighborGuid;
                    if (edge.From.Equals(currentNodeGuid))
                        neighborGuid = edge.To;
                    else
                        neighborGuid = edge.From;

                    bool needNewNode = !visitedNodes.Contains(neighborGuid);

                    if (needNewNode && maxNodes > 0 && visitedNodes.Count >= maxNodes)
                    {
                        nodesThresholdReached = true;
                        continue;
                    }

                    visitedEdges.Add(edge.GUID);

                    if (needNewNode && !pendingNeighborDepths.ContainsKey(neighborGuid))
                        pendingNeighborDepths[neighborGuid] = currentDepth + 1;
                }
            }

            if (pendingNeighborDepths.Count > 0 && !nodesThresholdReached)
            {
                List<Guid> neighborGuidsToLoad = pendingNeighborDepths.Keys.Where(guid => !visitedNodes.Contains(guid)).ToList();
                if (neighborGuidsToLoad.Count > 0)
                {
                    Dictionary<Guid, Node> loadedNodes = _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad)
                        .Where(n => n.GraphGUID == graphGuid)
                        .ToDictionary(n => n.GUID, n => n);

                    foreach (Guid neighborGuid in neighborGuidsToLoad)
                        if (loadedNodes.TryGetValue(neighborGuid, out Node neighbor))
                            visitedNodes.Add(neighborGuid);
                }
            }

            int nodeCount = visitedNodes.Count;
            int edgeCount = visitedEdges.Count;
            int labelsCount = 0;
            int tagsCount = 0;
            int vectorsCount = 0;

            if (nodeCount > 0 || edgeCount > 0)
            {
                List<Guid> nodeGuidList = visitedNodes.ToList();
                List<Guid> edgeGuidList = visitedEdges.ToList();

                // Count labels
                DataTable labelsTable = _Repo.ExecuteQuery(GraphQueries.CountLabelsForSubgraph(tenantGuid, graphGuid, nodeGuidList, edgeGuidList), true);
                if (labelsTable != null && labelsTable.Rows.Count > 0)
                    labelsCount = Convert.ToInt32(labelsTable.Rows[0][0]);

                // Count tags
                DataTable tagsTable = _Repo.ExecuteQuery(GraphQueries.CountTagsForSubgraph(tenantGuid, graphGuid, nodeGuidList, edgeGuidList), true);
                if (tagsTable != null && tagsTable.Rows.Count > 0)
                    tagsCount = Convert.ToInt32(tagsTable.Rows[0][0]);

                // Count vectors
                DataTable vectorsTable = _Repo.ExecuteQuery(GraphQueries.CountVectorsForSubgraph(tenantGuid, graphGuid, nodeGuidList, edgeGuidList), true);
                if (vectorsTable != null && vectorsTable.Rows.Count > 0)
                    vectorsCount = Convert.ToInt32(vectorsTable.Rows[0][0]);
            }

            return new GraphStatistics
            {
                Nodes = nodeCount,
                Edges = edgeCount,
                Labels = labelsCount,
                Tags = tagsCount,
                Vectors = vectorsCount
            };
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
