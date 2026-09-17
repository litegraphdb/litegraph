namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Native LiteGraph graph query request.
    /// </summary>
    public class GraphQueryRequest
    {
        #region Public-Members

        /// <summary>
        /// Cypher/GQL-inspired LiteGraph-native query text.
        /// </summary>
        public string Query
        {
            get
            {
                return _Query;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Query));
                _Query = value;
            }
        }

        /// <summary>
        /// Query parameters.  Inline literals are allowed for simple values; parameters should be used for user input and large values.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maximum rows to return when the query omits LIMIT.
        /// </summary>
        public int MaxResults
        {
            get
            {
                return _MaxResults;
            }
            set
            {
                if (value < 1 || value > 10000) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Query timeout in seconds.
        /// </summary>
        public int TimeoutSeconds
        {
            get
            {
                return _TimeoutSeconds;
            }
            set
            {
                if (value < 1 || value > 3600) throw new ArgumentOutOfRangeException(nameof(TimeoutSeconds));
                _TimeoutSeconds = value;
            }
        }

        /// <summary>
        /// Maximum number of matching rows a global operation (aggregate or ORDER BY) will examine.
        /// Aggregates (COUNT/SUM/AVG/MIN/MAX) and ORDER BY are evaluated over the whole matching set rather than
        /// the returned page, so this bounds the work such a query performs.  When the matching set exceeds this
        /// value the query is rejected rather than silently truncated (which would make the aggregate or top-N
        /// result wrong).  Minimum 0 (0 means unlimited), default 1000000.  Ordinary (non-global) queries are
        /// unaffected — they remain bounded by <see cref="MaxResults"/> and any LIMIT.
        /// </summary>
        public int MaxScanRows
        {
            get
            {
                return _MaxScanRows;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxScanRows));
                _MaxScanRows = value;
            }
        }

        /// <summary>
        /// Include parse, plan, execute, and total timing details in the response.
        /// </summary>
        public bool IncludeProfile { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Query = null;
        private int _MaxResults = 100;
        private int _TimeoutSeconds = 30;
        private int _MaxScanRows = 1000000;

        #endregion
    }
}
