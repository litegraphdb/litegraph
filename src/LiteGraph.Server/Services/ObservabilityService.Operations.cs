namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using LiteGraph;

    /// <summary>
    /// Operational instruments: backup operations, JSONL import outcomes, vector index rebuilds,
    /// retention sweeps, and request history capture drops.  Labels are deliberately low-cardinality
    /// (operation name, result classification, component, index type, success); tenant and graph GUIDs
    /// belong on trace spans, never on metric labels.
    /// </summary>
    public partial class ObservabilityService
    {
        #region Operations-Private-Members

        private readonly ConcurrentDictionary<string, LabeledCounter> _OperationCounters = new ConcurrentDictionary<string, LabeledCounter>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, LabeledSummary> _OperationSummaries = new ConcurrentDictionary<string, LabeledSummary>(StringComparer.Ordinal);

        private Counter<long> _BackupOperationsCounter;
        private Histogram<double> _BackupOperationDurationMs;
        private Counter<long> _GraphImportRecordsCounter;
        private Counter<long> _GraphImportWarningsCounter;
        private Counter<long> _VectorIndexRebuildsCounter;
        private Counter<long> _VectorIndexRebuildVectorsCounter;
        private Histogram<double> _VectorIndexRebuildDurationMs;
        private Counter<long> _RetentionSweepsCounter;
        private Histogram<double> _RetentionSweepDurationMs;
        private Counter<long> _RetentionDeletedCounter;
        private Counter<long> _RequestHistoryDroppedCounter;

        #endregion

        #region Operations-Public-Methods

        /// <summary>
        /// Record a backup administration operation.
        /// </summary>
        /// <param name="operation">Low-cardinality operation label: create, read, read_all, enumerate, exists, or delete.</param>
        /// <param name="success">Whether the operation completed successfully.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public void RecordBackupOperation(string operation, bool success, double durationMs)
        {
            if (!_Settings.Enable) return;

            operation = NormalizeLabel(operation);
            string[] labelNames = new string[] { "operation", "success" };
            string[] labelValues = new string[] { operation, (success ? "true" : "false") };
            OperationCounter("litegraph_backup_operations_total", "Total backup administration operations executed by LiteGraph.", labelNames, labelValues).Add(1);
            OperationSummary("litegraph_backup_operation_duration_ms", "Total and count of backup operation durations in milliseconds.", labelNames, labelValues).Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.backup.operation", operation),
                    new KeyValuePair<string, object>("litegraph.backup.success", success)
                };

                _BackupOperationsCounter?.Add(1, tags);
                _BackupOperationDurationMs?.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record the outcome of a JSONL graph import.
        /// </summary>
        /// <param name="recordsCreated">Number of records (graphs, nodes, and edges) created.</param>
        /// <param name="recordsUpdated">Number of records updated.</param>
        /// <param name="recordsSkipped">Number of records skipped.</param>
        /// <param name="warningCount">Number of warnings raised during the import.</param>
        public void RecordGraphImport(int recordsCreated, int recordsUpdated, int recordsSkipped, int warningCount)
        {
            if (!_Settings.Enable) return;

            if (recordsCreated < 0) recordsCreated = 0;
            if (recordsUpdated < 0) recordsUpdated = 0;
            if (recordsSkipped < 0) recordsSkipped = 0;
            if (warningCount < 0) warningCount = 0;

            string[] labelNames = new string[] { "result" };
            OperationCounter("litegraph_graph_import_records_total", "Total records processed by JSONL graph imports, by result.", labelNames, new string[] { "created" }).Add(recordsCreated);
            OperationCounter("litegraph_graph_import_records_total", "Total records processed by JSONL graph imports, by result.", labelNames, new string[] { "updated" }).Add(recordsUpdated);
            OperationCounter("litegraph_graph_import_records_total", "Total records processed by JSONL graph imports, by result.", labelNames, new string[] { "skipped" }).Add(recordsSkipped);
            OperationCounter("litegraph_graph_import_warnings_total", "Total warnings raised during JSONL graph imports.", Array.Empty<string>(), Array.Empty<string>()).Add(warningCount);

            if (_Settings.EnableOpenTelemetry)
            {
                _GraphImportRecordsCounter?.Add(recordsCreated, new KeyValuePair<string, object>("litegraph.import.result", "created"));
                _GraphImportRecordsCounter?.Add(recordsUpdated, new KeyValuePair<string, object>("litegraph.import.result", "updated"));
                _GraphImportRecordsCounter?.Add(recordsSkipped, new KeyValuePair<string, object>("litegraph.import.result", "skipped"));
                _GraphImportWarningsCounter?.Add(warningCount);
            }
        }

        /// <summary>
        /// Record a vector index rebuild.
        /// </summary>
        /// <param name="indexType">Vector index type label.</param>
        /// <param name="success">Whether the rebuild completed successfully.</param>
        /// <param name="vectorCount">Number of vectors added to the rebuilt index.</param>
        /// <param name="durationMs">Rebuild duration in milliseconds.</param>
        public void RecordVectorIndexRebuild(string indexType, bool success, long vectorCount, double durationMs)
        {
            if (!_Settings.Enable) return;

            indexType = NormalizeLabel(indexType);
            if (vectorCount < 0) vectorCount = 0;
            string[] labelNames = new string[] { "index_type", "success" };
            string[] labelValues = new string[] { indexType, (success ? "true" : "false") };
            OperationCounter("litegraph_vector_index_rebuilds_total", "Total vector index rebuilds executed by LiteGraph.", labelNames, labelValues).Add(1);
            OperationCounter("litegraph_vector_index_rebuild_vectors_total", "Total vectors added during vector index rebuilds.", labelNames, labelValues).Add(vectorCount);
            OperationSummary("litegraph_vector_index_rebuild_duration_ms", "Total and count of vector index rebuild durations in milliseconds.", labelNames, labelValues).Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("litegraph.vector.index.type", indexType),
                    new KeyValuePair<string, object>("litegraph.vector.index.success", success)
                };

                _VectorIndexRebuildsCounter?.Add(1, tags);
                _VectorIndexRebuildVectorsCounter?.Add(vectorCount, tags);
                _VectorIndexRebuildDurationMs?.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record a retention sweep pass.
        /// </summary>
        /// <param name="component">Low-cardinality component label: request_history or chat_history.</param>
        /// <param name="success">Whether the sweep completed successfully.</param>
        /// <param name="deletedCount">Number of records deleted, when known; pass zero when the underlying delete does not report a count.</param>
        /// <param name="durationMs">Sweep duration in milliseconds.</param>
        public void RecordRetentionSweep(string component, bool success, long deletedCount, double durationMs)
        {
            if (!_Settings.Enable) return;

            component = NormalizeLabel(component);
            if (deletedCount < 0) deletedCount = 0;
            string[] labelNames = new string[] { "component", "success" };
            string[] labelValues = new string[] { component, (success ? "true" : "false") };
            OperationCounter("litegraph_retention_sweeps_total", "Total retention sweep passes executed by LiteGraph.", labelNames, labelValues).Add(1);
            OperationSummary("litegraph_retention_sweep_duration_ms", "Total and count of retention sweep durations in milliseconds.", labelNames, labelValues).Record(durationMs);
            OperationCounter("litegraph_retention_deleted_total", "Total records deleted by retention sweeps.", new string[] { "component" }, new string[] { component }).Add(deletedCount);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("component", component),
                    new KeyValuePair<string, object>("litegraph.retention.success", success)
                };

                _RetentionSweepsCounter?.Add(1, tags);
                _RetentionSweepDurationMs?.Record(durationMs, tags);
                _RetentionDeletedCounter?.Add(deletedCount, new KeyValuePair<string, object>("component", component));
            }
        }

        /// <summary>
        /// Record a request history capture dropped because the capture queue was full.
        /// </summary>
        public void RecordRequestHistoryDrop()
        {
            if (!_Settings.Enable) return;

            OperationCounter("litegraph_request_history_dropped_total", "Total request history captures dropped because the capture queue was full.", Array.Empty<string>(), Array.Empty<string>()).Add(1);

            if (_Settings.EnableOpenTelemetry)
            {
                _RequestHistoryDroppedCounter?.Add(1);
            }
        }

        #endregion

        #region Operations-Private-Methods

        private void InitializeOperationInstruments()
        {
            _BackupOperationsCounter = Meter.CreateCounter<long>("litegraph.backup.operations", "operations", "Total backup administration operations executed by LiteGraph.");
            _BackupOperationDurationMs = Meter.CreateHistogram<double>("litegraph.backup.operation.duration", "ms", "Backup operation duration in milliseconds.");
            _GraphImportRecordsCounter = Meter.CreateCounter<long>("litegraph.graph.import.records", "records", "Total records processed by JSONL graph imports.");
            _GraphImportWarningsCounter = Meter.CreateCounter<long>("litegraph.graph.import.warnings", "warnings", "Total warnings raised during JSONL graph imports.");
            _VectorIndexRebuildsCounter = Meter.CreateCounter<long>("litegraph.server.vector.index.rebuilds", "rebuilds", "Total vector index rebuilds observed by the LiteGraph server.");
            _VectorIndexRebuildVectorsCounter = Meter.CreateCounter<long>("litegraph.server.vector.index.rebuild.vectors", "vectors", "Total vectors added during vector index rebuilds observed by the LiteGraph server.");
            _VectorIndexRebuildDurationMs = Meter.CreateHistogram<double>("litegraph.server.vector.index.rebuild.duration", "ms", "Vector index rebuild duration in milliseconds observed by the LiteGraph server.");
            _RetentionSweepsCounter = Meter.CreateCounter<long>("litegraph.retention.sweeps", "sweeps", "Total retention sweep passes executed by LiteGraph.");
            _RetentionSweepDurationMs = Meter.CreateHistogram<double>("litegraph.retention.sweep.duration", "ms", "Retention sweep duration in milliseconds.");
            _RetentionDeletedCounter = Meter.CreateCounter<long>("litegraph.retention.deleted", "records", "Total records deleted by retention sweeps.");
            _RequestHistoryDroppedCounter = Meter.CreateCounter<long>("litegraph.request_history.dropped", "captures", "Total request history captures dropped because the capture queue was full.");
        }

        private LabeledCounter OperationCounter(string name, string help, string[] labelNames, string[] labelValues)
        {
            string key = name + "\n" + String.Join("\n", labelValues);
            return _OperationCounters.GetOrAdd(key, _ => new LabeledCounter(name, help, labelNames, labelValues));
        }

        private LabeledSummary OperationSummary(string name, string help, string[] labelNames, string[] labelValues)
        {
            string key = name + "\n" + String.Join("\n", labelValues);
            return _OperationSummaries.GetOrAdd(key, _ => new LabeledSummary(name, help, labelNames, labelValues));
        }

        private void RenderPrometheusOperations(StringBuilder sb)
        {
            List<IGrouping<string, LabeledCounter>> counterFamilies = _OperationCounters.Values.GroupBy(c => c.Name).ToList();
            foreach (IGrouping<string, LabeledCounter> family in counterFamilies)
            {
                LabeledCounter first = family.First();
                sb.AppendLine("# HELP " + first.Name + " " + first.Help);
                sb.AppendLine("# TYPE " + first.Name + " counter");
                foreach (LabeledCounter metric in family)
                {
                    sb.Append(metric.Name);
                    sb.Append(metric.LabelText());
                    sb.Append(' ');
                    sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
                }
            }

            List<IGrouping<string, LabeledSummary>> summaryFamilies = _OperationSummaries.Values.GroupBy(s => s.Name).ToList();
            foreach (IGrouping<string, LabeledSummary> family in summaryFamilies)
            {
                LabeledSummary first = family.First();
                sb.AppendLine("# HELP " + first.Name + " " + first.Help);
                sb.AppendLine("# TYPE " + first.Name + " summary");
                foreach (LabeledSummary metric in family)
                {
                    metric.Render(sb);
                }
            }
        }

        private void HandleVectorIndexRebuildRecorded(object sender, VectorIndexRebuildTelemetryEventArgs e)
        {
            if (e == null) return;
            RecordVectorIndexRebuild(e.IndexType, e.Success, e.VectorCount, e.DurationMs);
        }

        #endregion

        #region Operations-Metric-Classes

        private sealed class LabeledSummary
        {
            internal readonly string Name;
            internal readonly string Help;
            private readonly string _LabelText;
            private readonly object _Lock = new object();
            private long _Count = 0;
            private double _Sum = 0;

            internal LabeledSummary(string name, string help, string[] labelNames, string[] labelValues)
            {
                Name = name;
                Help = help;
                _LabelText = BuildLabelText(labelNames, labelValues);
            }

            internal void Record(double value)
            {
                if (value < 0) value = 0;

                lock (_Lock)
                {
                    _Count++;
                    _Sum += value;
                }
            }

            internal void Render(StringBuilder sb)
            {
                long count;
                double sum;

                lock (_Lock)
                {
                    count = _Count;
                    sum = _Sum;
                }

                sb.Append(Name);
                sb.Append("_sum");
                sb.Append(_LabelText);
                sb.Append(' ');
                sb.AppendLine(sum.ToString(CultureInfo.InvariantCulture));

                sb.Append(Name);
                sb.Append("_count");
                sb.Append(_LabelText);
                sb.Append(' ');
                sb.AppendLine(count.ToString(CultureInfo.InvariantCulture));
            }
        }

        #endregion
    }
}
