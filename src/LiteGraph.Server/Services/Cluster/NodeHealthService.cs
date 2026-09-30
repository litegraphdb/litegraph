namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Computes this node's health for the readiness endpoint and the cluster node registry, so both always agree.
    /// A node is ready while the database answers and it is not draining.  Clutch and Redis are reported but do not make
    /// the node unready: reads, writes, and searches need neither, so losing them only delays coordinated work.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    public class NodeHealthService
    {
        #region Public-Members

        /// <summary>
        /// Host name of the machine or container running the node.
        /// </summary>
        public string Hostname { get; } = Dns.GetHostName();

        /// <summary>
        /// Server software version.
        /// </summary>
        public string Version { get; } = typeof(NodeHealthService).Assembly.GetName().Version?.ToString(3);

        /// <summary>
        /// Node registry, or null when the server is not running as a cluster node.  Set after construction.
        /// </summary>
        public ClusterRegistry Registry { get; set; } = null;

        #endregion

        #region Private-Members

        private static readonly string _Header = "[NodeHealthService] ";

        private readonly LiteGraphClient _LiteGraph;
        private readonly ClusterContext _Cluster;
        private readonly LoggingModule _Logging;
        private int _DatabaseCheckTimeoutMs = 5000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="litegraph">LiteGraph client, used for the database check.</param>
        /// <param name="cluster">Cluster context.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        public NodeHealthService(LiteGraphClient litegraph, ClusterContext cluster, LoggingModule logging)
        {
            _LiteGraph = litegraph ?? throw new ArgumentNullException(nameof(litegraph));
            _Cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the health response without running checks, as returned by the liveness endpoint.
        /// </summary>
        /// <returns>Health response with Status Healthy and no checks.</returns>
        public HealthResponse Basic()
        {
            return new HealthResponse
            {
                Status = "Healthy",
                NodeId = _Cluster.NodeId,
                ClusterName = _Cluster.Enabled ? _Cluster.ClusterName : null,
                Version = Version,
                StartedUtc = _Cluster.StartedUtc,
                Utc = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Run the readiness checks.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Health response.  Status is Healthy, Degraded (Clutch or Redis unreachable), or Unavailable (database
        /// unreachable or node draining).</returns>
        public async Task<HealthResponse> CheckAsync(CancellationToken token = default)
        {
            HealthResponse health = Basic();
            health.Checks = new HealthChecks
            {
                Draining = _Cluster.Draining,
                Clutch = _Cluster.Enabled ? _Cluster.LockProvider.IsAvailable : (bool?)null,
                Redis = _Cluster.Enabled ? (Registry?.IsAvailable ?? false) : (bool?)null
            };

            try
            {
                using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cts.CancelAfter(_DatabaseCheckTimeoutMs);
                    await _LiteGraph.Tenant.ExistsByGuid(Guid.Empty, cts.Token).ConfigureAwait(false);
                }
                health.Checks.Database = true;
            }
            catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
            {
                _Logging.Warn(_Header + "database check failed: " + e.Message);
                health.Checks.Database = false;
            }

            bool ready = health.Checks.Database && !health.Checks.Draining;
            if (!ready) health.Status = "Unavailable";
            else if (health.Checks.Clutch == false || health.Checks.Redis == false) health.Status = "Degraded";
            else health.Status = "Healthy";

            return health;
        }

        /// <summary>
        /// Map a health response to a registry state.
        /// </summary>
        /// <param name="health">Health response from CheckAsync.</param>
        /// <returns>Node state.</returns>
        /// <exception cref="ArgumentNullException">health is null.</exception>
        public static ClusterNodeStateEnum ToState(HealthResponse health)
        {
            if (health == null) throw new ArgumentNullException(nameof(health));
            if (health.Checks != null && health.Checks.Draining) return ClusterNodeStateEnum.Draining;
            if (health.Status == "Unavailable") return ClusterNodeStateEnum.Unavailable;
            if (health.Status == "Degraded") return ClusterNodeStateEnum.Degraded;
            return ClusterNodeStateEnum.Healthy;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
