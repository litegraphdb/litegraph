namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// The most recent run of a cluster singleton job (one node per cycle).
    /// </summary>
    public class ClusterJobRun
    {
        #region Public-Members

        /// <summary>
        /// Job name, for example chat-retention or request-history-purge.
        /// </summary>
        public string Job { get; set; } = null;

        /// <summary>
        /// Node that ran the job.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// UTC timestamp at which the run started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp at which the run finished.
        /// </summary>
        public DateTime CompletedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Run duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// True when the run completed without an error.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message when the run failed; otherwise null.
        /// </summary>
        public string Message { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterJobRun()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
