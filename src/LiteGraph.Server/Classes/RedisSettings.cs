namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Redis settings, used in cluster mode for the node registry and for signalling settings changes and restart
    /// requests between nodes.  Redis holds no data that cannot be rebuilt: node entries are rewritten by every heartbeat,
    /// and the change counters only need to increase, so Redis needs no persistence or backup.
    /// </summary>
    public class RedisSettings
    {
        #region Public-Members

        /// <summary>
        /// StackExchange.Redis connection string, for example "redis:6379" or "redis:6379,password=secret".
        /// Default is 127.0.0.1:6379.  Overridden by LITEGRAPH_REDIS_CONNECTION_STRING.  Secret when it carries a password.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(ConnectionString));
                _ConnectionString = value;
            }
        }

        /// <summary>
        /// Interval in milliseconds at which each node writes its heartbeat and checks the settings and restart counters.
        /// A setting change or restart request reaches every node within about this long.
        /// Default is 2000.  Minimum is 500, maximum is 60000.
        /// </summary>
        public int PollIntervalMs
        {
            get
            {
                return _PollIntervalMs;
            }
            set
            {
                if (value < 500 || value > 60000) throw new ArgumentOutOfRangeException(nameof(PollIntervalMs), "PollIntervalMs must be between 500 and 60000.");
                _PollIntervalMs = value;
            }
        }

        /// <summary>
        /// A node whose last heartbeat is older than this many milliseconds is reported offline.
        /// Default is 15000.  Minimum is 2000, maximum is 600000.  Should be several times PollIntervalMs.
        /// </summary>
        public int NodeTimeoutMs
        {
            get
            {
                return _NodeTimeoutMs;
            }
            set
            {
                if (value < 2000 || value > 600000) throw new ArgumentOutOfRangeException(nameof(NodeTimeoutMs), "NodeTimeoutMs must be between 2000 and 600000.");
                _NodeTimeoutMs = value;
            }
        }

        /// <summary>
        /// Entries for nodes that have sent no heartbeat for this many milliseconds are removed from the registry.
        /// Until then they are listed as offline or stopped.  Default is 86400000 (one day).  Minimum is 60000, maximum is 2592000000.
        /// </summary>
        public long NodeRetentionMs
        {
            get
            {
                return _NodeRetentionMs;
            }
            set
            {
                if (value < 60000 || value > 2592000000) throw new ArgumentOutOfRangeException(nameof(NodeRetentionMs), "NodeRetentionMs must be between 60000 and 2592000000.");
                _NodeRetentionMs = value;
            }
        }

        #endregion

        #region Private-Members

        private string _ConnectionString = "127.0.0.1:6379";
        private int _PollIntervalMs = 2000;
        private int _NodeTimeoutMs = 15000;
        private long _NodeRetentionMs = 86400000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RedisSettings()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
