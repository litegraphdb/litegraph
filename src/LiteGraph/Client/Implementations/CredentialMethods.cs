namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Credential methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class CredentialMethods : ICredentialMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Credential methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public CredentialMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Credential> Create(Credential cred, CancellationToken token = default)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(cred.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateUserExists(cred.TenantGUID, cred.UserGUID, token).ConfigureAwait(false);
            Credential created = await _Repo.Credential.Create(cred, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created credential " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Credential>> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credentials");
            return await _Repo.Credential.ReadAllInTenant(tenantGuid, order, skip, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Credential>> ReadMany(
            Guid? tenantGuid,
            Guid? userGuid = null,
            string bearerToken = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credentials");
            return await _Repo.Credential.ReadMany(tenantGuid, userGuid, bearerToken, order, skip, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credential with GUID " + guid);
            return await _Repo.Credential.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Credential>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credentials");
            return await _Repo.Credential.ReadByGuids(tenantGuid, guids, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByBearerToken(string bearerToken, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credential with token " + bearerToken);
            return await _Repo.Credential.ReadByBearerToken(bearerToken, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Credential>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            return await _Repo.Credential.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Credential> Update(Credential cred, CancellationToken token = default)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(cred.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateUserExists(cred.TenantGUID, cred.UserGUID, token).ConfigureAwait(false);
            Credential updated = await _Repo.Credential.Update(cred, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "updated credential " + cred.Name + " GUID " + cred.GUID);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Credential.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted credentials for tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            Credential credential = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            if (credential == null) return;
            await _Repo.Credential.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted credential " + credential.Name + " GUID " + credential.GUID);
        }

        /// <inheritdoc />
        public async Task DeleteByUser(Guid tenantGuid, Guid userGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateUserExists(tenantGuid, userGuid, token).ConfigureAwait(false);
            await _Repo.Credential.DeleteByUser(tenantGuid, userGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted credentials for user " + userGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Credential.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
