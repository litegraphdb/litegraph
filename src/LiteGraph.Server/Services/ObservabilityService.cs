namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using LiteGraph.Server.Classes;
    using OpenTelemetry;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Central request metrics and trace instrumentation.
    /// </summary>
    public class ObservabilityService : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// OpenTelemetry-compatible activity source.
        /// </summary>
        public ActivitySource ActivitySource { get; }

        /// <summary>
        /// OpenTelemetry-compatible meter.
        /// </summary>
        public Meter Meter { get; }

        #endregion

        #region Private-Members

        private readonly ObservabilitySettings _Settings;
        private static readonly double[] _HttpRequestDurationBucketsMs = new double[]
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

        private readonly ConcurrentDictionary<string, RequestMetric> _RequestMetrics = new ConcurrentDictionary<string, RequestMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, GraphQueryMetric> _GraphQueryMetrics = new ConcurrentDictionary<string, GraphQueryMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, VectorSearchMetric> _VectorSearchMetrics = new ConcurrentDictionary<string, VectorSearchMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, GraphTransactionMetric> _GraphTransactionMetrics = new ConcurrentDictionary<string, GraphTransactionMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, AuthenticationMetric> _AuthenticationMetrics = new ConcurrentDictionary<string, AuthenticationMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, RepositoryOperationMetric> _RepositoryOperationMetrics = new ConcurrentDictionary<string, RepositoryOperationMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, StorageBackendMetric> _StorageBackendMetrics = new ConcurrentDictionary<string, StorageBackendMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, StorageConnectionPoolMetric> _StorageConnectionPoolMetrics = new ConcurrentDictionary<string, StorageConnectionPoolMetric>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, EntityCountMetric> _EntityCountMetrics = new ConcurrentDictionary<string, EntityCountMetric>(StringComparer.Ordinal);
        private readonly Counter<long> _HttpRequestsCounter;
        private readonly Histogram<double> _HttpRequestDurationMs;
        private readonly Counter<long> _GraphQueryCounter;
        private readonly Histogram<double> _GraphQueryDurationMs;
        private readonly Counter<long> _VectorSearchCounter;
        private readonly Counter<long> _VectorSearchResultCounter;
        private readonly Histogram<double> _VectorSearchDurationMs;
        private readonly Counter<long> _GraphTransactionCounter;
        private readonly Histogram<double> _GraphTransactionDurationMs;
        private readonly Histogram<double> _GraphTransactionQueueWaitDurationMs;
        private readonly Histogram<double> _GraphTransactionCommitDurationMs;
        private readonly Histogram<double> _GraphTransactionRollbackDurationMs;
        private readonly Counter<long> _GraphTransactionConflictCounter;
        private readonly Counter<long> _GraphTransactionRetryCounter;
        private readonly Counter<long> _AuthenticationCounter;
        private readonly Counter<long> _RepositoryOperationCounter;
        private readonly Histogram<double> _RepositoryOperationDurationMs;
        private readonly ObservableGauge<long> _GraphTransactionActiveGauge;
        private readonly ObservableGauge<long> _StorageBackendGauge;
        private readonly ObservableGauge<long> _StorageConnectionPoolMaxGauge;
        private readonly ObservableGauge<long> _StorageCommandTimeoutGauge;
        private readonly ObservableGauge<long> _EntityCountGauge;
        private TracerProvider _TracerProvider;
        private MeterProvider _MeterProvider;
        private long _ActiveGraphTransactions = 0;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Observability settings.</param>
        public ObservabilityService(ObservabilitySettings settings)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ActivitySource = new ActivitySource(_Settings.ServiceName);
            Meter = new Meter(_Settings.ServiceName);
            _HttpRequestsCounter = Meter.CreateCounter<long>("litegraph.http.requests", "requests", "Total HTTP requests processed by LiteGraph.");
            _HttpRequestDurationMs = Meter.CreateHistogram<double>("litegraph.http.request.duration", "ms", "HTTP request duration in milliseconds.");
            _GraphQueryCounter = Meter.CreateCounter<long>("litegraph.graph.queries", "queries", "Total native graph queries processed by LiteGraph.");
            _GraphQueryDurationMs = Meter.CreateHistogram<double>("litegraph.graph.query.duration", "ms", "Native graph query duration in milliseconds.");
            _VectorSearchCounter = Meter.CreateCounter<long>("litegraph.vector.searches", "searches", "Total vector searches processed by LiteGraph.");
            _VectorSearchResultCounter = Meter.CreateCounter<long>("litegraph.vector.search.results", "results", "Total vector search results returned by LiteGraph.");
            _VectorSearchDurationMs = Meter.CreateHistogram<double>("litegraph.vector.search.duration", "ms", "Vector search duration in milliseconds.");
            _GraphTransactionCounter = Meter.CreateCounter<long>("litegraph.graph.transactions", "transactions", "Total graph transactions processed by LiteGraph.");
            _GraphTransactionDurationMs = Meter.CreateHistogram<double>("litegraph.graph.transaction.duration", "ms", "Graph transaction duration in milliseconds.");
            _GraphTransactionQueueWaitDurationMs = Meter.CreateHistogram<double>("litegraph.graph.transaction.queue_wait.duration", "ms", "Graph transaction serialized fallback queue wait duration in milliseconds.");
            _GraphTransactionCommitDurationMs = Meter.CreateHistogram<double>("litegraph.graph.transaction.commit.duration", "ms", "Graph transaction commit duration in milliseconds.");
            _GraphTransactionRollbackDurationMs = Meter.CreateHistogram<double>("litegraph.graph.transaction.rollback.duration", "ms", "Graph transaction rollback duration in milliseconds.");
            _GraphTransactionConflictCounter = Meter.CreateCounter<long>("litegraph.graph.transaction.conflicts", "conflicts", "Total graph transaction concurrency conflicts.");
            _GraphTransactionRetryCounter = Meter.CreateCounter<long>("litegraph.graph.transaction.retries", "retries", "Total graph transaction retry attempts reported by LiteGraph.");
            _AuthenticationCounter = Meter.CreateCounter<long>("litegraph.authentication.requests", "requests", "Total authenticated requests by authentication and authorization result.");
            _RepositoryOperationCounter = Meter.CreateCounter<long>("litegraph.repository.operations", "operations", "Total repository operations executed by LiteGraph.");
            _RepositoryOperationDurationMs = Meter.CreateHistogram<double>("litegraph.repository.operation.duration", "ms", "Repository operation duration in milliseconds.");
            _GraphTransactionActiveGauge = Meter.CreateObservableGauge<long>("litegraph.graph.transaction.active", ObserveActiveGraphTransactions, "transactions", "Currently active graph transactions.");
            _StorageBackendGauge = Meter.CreateObservableGauge<long>("litegraph.storage.backend.info", ObserveStorageBackends, "backend", "Selected LiteGraph storage backend.");
            _StorageConnectionPoolMaxGauge = Meter.CreateObservableGauge<long>("litegraph.storage.connection.pool.max", ObserveStorageConnectionPoolMax, "connections", "Configured maximum storage connection pool size.");
            _StorageCommandTimeoutGauge = Meter.CreateObservableGauge<long>("litegraph.storage.command.timeout", ObserveStorageCommandTimeouts, "s", "Configured storage command timeout in seconds.");
            _EntityCountGauge = Meter.CreateObservableGauge<long>("litegraph.entity.count", ObserveEntityCounts, "entities", "Latest observed LiteGraph entity counts from statistics endpoints.");
            InitializeOpenTelemetryExporters();
            LiteGraphTelemetry.RepositoryOperationRecorded += HandleRepositoryOperationRecorded;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start an OpenTelemetry-compatible activity.
        /// </summary>
        /// <param name="name">Activity name.</param>
        /// <param name="kind">Activity kind.</param>
        /// <returns>Activity, if tracing is enabled and sampled.</returns>
        public Activity StartActivity(string name, ActivityKind kind)
        {
            if (!_Settings.Enable || !_Settings.EnableOpenTelemetry) return null;
            return ActivitySource.StartActivity(name, kind);
        }

        /// <summary>
        /// Start an OpenTelemetry-compatible activity with an explicit parent context.
        /// </summary>
        /// <param name="name">Activity name.</param>
        /// <param name="kind">Activity kind.</param>
        /// <param name="parentContext">Parent activity context.</param>
        /// <returns>Activity, if tracing is enabled and sampled.</returns>
        public Activity StartActivity(string name, ActivityKind kind, ActivityContext parentContext)
        {
            if (!_Settings.Enable || !_Settings.EnableOpenTelemetry) return null;
            return ActivitySource.StartActivity(name, kind, parentContext);
        }

        /// <summary>
        /// Try to parse W3C trace context headers.
        /// </summary>
        /// <param name="traceparent">traceparent header value.</param>
        /// <param name="tracestate">tracestate header value.</param>
        /// <param name="parentContext">Parsed parent context.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParseTraceContext(string traceparent, string tracestate, out ActivityContext parentContext)
        {
            if (String.IsNullOrWhiteSpace(traceparent))
            {
                parentContext = default;
                return false;
            }

            return ActivityContext.TryParse(traceparent, tracestate, out parentContext);
        }

        /// <summary>
        /// Record a completed HTTP request.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path.</param>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordHttpRequest(string method, string path, int statusCode, double durationMs)
        {
            if (!_Settings.Enable) return;

            method = NormalizeLabel(method);
            path = NormalizePath(path);
            string status = statusCode.ToString(CultureInfo.InvariantCulture);
            string key = method + "\n" + path + "\n" + status;

            RequestMetric metric = _RequestMetrics.GetOrAdd(key, _ => new RequestMetric(method, path, statusCode));
            metric.Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("http.request.method", method),
                    new KeyValuePair<string, object>("url.path", path),
                    new KeyValuePair<string, object>("http.response.status_code", statusCode)
                };

                _HttpRequestsCounter.Add(1, tags);
                _HttpRequestDurationMs.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record a native graph query.
        /// </summary>
        /// <param name="mutated">Whether or not the query mutated graph data.</param>
        /// <param name="success">Whether or not the query completed successfully.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordGraphQuery(bool mutated, bool success, double durationMs)
        {
            if (!_Settings.Enable) return;

            string key = mutated.ToString() + "\n" + success.ToString();
            GraphQueryMetric metric = _GraphQueryMetrics.GetOrAdd(key, _ => new GraphQueryMetric(mutated, success));
            metric.Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.query.mutated", mutated),
                    new KeyValuePair<string, object>("litegraph.query.success", success)
                };

                _GraphQueryCounter.Add(1, tags);
                _GraphQueryDurationMs.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record a vector search.
        /// </summary>
        /// <param name="domain">Vector search domain.</param>
        /// <param name="success">Whether or not the search completed successfully.</param>
        /// <param name="resultCount">Number of results returned.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordVectorSearch(string domain, bool success, int resultCount, double durationMs)
        {
            if (!_Settings.Enable) return;

            domain = NormalizeLabel(domain);
            if (resultCount < 0) resultCount = 0;
            string key = domain + "\n" + success.ToString();
            VectorSearchMetric metric = _VectorSearchMetrics.GetOrAdd(key, _ => new VectorSearchMetric(domain, success));
            metric.Record(resultCount, durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.vector.domain", domain),
                    new KeyValuePair<string, object>("litegraph.vector.success", success)
                };

                _VectorSearchCounter.Add(1, tags);
                _VectorSearchResultCounter.Add(resultCount, tags);
                _VectorSearchDurationMs.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record a graph transaction.
        /// </summary>
        /// <param name="success">Whether or not the transaction committed.</param>
        /// <param name="rolledBack">Whether or not the transaction rolled back.</param>
        /// <param name="operationCount">Number of requested operations.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordGraphTransaction(bool success, bool rolledBack, int operationCount, double durationMs)
        {
            RecordGraphTransaction(success, rolledBack, false, operationCount, durationMs);
        }

        /// <summary>
        /// Record a graph transaction.
        /// </summary>
        /// <param name="success">Whether or not the transaction committed.</param>
        /// <param name="rolledBack">Whether or not the transaction rolled back.</param>
        /// <param name="validationFailure">Whether or not validation failed before a provider transaction started.</param>
        /// <param name="operationCount">Number of requested operations.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordGraphTransaction(bool success, bool rolledBack, bool validationFailure, int operationCount, double durationMs)
        {
            RecordGraphTransaction(
                success,
                rolledBack,
                validationFailure,
                operationCount,
                durationMs,
                provider: null,
                isolationLevel: null,
                state: null,
                serializedByGate: false,
                retryable: false,
                concurrencyConflict: false,
                retryCount: 0,
                queueWaitDurationMs: 0,
                commitDurationMs: 0,
                rollbackDurationMs: 0);
        }

        /// <summary>
        /// Record a graph transaction from a transaction result.
        /// </summary>
        /// <param name="result">Transaction result.</param>
        /// <param name="operationCount">Number of requested operations.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordGraphTransaction(TransactionResult result, int operationCount, double durationMs)
        {
            if (result == null)
            {
                RecordGraphTransaction(false, false, false, operationCount, durationMs);
                return;
            }

            RecordGraphTransaction(
                result.Success,
                result.RolledBack,
                result.ValidationFailure,
                operationCount,
                durationMs,
                result.Provider,
                result.IsolationLevel,
                result.State,
                result.SerializedByGate,
                result.Retryable,
                result.ConcurrencyConflict,
                result.RetryCount,
                result.QueueWaitDurationMs,
                result.CommitDurationMs,
                result.RollbackDurationMs);
        }

        /// <summary>
        /// Record a graph transaction.
        /// </summary>
        /// <param name="success">Whether or not the transaction committed.</param>
        /// <param name="rolledBack">Whether or not the transaction rolled back.</param>
        /// <param name="validationFailure">Whether or not validation failed before a provider transaction started.</param>
        /// <param name="operationCount">Number of requested operations.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        /// <param name="provider">Provider that executed the transaction.</param>
        /// <param name="isolationLevel">Requested isolation level.</param>
        /// <param name="state">Final transaction state.</param>
        /// <param name="serializedByGate">Whether the fallback transaction gate serialized execution.</param>
        /// <param name="retryable">Whether the failure is retryable.</param>
        /// <param name="concurrencyConflict">Whether the failure was a concurrency conflict.</param>
        /// <param name="retryCount">Retry count reported by LiteGraph.</param>
        /// <param name="queueWaitDurationMs">Serialized fallback queue wait in milliseconds.</param>
        /// <param name="commitDurationMs">Commit duration in milliseconds.</param>
        /// <param name="rollbackDurationMs">Rollback duration in milliseconds.</param>
        public void RecordGraphTransaction(
            bool success,
            bool rolledBack,
            bool validationFailure,
            int operationCount,
            double durationMs,
            string provider,
            string isolationLevel,
            string state,
            bool serializedByGate,
            bool retryable,
            bool concurrencyConflict,
            int retryCount,
            double queueWaitDurationMs,
            double commitDurationMs,
            double rollbackDurationMs)
        {
            if (!_Settings.Enable) return;

            provider = NormalizeLabel(provider);
            isolationLevel = NormalizeLabel(isolationLevel);
            state = NormalizeLabel(state);
            if (operationCount < 0) operationCount = 0;
            if (retryCount < 0) retryCount = 0;
            if (queueWaitDurationMs < 0) queueWaitDurationMs = 0;
            if (commitDurationMs < 0) commitDurationMs = 0;
            if (rollbackDurationMs < 0) rollbackDurationMs = 0;

            string key = success.ToString()
                + "\n" + rolledBack.ToString()
                + "\n" + validationFailure.ToString()
                + "\n" + provider
                + "\n" + isolationLevel
                + "\n" + state
                + "\n" + serializedByGate.ToString()
                + "\n" + retryable.ToString()
                + "\n" + concurrencyConflict.ToString();
            GraphTransactionMetric metric = _GraphTransactionMetrics.GetOrAdd(key, _ => new GraphTransactionMetric(
                success,
                rolledBack,
                validationFailure,
                provider,
                isolationLevel,
                state,
                serializedByGate,
                retryable,
                concurrencyConflict));
            metric.Record(operationCount, durationMs, queueWaitDurationMs, commitDurationMs, rollbackDurationMs, retryCount);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.transaction.success", success),
                    new KeyValuePair<string, object>("litegraph.transaction.rolled_back", rolledBack),
                    new KeyValuePair<string, object>("litegraph.transaction.validation_failure", validationFailure),
                    new KeyValuePair<string, object>("litegraph.transaction.provider", provider),
                    new KeyValuePair<string, object>("litegraph.transaction.isolation_level", isolationLevel),
                    new KeyValuePair<string, object>("litegraph.transaction.state", state),
                    new KeyValuePair<string, object>("litegraph.transaction.serialized_by_gate", serializedByGate),
                    new KeyValuePair<string, object>("litegraph.transaction.retryable", retryable),
                    new KeyValuePair<string, object>("litegraph.transaction.concurrency_conflict", concurrencyConflict)
                };

                _GraphTransactionCounter.Add(1, tags);
                _GraphTransactionDurationMs.Record(durationMs, tags);
                _GraphTransactionQueueWaitDurationMs.Record(queueWaitDurationMs, tags);
                _GraphTransactionCommitDurationMs.Record(commitDurationMs, tags);
                _GraphTransactionRollbackDurationMs.Record(rollbackDurationMs, tags);
                if (concurrencyConflict) _GraphTransactionConflictCounter.Add(1, tags);
                if (retryCount > 0) _GraphTransactionRetryCounter.Add(retryCount, tags);
            }
        }

        /// <summary>
        /// Track an active graph transaction for active-transaction gauges.
        /// </summary>
        /// <returns>Disposable scope.</returns>
        public IDisposable BeginGraphTransaction()
        {
            if (!_Settings.Enable) return NullDisposable.Instance;
            Interlocked.Increment(ref _ActiveGraphTransactions);
            return new ActiveGraphTransactionScope(this);
        }

        /// <summary>
        /// Record an authentication and authorization result.
        /// </summary>
        /// <param name="authenticationResult">Authentication result.</param>
        /// <param name="authorizationResult">Authorization result.</param>
        public void RecordAuthentication(AuthenticationResultEnum authenticationResult, AuthorizationResultEnum authorizationResult)
        {
            if (!_Settings.Enable) return;

            string auth = authenticationResult.ToString();
            string authz = authorizationResult.ToString();
            string key = auth + "\n" + authz;
            AuthenticationMetric metric = _AuthenticationMetrics.GetOrAdd(key, _ => new AuthenticationMetric(auth, authz));
            metric.Record();

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.authentication.result", auth),
                    new KeyValuePair<string, object>("litegraph.authorization.result", authz)
                };

                _AuthenticationCounter.Add(1, tags);
            }
        }

        /// <summary>
        /// Record a repository operation.
        /// </summary>
        /// <param name="provider">Storage provider name.</param>
        /// <param name="operation">Operation classification.</param>
        /// <param name="success">Whether or not the operation completed successfully.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordRepositoryOperation(string provider, string operation, bool success, double durationMs)
        {
            if (!_Settings.Enable) return;

            provider = NormalizeLabel(provider);
            operation = NormalizeLabel(operation);
            string key = provider + "\n" + operation + "\n" + success.ToString();
            RepositoryOperationMetric metric = _RepositoryOperationMetrics.GetOrAdd(key, _ => new RepositoryOperationMetric(provider, operation, success));
            metric.Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.repository.provider", provider),
                    new KeyValuePair<string, object>("litegraph.repository.operation", operation),
                    new KeyValuePair<string, object>("litegraph.repository.success", success)
                };

                _RepositoryOperationCounter.Add(1, tags);
                _RepositoryOperationDurationMs.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record the configured storage backend.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <param name="productionRecommended">Whether this provider is recommended for production.</param>
        public void RecordStorageBackend(string provider, bool productionRecommended)
        {
            if (!_Settings.Enable) return;

            provider = NormalizeLabel(provider);
            _StorageBackendMetrics[provider] = new StorageBackendMetric(provider, productionRecommended);
        }

        /// <summary>
        /// Record configured storage connection and command timeout settings.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <param name="maxConnections">Configured maximum connection count.</param>
        /// <param name="commandTimeoutSeconds">Configured command timeout, in seconds.</param>
        public void RecordStorageConnectionPool(string provider, int maxConnections, int commandTimeoutSeconds)
        {
            if (!_Settings.Enable) return;

            provider = NormalizeLabel(provider);
            if (maxConnections < 0) maxConnections = 0;
            if (commandTimeoutSeconds < 0) commandTimeoutSeconds = 0;
            _StorageConnectionPoolMetrics[provider] = new StorageConnectionPoolMetric(provider, maxConnections, commandTimeoutSeconds);
        }

        /// <summary>
        /// Record an entity count observed from a statistics endpoint.
        /// </summary>
        /// <param name="scope">Low-cardinality scope label.</param>
        /// <param name="entity">Entity type label.</param>
        /// <param name="count">Count.</param>
        public void RecordEntityCount(string scope, string entity, long count)
        {
            if (!_Settings.Enable) return;

            scope = NormalizeLabel(scope);
            entity = NormalizeLabel(entity);
            if (count < 0) count = 0;
            string key = scope + "\n" + entity;
            _EntityCountMetrics[key] = new EntityCountMetric(scope, entity, count);
        }

        /// <summary>
        /// Render metrics using the Prometheus text exposition format.
        /// </summary>
        /// <returns>Prometheus metrics text.</returns>
        public string RenderPrometheus()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# HELP litegraph_http_requests_total Total HTTP requests processed by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_http_requests_total counter");
            foreach (RequestMetric metric in _RequestMetrics.Values)
            {
                sb.Append("litegraph_http_requests_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_http_request_duration_ms HTTP request duration in milliseconds.");
            sb.AppendLine("# TYPE litegraph_http_request_duration_ms histogram");
            foreach (RequestMetric metric in _RequestMetrics.Values)
            {
                long[] bucketCounts = metric.GetBucketCounts();
                for (int i = 0; i < _HttpRequestDurationBucketsMs.Length; i++)
                {
                    sb.Append("litegraph_http_request_duration_ms_bucket");
                    AppendLabels(sb, metric, _HttpRequestDurationBucketsMs[i].ToString(CultureInfo.InvariantCulture));
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

            sb.AppendLine("# HELP litegraph_graph_queries_total Total native graph queries processed by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_graph_queries_total counter");
            foreach (GraphQueryMetric metric in _GraphQueryMetrics.Values)
            {
                sb.Append("litegraph_graph_queries_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_query_duration_ms Total and count of native graph query durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_graph_query_duration_ms summary");
            foreach (GraphQueryMetric metric in _GraphQueryMetrics.Values)
            {
                sb.Append("litegraph_graph_query_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.DurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_graph_query_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_vector_searches_total Total vector searches processed by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_vector_searches_total counter");
            foreach (VectorSearchMetric metric in _VectorSearchMetrics.Values)
            {
                sb.Append("litegraph_vector_searches_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_vector_search_results_total Total vector search results returned by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_vector_search_results_total counter");
            foreach (VectorSearchMetric metric in _VectorSearchMetrics.Values)
            {
                sb.Append("litegraph_vector_search_results_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.ResultCount.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_vector_search_duration_ms Total and count of vector search durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_vector_search_duration_ms summary");
            foreach (VectorSearchMetric metric in _VectorSearchMetrics.Values)
            {
                sb.Append("litegraph_vector_search_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.DurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_vector_search_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transactions_total Total graph transactions processed by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_graph_transactions_total counter");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transactions_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_operations_total Total graph transaction operations requested.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_operations_total counter");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_operations_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.OperationCount.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_duration_ms Total and count of graph transaction durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_duration_ms summary");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.DurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_graph_transaction_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_queue_wait_duration_ms Total and count of serialized fallback queue wait durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_queue_wait_duration_ms summary");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_queue_wait_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.QueueWaitDurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_graph_transaction_queue_wait_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_commit_duration_ms Total and count of graph transaction commit durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_commit_duration_ms summary");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_commit_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.CommitDurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_graph_transaction_commit_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_rollback_duration_ms Total and count of graph transaction rollback durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_rollback_duration_ms summary");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_rollback_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.RollbackDurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_graph_transaction_rollback_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_conflicts_total Total graph transaction concurrency conflicts.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_conflicts_total counter");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_conflicts_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.ConflictCount.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_retries_total Total graph transaction retry attempts reported by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_retries_total counter");
            foreach (GraphTransactionMetric metric in _GraphTransactionMetrics.Values)
            {
                sb.Append("litegraph_graph_transaction_retries_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.RetryCount.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_graph_transaction_active Currently active graph transactions.");
            sb.AppendLine("# TYPE litegraph_graph_transaction_active gauge");
            sb.Append("litegraph_graph_transaction_active ");
            sb.AppendLine(Interlocked.Read(ref _ActiveGraphTransactions).ToString(CultureInfo.InvariantCulture));

            sb.AppendLine("# HELP litegraph_authentication_requests_total Total authenticated requests by authentication and authorization result.");
            sb.AppendLine("# TYPE litegraph_authentication_requests_total counter");
            foreach (AuthenticationMetric metric in _AuthenticationMetrics.Values)
            {
                sb.Append("litegraph_authentication_requests_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_repository_operations_total Total repository operations executed by LiteGraph.");
            sb.AppendLine("# TYPE litegraph_repository_operations_total counter");
            foreach (RepositoryOperationMetric metric in _RepositoryOperationMetrics.Values)
            {
                sb.Append("litegraph_repository_operations_total");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_repository_operation_duration_ms Total and count of repository operation durations in milliseconds.");
            sb.AppendLine("# TYPE litegraph_repository_operation_duration_ms summary");
            foreach (RepositoryOperationMetric metric in _RepositoryOperationMetrics.Values)
            {
                sb.Append("litegraph_repository_operation_duration_ms_sum");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.DurationSumMs.ToString(CultureInfo.InvariantCulture));

                sb.Append("litegraph_repository_operation_duration_ms_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_storage_backend_info Selected LiteGraph storage backend.");
            sb.AppendLine("# TYPE litegraph_storage_backend_info gauge");
            foreach (StorageBackendMetric metric in _StorageBackendMetrics.Values)
            {
                sb.Append("litegraph_storage_backend_info");
                AppendLabels(sb, metric);
                sb.AppendLine(" 1");
            }

            sb.AppendLine("# HELP litegraph_storage_connection_pool_max Configured maximum storage connection pool size.");
            sb.AppendLine("# TYPE litegraph_storage_connection_pool_max gauge");
            foreach (StorageConnectionPoolMetric metric in _StorageConnectionPoolMetrics.Values)
            {
                sb.Append("litegraph_storage_connection_pool_max");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.MaxConnections.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_storage_command_timeout_seconds Configured storage command timeout in seconds.");
            sb.AppendLine("# TYPE litegraph_storage_command_timeout_seconds gauge");
            foreach (StorageConnectionPoolMetric metric in _StorageConnectionPoolMetrics.Values)
            {
                sb.Append("litegraph_storage_command_timeout_seconds");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.CommandTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("# HELP litegraph_entity_count Latest observed LiteGraph entity counts from statistics endpoints.");
            sb.AppendLine("# TYPE litegraph_entity_count gauge");
            foreach (EntityCountMetric metric in _EntityCountMetrics.Values)
            {
                sb.Append("litegraph_entity_count");
                AppendLabels(sb, metric);
                sb.Append(' ');
                sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
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
            LiteGraphTelemetry.RepositoryOperationRecorded -= HandleRepositoryOperationRecorded;
            _TracerProvider?.Dispose();
            _MeterProvider?.Dispose();
            ActivitySource.Dispose();
            Meter.Dispose();
        }

        #endregion

        #region Private-Methods

        private static string NormalizeLabel(string value)
        {
            if (String.IsNullOrEmpty(value)) return "unknown";
            return value.Trim();
        }

        private static string NormalizePath(string value)
        {
            if (String.IsNullOrEmpty(value)) return "/";
            int queryIndex = value.IndexOf('?');
            if (queryIndex >= 0) value = value.Substring(0, queryIndex);
            return value.Trim();
        }

        private static string EscapeLabel(string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void InitializeOpenTelemetryExporters()
        {
            if (!_Settings.Enable || !_Settings.EnableOpenTelemetry || !_Settings.EnableOtlpExporter) return;

            ResourceBuilder resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService(_Settings.ServiceName);

            _TracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource(_Settings.ServiceName, LiteGraphTelemetry.ActivitySourceName)
                .AddOtlpExporter(ConfigureOtlpExporter)
                .Build();

            _MeterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter(_Settings.ServiceName, LiteGraphTelemetry.ActivitySourceName)
                .AddOtlpExporter(ConfigureOtlpExporter)
                .Build();
        }

        private void ConfigureOtlpExporter(OtlpExporterOptions options)
        {
            if (options == null) return;

            if (!String.IsNullOrWhiteSpace(_Settings.OtlpEndpoint))
            {
                options.Endpoint = new Uri(_Settings.OtlpEndpoint, UriKind.Absolute);
            }

            if (!String.IsNullOrWhiteSpace(_Settings.OtlpHeaders))
            {
                options.Headers = _Settings.OtlpHeaders;
            }

            options.TimeoutMilliseconds = _Settings.OtlpTimeoutMilliseconds;
            options.Protocol = ParseOtlpProtocol(_Settings.OtlpProtocol);
        }

        private static OtlpExportProtocol ParseOtlpProtocol(string protocol)
        {
            if (String.IsNullOrWhiteSpace(protocol)) return OtlpExportProtocol.Grpc;

            string normalized = protocol.Trim().Replace("_", "-").ToLowerInvariant();
            if (String.Equals(normalized, "grpc", StringComparison.Ordinal)) return OtlpExportProtocol.Grpc;
            if (String.Equals(normalized, "http/protobuf", StringComparison.Ordinal)
                || String.Equals(normalized, "http-protobuf", StringComparison.Ordinal)
                || String.Equals(normalized, "httpprotobuf", StringComparison.Ordinal))
                return OtlpExportProtocol.HttpProtobuf;

            throw new ArgumentException("Unsupported OTLP protocol '" + protocol + "'.");
        }

        private static void AppendLabels(StringBuilder sb, RequestMetric metric)
        {
            sb.Append("{method=\"");
            sb.Append(EscapeLabel(metric.Method));
            sb.Append("\",path=\"");
            sb.Append(EscapeLabel(metric.Path));
            sb.Append("\",status_code=\"");
            sb.Append(metric.StatusCode.ToString(CultureInfo.InvariantCulture));
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, RequestMetric metric, string le)
        {
            sb.Append("{method=\"");
            sb.Append(EscapeLabel(metric.Method));
            sb.Append("\",path=\"");
            sb.Append(EscapeLabel(metric.Path));
            sb.Append("\",status_code=\"");
            sb.Append(metric.StatusCode.ToString(CultureInfo.InvariantCulture));
            sb.Append("\",le=\"");
            sb.Append(EscapeLabel(le));
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, GraphQueryMetric metric)
        {
            sb.Append("{mutated=\"");
            sb.Append(metric.Mutated ? "true" : "false");
            sb.Append("\",success=\"");
            sb.Append(metric.Success ? "true" : "false");
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, VectorSearchMetric metric)
        {
            sb.Append("{domain=\"");
            sb.Append(EscapeLabel(metric.Domain));
            sb.Append("\",success=\"");
            sb.Append(metric.Success ? "true" : "false");
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, GraphTransactionMetric metric)
        {
            sb.Append("{success=\"");
            sb.Append(metric.Success ? "true" : "false");
            sb.Append("\",rolled_back=\"");
            sb.Append(metric.RolledBack ? "true" : "false");
            sb.Append("\",validation_failure=\"");
            sb.Append(metric.ValidationFailure ? "true" : "false");
            sb.Append("\",provider=\"");
            sb.Append(EscapeLabel(metric.Provider));
            sb.Append("\",isolation_level=\"");
            sb.Append(EscapeLabel(metric.IsolationLevel));
            sb.Append("\",state=\"");
            sb.Append(EscapeLabel(metric.State));
            sb.Append("\",serialized_by_gate=\"");
            sb.Append(metric.SerializedByGate ? "true" : "false");
            sb.Append("\",retryable=\"");
            sb.Append(metric.Retryable ? "true" : "false");
            sb.Append("\",concurrency_conflict=\"");
            sb.Append(metric.ConcurrencyConflict ? "true" : "false");
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, AuthenticationMetric metric)
        {
            sb.Append("{authentication_result=\"");
            sb.Append(EscapeLabel(metric.AuthenticationResult));
            sb.Append("\",authorization_result=\"");
            sb.Append(EscapeLabel(metric.AuthorizationResult));
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, RepositoryOperationMetric metric)
        {
            sb.Append("{provider=\"");
            sb.Append(EscapeLabel(metric.Provider));
            sb.Append("\",operation=\"");
            sb.Append(EscapeLabel(metric.Operation));
            sb.Append("\",success=\"");
            sb.Append(metric.Success ? "true" : "false");
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, StorageBackendMetric metric)
        {
            sb.Append("{provider=\"");
            sb.Append(EscapeLabel(metric.Provider));
            sb.Append("\",production_recommended=\"");
            sb.Append(metric.ProductionRecommended ? "true" : "false");
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, StorageConnectionPoolMetric metric)
        {
            sb.Append("{provider=\"");
            sb.Append(EscapeLabel(metric.Provider));
            sb.Append("\"}");
        }

        private static void AppendLabels(StringBuilder sb, EntityCountMetric metric)
        {
            sb.Append("{scope=\"");
            sb.Append(EscapeLabel(metric.Scope));
            sb.Append("\",entity=\"");
            sb.Append(EscapeLabel(metric.Entity));
            sb.Append("\"}");
        }

        private IEnumerable<Measurement<long>> ObserveStorageBackends()
        {
            foreach (StorageBackendMetric metric in _StorageBackendMetrics.Values)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.storage.provider", metric.Provider),
                    new KeyValuePair<string, object>("litegraph.storage.production_recommended", metric.ProductionRecommended)
                };

                yield return new Measurement<long>(1, tags);
            }
        }

        private IEnumerable<Measurement<long>> ObserveActiveGraphTransactions()
        {
            yield return new Measurement<long>(Interlocked.Read(ref _ActiveGraphTransactions));
        }

        private IEnumerable<Measurement<long>> ObserveStorageConnectionPoolMax()
        {
            foreach (StorageConnectionPoolMetric metric in _StorageConnectionPoolMetrics.Values)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.storage.provider", metric.Provider)
                };

                yield return new Measurement<long>(metric.MaxConnections, tags);
            }
        }

        private IEnumerable<Measurement<long>> ObserveStorageCommandTimeouts()
        {
            foreach (StorageConnectionPoolMetric metric in _StorageConnectionPoolMetrics.Values)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.storage.provider", metric.Provider)
                };

                yield return new Measurement<long>(metric.CommandTimeoutSeconds, tags);
            }
        }

        private IEnumerable<Measurement<long>> ObserveEntityCounts()
        {
            foreach (EntityCountMetric metric in _EntityCountMetrics.Values)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.entity.scope", metric.Scope),
                    new KeyValuePair<string, object>("litegraph.entity.type", metric.Entity)
                };

                yield return new Measurement<long>(metric.Count, tags);
            }
        }

        private void HandleRepositoryOperationRecorded(object sender, RepositoryOperationTelemetryEventArgs e)
        {
            if (e == null) return;
            RecordRepositoryOperation(e.Provider, e.Operation, e.Success, e.DurationMs);
        }

        #endregion

        #region Private-Classes

        private sealed class RequestMetric
        {
            private readonly object _Lock = new object();

            internal string Method { get; }
            internal string Path { get; }
            internal int StatusCode { get; }
            internal long Count { get; private set; }
            internal double DurationSumMs { get; private set; }
            private readonly long[] _BucketCounts = new long[_HttpRequestDurationBucketsMs.Length];

            internal RequestMetric(string method, string path, int statusCode)
            {
                Method = method;
                Path = path;
                StatusCode = statusCode;
            }

            internal void Record(double durationMs)
            {
                if (durationMs < 0) durationMs = 0;

                lock (_Lock)
                {
                    Count++;
                    DurationSumMs += durationMs;
                    for (int i = 0; i < _HttpRequestDurationBucketsMs.Length; i++)
                    {
                        if (durationMs <= _HttpRequestDurationBucketsMs[i]) _BucketCounts[i]++;
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

        private sealed class GraphQueryMetric
        {
            private readonly object _Lock = new object();

            internal bool Mutated { get; }
            internal bool Success { get; }
            internal long Count { get; private set; }
            internal double DurationSumMs { get; private set; }

            internal GraphQueryMetric(bool mutated, bool success)
            {
                Mutated = mutated;
                Success = success;
            }

            internal void Record(double durationMs)
            {
                lock (_Lock)
                {
                    Count++;
                    DurationSumMs += durationMs;
                }
            }
        }

        private sealed class VectorSearchMetric
        {
            private readonly object _Lock = new object();

            internal string Domain { get; }
            internal bool Success { get; }
            internal long Count { get; private set; }
            internal long ResultCount { get; private set; }
            internal double DurationSumMs { get; private set; }

            internal VectorSearchMetric(string domain, bool success)
            {
                Domain = domain;
                Success = success;
            }

            internal void Record(int resultCount, double durationMs)
            {
                lock (_Lock)
                {
                    Count++;
                    ResultCount += resultCount;
                    DurationSumMs += durationMs;
                }
            }
        }

        private sealed class GraphTransactionMetric
        {
            private readonly object _Lock = new object();

            internal bool Success { get; }
            internal bool RolledBack { get; }
            internal bool ValidationFailure { get; }
            internal string Provider { get; }
            internal string IsolationLevel { get; }
            internal string State { get; }
            internal bool SerializedByGate { get; }
            internal bool Retryable { get; }
            internal bool ConcurrencyConflict { get; }
            internal long Count { get; private set; }
            internal long OperationCount { get; private set; }
            internal long RetryCount { get; private set; }
            internal long ConflictCount { get; private set; }
            internal double DurationSumMs { get; private set; }
            internal double QueueWaitDurationSumMs { get; private set; }
            internal double CommitDurationSumMs { get; private set; }
            internal double RollbackDurationSumMs { get; private set; }

            internal GraphTransactionMetric(
                bool success,
                bool rolledBack,
                bool validationFailure,
                string provider,
                string isolationLevel,
                string state,
                bool serializedByGate,
                bool retryable,
                bool concurrencyConflict)
            {
                Success = success;
                RolledBack = rolledBack;
                ValidationFailure = validationFailure;
                Provider = provider;
                IsolationLevel = isolationLevel;
                State = state;
                SerializedByGate = serializedByGate;
                Retryable = retryable;
                ConcurrencyConflict = concurrencyConflict;
            }

            internal void Record(
                int operationCount,
                double durationMs,
                double queueWaitDurationMs,
                double commitDurationMs,
                double rollbackDurationMs,
                int retryCount)
            {
                lock (_Lock)
                {
                    Count++;
                    OperationCount += operationCount;
                    DurationSumMs += durationMs;
                    QueueWaitDurationSumMs += queueWaitDurationMs;
                    CommitDurationSumMs += commitDurationMs;
                    RollbackDurationSumMs += rollbackDurationMs;
                    RetryCount += retryCount;
                    if (ConcurrencyConflict) ConflictCount++;
                }
            }
        }

        private sealed class ActiveGraphTransactionScope : IDisposable
        {
            private readonly ObservabilityService _Owner;
            private bool _Disposed = false;

            internal ActiveGraphTransactionScope(ObservabilityService owner)
            {
                _Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void Dispose()
            {
                if (_Disposed) return;
                _Disposed = true;
                Interlocked.Decrement(ref _Owner._ActiveGraphTransactions);
            }
        }

        private sealed class NullDisposable : IDisposable
        {
            internal static readonly NullDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }

        private sealed class AuthenticationMetric
        {
            private readonly object _Lock = new object();

            internal string AuthenticationResult { get; }
            internal string AuthorizationResult { get; }
            internal long Count { get; private set; }

            internal AuthenticationMetric(string authenticationResult, string authorizationResult)
            {
                AuthenticationResult = authenticationResult;
                AuthorizationResult = authorizationResult;
            }

            internal void Record()
            {
                lock (_Lock)
                {
                    Count++;
                }
            }
        }

        private sealed class RepositoryOperationMetric
        {
            private readonly object _Lock = new object();

            internal string Provider { get; }
            internal string Operation { get; }
            internal bool Success { get; }
            internal long Count { get; private set; }
            internal double DurationSumMs { get; private set; }

            internal RepositoryOperationMetric(string provider, string operation, bool success)
            {
                Provider = provider;
                Operation = operation;
                Success = success;
            }

            internal void Record(double durationMs)
            {
                lock (_Lock)
                {
                    Count++;
                    DurationSumMs += durationMs;
                }
            }
        }

        private sealed class StorageBackendMetric
        {
            internal string Provider { get; }
            internal bool ProductionRecommended { get; }

            internal StorageBackendMetric(string provider, bool productionRecommended)
            {
                Provider = provider;
                ProductionRecommended = productionRecommended;
            }
        }

        private sealed class StorageConnectionPoolMetric
        {
            internal string Provider { get; }
            internal int MaxConnections { get; }
            internal int CommandTimeoutSeconds { get; }

            internal StorageConnectionPoolMetric(string provider, int maxConnections, int commandTimeoutSeconds)
            {
                Provider = provider;
                MaxConnections = maxConnections;
                CommandTimeoutSeconds = commandTimeoutSeconds;
            }
        }

        private sealed class EntityCountMetric
        {
            internal string Scope { get; }
            internal string Entity { get; }
            internal long Count { get; }

            internal EntityCountMetric(string scope, string entity, long count)
            {
                Scope = scope;
                Entity = entity;
                Count = count;
            }
        }

        #endregion
    }
}
