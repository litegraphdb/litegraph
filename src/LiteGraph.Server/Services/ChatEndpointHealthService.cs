namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Background health monitoring for chat endpoints.
    /// Runs one probe loop per monitored endpoint; state is in-memory only and resets on restart.
    /// Thread safety: all public members are safe for concurrent use.
    /// </summary>
    public class ChatEndpointHealthService : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Header = "[ChatEndpointHealthService] ";
        private readonly LoggingModule _Logging;
        private readonly LiteGraphClient _LiteGraph;
        private readonly ObservabilityService _Observability;
        private readonly HttpClient _HttpClient;
        private readonly ConcurrentDictionary<Guid, MonitorState> _Monitors = new ConcurrentDictionary<Guid, MonitorState>();
        private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private readonly TimeSpan _HistoryWindow = TimeSpan.FromHours(24);
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="liteGraph">LiteGraph client.</param>
        /// <param name="observability">Observability service.</param>
        /// <param name="httpClient">Optional HTTP client; when null an internally owned client is created.</param>
        public ChatEndpointHealthService(
            LoggingModule logging,
            LiteGraphClient liteGraph,
            ObservabilityService observability,
            HttpClient httpClient = null)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = liteGraph ?? throw new ArgumentNullException(nameof(liteGraph));
            _Observability = observability;
            _HttpClient = httpClient ?? new HttpClient(new HttpClientHandler(), disposeHandler: true);
            _HttpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Enumerate all endpoints across tenants and start monitoring the eligible ones.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task Start(CancellationToken token = default)
        {
            await foreach (TenantMetadata tenant in _LiteGraph.Tenant.ReadMany(EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false))
            {
                await foreach (ChatEndpoint endpoint in _LiteGraph.ChatEndpoint.ReadAllInTenant(tenant.GUID, null, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false))
                {
                    OnEndpointCreatedOrUpdated(endpoint);
                }
            }

            _Logging.Info(_Header + "started; monitoring " + _Monitors.Count + " chat endpoint(s)");
        }

        /// <summary>
        /// React to a created or updated endpoint: start, restart, or stop its monitor as its configuration dictates.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        public void OnEndpointCreatedOrUpdated(ChatEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            OnEndpointDeleted(endpoint.TenantGUID, endpoint.GUID, endpoint.Name, endpoint.EndpointType);

            if (!endpoint.Active || !endpoint.HealthCheckEnabled) return;

            MonitorState state = new MonitorState(endpoint);
            if (_Monitors.TryAdd(endpoint.GUID, state))
            {
                state.LoopTask = Task.Run(() => ProbeLoop(state), _TokenSource.Token);
            }
        }

        /// <summary>
        /// React to a deleted endpoint: stop its monitor and remove its state.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Endpoint GUID.</param>
        /// <param name="endpointName">Endpoint name, for metric series removal.  Null skips metric removal.</param>
        /// <param name="endpointType">Endpoint type.</param>
        public void OnEndpointDeleted(Guid tenantGuid, Guid endpointGuid, string endpointName = null, ChatEndpointTypeEnum endpointType = ChatEndpointTypeEnum.Completion)
        {
            if (_Monitors.TryRemove(endpointGuid, out MonitorState state))
            {
                state.Cancel();
                if (!String.IsNullOrEmpty(endpointName)) _Observability?.SetChatEndpointHealth(endpointName, endpointType, null);
            }
        }

        /// <summary>
        /// Get health status for a single endpoint.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <returns>Health status.  Endpoints without a monitor report Monitored = false.</returns>
        public ChatEndpointHealth GetHealth(ChatEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            if (_Monitors.TryGetValue(endpoint.GUID, out MonitorState state))
            {
                return state.ToHealth();
            }

            return new ChatEndpointHealth
            {
                EndpointGUID = endpoint.GUID,
                TenantGUID = endpoint.TenantGUID,
                Name = endpoint.Name,
                EndpointType = endpoint.EndpointType,
                Monitored = false
            };
        }

        /// <summary>
        /// Get health status for all monitored endpoints in a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <returns>Health statuses.</returns>
        public List<ChatEndpointHealth> GetTenantHealth(Guid tenantGuid)
        {
            return _Monitors.Values
                .Where(m => m.Endpoint.TenantGUID.Equals(tenantGuid))
                .Select(m => m.ToHealth())
                .ToList();
        }

        /// <summary>
        /// Dispose.
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

            if (disposing)
            {
                _TokenSource.Cancel();
                foreach (MonitorState state in _Monitors.Values) state.Cancel();
                _Monitors.Clear();
                _TokenSource.Dispose();
                _HttpClient.Dispose();
            }

            _Disposed = true;
        }

        private async Task ProbeLoop(MonitorState state)
        {
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(_TokenSource.Token, state.Token).Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(state.Endpoint.HealthCheckIntervalMs, token).ConfigureAwait(false);
                    await ProbeOnce(state, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "probe loop error for endpoint " + state.Endpoint.GUID + ": " + e.Message);
                }
            }
        }

        private async Task ProbeOnce(MonitorState state, CancellationToken token)
        {
            ChatEndpoint endpoint = state.Endpoint;
            string url = (!String.IsNullOrEmpty(endpoint.HealthCheckUrl) ? endpoint.HealthCheckUrl : endpoint.Endpoint);
            Stopwatch sw = Stopwatch.StartNew();
            bool success = false;
            string error = null;

            try
            {
                using (CancellationTokenSource probeCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    probeCts.CancelAfter(endpoint.HealthCheckTimeoutMs);

                    using (HttpRequestMessage request = new HttpRequestMessage(
                        (String.Equals(endpoint.HealthCheckMethod, "HEAD", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Head : HttpMethod.Get),
                        url))
                    {
                        if (endpoint.HealthCheckUseAuth && !String.IsNullOrEmpty(endpoint.ApiKey)) AddAuthHeader(request, endpoint);

                        using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, probeCts.Token).ConfigureAwait(false))
                        {
                            success = ((int)response.StatusCode == endpoint.HealthCheckExpectedStatusCode);
                            if (!success) error = "Unexpected status " + (int)response.StatusCode + " (expected " + endpoint.HealthCheckExpectedStatusCode + ").";
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                error = "Probe timed out after " + endpoint.HealthCheckTimeoutMs + "ms.";
            }
            catch (HttpRequestException hre)
            {
                error = hre.Message;
            }

            sw.Stop();
            state.RecordProbe(success, error, sw.Elapsed.TotalMilliseconds, _HistoryWindow, out bool? transitionedTo);

            _Observability?.RecordChatHealthProbe(endpoint.Name, endpoint.EndpointType, success, sw.Elapsed.TotalMilliseconds);

            if (transitionedTo != null)
            {
                _Observability?.RecordChatHealthTransition(endpoint.Name, transitionedTo.Value);
                _Logging.Info(_Header + "endpoint " + endpoint.Name + " (" + endpoint.GUID + ") transitioned to " + (transitionedTo.Value ? "healthy" : "unhealthy"));
            }

            _Observability?.SetChatEndpointHealth(endpoint.Name, endpoint.EndpointType, state.Healthy);
        }

        private static void AddAuthHeader(HttpRequestMessage request, ChatEndpoint endpoint)
        {
            switch (endpoint.Provider)
            {
                case ChatProviderTypeEnum.Anthropic:
                    request.Headers.TryAddWithoutValidation("x-api-key", endpoint.ApiKey);
                    break;
                case ChatProviderTypeEnum.Gemini:
                    request.Headers.TryAddWithoutValidation("x-goog-api-key", endpoint.ApiKey);
                    break;
                default:
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + endpoint.ApiKey);
                    break;
            }
        }

        #endregion

        #region Private-Classes

        private sealed class MonitorState
        {
            internal readonly ChatEndpoint Endpoint;
            internal Task LoopTask = null;
            internal bool? Healthy = null;
            internal DateTime? LastCheckedUtc = null;
            internal string LastError = null;
            internal int ConsecutiveSuccesses = 0;
            internal int ConsecutiveFailures = 0;

            private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
            private readonly List<ChatEndpointHealthSample> _History = new List<ChatEndpointHealthSample>();
            private readonly object _Lock = new object();

            internal MonitorState(ChatEndpoint endpoint)
            {
                Endpoint = endpoint;
            }

            internal CancellationToken Token
            {
                get
                {
                    return _Cts.Token;
                }
            }

            internal void Cancel()
            {
                try
                {
                    _Cts.Cancel();
                    _Cts.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            internal void RecordProbe(bool success, string error, double durationMs, TimeSpan historyWindow, out bool? transitionedTo)
            {
                lock (_Lock)
                {
                    LastCheckedUtc = DateTime.UtcNow;
                    LastError = (success ? null : error);

                    if (success)
                    {
                        ConsecutiveSuccesses++;
                        ConsecutiveFailures = 0;
                    }
                    else
                    {
                        ConsecutiveFailures++;
                        ConsecutiveSuccesses = 0;
                    }

                    transitionedTo = null;

                    if (success && Healthy != true && ConsecutiveSuccesses >= Endpoint.HealthyThreshold)
                    {
                        Healthy = true;
                        transitionedTo = true;
                    }
                    else if (!success && Healthy != false && ConsecutiveFailures >= Endpoint.UnhealthyThreshold)
                    {
                        Healthy = false;
                        transitionedTo = false;
                    }

                    _History.Add(new ChatEndpointHealthSample
                    {
                        TimestampUtc = LastCheckedUtc.Value,
                        Success = success,
                        DurationMs = durationMs
                    });

                    DateTime cutoff = DateTime.UtcNow.Subtract(historyWindow);
                    _History.RemoveAll(s => s.TimestampUtc < cutoff);
                }
            }

            internal ChatEndpointHealth ToHealth()
            {
                lock (_Lock)
                {
                    List<ChatEndpointHealthSample> history = new List<ChatEndpointHealthSample>(_History);
                    double? uptime = null;
                    if (history.Count > 0) uptime = (100.0 * history.Count(s => s.Success)) / history.Count;

                    return new ChatEndpointHealth
                    {
                        EndpointGUID = Endpoint.GUID,
                        TenantGUID = Endpoint.TenantGUID,
                        Name = Endpoint.Name,
                        EndpointType = Endpoint.EndpointType,
                        Monitored = true,
                        Healthy = Healthy,
                        LastCheckedUtc = LastCheckedUtc,
                        LastError = LastError,
                        ConsecutiveSuccesses = ConsecutiveSuccesses,
                        ConsecutiveFailures = ConsecutiveFailures,
                        UptimePercentage = uptime,
                        CheckHistory = history
                    };
                }
            }
        }

        #endregion
    }
}
