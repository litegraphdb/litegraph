namespace LiteGraph.Server.Classes
{
    using System.Runtime.Serialization;

    /// <summary>
    /// State of a cluster node as reported in the node registry.
    /// </summary>
    public enum ClusterNodeStateEnum
    {
        /// <summary>
        /// Serving requests with every check passing.
        /// </summary>
        [EnumMember(Value = "Healthy")]
        Healthy,
        /// <summary>
        /// Serving requests, but Clutch or Redis is unreachable, so coordinated work waits.
        /// </summary>
        [EnumMember(Value = "Degraded")]
        Degraded,
        /// <summary>
        /// Running but not ready to serve requests, for example because the database does not answer.
        /// </summary>
        [EnumMember(Value = "Unavailable")]
        Unavailable,
        /// <summary>
        /// Shutting down; load balancers should stop sending it requests.
        /// </summary>
        [EnumMember(Value = "Draining")]
        Draining,
        /// <summary>
        /// Exiting as part of a rolling restart and expected back shortly.
        /// </summary>
        [EnumMember(Value = "Restarting")]
        Restarting,
        /// <summary>
        /// Stopped cleanly and not expected back until started again.
        /// </summary>
        [EnumMember(Value = "Stopped")]
        Stopped,
        /// <summary>
        /// No heartbeat within Cluster.Redis.NodeTimeoutMs.  Reported by the registry, never by the node itself.
        /// </summary>
        [EnumMember(Value = "Offline")]
        Offline
    }
}
