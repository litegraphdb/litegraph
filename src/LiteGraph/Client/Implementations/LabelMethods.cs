namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
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
        public async Task<LabelMetadata> Create(LabelMetadata label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            token.ThrowIfCancellationRequested();

            _Client.ValidateTenantExists(label.TenantGUID);
            await _Client.ValidateGraphExists(label.TenantGUID, label.GraphGUID, token).ConfigureAwait(false);

            if (label.NodeGUID != null) await _Client.ValidateNodeExists(
                label.TenantGUID,
                label.NodeGUID.Value, token).ConfigureAwait(false);

            if (label.EdgeGUID != null) await _Client.ValidateEdgeExists(
                label.TenantGUID,
                label.EdgeGUID.Value, token).ConfigureAwait(false);

            LabelMetadata created = await _Repo.Label.Create(label, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created label " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<LabelMetadata>> CreateMany(Guid tenantGuid, List<LabelMetadata> labels, CancellationToken token = default)
        {
            if (labels == null || labels.Count < 1) return new List<LabelMetadata>();
            token.ThrowIfCancellationRequested();

            _Client.ValidateTenantExists(tenantGuid);
            foreach (LabelMetadata label in labels)
            {
                await _Client.ValidateGraphExists(label.TenantGUID, label.GraphGUID, token).ConfigureAwait(false);

                if (string.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");

                if (label.NodeGUID != null) await _Client.ValidateNodeExists(
                    label.TenantGUID,
                    label.NodeGUID.Value, token).ConfigureAwait(false);

                if (label.EdgeGUID != null) await _Client.ValidateEdgeExists(
                    label.TenantGUID,
                    label.EdgeGUID.Value, token).ConfigureAwait(false);

                label.TenantGUID = tenantGuid;
            }

            labels = await _Repo.Label.CreateMany(tenantGuid, labels, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created " + labels.Count + " label(s)");
            return labels;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            await foreach (LabelMetadata curr in _Repo.Label.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadAllInGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            await foreach (LabelMetadata curr in _Repo.Label.ReadAllInGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            await foreach (LabelMetadata curr in _Repo.Label.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, label, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            await foreach (LabelMetadata curr in _Repo.Label.ReadManyGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            await foreach (LabelMetadata curr in _Repo.Label.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");

            await foreach (LabelMetadata curr in _Repo.Label.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return curr;
            }
        }

        /// <inheritdoc />
        public async Task<LabelMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving label with GUID " + guid);

            return await _Repo.Label.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving labels");
            await foreach (LabelMetadata obj in _Repo.Label.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            return await _Repo.Label.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<LabelMetadata> Update(LabelMetadata label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            token.ThrowIfCancellationRequested();

            _Client.ValidateTenantExists(label.TenantGUID);
            await _Client.ValidateGraphExists(label.TenantGUID, label.GraphGUID, token).ConfigureAwait(false);

            if (label.NodeGUID != null) await _Client.ValidateNodeExists(
                label.TenantGUID,
                label.NodeGUID.Value, token).ConfigureAwait(false);

            if (label.EdgeGUID != null) await _Client.ValidateEdgeExists(
                label.TenantGUID,
                label.EdgeGUID.Value, token).ConfigureAwait(false);

            label = await _Repo.Label.Update(label, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "updated label " + label.Label + " in GUID " + label.GUID);
            return label;
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            _Client.ValidateTenantExists(tenantGuid);
            await _Repo.Label.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Repo.Label.DeleteAllInGraph(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in graph " + graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteGraphLabels(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Repo.Label.DeleteGraphLabels(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels for graph " + graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);
            await _Repo.Label.DeleteNodeLabels(tenantGuid, graphGuid, nodeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels for node " + nodeGuid);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateEdgeExists(tenantGuid, edgeGuid, token).ConfigureAwait(false);
            await _Repo.Label.DeleteEdgeLabels(tenantGuid, graphGuid, edgeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels for edge " + edgeGuid);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            _Client.ValidateTenantExists(tenantGuid);
            LabelMetadata label = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            if (label == null) return;
            await _Repo.Label.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted label " + label.GUID);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default)
        {
            _Client.ValidateTenantExists(tenantGuid);
            if (graphGuid != null) await _Client.ValidateGraphExists(tenantGuid, graphGuid.Value, token).ConfigureAwait(false);
            await _Repo.Label.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            _Client.ValidateTenantExists(tenantGuid);
            await _Repo.Label.DeleteMany(tenantGuid, guids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted labels in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            _Client.ValidateTenantExists(tenantGuid);
            return await _Repo.Label.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
