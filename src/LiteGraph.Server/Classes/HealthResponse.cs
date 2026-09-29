namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Body returned by the health endpoints.
    /// </summary>
    public class HealthResponse
    {
        /// <summary>
        /// Healthy when every check passes, otherwise Unavailable.
        /// </summary>
        public string Status { get; set; } = "Healthy";

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
        /// UTC timestamp at which the node started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Individual checks.  Null for the liveness endpoint.
        /// </summary>
        public HealthChecks Checks { get; set; } = null;

        /// <summary>
        /// UTC timestamp of the response.
        /// </summary>
        public DateTime Utc { get; set; } = DateTime.UtcNow;
    }
}
