namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Threading;
    using LiteGraph.Coordination;
    using LiteGraph.Server.Classes;

    /// <summary>
    /// Identity and coordination state of this server process, whether it runs alone or as one node of a cluster.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    public class ClusterContext
    {
        #region Public-Members

        /// <summary>
        /// True when the server runs as a cluster node.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Cluster name.
        /// </summary>
        public string ClusterName { get; }

        /// <summary>
        /// Node identifier, returned in the x-litegraph-node response header.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// UTC timestamp at which this process started.
        /// </summary>
        public DateTime StartedUtc { get; } = DateTime.UtcNow;

        /// <summary>
        /// Lock provider: Clutch in cluster mode, in-process otherwise.
        /// </summary>
        public ILockProvider LockProvider { get; }

        /// <summary>
        /// True once the node has been asked to stop; readiness then reports unavailable so load balancers route elsewhere.
        /// </summary>
        public bool Draining
        {
            get { return Volatile.Read(ref _Draining) == 1; }
        }

        #endregion

        #region Private-Members

        private int _Draining = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Cluster settings.</param>
        /// <param name="lockProvider">Lock provider.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        public ClusterContext(ClusterSettings settings, ILockProvider lockProvider)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            LockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
            Enabled = settings.Enable;
            ClusterName = settings.ClusterName;
            NodeId = settings.ResolveNodeId();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Mark the node as draining.  Idempotent.
        /// </summary>
        public void BeginDrain()
        {
            Interlocked.Exchange(ref _Draining, 1);
        }

        #endregion
    }
}
