namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;
    using SQLitePCL;
    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Label methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class LabelMethods : ILabelMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Label methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public LabelMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public LabelMetadata Create(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            _Client.ValidateTenantExists(label.TenantGUID);
            _Client.ValidateGraphExists(label.TenantGUID, label.GraphGUID);

            if (label.NodeGUID != null) _Client.ValidateNodeExists(
                label.TenantGUID,
                label.GraphGUID,
                label.NodeGUID.Value);

            if (label.EdgeGUID != null) _Client.ValidateEdgeExists(
                label.TenantGUID,
                label.GraphGUID,
                label.EdgeGUID.Value);

            LabelMetadata created = _Repo.Label.Create(label);
            _Client.Logging.Log(SeverityEnum.Info, "created label " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public List<LabelMetadata> CreateMany(Guid tenantGuid, List<LabelMetadata> labels)
        {
            if (labels == null || labels.Count < 1) return new List<LabelMetadata>();

            _Client.ValidateTenantExists(tenantGuid);
            foreach (LabelMetadata label in labels)
            {
                _Client.ValidateGraphExists(label.TenantGUID, label.GraphGUID);

                if (string.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");

                if (label.NodeGUID != null) _Client.ValidateNodeExists(
                    label.TenantGUID,
                    label.GraphGUID,
                    label.NodeGUID.Value);

                if (label.EdgeGUID != null) _Client.ValidateEdgeExists(
                    label.TenantGUID,
                    label.GraphGUID,
                    label.EdgeGUID.Value);

                label.TenantGUID = tenantGuid;
            }

            labels = _Repo.Label.CreateMany(tenantGuid, labels);
            _Client.Logging.Log(SeverityEnum.Info, "created " + labels.Count + " label(s)");
            return labels;
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repo.Label.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadAllInGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repo.Label.ReadAllInGraph(tenantGuid, graphGuid, order, skip))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repo.Label.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, label, order, skip))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repo.Label.ReadManyGraph(tenantGuid, graphGuid, order, skip))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repo.Label.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repo.Label.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public LabelMetadata ReadByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving label with GUID " + guid);

            return _Repo.Label.ReadByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public EnumerationResult<LabelMetadata> Enumerate(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Repo.Label.Enumerate(query);
        }

        /// <inheritdoc />
        public LabelMetadata Update(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            _Client.ValidateTenantExists(label.TenantGUID);
            _Client.ValidateGraphExists(label.TenantGUID, label.GraphGUID);

            if (label.NodeGUID != null) _Client.ValidateNodeExists(
                label.TenantGUID,
                label.GraphGUID,
                label.NodeGUID.Value);

            if (label.EdgeGUID != null) _Client.ValidateEdgeExists(
                label.TenantGUID,
                label.GraphGUID,
                label.EdgeGUID.Value);

            label = _Repo.Label.Update(label);
            _Client.Logging.Log(SeverityEnum.Info, "updated label " + label.Label + " in GUID " + label.GUID);
            return label;
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Label.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Label.DeleteAllInGraph(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in graph " + graphGuid);
        }

        /// <inheritdoc />
        public void DeleteGraphLabels(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Label.DeleteGraphLabels(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels for graph " + graphGuid);
        }

        /// <inheritdoc />
        public void DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, graphGuid, nodeGuid);
            _Repo.Label.DeleteNodeLabels(tenantGuid, graphGuid, nodeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels for node " + nodeGuid);
        }

        /// <inheritdoc />
        public void DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateEdgeExists(tenantGuid, graphGuid, edgeGuid);
            _Repo.Label.DeleteEdgeLabels(tenantGuid, graphGuid, edgeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels for edge " + edgeGuid);
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            LabelMetadata label = ReadByGuid(tenantGuid, guid);
            if (label == null) return;
            _Repo.Label.DeleteByGuid(tenantGuid, guid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted label " + label.GUID);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            _Client.ValidateTenantExists(tenantGuid);
            if (graphGuid != null) _Client.ValidateGraphExists(tenantGuid, graphGuid.Value);
            _Repo.Label.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Label.DeleteMany(tenantGuid, guids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            return _Repo.Label.ExistsByGuid(tenantGuid, guid);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
