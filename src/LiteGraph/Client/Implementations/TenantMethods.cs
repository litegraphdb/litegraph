namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Caching;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Tenant methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class TenantMethods : ITenantMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;
        private LRUCache<Guid, TenantMetadata> _TenantCache = null;

        #endregion

        #region Constructors-and-Factories

        /// <inheritdoc />
        public TenantMethods(LiteGraphClient client, GraphRepositoryBase repo, LRUCache<Guid, TenantMetadata> cache)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _TenantCache = cache;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TenantMetadata> Create(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            token.ThrowIfCancellationRequested();
            TenantMetadata created = await _Repo.Tenant.Create(tenant, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created tenant name " + created.Name + " GUID " + created.GUID);
            _TenantCache.AddReplace(created.GUID, created);
            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TenantMetadata> ReadMany(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0, [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants");

            await foreach (TenantMetadata tenant in _Repo.Tenant.ReadMany(order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tenant;
            }
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByGuid(Guid guid, CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenant with GUID " + guid);
            token.ThrowIfCancellationRequested();
            TenantMetadata tenant = await _Repo.Tenant.ReadByGuid(guid, token).ConfigureAwait(false);
            if (tenant != null) _TenantCache.AddReplace(tenant.GUID, tenant);
            return tenant;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TenantMetadata> ReadByGuids(List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants");
            await foreach (TenantMetadata obj in _Repo.Tenant.ReadByGuids(guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TenantMetadata>> Enumerate(EnumerationRequest query = null, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            return await _Repo.Tenant.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> Update(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "updating tenant with name " + tenant.Name + " GUID " + tenant.GUID);
            TenantMetadata updated = await _Repo.Tenant.Update(tenant, token).ConfigureAwait(false);
            if (updated != null) _TenantCache.AddReplace(tenant.GUID, tenant);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid guid, bool force = false, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(guid, token).ConfigureAwait(false);

            if (!force)
            {
                bool hasUsers = false;
                await using (var userEnumerator = _Repo.User.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await userEnumerator.MoveNextAsync().ConfigureAwait(false))
                        hasUsers = true;
                }
                if (hasUsers)
                    throw new InvalidOperationException("The specified tenant has dependent users.");

                if ((await _Repo.Credential.ReadAllInTenant(guid, token: token).ConfigureAwait(false)).Any())
                    throw new InvalidOperationException("The specified tenant has dependent credentials.");

                await using (var graphEnumerator = _Repo.Graph.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await graphEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified tenant has dependent graphs.");
                }

                await using (var nodeEnumerator = _Repo.Node.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await nodeEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified tenant has dependent nodes.");
                }

                await using (var edgeEnumerator = _Repo.Edge.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await edgeEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified tenant has dependent edges.");
                }

                await using (var labelEnumerator = _Repo.Label.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await labelEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified tenant has dependent labels.");
                }

                await using (var tagEnumerator = _Repo.Tag.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await tagEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified tenant has dependent tags.");
                }

                await using (var vectorEnumerator = _Repo.Vector.ReadAllInTenant(guid, token: token).GetAsyncEnumerator())
                {
                    if (await vectorEnumerator.MoveNextAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("The specified tenant has dependent vectors.");
                }
            }

            await _Repo.Tenant.DeleteByGuid(guid, force, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tenant " + guid + " (force " + force + ")");
            _TenantCache.TryRemove(guid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Tenant.ExistsByGuid(guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TenantStatistics> GetStatistics(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Tenant.GetStatistics(tenantGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, TenantStatistics>> GetStatistics(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Tenant.GetStatistics(token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
