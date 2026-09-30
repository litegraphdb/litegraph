namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Filters and paging for request history searches and bulk deletes.  Every filter is optional; null means no filter.
    /// </summary>
    public class RequestHistorySearchRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.  Honored for system administrators; tenant administrators are always scoped to their own tenant.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Request identifier (exact match).
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation identifier (exact match).
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace identifier (exact match).
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// HTTP method (exact match).
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        /// <summary>
        /// Success flag.
        /// </summary>
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Path substring.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Client address (exact match).
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Server node that handled the request (exact match, v10.0).
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Only entries with (true) or without (false) graph transaction diagnostics.
        /// </summary>
        public bool? HasTransactionDiagnostics { get; set; } = null;

        /// <summary>
        /// Graph transaction identifier.
        /// </summary>
        public string TransactionId { get; set; } = null;

        /// <summary>
        /// Earliest creation time (UTC), inclusive.
        /// </summary>
        public DateTime? FromUtc { get; set; } = null;

        /// <summary>
        /// Latest creation time (UTC), inclusive.
        /// </summary>
        public DateTime? ToUtc { get; set; } = null;

        /// <summary>
        /// Records to skip.  The server aligns this down to a multiple of MaxKeys.  Default 0.  Minimum 0.
        /// </summary>
        public int Skip
        {
            get
            {
                return _Skip;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Skip));
                _Skip = value;
            }
        }

        /// <summary>
        /// Maximum records per page.  Default 100.  Minimum 1, maximum 1000.
        /// </summary>
        public int MaxKeys
        {
            get
            {
                return _MaxKeys;
            }
            set
            {
                if (value < 1 || value > 1000) throw new ArgumentOutOfRangeException(nameof(MaxKeys));
                _MaxKeys = value;
            }
        }

        #endregion

        #region Private-Members

        private int _Skip = 0;
        private int _MaxKeys = 100;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySearchRequest()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
