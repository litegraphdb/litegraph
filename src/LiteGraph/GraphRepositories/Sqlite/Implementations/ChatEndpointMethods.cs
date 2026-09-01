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
    /// Chat endpoint methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class ChatEndpointMethods : IChatEndpointMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat endpoint methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public ChatEndpointMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatEndpoint> Create(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            token.ThrowIfCancellationRequested();
            DataTable createResult = await _Repo.ExecuteQueryAsync(ChatEndpointQueries.Insert(endpoint), true, token).ConfigureAwait(false);
            return Converters.ChatEndpointFromDataRow(createResult.Rows[0]);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatEndpoint> ReadAllInTenant(
            Guid tenantGuid,
            ChatEndpointTypeEnum? endpointType = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(ChatEndpointQueries.SelectAllInTenant(
                    tenantGuid,
                    endpointType,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) yield break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.ChatEndpointFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) yield break;
            }
        }

        /// <inheritdoc />
        public async Task<ChatEndpoint> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatEndpointQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.ChatEndpointFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatEndpoint>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            ChatEndpoint marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken + " could not be found.");
            }

            EnumerationResult<ChatEndpoint> ret = new EnumerationResult<ChatEndpoint>
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

            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatEndpointQueries.GetRecordPage(
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

            ret.Objects = Converters.ChatEndpointsFromDataTable(result);

            ChatEndpoint lastItem = ret.Objects.Last();

            ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);
            if (ret.RecordsRemaining > 0)
            {
                ret.ContinuationToken = lastItem.GUID;
                ret.EndOfResults = false;
            }
            else
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
            }

            ret.Timestamp.End = DateTime.UtcNow;
            return ret;
        }

        /// <inheritdoc />
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ChatEndpoint marker = null;

            if (tenantGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatEndpointQueries.GetRecordCount(
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
        public async Task<ChatEndpoint> Update(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatEndpointQueries.Update(endpoint), true, token).ConfigureAwait(false);
            return Converters.ChatEndpointFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(ChatEndpointQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(ChatEndpointQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ChatEndpoint endpoint = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            return (endpoint != null);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
