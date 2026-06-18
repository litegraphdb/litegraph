namespace LiteGraph
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Core LiteGraph telemetry names and helpers.
    /// </summary>
    public static class LiteGraphTelemetry
    {
        #region Public-Members

        /// <summary>
        /// Core ActivitySource name used by embedded LiteGraph operations.
        /// </summary>
        public const string ActivitySourceName = "LiteGraph";

        /// <summary>
        /// Core Meter name used by embedded LiteGraph operations.
        /// </summary>
        public const string MeterName = ActivitySourceName;

        /// <summary>
        /// Native graph query activity name.
        /// </summary>
        public const string QueryActivityName = "litegraph.query";

        /// <summary>
        /// Native graph query parse phase activity name.
        /// </summary>
        public const string QueryParseActivityName = "litegraph.query.parse";

        /// <summary>
        /// Native graph query plan phase activity name.
        /// </summary>
        public const string QueryPlanActivityName = "litegraph.query.plan";

        /// <summary>
        /// Native graph query executor phase activity name.
        /// </summary>
        public const string QueryExecuteActivityName = "litegraph.query.execute";

        /// <summary>
        /// Vector search activity name.
        /// </summary>
        public const string VectorSearchActivityName = "litegraph.vector.search";

        /// <summary>
        /// Vector index search activity name.
        /// </summary>
        public const string VectorIndexSearchActivityName = "litegraph.vector.index.search";

        /// <summary>
        /// Repository operation activity name.
        /// </summary>
        public const string RepositoryOperationActivityName = "litegraph.repository.operation";

        /// <summary>
        /// Core ActivitySource used by embedded LiteGraph operations.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

        /// <summary>
        /// Core Meter used by embedded LiteGraph operations.
        /// </summary>
        public static readonly Meter Meter = new Meter(MeterName);

        /// <summary>
        /// Raised whenever a repository operation completes.
        /// </summary>
        public static event EventHandler<RepositoryOperationTelemetryEventArgs> RepositoryOperationRecorded;

        /// <summary>
        /// Raised whenever a vector search completes.
        /// </summary>
        public static event EventHandler<VectorSearchTelemetryEventArgs> VectorSearchRecorded;

        #endregion

        #region Internal-Methods

        internal static void SetActivityOk(Activity activity)
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        internal static void SetActivityException(Activity activity, Exception exception)
        {
            if (activity == null || exception == null) return;

            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message }
                }));
        }

        internal static void RecordRepositoryOperation(RepositoryOperationTelemetryEventArgs args)
        {
            if (args == null) return;
            _TimingCapture.Value?.AddRepositoryOperation(args.DurationMs);

            KeyValuePair<string, object>[] tags =
            {
                new KeyValuePair<string, object>("litegraph.repository.provider", args.Provider),
                new KeyValuePair<string, object>("litegraph.repository.operation", args.Operation),
                new KeyValuePair<string, object>("litegraph.repository.success", args.Success)
            };

            _RepositoryOperationCounter.Add(1, tags);
            _RepositoryOperationDurationMs.Record(args.DurationMs, tags);

            EventHandler<RepositoryOperationTelemetryEventArgs> handler = RepositoryOperationRecorded;
            if (handler == null) return;

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((EventHandler<RepositoryOperationTelemetryEventArgs>)subscriber)(null, args);
                }
                catch
                {
                    // Telemetry subscribers must not affect repository behavior.
                }
            }
        }

        internal static void RecordVectorSearch(VectorSearchTelemetryEventArgs args)
        {
            if (args == null) return;
            _TimingCapture.Value?.AddVectorSearch(args.DurationMs);

            KeyValuePair<string, object>[] tags =
            {
                new KeyValuePair<string, object>("litegraph.vector.domain", args.Domain),
                new KeyValuePair<string, object>("litegraph.vector.success", args.Success)
            };

            _VectorSearchCounter.Add(1, tags);
            _VectorSearchResultCounter.Add(args.ResultCount, tags);
            _VectorSearchDurationMs.Record(args.DurationMs, tags);

            EventHandler<VectorSearchTelemetryEventArgs> handler = VectorSearchRecorded;
            if (handler == null) return;

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((EventHandler<VectorSearchTelemetryEventArgs>)subscriber)(null, args);
                }
                catch
                {
                    // Telemetry subscribers must not affect vector search behavior.
                }
            }
        }

        internal static LiteGraphTelemetryTimingCapture BeginTimingCapture()
        {
            return new LiteGraphTelemetryTimingCapture(_TimingCapture.Value, capture => _TimingCapture.Value = capture);
        }

        internal static void RecordTransactionTiming(double durationMs)
        {
            _TimingCapture.Value?.AddTransaction(durationMs);
        }

        internal static void RecordVectorIndexMutationFailure(string provider, string indexType, string errorType)
        {
            KeyValuePair<string, object>[] tags =
            {
                new KeyValuePair<string, object>("litegraph.repository.provider", provider ?? "unknown"),
                new KeyValuePair<string, object>("litegraph.vector.index.type", indexType ?? "unknown"),
                new KeyValuePair<string, object>("litegraph.vector.index.error_type", errorType ?? "unknown")
            };

            _VectorIndexMutationFailureCounter.Add(1, tags);
        }

        #endregion

        #region Private-Members

        private static readonly AsyncLocal<LiteGraphTelemetryTimingCapture> _TimingCapture = new AsyncLocal<LiteGraphTelemetryTimingCapture>();

        private static readonly Counter<long> _RepositoryOperationCounter = Meter.CreateCounter<long>(
            "litegraph.repository.operations",
            "operations",
            "Total repository operations executed by LiteGraph.");

        private static readonly Histogram<double> _RepositoryOperationDurationMs = Meter.CreateHistogram<double>(
            "litegraph.repository.operation.duration",
            "ms",
            "Repository operation duration in milliseconds.");

        private static readonly Counter<long> _VectorSearchCounter = Meter.CreateCounter<long>(
            "litegraph.vector.searches",
            "searches",
            "Total vector searches processed by LiteGraph.");

        private static readonly Counter<long> _VectorSearchResultCounter = Meter.CreateCounter<long>(
            "litegraph.vector.search.results",
            "results",
            "Total vector search results returned by LiteGraph.");

        private static readonly Histogram<double> _VectorSearchDurationMs = Meter.CreateHistogram<double>(
            "litegraph.vector.search.duration",
            "ms",
            "Vector search duration in milliseconds.");

        private static readonly Counter<long> _VectorIndexMutationFailureCounter = Meter.CreateCounter<long>(
            "litegraph.vector.index.mutation.failures",
            "failures",
            "Total vector index mutation failures after database commit.");

        #endregion
    }

    /// <summary>
    /// Captures scoped telemetry timing for opt-in query profiling.
    /// </summary>
    internal sealed class LiteGraphTelemetryTimingCapture : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Total repository operation time in milliseconds.
        /// </summary>
        public double RepositoryTimeMs { get; private set; } = 0;

        /// <summary>
        /// Number of repository operations.
        /// </summary>
        public int RepositoryOperationCount { get; private set; } = 0;

        /// <summary>
        /// Total vector search time in milliseconds.
        /// </summary>
        public double VectorSearchTimeMs { get; private set; } = 0;

        /// <summary>
        /// Number of vector searches.
        /// </summary>
        public int VectorSearchCount { get; private set; } = 0;

        /// <summary>
        /// Total graph transaction time in milliseconds.
        /// </summary>
        public double TransactionTimeMs { get; private set; } = 0;

        #endregion

        #region Private-Members

        private readonly LiteGraphTelemetryTimingCapture _Previous;
        private readonly Action<LiteGraphTelemetryTimingCapture> _Restore;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        internal LiteGraphTelemetryTimingCapture(
            LiteGraphTelemetryTimingCapture previous,
            Action<LiteGraphTelemetryTimingCapture> restore)
        {
            _Previous = previous;
            _Restore = restore ?? throw new ArgumentNullException(nameof(restore));
            _Restore(this);
        }

        #endregion

        #region Internal-Methods

        internal void AddRepositoryOperation(double durationMs)
        {
            RepositoryOperationCount++;
            RepositoryTimeMs += durationMs < 0 ? 0 : durationMs;
        }

        internal void AddVectorSearch(double durationMs)
        {
            VectorSearchCount++;
            VectorSearchTimeMs += durationMs < 0 ? 0 : durationMs;
        }

        internal void AddTransaction(double durationMs)
        {
            TransactionTimeMs += durationMs < 0 ? 0 : durationMs;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            _Restore(_Previous);
        }

        #endregion
    }

    /// <summary>
    /// Repository operation telemetry event.
    /// </summary>
    public sealed class RepositoryOperationTelemetryEventArgs : EventArgs
    {
        #region Public-Members

        /// <summary>
        /// Storage provider name.
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// Operation classification.
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Whether the operation completed successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Whether the operation ran within a repository transaction.
        /// </summary>
        public bool Transactional { get; }

        /// <summary>
        /// Number of SQL statements represented by the operation.
        /// </summary>
        public int StatementCount { get; }

        /// <summary>
        /// Number of result rows returned by the operation.
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// Operation duration in milliseconds.
        /// </summary>
        public double DurationMs { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="provider">Storage provider name.</param>
        /// <param name="operation">Operation classification.</param>
        /// <param name="success">Whether the operation completed successfully.</param>
        /// <param name="transactional">Whether the operation ran within a repository transaction.</param>
        /// <param name="statementCount">Number of SQL statements represented by the operation.</param>
        /// <param name="rowCount">Number of result rows returned by the operation.</param>
        /// <param name="durationMs">Operation duration in milliseconds.</param>
        public RepositoryOperationTelemetryEventArgs(
            string provider,
            string operation,
            bool success,
            bool transactional,
            int statementCount,
            int rowCount,
            double durationMs)
        {
            Provider = String.IsNullOrEmpty(provider) ? "unknown" : provider;
            Operation = String.IsNullOrEmpty(operation) ? "unknown" : operation;
            Success = success;
            Transactional = transactional;
            StatementCount = statementCount < 0 ? 0 : statementCount;
            RowCount = rowCount < 0 ? 0 : rowCount;
            DurationMs = durationMs < 0 ? 0 : durationMs;
        }

        #endregion
    }

    /// <summary>
    /// Vector search telemetry event.
    /// </summary>
    public sealed class VectorSearchTelemetryEventArgs : EventArgs
    {
        #region Public-Members

        /// <summary>
        /// Search domain.
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// Whether the search completed successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Number of returned results.
        /// </summary>
        public int ResultCount { get; }

        /// <summary>
        /// Search duration in milliseconds.
        /// </summary>
        public double DurationMs { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="domain">Search domain.</param>
        /// <param name="success">Whether the search completed successfully.</param>
        /// <param name="resultCount">Number of returned results.</param>
        /// <param name="durationMs">Search duration in milliseconds.</param>
        public VectorSearchTelemetryEventArgs(
            string domain,
            bool success,
            int resultCount,
            double durationMs)
        {
            Domain = String.IsNullOrEmpty(domain) ? "unknown" : domain;
            Success = success;
            ResultCount = resultCount < 0 ? 0 : resultCount;
            DurationMs = durationMs < 0 ? 0 : durationMs;
        }

        #endregion
    }
}
