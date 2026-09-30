namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Clutch.Sdk;
    using LiteGraph.Coordination;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Distributed lock provider backed by Clutch, using the Clutch.Sdk WebSocket lock client.
    /// All locks held by this process share one lock connection, and the SDK renews their leases in the background.
    /// When the connection closes, Clutch releases every lock it held, so each of those handles is marked lost and its
    /// LostToken is cancelled; a handle whose lease goes unrenewed for a full lease period is marked lost the same way.
    /// The provider reconnects in the background, so a Clutch restart or failover costs only the locks held at that
    /// moment.  If the process dies, Clutch releases its locks when the connection drops or the leases expire.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    public class ClutchLockProvider : ILockProvider
    {
        #region Public-Members

        /// <inheritdoc />
        public string Name { get { return "Clutch"; } }

        /// <inheritdoc />
        public bool IsDistributed { get { return true; } }

        /// <inheritdoc />
        public bool IsAvailable { get { return Volatile.Read(ref _Client) != null; } }

        /// <summary>
        /// Number of locks currently held by this process.
        /// </summary>
        public int HeldCount { get { return _Held.Count; } }

        /// <summary>
        /// Raised with the lock key when a held lock is lost before release (connection closed or lease unrenewed).
        /// Raised on the thread that detected the loss; handlers must not block.
        /// </summary>
        public event EventHandler<string> LockLost;

        /// <summary>
        /// Clutch session identifier of the current lock connection, or null while disconnected.  Locks listed by Clutch
        /// carry this session, which is how a held lock is attributed to this node.
        /// </summary>
        public string SessionId
        {
            get { return Volatile.Read(ref _Client)?.Welcome?.SessionId; }
        }

        #endregion

        #region Private-Members

        private static readonly string _Header = "[ClutchLockProvider] ";
        private static readonly int _MaxServerWaitMs = 55000;
        private static readonly int _MaxTransientRetries = 10;
        private static readonly int _MonitorIntervalMs = 1000;
        private static readonly int _ReconnectIntervalMs = 5000;

        private readonly ClutchSettings _Settings;
        private readonly string _KeyPrefix;
        private readonly LoggingModule _Logging;
        private readonly ConcurrentDictionary<string, ClutchLockHandle> _Held = new ConcurrentDictionary<string, ClutchLockHandle>(StringComparer.Ordinal);
        private readonly SemaphoreSlim _ConnectLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private readonly Task _MonitorTask;

        private ClutchLockClient _Client = null;
        private DateTime _LastReconnectAttemptUtc = DateTime.MinValue;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Clutch settings.</param>
        /// <param name="clusterName">Cluster name, used to namespace lock keys.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">A required argument is null or the access key is not configured.</exception>
        public ClutchLockProvider(ClutchSettings settings, string clusterName, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (String.IsNullOrEmpty(settings.AccessKey)) throw new ArgumentNullException(nameof(settings), "Clutch access key is not configured.");
            if (String.IsNullOrEmpty(clusterName)) throw new ArgumentNullException(nameof(clusterName));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _KeyPrefix = "litegraph/" + clusterName + "/";

            _MonitorTask = Task.Run(() => MonitorLoopAsync(_Cts.Token));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Open the lock connection to Clutch, retrying until it answers or the startup connect timeout elapses.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="LockProviderUnavailableException">Clutch did not answer within StartupConnectTimeoutMs.</exception>
        public async Task ConnectAsync(CancellationToken token = default)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(_Settings.StartupConnectTimeoutMs);
            int delayMs = 500;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    await GetClientAsync(token).ConfigureAwait(false);
                    return;
                }
                catch (LockProviderUnavailableException e)
                {
                    if (DateTime.UtcNow >= deadline)
                        throw new LockProviderUnavailableException(Name, "Unable to reach Clutch at " + _Settings.Endpoint + ": " + e.Message, e);

                    _Logging.Warn(_Header + "waiting for Clutch at " + _Settings.Endpoint + ": " + e.Message);
                    await Task.Delay(delayMs, token).ConfigureAwait(false);
                    delayMs = Math.Min(delayMs * 2, 5000);
                }
            }
        }

        /// <inheritdoc />
        public async Task<ILockHandle> AcquireAsync(string key, LockModeEnum mode, LockAcquireOptions options = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            ThrowIfDisposed();
            if (options == null) options = new LockAcquireOptions();

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(options.Wait ? options.TimeoutMs : 0);
            int transientFailures = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                int remainingMs = (int)Math.Max(0, (deadline - DateTime.UtcNow).TotalMilliseconds);
                bool wait = options.Wait && remainingMs > 0;
                int serverWaitMs = Math.Min(remainingMs, _MaxServerWaitMs);

                AcquireOutcome outcome;
                try
                {
                    outcome = await TryAcquireCoreAsync(key, mode, wait, serverWaitMs, token).ConfigureAwait(false);
                }
                catch (LockProviderUnavailableException e) when (options.Wait && DateTime.UtcNow < deadline && transientFailures < _MaxTransientRetries)
                {
                    // A Clutch node restarting or failing over drops the connection; reconnect and retry while the caller is willing to wait.
                    transientFailures++;
                    _Logging.Warn(_Header + "transient failure acquiring " + key + " (attempt " + transientFailures + "): " + e.Message);
                    await Task.Delay(Math.Min(250 * transientFailures, 2000), token).ConfigureAwait(false);
                    continue;
                }

                if (outcome.Handle != null) return outcome.Handle;

                if (!wait || DateTime.UtcNow >= deadline)
                    throw new LockNotAcquiredException(key, mode, outcome.Reason ?? (options.Wait ? "Timeout" : "Denied"));
            }
        }

        /// <inheritdoc />
        public async Task<ILockHandle> TryAcquireAsync(string key, LockModeEnum mode, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            ThrowIfDisposed();
            AcquireOutcome outcome = await TryAcquireCoreAsync(key, mode, false, 0, token).ConfigureAwait(false);
            return outcome.Handle;
        }

        /// <summary>
        /// List the locks this cluster currently holds in Clutch (keys under litegraph/&lt;ClusterName&gt;/), using the Clutch
        /// administration API with the configured access key.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Held locks with keys relative to the cluster prefix; NodeId is left for the caller to resolve.</returns>
        /// <exception cref="LockProviderUnavailableException">Clutch could not be reached or refused the request.</exception>
        public async Task<List<ClusterLock>> ListLocksAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            try
            {
                using (ClutchAdminClient admin = new ClutchAdminClient(_Settings.Endpoint))
                using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeout.CancelAfter(_Settings.RequestTimeoutMs);
                    TokenResponse auth = await admin.AuthenticateWithKeyAsync(_Settings.AccessKey, timeout.Token).ConfigureAwait(false);
                    EnumerationResult<LockHolder> page = await admin.ListLocksAsync(auth.TenantId, null, null, 1000, 0, null, timeout.Token).ConfigureAwait(false);
                    List<LockHolder> holders = page?.Objects ?? new List<LockHolder>();

                    return holders
                        .Where(h => h.LockKey != null && h.LockKey.StartsWith(_KeyPrefix, StringComparison.Ordinal))
                        .Select(h =>
                        {
                            string key = h.LockKey.Substring(_KeyPrefix.Length);
                            return new ClusterLock
                            {
                                Key = key,
                                KeyClass = LockKeys.KeyClass(key),
                                Mode = h.Mode.ToString(),
                                ClutchNodeId = h.NodeId,
                                FencingToken = h.FencingToken,
                                AcquiredUtc = h.AcquiredUtc,
                                LeaseExpiresUtc = h.LeaseExpiresUtc,
                                NodeId = h.SessionId
                            };
                        })
                        .OrderBy(l => l.Key, StringComparer.Ordinal)
                        .ToList();
                }
            }
            catch (Exception e) when (e is ClutchException || (e is OperationCanceledException && !token.IsCancellationRequested) || e is System.Net.Http.HttpRequestException)
            {
                throw new LockProviderUnavailableException(Name, "Unable to list locks in Clutch: " + e.Message, e);
            }
        }

        /// <summary>
        /// Dispose, closing the lock connection, which releases every lock held by this process.
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
                try { _MonitorTask.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }

                ClutchLockClient client = Interlocked.Exchange(ref _Client, null);
                foreach (ClutchLockHandle handle in _Held.Values.ToList()) handle.MarkLost();
                if (client != null) CloseClient(client);

                _ConnectLock.Dispose();
                _Cts.Dispose();
            }
        }

        internal void Forget(ClutchLockHandle handle)
        {
            _Held.TryRemove(handle.HolderId, out _);
            if (_Disposed) return;
            try
            {
                LockLost?.Invoke(this, handle.Key);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "lock-lost handler failed: " + e.Message);
            }
        }

        internal async Task ReleaseAsync(ClutchLockHandle handle)
        {
            _Held.TryRemove(handle.HolderId, out _);
            if (_Disposed) return;

            // A closed connection has already released every lock it held.
            if (!ReferenceEquals(Volatile.Read(ref _Client), handle.Client)) return;

            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(_Settings.RequestTimeoutMs))
                {
                    await handle.Client.ReleaseAsync(handle.HolderId, timeout.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (e is ClutchException || e is OperationCanceledException || e is ObjectDisposedException)
            {
                _Logging.Warn(_Header + "release of " + handle.Key + " failed (" + e.Message + "); the lease will expire on its own");
            }
        }

        private async Task<AcquireOutcome> TryAcquireCoreAsync(string key, LockModeEnum mode, bool wait, int serverWaitMs, CancellationToken token)
        {
            ClutchLockClient client = await GetClientAsync(token).ConfigureAwait(false);

            AcquireOptions acquireOptions = new AcquireOptions
            {
                Behavior = wait ? LockBehavior.Wait : LockBehavior.FailFast,
                LeaseMs = _Settings.LeaseMs
            };
            if (wait) acquireOptions.TimeoutMs = Math.Max(1, serverWaitMs);

            AcquiredLock acquired;
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(_Settings.RequestTimeoutMs + (wait ? serverWaitMs : 0));
                try
                {
                    acquired = await client.AcquireAsync(_KeyPrefix + key, MapMode(mode), acquireOptions, timeout.Token).ConfigureAwait(false);
                }
                catch (LockDeniedException e)
                {
                    return new AcquireOutcome(null, e.Result.ToString());
                }
                catch (Exception e) when (e is ClutchException || e is ObjectDisposedException || (e is OperationCanceledException && !token.IsCancellationRequested))
                {
                    // A timed-out request may still be granted later; that hold is unknown to this process, so the SDK
                    // does not renew it and Clutch releases it when its lease expires.
                    throw new LockProviderUnavailableException(Name, "Clutch lock request for '" + key + "' failed: " + e.Message, e);
                }
            }

            ClutchLockHandle handle = new ClutchLockHandle(this, client, key, mode, acquired);
            _Held[handle.HolderId] = handle;

            // The connection may have closed between the grant and registering the handle; its locks are gone.
            if (!ReferenceEquals(Volatile.Read(ref _Client), client))
            {
                handle.MarkLost();
                throw new LockProviderUnavailableException(Name, "The Clutch connection closed while acquiring '" + key + "'.");
            }

            return new AcquireOutcome(handle, null);
        }

        private async Task<ClutchLockClient> GetClientAsync(CancellationToken token)
        {
            ClutchLockClient current = Volatile.Read(ref _Client);
            if (current != null) return current;

            await _ConnectLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                current = Volatile.Read(ref _Client);
                if (current != null) return current;
                ThrowIfDisposed();

                ClutchLockClient client = new ClutchLockClient(_Settings.Endpoint, _Settings.AccessKey);
                client.HeartbeatReceived += OnHeartbeatReceived;
                client.Closed += OnClosed;

                WelcomeInfo welcome;
                try
                {
                    using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        timeout.CancelAfter(_Settings.RequestTimeoutMs);
                        welcome = await client.ConnectAsync(timeout.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (e is ClutchException || (e is OperationCanceledException && !token.IsCancellationRequested))
                {
                    CloseClient(client);
                    throw new LockProviderUnavailableException(Name, "Unable to open a lock connection to Clutch at " + _Settings.Endpoint + ": " + e.Message, e);
                }

                if (welcome.HeartbeatIntervalMs * 2 > _Settings.LeaseMs)
                {
                    _Logging.Warn(
                        _Header + "Clutch heartbeat interval " + welcome.HeartbeatIntervalMs + "ms is more than half of Cluster.Clutch.LeaseMs "
                        + _Settings.LeaseMs + "ms; leases may expire between renewals");
                }

                Volatile.Write(ref _Client, client);
                _Logging.Info(_Header + "connected to Clutch at " + _Settings.Endpoint + " (tenant " + welcome.TenantId + ", session " + welcome.SessionId + ")");
                return client;
            }
            finally
            {
                _ConnectLock.Release();
            }
        }

        private void OnHeartbeatReceived(object sender, IReadOnlyList<AcquiredLock> renewed)
        {
            foreach (AcquiredLock acquired in renewed)
            {
                if (acquired != null && _Held.TryGetValue(acquired.HolderId, out ClutchLockHandle handle)) handle.MarkRenewed();
            }
        }

        private void OnClosed(object sender, string reason)
        {
            ClutchLockClient client = sender as ClutchLockClient;
            if (client == null) return;

            bool wasCurrent = ReferenceEquals(Interlocked.CompareExchange(ref _Client, null, client), client);
            List<ClutchLockHandle> lost = _Held.Values.Where(h => ReferenceEquals(h.Client, client)).ToList();

            if (wasCurrent && !_Disposed)
            {
                if (lost.Count > 0)
                    _Logging.Warn(_Header + "Clutch lock connection closed (" + reason + "); " + lost.Count + " held lock(s) lost");
                else
                    _Logging.Info(_Header + "Clutch lock connection closed (" + reason + "); reconnecting");
            }

            foreach (ClutchLockHandle handle in lost) handle.MarkLost();
            if (wasCurrent) Task.Run(() => CloseClient(client));
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_MonitorIntervalMs, token).ConfigureAwait(false);

                    DateTime cutoff = DateTime.UtcNow.AddMilliseconds(-_Settings.LeaseMs);
                    foreach (ClutchLockHandle handle in _Held.Values.ToList())
                    {
                        if (handle.LastRenewedUtc < cutoff)
                        {
                            _Logging.Warn(_Header + "lease for " + handle.Key + " was not renewed within " + _Settings.LeaseMs + "ms; treating the lock as lost");
                            handle.MarkLost();
                        }
                    }

                    if (Volatile.Read(ref _Client) == null && DateTime.UtcNow - _LastReconnectAttemptUtc >= TimeSpan.FromMilliseconds(_ReconnectIntervalMs))
                    {
                        _LastReconnectAttemptUtc = DateTime.UtcNow;
                        try
                        {
                            await GetClientAsync(token).ConfigureAwait(false);
                        }
                        catch (LockProviderUnavailableException e)
                        {
                            _Logging.Warn(_Header + "reconnect to Clutch failed: " + e.Message);
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "lock monitor error: " + e.Message);
                }
            }
        }

        private void CloseClient(ClutchLockClient client)
        {
            client.HeartbeatReceived -= OnHeartbeatReceived;
            client.Closed -= OnClosed;
            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(2000))
                {
                    client.CloseAsync(timeout.Token).GetAwaiter().GetResult();
                }
            }
            catch (Exception e) when (e is ClutchException || e is OperationCanceledException || e is ObjectDisposedException)
            {
            }
            finally
            {
                client.Dispose();
            }
        }

        private static LockMode MapMode(LockModeEnum mode)
        {
            switch (mode)
            {
                case LockModeEnum.Read:
                    return LockMode.Read;
                case LockModeEnum.Write:
                    return LockMode.Write;
                default:
                    return LockMode.Delete;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(ClutchLockProvider));
        }

        #endregion
    }
}
