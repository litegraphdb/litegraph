namespace LiteGraph.Server.API.Agnostic
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using LiteGraph.Server.Services.Cluster;

    /// <summary>
    /// Agnostic cluster handlers: node registry and restart requests.
    /// </summary>
    internal partial class ServiceHandler
    {
        #region Cluster-Services

        /// <summary>
        /// Settings file service used by the settings routes.  Null until wired at startup.
        /// </summary>
        internal SettingsFileService SettingsFile { get; set; } = null;

        /// <summary>
        /// Cluster context.  Null until wired at startup.
        /// </summary>
        internal ClusterContext Cluster { get; set; } = null;

        /// <summary>
        /// Node registry.  Null when the server is not running as a cluster node.
        /// </summary>
        internal ClusterRegistry Registry { get; set; } = null;

        /// <summary>
        /// Node health service.  Null until wired at startup.
        /// </summary>
        internal NodeHealthService NodeHealth { get; set; } = null;

        #endregion

        #region Cluster-Routes

        internal async Task<ResponseContext> ClusterNodesRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsSystemAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            bool clusterEnabled = Cluster != null && Cluster.Enabled;
            if (clusterEnabled && Registry != null)
            {
                try
                {
                    ClusterStatus status = await Registry.GetStatusAsync(token).ConfigureAwait(false);
                    return new ResponseContext(req, status);
                }
                catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
                {
                    _Logging.Warn(_Header + "unable to read the node registry: " + e.Message);
                }
            }

            HealthResponse health = await NodeHealth.CheckAsync(token).ConfigureAwait(false);
            ClusterStatus local = new ClusterStatus
            {
                ClusterEnabled = clusterEnabled,
                ClusterName = clusterEnabled ? Cluster.ClusterName : null,
                AnsweredBy = Cluster?.NodeId,
                RegistryAvailable = clusterEnabled ? false : (bool?)null
            };
            local.Nodes.Add(new ClusterNode
            {
                NodeId = health.NodeId,
                Hostname = NodeHealth.Hostname,
                Version = health.Version,
                StartedUtc = health.StartedUtc,
                LastHeartbeatUtc = health.Utc,
                HeartbeatAgeMs = 0,
                State = NodeHealthService.ToState(health),
                Checks = health.Checks,
                RestartPending = SettingsFile != null && SettingsFile.RestartNeeded()
            });

            return new ResponseContext(req, local);
        }

        internal async Task<ResponseContext> ClusterRestart(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsSystemAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            if (Cluster == null || !Cluster.Enabled) return RestartThisNode(req);

            if (Registry == null)
                return ResponseContext.FromError(req, ApiErrorEnum.Unavailable, null, "The node registry is not running, so a cluster restart cannot be requested.");

            try
            {
                long version = await Registry.RequestRestartAsync(token).ConfigureAwait(false);
                _Logging.Warn(_Header + "cluster rolling restart " + version + " requested by system administrator " + req.Authentication.UserGUID);
                return new ResponseContext(req, new ClusterRestartResult
                {
                    Restarting = true,
                    Rolling = true,
                    RestartVersion = version,
                    Message = "Every node will restart, one at a time; each waits for the previous one to report healthy. Watch GET /v1.0/cluster/nodes for progress."
                });
            }
            catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
            {
                _Logging.Warn(_Header + "unable to request a cluster restart: " + e.Message);
                return ResponseContext.FromError(req, ApiErrorEnum.Unavailable, null, "Redis is unreachable, so a cluster restart cannot be requested: " + e.Message);
            }
        }

        #endregion

        #region Cluster-Helpers

        private ResponseContext RestartThisNode(RequestContext req)
        {
            _Logging.Warn(_Header + "server restart requested by system administrator " + req.Authentication.UserGUID + "; process will exit so the container restart policy applies the new settings.");

            // Flush and schedule a clean process exit shortly after the response is sent, so the container's
            // restart policy (unless-stopped) brings the server back with the updated litegraph.json.
            _ = Task.Run(async () =>
            {
                try { _LiteGraph.Flush(); } catch { }
                await Task.Delay(500).ConfigureAwait(false);
                Environment.Exit(0);
            });

            return new ResponseContext(req, new ClusterRestartResult
            {
                Restarting = true,
                Rolling = false,
                Message = "This server is restarting."
            });
        }

        #endregion
    }
}
