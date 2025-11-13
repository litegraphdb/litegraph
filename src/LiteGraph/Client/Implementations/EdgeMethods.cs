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
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Edge methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class EdgeMethods : IEdgeMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;
        private LRUCache<Guid, Edge> _EdgeCache = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Edge methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        /// <param name="cache">Cache.</param>
        public EdgeMethods(LiteGraphClient client, GraphRepositoryBase repo, LRUCache<Guid, Edge> cache)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _EdgeCache = cache;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Edge> Create(Edge edge, CancellationToken token = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            token.ThrowIfCancellationRequested();

            _Client.ValidateTenantExists(edge.TenantGUID);
            _Client.ValidateGraphExists(edge.TenantGUID, edge.GraphGUID);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.To);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.From);
            _Client.ValidateLabels(edge.Labels);
            _Client.ValidateTags(edge.Tags);
            _Client.ValidateVectors(edge.Vectors);

            Edge created = await _Repo.Edge.Create(edge, token).ConfigureAwait(false);
            created.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
            created.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
            created.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Info, "created edge " + created.GUID + " in graph " + created.GraphGUID);
            _EdgeCache.AddReplace(created.GUID, created);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<Edge>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Edge> edges, CancellationToken token = default)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            token.ThrowIfCancellationRequested();

            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            List<Edge> created = await _Repo.Edge.CreateMany(tenantGuid, graphGuid, edges, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created " + created.Count + " edges(s) in graph " + graphGuid);

            // Add created edges to cache
            foreach (Edge edge in created)
            {
                token.ThrowIfCancellationRequested();
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                _EdgeCache.AddReplace(edge.GUID, edge);
            }

            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateTenantExists(tenantGuid);

            await foreach (Edge obj in _Repo.Edge.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await foreach (Edge obj in _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
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
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await foreach (Edge obj in _Repo.Edge.ReadMany(tenantGuid, graphGuid, name, labels, tags, expr, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async Task<Edge> ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            Edge obj = await _Repo.Edge.ReadFirst(tenantGuid, graphGuid, name, labels, tags, expr, order, token).ConfigureAwait(false);

            if (obj != null) return PopulateEdge(obj, includeSubordinates, includeData);
            return null;
        }

        /// <inheritdoc />
        public async Task<Edge> ReadByGuid(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            Edge obj = await _Repo.Edge.ReadByGuid(tenantGuid, edgeGuid, token).ConfigureAwait(false);
            if (obj == null) return null;

            return PopulateEdge(obj, includeSubordinates, includeData);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadByGuids(
            Guid tenantGuid, 
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving edges");

            await foreach (Edge obj in _Repo.Edge.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Edge>> Enumerate(EnumerationRequest query = null, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();

            EnumerationResult<Edge> er = await _Repo.Edge.Enumerate(query, token).ConfigureAwait(false);

            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                foreach (Edge obj in er.Objects)
                {
                    token.ThrowIfCancellationRequested();
                    if (query.IncludeSubordinates)
                    {
                        List<LabelMetadata> allLabels = _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, null, obj.GUID, null).ToList();
                        if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, null, obj.GUID, null, null).ToList();
                        if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                        obj.Vectors = _Repo.Vector.ReadManyEdge(obj.TenantGUID, obj.GraphGUID, obj.GUID).ToList();
                    }

                    if (!query.IncludeData)
                    {
                        obj.Data = null;
                    }
                }
            }

            return er;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadNodeEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await foreach (Edge obj in _Repo.Edge.ReadNodeEdges(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadEdgesFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await foreach (Edge obj in _Repo.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadEdgesToNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await foreach (Edge obj in _Repo.Edge.ReadEdgesToNode(
                tenantGuid,
                graphGuid,
                nodeGuid,
                labels,
                tags,
                edgeFilter,
                order,
                skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadEdgesBetweenNodes(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await foreach (Edge obj in _Repo.Edge.ReadEdgesBetweenNodes(
                tenantGuid,
                graphGuid,
                fromNodeGuid,
                toNodeGuid,
                labels,
                tags,
                edgeFilter,
                order,
                skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return PopulateEdge(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public async Task<Edge> Update(Edge edge, CancellationToken token = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            token.ThrowIfCancellationRequested();

            _Client.ValidateTenantExists(edge.TenantGUID);
            _Client.ValidateGraphExists(edge.TenantGUID, edge.GraphGUID);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.To);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.From);
            _Client.ValidateLabels(edge.Labels);
            _Client.ValidateTags(edge.Tags);
            _Client.ValidateVectors(edge.Vectors);

            Edge updated = await _Repo.Edge.Update(edge, token).ConfigureAwait(false);
            updated.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
            updated.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
            updated.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Debug, "updated edge " + updated.GUID + " in graph " + updated.GraphGUID);
            _EdgeCache.AddReplace(updated.GUID, updated);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.ValidateEdgeExists(tenantGuid, edgeGuid);
            await _Repo.Edge.DeleteByGuid(tenantGuid, graphGuid, edgeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "deleted edge " + edgeGuid + " in graph " + graphGuid);
            _EdgeCache.TryRemove(edgeGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.ValidateTenantExists(tenantGuid);
            await _Repo.Edge.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges in tenant " + tenantGuid);
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            await _Repo.Edge.DeleteAllInGraph(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges in graph " + graphGuid);
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids, CancellationToken token = default)
        {
            if (edgeGuids == null || edgeGuids.Count < 1) return;
            token.ThrowIfCancellationRequested();

            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            await _Repo.Edge.DeleteMany(tenantGuid, graphGuid, edgeGuids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted " + edgeGuids.Count + " edge(s) in graph " + graphGuid);

            foreach (Guid edgeGuid in edgeGuids)
            {
                _EdgeCache.TryRemove(edgeGuid);
            }
        }

        /// <inheritdoc />
        public async Task DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);
            await _Repo.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges for node " + nodeGuid);
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public async Task DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            token.ThrowIfCancellationRequested();

            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            await _Repo.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges for " + nodeGuids.Count + " node(s)");
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            return await _Repo.Edge.ExistsByGuid(tenantGuid, edgeGuid, token).ConfigureAwait(false);
        }

        #endregion

        #region Internal-Methods

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