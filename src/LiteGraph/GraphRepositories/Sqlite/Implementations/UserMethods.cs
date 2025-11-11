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
            DataTable result = _Repo.ExecuteQuery(UserQueries.SelectByGuid(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = _Repo.ExecuteQuery(UserQueries.SelectByGuids(tenantGuid, guids));

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                yield return Converters.UserFromDataRow(result.Rows[i]);
            }
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
            DataTable result = _Repo.ExecuteQuery(UserQueries.SelectByEmail(tenantGuid, email));
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
        public EnumerationResult<UserMaster> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            UserMaster marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<UserMaster> ret = new EnumerationResult<UserMaster>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.Ordering, null);

            if (ret.TotalRecords < 1)
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
                ret.RecordsRemaining = 0;
                ret.Timestamp.End = DateTime.UtcNow;
                return ret;
            }
            else
            {
                DataTable result = _Repo.ExecuteQuery(UserQueries.GetRecordPage(
                    query.TenantGUID,
                    query.MaxResults,
                    query.Skip,
                    query.Ordering,
                    marker));

                if (result == null || result.Rows.Count < 1)
                {
                    ret.ContinuationToken = null;
                    ret.EndOfResults = true;
                    ret.RecordsRemaining = 0;
                    ret.Timestamp.End = DateTime.UtcNow;
                    return ret;
                }
                else
                {
                    ret.Objects = Converters.UsersFromDataTable(result);

                    UserMaster lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.Ordering, lastItem.GUID);

                    if (ret.RecordsRemaining > 0)
                    {
                        ret.ContinuationToken = lastItem.GUID;
                        ret.EndOfResults = false;
                        ret.Timestamp.End = DateTime.UtcNow;
                        return ret;
                    }
                    else
                    {
                        ret.ContinuationToken = null;
                        ret.EndOfResults = true;
                        ret.Timestamp.End = DateTime.UtcNow;
                        return ret;
                    }
                }
            }
        }

        /// <inheritdoc />
        public int GetRecordCount(Guid? tenantGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, Guid? markerGuid = null)
        {
            UserMaster marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(UserQueries.GetRecordCount(
                tenantGuid,
                order,
                marker));

            if (result != null && result.Rows != null && result.Rows.Count > 0)
            {
                if (result.Columns.Contains("record_count"))
                {
                    return Convert.ToInt32(result.Rows[0]["record_count"]);
                }
            }
            return 0;
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
