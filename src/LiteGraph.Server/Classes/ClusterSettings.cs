namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Cluster settings.  When Enable is true the server runs as one of several identical nodes sharing a PostgreSQL
    /// database behind a load balancer, coordinates schema migrations, background jobs, index builds, settings writes,
    /// and rolling restarts through Clutch, and registers itself and signals settings changes and restart requests
    /// through Redis.  All nodes must share the same settings file, database, and encryption key.
    /// </summary>
    public class ClusterSettings
    {
        #region Public-Members

        /// <summary>
        /// Run as a cluster node.  Default is false (single node).  Requires Database.Type = Postgresql.
        /// Overridden by LITEGRAPH_CLUSTER_ENABLE.  Changing it requires a restart.
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// Cluster name.  Prefixes every Clutch lock key, so several clusters can share one Clutch deployment.
        /// Default is litegraph.  1 to 64 characters: lowercase letters, digits, and hyphens.
        /// Overridden by LITEGRAPH_CLUSTER_NAME.  Changing it requires a restart.
        /// </summary>
        public string ClusterName
        {
            get
            {
                return _ClusterName;
            }
            set
            {
                if (String.IsNullOrEmpty(value) || value.Length > 64 || !Regex.IsMatch(value, "^[a-z0-9-]+$"))
                    throw new ArgumentException("ClusterName must be 1 to 64 characters of lowercase letters, digits, and hyphens.", nameof(ClusterName));
                _ClusterName = value;
            }
        }

        /// <summary>
        /// Node identifier, unique within the cluster.  Default is null, meaning the machine (container) host name.
        /// Set per node through LITEGRAPH_NODE_ID rather than in the shared settings file.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Trust X-Forwarded-For from the load balancer when resolving client addresses for request history, audit, and logs.
        /// Never used for access control.  Default is false.
        /// </summary>
        public bool TrustForwardedHeaders { get; set; } = false;

        /// <summary>
        /// Proxy addresses or CIDR ranges whose X-Forwarded-For headers are trusted when TrustForwardedHeaders is true.
        /// Default is empty.  Overridden by LITEGRAPH_TRUSTED_PROXIES (comma separated).
        /// </summary>
        public List<string> TrustedProxies
        {
            get
            {
                return _TrustedProxies;
            }
            set
            {
                _TrustedProxies = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Allow cluster mode to start with the default encryption key and administrator token.
        /// Intended only for the demonstration Docker deployment.  Default is false.
        /// </summary>
        public bool AllowInsecureDefaults { get; set; } = false;

        /// <summary>
        /// Interval in milliseconds at which each node re-reads chat endpoints from the database so endpoint changes made
        /// through other nodes are monitored.  Default is 30000.  Minimum is 5000, maximum is 600000.
        /// </summary>
        public int EndpointResyncIntervalMs
        {
            get
            {
                return _EndpointResyncIntervalMs;
            }
            set
            {
                if (value < 5000 || value > 600000) throw new ArgumentOutOfRangeException(nameof(EndpointResyncIntervalMs), "EndpointResyncIntervalMs must be between 5000 and 600000.");
                _EndpointResyncIntervalMs = value;
            }
        }

        /// <summary>
        /// Clutch connection settings.
        /// </summary>
        public ClutchSettings Clutch
        {
            get
            {
                return _Clutch;
            }
            set
            {
                _Clutch = value ?? new ClutchSettings();
            }
        }

        /// <summary>
        /// Redis settings for the node registry and for settings-change and restart signalling.
        /// </summary>
        public RedisSettings Redis
        {
            get
            {
                return _Redis;
            }
            set
            {
                _Redis = value ?? new RedisSettings();
            }
        }

        /// <summary>
        /// During a rolling restart, how long a node keeps serving after it reports itself draining and before it exits,
        /// in milliseconds, so load balancers stop sending it new requests.  Default is 5000.  Minimum is 0, maximum is 120000.
        /// </summary>
        public int RestartDrainMs
        {
            get
            {
                return _RestartDrainMs;
            }
            set
            {
                if (value < 0 || value > 120000) throw new ArgumentOutOfRangeException(nameof(RestartDrainMs), "RestartDrainMs must be between 0 and 120000.");
                _RestartDrainMs = value;
            }
        }

        /// <summary>
        /// During a rolling restart, how long a node waits for a peer that is restarting to report healthy again before
        /// restarting itself anyway, in milliseconds.  Default is 180000.  Minimum is 10000, maximum is 3600000.
        /// </summary>
        public int RestartPeerTimeoutMs
        {
            get
            {
                return _RestartPeerTimeoutMs;
            }
            set
            {
                if (value < 10000 || value > 3600000) throw new ArgumentOutOfRangeException(nameof(RestartPeerTimeoutMs), "RestartPeerTimeoutMs must be between 10000 and 3600000.");
                _RestartPeerTimeoutMs = value;
            }
        }

        #endregion

        #region Private-Members

        private string _ClusterName = "litegraph";
        private List<string> _TrustedProxies = new List<string>();
        private int _EndpointResyncIntervalMs = 30000;
        private ClutchSettings _Clutch = new ClutchSettings();
        private RedisSettings _Redis = new RedisSettings();
        private int _RestartDrainMs = 5000;
        private int _RestartPeerTimeoutMs = 180000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterSettings()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Resolve the effective node identifier: NodeId when set, otherwise the machine host name.
        /// </summary>
        /// <returns>Node identifier.</returns>
        public string ResolveNodeId()
        {
            if (!String.IsNullOrWhiteSpace(NodeId)) return NodeId.Trim();
            return Environment.MachineName;
        }

        #endregion
    }
}
