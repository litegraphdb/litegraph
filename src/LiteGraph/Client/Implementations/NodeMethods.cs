namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
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
        public Node Create(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            _Client.ValidateTags(node.Tags);
            _Client.ValidateLabels(node.Labels);
            _Client.ValidateVectors(node.Vectors);
            _Client.ValidateTenantExists(node.TenantGUID);
            _Client.ValidateGraphExists(node.TenantGUID, node.GraphGUID);
            Node created = _Repo.Node.Create(node);
            created.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null).ToList());
            created.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null).ToList());
            created.Vectors = _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Info, "created node " + created.GUID + " in graph " + created.GraphGUID);
            _NodeCache.AddReplace(created.GUID, created);
            return created;
        }

        /// <inheritdoc />
        public List<Node> CreateMany(Guid tenantGuid, Guid graphGuid, List<Node> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            List<Node> created = _Repo.Node.CreateMany(tenantGuid, graphGuid, nodes);
            _Client.Logging.Log(SeverityEnum.Info, "created " + created.Count + " node(s) in graph " + graphGuid);

            foreach (Node node in created)
            {
                node.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null).ToList());
                node.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null).ToList());
                node.Vectors = _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID).ToList();
                _NodeCache.AddReplace(node.GUID, node);
            }

            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadAllInTenant(
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

            foreach (Node obj in _Repo.Node.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return PopulateNode(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            if (order == EnumerationOrderEnum.MostConnected)
            {
                foreach (Node node in ReadMostConnected(tenantGuid, graphGuid, null, null, null, skip))
                {
                    yield return node;
                }
            }
            else if (order == EnumerationOrderEnum.LeastConnected)
            {
                foreach (Node node in ReadLeastConnected(tenantGuid, graphGuid, null, null, null, skip))
                {
                    yield return node;
                }
            }
            else
            {
                foreach (Node obj in _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, order, skip))
                {
                    yield return PopulateNode(obj, includeSubordinates, includeData);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
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

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            if (order == EnumerationOrderEnum.MostConnected)
            {
                foreach (Node node in ReadMostConnected(tenantGuid, graphGuid, labels, tags, expr, skip))
                {
                    yield return node;
                }
            }
            else if (order == EnumerationOrderEnum.LeastConnected)
            {
                foreach (Node node in ReadLeastConnected(tenantGuid, graphGuid, labels, tags, expr, skip))
                {
                    yield return node;
                }
            }
            else
            {
                foreach (Node obj in _Repo.Node.ReadMany(tenantGuid, graphGuid, name, labels, tags, expr, order, skip))
                {
                    yield return PopulateNode(obj, includeSubordinates, includeData);
                }
            }
        }

        /// <inheritdoc />
        public Node ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
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

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            Node obj = _Repo.Node.ReadFirst(tenantGuid, graphGuid, name, labels, tags, expr, order);

            if (obj != null)
            {
                return PopulateNode(obj, includeSubordinates, includeData);
            }

            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Node obj in _Repo.Node.ReadMostConnected(tenantGuid, graphGuid, labels, tags, expr, skip))
            {
                yield return PopulateNode(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Node obj in _Repo.Node.ReadLeastConnected(tenantGuid, graphGuid, labels, tags, expr, skip))
            {
                yield return PopulateNode(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public Node ReadByGuid(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            Node obj = _Repo.Node.ReadByGuid(tenantGuid, nodeGuid);
            if (obj == null) return null;
            return PopulateNode(obj, includeSubordinates, includeData);
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadByGuids(
            Guid tenantGuid, 
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving nodes");

            foreach (Node obj in _Repo.Node.ReadByGuids(tenantGuid, guids))
            {
                yield return PopulateNode(obj, includeSubordinates, includeData);
            }
        }

        /// <inheritdoc />
        public EnumerationResult<Node> Enumerate(EnumerationRequest query)
        {
            if (query == null) query = new EnumerationRequest();
            EnumerationResult<Node> er = _Repo.Node.Enumerate(query);

            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                foreach (Node obj in er.Objects)
                {
                    if (query.IncludeSubordinates)
                    {
                        List<LabelMetadata> allLabels = _Repo.Label.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null).ToList();
                        if (allLabels != null) obj.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = _Repo.Tag.ReadMany(obj.TenantGUID, obj.GraphGUID, obj.GUID, null, null, null).ToList();
                        if (allTags != null) obj.Tags = TagMetadata.ToNameValueCollection(allTags);

                        obj.Vectors = _Repo.Vector.ReadManyNode(obj.TenantGUID, obj.GraphGUID, obj.GUID).ToList();
                    }

                    if (!query.IncludeData) obj.Data = null;
                }                
            }

            return er;
        }

        /// <inheritdoc />
        public Node Update(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            _Client.ValidateLabels(node.Labels);
            _Client.ValidateTags(node.Tags);
            _Client.ValidateVectors(node.Vectors);
            _Client.ValidateTenantExists(node.TenantGUID);
            _Client.ValidateGraphExists(node.TenantGUID, node.GraphGUID);
            Node updated = _Repo.Node.Update(node);
            updated.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null).ToList());
            updated.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null).ToList());
            updated.Vectors = _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Debug, "updated node " + updated.GUID + " in graph " + updated.GraphGUID);
            _NodeCache.AddReplace(updated.GUID, updated);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);
            _Repo.Node.DeleteByGuid(tenantGuid, graphGuid, nodeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted node " + nodeGuid + " in graph " + graphGuid);
            _NodeCache.TryRemove(nodeGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Node.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted nodes for tenant " + tenantGuid);
            _NodeCache.Clear();
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Node.DeleteAllInGraph(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted nodes for graph " + graphGuid);
            _NodeCache.Clear();
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Node.DeleteMany(tenantGuid, graphGuid, nodeGuids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted " + nodeGuids.Count + " node(s) for graph " + graphGuid);

            foreach (Guid nodeGuid in nodeGuids)
            {
                _NodeCache.TryRemove(nodeGuid);
            }
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid nodeGuid)
        {
            return _Repo.Node.ExistsByGuid(tenantGuid, nodeGuid);
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);

            foreach (Node node in _Repo.Node.ReadParents(tenantGuid, graphGuid, nodeGuid, order, skip))
            {
                yield return node;
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
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);

            foreach (Node node in _Repo.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid, order, skip))
            {
                yield return node;
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
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);

            foreach (Node node in _Repo.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid, order, skip))
            {
                yield return node;
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
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, fromNodeGuid);
            _Client.ValidateNodeExists(tenantGuid, toNodeGuid);

            foreach (RouteDetail route in _Repo.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, edgeFilter, nodeFilter))
            {
                yield return route;
            }
        }

        #endregion

        #region Private-Methods

        private Node PopulateNode(Node obj, bool includeSubordinates, bool includeData)
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

        #endregion
    }
}