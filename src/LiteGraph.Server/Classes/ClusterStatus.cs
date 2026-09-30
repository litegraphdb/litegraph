namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Cluster status returned by GET /v1.0/cluster/nodes: the nodes in the registry and the settings and restart counters.
    /// </summary>
    public class ClusterStatus
    {
        #region Public-Members

        /// <summary>
        /// True when the server runs as a cluster node.  When false, Nodes lists only the answering node.
        /// </summary>
        public bool ClusterEnabled { get; set; } = false;

        /// <summary>
        /// Cluster name, or null when the server is not running as a cluster node.
        /// </summary>
        public string ClusterName { get; set; } = null;

        /// <summary>
        /// Node that answered the request.
        /// </summary>
        public string AnsweredBy { get; set; } = null;

        /// <summary>
        /// True when Redis answered.  Null when the server is not running as a cluster node.  When false, Nodes lists
        /// only the answering node.
        /// </summary>
        public bool? RegistryAvailable { get; set; } = null;

        /// <summary>
        /// Settings version: incremented every time settings are saved through any node.
        /// </summary>
        public long SettingsVersion { get; set; } = 0;

        /// <summary>
        /// UTC timestamp of the most recent settings save, or null if none has been recorded.
        /// </summary>
        public DateTime? SettingsUpdatedUtc { get; set; } = null;

        /// <summary>
        /// Restart version: incremented every time a cluster restart is requested through any node.
        /// </summary>
        public long RestartVersion { get; set; } = 0;

        /// <summary>
        /// UTC timestamp of the most recent restart request, or null if none has been recorded.
        /// </summary>
        public DateTime? RestartRequestedUtc { get; set; } = null;

        /// <summary>
        /// Nodes in the registry, ordered by node identifier.
        /// </summary>
        public List<ClusterNode> Nodes
        {
            get
            {
                return _Nodes;
            }
            set
            {
                _Nodes = value ?? new List<ClusterNode>();
            }
        }

        /// <summary>
        /// UTC timestamp at which the status was produced.
        /// </summary>
        public DateTime Utc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<ClusterNode> _Nodes = new List<ClusterNode>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterStatus()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
