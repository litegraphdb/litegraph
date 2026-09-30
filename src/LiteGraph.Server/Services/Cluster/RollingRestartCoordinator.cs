namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Coordination;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Restarts this node as its turn comes in a cluster-wide rolling restart.
    /// When a restart is requested, every node tries to take the Clutch restart lock; the holder waits until no other node
    /// is draining or restarting (so the node that restarted before it is back and healthy), reports itself Restarting,
    /// drains for Cluster.RestartDrainMs, begins shutting down so its supervisor (the container restart policy) starts it
    /// again, and releases the lock.  The next node then waits for this one to report healthy before taking its own turn,
    /// so only one node is ever down.
    /// Thread safety: safe for concurrent use; at most one restart runs per process.
    /// </summary>
    public class RollingRestartCoordinator
    {
        #region Public-Members

        /// <summary>
        /// True once this node has started taking its turn in a rolling restart.
        /// </summary>
        public bool InProgress
        {
            get { return Volatile.Read(ref _InProgress) == 1; }
        }

        #endregion

        #region Private-Members

        private static readonly string _Header = "[RollingRestart] ";
        private static readonly int _LockWaitMs = 55000;
        private static readonly int _RetryDelayMs = 2000;

        private readonly ClusterSettings _Settings;
        private readonly ClusterContext _Cluster;
        private readonly ClusterRegistry _Registry;
        private readonly LoggingModule _Logging;
        private readonly Action<string> _Exit;
        private int _InProgress = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Cluster settings.</param>
        /// <param name="cluster">Cluster context, providing the lock provider and drain flag.</param>
        /// <param name="registry">Node registry.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="exit">Called with a reason to shut the process down cleanly once this node's turn has come.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        public RollingRestartCoordinator(ClusterSettings settings, ClusterContext cluster, ClusterRegistry registry, LoggingModule logging, Action<string> exit)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
            _Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Exit = exit ?? throw new ArgumentNullException(nameof(exit));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Take this node's turn in a rolling restart.  Returns without doing anything if a turn is already in progress.
        /// Runs until the node exits or the token is cancelled.
        /// </summary>
        /// <param name="restartVersion">Restart version being applied, for logging.</param>
        /// <param name="token">Cancellation token, cancelled when the server shuts down for another reason.</param>
        public async Task RunAsync(long restartVersion, CancellationToken token = default)
        {
            if (Interlocked.Exchange(ref _InProgress, 1) == 1) return;
            _Logging.Info(_Header + "restart version " + restartVersion + " requested; waiting for this node's turn");

            ILockHandle turn = await AcquireTurnAsync(token).ConfigureAwait(false);
            _Logging.Info(_Header + "this node's turn; waiting for peers that are restarting to come back");

            await WaitForPeersAsync(token).ConfigureAwait(false);

            _Logging.Info(_Header + "restarting: draining for " + _Settings.RestartDrainMs + "ms, then exiting");
            await _Registry.PublishStateAsync(ClusterNodeStateEnum.Restarting, token).ConfigureAwait(false);
            _Cluster.BeginDrain();
            await Task.Delay(_Settings.RestartDrainMs, token).ConfigureAwait(false);

            // This node already reports Restarting, so the next node to take the lock waits until it is back and healthy.
            _Exit("rolling restart version " + restartVersion);
            await turn.DisposeAsync().ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task<ILockHandle> AcquireTurnAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    return await _Cluster.LockProvider.AcquireAsync(LockKeys.Restart, LockModeEnum.Write, LockAcquireOptions.WaitUpTo(_LockWaitMs), token).ConfigureAwait(false);
                }
                catch (LockNotAcquiredException)
                {
                    // Another node is taking its turn; keep waiting.
                }
                catch (LockProviderUnavailableException e)
                {
                    _Logging.Warn(_Header + "Clutch unavailable while waiting for this node's turn: " + e.Message);
                    await Task.Delay(_RetryDelayMs, token).ConfigureAwait(false);
                }
            }
        }

        private async Task WaitForPeersAsync(CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(_Settings.RestartPeerTimeoutMs);

            while (true)
            {
                token.ThrowIfCancellationRequested();
                List<string> busy = null;

                try
                {
                    ClusterStatus status = await _Registry.GetStatusAsync(token).ConfigureAwait(false);
                    busy = status.Nodes
                        .Where(n => !String.Equals(n.NodeId, _Cluster.NodeId, StringComparison.Ordinal))
                        .Where(n => n.State == ClusterNodeStateEnum.Restarting || n.State == ClusterNodeStateEnum.Draining)
                        .Select(n => n.NodeId + " (" + n.State + ")")
                        .ToList();
                }
                catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
                {
                    _Logging.Warn(_Header + "unable to read the node registry: " + e.Message);
                }

                if (busy != null && busy.Count == 0) return;

                if (DateTime.UtcNow >= deadline)
                {
                    _Logging.Warn(_Header + "gave up waiting after " + _Settings.RestartPeerTimeoutMs + "ms for " + (busy == null ? "the node registry" : String.Join(", ", busy)) + "; restarting anyway");
                    return;
                }

                await Task.Delay(_RetryDelayMs, token).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
