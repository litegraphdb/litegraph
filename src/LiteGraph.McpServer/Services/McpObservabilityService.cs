namespace LiteGraph.McpServer.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.McpServer.Classes;

    /// <summary>
    /// MCP server metrics instrumentation. Emits the same metric names and label conventions as the REST
    /// server (distinguished by a <c>component="mcp"</c> label) plus <c>transport</c> and <c>tool</c> labels,
    /// and exposes a Prometheus scrape endpoint on its own port. Thread-safe.
    /// </summary>
    public class McpObservabilityService : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// OpenTelemetry-compatible meter.
        /// </summary>
        public Meter Meter { get; }

        #endregion

        #region Private-Members

        private const string _McpComponent = "mcp";

        private static readonly double[] _DurationBucketsMs = new double[]
        {
            5,
            10,
            25,
            50,
            100,
            250,
            500,
            1000,
            2500,
            5000,
            10000,
            30000
        };

        private readonly ObservabilitySettings _Settings;
        private readonly ConcurrentDictionary<string, ToolCallMetric> _ToolCallMetrics = new ConcurrentDictionary<string, ToolCallMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> _InFlight = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private readonly Counter<long> _RequestsCounter;
        private readonly Counter<long> _RequestErrorsCounter;
        private readonly Histogram<double> _RequestDurationMs;
        private readonly ObservableGauge<long> _RequestsInFlightGauge;
        private HttpListener? _Listener;
        private CancellationTokenSource? _ListenerTokenSource;
        private Task? _ListenerTask;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Observability settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
        public McpObservabilityService(ObservabilitySettings settings)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Meter = new Meter(_Settings.ServiceName);
            _RequestsCounter = Meter.CreateCounter<long>("litegraph.http.requests", "requests", "Total MCP tool calls processed by LiteGraph.");
            _RequestErrorsCounter = Meter.CreateCounter<long>("litegraph.http.request.errors", "errors", "Total MCP tool calls that resulted in an error.");
            _RequestDurationMs = Meter.CreateHistogram<double>("litegraph.http.request.duration", "ms", "MCP tool call duration in milliseconds.");
            _RequestsInFlightGauge = Meter.CreateObservableGauge<long>("litegraph.http.requests.in_flight", ObserveInFlight, "requests", "Currently in-flight MCP tool calls.");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the Prometheus scrape endpoint on the configured hostname and port.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the listener cannot be started.</exception>
        public void Start()
        {
            if (!_Settings.Enable || !_Settings.EnablePrometheus) return;
            if (_Listener != null) return;

            string prefix = "http://" + _Settings.Hostname + ":" + _Settings.Port.ToString(CultureInfo.InvariantCulture) + "/";
            _Listener = new HttpListener();
            _Listener.Prefixes.Add(prefix);

            try
            {
                _Listener.Start();
            }
            catch (HttpListenerException e)
            {
                throw new InvalidOperationException("Unable to start MCP metrics listener on '" + prefix + "'.", e);
            }

            _ListenerTokenSource = new CancellationTokenSource();
            _ListenerTask = Task.Run(() => ListenLoopAsync(_ListenerTokenSource.Token));
        }

        /// <summary>
        /// Record a completed MCP tool call.
        /// </summary>
        /// <param name="transport">Transport label (http, tcp, ws, stdio).</param>
        /// <param name="tool">Tool name.</param>
        /// <param name="isError">Whether the call resulted in an error.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordToolCall(string transport, string tool, bool isError, double durationMs)
        {
            if (!_Settings.Enable) return;

            transport = NormalizeLabel(transport);
            tool = NormalizeLabel(tool);
            string statusClass = isError ? "5xx" : "2xx";
            string key = transport + "\n" + tool + "\n" + statusClass;

            ToolCallMetric metric = _ToolCallMetrics.GetOrAdd(key, _ => new ToolCallMetric(transport, tool, statusClass));
            metric.Record(durationMs, isError);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object?>[] tags =
                {
                    new KeyValuePair<string, object?>("component", _McpComponent),
                    new KeyValuePair<string, object?>("transport", transport),
                    new KeyValuePair<string, object?>("tool", tool),
                    new KeyValuePair<string, object?>("status_class", statusClass)
                };

                _RequestsCounter.Add(1, tags);
                _RequestDurationMs.Record(durationMs, tags);
                if (isError) _RequestErrorsCounter.Add(1, tags);
            }
        }

        /// <summary>
        /// Increment the in-flight gauge for a transport. Pair every call with <see cref="DecrementInFlight"/>.
        /// </summary>
        /// <param name="transport">Transport label (http, tcp, ws, stdio).</param>
        public void IncrementInFlight(string transport)
        {
            if (!_Settings.Enable) return;
            transport = NormalizeLabel(transport);
            _InFlight.AddOrUpdate(transport, 1, (_, current) => current + 1);
        }

        /// <summary>
        /// Decrement the in-flight gauge for a transport.
        /// </summary>
        /// <param name="transport">Transport label (http, tcp, ws, stdio).</param>
        public void DecrementInFlight(string transport)
        {
            if (!_Settings.Enable) return;
            transport = NormalizeLabel(transport);
            _InFlight.AddOrUpdate(transport, 0, (_, current) => current > 0 ? current - 1 : 0);
        }

        /// <summary>
        /// Render metrics using the Prometheus text exposition format.
        /// </summary>
        /// <returns>Prometheus metrics text.</returns>
        public string RenderPrometheus()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# HELP litegraph_http_requests_total Total MCP tool calls processed by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_http_requests_total counter");
            foreach (ToolCallMetric metric in _ToolCallMetrics.Values)
            {
                sb.Append("litegraph_http_requests_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_http_request_duration_ms MCP tool call duration in milliseconds.");
            sb.AppendLine("# TYPE litegraph_http_request_duration_ms histogram");
            foreach (ToolCallMetric metric in _ToolCallMetrics.Values)
            {
                long[] bucketCounts = metric.GetBucketCounts();
                for (int i = 0; i < _DurationBucketsMs.Length; i++)
                {
                    sb.Append("litegraph_http_request_duration_ms_bucket");
                    AppendLabels(sb, metric, _DurationBucketsMs[i].ToString(CultureInfo.InvariantCulture));
                    sb.Append(' ');
                    sb.AppendLine(bucketCounts[i].ToString(CultureInfo.InvariantCulture));
                }

                sb.Append("litegraph_http_request_duration_ms_bucket");
                AppendLabels(sb, metric, "+Inf");
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_http_request_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.DurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_http_request_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_http_request_errors_total Total MCP tool calls that resulted in an error.");
            sb.AppendLine("# TYPE litegraph_http_request_errors_total counter");
            foreach (ToolCallMetric metric in _ToolCallMetrics.Values)
            {
                sb.Append("litegraph_http_request_errors_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.ErrorCount.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_http_requests_in_flight Currently in-flight MCP tool calls.");
            sb.AppendLine("# TYPE litegraph_http_requests_in_flight gauge");
            foreach (KeyValuePair<string, long> kvp in _InFlight)
            {
                sb.Append("litegraph_http_requests_in_flight{component=\"");
                sb.Append(EscapeLabel(_McpComponent));
                sb.Append("\",transport=\"");
                sb.Append(EscapeLabel(kvp.Key));
                sb.Append("\"} ");
                sb.AppendLine(kvp.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            try
            {
                _ListenerTokenSource?.Cancel();
                _Listener?.Stop();
                _Listener?.Close();
            }
            catch (ObjectDisposedException)
            {
            }

            Meter.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _Listener != null && _Listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await _Listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                try
                {
                    if (String.Equals(context.Request.Url?.AbsolutePath, _Settings.MetricsPath, StringComparison.OrdinalIgnoreCase)
                        && String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] payload = Encoding.UTF8.GetBytes(RenderPrometheus());
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "text/plain; version=0.0.4";
                        context.Response.ContentLength64 = payload.LongLength;
                        await context.Response.OutputStream.WriteAsync(payload, 0, payload.Length, token).ConfigureAwait(false);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        context.Response.StatusCode = 500;
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    try
                    {
                        context.Response.OutputStream.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private IEnumerable<Measurement<long>> ObserveInFlight()
        {
            foreach (KeyValuePair<string, long> kvp in _InFlight)
            {
                KeyValuePair<string, object?>[] tags =
                {
                    new KeyValuePair<string, object?>("component", _McpComponent),
                    new KeyValuePair<string, object?>("transport", kvp.Key)
                };

                yield return new Measurement<long>(kvp.Value, tags);
            }
        }

        private static string NormalizeLabel(string value)
        {
            if (String.IsNullOrEmpty(value)) return "unknown";
            return value.Trim();
        }

        private static string EscapeLabel(string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void AppendLabels(StringBuilder sb, ToolCallMetric metric)
        {
            sb.Append("{component=\"");
            sb.Append(EscapeLabel(_McpComponent));
            sb.Append("\",transport=\"");
            sb.Append(EscapeLabel(metric.Transport));
            sb.Append("\",tool=\"");
            sb.Append(EscapeLabel(metric.Tool));
            sb.Append("\",status_class=\"");
            sb.Append(EscapeLabel(metric.StatusClass));
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, ToolCallMetric metric, string le)
        {
            sb.Append("{component=\"");
            sb.Append(EscapeLabel(_McpComponent));
            sb.Append("\",transport=\"");
            sb.Append(EscapeLabel(metric.Transport));
            sb.Append("\",tool=\"");
            sb.Append(EscapeLabel(metric.Tool));
            sb.Append("\",status_class=\"");
            sb.Append(EscapeLabel(metric.StatusClass));
            sb.Append("\",le=\"");
            sb.Append(EscapeLabel(le));
            sb.Append("\"}");
        }

        #endregion

        #region Private-Classes

        private sealed class ToolCallMetric
        {
            private readonly object _Lock = new object();

            internal string Transport { get; }
            internal string Tool { get; }
            internal string StatusClass { get; }
            internal long Count { get; private set; }
            internal long ErrorCount { get; private set; }
            internal double DurationSumMs { get; private set; }
            private readonly long[] _BucketCounts = new long[_DurationBucketsMs.Length];

            internal ToolCallMetric(string transport, string tool, string statusClass)
            {
                Transport = transport;
                Tool = tool;
                StatusClass = statusClass;
            }

            internal void Record(double durationMs, bool isError)
            {
                if (durationMs < 0) durationMs = 0;

                lock (_Lock)
                {
                    Count++;
                    if (isError) ErrorCount++;
                    DurationSumMs += durationMs;
                    for (int i = 0; i < _DurationBucketsMs.Length; i++)
                    {
                        if (durationMs <= _DurationBucketsMs[i]) _BucketCounts[i]++;
                    }
                }
            }

            internal long[] GetBucketCounts()
            {
                lock (_Lock)
                {
                    long[] ret = new long[_BucketCounts.Length];
                    Array.Copy(_BucketCounts, ret, _BucketCounts.Length);
                    return ret;
                }
            }
        }

        #endregion
    }
}
