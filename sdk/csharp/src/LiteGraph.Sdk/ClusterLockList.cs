namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Locks currently held by the cluster, returned by GET /v1.0/cluster/locks.
    /// </summary>
    public class ClusterLockList
    {
        #region Public-Members

        /// <summary>
        /// True when the server runs as a cluster node.  A single node takes only in-process locks, so Locks is empty.
        /// </summary>
        public bool ClusterEnabled { get; set; } = false;

        /// <summary>
        /// True when Clutch answered.  Null on a single node.
        /// </summary>
        public bool? LockServiceAvailable { get; set; } = null;

        /// <summary>
        /// Held locks, ordered by key.
        /// </summary>
        public List<ClusterLock> Locks
        {
            get
            {
                return _Locks;
            }
            set
            {
                _Locks = value ?? new List<ClusterLock>();
            }
        }

        /// <summary>
        /// UTC timestamp at which the list was produced.
        /// </summary>
        public DateTime Utc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<ClusterLock> _Locks = new List<ClusterLock>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterLockList()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
