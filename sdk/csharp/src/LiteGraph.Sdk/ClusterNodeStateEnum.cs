namespace LiteGraph.Sdk
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// State of a cluster node as reported in the node registry.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ClusterNodeStateEnum>))]
    public enum ClusterNodeStateEnum
    {
        /// <summary>
        /// Serving requests with every check passing.
        /// </summary>
        Healthy,
        /// <summary>
        /// Serving requests, but Clutch or Redis is unreachable, so coordinated work waits.
        /// </summary>
        Degraded,
        /// <summary>
        /// Running but not ready to serve requests, for example because the database does not answer.
        /// </summary>
        Unavailable,
        /// <summary>
        /// Shutting down; load balancers should stop sending it requests.
        /// </summary>
        Draining,
        /// <summary>
        /// Exiting as part of a rolling restart and expected back shortly.
        /// </summary>
        Restarting,
        /// <summary>
        /// Stopped cleanly and not expected back until started again.
        /// </summary>
        Stopped,
        /// <summary>
        /// No recent heartbeat.
        /// </summary>
        Offline
    }
}
