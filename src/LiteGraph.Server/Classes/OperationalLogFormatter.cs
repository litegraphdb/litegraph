namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using LiteGraph.Serialization;

    /// <summary>
    /// Formats operational log records.
    /// </summary>
    public static class OperationalLogFormatter
    {
        #region Public-Methods

        /// <summary>
        /// Format a completed REST request log entry.
        /// </summary>
        /// <param name="header">Plain-text log header.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Redacted request URL.</param>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="durationMs">Request duration in milliseconds.</param>
        /// <param name="requestId">Request ID.</param>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="traceId">Trace ID.</param>
        /// <param name="responseBody">Response body.</param>
        /// <param name="includeResponseBody">True to include the response body.</param>
        /// <param name="json">True to emit JSON.</param>
        /// <returns>Formatted log line.</returns>
        public static string FormatRequestCompletion(
            string header,
            string method,
            string url,
            int statusCode,
            double? durationMs,
            string requestId,
            string correlationId,
            string traceId,
            string responseBody,
            bool includeResponseBody,
            bool json)
        {
            header ??= String.Empty;
            method = String.IsNullOrEmpty(method) ? "UNKNOWN" : method;
            url = String.IsNullOrEmpty(url) ? "/" : url;

            if (!json)
            {
                string msg = header
                    + method + " " + url + " "
                    + statusCode.ToString(CultureInfo.InvariantCulture) + " "
                    + (durationMs != null ? durationMs.Value.ToString(CultureInfo.InvariantCulture) : "0")
                    + "ms";

                if (!String.IsNullOrEmpty(requestId)) msg += " requestId=" + requestId;
                if (!String.IsNullOrEmpty(correlationId)) msg += " correlationId=" + correlationId;
                if (!String.IsNullOrEmpty(traceId)) msg += " traceId=" + traceId;
                if (includeResponseBody && !String.IsNullOrEmpty(responseBody)) msg += Environment.NewLine + responseBody;
                return msg;
            }

            Dictionary<string, object> record = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "timestampUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
                { "event", "http_request_completed" },
                { "component", "LiteGraph.Server.REST" },
                { "method", method },
                { "url", url },
                { "statusCode", statusCode },
                { "durationMs", durationMs ?? 0 }
            };

            if (!String.IsNullOrEmpty(requestId)) record["requestId"] = requestId;
            if (!String.IsNullOrEmpty(correlationId)) record["correlationId"] = correlationId;
            if (!String.IsNullOrEmpty(traceId)) record["traceId"] = traceId;
            if (includeResponseBody && !String.IsNullOrEmpty(responseBody)) record["responseBody"] = responseBody;

            return JsonSerializer.Serialize(record, (JsonTypeInfo<Dictionary<string, object>>)JsonOptions.GetTypeInfo(typeof(Dictionary<string, object>)));
        }

        #endregion

        #region Private-Members

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            TypeInfoResolver = LiteGraphJsonContext.Default
        };

        #endregion
    }
}
