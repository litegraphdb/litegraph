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
    /// Credential methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class CredentialMethods : ICredentialMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Credential methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public CredentialMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Credential> Create(Credential cred, CancellationToken token = default)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            token.ThrowIfCancellationRequested();
            string createQuery = CredentialQueries.Insert(cred);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            Credential created = Converters.CredentialFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(CredentialQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(CredentialQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByUser(Guid tenantGuid, Guid userGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(CredentialQueries.DeleteUser(tenantGuid, userGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Credential cred = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            return (cred != null);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Credential> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.SelectAllInTenant(
                    tenantGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) yield break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.CredentialFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) yield break;
            }
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.CredentialFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Credential> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.CredentialFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByBearerToken(string bearerToken, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bearerToken)) throw new ArgumentNullException(nameof(bearerToken));
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.SelectByToken(bearerToken), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.CredentialFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Credential> ReadMany(
            Guid? tenantGuid,
            Guid? userGuid,
            string bearerToken,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.Select(
                    tenantGuid,
                    userGuid,
                    bearerToken,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) yield break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.CredentialFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) yield break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Credential>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            Credential marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken + " could not be found.");
            }

            EnumerationResult<Credential> ret = new EnumerationResult<Credential>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;

            ret.TotalRecords = await GetRecordCount(query.TenantGUID, query.UserGUID, query.Ordering, null, token).ConfigureAwait(false);

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
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.GetRecordPage(
                    query.TenantGUID,
                    query.UserGUID,
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
                    ret.Objects = Converters.CredentialFromDataTable(result);

                    Credential lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.UserGUID, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);
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
        public async Task<int> GetRecordCount(
            Guid? tenantGuid, 
            Guid? userGuid, 
            EnumerationOrderEnum order, 
            Guid? markerGuid,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Credential marker = null;

            if (tenantGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.GetRecordCount(
                tenantGuid,
                userGuid,
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
        public async Task<Credential> Update(Credential cred, CancellationToken token = default)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(CredentialQueries.Update(cred), true, token).ConfigureAwait(false);
            return Converters.CredentialFromDataRow(result.Rows[0]);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
