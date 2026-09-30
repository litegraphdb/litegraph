namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Body returned by the health endpoints (GET /v1.0/health/live and GET /v1.0/health/ready).
    /// </summary>
    public class HealthResponse
    {
        #region Public-Members

        /// <summary>
        /// Healthy, Degraded (Clutch or Redis unreachable; still serving), or Unavailable (database unreachable or draining).
        /// </summary>
        public string Status { get; set; } = null;

        /// <summary>
        /// Node that answered.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Cluster name, or null when the server is not running as a cluster node.
        /// </summary>
        public string ClusterName { get; set; } = null;

        /// <summary>
        /// Server software version.
        /// </summary>
        public string Version { get; set; } = null;

        /// <summary>
        /// UTC timestamp at which the server process started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Readiness checks.  Null on the liveness endpoint.
        /// </summary>
        public ClusterNodeChecks Checks { get; set; } = null;

        /// <summary>
        /// UTC timestamp at which the response was produced.
        /// </summary>
        public DateTime Utc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// True when Status is Healthy or Degraded, meaning the node can serve requests.
        /// </summary>
        public bool IsReady
        {
            get
            {
                return String.Equals(Status, "Healthy", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(Status, "Degraded", StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public HealthResponse()
        {
        }

        #endregion
    }
}
