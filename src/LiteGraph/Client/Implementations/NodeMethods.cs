namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Caching;
    using ExpressionTree;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

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
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);

            foreach (Node node in _Repo.Node.ReadAllInTenant(tenantGuid, order, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, node.GraphGUID, node.GUID, null, null).ToList();
                if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, node.GraphGUID, node.GUID, null, null, null).ToList();
                if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, node.GraphGUID, node.GUID).ToList();
                yield return node;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
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
                foreach (Node node in _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, order, skip))
                {
                    List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, node.GraphGUID, node.GUID, null, null).ToList();
                    if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                    List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, node.GraphGUID, node.GUID, null, null, null).ToList();
                    if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                    node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, node.GraphGUID, node.GUID).ToList();
                    yield return node;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
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
                foreach (Node node in _Repo.Node.ReadMany(tenantGuid, graphGuid, labels, tags, expr, order, skip))
                {
                    List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, node.GUID, null, null).ToList();
                    if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                    List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, node.GUID, null, null, null).ToList();
                    if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                    node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, node.GUID).ToList();
                    yield return node;
                }
            }
        }

        /// <inheritdoc />
        public Node ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            Node node = _Repo.Node.ReadFirst(tenantGuid, graphGuid, labels, tags, expr, order);

            if (node != null)
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, node.GUID, null, null).ToList();
                if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, node.GUID, null, null, null).ToList();
                if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, node.GUID).ToList();
                return node;
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
            int skip = 0)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Node node in _Repo.Node.ReadMostConnected(tenantGuid, graphGuid, labels, tags, expr, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, node.GUID, null, null).ToList();
                if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, node.GUID, null, null, null).ToList();
                if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, node.GUID).ToList();
                yield return node;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Node> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            int skip = 0)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Node node in _Repo.Node.ReadLeastConnected(tenantGuid, graphGuid, labels, tags, expr, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, node.GUID, null, null).ToList();
                if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, node.GUID, null, null, null).ToList();
                if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, node.GUID).ToList();
                yield return node;
            }
        }

        /// <inheritdoc />
        public Node ReadByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            Node node = _Repo.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid);
            if (node == null) return null;
            List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, nodeGuid, null, null).ToList();
            if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);
            List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, nodeGuid, null, null, null).ToList();
            if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);
            node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, nodeGuid).ToList();
            return node;
        }

        /// <inheritdoc />
        public EnumerationResult<Node> Enumerate(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            EnumerationResult<Node> er = _Repo.Node.Enumerate(query);

            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                if (query.IncludeSubordinates)
                {
                    foreach (Node node in er.Objects)
                    {
                        List<LabelMetadata> allLabels = _Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null).ToList();
                        if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = _Repo.Tag.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, null, null).ToList();
                        if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                        node.Vectors = _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID).ToList();
                    }
                }

                if (!query.IncludeData)
                {
                    foreach (Node node in er.Objects)
                    {
                        node.Data = null;
                    }
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
            _Client.ValidateNodeExists(tenantGuid, graphGuid, nodeGuid);
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
        public bool ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return _Repo.Node.ExistsByGuid(tenantGuid, graphGuid, nodeGuid);
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
            _Client.ValidateNodeExists(tenantGuid, graphGuid, nodeGuid);

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
            _Client.ValidateNodeExists(tenantGuid, graphGuid, nodeGuid);

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
            _Client.ValidateNodeExists(tenantGuid, graphGuid, nodeGuid);

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
            _Client.ValidateNodeExists(tenantGuid, graphGuid, fromNodeGuid);
            _Client.ValidateNodeExists(tenantGuid, graphGuid, toNodeGuid);

            foreach (RouteDetail route in _Repo.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, edgeFilter, nodeFilter))
            {
                yield return route;
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}