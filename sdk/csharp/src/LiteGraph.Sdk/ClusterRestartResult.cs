namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Result of a restart request.
    /// </summary>
    public class ClusterRestartResult
    {
        #region Public-Members

        /// <summary>
        /// True when a restart was requested.
        /// </summary>
        public bool Restarting { get; set; } = false;

        /// <summary>
        /// True for a rolling restart of every cluster node (one at a time); false when only the answering server restarts.
        /// </summary>
        public bool Rolling { get; set; } = false;

        /// <summary>
        /// Restart version assigned to the request.  Null on a single node.
        /// </summary>
        public long? RestartVersion { get; set; } = null;

        /// <summary>
        /// Description of what happens next.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// UTC timestamp of the request.
        /// </summary>
        public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterRestartResult()
        {
        }

        #endregion
    }
}
