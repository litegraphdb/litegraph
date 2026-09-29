namespace LiteGraph.Server.Classes
{
    /// <summary>
    /// Individual readiness checks reported by GET /v1.0/health/ready.
    /// </summary>
    public class HealthChecks
    {
        /// <summary>
        /// True when the database answered a query.
        /// </summary>
        public bool Database { get; set; } = false;

        /// <summary>
        /// True when the Clutch lock service is reachable.  Null when the server is not running as a cluster node.
        /// </summary>
        public bool? Clutch { get; set; } = null;

        /// <summary>
        /// True when the node is shutting down or restarting and should not receive new requests.
        /// </summary>
        public bool Draining { get; set; } = false;
    }
}
