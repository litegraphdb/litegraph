namespace LiteGraph
{
    using System;

    /// <summary>
    /// Search/filter parameters for request history listing.
    /// </summary>
    public class RequestHistorySearchRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID filter (null means all tenants when admin).
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Request ID filter.
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation ID filter.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace ID filter.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// HTTP method filter.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// HTTP status code filter.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        /// <summary>
        /// Success filter.
        /// </summary>
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Path substring filter.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Source IP filter.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Transaction diagnostics presence filter.
        /// </summary>
        public bool? HasTransactionDiagnostics { get; set; } = null;

        /// <summary>
        /// Transaction ID substring filter.
        /// </summary>
        public string TransactionId { get; set; } = null;

        /// <summary>
        /// Inclusive lower bound on createdutc.
        /// </summary>
        public DateTime? FromUtc { get; set; } = null;

        /// <summary>
        /// Exclusive upper bound on createdutc.
        /// </summary>
        public DateTime? ToUtc { get; set; } = null;

        /// <summary>
        /// Zero-based page index.
        /// </summary>
        public int Page
        {
            get
            {
                return _Page;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Page));
                _Page = value;
            }
        }

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                if (value < 1 || value > 1000) throw new ArgumentOutOfRangeException(nameof(PageSize));
                _PageSize = value;
            }
        }

        #endregion

        #region Private-Members

        private int _Page = 0;
        private int _PageSize = 25;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySearchRequest()
        {
        }

        #endregion
    }
}
