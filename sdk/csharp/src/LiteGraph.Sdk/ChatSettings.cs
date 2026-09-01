namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Chat settings.  Per-tenant chat defaults; one record per tenant.
    /// </summary>
    public class ChatSettings
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.  Keys the record; one settings record exists per tenant.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// GUID of the default completion endpoint.  Null when no default has been chosen.
        /// </summary>
        public Guid? DefaultCompletionEndpointGUID { get; set; } = null;

        /// <summary>
        /// GUID of the default embedding endpoint.  Null when no default has been chosen.
        /// </summary>
        public Guid? DefaultEmbeddingEndpointGUID { get; set; } = null;

        /// <summary>
        /// Tenant system prompt, merged with the server's fixed preamble.  Null uses the preamble alone.
        /// </summary>
        public string SystemPrompt { get; set; } = null;

        /// <summary>
        /// Enable chat for the tenant.  Default is true.
        /// </summary>
        public bool EnableChat { get; set; } = true;

        /// <summary>
        /// Advertise graph tools to the model.  Default is true.
        /// </summary>
        public bool EnableTools { get; set; } = true;

        /// <summary>
        /// Advertise mutation (create/update/delete) tools to the model.  Default is false.
        /// </summary>
        public bool EnableMutationTools { get; set; } = false;

        /// <summary>
        /// Maximum tool loop iterations per turn.  Default is 10.  Minimum is 1.
        /// The server-level MaxToolIterationsCap bounds the effective value.
        /// </summary>
        public int MaxToolIterations
        {
            get
            {
                return _MaxToolIterations;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxToolIterations));
                _MaxToolIterations = value;
            }
        }

        /// <summary>
        /// Enable automatic vector retrieval when a thread is bound to a graph.  Default is true.
        /// </summary>
        public bool EnableRag { get; set; } = true;

        /// <summary>
        /// Number of vector search results retrieved for context.  Default is 8.  Minimum is 1, maximum is 100.
        /// </summary>
        public int RagTopK
        {
            get
            {
                return _RagTopK;
            }
            set
            {
                if (value < 1 || value > 100) throw new ArgumentOutOfRangeException(nameof(RagTopK));
                _RagTopK = value;
            }
        }

        /// <summary>
        /// Minimum similarity score for retrieved context.  Default is 0.  Minimum is -1, maximum is 1.
        /// </summary>
        public double RagScoreThreshold
        {
            get
            {
                return _RagScoreThreshold;
            }
            set
            {
                if (value < -1 || value > 1) throw new ArgumentOutOfRangeException(nameof(RagScoreThreshold));
                _RagScoreThreshold = value;
            }
        }

        /// <summary>
        /// Token budget for prior-turn history included in the prompt.  Default is 16384.  Minimum is 1024.
        /// </summary>
        public int MaxContextTokens
        {
            get
            {
                return _MaxContextTokens;
            }
            set
            {
                if (value < 1024) throw new ArgumentOutOfRangeException(nameof(MaxContextTokens));
                _MaxContextTokens = value;
            }
        }

        /// <summary>
        /// Days to retain chat turns before pruning.  Default is 90.  Minimum is 0; 0 retains forever.
        /// </summary>
        public int HistoryRetentionDays
        {
            get
            {
                return _HistoryRetentionDays;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(HistoryRetentionDays));
                _HistoryRetentionDays = value;
            }
        }

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private int _MaxToolIterations = 10;
        private int _RagTopK = 8;
        private double _RagScoreThreshold = 0;
        private int _MaxContextTokens = 16384;
        private int _HistoryRetentionDays = 90;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
