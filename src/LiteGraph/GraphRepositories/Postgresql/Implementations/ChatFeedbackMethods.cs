namespace LiteGraph.GraphRepositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql;
    using LiteGraph.GraphRepositories.Postgresql.Queries;

    /// <summary>
    /// Chat feedback methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class ChatFeedbackMethods : IChatFeedbackMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private PostgresqlGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat feedback methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public ChatFeedbackMethods(PostgresqlGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatFeedback> Create(ChatFeedback feedback, CancellationToken token = default)
        {
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));
            token.ThrowIfCancellationRequested();
            DataTable createResult = await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.Insert(feedback), true, token).ConfigureAwait(false);
            return Converters.ChatFeedbackFromDataRow(createResult.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<ChatFeedback> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.ChatFeedbackFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatFeedback> ReadAllInTenant(
            Guid tenantGuid,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.SelectAllInTenant(
                    tenantGuid,
                    rating,
                    threadGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) yield break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.ChatFeedbackFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) yield break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatFeedback>> Enumerate(
            EnumerationRequest query,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            ChatFeedback marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken + " could not be found.");
            }

            EnumerationResult<ChatFeedback> ret = new EnumerationResult<ChatFeedback>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;

            ret.TotalRecords = await GetRecordCount(query.TenantGUID, rating, threadGuid, query.Ordering, null, token).ConfigureAwait(false);

            if (ret.TotalRecords < 1)
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
                ret.RecordsRemaining = 0;
                ret.Timestamp.End = DateTime.UtcNow;
                return ret;
            }

            token.ThrowIfCancellationRequested();
            DataTable page = await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.GetRecordPage(
                query.TenantGUID,
                rating,
                threadGuid,
                query.MaxResults,
                query.Skip,
                query.Ordering,
                marker), false, token).ConfigureAwait(false);

            if (page == null || page.Rows.Count < 1)
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
                ret.RecordsRemaining = 0;
                ret.Timestamp.End = DateTime.UtcNow;
                return ret;
            }

            ret.Objects = Converters.ChatFeedbackFromDataTable(page);

            ChatFeedback lastItem = ret.Objects[ret.Objects.Count - 1];

            ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, rating, threadGuid, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);
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
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ChatFeedback marker = null;

            if (tenantGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.GetRecordCount(
                tenantGuid,
                rating,
                threadGuid,
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
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.DeleteByThread(tenantGuid, threadGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(ChatFeedbackQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ChatFeedback feedback = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            return (feedback != null);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
