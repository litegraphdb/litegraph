namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk.Interfaces;
    using PrettyId;

    /// <summary>
    /// Credential methods.
    /// </summary>
    public class CredentialMethods : ICredentialMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Credential methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public CredentialMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Credential> Create(Credential cred, CancellationToken token = default)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + cred.TenantGUID + "/credentials";
            return await _Sdk.PutCreate<Credential>(url, cred, token).ConfigureAwait(false);
        }
        
        /// <inheritdoc />
        public async Task<List<Credential>> ReadMany(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/credentials?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<Credential>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/credentials/" + guid;
            return await _Sdk.Get<Credential>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Credential>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/credentials?guids=" + string.Join(",", guids);
            return await _Sdk.Get<List<Credential>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Credential> Update(Credential cred, CancellationToken token = default)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + cred.TenantGUID + "/credentials/" + cred.GUID;
            return await _Sdk.PutUpdate<Credential>(url, cred, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/credentials/" + guid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/credentials/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Credential>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/credentials";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<Credential>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByBearerToken(string bearerToken, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(bearerToken)) throw new ArgumentNullException(nameof(bearerToken));

            string url = _Sdk.Endpoint + $"v1.0/credentials/bearer/{bearerToken}";
            return await _Sdk.Get<Credential>(url, null, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/credentials";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByUser(Guid tenantGuid, Guid userGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/users/" + userGuid + "/credentials";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
