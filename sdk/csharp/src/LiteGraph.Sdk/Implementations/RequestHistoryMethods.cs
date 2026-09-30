namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Request history methods.
    /// Thread safety: stateless; safe for concurrent use.
    /// </summary>
    public class RequestHistoryMethods : IRequestHistoryMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Request history methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        /// <exception cref="ArgumentNullException">sdk is null.</exception>
        public RequestHistoryMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<EnumerationResult<RequestHistoryEntry>> Search(RequestHistorySearchRequest request = null, CancellationToken token = default)
        {
            if (request == null) request = new RequestHistorySearchRequest();
            string url = _Sdk.Endpoint + "v1.0/requesthistory" + BuildQuery(request, true);
            return await _Sdk.GetEnumeration<RequestHistoryEntry>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<RequestHistoryEntry> Enumerate(RequestHistorySearchRequest request = null, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (request == null) request = new RequestHistorySearchRequest();
            int originalSkip = request.Skip;
            DateTime? originalToUtc = request.ToUtc;
            int skip = originalSkip;

            // Entries are newest first and every page request is itself recorded, so without an upper bound new entries
            // would shift later pages.  Pin the window to the moment enumeration starts unless the caller set one.
            if (!request.ToUtc.HasValue) request.ToUtc = DateTime.UtcNow;

            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    request.Skip = skip;
                    EnumerationResult<RequestHistoryEntry> page = await Search(request, token).ConfigureAwait(false);
                    if (page == null || page.Objects == null || page.Objects.Count == 0) yield break;

                    foreach (RequestHistoryEntry entry in page.Objects)
                    {
                        yield return entry;
                    }

                    if (page.EndOfResults) yield break;
                    skip += request.MaxKeys;
                }
            }
            finally
            {
                // Leave the caller's request as it was.
                request.Skip = originalSkip;
                request.ToUtc = originalToUtc;
            }
        }

        /// <inheritdoc />
        public async Task<RequestHistoryEntry> ReadByGuid(Guid requestGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/requesthistory/" + requestGuid;
            return await _Sdk.Get<RequestHistoryEntry>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RequestHistoryDetail> ReadDetail(Guid requestGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/requesthistory/" + requestGuid + "/detail";
            return await _Sdk.Get<RequestHistoryDetail>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RequestHistorySummary> ReadSummary(string interval = null, DateTime? startUtc = null, DateTime? endUtc = null, Guid? tenantGuid = null, CancellationToken token = default)
        {
            List<string> parts = new List<string>();
            if (!String.IsNullOrEmpty(interval)) parts.Add("interval=" + EncodeQueryValue(interval));
            if (startUtc.HasValue) parts.Add("startUtc=" + EncodeQueryValue(FormatUtc(startUtc.Value)));
            if (endUtc.HasValue) parts.Add("endUtc=" + EncodeQueryValue(FormatUtc(endUtc.Value)));
            if (tenantGuid.HasValue) parts.Add("tenantGuid=" + tenantGuid.Value);

            string url = _Sdk.Endpoint + "v1.0/requesthistory/summary" + (parts.Count > 0 ? "?" + String.Join("&", parts) : "");
            return await _Sdk.Get<RequestHistorySummary>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid requestGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/requesthistory/" + requestGuid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RequestHistoryDeleteResult> DeleteMany(RequestHistorySearchRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string url = _Sdk.Endpoint + "v1.0/requesthistory/bulk" + BuildQuery(request, false);
            return await _Sdk.DeleteWithResult<RequestHistoryDeleteResult>(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static string BuildQuery(RequestHistorySearchRequest request, bool includePaging)
        {
            List<string> parts = new List<string>();
            if (includePaging)
            {
                parts.Add("max-keys=" + request.MaxKeys.ToString(CultureInfo.InvariantCulture));
                parts.Add("skip=" + request.Skip.ToString(CultureInfo.InvariantCulture));
            }

            if (request.TenantGUID.HasValue) parts.Add("tenantGuid=" + request.TenantGUID.Value);
            AddString(parts, "requestId", request.RequestId);
            AddString(parts, "correlationId", request.CorrelationId);
            AddString(parts, "traceId", request.TraceId);
            AddString(parts, "method", request.Method);
            AddString(parts, "path", request.Path);
            AddString(parts, "sourceIp", request.SourceIp);
            AddString(parts, "nodeId", request.NodeId);
            AddString(parts, "transactionId", request.TransactionId);
            if (request.StatusCode.HasValue) parts.Add("statusCode=" + request.StatusCode.Value.ToString(CultureInfo.InvariantCulture));
            if (request.Success.HasValue) parts.Add("success=" + (request.Success.Value ? "true" : "false"));
            if (request.HasTransactionDiagnostics.HasValue) parts.Add("hasTransactionDiagnostics=" + (request.HasTransactionDiagnostics.Value ? "true" : "false"));
            if (request.FromUtc.HasValue) parts.Add("fromUtc=" + EncodeQueryValue(FormatUtc(request.FromUtc.Value)));
            if (request.ToUtc.HasValue) parts.Add("toUtc=" + EncodeQueryValue(FormatUtc(request.ToUtc.Value)));

            return parts.Count > 0 ? "?" + String.Join("&", parts) : "";
        }

        private static void AddString(List<string> parts, string name, string value)
        {
            if (!String.IsNullOrEmpty(value)) parts.Add(name + "=" + EncodeQueryValue(value));
        }

        private static string EncodeQueryValue(string value)
        {
            // The server matches query values as sent, without percent-decoding, so only characters that would break the
            // query string are escaped; path separators, colons in timestamps, and other safe characters are sent as-is.
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c == '&' || c == '#' || c == '+' || c == '%' || c == '=' || c == '?' || Char.IsWhiteSpace(c) || c < 0x20 || c > 0x7E)
                    sb.Append(Uri.EscapeDataString(c.ToString()));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static string FormatUtc(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
