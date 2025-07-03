namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
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
        public TenantMetadata Create(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            TenantMetadata created = _Repo.Tenant.Create(tenant);
            _Client.Logging.Log(SeverityEnum.Info, "created tenant name " + created.Name + " GUID " + created.GUID);
            _TenantCache.AddReplace(created.GUID, created);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<TenantMetadata> ReadMany(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants");

            foreach (TenantMetadata tenant in _Repo.Tenant.ReadMany(order, skip))
            {
                yield return tenant;
            }
        }

        /// <inheritdoc />
        public TenantMetadata ReadByGuid(Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenant with GUID " + guid);
            TenantMetadata tenant = _Repo.Tenant.ReadByGuid(guid);
            if (tenant != null) _TenantCache.AddReplace(tenant.GUID, tenant);
            return tenant;
        }

        /// <inheritdoc />
        public IEnumerable<TenantMetadata> ReadByGuids(List<Guid> guids)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants");
            foreach (TenantMetadata obj in _Repo.Tenant.ReadByGuids(guids))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<TenantMetadata> Enumerate(EnumerationRequest query)
        {
            if (query == null) query = new EnumerationRequest();
            return _Repo.Tenant.Enumerate(query);
        }

        /// <inheritdoc />
        public TenantMetadata Update(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            _Client.Logging.Log(SeverityEnum.Debug, "updating tenant with name " + tenant.Name + " GUID " + tenant.GUID);
            TenantMetadata updated = _Repo.Tenant.Update(tenant);
            if (updated != null) _TenantCache.AddReplace(tenant.GUID, tenant);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid guid, bool force = false)
        {
            _Client.ValidateTenantExists(guid);

            if (!force)
            {
                if (_Repo.User.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent users.");

                if (_Repo.Credential.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent credentials.");

                if (_Repo.Graph.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent graphs.");

                if (_Repo.Node.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent nodes.");

                if (_Repo.Edge.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent edges.");

                if (_Repo.Label.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent labels.");

                if (_Repo.Tag.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent tags.");

                if (_Repo.Vector.ReadAllInTenant(guid).Any())
                    throw new InvalidOperationException("The specified tenant has dependent vectors.");
            }

            _Repo.Tenant.DeleteByGuid(guid, force);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tenant " + guid + " (force " + force + ")");
            _TenantCache.TryRemove(guid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid guid)
        {
            return _Repo.Tenant.ExistsByGuid(guid);
        }

        /// <inheritdoc />
        public TenantStatistics GetStatistics(Guid tenantGuid)
        {
            return _Repo.Tenant.GetStatistics(tenantGuid);
        }

        /// <inheritdoc />
        public Dictionary<Guid, TenantStatistics> GetStatistics()
        {
            return _Repo.Tenant.GetStatistics();
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
