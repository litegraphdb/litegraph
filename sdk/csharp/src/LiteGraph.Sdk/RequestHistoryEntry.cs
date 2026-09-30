namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// A request recorded in the server's request history.
    /// </summary>
    public class RequestHistoryEntry
    {
        #region Public-Members

        /// <summary>
        /// Request history entry identifier.
        /// </summary>
        public Guid GUID { get; set; } = Guid.Empty;

        /// <summary>
        /// Request identifier (x-request-id).
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation identifier (x-correlation-id).
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace identifier.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// UTC timestamp at which the request was received.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp at which the request completed, or null.
        /// </summary>
        public DateTime? CompletedUtc { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Request path, with sensitive values redacted.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Full request URL, with sensitive values redacted.
        /// </summary>
        public string Url { get; set; } = null;

        /// <summary>
        /// Client address; behind a trusted load balancer, the address from X-Forwarded-For.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Server node that handled the request (v10.0).  Null for records written before v10.0.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Tenant GUID, or null.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// User GUID, or null.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// True when the request succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Processing time in milliseconds.
        /// </summary>
        public double ProcessingTimeMs { get; set; } = 0;

        /// <summary>
        /// Request body length in bytes.
        /// </summary>
        public long RequestBodyLength { get; set; } = 0;

        /// <summary>
        /// Response body length in bytes.
        /// </summary>
        public long ResponseBodyLength { get; set; } = 0;

        /// <summary>
        /// True when the captured request body was truncated.
        /// </summary>
        public bool RequestBodyTruncated { get; set; } = false;

        /// <summary>
        /// True when the captured response body was truncated.
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
        /// Graph transaction diagnostics as JSON, when the request was a graph transaction.
        /// </summary>
        public string TransactionDiagnosticsJson { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryEntry()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
