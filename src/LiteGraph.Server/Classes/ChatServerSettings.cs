namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Server-level chat settings.  Operator policy for the chat feature; per-tenant defaults live in the tenant chat settings record.
    /// </summary>
    public class ChatServerSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable the chat feature server-wide.  Default is true.
        /// When false, chat completion requests return 503; chat management routes stay available.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Maximum retries before the first token arrives.  Default is 2.  Minimum is 0, maximum is 10.
        /// A stream that fails after the first token is never retried.
        /// </summary>
        public int MaxRetries
        {
            get
            {
                return _MaxRetries;
            }
            set
            {
                if (value < 0 || value > 10) throw new ArgumentOutOfRangeException(nameof(MaxRetries));
                _MaxRetries = value;
            }
        }

        /// <summary>
        /// Base delay for exponential retry backoff, in milliseconds.  Default is 500.  Minimum is 50, maximum is 30000.
        /// </summary>
        public int RetryBackoffMs
        {
            get
            {
                return _RetryBackoffMs;
            }
            set
            {
                if (value < 50 || value > 30000) throw new ArgumentOutOfRangeException(nameof(RetryBackoffMs));
                _RetryBackoffMs = value;
            }
        }

        /// <summary>
        /// Hard ceiling on per-tenant tool loop iterations.  Default is 100.  Minimum is 1, maximum is 100.
        /// </summary>
        public int MaxToolIterationsCap
        {
            get
            {
                return _MaxToolIterationsCap;
            }
            set
            {
                if (value < 1 || value > 100) throw new ArgumentOutOfRangeException(nameof(MaxToolIterationsCap));
                _MaxToolIterationsCap = value;
            }
        }

        /// <summary>
        /// Server-wide cap on concurrent chat completions.  Default is 50.  Minimum is 1, maximum is 1000.
        /// Requests beyond the cap receive 429.
        /// </summary>
        public int MaxConcurrentChats
        {
            get
            {
                return _MaxConcurrentChats;
            }
            set
            {
                if (value < 1 || value > 1000) throw new ArgumentOutOfRangeException(nameof(MaxConcurrentChats));
                _MaxConcurrentChats = value;
            }
        }

        /// <summary>
        /// Interval between SSE keep-alive comment frames, in seconds.  Default is 15.  Minimum is 1, maximum is 300.
        /// </summary>
        public int SseKeepAliveSeconds
        {
            get
            {
                return _SseKeepAliveSeconds;
            }
            set
            {
                if (value < 1 || value > 300) throw new ArgumentOutOfRangeException(nameof(SseKeepAliveSeconds));
                _SseKeepAliveSeconds = value;
            }
        }

        /// <summary>
        /// Default upstream request timeout when an endpoint specifies none, in milliseconds.  Default is 120000.  Minimum is 1000.
        /// </summary>
        public int DefaultTimeoutMs
        {
            get
            {
                return _DefaultTimeoutMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(DefaultTimeoutMs));
                _DefaultTimeoutMs = value;
            }
        }

        #endregion

        #region Private-Members

        private int _MaxRetries = 2;
        private int _RetryBackoffMs = 500;
        private int _MaxToolIterationsCap = 100;
        private int _MaxConcurrentChats = 50;
        private int _SseKeepAliveSeconds = 15;
        private int _DefaultTimeoutMs = 120000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatServerSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
