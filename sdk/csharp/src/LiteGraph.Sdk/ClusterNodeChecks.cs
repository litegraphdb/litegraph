namespace LiteGraph.Sdk
{
    /// <summary>
    /// Readiness checks reported for a cluster node.
    /// </summary>
    public class ClusterNodeChecks
    {
        #region Public-Members

        /// <summary>
        /// True when the database answered a query.
        /// </summary>
        public bool Database { get; set; } = false;

        /// <summary>
        /// True when the node has a lock connection to Clutch.  Null when the server is not running as a cluster node.
        /// </summary>
        public bool? Clutch { get; set; } = null;

        /// <summary>
        /// True when the node can reach Redis.  Null when the server is not running as a cluster node.
        /// </summary>
        public bool? Redis { get; set; } = null;

        /// <summary>
        /// True when the node is shutting down or restarting.
        /// </summary>
        public bool Draining { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterNodeChecks()
        {
        }

        #endregion
    }
}
