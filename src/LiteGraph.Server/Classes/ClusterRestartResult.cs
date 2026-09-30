namespace LiteGraph.Server.Classes
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
        public bool Restarting { get; set; } = true;

        /// <summary>
        /// True for a cluster-wide rolling restart, in which every node restarts one at a time.  False when only the
        /// answering node restarts (single-node mode).
        /// </summary>
        public bool Rolling { get; set; } = false;

        /// <summary>
        /// Restart version assigned to this request.  Null in single-node mode.
        /// </summary>
        public long? RestartVersion { get; set; } = null;

        /// <summary>
        /// Human-readable description of what happens next.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// UTC timestamp of the request.
        /// </summary>
        public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterRestartResult()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
