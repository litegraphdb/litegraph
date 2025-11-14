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
        public async Task<Graph> Create(Graph graph, CancellationToken token = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            token.ThrowIfCancellationRequested();
            string createQuery = GraphQueries.Insert(graph);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            Graph created = Converters.GraphFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);
                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.SelectMany(
                    tenantGuid,
                    name,
                    labels,
                    tags,
                    graphFilter,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);
                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<Graph> ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.SelectMany(
                tenantGuid,
                name,
                labels,
                tags,
                graphFilter,
                1,
                0,
                order), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.GraphFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<Graph> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1)
            {
                Graph graph = Converters.GraphFromDataRow(result.Rows[0]);
                return graph;
            }
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Graph> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.GraphFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Graph>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            Graph marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Graph> ret = new EnumerationResult<Graph>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = await GetRecordCount(query.TenantGUID, query.Labels, query.Tags, query.Expr, query.Ordering, null, token).ConfigureAwait(false);

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
                DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.GetRecordPage(
                    query.TenantGUID,
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
                    ret.Objects = Converters.GraphsFromDataTable(result);

                    Graph lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);

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
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.GetRecordCount(
                tenantGuid,
                labels,
                tags,
                filter,
                order,
                marker), false, token).ConfigureAwait(false);

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
        public async Task<Graph> Update(Graph graph, CancellationToken token = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(GraphQueries.Update(graph), true, token).ConfigureAwait(false);
            Graph updated = Converters.GraphFromDataRow(result.Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(GraphQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(GraphQueries.Delete(tenantGuid, graphGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph graph = await ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            return (graph != null);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, GraphStatistics>> GetStatistics(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Dictionary<Guid, GraphStatistics> ret = new Dictionary<Guid, GraphStatistics>();
            DataTable table = await _Repo.ExecuteQueryAsync(GraphQueries.GetStatistics(tenantGuid, null), true, token).ConfigureAwait(false);
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    token.ThrowIfCancellationRequested();
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
        public async Task<GraphStatistics> GetStatistics(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable table = await _Repo.ExecuteQueryAsync(GraphQueries.GetStatistics(tenantGuid, guid), true, token).ConfigureAwait(false);
            if (table != null && table.Rows.Count > 0) return Converters.GraphStatisticsFromDataRow(table.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async Task EnableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            VectorIndexConfiguration configuration,
            CancellationToken token = default)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            token.ThrowIfCancellationRequested();

            Graph graph = await ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            // Apply configuration to the graph object
            configuration.ApplyToGraph(graph);

            // Get all existing vectors in the graph before enabling indexing
            List<VectorMetadata> existingVectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Repo.Vector.ReadAllInGraph(tenantGuid, graphGuid, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                existingVectors.Add(vector);
            }

            // Enable indexing using the index manager
            await _Repo.VectorIndexManager.EnableIndexingAsync(graph, configuration.VectorIndexType, configuration.VectorIndexFile).ConfigureAwait(false);

            // If there are existing vectors, populate the index
            if (existingVectors.Count > 0)
            {
                await _Repo.VectorIndexManager.RebuildIndexAsync(graph, existingVectors).ConfigureAwait(false);
            }

            // Update the graph in the database with all configuration values
            await _Repo.ExecuteQueryAsync(GraphQueries.Update(graph), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DisableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            bool deleteIndexFile = false,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph graph = await ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            // Disable indexing using the index manager
            await _Repo.VectorIndexManager.DisableIndexingAsync(graphGuid, deleteIndexFile).ConfigureAwait(false);

            // Apply disabled configuration to clear all vector index settings
            VectorIndexConfiguration disabledConfig = VectorIndexConfiguration.CreateDisabled();
            disabledConfig.ApplyToGraph(graph);

            // Update graph in database
            await _Repo.ExecuteQueryAsync(GraphQueries.Update(graph), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndexAsync(
            Guid tenantGuid,
            Guid graphGuid,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph graph = await ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                throw new InvalidOperationException("Graph does not have indexing enabled.");

            // Get all vectors for the graph (including nodes and edges)
            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Repo.Vector.ReadAllInGraph(tenantGuid, graphGuid, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                vectors.Add(vector);
            }

            // Rebuild the index
            await _Repo.VectorIndexManager.RebuildIndexAsync(graph, vectors).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorIndexStatistics> GetVectorIndexStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph graph = await ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null)
                throw new KeyNotFoundException($"Graph {graphGuid} not found.");

            return _Repo.VectorIndexManager.GetStatistics(graphGuid);
        }

        /// <inheritdoc />
        public async Task<SearchResult> GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            CancellationToken token = default)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));
            token.ThrowIfCancellationRequested();

            SearchResult result = new SearchResult
            {
                Nodes = new List<Node>(),
                Edges = new List<Edge>(),
                Graphs = new List<Graph>()
            };

            Graph graph = await ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");
            result.Graphs.Add(graph);

            Node startingNode = await _Repo.Node.ReadByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
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
                token.ThrowIfCancellationRequested();
                if (pendingNeighborDepths.Count > 0 && (nodeQueue.Count == 0 || pendingNeighborDepths.Count >= 10))
                {
                    List<Guid> neighborGuidsToLoad = pendingNeighborDepths.Keys.Where(guid => !visitedNodes.Contains(guid)).ToList();
                    if (neighborGuidsToLoad.Count > 0)
                    {
                        List<Node> nodes = new List<Node>();
                        await foreach (Node node in _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad, token).WithCancellation(token).ConfigureAwait(false))
                        {
                            nodes.Add(node);
                        }
                        Dictionary<Guid, Node> loadedNodes = nodes
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

                await foreach (Edge edge in _Repo.Edge.ReadNodeEdges(
                    tenantGuid,
                    graphGuid,
                    currentNode.GUID,
                    token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
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
                    List<Node> nodes = new List<Node>();
                    await foreach (Node node in _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad, token).WithCancellation(token).ConfigureAwait(false))
                    {
                        nodes.Add(node);
                    }
                    Dictionary<Guid, Node> loadedNodes = nodes
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
        public async Task<GraphStatistics> GetSubgraphStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            CancellationToken token = default)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));
            token.ThrowIfCancellationRequested();

            Node startingNode = await _Repo.Node.ReadByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
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
                token.ThrowIfCancellationRequested();
                if (pendingNeighborDepths.Count > 0 && (nodeQueue.Count == 0 || pendingNeighborDepths.Count >= 10))
                {
                    List<Guid> neighborGuidsToLoad = pendingNeighborDepths.Keys.Where(guid => !visitedNodes.Contains(guid)).ToList();
                    if (neighborGuidsToLoad.Count > 0)
                    {
                        List<Node> nodes = new List<Node>();
                        await foreach (Node node in _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad, token).WithCancellation(token).ConfigureAwait(false))
                        {
                            nodes.Add(node);
                        }
                        Dictionary<Guid, Node> loadedNodes = nodes
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

                await foreach (Edge edge in _Repo.Edge.ReadNodeEdges(
                    tenantGuid,
                    graphGuid,
                    currentNodeGuid,
                    token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
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
                    List<Node> nodes = new List<Node>();
                    await foreach (Node node in _Repo.Node.ReadByGuids(tenantGuid, neighborGuidsToLoad, token).WithCancellation(token).ConfigureAwait(false))
                    {
                        nodes.Add(node);
                    }
                    Dictionary<Guid, Node> loadedNodes = nodes
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
                DataTable labelsTable = await _Repo.ExecuteQueryAsync(GraphQueries.CountLabelsForSubgraph(tenantGuid, graphGuid, nodeGuidList, edgeGuidList), true, token).ConfigureAwait(false);
                if (labelsTable != null && labelsTable.Rows.Count > 0)
                    labelsCount = Convert.ToInt32(labelsTable.Rows[0][0]);

                // Count tags
                DataTable tagsTable = await _Repo.ExecuteQueryAsync(GraphQueries.CountTagsForSubgraph(tenantGuid, graphGuid, nodeGuidList, edgeGuidList), true, token).ConfigureAwait(false);
                if (tagsTable != null && tagsTable.Rows.Count > 0)
                    tagsCount = Convert.ToInt32(tagsTable.Rows[0][0]);

                // Count vectors
                DataTable vectorsTable = await _Repo.ExecuteQueryAsync(GraphQueries.CountVectorsForSubgraph(tenantGuid, graphGuid, nodeGuidList, edgeGuidList), true, token).ConfigureAwait(false);
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
