namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Caching;
    using ExpressionTree;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;
    using SQLitePCL;
    using LoggingSettings = LoggingSettings;

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
        public Edge Create(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            _Client.ValidateTenantExists(edge.TenantGUID);
            _Client.ValidateGraphExists(edge.TenantGUID, edge.GraphGUID);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.GraphGUID, edge.To);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.GraphGUID, edge.From);
            _Client.ValidateLabels(edge.Labels);
            _Client.ValidateTags(edge.Tags);
            _Client.ValidateVectors(edge.Vectors);
            Edge created = _Repo.Edge.Create(edge);
            created.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
            created.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
            created.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Info, "created edge " + created.GUID + " in graph " + created.GraphGUID);
            _EdgeCache.AddReplace(created.GUID, created);
            return created;
        }

        /// <inheritdoc />
        public List<Edge> CreateMany(Guid tenantGuid, Guid graphGuid, List<Edge> edges)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            List<Edge> created = _Repo.Edge.CreateMany(tenantGuid, graphGuid, edges);
            _Client.Logging.Log(SeverityEnum.Info, "created " + created.Count + " edges(s) in graph " + graphGuid);

            // Add created edges to cache
            foreach (Edge edge in created)
            {
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                _EdgeCache.AddReplace(edge.GUID, edge);
            }

            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);

            foreach (Edge edge in _Repo.Edge.ReadAllInTenant(tenantGuid, order, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, edge.GraphGUID, null, edge.GUID, null).ToList();
                if (allLabels != null) edge.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, edge.GraphGUID, null, edge.GUID, null, null).ToList();
                if (allTags != null) edge.Tags = TagMetadata.ToNameValueCollection(allTags);

                edge.Vectors = _Repo.Vector.ReadManyEdge(tenantGuid, edge.GraphGUID, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Edge edge in _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, order, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, null, edge.GUID, null).ToList();
                if (allLabels != null) edge.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, null, edge.GUID, null, null).ToList();
                if (allTags != null) edge.Tags = TagMetadata.ToNameValueCollection(allTags);

                edge.Vectors = _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Edge edge in _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels, tags, expr, order, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, null, edge.GUID, null).ToList();
                if (allLabels != null) edge.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, null, edge.GUID, null, null).ToList();
                if (allTags != null) edge.Tags = TagMetadata.ToNameValueCollection(allTags);

                edge.Vectors = _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public Edge ReadByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            Edge edge = _Repo.Edge.ReadByGuid(tenantGuid, graphGuid, edgeGuid);
            if (edge == null) return null;
            List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graphGuid, null, edgeGuid, null).ToList();
            if (allLabels != null) edge.Labels = LabelMetadata.ToListString(allLabels);
            List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graphGuid, null, edgeGuid, null, null).ToList();
            if (allTags != null) edge.Tags = TagMetadata.ToNameValueCollection(allTags);
            edge.Vectors = _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid, edge.GUID).ToList();
            return edge;
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadNodeEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Edge edge in _Repo.Edge.ReadNodeEdges(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip))
            {
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadEdgesFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Edge edge in _Repo.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip))
            {
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadEdgesToNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Edge edge in _Repo.Edge.ReadEdgesToNode(
                tenantGuid,
                graphGuid,
                nodeGuid,
                labels,
                tags,
                edgeFilter,
                order,
                skip))
            {
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadEdgesBetweenNodes(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (Edge edge in _Repo.Edge.ReadEdgesBetweenNodes(
                tenantGuid,
                graphGuid,
                fromNodeGuid,
                toNodeGuid,
                labels,
                tags,
                edgeFilter,
                order,
                skip))
            {
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                yield return edge;
            }
        }

        /// <inheritdoc />
        public Edge Update(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            _Client.ValidateTenantExists(edge.TenantGUID);
            _Client.ValidateGraphExists(edge.TenantGUID, edge.GraphGUID);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.GraphGUID, edge.To);
            _Client.ValidateNodeExists(edge.TenantGUID, edge.GraphGUID, edge.From);
            _Client.ValidateLabels(edge.Labels);
            _Client.ValidateTags(edge.Tags);
            _Client.ValidateVectors(edge.Vectors);
            Edge updated = _Repo.Edge.Update(edge);
            updated.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
            updated.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
            updated.Vectors = _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
            _Client.Logging.Log(SeverityEnum.Debug, "updated edge " + updated.GUID + " in graph " + updated.GraphGUID);
            _EdgeCache.AddReplace(updated.GUID, updated);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Client.ValidateEdgeExists(tenantGuid, graphGuid, edgeGuid);
            _Repo.Edge.DeleteByGuid(tenantGuid, graphGuid, edgeGuid);
            _Client.Logging.Log(SeverityEnum.Debug, "deleted edge " + edgeGuid + " in graph " + graphGuid);
            _EdgeCache.TryRemove(edgeGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Edge.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges in tenant " + tenantGuid);
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Edge.DeleteAllInGraph(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges in graph " + graphGuid);
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            if (edgeGuids == null || edgeGuids.Count < 1) return;
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Edge.DeleteMany(tenantGuid, graphGuid, edgeGuids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted " + edgeGuids.Count + " edge(s) in graph " + graphGuid);

            foreach (Guid edgeGuid in edgeGuids)
            {
                _EdgeCache.TryRemove(edgeGuid);
            }
        }

        /// <inheritdoc />
        public void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, graphGuid, nodeGuid);
            _Repo.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges for node " + nodeGuid);
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted edges for " + nodeGuids.Count + " node(s)");
            _EdgeCache.Clear();
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            return _Repo.Edge.ExistsByGuid(tenantGuid, graphGuid, edgeGuid);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}