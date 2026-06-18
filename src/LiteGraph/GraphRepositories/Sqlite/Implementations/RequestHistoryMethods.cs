namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    /// <summary>
    /// Request history methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class RequestHistoryMethods : IRequestHistoryMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;
        private Serializer _Serializer = new Serializer();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Request history methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public RequestHistoryMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task Insert(RequestHistoryDetail detail, CancellationToken token = default)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(RequestHistoryQueries.Insert(detail), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RequestHistoryEntry> ReadByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(RequestHistoryQueries.SelectByGuid(guid), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToEntry(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<RequestHistoryDetail> ReadDetailByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(RequestHistoryQueries.SelectByGuid(guid), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToDetail(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<RequestHistorySearchResult> Search(RequestHistorySearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            token.ThrowIfCancellationRequested();

            RequestHistorySearchResult ret = new RequestHistorySearchResult
            {
                Page = search.Page,
                PageSize = search.PageSize
            };

            DataTable countTable = await _Repo.ExecuteQueryAsync(RequestHistoryQueries.Search(search, true), false, token).ConfigureAwait(false);
            if (countTable != null && countTable.Rows.Count > 0 && countTable.Columns.Contains("record_count"))
                ret.TotalCount = Convert.ToInt64(countTable.Rows[0]["record_count"]);

            ret.TotalPages = (int)Math.Ceiling((double)ret.TotalCount / search.PageSize);

            DataTable dataTable = await _Repo.ExecuteQueryAsync(RequestHistoryQueries.Search(search, false), false, token).ConfigureAwait(false);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    ret.Objects.Add(RowToEntry(row));
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public async Task<RequestHistorySummary> GetSummary(
            Guid? tenantGuid,
            string interval,
            DateTime startUtc,
            DateTime endUtc,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            RequestHistorySummary summary = new RequestHistorySummary
            {
                Interval = interval ?? "hour",
                StartUtc = startUtc,
                EndUtc = endUtc
            };

            DataTable table = await _Repo.ExecuteQueryAsync(
                RequestHistoryQueries.GetSummary(tenantGuid, interval, startUtc, endUtc),
                false,
                token).ConfigureAwait(false);

            if (table == null || table.Rows.Count < 1) return summary;

            foreach (DataRow row in table.Rows)
            {
                token.ThrowIfCancellationRequested();

                string bucketStr = row["bucketutc"]?.ToString();
                if (string.IsNullOrEmpty(bucketStr)) continue;
                if (!DateTime.TryParse(bucketStr, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime bucketTime))
                {
                    continue;
                }

                long successCount = Convert.ToInt64(row["successcount"] == DBNull.Value ? 0 : row["successcount"]);
                long failureCount = Convert.ToInt64(row["failurecount"] == DBNull.Value ? 0 : row["failurecount"]);

                RequestHistorySummaryBucket bucket = new RequestHistorySummaryBucket
                {
                    TimestampUtc = DateTime.SpecifyKind(bucketTime, DateTimeKind.Utc),
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    TotalCount = successCount + failureCount
                };

                summary.Data.Add(bucket);
                summary.TotalSuccess += successCount;
                summary.TotalFailure += failureCount;
            }

            summary.TotalRequests = summary.TotalSuccess + summary.TotalFailure;
            return summary;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(RequestHistoryQueries.Delete(guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> DeleteMany(RequestHistorySearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            token.ThrowIfCancellationRequested();

            DataTable countTable = await _Repo.ExecuteQueryAsync(RequestHistoryQueries.Search(search, true), false, token).ConfigureAwait(false);
            int count = 0;
            if (countTable != null && countTable.Rows.Count > 0 && countTable.Columns.Contains("record_count"))
                count = Convert.ToInt32(countTable.Rows[0]["record_count"]);

            await _Repo.ExecuteQueryAsync(RequestHistoryQueries.DeleteMany(search), true, token).ConfigureAwait(false);
            return count;
        }

        /// <inheritdoc />
        public async Task<int> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            RequestHistorySearchRequest search = new RequestHistorySearchRequest
            {
                ToUtc = cutoffUtc,
                PageSize = 1
            };

            return await DeleteMany(search, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private RequestHistoryEntry RowToEntry(DataRow row)
        {
            RequestHistoryEntry entry = new RequestHistoryEntry();
            PopulateCommon(row, entry);
            return entry;
        }

        private RequestHistoryDetail RowToDetail(DataRow row)
        {
            RequestHistoryDetail detail = new RequestHistoryDetail();
            PopulateCommon(row, detail);

            if (row.Table.Columns.Contains("requestheadersjson"))
            {
                string json = Converters.GetDataRowStringValue(row, "requestheadersjson");
                if (!string.IsNullOrEmpty(json))
                {
                    try { detail.RequestHeaders = _Serializer.DeserializeJson<Dictionary<string, string>>(json); }
                    catch { detail.RequestHeaders = new Dictionary<string, string>(); }
                }
            }

            if (row.Table.Columns.Contains("responseheadersjson"))
            {
                string json = Converters.GetDataRowStringValue(row, "responseheadersjson");
                if (!string.IsNullOrEmpty(json))
                {
                    try { detail.ResponseHeaders = _Serializer.DeserializeJson<Dictionary<string, string>>(json); }
                    catch { detail.ResponseHeaders = new Dictionary<string, string>(); }
                }
            }

            if (row.Table.Columns.Contains("requestbodyb64"))
            {
                string b64 = Converters.GetDataRowStringValue(row, "requestbodyb64");
                detail.RequestBody = RequestHistoryQueries.DecodeBody(b64);
            }

            if (row.Table.Columns.Contains("responsebodyb64"))
            {
                string b64 = Converters.GetDataRowStringValue(row, "responsebodyb64");
                detail.ResponseBody = RequestHistoryQueries.DecodeBody(b64);
            }

            return detail;
        }

        private void PopulateCommon(DataRow row, RequestHistoryEntry entry)
        {
            string guidStr = Converters.GetDataRowStringValue(row, "guid");
            if (!string.IsNullOrEmpty(guidStr) && Guid.TryParse(guidStr, out Guid g)) entry.GUID = g;

            if (row.Table.Columns.Contains("requestid")) entry.RequestId = Converters.GetDataRowStringValue(row, "requestid");
            if (row.Table.Columns.Contains("correlationid")) entry.CorrelationId = Converters.GetDataRowStringValue(row, "correlationid");
            if (row.Table.Columns.Contains("traceid")) entry.TraceId = Converters.GetDataRowStringValue(row, "traceid");

            string createdStr = Converters.GetDataRowStringValue(row, "createdutc");
            if (!string.IsNullOrEmpty(createdStr) && DateTime.TryParse(createdStr, out DateTime created))
                entry.CreatedUtc = DateTime.SpecifyKind(created, DateTimeKind.Utc);

            string completedStr = Converters.GetDataRowStringValue(row, "completedutc");
            if (!string.IsNullOrEmpty(completedStr) && DateTime.TryParse(completedStr, out DateTime completed))
                entry.CompletedUtc = DateTime.SpecifyKind(completed, DateTimeKind.Utc);

            entry.Method = Converters.GetDataRowStringValue(row, "method");
            entry.Path = Converters.GetDataRowStringValue(row, "path");
            entry.Url = Converters.GetDataRowStringValue(row, "url");
            entry.SourceIp = Converters.GetDataRowStringValue(row, "sourceip");

            string tenantStr = Converters.GetDataRowStringValue(row, "tenantguid");
            if (!string.IsNullOrEmpty(tenantStr) && Guid.TryParse(tenantStr, out Guid t)) entry.TenantGUID = t;

            string userStr = Converters.GetDataRowStringValue(row, "userguid");
            if (!string.IsNullOrEmpty(userStr) && Guid.TryParse(userStr, out Guid u)) entry.UserGUID = u;

            entry.StatusCode = Converters.GetDataRowIntValue(row, "statuscode");
            entry.Success = Converters.GetDataRowIntValue(row, "success") == 1;

            if (row.Table.Columns.Contains("processingtimems") && row["processingtimems"] != DBNull.Value)
                entry.ProcessingTimeMs = Convert.ToDouble(row["processingtimems"]);

            entry.RequestBodyLength = Convert.ToInt64(row["requestbodylength"] == DBNull.Value ? 0 : row["requestbodylength"]);
            entry.ResponseBodyLength = Convert.ToInt64(row["responsebodylength"] == DBNull.Value ? 0 : row["responsebodylength"]);
            entry.RequestBodyTruncated = Converters.GetDataRowIntValue(row, "requestbodytruncated") == 1;
            entry.ResponseBodyTruncated = Converters.GetDataRowIntValue(row, "responsebodytruncated") == 1;
            entry.RequestContentType = Converters.GetDataRowStringValue(row, "requestcontenttype");
            entry.ResponseContentType = Converters.GetDataRowStringValue(row, "responsecontenttype");
            if (row.Table.Columns.Contains("transactiondiagnosticsjson")) entry.TransactionDiagnosticsJson = Converters.GetDataRowStringValue(row, "transactiondiagnosticsjson");
        }

        #endregion
    }
}
