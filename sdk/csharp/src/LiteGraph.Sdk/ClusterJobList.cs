namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Most recent run of each cluster singleton job, returned by GET /v1.0/cluster/jobs.
    /// </summary>
    public class ClusterJobList
    {
        #region Public-Members

        /// <summary>
        /// True when the server runs as a cluster node.  A single node records no runs, so Jobs is empty.
        /// </summary>
        public bool ClusterEnabled { get; set; } = false;

        /// <summary>
        /// True when Redis answered.  Null on a single node.
        /// </summary>
        public bool? RegistryAvailable { get; set; } = null;

        /// <summary>
        /// Most recent run of each job, ordered by job name.
        /// </summary>
        public List<ClusterJobRun> Jobs
        {
            get
            {
                return _Jobs;
            }
            set
            {
                _Jobs = value ?? new List<ClusterJobRun>();
            }
        }

        /// <summary>
        /// UTC timestamp at which the list was produced.
        /// </summary>
        public DateTime Utc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<ClusterJobRun> _Jobs = new List<ClusterJobRun>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterJobList()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
