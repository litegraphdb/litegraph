namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// User methods.
    /// </summary>
    public class UserMethods : IUserMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// User methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public UserMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<UserMaster> Create(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + user.TenantGUID + "/users";
            return await _Sdk.PutCreate<UserMaster>(url, user, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<UserMaster>> ReadMany(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/users?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<UserMaster>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/users/" + guid;
            return await _Sdk.Get<UserMaster>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<UserMaster>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/users?guids=" + string.Join(",", guids);
            return await _Sdk.Get<List<UserMaster>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<UserMaster> Update(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + user.TenantGUID + "/users/" + user.GUID;
            return await _Sdk.PutUpdate<UserMaster>(url, user, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/users/" + guid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/users/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<UserMaster>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/users";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<UserMaster>>(url, query, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
