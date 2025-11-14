namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Caching;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Indexing.Vector;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Graph methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class GraphMethods : IGraphMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;
        private LRUCache<Guid, Graph> _GraphCache = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Graph methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        /// <param name="cache">Cache.</param>
        public GraphMethods(LiteGraphClient client, GraphRepositoryBase repo, LRUCache<Guid, Graph> cache)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _GraphCache = cache;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Graph> Create(Graph graph, CancellationToken token = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            token.ThrowIfCancellationRequested();
            _Client.ValidateLabels(graph.Labels);
            _Client.ValidateTags(graph.Tags);
            _Client.ValidateVectors(graph.Vectors);
            await _Client.ValidateTenantExists(graph.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(graph.TenantGUID, graph.GUID, token).ConfigureAwait(false);
            graph = await _Repo.Graph.Create(graph, token).ConfigureAwait(false);
            List<LabelMetadata> createdLabels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Repo.Label.ReadMany(graph.TenantGUID, graph.GUID, null, null, null,token: token).WithCancellation(token).ConfigureAwait(false))
            {
                createdLabels.Add(label);
            }
            graph.Labels = LabelMetadata.ToListString(createdLabels);
            List<TagMetadata> createdTags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Repo.Tag.ReadMany(graph.TenantGUID, graph.GUID, null, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                createdTags.Add(tag);
            }
            graph.Tags = TagMetadata.ToNameValueCollection(createdTags);
            graph.Vectors = _Repo.Vector.ReadManyGraph(graph.TenantGUID, graph.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Info, "created graph name " + graph.Name + " GUID " + graph.GUID);
            _GraphCache.AddReplace(graph.GUID, graph);
            return graph;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            await foreach (Graph obj in _Repo.Graph.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateGraph(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            await foreach (Graph obj in _Repo.Graph.ReadMany(tenantGuid, name, labels, tags, expr, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateGraph(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<Graph> ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);

            Graph obj = await _Repo.Graph.ReadFirst(tenantGuid, name, labels, tags, expr, order, token).ConfigureAwait(false);
            if (obj == null) return null;
            return await PopulateGraph(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Graph> ReadByGuid(
            Guid tenantGuid,
            Guid graphGuid,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graph with GUID " + graphGuid);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            Graph obj = await _Repo.Graph.ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (obj == null) return null;
            return await PopulateGraph(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Graph> ReadByGuids(
            Guid tenantGuid,
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            await foreach (Graph obj in _Repo.Graph.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateGraph(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Graph>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            EnumerationResult<Graph> er = await _Repo.Graph.Enumerate(query, token).ConfigureAwait(false);

            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                foreach (Graph obj in er.Objects)
                {
                    token.ThrowIfCancellationRequested();
                    if (query.IncludeSubordinates)
                    {
                        List<LabelMetadata> allLabels = new List<LabelMetadata>();
                        await foreach (LabelMetadata label in _Repo.Label.ReadMany(obj.TenantGUID, obj.GUID, null, null, null,token: token).WithCancellation(token).ConfigureAwait(false))
                        {
                            allLabels.Add(label);
                        }
                        if (allLabels.Count > 0) obj.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = new List<TagMetadata>();
                        await foreach (TagMetadata tag in _Repo.Tag.ReadMany(obj.TenantGUID, obj.GUID, null, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                        {
                            allTags.Add(tag);
                        }
                        if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                        obj.Vectors = _Repo.Vector.ReadManyGraph(obj.TenantGUID, obj.GUID).ToList();
                    }

                    if (!query.IncludeData) obj.Data = null;
                }
            }

            return er;
        }

        /// <inheritdoc />
        public async Task<Graph> Update(Graph graph, CancellationToken token = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(graph.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(graph.TenantGUID, graph.GUID, token).ConfigureAwait(false);
            _Client.ValidateLabels(graph.Labels);
            _Client.ValidateTags(graph.Tags);
            _Client.ValidateVectors(graph.Vectors);
            Graph updated = await _Repo.Graph.Update(graph, token).ConfigureAwait(false);
            List<LabelMetadata> updatedLabels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Repo.Label.ReadMany(graph.TenantGUID, graph.GUID, null, null, null,token: token).WithCancellation(token).ConfigureAwait(false))
            {
                updatedLabels.Add(label);
            }
            updated.Labels = LabelMetadata.ToListString(updatedLabels);
            List<TagMetadata> updatedTags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Repo.Tag.ReadMany(graph.TenantGUID, graph.GUID, null, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                updatedTags.Add(tag);
            }
            updated.Tags = TagMetadata.ToNameValueCollection(updatedTags);
            updated.Vectors = _Repo.Vector.ReadManyGraph(graph.TenantGUID, graph.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Debug, "updated graph with name " + graph.Name + " GUID " + graph.GUID);
            _GraphCache.AddReplace(updated.GUID, updated);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, bool force = false, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleting graph " + graphGuid);

            if (!force)
            {
                bool hasNodes = false;
                await using (var nodeEnumerator = _Repo.Node.ReadMany(tenantGuid, graphGuid, token: token).GetAsyncEnumerator())
                {
                    hasNodes = await nodeEnumerator.MoveNextAsync().ConfigureAwait(false);
                }
                if (hasNodes)
                    throw new InvalidOperationException("The specified graph has dependent nodes or edges.");

                await using (var edgeEnumerator = _Repo.Edge.ReadMany(tenantGuid, graphGuid, token: token).GetAsyncEnumerator())
                {
                    if (await edgeEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified graph has dependent nodes or edges.");
                }
            }

            await _Repo.Graph.DeleteByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted graph " + graphGuid + " (force " + force + ")");
            _GraphCache.TryRemove(graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await foreach (Graph graph in ReadMany(tenantGuid, null, null, null, null, EnumerationOrderEnum.CreatedDescending, 0, false, false, token).WithCancellation(token).ConfigureAwait(false))
            {
                await DeleteByGuid(tenantGuid, graph.GUID, false, token).ConfigureAwait(false);
            }
            _Client.Logging.Log(SeverityEnum.Info, "deleted graphs in tenant " + tenantGuid);
            _GraphCache.Clear();
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            return await _Repo.Graph.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<GraphStatistics> GetStatistics(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Graph.GetStatistics(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, GraphStatistics>> GetStatistics(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Graph.GetStatistics(tenantGuid, token).ConfigureAwait(false);
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

            // Validate configuration
            if (!configuration.IsValid(out string errorMessage))
                throw new ArgumentException($"Invalid vector index configuration: {errorMessage}");

            await _Repo.Graph.EnableVectorIndexingAsync(tenantGuid, graphGuid, configuration, token).ConfigureAwait(false);

            // Invalidate cache
            if (_GraphCache != null) _GraphCache.Remove(graphGuid);
        }

        /// <inheritdoc />
        public async Task DisableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            bool deleteIndexFile = false,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.Graph.DisableVectorIndexingAsync(tenantGuid, graphGuid, deleteIndexFile, token).ConfigureAwait(false);

            // Invalidate cache
            if (_GraphCache != null) _GraphCache.Remove(graphGuid);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndexAsync(
            Guid tenantGuid,
            Guid graphGuid,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.Graph.RebuildVectorIndexAsync(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorIndexStatistics> GetVectorIndexStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SearchResult> GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);

            _Client.Logging.Log(SeverityEnum.Debug, "retrieving subgraph starting from node " + nodeGuid + " with max depth " + maxDepth + ", maxNodes " + maxNodes + ", maxEdges " + maxEdges);

            SearchResult result = await _Repo.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, token).ConfigureAwait(false);

            // Populate graphs
            if (result.Graphs != null && result.Graphs.Count > 0)
            {
                for (int i = 0; i < result.Graphs.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    result.Graphs[i] = await PopulateGraph(result.Graphs[i], includeSubordinates, includeData, token).ConfigureAwait(false);
                }
            }

            // Populate nodes
            if (result.Nodes != null && result.Nodes.Count > 0)
            {
                for (int i = 0; i < result.Nodes.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    result.Nodes[i] = await PopulateNode(result.Nodes[i], includeSubordinates, includeData, token).ConfigureAwait(false);
                }
            }

            // Populate edges
            if (result.Edges != null && result.Edges.Count > 0)
            {
                for (int i = 0; i < result.Edges.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    result.Edges[i] = await PopulateEdge(result.Edges[i], includeSubordinates, includeData, token).ConfigureAwait(false);
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

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);

            _Client.Logging.Log(SeverityEnum.Debug, "retrieving subgraph statistics starting from node " + nodeGuid + " with max depth " + maxDepth + ", maxNodes " + maxNodes + ", maxEdges " + maxEdges);
            return await _Repo.Graph.GetSubgraphStatistics(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, token).ConfigureAwait(false);
        }

        #endregion

        #region Internal-Methods

        internal async Task<Graph> PopulateGraph(Graph obj, bool includeSubordinates, bool includeData, CancellationToken token = default)
        {
            if (obj == null) return null;

            if (includeSubordinates)
            {
                List<LabelMetadata> allLabels = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _Repo.Label.ReadMany(obj.TenantGUID, obj.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allLabels.Add(label);
                }
                if (allLabels.Count > 0) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = new List<TagMetadata>();
                await foreach (TagMetadata tag in _Repo.Tag.ReadMany(obj.TenantGUID, obj.GUID, null, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allTags.Add(tag);
                }
                if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                obj.Vectors = _Repo.Vector.ReadManyGraph(obj.TenantGUID, obj.GUID).ToList();
            }

            if (!includeData) obj.Data = null;
            return obj;
        }

        internal async Task<Node> PopulateNode(Node obj, bool includeSubordinates, bool includeData, CancellationToken token = default)
        {
            if (obj == null) return null;

            if (includeSubordinates)
            {
                List<LabelMetadata> allLabels = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allLabels.Add(label);
                }
                if (allLabels.Count > 0) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = new List<TagMetadata>();
                await foreach (TagMetadata tag in _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allTags.Add(tag);
                }
                if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                obj.Vectors = _Repo.Vector.ReadManyNode(obj.TenantGUID, obj.GraphGUID, obj.GUID).ToList();
            }

            if (!includeData) obj.Data = null;
            return obj;
        }

        internal async Task<Edge> PopulateEdge(Edge obj, bool includeSubordinates, bool includeData, CancellationToken token = default)
        {
            if (obj == null) return null;

            if (includeSubordinates)
            {
                List<LabelMetadata> allLabels = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, null, obj.GUID, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allLabels.Add(label);
                }
                if (allLabels.Count > 0) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = new List<TagMetadata>();
                await foreach (TagMetadata tag in _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, null, obj.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allTags.Add(tag);
                }
                if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                obj.Vectors = _Repo.Vector.ReadManyEdge(obj.TenantGUID, obj.GraphGUID, obj.GUID).ToList();
            }

            if (!includeData) obj.Data = null;
            return obj;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}