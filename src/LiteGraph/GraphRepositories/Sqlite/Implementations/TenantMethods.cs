namespace LiteGraph.GraphRepositories.Sqlite.Implementations
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
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Tenant methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class TenantMethods : ITenantMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tenant methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public TenantMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TenantMetadata> Create(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            token.ThrowIfCancellationRequested();
            string createQuery = TenantQueries.Insert(tenant);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            TenantMetadata created = Converters.TenantFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid guid, bool force = false, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TenantQueries.Delete(guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            TenantMetadata tenant = await ReadByGuid(tenantGuid, token).ConfigureAwait(false);
            return tenant != null;
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(TenantQueries.SelectByGuid(guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.TenantFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TenantMetadata> ReadByGuids(List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(TenantQueries.SelectByGuids(guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.TenantFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TenantMetadata> ReadMany(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(TenantQueries.SelectMany(_Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.TenantFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TenantMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            TenantMetadata marker = null;

            if (query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<TenantMetadata> ret = new EnumerationResult<TenantMetadata>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;

            ret.TotalRecords = await GetRecordCount(query.Ordering, null, token).ConfigureAwait(false);

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

                DataTable result = await _Repo.ExecuteQueryAsync(TenantQueries.GetRecordPage(
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
                    ret.Objects = Converters.TenantsFromDataTable(result);

                    TenantMetadata lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = await GetRecordCount(query.Ordering, lastItem.GUID, token).ConfigureAwait(false);

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
        public async Task<int> GetRecordCount(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, Guid? markerGuid = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            TenantMetadata marker = null;
            if (markerGuid != null)
            {
                marker = await ReadByGuid(markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(TenantQueries.GetRecordCount(
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
        public async Task<TenantMetadata> Update(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(TenantQueries.Update(tenant), true, token).ConfigureAwait(false);
            return Converters.TenantFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, TenantStatistics>> GetStatistics(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Dictionary<Guid, TenantStatistics> ret = new Dictionary<Guid, TenantStatistics>();
            DataTable table = await _Repo.ExecuteQueryAsync(TenantQueries.GetStatistics(null), true, token).ConfigureAwait(false);
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    ret.Add(Guid.Parse(row["guid"].ToString()), Converters.TenantStatisticsFromDataRow(row));
                }
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<TenantStatistics> GetStatistics(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable table = await _Repo.ExecuteQueryAsync(TenantQueries.GetStatistics(tenantGuid), true, token).ConfigureAwait(false);
            if (table != null && table.Rows.Count > 0) return Converters.TenantStatisticsFromDataRow(table.Rows[0]);
            return null;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
