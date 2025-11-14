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
    /// Node methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class NodeMethods : INodeMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;
        private LRUCache<Guid, Node> _NodeCache = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Node methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        /// <param name="cache">Cache.</param>
        public NodeMethods(LiteGraphClient client, GraphRepositoryBase repo, LRUCache<Guid, Node> cache)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _NodeCache = cache;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Node> Create(Node node, CancellationToken token = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            token.ThrowIfCancellationRequested();

            _Client.ValidateTags(node.Tags);
            _Client.ValidateLabels(node.Labels);
            _Client.ValidateVectors(node.Vectors);
            await _Client.ValidateTenantExists(node.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(node.TenantGUID, node.GraphGUID, token).ConfigureAwait(false);
            Node created = await _Repo.Node.Create(node, token).ConfigureAwait(false);
            List<LabelMetadata> createdLabels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                createdLabels.Add(label);
            }
            created.Labels = LabelMetadata.ToListString(createdLabels);

            List<TagMetadata> createdTags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                createdTags.Add(tag);
            }
            created.Tags = TagMetadata.ToNameValueCollection(createdTags);

            List<VectorMetadata> createdVectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                createdVectors.Add(vector);
            }
            created.Vectors = createdVectors;
            _Client.Logging.Log(SeverityEnum.Info, "created node " + created.GUID + " in graph " + created.GraphGUID);
            _NodeCache.AddReplace(created.GUID, created);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<Node>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Node> nodes, CancellationToken token = default)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            List<Node> created = await _Repo.Node.CreateMany(tenantGuid, graphGuid, nodes, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created " + created.Count + " node(s) in graph " + graphGuid);

            foreach (Node node in created)
            {
                token.ThrowIfCancellationRequested();
                List<LabelMetadata> nodeLabels = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    nodeLabels.Add(label);
                }
                node.Labels = LabelMetadata.ToListString(nodeLabels);

                List<TagMetadata> nodeTags = new List<TagMetadata>();
                await foreach (TagMetadata tag in _Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    nodeTags.Add(tag);
                }
                node.Tags = TagMetadata.ToNameValueCollection(nodeTags);

                List<VectorMetadata> nodeVectors = new List<VectorMetadata>();
                await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    nodeVectors.Add(vector);
                }
                node.Vectors = nodeVectors;
                _NodeCache.AddReplace(node.GUID, node);
            }

            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadAllInTenant(
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

            await foreach (Node obj in _Repo.Node.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            if (order == EnumerationOrderEnum.MostConnected)
            {
                await foreach (Node node in ReadMostConnected(tenantGuid, graphGuid, null, null, null, skip, includeData, includeSubordinates, token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return node;
                }
            }
            else if (order == EnumerationOrderEnum.LeastConnected)
            {
                await foreach (Node node in ReadLeastConnected(tenantGuid, graphGuid, null, null, null, skip, includeData, includeSubordinates, token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return node;
                }
            }
            else
            {
                await foreach (Node obj in _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadMany(
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
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            if (order == EnumerationOrderEnum.MostConnected)
            {
                await foreach (Node node in ReadMostConnected(tenantGuid, graphGuid, labels, tags, expr, skip, includeData, includeSubordinates, token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return node;
                }
            }
            else if (order == EnumerationOrderEnum.LeastConnected)
            {
                await foreach (Node node in ReadLeastConnected(tenantGuid, graphGuid, labels, tags, expr, skip, includeData, includeSubordinates, token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return node;
                }
            }
            else
            {
                await foreach (Node obj in _Repo.Node.ReadMany(tenantGuid, graphGuid, name, labels, tags, expr, order, skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    yield return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Node> ReadFirst(
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
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            Node obj = await _Repo.Node.ReadFirst(tenantGuid, graphGuid, name, labels, tags, expr, order, token).ConfigureAwait(false);

            if (obj != null)
            {
                return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }

            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await foreach (Node obj in _Repo.Node.ReadMostConnected(tenantGuid, graphGuid, labels, tags, expr, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await foreach (Node obj in _Repo.Node.ReadLeastConnected(tenantGuid, graphGuid, labels, tags, expr, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<Node> ReadByGuid(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Node obj = await _Repo.Node.ReadByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
            if (obj == null) return null;
            return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Node> ReadByGuids(
            Guid tenantGuid, 
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving nodes");

            await foreach (Node obj in _Repo.Node.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return await PopulateNode(obj, includeSubordinates, includeData, token).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Node>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            EnumerationResult<Node> er = await _Repo.Node.Enumerate(query, token).ConfigureAwait(false);

            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                foreach (Node obj in er.Objects)
                {
                    token.ThrowIfCancellationRequested();
                    if (query.IncludeSubordinates)
                    {
                        List<LabelMetadata> allLabels = new List<LabelMetadata>();
                        await foreach (LabelMetadata label in _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                        {
                            allLabels.Add(label);
                        }
                        if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = new List<TagMetadata>();
                        await foreach (TagMetadata tag in _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                        {
                            allTags.Add(tag);
                        }
                        if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                        List<VectorMetadata> allVectors = new List<VectorMetadata>();
                        await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(obj.TenantGUID, obj.GraphGUID, obj.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                        {
                            allVectors.Add(vector);
                        }
                        obj.Vectors = allVectors;
                    }

                    if (!query.IncludeData) obj.Data = null;
                }                
            }

            return er;
        }

        /// <inheritdoc />
        public async Task<Node> Update(Node node, CancellationToken token = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            token.ThrowIfCancellationRequested();

            _Client.ValidateLabels(node.Labels);
            _Client.ValidateTags(node.Tags);
            _Client.ValidateVectors(node.Vectors);
            await _Client.ValidateTenantExists(node.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(node.TenantGUID, node.GraphGUID, token).ConfigureAwait(false);
            Node updated = await _Repo.Node.Update(node, token).ConfigureAwait(false);
            List<LabelMetadata> updatedLabels = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                updatedLabels.Add(label);
            }
            updated.Labels = LabelMetadata.ToListString(updatedLabels);

            List<TagMetadata> updatedTags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                updatedTags.Add(tag);
            }
            updated.Tags = TagMetadata.ToNameValueCollection(updatedTags);

            List<VectorMetadata> updatedVectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                updatedVectors.Add(vector);
            }
            updated.Vectors = updatedVectors;
            _Client.Logging.Log(SeverityEnum.Debug, "updated node " + updated.GUID + " in graph " + updated.GraphGUID);
            _NodeCache.AddReplace(updated.GUID, updated);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await _Repo.Node.DeleteByGuid(tenantGuid, graphGuid, nodeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted node " + nodeGuid + " in graph " + graphGuid);
            _NodeCache.TryRemove(nodeGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await _Repo.Node.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted nodes for tenant " + tenantGuid);
            _NodeCache.Clear();
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await _Repo.Node.DeleteAllInGraph(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted nodes for graph " + graphGuid);
            _NodeCache.Clear();
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await _Repo.Node.DeleteMany(tenantGuid, graphGuid, nodeGuids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted " + nodeGuids.Count + " node(s) for graph " + graphGuid);

            foreach (Guid nodeGuid in nodeGuids)
            {
                _NodeCache.TryRemove(nodeGuid);
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Node.ExistsByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
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
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);

            await foreach (Node node in _Repo.Node.ReadParents(tenantGuid, graphGuid, nodeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return node;
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
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);

            await foreach (Node node in _Repo.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return node;
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
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);

            await foreach (Node node in _Repo.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return node;
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
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, fromNodeGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, toNodeGuid, token).ConfigureAwait(false);

            await foreach (RouteDetail route in _Repo.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, edgeFilter, nodeFilter, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return route;
            }
        }

        #endregion

        #region Internal-Methods

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
                if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = new List<TagMetadata>();
                await foreach (TagMetadata tag in _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allTags.Add(tag);
                }
                if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                List<VectorMetadata> allVectors = new List<VectorMetadata>();
                await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(obj.TenantGUID, obj.GraphGUID, obj.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    allVectors.Add(vector);
                }
                obj.Vectors = allVectors;
            }

            if (!includeData) obj.Data = null;
            return obj;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}