namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// A distributed lock currently held in Clutch by a node of this cluster.
    /// </summary>
    public class ClusterLock
    {
        #region Public-Members

        /// <summary>
        /// Lock key without the cluster prefix, for example job/chat-retention.
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// Key class: the first key segment (schema, vectorindex, job, settings, restart).
        /// </summary>
        public string KeyClass { get; set; } = null;

        /// <summary>
        /// Lock mode: Read, Write, or Delete.
        /// </summary>
        public string Mode { get; set; } = null;

        /// <summary>
        /// LiteGraph node holding the lock, resolved from its Clutch session; null when the session is not in the registry.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Clutch node the holder is connected to.
        /// </summary>
        public string ClutchNodeId { get; set; } = null;

        /// <summary>
        /// Monotonic fencing token issued for this hold.
        /// </summary>
        public long FencingToken { get; set; } = 0;

        /// <summary>
        /// UTC timestamp at which the lock was acquired.
        /// </summary>
        public DateTime? AcquiredUtc { get; set; } = null;

        /// <summary>
        /// UTC timestamp at which the current lease expires unless renewed.
        /// </summary>
        public DateTime? LeaseExpiresUtc { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterLock()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
