namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// User methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class UserMethods : IUserMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// User methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public UserMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public UserMaster Create(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (ExistsByEmail(user.TenantGUID, user.Email)) throw new InvalidOperationException("A user with the specified email address already exists within the specified tenant.");
            string createQuery = UserQueries.Insert(user);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            UserMaster created = Converters.UserFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Repo.ExecuteQuery(UserQueries.Delete(tenantGuid, guid), true);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(UserQueries.DeleteAllInTenant(tenantGuid), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid userGuid)
        {
            return (ReadByGuid(tenantGuid, userGuid) != null);
        }

        /// <inheritdoc />
        public bool ExistsByEmail(Guid tenantGuid, string email)
        {
            return (ReadByEmail(tenantGuid, email) != null);
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(UserQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    UserMaster user = Converters.UserFromDataRow(result.Rows[i]);
                    yield return user;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public UserMaster ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(UserQueries.Select(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public List<TenantMetadata> ReadTenantsByEmail(string email)
        {
            DataTable result = _Repo.ExecuteQuery(UserQueries.SelectTenantsByEmail(email));
            List<TenantMetadata> tenants = new List<TenantMetadata>();
            if (result != null && result.Rows.Count > 0) 
                foreach (DataRow row in result.Rows) tenants.Add(Converters.TenantFromDataRow(row));
            return tenants;
        }

        /// <inheritdoc />
        public UserMaster ReadByEmail(Guid tenantGuid, string email)
        {
            DataTable result = _Repo.ExecuteQuery(UserQueries.Select(tenantGuid, email));
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(UserQueries.SelectMany(tenantGuid, email, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.UserFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public UserMaster Update(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Converters.UserFromDataRow(_Repo.ExecuteQuery(UserQueries.Update(user), true).Rows[0]);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
