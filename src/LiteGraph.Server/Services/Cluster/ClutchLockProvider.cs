namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Coordination;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Distributed lock provider backed by Clutch, using Clutch's REST lock API.
    /// All locks held by this process share one Clutch lock session.  Leases are renewed in the background at a third of
    /// the lease duration; a lock whose lease cannot be renewed is marked lost and its LostToken is cancelled, so work done
    /// under it stops.  If the process dies, Clutch releases its locks when their leases expire.
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
        public bool IsAvailable { get { return Volatile.Read(ref _Available) == 1; } }

        /// <summary>
        /// UTC timestamp of the last successful exchange with Clutch, or null if none yet.
        /// </summary>
        public DateTime? LastContactUtc { get; private set; } = null;

        /// <summary>
        /// Number of locks currently held by this process.
        /// </summary>
        public int HeldCount { get { return _Held.Count; } }

        #endregion

        #region Private-Members

        private static readonly string _Header = "[ClutchLockProvider] ";
        private static readonly int _MaxServerWaitMs = 55000;
        private const int MaxTransientRetries = 10;

        private readonly ClutchSettings _Settings;
        private readonly string _KeyPrefix;
        private readonly LoggingModule _Logging;
        private readonly HttpClient _Http;
        private readonly ConcurrentDictionary<string, ClutchLockHandle> _Held = new ConcurrentDictionary<string, ClutchLockHandle>(StringComparer.Ordinal);
        private readonly SemaphoreSlim _AuthLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private readonly Task _HeartbeatTask;

        private string _Token = null;
        private string _TenantId = null;
        private string _SessionId = null;
        private int _Available = 0;
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

            _Http = new HttpClient();
            _Http.BaseAddress = new Uri(settings.Endpoint + "/");
            _Http.Timeout = Timeout.InfiniteTimeSpan;

            _HeartbeatTask = Task.Run(() => HeartbeatLoopAsync(_Cts.Token));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Authenticate with Clutch, retrying until it answers or the startup connect timeout elapses.
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
                    await AuthenticateAsync(token).ConfigureAwait(false);
                    _Logging.Info(_Header + "connected to Clutch at " + _Settings.Endpoint + " (tenant " + _TenantId + ")");
                    return;
                }
                catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
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
                catch (LockProviderUnavailableException e) when (options.Wait && DateTime.UtcNow < deadline && transientFailures < MaxTransientRetries)
                {
                    // Clutch occasionally answers 5xx under contention; retry with backoff while the caller is willing to wait.
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
        /// Dispose, releasing every lock held by this process.
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
                try
                {
                    ReleaseSessionAsync().GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "unable to release Clutch session during shutdown: " + e.Message);
                }

                try { _HeartbeatTask.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
                _Http.Dispose();
                _AuthLock.Dispose();
                _Cts.Dispose();
            }
        }

        internal void Forget(ClutchLockHandle handle)
        {
            _Held.TryRemove(handle.HolderId, out _);
        }

        internal async Task ReleaseAsync(ClutchLockHandle handle)
        {
            _Held.TryRemove(handle.HolderId, out _);
            if (_Disposed) return;

            try
            {
                string body = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "holderId", handle.HolderId },
                    { "sessionId", _SessionId }
                });

                using (HttpResponseMessage response = await SendAsync(
                    HttpMethod.Post,
                    "v1.0/api/tenants/" + _TenantId + "/locks/" + Uri.EscapeDataString(handle.QualifiedKey) + "/release",
                    body,
                    _Settings.RequestTimeoutMs,
                    CancellationToken.None).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        _Logging.Warn(_Header + "release of " + handle.Key + " returned " + (int)response.StatusCode + "; the lease will expire on its own");
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "release of " + handle.Key + " failed (" + e.Message + "); the lease will expire on its own");
            }
        }

        private async Task<AcquireOutcome> TryAcquireCoreAsync(string key, LockModeEnum mode, bool wait, int serverWaitMs, CancellationToken token)
        {
            if (_Token == null || _TenantId == null) await AuthenticateAsync(token).ConfigureAwait(false);
            string qualifiedKey = _KeyPrefix + key;

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "mode", MapMode(mode) },
                { "behavior", wait ? "Wait" : "FailFast" },
                { "leaseMs", _Settings.LeaseMs }
            };
            if (wait) payload["timeoutMs"] = Math.Max(1, serverWaitMs);
            if (_SessionId != null) payload["sessionId"] = _SessionId;

            int requestTimeoutMs = _Settings.RequestTimeoutMs + (wait ? serverWaitMs : 0);

            using (HttpResponseMessage response = await SendAsync(
                HttpMethod.Post,
                "v1.0/api/tenants/" + _TenantId + "/locks/" + Uri.EscapeDataString(qualifiedKey) + "/acquire",
                JsonSerializer.Serialize(payload),
                requestTimeoutMs,
                token).ConfigureAwait(false))
            {
                string json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
                {
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement root = doc.RootElement;
                        string holderId = root.GetProperty("holderId").GetString();
                        string sessionId = root.TryGetProperty("sessionId", out JsonElement s) ? s.GetString() : null;
                        long fencing = root.TryGetProperty("fencingToken", out JsonElement f) && f.ValueKind == JsonValueKind.Number ? f.GetInt64() : 0;

                        if (_SessionId == null && !String.IsNullOrEmpty(sessionId)) _SessionId = sessionId;

                        ClutchLockHandle handle = new ClutchLockHandle(this, key, qualifiedKey, mode, holderId, fencing);
                        _Held[holderId] = handle;
                        return new AcquireOutcome(handle, null);
                    }
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    string reason = "Denied";
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("result", out JsonElement r)) reason = r.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                    }
                    return new AcquireOutcome(null, reason);
                }

                throw new LockProviderUnavailableException(Name, "Clutch returned " + (int)response.StatusCode + " acquiring '" + key + "': " + Truncate(json));
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Math.Max(1000, _Settings.LeaseMs / 3), token).ConfigureAwait(false);
                    if (_Held.IsEmpty || _SessionId == null) continue;

                    List<ClutchLockHandle> held = _Held.Values.ToList();
                    string body = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        { "holderIds", held.Select(h => h.HolderId).ToList() }
                    });

                    HashSet<string> renewed = new HashSet<string>(StringComparer.Ordinal);
                    using (HttpResponseMessage response = await SendAsync(
                        HttpMethod.Post,
                        "v1.0/api/tenants/" + _TenantId + "/lock-sessions/" + _SessionId + "/heartbeat",
                        body,
                        _Settings.RequestTimeoutMs,
                        token).ConfigureAwait(false))
                    {
                        string json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                            throw new HttpRequestException("heartbeat returned " + (int)response.StatusCode + ": " + Truncate(json));

                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("renewed", out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement item in arr.EnumerateArray())
                                {
                                    if (item.TryGetProperty("holderId", out JsonElement id)) renewed.Add(id.GetString());
                                }
                            }
                        }
                    }

                    foreach (ClutchLockHandle handle in held)
                    {
                        if (!renewed.Contains(handle.HolderId) && handle.IsHeld)
                        {
                            _Logging.Warn(_Header + "lease for " + handle.Key + " was not renewed; treating the lock as lost");
                            handle.MarkLost();
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "lease renewal failed: " + e.Message);
                    DateTime cutoff = DateTime.UtcNow.AddMilliseconds(-_Settings.LeaseMs);
                    if (LastContactUtc == null || LastContactUtc.Value < cutoff)
                    {
                        foreach (ClutchLockHandle handle in _Held.Values.ToList())
                        {
                            _Logging.Warn(_Header + "no contact with Clutch for a full lease; treating " + handle.Key + " as lost");
                            handle.MarkLost();
                        }
                    }
                }
            }
        }

        private async Task ReleaseSessionAsync()
        {
            if (_SessionId == null || _TenantId == null) return;

            using (HttpResponseMessage response = await SendAsync(
                HttpMethod.Post,
                "v1.0/api/tenants/" + _TenantId + "/lock-sessions/" + _SessionId + "/release",
                "{}",
                _Settings.RequestTimeoutMs,
                CancellationToken.None).ConfigureAwait(false))
            {
                _Held.Clear();
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string body, int timeoutMs, CancellationToken token)
        {
            if (_Token == null) await AuthenticateAsync(token).ConfigureAwait(false);

            HttpResponseMessage response = await SendOnceAsync(method, path, body, timeoutMs, token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                await AuthenticateAsync(token).ConfigureAwait(false);
                response = await SendOnceAsync(method, path, body, timeoutMs, token).ConfigureAwait(false);
            }

            return response;
        }

        private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, string body, int timeoutMs, CancellationToken token)
        {
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(timeoutMs);
                using (HttpRequestMessage request = new HttpRequestMessage(method, path))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _Token);
                    if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    try
                    {
                        HttpResponseMessage response = await _Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
                        MarkContact(true);
                        return response;
                    }
                    catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
                    {
                        MarkContact(false);
                        throw new LockProviderUnavailableException(Name, "Clutch request to " + path + " failed: " + e.Message, e);
                    }
                }
            }
        }

        private async Task AuthenticateAsync(CancellationToken token)
        {
            await _AuthLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                string body = JsonSerializer.Serialize(new Dictionary<string, object> { { "accessKey", _Settings.AccessKey } });
                using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeout.CancelAfter(_Settings.RequestTimeoutMs);
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "v1.0/token"))
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                        HttpResponseMessage response;
                        try
                        {
                            response = await _Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
                        }
                        catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
                        {
                            MarkContact(false);
                            throw new LockProviderUnavailableException(Name, "Unable to authenticate with Clutch at " + _Settings.Endpoint + ": " + e.Message, e);
                        }

                        using (response)
                        {
                            string json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                            if (!response.IsSuccessStatusCode)
                            {
                                MarkContact(false);
                                throw new LockProviderUnavailableException(Name, "Clutch rejected the configured access key (" + (int)response.StatusCode + ").");
                            }

                            using (JsonDocument doc = JsonDocument.Parse(json))
                            {
                                _Token = doc.RootElement.GetProperty("token").GetString();
                                _TenantId = doc.RootElement.GetProperty("tenantId").GetString();
                            }

                            MarkContact(true);
                        }
                    }
                }
            }
            finally
            {
                _AuthLock.Release();
            }
        }

        private void MarkContact(bool success)
        {
            if (success)
            {
                LastContactUtc = DateTime.UtcNow;
                Interlocked.Exchange(ref _Available, 1);
            }
            else
            {
                Interlocked.Exchange(ref _Available, 0);
            }
        }

        private static string MapMode(LockModeEnum mode)
        {
            switch (mode)
            {
                case LockModeEnum.Read:
                    return "Read";
                case LockModeEnum.Write:
                    return "Write";
                default:
                    return "Delete";
            }
        }

        private static string Truncate(string value)
        {
            if (String.IsNullOrEmpty(value)) return value;
            return value.Length <= 256 ? value : value.Substring(0, 256) + "...";
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(ClutchLockProvider));
        }

        #endregion
    }
}
