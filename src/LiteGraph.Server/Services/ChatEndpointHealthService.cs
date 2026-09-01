namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Background health monitoring for chat endpoints.
    /// Probes are deduplicated by target: endpoints sharing the same probe URL, method, expected
    /// status, and authentication material share a single probe loop, and every subscriber reports
    /// the shared verdict.  Five models on one Ollama host produce one probe, not five.
    /// State is in-memory only and resets on restart.
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
        private readonly ConcurrentDictionary<string, TargetMonitor> _Targets = new ConcurrentDictionary<string, TargetMonitor>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<Guid, string> _EndpointTargets = new ConcurrentDictionary<Guid, string>();
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

            _Logging.Info(_Header + "started; monitoring " + _Targets.Count + " probe target(s) for " + _EndpointTargets.Count + " chat endpoint(s)");
        }

        /// <summary>
        /// React to a created or updated endpoint: subscribe it to its probe target, starting a
        /// probe loop only when the target is new.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        public void OnEndpointCreatedOrUpdated(ChatEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            Unsubscribe(endpoint.GUID, endpoint.Name, endpoint.EndpointType);

            if (!endpoint.Active || !endpoint.HealthCheckEnabled) return;

            string key = ProbeKey(endpoint);
            _EndpointTargets[endpoint.GUID] = key;

            TargetMonitor target = _Targets.GetOrAdd(key, _ => new TargetMonitor(key));
            bool startLoop = target.AddSubscriber(endpoint);

            if (startLoop)
            {
                target.LoopTask = Task.Run(() => ProbeLoop(target), _TokenSource.Token);
            }
        }

        /// <summary>
        /// React to a deleted endpoint: unsubscribe it, stopping the probe loop when it was the
        /// target's last subscriber.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Endpoint GUID.</param>
        /// <param name="endpointName">Endpoint name, for metric series removal.  Null skips metric removal.</param>
        /// <param name="endpointType">Endpoint type.</param>
        public void OnEndpointDeleted(Guid tenantGuid, Guid endpointGuid, string endpointName = null, ChatEndpointTypeEnum endpointType = ChatEndpointTypeEnum.Completion)
        {
            Unsubscribe(endpointGuid, endpointName, endpointType);
        }

        /// <summary>
        /// Get health status for a single endpoint.  Endpoints sharing a probe target report the shared verdict.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <returns>Health status.  Endpoints without a monitor report Monitored = false.</returns>
        public ChatEndpointHealth GetHealth(ChatEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            if (_EndpointTargets.TryGetValue(endpoint.GUID, out string key)
                && _Targets.TryGetValue(key, out TargetMonitor target))
            {
                return target.ToHealth(endpoint, _HistoryWindow);
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
        /// <returns>Health statuses, one per subscribed endpoint.</returns>
        public List<ChatEndpointHealth> GetTenantHealth(Guid tenantGuid)
        {
            List<ChatEndpointHealth> ret = new List<ChatEndpointHealth>();

            foreach (TargetMonitor target in _Targets.Values)
            {
                foreach (ChatEndpoint subscriber in target.Subscribers())
                {
                    if (subscriber.TenantGUID.Equals(tenantGuid)) ret.Add(target.ToHealth(subscriber, _HistoryWindow));
                }
            }

            return ret;
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
                foreach (TargetMonitor target in _Targets.Values) target.Cancel();
                _Targets.Clear();
                _EndpointTargets.Clear();
                _TokenSource.Dispose();
                _HttpClient.Dispose();
            }

            _Disposed = true;
        }

        private void Unsubscribe(Guid endpointGuid, string endpointName, ChatEndpointTypeEnum endpointType)
        {
            if (_EndpointTargets.TryRemove(endpointGuid, out string key)
                && _Targets.TryGetValue(key, out TargetMonitor target))
            {
                bool empty = target.RemoveSubscriber(endpointGuid);
                if (!String.IsNullOrEmpty(endpointName)) _Observability?.SetChatEndpointHealth(endpointName, endpointType, null);

                if (empty && _Targets.TryRemove(key, out TargetMonitor removed))
                {
                    removed.Cancel();
                }
            }
        }

        private static string ProbeKey(ChatEndpoint endpoint)
        {
            string url = (!String.IsNullOrEmpty(endpoint.HealthCheckUrl) ? endpoint.HealthCheckUrl : endpoint.Endpoint) ?? String.Empty;
            string method = endpoint.HealthCheckMethod ?? "GET";
            string auth = "none";

            if (endpoint.HealthCheckUseAuth && !String.IsNullOrEmpty(endpoint.ApiKey))
            {
                using (SHA256 sha = SHA256.Create())
                {
                    auth = endpoint.Provider.ToString() + ":" + Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(endpoint.ApiKey)));
                }
            }

            return method.ToUpperInvariant() + "|" + url.TrimEnd('/').ToLowerInvariant() + "|" + endpoint.HealthCheckExpectedStatusCode + "|" + auth;
        }

        private async Task ProbeLoop(TargetMonitor target)
        {
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(_TokenSource.Token, target.Token).Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(target.IntervalMs, token).ConfigureAwait(false);
                    await ProbeOnce(target, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "probe loop error for target " + target.Key + ": " + e.Message);
                }
            }
        }

        private async Task ProbeOnce(TargetMonitor target, CancellationToken token)
        {
            ChatEndpoint representative = target.Representative();
            if (representative == null) return;

            string url = (!String.IsNullOrEmpty(representative.HealthCheckUrl) ? representative.HealthCheckUrl : representative.Endpoint);
            Stopwatch sw = Stopwatch.StartNew();
            bool success = false;
            string error = null;

            try
            {
                using (CancellationTokenSource probeCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    probeCts.CancelAfter(representative.HealthCheckTimeoutMs);

                    using (HttpRequestMessage request = new HttpRequestMessage(
                        (String.Equals(representative.HealthCheckMethod, "HEAD", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Head : HttpMethod.Get),
                        url))
                    {
                        if (representative.HealthCheckUseAuth && !String.IsNullOrEmpty(representative.ApiKey)) AddAuthHeader(request, representative);

                        using (HttpResponseMessage response = await _HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, probeCts.Token).ConfigureAwait(false))
                        {
                            success = ((int)response.StatusCode == representative.HealthCheckExpectedStatusCode);
                            if (!success) error = "Unexpected status " + (int)response.StatusCode + " (expected " + representative.HealthCheckExpectedStatusCode + ").";
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
                error = "Probe timed out after " + representative.HealthCheckTimeoutMs + "ms.";
            }
            catch (HttpRequestException hre)
            {
                error = hre.Message;
            }

            sw.Stop();
            target.RecordProbe(success, error, sw.Elapsed.TotalMilliseconds, _HistoryWindow, out bool? transitionedTo);

            _Observability?.RecordChatHealthProbe(target.MetricLabel(), representative.EndpointType, success, sw.Elapsed.TotalMilliseconds);

            foreach (ChatEndpoint subscriber in target.Subscribers())
            {
                if (transitionedTo != null)
                {
                    _Observability?.RecordChatHealthTransition(subscriber.Name, transitionedTo.Value);
                }

                _Observability?.SetChatEndpointHealth(subscriber.Name, subscriber.EndpointType, target.Healthy);
            }

            if (transitionedTo != null)
            {
                _Logging.Info(_Header + "target " + target.MetricLabel() + " transitioned to " + (transitionedTo.Value ? "healthy" : "unhealthy") + " (" + target.SubscriberCount + " endpoint(s))");
            }
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

        private sealed class TargetMonitor
        {
            internal readonly string Key;
            internal Task LoopTask = null;
            internal bool? Healthy = null;

            private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
            private readonly Dictionary<Guid, ChatEndpoint> _Subscribers = new Dictionary<Guid, ChatEndpoint>();
            private readonly List<ChatEndpointHealthSample> _History = new List<ChatEndpointHealthSample>();
            private readonly object _Lock = new object();
            private DateTime? _LastCheckedUtc = null;
            private string _LastError = null;
            private int _ConsecutiveSuccesses = 0;
            private int _ConsecutiveFailures = 0;

            internal TargetMonitor(string key)
            {
                Key = key;
            }

            internal CancellationToken Token
            {
                get
                {
                    return _Cts.Token;
                }
            }

            internal int IntervalMs
            {
                get
                {
                    lock (_Lock)
                    {
                        // The shared loop honors the most aggressive interval among subscribers.
                        return (_Subscribers.Count > 0 ? _Subscribers.Values.Min(s => s.HealthCheckIntervalMs) : 30000);
                    }
                }
            }

            internal int SubscriberCount
            {
                get
                {
                    lock (_Lock) return _Subscribers.Count;
                }
            }

            internal bool AddSubscriber(ChatEndpoint endpoint)
            {
                lock (_Lock)
                {
                    bool first = (_Subscribers.Count == 0);
                    _Subscribers[endpoint.GUID] = endpoint;
                    return first;
                }
            }

            internal bool RemoveSubscriber(Guid endpointGuid)
            {
                lock (_Lock)
                {
                    _Subscribers.Remove(endpointGuid);
                    return (_Subscribers.Count == 0);
                }
            }

            internal List<ChatEndpoint> Subscribers()
            {
                lock (_Lock) return _Subscribers.Values.ToList();
            }

            internal ChatEndpoint Representative()
            {
                lock (_Lock) return _Subscribers.Values.FirstOrDefault();
            }

            internal string MetricLabel()
            {
                ChatEndpoint representative = Representative();
                if (representative == null) return Key;

                string url = (!String.IsNullOrEmpty(representative.HealthCheckUrl) ? representative.HealthCheckUrl : representative.Endpoint);
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri parsed)) return parsed.Authority;
                return Key;
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
                    _LastCheckedUtc = DateTime.UtcNow;
                    _LastError = (success ? null : error);

                    if (success)
                    {
                        _ConsecutiveSuccesses++;
                        _ConsecutiveFailures = 0;
                    }
                    else
                    {
                        _ConsecutiveFailures++;
                        _ConsecutiveSuccesses = 0;
                    }

                    transitionedTo = null;

                    // Verdict thresholds: the fastest-converging (lowest) among subscribers, so a
                    // shared target never waits on the most conservative endpoint configuration.
                    int healthyThreshold = (_Subscribers.Count > 0 ? _Subscribers.Values.Min(s => s.HealthyThreshold) : 2);
                    int unhealthyThreshold = (_Subscribers.Count > 0 ? _Subscribers.Values.Min(s => s.UnhealthyThreshold) : 2);

                    if (success && Healthy != true && _ConsecutiveSuccesses >= healthyThreshold)
                    {
                        Healthy = true;
                        transitionedTo = true;
                    }
                    else if (!success && Healthy != false && _ConsecutiveFailures >= unhealthyThreshold)
                    {
                        Healthy = false;
                        transitionedTo = false;
                    }

                    _History.Add(new ChatEndpointHealthSample
                    {
                        TimestampUtc = _LastCheckedUtc.Value,
                        Success = success,
                        DurationMs = durationMs
                    });

                    DateTime cutoff = DateTime.UtcNow.Subtract(historyWindow);
                    _History.RemoveAll(s => s.TimestampUtc < cutoff);
                }
            }

            internal ChatEndpointHealth ToHealth(ChatEndpoint endpoint, TimeSpan historyWindow)
            {
                lock (_Lock)
                {
                    List<ChatEndpointHealthSample> history = new List<ChatEndpointHealthSample>(_History);
                    double? uptime = null;
                    if (history.Count > 0) uptime = (100.0 * history.Count(s => s.Success)) / history.Count;

                    return new ChatEndpointHealth
                    {
                        EndpointGUID = endpoint.GUID,
                        TenantGUID = endpoint.TenantGUID,
                        Name = endpoint.Name,
                        EndpointType = endpoint.EndpointType,
                        Monitored = true,
                        Healthy = Healthy,
                        LastCheckedUtc = _LastCheckedUtc,
                        LastError = _LastError,
                        ConsecutiveSuccesses = _ConsecutiveSuccesses,
                        ConsecutiveFailures = _ConsecutiveFailures,
                        UptimePercentage = uptime,
                        CheckHistory = history
                    };
                }
            }
        }

        #endregion
    }
}
