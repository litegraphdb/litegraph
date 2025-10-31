namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
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
        public Graph Create(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            _Client.ValidateLabels(graph.Labels);
            _Client.ValidateTags(graph.Tags);
            _Client.ValidateVectors(graph.Vectors);
            _Client.ValidateTenantExists(graph.TenantGUID);
            graph = _Repo.Graph.Create(graph);
            graph.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(graph.TenantGUID, graph.GUID, null, null, null).ToList());
            graph.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(graph.TenantGUID, graph.GUID, null, null, null, null).ToList());
            graph.Vectors = _Repo.Vector.ReadManyGraph(graph.TenantGUID, graph.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Info, "created graph name " + graph.Name + " GUID " + graph.GUID);
            _GraphCache.AddReplace(graph.GUID, graph);
            return graph;
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            foreach (Graph obj in _Repo.Graph.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return PopulateGraph(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            foreach (Graph obj in _Repo.Graph.ReadMany(tenantGuid, name, labels, tags, expr, order, skip))
            {
                yield return PopulateGraph(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public Graph ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);

            Graph obj = _Repo.Graph.ReadFirst(tenantGuid, name, labels, tags, expr, order);
            if (obj == null) return null;
            return PopulateGraph(obj, includeSubordinates, includeData);
        }

        /// <inheritdoc />
        public Graph ReadByGuid(
            Guid tenantGuid, 
            Guid graphGuid,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graph with GUID " + graphGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            Graph obj = _Repo.Graph.ReadByGuid(tenantGuid, graphGuid);
            if (obj == null) return null;
            return PopulateGraph(obj, includeSubordinates, includeData);
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadByGuids(
            Guid tenantGuid, 
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            foreach (Graph obj in _Repo.Graph.ReadByGuids(tenantGuid, guids))
            {
                yield return PopulateGraph(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public EnumerationResult<Graph> Enumerate(EnumerationRequest query)
        {
            if (query == null) query = new EnumerationRequest();
            EnumerationResult<Graph> er = _Repo.Graph.Enumerate(query);

            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                foreach (Graph obj in er.Objects)
                {
                    if (query.IncludeSubordinates)
                    {
                        List<LabelMetadata> allLabels = _Repo.Label.ReadMany(obj.TenantGUID, obj.GUID, null, null, null).ToList();
                        if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = _Repo.Tag.ReadMany(obj.TenantGUID, obj.GUID, null, null, null, null).ToList();
                        if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                        obj.Vectors = _Repo.Vector.ReadManyGraph(obj.TenantGUID, obj.GUID).ToList();
                    }

                    if (!query.IncludeData) obj.Data = null;
                }
            }

            return er;
        }

        /// <inheritdoc />
        public Graph Update(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            _Client.ValidateTenantExists(graph.TenantGUID);
            _Client.ValidateGraphExists(graph.TenantGUID, graph.GUID);
            _Client.ValidateLabels(graph.Labels);
            _Client.ValidateTags(graph.Tags);
            _Client.ValidateVectors(graph.Vectors);
            Graph updated = _Repo.Graph.Update(graph);
            updated.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(graph.TenantGUID, graph.GUID, null, null, null).ToList());
            updated.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(graph.TenantGUID, graph.GUID, null, null, null, null).ToList());
            updated.Vectors = _Repo.Vector.ReadManyGraph(graph.TenantGUID, graph.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Debug, "updated graph with name " + graph.Name + " GUID " + graph.GUID);
            _GraphCache.AddReplace(updated.GUID, updated);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid, bool force = false)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleting graph " + graphGuid);

            if (!force)
            {
                if (_Repo.Node.ReadMany(tenantGuid, graphGuid).Any())
                    throw new InvalidOperationException("The specified graph has dependent nodes or edges.");

                if (_Repo.Edge.ReadMany(tenantGuid, graphGuid).Any())
                    throw new InvalidOperationException("The specified graph has dependent nodes or edges.");
            }

            _Repo.Graph.DeleteByGuid(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted graph " + graphGuid + " (force " + force + ")");
            _GraphCache.TryRemove(graphGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            foreach (Graph graph in ReadMany(tenantGuid))
            {
                DeleteByGuid(tenantGuid, graph.GUID);
            }
            _Client.Logging.Log(SeverityEnum.Info, "deleted graphs in tenant " + tenantGuid);
            _GraphCache.Clear();
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            return _Repo.Graph.ExistsByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public GraphStatistics GetStatistics(Guid tenantGuid, Guid guid)
        {
            return _Repo.Graph.GetStatistics(tenantGuid, guid);
        }

        /// <inheritdoc />
        public Dictionary<Guid, GraphStatistics> GetStatistics(Guid tenantGuid)
        {
            return _Repo.Graph.GetStatistics(tenantGuid);
        }

        /// <inheritdoc />
        public async Task EnableVectorIndexingAsync(
            Guid tenantGuid, 
            Guid graphGuid, 
            VectorIndexConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // Validate configuration
            if (!configuration.IsValid(out string errorMessage))
                throw new ArgumentException($"Invalid vector index configuration: {errorMessage}");

            await _Repo.Graph.EnableVectorIndexingAsync(tenantGuid, graphGuid, configuration);

            // Invalidate cache
            if (_GraphCache != null) _GraphCache.Remove(graphGuid);
        }

        /// <inheritdoc />
        public async Task DisableVectorIndexingAsync(
            Guid tenantGuid, 
            Guid graphGuid, 
            bool deleteIndexFile = false)
        {
            await _Repo.Graph.DisableVectorIndexingAsync(tenantGuid, graphGuid, deleteIndexFile);

            // Invalidate cache
            if (_GraphCache != null) _GraphCache.Remove(graphGuid);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndexAsync(
            Guid tenantGuid, 
            Guid graphGuid)
        {
            await _Repo.Graph.RebuildVectorIndexAsync(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public VectorIndexStatistics GetVectorIndexStatistics(
            Guid tenantGuid, 
            Guid graphGuid)
        {
            return _Repo.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public SearchResult GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);

            _Client.Logging.Log(SeverityEnum.Debug, "retrieving subgraph starting from node " + nodeGuid + " with max depth " + maxDepth);

            SearchResult result = _Repo.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth);

            // Populate graphs
            if (result.Graphs != null && result.Graphs.Count > 0)
            {
                for (int i = 0; i < result.Graphs.Count; i++)
                {
                    result.Graphs[i] = PopulateGraph(result.Graphs[i], includeSubordinates, includeData);
                }
            }

            // Populate nodes
            if (result.Nodes != null && result.Nodes.Count > 0)
            {
                for (int i = 0; i < result.Nodes.Count; i++)
                {
                    result.Nodes[i] = PopulateNode(result.Nodes[i], includeSubordinates, includeData);
                }
            }

            // Populate edges
            if (result.Edges != null && result.Edges.Count > 0)
            {
                for (int i = 0; i < result.Edges.Count; i++)
                {
                    result.Edges[i] = PopulateEdge(result.Edges[i], includeSubordinates, includeData);
                }
            }

            return result;
        }

        #endregion

        #region Internal-Methods

        internal Graph PopulateGraph(Graph obj, bool includeSubordinates, bool includeData)
        {
            if (obj == null) return null;

            if (includeSubordinates)
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(obj.TenantGUID, obj.GUID, null, null, null).ToList();
                if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(obj.TenantGUID, obj.GUID, null, null, null, null).ToList();
                if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                obj.Vectors = _Repo.Vector.ReadManyGraph(obj.TenantGUID, obj.GUID).ToList();
            }

            if (!includeData) obj.Data = null;
            return obj;
        }

        internal Node PopulateNode(Node obj, bool includeSubordinates, bool includeData)
        {
            if (obj == null) return null;

            if (includeSubordinates)
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null).ToList();
                if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, null).ToList();
                if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                obj.Vectors = _Repo.Vector.ReadManyNode(obj.TenantGUID, obj.GraphGUID, obj.GUID).ToList();
            }

            if (!includeData) obj.Data = null;
            return obj;
        }

        internal Edge PopulateEdge(Edge obj, bool includeSubordinates, bool includeData)
        {
            if (obj == null) return null;

            if (includeSubordinates)
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, null, obj.GUID, null).ToList();
                if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, null, obj.GUID, null, null).ToList();
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