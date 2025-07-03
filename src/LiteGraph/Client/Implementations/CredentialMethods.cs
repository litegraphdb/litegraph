namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;
    using PrettyId;

    using LoggingSettings = LoggingSettings;

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
        public Credential Create(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            _Client.ValidateTenantExists(cred.TenantGUID);
            _Client.ValidateUserExists(cred.TenantGUID, cred.UserGUID);
            Credential created = _Repo.Credential.Create(cred);
            _Client.Logging.Log(SeverityEnum.Info, "created credential " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Credential> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credentials");
            foreach (Credential credential in _Repo.Credential.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return credential;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Credential> ReadMany(
            Guid? tenantGuid,
            Guid? userGuid = null,
            string bearerToken = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credentials");
            foreach (Credential credential in _Repo.Credential.ReadMany(tenantGuid, userGuid, bearerToken, order, skip))
            {
                yield return credential;
            }
        }

        /// <inheritdoc />
        public Credential ReadByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credential with GUID " + guid);
            return _Repo.Credential.ReadByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public IEnumerable<Credential> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credentials");
            foreach (Credential obj in _Repo.Credential.ReadByGuids(tenantGuid, guids))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public Credential ReadByBearerToken(string bearerToken)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving credential with token " + bearerToken);
            return _Repo.Credential.ReadByBearerToken(bearerToken);
        }

        /// <inheritdoc />
        public EnumerationResult<Credential> Enumerate(EnumerationRequest query)
        {
            if (query == null) query = new EnumerationRequest();
            return _Repo.Credential.Enumerate(query);
        }

        /// <inheritdoc />
        public Credential Update(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            _Client.ValidateTenantExists(cred.TenantGUID);
            _Client.ValidateUserExists(cred.TenantGUID, cred.UserGUID);
            Credential updated = _Repo.Credential.Update(cred);
            _Client.Logging.Log(SeverityEnum.Debug, "updated credential " + cred.Name + " GUID " + cred.GUID);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Credential.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted credentials for tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            Credential credential = ReadByGuid(tenantGuid, guid);
            if (credential == null) return;
            _Repo.Credential.DeleteByGuid(tenantGuid, guid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted credential " + credential.Name + " GUID " + credential.GUID);
        }

        /// <inheritdoc />
        public void DeleteByUser(Guid tenantGuid, Guid userGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateUserExists(tenantGuid, userGuid);
            _Repo.Credential.DeleteByUser(tenantGuid, userGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted credentials for user " + userGuid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            return _Repo.Credential.ExistsByGuid(tenantGuid, guid);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
