namespace LiteGraph.Server.Services
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using LiteGraph.Coordination;
    using LiteGraph.Server.Classes;

    /// <summary>
    /// Node and cluster instruments: node identity, state, dependency connectivity, pending restarts, and distributed lock
    /// acquisitions and losses.  Per-node gauges carry a node_id label (not node, which Prometheus scrape configurations
    /// commonly attach as a target label); lock metrics are labeled by key class, the first key segment, never a GUID.
    /// Offline nodes are visible through Prometheus's own up metric for their scrape target.
    /// </summary>
    public partial class ObservabilityService
    {
        #region Cluster-Private-Members

        private string _NodeId = null;
        private string _NodeClusterName = null;
        private string _NodeVersion = null;
        private DateTime _NodeStartedUtc = DateTime.UtcNow;
        private int _NodeState = -1;
        private int _ClutchConnected = -1;
        private int _RedisConnected = -1;
        private int _NodeRestartPending = 0;
        private long _NodeSettingsVersion = 0;

        #endregion

        #region Cluster-Public-Methods

        /// <summary>
        /// Record this node's identity, reported as litegraph_node_info.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="clusterName">Cluster name, or null on a single node.</param>
        /// <param name="version">Server software version.</param>
        /// <param name="startedUtc">UTC time the process started.</param>
        public void RecordNodeIdentity(string nodeId, string clusterName, string version, DateTime startedUtc)
        {
            _NodeId = nodeId;
            _NodeClusterName = clusterName;
            _NodeVersion = version;
            _NodeStartedUtc = startedUtc;
        }

        /// <summary>
        /// Record this node's latest state, written on every cluster heartbeat.
        /// </summary>
        /// <param name="state">Node state.</param>
        /// <param name="clutchConnected">Whether the Clutch lock connection is open; null when not a cluster node.</param>
        /// <param name="redisConnected">Whether Redis is reachable; null when not a cluster node.</param>
        /// <param name="restartPending">Whether the node needs a restart to apply saved settings or a requested restart.</param>
        /// <param name="settingsVersion">Most recent cluster settings version the node has seen.</param>
        public void RecordNodeStatus(ClusterNodeStateEnum state, bool? clutchConnected, bool? redisConnected, bool restartPending, long settingsVersion)
        {
            Interlocked.Exchange(ref _NodeState, (int)state);
            Interlocked.Exchange(ref _ClutchConnected, clutchConnected.HasValue ? (clutchConnected.Value ? 1 : 0) : -1);
            Interlocked.Exchange(ref _RedisConnected, redisConnected.HasValue ? (redisConnected.Value ? 1 : 0) : -1);
            Interlocked.Exchange(ref _NodeRestartPending, restartPending ? 1 : 0);
            Interlocked.Exchange(ref _NodeSettingsVersion, settingsVersion);
        }

        /// <summary>
        /// Record a lock acquisition attempt.
        /// </summary>
        /// <param name="key">Lock key; only its class (first segment) is used as a label.</param>
        /// <param name="outcome">acquired, denied, unavailable, or cancelled.</param>
        /// <param name="durationMs">Time spent acquiring, in milliseconds.</param>
        public void RecordLockAcquire(string key, string outcome, double durationMs)
        {
            if (!_Settings.Enable) return;
            string[] labelNames = new string[] { "key_class", "outcome" };
            string[] labelValues = new string[] { NormalizeLabel(LockKeys.KeyClass(key)), NormalizeLabel(outcome) };
            OperationCounter("litegraph_lock_acquires_total", "Total distributed and local lock acquisition attempts by key class and outcome.", labelNames, labelValues).Add(1);
            OperationSummary("litegraph_lock_acquire_duration_ms", "Total and count of lock acquisition durations in milliseconds.", labelNames, labelValues).Record(durationMs);
        }

        /// <summary>
        /// Record a held lock that was lost, for example because the Clutch connection dropped.
        /// </summary>
        /// <param name="key">Lock key; only its class (first segment) is used as a label.</param>
        public void RecordLockLost(string key)
        {
            if (!_Settings.Enable) return;
            string[] labelNames = new string[] { "key_class" };
            string[] labelValues = new string[] { NormalizeLabel(LockKeys.KeyClass(key)) };
            OperationCounter("litegraph_lock_lost_total", "Total held locks lost before release, by key class.", labelNames, labelValues).Add(1);
        }

        #endregion

        #region Cluster-Private-Methods

        private void RenderPrometheusCluster(StringBuilder sb)
        {
            if (String.IsNullOrEmpty(_NodeId)) return;
            string node = "node_id=\"" + EscapeLabel(_NodeId) + "\"";

            sb.AppendLine("# HELP litegraph_node_info Identity of this LiteGraph node; always 1.");
            sb.AppendLine("# TYPE litegraph_node_info gauge");
            sb.AppendLine("litegraph_node_info{" + node + ",cluster=\"" + EscapeLabel(_NodeClusterName ?? "") + "\",version=\"" + EscapeLabel(_NodeVersion ?? "") + "\"} 1");

            sb.AppendLine("# HELP litegraph_node_start_time_seconds Unix time at which this node's process started.");
            sb.AppendLine("# TYPE litegraph_node_start_time_seconds gauge");
            sb.AppendLine("litegraph_node_start_time_seconds{" + node + "} " + new DateTimeOffset(_NodeStartedUtc).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

            int state = Volatile.Read(ref _NodeState);
            if (state >= 0)
            {
                sb.AppendLine("# HELP litegraph_node_state Current state of this node as reported to the node registry: 1 for the current state, 0 otherwise.");
                sb.AppendLine("# TYPE litegraph_node_state gauge");
                foreach (ClusterNodeStateEnum value in Enum.GetValues<ClusterNodeStateEnum>())
                {
                    if (value == ClusterNodeStateEnum.Offline) continue;
                    sb.AppendLine("litegraph_node_state{" + node + ",state=\"" + value + "\"} " + ((int)value == state ? "1" : "0"));
                }

                sb.AppendLine("# HELP litegraph_node_restart_pending 1 when this node needs a restart to apply saved settings or a requested restart.");
                sb.AppendLine("# TYPE litegraph_node_restart_pending gauge");
                sb.AppendLine("litegraph_node_restart_pending{" + node + "} " + Volatile.Read(ref _NodeRestartPending).ToString(CultureInfo.InvariantCulture));

                sb.AppendLine("# HELP litegraph_node_settings_version Most recent cluster settings version this node has seen.");
                sb.AppendLine("# TYPE litegraph_node_settings_version gauge");
                sb.AppendLine("litegraph_node_settings_version{" + node + "} " + Interlocked.Read(ref _NodeSettingsVersion).ToString(CultureInfo.InvariantCulture));
            }

            int clutch = Volatile.Read(ref _ClutchConnected);
            if (clutch >= 0)
            {
                sb.AppendLine("# HELP litegraph_clutch_connected 1 when this node holds an open lock connection to Clutch.");
                sb.AppendLine("# TYPE litegraph_clutch_connected gauge");
                sb.AppendLine("litegraph_clutch_connected{" + node + "} " + clutch.ToString(CultureInfo.InvariantCulture));
            }

            int redis = Volatile.Read(ref _RedisConnected);
            if (redis >= 0)
            {
                sb.AppendLine("# HELP litegraph_redis_connected 1 when this node can reach the Redis node registry.");
                sb.AppendLine("# TYPE litegraph_redis_connected gauge");
                sb.AppendLine("litegraph_redis_connected{" + node + "} " + redis.ToString(CultureInfo.InvariantCulture));
            }
        }

        #endregion
    }
}
