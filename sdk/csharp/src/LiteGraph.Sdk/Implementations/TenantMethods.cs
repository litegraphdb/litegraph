namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Tenant methods.
    /// </summary>
    public class TenantMethods : ITenantMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tenant methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public TenantMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TenantMetadata> Create(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            string url = _Sdk.Endpoint + "v1.0/tenants";
            return await _Sdk.PutCreate<TenantMetadata>(url, tenant, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TenantMetadata>> ReadMany(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<TenantMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByGuid(Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + guid;
            return await _Sdk.Get<TenantMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TenantMetadata>> ReadByGuids(List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants?guids=" + string.Join(",", guids);
            return await _Sdk.Get<List<TenantMetadata>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> Update(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenant.GUID;
            return await _Sdk.PutUpdate<TenantMetadata>(url, tenant, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid guid, bool force = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + guid;
            if (force) url += "?force";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TenantMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            string url = _Sdk.Endpoint + "v2.0/tenants";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<TenantMetadata>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TenantStatistics> GetStatistics(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/stats";
            return await _Sdk.Get<TenantStatistics>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, TenantStatistics>> GetStatistics(CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/stats";
            return await _Sdk.Get<Dictionary<Guid, TenantStatistics>>(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
