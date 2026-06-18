namespace LiteGraph
{
    using System;

    /// <summary>
    /// Request history entry metadata (no bodies or headers).
    /// </summary>
    public class RequestHistoryEntry
    {
        #region Public-Members

        /// <summary>
        /// GUID of the request.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Request ID exposed to clients for correlation.
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation ID used to group related requests.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace ID from W3C trace context, if available.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Timestamp when the request was received, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the request finished processing, in UTC.
        /// </summary>
        public DateTime? CompletedUtc { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Request path without query string.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Full request URL with query string.
        /// </summary>
        public string Url { get; set; } = null;

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Tenant GUID associated with the request, if any.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// User GUID associated with the request, if any.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Whether the request completed successfully (2xx or 3xx).
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Total processing time, in milliseconds.
        /// </summary>
        public double ProcessingTimeMs { get; set; } = 0;

        /// <summary>
        /// Request body size, in bytes.
        /// </summary>
        public long RequestBodyLength { get; set; } = 0;

        /// <summary>
        /// Response body size, in bytes.
        /// </summary>
        public long ResponseBodyLength { get; set; } = 0;

        /// <summary>
        /// Whether the captured request body was truncated.
        /// </summary>
        public bool RequestBodyTruncated { get; set; } = false;

        /// <summary>
        /// Whether the captured response body was truncated.
        /// </summary>
        public bool ResponseBodyTruncated { get; set; } = false;

        /// <summary>
        /// Request content type.
        /// </summary>
        public string RequestContentType { get; set; } = null;

        /// <summary>
        /// Response content type.
        /// </summary>
        public string ResponseContentType { get; set; } = null;

        /// <summary>
        /// Compact transaction diagnostics JSON captured from graph transaction responses, if applicable.
        /// </summary>
        public string TransactionDiagnosticsJson { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryEntry()
        {
        }

        #endregion
    }
}
