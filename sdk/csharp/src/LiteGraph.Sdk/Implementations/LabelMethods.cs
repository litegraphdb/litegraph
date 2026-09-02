namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Label methods.
    /// </summary>
    public class LabelMethods : ILabelMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Label methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public LabelMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<LabelMetadata> Create(LabelMetadata label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + label.TenantGUID + "/labels";
            return await _Sdk.PutCreate<LabelMetadata>(url, label, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<LabelMetadata>> CreateMany(Guid tenantGuid, List<LabelMetadata> labels, CancellationToken token = default)
        {
            return await CreateMany(tenantGuid, labels, BulkCreateReturnModeEnum.Full, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<LabelMetadata>> CreateMany(Guid tenantGuid, List<LabelMetadata> labels, BulkCreateReturnModeEnum returnMode, CancellationToken token = default)
        {
            if (labels == null || labels.Count < 1) throw new ArgumentNullException(nameof(labels));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/bulk";
            url = BulkCreateUrlHelper.AppendReturnMode(url, returnMode);
            return await _Sdk.PutCreate<List<LabelMetadata>>(url, labels, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadMany(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<LabelMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/" + guid;
            return await _Sdk.Get<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels?guids=" + string.Join(",", guids);
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<LabelMetadata> Update(LabelMetadata label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + label.TenantGUID + "/labels/" + label.GUID;
            return await _Sdk.PutUpdate<LabelMetadata>(url, label, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/" + guid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/bulk";
            await _Sdk.Delete<List<Guid>>(url, guids, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/labels";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<LabelMetadata>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/all?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/labels/all?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadManyGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/labels?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadManyNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/labels?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> ReadManyEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid + "/labels?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            return await _Sdk.GetEnumeration<LabelMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/labels/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/labels/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteGraphLabels(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/labels";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/labels";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid + "/labels";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
