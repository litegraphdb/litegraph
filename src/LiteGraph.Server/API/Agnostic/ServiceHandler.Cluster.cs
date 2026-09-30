namespace LiteGraph.Server.API.Agnostic
{
    using System;
    using System.Linq;
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

        internal async Task<ResponseContext> ClusterNodeRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsSystemAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (String.IsNullOrEmpty(req.NodeId)) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "A node identifier is required.");

            ResponseContext list = await ClusterNodesRead(req, token).ConfigureAwait(false);
            ClusterStatus status = list.Data as ClusterStatus;
            ClusterNode node = status?.Nodes.FirstOrDefault(n => String.Equals(n.NodeId, req.NodeId, StringComparison.Ordinal));
            if (node == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "Node '" + req.NodeId + "' is not in the node registry.");
            return new ResponseContext(req, node);
        }

        internal async Task<ResponseContext> ClusterNodeRestart(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsSystemAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (String.IsNullOrEmpty(req.NodeId)) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "A node identifier is required.");

            if (Cluster == null || !Cluster.Enabled)
            {
                if (!String.Equals(req.NodeId, Cluster?.NodeId, StringComparison.Ordinal))
                    return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "Node '" + req.NodeId + "' is not this server.");
                return RestartThisNode(req);
            }

            if (Registry == null)
                return ResponseContext.FromError(req, ApiErrorEnum.Unavailable, null, "The node registry is not running, so a node restart cannot be requested.");

            try
            {
                ClusterStatus status = await Registry.GetStatusAsync(token).ConfigureAwait(false);
                ClusterNode node = status.Nodes.FirstOrDefault(n => String.Equals(n.NodeId, req.NodeId, StringComparison.Ordinal));
                if (node == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "Node '" + req.NodeId + "' is not in the node registry.");
                if (node.State == ClusterNodeStateEnum.Offline || node.State == ClusterNodeStateEnum.Stopped)
                    return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, "Node '" + req.NodeId + "' is " + node.State + " and cannot be restarted through the cluster; start it with its supervisor.");

                await Registry.RequestNodeRestartAsync(req.NodeId, token).ConfigureAwait(false);
                _Logging.Warn(_Header + "restart of node " + req.NodeId + " requested by system administrator " + req.Authentication.UserGUID);
                return new ResponseContext(req, new ClusterRestartResult
                {
                    Restarting = true,
                    Rolling = false,
                    RestartVersion = null,
                    Message = "Node '" + req.NodeId + "' will restart within a few seconds, after any other node that is restarting is back."
                });
            }
            catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
            {
                _Logging.Warn(_Header + "unable to request a node restart: " + e.Message);
                return ResponseContext.FromError(req, ApiErrorEnum.Unavailable, null, "Redis is unreachable, so a node restart cannot be requested: " + e.Message);
            }
        }

        internal async Task<ResponseContext> ClusterNodeDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsSystemAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (String.IsNullOrEmpty(req.NodeId)) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "A node identifier is required.");

            if (Cluster == null || !Cluster.Enabled || Registry == null)
                return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "There is no node registry: this server is not running as a cluster node.");

            try
            {
                ClusterStatus status = await Registry.GetStatusAsync(token).ConfigureAwait(false);
                ClusterNode node = status.Nodes.FirstOrDefault(n => String.Equals(n.NodeId, req.NodeId, StringComparison.Ordinal));
                if (node == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "Node '" + req.NodeId + "' is not in the node registry.");
                if (node.State != ClusterNodeStateEnum.Offline && node.State != ClusterNodeStateEnum.Stopped)
                    return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, "Node '" + req.NodeId + "' is " + node.State + "; only Offline or Stopped nodes can be removed, because a running node registers again on its next heartbeat.");

                await Registry.RemoveNodeAsync(req.NodeId, token).ConfigureAwait(false);
                return new ResponseContext(req);
            }
            catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
            {
                _Logging.Warn(_Header + "unable to remove a node: " + e.Message);
                return ResponseContext.FromError(req, ApiErrorEnum.Unavailable, null, "Redis is unreachable: " + e.Message);
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
