namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// A cluster node's entry in the node registry, written by the node on every heartbeat.
    /// </summary>
    public class ClusterNode
    {
        #region Public-Members

        /// <summary>
        /// Node identifier.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Host name of the machine or container running the node.
        /// </summary>
        public string Hostname { get; set; } = null;

        /// <summary>
        /// Server software version.
        /// </summary>
        public string Version { get; set; } = null;

        /// <summary>
        /// UTC timestamp at which the node's current process started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp of the node's most recent heartbeat.
        /// </summary>
        public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Milliseconds since the most recent heartbeat, computed when the registry is read.  Null in stored entries.
        /// </summary>
        public long? HeartbeatAgeMs { get; set; } = null;

        /// <summary>
        /// Node state.  Offline when the registry has not heard from the node within Cluster.Redis.NodeTimeoutMs.
        /// </summary>
        public ClusterNodeStateEnum State { get; set; } = ClusterNodeStateEnum.Healthy;

        /// <summary>
        /// Health checks from the node's most recent heartbeat.
        /// </summary>
        public HealthChecks Checks
        {
            get
            {
                return _Checks;
            }
            set
            {
                _Checks = value ?? new HealthChecks();
            }
        }

        /// <summary>
        /// Most recent settings version the node has seen.  Lower than the cluster's settings version while the node has
        /// not yet noticed a change.
        /// </summary>
        public long SettingsVersion { get; set; } = 0;

        /// <summary>
        /// True when the settings file has changed since the node started in a way that only takes effect after a restart.
        /// </summary>
        public bool RestartPending { get; set; } = false;

        /// <summary>
        /// Most recent restart version the node has seen.
        /// </summary>
        public long RestartVersion { get; set; } = 0;

        /// <summary>
        /// Clutch session of the node's lock connection, used to attribute held locks to nodes.  Null on a single node or
        /// while the node is disconnected from Clutch.
        /// </summary>
        public string ClutchSessionId { get; set; } = null;

        #endregion

        #region Private-Members

        private HealthChecks _Checks = new HealthChecks();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterNode()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
