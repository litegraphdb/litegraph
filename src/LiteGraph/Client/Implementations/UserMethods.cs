namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// User methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class UserMethods : IUserMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// User methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public UserMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public UserMaster Create(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Client.ValidateTenantExists(user.TenantGUID);
            UserMaster created = _Repo.User.Create(user);
            _Client.Logging.Log(SeverityEnum.Info, "created user email " + created.Email + " GUID " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");

            foreach (UserMaster user in _Repo.User.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return user;
            }
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");

            foreach (UserMaster user in _Repo.User.ReadMany(tenantGuid, email, order, skip))
            {
                yield return user;
            }
        }

        /// <inheritdoc />
        public UserMaster ReadByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving user with GUID " + guid);

            return _Repo.User.ReadByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public UserMaster ReadByEmail(Guid tenantGuid, string email)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving user with email " + email);

            return _Repo.User.ReadByEmail(tenantGuid, email);
        }

        /// <inheritdoc />
        public List<TenantMetadata> ReadTenantsByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants associated with user " + email);
            return _Repo.User.ReadTenantsByEmail(email);
        }

        /// <inheritdoc />
        public UserMaster Update(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Client.ValidateTenantExists(user.TenantGUID);
            user = _Repo.User.Update(user);
            _Client.Logging.Log(SeverityEnum.Debug, "updated user with email " + user.Email + " GUID " + user.GUID);
            return user;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            UserMaster user = _Repo.User.ReadByGuid(tenantGuid, guid);
            if (user == null) return;
            _Repo.User.DeleteByGuid(tenantGuid, guid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted user with email " + user.Email + " GUID " + user.GUID);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.User.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted users for tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            return _Repo.User.ExistsByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public bool ExistsByEmail(Guid tenantGuid, string email)
        {
            return _Repo.User.ExistsByEmail(tenantGuid, email);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
