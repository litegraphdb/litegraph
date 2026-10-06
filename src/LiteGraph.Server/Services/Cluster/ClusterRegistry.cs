namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;
    using StackExchange.Redis;
    using SyslogLogging;
    using ClusterNode = LiteGraph.Server.Classes.ClusterNode;

    /// <summary>
    /// Cluster node registry and change signalling, backed by Redis.
    /// Every PollIntervalMs the node writes its entry (identity, state, health) to the hash litegraph:&lt;cluster&gt;:nodes
    /// and reads four keys:
    ///   litegraph:&lt;cluster&gt;:settings:version and settings:changed-utc, written when settings are saved through any node;
    ///   litegraph:&lt;cluster&gt;:restart:version and restart:requested-utc, written when a cluster restart is requested;
    ///   litegraph:&lt;cluster&gt;:restart:node:&lt;nodeId&gt;, written when one node's restart is requested.
    /// A change to settings:changed-utc raises SettingsChanged.  A restart:requested-utc (or this node's own
    /// restart:node key) later than this process's start raises RestartRequested once, so a node that has already restarted since the request never restarts again, and a
    /// Redis restart (which loses every key) can only drop signals, never repeat them.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    public class ClusterRegistry : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// True when the most recent exchange with Redis succeeded.
        /// </summary>
        public bool IsAvailable
        {
            get { return Volatile.Read(ref _Available) == 1 && _Redis != null && _Redis.IsConnected; }
        }

        /// <summary>
        /// Set by the server when the settings file has changed since startup in a way that needs a restart.
        /// Reported in this node's registry entry as RestartPending.
        /// </summary>
        public bool SettingsRestartPending { get; set; } = false;

        /// <summary>
        /// Observability service that receives this node's state on every heartbeat.  Null to record nothing.
        /// </summary>
        public ObservabilityService Observability { get; set; } = null;

        /// <summary>
        /// Returns this node's Clutch session identifier for its registry entry.  Null to report none.
        /// </summary>
        public Func<string> ClutchSessionIdProvider { get; set; } = null;

        /// <summary>
        /// This node's identifier.
        /// </summary>
        public string NodeId { get { return _Cluster.NodeId; } }

        /// <summary>
        /// Raised when settings were saved through any node, including this one.  The argument is the settings version.
        /// Raised on the polling thread; handlers must not block.
        /// </summary>
        public event EventHandler<long> SettingsChanged;

        /// <summary>
        /// Raised once when a cluster restart was requested after this process started.  The argument is the restart version.
        /// Raised on the polling thread; handlers must not block.
        /// </summary>
        public event EventHandler<long> RestartRequested;

        #endregion

        #region Private-Members

        private static readonly string _Header = "[ClusterRegistry] ";

        private readonly RedisSettings _Settings;
        private readonly ClusterSettings _ClusterSettings;
        private readonly ClusterContext _Cluster;
        private readonly NodeHealthService _Health;
        private readonly LoggingModule _Logging;
        private readonly Serializer _Serializer;
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();

        private readonly RedisKey _NodesKey;
        private readonly RedisKey _SettingsVersionKey;
        private readonly RedisKey _SettingsChangedKey;
        private readonly RedisKey _RestartVersionKey;
        private readonly RedisKey _RestartRequestedKey;
        private readonly RedisKey _NodeRestartKey;
        private readonly RedisKey _JobsKey;
        private readonly string _NodeRestartPrefix;

        private ConnectionMultiplexer _Redis = null;
        private Task _PollTask = null;
        private int _Available = 0;
        private bool _Baselined = false;
        private string _SettingsChangedSeen = null;
        private long _SettingsVersionSeen = 0;
        private long _RestartVersionSeen = 0;
        private int _RestartSignalled = 0;
        private int _StateOverride = -1;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.  Call StartAsync to connect and begin heartbeats.
        /// </summary>
        /// <param name="clusterSettings">Cluster settings, including the Redis settings.</param>
        /// <param name="cluster">Cluster context.</param>
        /// <param name="health">Node health service.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        public ClusterRegistry(ClusterSettings clusterSettings, ClusterContext cluster, NodeHealthService health, Serializer serializer, LoggingModule logging)
        {
            _ClusterSettings = clusterSettings ?? throw new ArgumentNullException(nameof(clusterSettings));
            _Settings = clusterSettings.Redis;
            _Cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
            _Health = health ?? throw new ArgumentNullException(nameof(health));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            string prefix = "litegraph:" + cluster.ClusterName + ":";
            _NodesKey = prefix + "nodes";
            _SettingsVersionKey = prefix + "settings:version";
            _SettingsChangedKey = prefix + "settings:changed-utc";
            _RestartVersionKey = prefix + "restart:version";
            _RestartRequestedKey = prefix + "restart:requested-utc";
            _NodeRestartPrefix = prefix + "restart:node:";
            _NodeRestartKey = _NodeRestartPrefix + cluster.NodeId;
            _JobsKey = prefix + "jobs";
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Connect to Redis and start the heartbeat loop.  Does not fail when Redis is unreachable: the connection keeps
        /// retrying in the background and the node reports Redis unavailable until it succeeds.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="ArgumentException">The connection string is invalid.</exception>
        public async Task StartAsync(CancellationToken token = default)
        {
            ConfigurationOptions options = ConfigurationOptions.Parse(_Settings.ConnectionString);
            options.AbortOnConnectFail = false;
            options.ClientName = "litegraph-" + _Cluster.NodeId;

            _Redis = await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
            _Logging.Info(_Header + "node registry at " + RedactedEndpoint() + (_Redis.IsConnected ? " connected" : " not reachable yet; retrying in the background"));

            await PollOnceAsync(token).ConfigureAwait(false);
            _PollTask = Task.Run(() => PollLoopAsync(_Cts.Token));
        }

        /// <summary>
        /// Record that settings were saved, so every node notices within PollIntervalMs.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>New settings version.</returns>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task<long> SignalSettingsChangedAsync(CancellationToken token = default)
        {
            IDatabase db = Database();
            long version = await db.StringIncrementAsync(_SettingsVersionKey).ConfigureAwait(false);
            await db.StringSetAsync(_SettingsChangedKey, FormatUtc(DateTime.UtcNow)).ConfigureAwait(false);
            _Logging.Info(_Header + "settings change signalled, version " + version);
            return version;
        }

        /// <summary>
        /// Request a cluster-wide rolling restart.  Every node started before now restarts, one at a time.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>New restart version.</returns>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task<long> RequestRestartAsync(CancellationToken token = default)
        {
            IDatabase db = Database();
            long version = await db.StringIncrementAsync(_RestartVersionKey).ConfigureAwait(false);
            await db.StringSetAsync(_RestartRequestedKey, FormatUtc(DateTime.UtcNow)).ConfigureAwait(false);
            _Logging.Info(_Header + "cluster restart requested, version " + version);
            return version;
        }

        /// <summary>
        /// Request a restart of one node.  The node restarts within PollIntervalMs, taking the same Clutch restart lock as a
        /// rolling restart, so it still waits for any other node that is restarting.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">nodeId is null or empty.</exception>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task RequestNodeRestartAsync(string nodeId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(nodeId)) throw new ArgumentNullException(nameof(nodeId));
            IDatabase db = Database();
            await db.StringSetAsync(_NodeRestartPrefix + nodeId, FormatUtc(DateTime.UtcNow), TimeSpan.FromMilliseconds(_ClusterSettings.RestartPeerTimeoutMs)).ConfigureAwait(false);
            _Logging.Info(_Header + "restart of node " + nodeId + " requested");
        }

        /// <summary>
        /// Record a run of a cluster singleton job, replacing the job's previous run.
        /// </summary>
        /// <param name="run">Job run.</param>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">run is null.</exception>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task RecordJobRunAsync(ClusterJobRun run, CancellationToken token = default)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            await Database().HashSetAsync(_JobsKey, run.Job, _Serializer.SerializeJson(run, false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Read the most recent run of every cluster singleton job.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Job runs ordered by job name.</returns>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task<List<ClusterJobRun>> GetJobRunsAsync(CancellationToken token = default)
        {
            HashEntry[] entries = await Database().HashGetAllAsync(_JobsKey).ConfigureAwait(false);
            List<ClusterJobRun> runs = new List<ClusterJobRun>();
            foreach (HashEntry entry in entries)
            {
                try
                {
                    ClusterJobRun run = _Serializer.DeserializeJson<ClusterJobRun>(entry.Value.ToString());
                    runs.Add(run);
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "ignoring unreadable job run for " + entry.Name + ": " + e.Message);
                }
            }
            return runs.OrderBy(r => r.Job, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Remove a node's registry entry, for example a node that was decommissioned.  A running node re-registers on its
        /// next heartbeat, so callers should only remove nodes that are Offline or Stopped.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if an entry was removed.</returns>
        /// <exception cref="ArgumentNullException">nodeId is null or empty.</exception>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task<bool> RemoveNodeAsync(string nodeId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(nodeId)) throw new ArgumentNullException(nameof(nodeId));
            bool removed = await Database().HashDeleteAsync(_NodesKey, nodeId).ConfigureAwait(false);
            if (removed) _Logging.Info(_Header + "removed registry entry for node " + nodeId);
            return removed;
        }

        /// <summary>
        /// Read the registry: every node's entry plus the settings and restart counters.  Entries with no heartbeat within
        /// NodeTimeoutMs are reported Offline (Restarting and Stopped entries keep their state; a Restarting entry becomes
        /// Offline after Cluster.RestartPeerTimeoutMs).  Entries older than NodeRetentionMs are removed.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Cluster status.</returns>
        /// <exception cref="RedisException">Redis could not be reached.</exception>
        public async Task<ClusterStatus> GetStatusAsync(CancellationToken token = default)
        {
            IDatabase db = Database();
            HashEntry[] entries = await db.HashGetAllAsync(_NodesKey).ConfigureAwait(false);
            RedisValue[] values = await db.StringGetAsync(new RedisKey[] { _SettingsVersionKey, _SettingsChangedKey, _RestartVersionKey, _RestartRequestedKey }).ConfigureAwait(false);

            DateTime now = DateTime.UtcNow;
            ClusterStatus status = new ClusterStatus
            {
                ClusterEnabled = true,
                ClusterName = _Cluster.ClusterName,
                AnsweredBy = _Cluster.NodeId,
                RegistryAvailable = true,
                SettingsVersion = ParseLong(values[0]),
                SettingsUpdatedUtc = ParseUtc(values[1]),
                RestartVersion = ParseLong(values[2]),
                RestartRequestedUtc = ParseUtc(values[3]),
                Utc = now
            };

            List<RedisValue> expired = new List<RedisValue>();
            foreach (HashEntry entry in entries)
            {
                ClusterNode node;
                try
                {
                    node = _Serializer.DeserializeJson<ClusterNode>(entry.Value.ToString());
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "ignoring unreadable registry entry for " + entry.Name + ": " + e.Message);
                    continue;
                }

                long ageMs = (long)Math.Max(0, (now - node.LastHeartbeatUtc).TotalMilliseconds);
                if (ageMs > _Settings.NodeRetentionMs)
                {
                    expired.Add(entry.Name);
                    continue;
                }

                node.HeartbeatAgeMs = ageMs;
                if (ageMs > _Settings.NodeTimeoutMs)
                {
                    if (node.State == ClusterNodeStateEnum.Restarting)
                    {
                        if (ageMs > _ClusterSettings.RestartPeerTimeoutMs) node.State = ClusterNodeStateEnum.Offline;
                    }
                    else if (node.State != ClusterNodeStateEnum.Stopped)
                    {
                        node.State = ClusterNodeStateEnum.Offline;
                    }
                }

                status.Nodes.Add(node);
            }

            if (expired.Count > 0) await db.HashDeleteAsync(_NodesKey, expired.ToArray()).ConfigureAwait(false);

            status.Nodes = status.Nodes.OrderBy(n => n.NodeId, StringComparer.Ordinal).ToList();
            return status;
        }

        /// <summary>
        /// Report this node in a fixed state (for example Restarting) from now on and write its entry immediately.
        /// </summary>
        /// <param name="state">State to report.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task PublishStateAsync(ClusterNodeStateEnum state, CancellationToken token = default)
        {
            Interlocked.Exchange(ref _StateOverride, (int)state);
            try
            {
                await WriteHeartbeatAsync(token).ConfigureAwait(false);
            }
            catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
            {
                _Logging.Warn(_Header + "unable to publish state " + state + ": " + e.Message);
            }
        }

        /// <summary>
        /// Stop the heartbeat loop, write this node's final state (Stopped, or Restarting during a rolling restart), and
        /// close the Redis connection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;
            _Disposed = true;

            if (disposing)
            {
                _Cts.Cancel();
                try { _PollTask?.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }

                if (_Redis != null)
                {
                    if (Volatile.Read(ref _StateOverride) != (int)ClusterNodeStateEnum.Restarting)
                        Interlocked.Exchange(ref _StateOverride, (int)ClusterNodeStateEnum.Stopped);

                    try
                    {
                        using (CancellationTokenSource timeout = new CancellationTokenSource(2000))
                        {
                            WriteHeartbeatAsync(timeout.Token).GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "unable to write final registry entry: " + e.Message);
                    }

                    _Redis.Dispose();
                    _Redis = null;
                }

                _Cts.Dispose();
            }
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_Settings.PollIntervalMs, token).ConfigureAwait(false);
                    await PollOnceAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task PollOnceAsync(CancellationToken token)
        {
            try
            {
                IDatabase db = Database();
                RedisValue[] values = await db.StringGetAsync(new RedisKey[] { _SettingsVersionKey, _SettingsChangedKey, _RestartVersionKey, _RestartRequestedKey, _NodeRestartKey }).ConfigureAwait(false);
                SetAvailable(true);

                long settingsVersion = ParseLong(values[0]);
                string settingsChanged = values[1].IsNull ? null : values[1].ToString();
                long restartVersion = ParseLong(values[2]);
                DateTime? restartRequested = ParseUtc(values[3]);
                DateTime? nodeRestartRequested = ParseUtc(values[4]);

                Volatile.Write(ref _SettingsVersionSeen, settingsVersion);
                Volatile.Write(ref _RestartVersionSeen, restartVersion);

                if (!_Baselined)
                {
                    _Baselined = true;
                    _SettingsChangedSeen = settingsChanged;
                }
                else if (!String.Equals(settingsChanged, _SettingsChangedSeen, StringComparison.Ordinal))
                {
                    _SettingsChangedSeen = settingsChanged;
                    if (settingsChanged != null) Raise(SettingsChanged, settingsVersion, "settings change");
                }

                bool clusterRestart = restartRequested.HasValue && restartRequested.Value > _Cluster.StartedUtc;
                bool nodeRestart = nodeRestartRequested.HasValue && nodeRestartRequested.Value > _Cluster.StartedUtc;
                if ((clusterRestart || nodeRestart) && Interlocked.Exchange(ref _RestartSignalled, 1) == 0)
                    Raise(RestartRequested, restartVersion, clusterRestart ? "restart request" : "restart request for this node");

                await WriteHeartbeatAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                if (SetAvailable(false)) _Logging.Warn(_Header + "Redis at " + RedactedEndpoint() + " is unreachable: " + e.Message);
            }
        }

        private async Task WriteHeartbeatAsync(CancellationToken token)
        {
            HealthResponse health = await _Health.CheckAsync(token).ConfigureAwait(false);

            int stateOverride = Volatile.Read(ref _StateOverride);
            ClusterNode node = new ClusterNode
            {
                NodeId = _Cluster.NodeId,
                Hostname = _Health.Hostname,
                Version = _Health.Version,
                StartedUtc = _Cluster.StartedUtc,
                LastHeartbeatUtc = DateTime.UtcNow,
                State = stateOverride >= 0 ? (ClusterNodeStateEnum)stateOverride : NodeHealthService.ToState(health),
                Checks = health.Checks,
                SettingsVersion = Volatile.Read(ref _SettingsVersionSeen),
                RestartVersion = Volatile.Read(ref _RestartVersionSeen),
                RestartPending = SettingsRestartPending || Volatile.Read(ref _RestartSignalled) == 1,
                ClutchSessionId = ClutchSessionIdProvider?.Invoke()
            };

            Observability?.RecordNodeStatus(node.State, node.Checks.Clutch, node.Checks.Redis, node.RestartPending, node.SettingsVersion);
            await Database().HashSetAsync(_NodesKey, _Cluster.NodeId, _Serializer.SerializeJson(node, false)).ConfigureAwait(false);
        }

        private void Raise(EventHandler<long> handler, long version, string what)
        {
            _Logging.Info(_Header + what + " noticed, version " + version);
            try
            {
                handler?.Invoke(this, version);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + what + " handler failed: " + e.Message);
            }
        }

        private IDatabase Database()
        {
            ConnectionMultiplexer redis = _Redis;
            if (redis == null) throw new InvalidOperationException("The cluster registry is not started.");
            return redis.GetDatabase();
        }

        private bool SetAvailable(bool available)
        {
            int previous = Interlocked.Exchange(ref _Available, available ? 1 : 0);
            if (available && previous == 0) _Logging.Info(_Header + "Redis at " + RedactedEndpoint() + " is reachable");
            return previous != (available ? 1 : 0);
        }

        private string RedactedEndpoint()
        {
            string endpoint = _Settings.ConnectionString.Split(',')[0];
            return endpoint;
        }

        private static long ParseLong(RedisValue value)
        {
            if (value.IsNull) return 0;
            return Int64.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) ? parsed : 0;
        }

        private static DateTime? ParseUtc(RedisValue value)
        {
            if (value.IsNull) return null;
            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed)) return parsed.ToUniversalTime();
            return null;
        }

        private static string FormatUtc(DateTime utc)
        {
            return utc.ToString("o", CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
