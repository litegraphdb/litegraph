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
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            foreach (Graph graph in _Repo.Graph.ReadAllInTenant(tenantGuid, order, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graph.GUID, null, null, null).ToList();
                if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graph.GUID, null, null, null, null).ToList();
                if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);

                graph.Vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graph.GUID).ToList();
                yield return graph;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
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
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graphs");
            foreach (Graph graph in _Repo.Graph.ReadMany(tenantGuid, labels, tags, expr, order, skip))
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graph.GUID, null, null, null).ToList();
                if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);
                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graph.GUID, null, null, null, null).ToList();
                if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);
                graph.Vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graph.GUID).ToList();
                yield return graph;
            }
        }

        /// <inheritdoc />
        public Graph ReadFirst(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            if (order == EnumerationOrderEnum.MostConnected
                || order == EnumerationOrderEnum.LeastConnected)
                throw new ArgumentException("Connectedness enumeration orders are only available to node retrieval within a graph.");

            _Client.ValidateTenantExists(tenantGuid);

            Graph graph = _Repo.Graph.ReadFirst(tenantGuid, labels, tags, expr, order);

            if (graph != null)
            {
                List<LabelMetadata> allLabels = _Repo.Label.ReadMany(tenantGuid, graph.GUID, null, null, null).ToList();
                if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = _Repo.Tag.ReadMany(tenantGuid, graph.GUID, null, null, null, null).ToList();
                if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);

                graph.Vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graph.GUID).ToList();
                return graph;
            }

            return null;
        }

        /// <inheritdoc />
        public Graph ReadByGuid(Guid tenantGuid, Guid graphGuid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving graph with GUID " + graphGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            Graph graph = _Repo.Graph.ReadByGuid(tenantGuid, graphGuid);
            if (graph == null) return null;
            List<LabelMetadata> allLabels = _Repo.Label.ReadMany(
                tenantGuid,
                graph.GUID,
                null,
                null,
                null).ToList();
            if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);
            List<TagMetadata> allTags = _Repo.Tag.ReadMany(
                tenantGuid,
                graphGuid,
                null,
                null,
                null,
                null).ToList();
            if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);
            graph.Vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graphGuid).ToList();
            return graph;
        }

        /// <inheritdoc />
        public EnumerationResult<Graph> Enumerate(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            EnumerationResult<Graph> er = _Repo.Graph.Enumerate(query);


            if (er != null
                && er.Objects != null
                && er.Objects.Count > 0)
            {
                if (query.IncludeSubordinates)
                {
                    foreach (Graph graph in er.Objects)
                    {
                        List<LabelMetadata> allLabels = _Repo.Label.ReadMany(graph.TenantGUID, graph.GUID, null, null, null).ToList();
                        if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);

                        List<TagMetadata> allTags = _Repo.Tag.ReadMany(graph.TenantGUID, graph.GUID, null, null, null, null).ToList();
                        if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);

                        graph.Vectors = _Repo.Vector.ReadManyGraph(graph.TenantGUID, graph.GUID).ToList();
                    }
                }

                if (!query.IncludeData)
                {
                    foreach (Graph graph in er.Objects)
                    {
                        graph.Data = null;
                    }
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

        #endregion

        #region Private-Methods

        #endregion
    }
}