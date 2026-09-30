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
        /// True when the node holds an open lock connection to Clutch.  Null when the server is not running as a cluster node.
        /// False does not make the node unready: reads, writes, and searches take no distributed lock.
        /// </summary>
        public bool? Clutch { get; set; } = null;

        /// <summary>
        /// True when the node can reach Redis, which carries the node registry and settings-change and restart signals.
        /// Null when the server is not running as a cluster node.  False does not make the node unready.
        /// </summary>
        public bool? Redis { get; set; } = null;

        /// <summary>
        /// True when the node is shutting down or restarting and should not receive new requests.
        /// </summary>
        public bool Draining { get; set; } = false;
    }
}
