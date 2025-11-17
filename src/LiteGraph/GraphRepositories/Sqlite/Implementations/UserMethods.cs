namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;

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
        public async Task<UserMaster> Create(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (await ExistsByEmail(user.TenantGUID, user.Email, token).ConfigureAwait(false)) throw new InvalidOperationException("A user with the specified email address already exists within the specified tenant.");
            string createQuery = UserQueries.Insert(user);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            UserMaster created = Converters.UserFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            await _Repo.ExecuteQueryAsync(UserQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            await _Repo.ExecuteQueryAsync(UserQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid userGuid, CancellationToken token = default)
        {
            return (await ReadByGuid(tenantGuid, userGuid, token).ConfigureAwait(false) != null);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByEmail(Guid tenantGuid, string email, CancellationToken token = default)
        {
            return (await ReadByEmail(tenantGuid, email, token).ConfigureAwait(false) != null);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    UserMaster user = Converters.UserFromDataRow(result.Rows[i]);
                    yield return user;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<UserMaster> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.UserFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async Task<List<TenantMetadata>> ReadTenantsByEmail(string email, CancellationToken token = default)
        {
            DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.SelectTenantsByEmail(email), false, token).ConfigureAwait(false);
            List<TenantMetadata> tenants = new List<TenantMetadata>();
            if (result != null && result.Rows.Count > 0) 
                foreach (DataRow row in result.Rows) tenants.Add(Converters.TenantFromDataRow(row));
            return tenants;
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByEmail(Guid tenantGuid, string email, CancellationToken token = default)
        {
            DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.SelectByEmail(tenantGuid, email), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.SelectMany(tenantGuid, email, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.UserFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<UserMaster>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            UserMaster marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<UserMaster> ret = new EnumerationResult<UserMaster>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = await GetRecordCount(query.TenantGUID, query.Ordering, null, token).ConfigureAwait(false);

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
                DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.GetRecordPage(
                    query.TenantGUID,
                    query.MaxResults,
                    query.Skip,
                    query.Ordering,
                    marker), false, token).ConfigureAwait(false);

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

                    ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);

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
        public async Task<int> GetRecordCount(Guid? tenantGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, Guid? markerGuid = null, CancellationToken token = default)
        {
            UserMaster marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(UserQueries.GetRecordCount(
                tenantGuid,
                order,
                marker), false, token).ConfigureAwait(false);

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
        public async Task<UserMaster> Update(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Converters.UserFromDataRow((await _Repo.ExecuteQueryAsync(UserQueries.Update(user), true, token).ConfigureAwait(false)).Rows[0]);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
