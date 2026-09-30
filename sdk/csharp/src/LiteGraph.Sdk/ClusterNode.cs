namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// A node's entry in the cluster node registry.
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
        /// Milliseconds since the most recent heartbeat when the status was produced.
        /// </summary>
        public long? HeartbeatAgeMs { get; set; } = null;

        /// <summary>
        /// Node state.
        /// </summary>
        public ClusterNodeStateEnum State { get; set; } = ClusterNodeStateEnum.Healthy;

        /// <summary>
        /// Health checks from the node's most recent heartbeat.
        /// </summary>
        public ClusterNodeChecks Checks { get; set; } = new ClusterNodeChecks();

        /// <summary>
        /// Most recent settings version the node has seen.
        /// </summary>
        public long SettingsVersion { get; set; } = 0;

        /// <summary>
        /// True when the node needs a restart to apply saved settings or has a requested restart still to take.
        /// </summary>
        public bool RestartPending { get; set; } = false;

        /// <summary>
        /// Most recent restart version the node has seen.
        /// </summary>
        public long RestartVersion { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterNode()
        {
        }

        #endregion
    }
}
