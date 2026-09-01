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
    /// Chat instruments: request, token, tool, retrieval, feedback, and endpoint-health metrics.
    /// Labels are deliberately low-cardinality (provider, model, tool name, endpoint name, status class);
    /// per-GUID detail belongs on trace spans and chat turn records, never on metric labels.
    /// </summary>
    public partial class ObservabilityService
    {
        #region Chat-Private-Members

        private static readonly double[] _ChatDurationBucketsMs = new double[] { 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000, 30000, 60000, 120000 };
        private static readonly double[] _ChatTokensPerSecondBuckets = new double[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000 };
        private static readonly double[] _ChatIterationBuckets = new double[] { 1, 2, 3, 5, 8, 13, 25 };

        private readonly ConcurrentDictionary<string, ChatLabeledCounter> _ChatCounters = new ConcurrentDictionary<string, ChatLabeledCounter>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ChatLabeledHistogram> _ChatHistograms = new ConcurrentDictionary<string, ChatLabeledHistogram>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ChatLabeledGauge> _ChatGauges = new ConcurrentDictionary<string, ChatLabeledGauge>(StringComparer.Ordinal);

        private Counter<long> _ChatRequestsCounter;
        private Counter<long> _ChatRequestErrorsCounter;
        private Histogram<double> _ChatRequestDurationMs;
        private Histogram<double> _ChatTtftMs;
        private Counter<long> _ChatPromptTokensCounter;
        private Counter<long> _ChatCompletionTokensCounter;
        private Histogram<double> _ChatTokensPerSecond;
        private Counter<long> _ChatToolCallsCounter;
        private Histogram<double> _ChatToolDurationMs;
        private Histogram<double> _ChatToolIterations;
        private Histogram<double> _ChatRagDurationMs;
        private Counter<long> _ChatEmbeddingRequestsCounter;
        private Histogram<double> _ChatEmbeddingDurationMs;
        private Counter<long> _ChatRetriesCounter;
        private Counter<long> _ChatFeedbackCounter;
        private Counter<long> _ChatHealthTransitionsCounter;
        private Histogram<double> _ChatHealthCheckDurationMs;
        private ObservableGauge<long> _ChatActiveGauge;
        private long _ActiveChats = 0;

        #endregion

        #region Chat-Public-Methods

        /// <summary>
        /// Record a chat completion request.
        /// </summary>
        /// <param name="provider">Provider label.</param>
        /// <param name="model">Model label.</param>
        /// <param name="streamed">Whether the client received a streamed response.</param>
        /// <param name="statusCode">HTTP status code of the response.</param>
        /// <param name="durationMs">Total turn duration in milliseconds.</param>
        /// <param name="ttftMs">Time to first token in milliseconds, when known.</param>
        /// <param name="promptTokens">Prompt tokens, when reported.</param>
        /// <param name="completionTokens">Completion tokens, when reported.</param>
        /// <param name="tokensPerSecond">Overall tokens per second, when known.</param>
        /// <param name="toolIterations">Tool loop iterations within the turn.</param>
        /// <param name="retryCount">Retries performed before the response started.</param>
        public void RecordChatRequest(
            string provider,
            string model,
            bool streamed,
            int statusCode,
            double durationMs,
            double? ttftMs,
            int? promptTokens,
            int? completionTokens,
            double? tokensPerSecond,
            int toolIterations,
            int retryCount)
        {
            if (!_Settings.Enable) return;

            provider = NormalizeLabel(provider);
            model = NormalizeLabel(model);
            string streamedLabel = (streamed ? "true" : "false");
            string statusClass = StatusClass(statusCode);
            bool isError = statusCode >= 400;

            string[] requestLabelNames = new string[] { "component", "provider", "model", "streamed", "status_class" };
            string[] requestLabelValues = new string[] { _RestComponent, provider, model, streamedLabel, statusClass };
            ChatCounter("litegraph_chat_requests_total", "Total chat completion requests processed by LiteGraph.", requestLabelNames, requestLabelValues).Add(1);
            if (isError) ChatCounter("litegraph_chat_request_errors_total", "Total chat completion requests that resulted in an error.", requestLabelNames, requestLabelValues).Add(1);
            ChatHistogram("litegraph_chat_request_duration_ms", "Chat completion duration in milliseconds.", _ChatDurationBucketsMs, requestLabelNames, requestLabelValues).Record(durationMs);

            string[] modelLabelNames = new string[] { "component", "provider", "model" };
            string[] modelLabelValues = new string[] { _RestComponent, provider, model };
            if (ttftMs != null) ChatHistogram("litegraph_chat_ttft_ms", "Chat time to first token in milliseconds.", _ChatDurationBucketsMs, modelLabelNames, modelLabelValues).Record(ttftMs.Value);
            if (promptTokens != null) ChatCounter("litegraph_chat_tokens_prompt_total", "Total prompt tokens consumed by chat completions.", modelLabelNames, modelLabelValues).Add(promptTokens.Value);
            if (completionTokens != null) ChatCounter("litegraph_chat_tokens_completion_total", "Total completion tokens produced by chat completions.", modelLabelNames, modelLabelValues).Add(completionTokens.Value);
            if (tokensPerSecond != null) ChatHistogram("litegraph_chat_tokens_per_second", "Overall chat tokens per second.", _ChatTokensPerSecondBuckets, modelLabelNames, modelLabelValues).Record(tokensPerSecond.Value);
            ChatHistogram("litegraph_chat_tool_iterations", "Tool loop iterations per chat turn.", _ChatIterationBuckets, modelLabelNames, modelLabelValues).Record(toolIterations);
            if (retryCount > 0) ChatCounter("litegraph_chat_retries_total", "Total chat completion retries.", new string[] { "component", "provider" }, new string[] { _RestComponent, provider }).Add(retryCount);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("component", _RestComponent),
                    new KeyValuePair<string, object>("provider", provider),
                    new KeyValuePair<string, object>("model", model),
                    new KeyValuePair<string, object>("streamed", streamedLabel),
                    new KeyValuePair<string, object>("status_class", statusClass)
                };

                _ChatRequestsCounter?.Add(1, tags);
                _ChatRequestDurationMs?.Record(durationMs, tags);
                if (isError) _ChatRequestErrorsCounter?.Add(1, tags);
                if (ttftMs != null) _ChatTtftMs?.Record(ttftMs.Value, tags);
                if (promptTokens != null) _ChatPromptTokensCounter?.Add(promptTokens.Value, tags);
                if (completionTokens != null) _ChatCompletionTokensCounter?.Add(completionTokens.Value, tags);
                if (tokensPerSecond != null) _ChatTokensPerSecond?.Record(tokensPerSecond.Value, tags);
                _ChatToolIterations?.Record(toolIterations, tags);
                if (retryCount > 0) _ChatRetriesCounter?.Add(retryCount, tags);
            }
        }

        /// <summary>
        /// Record a chat tool call executed by the in-process dispatcher.
        /// </summary>
        /// <param name="tool">Tool name.</param>
        /// <param name="success">Whether the tool call succeeded.</param>
        /// <param name="durationMs">Tool execution duration in milliseconds.</param>
        public void RecordChatToolCall(string tool, bool success, double durationMs)
        {
            if (!_Settings.Enable) return;

            tool = NormalizeLabel(tool);
            string[] labelNames = new string[] { "component", "tool", "success" };
            string[] labelValues = new string[] { _RestComponent, tool, (success ? "true" : "false") };
            ChatCounter("litegraph_chat_tool_calls_total", "Total chat tool calls executed.", labelNames, labelValues).Add(1);
            ChatHistogram("litegraph_chat_tool_duration_ms", "Chat tool call duration in milliseconds.", _ChatDurationBucketsMs, labelNames, labelValues).Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("component", _RestComponent),
                    new KeyValuePair<string, object>("tool", tool),
                    new KeyValuePair<string, object>("success", (success ? "true" : "false"))
                };

                _ChatToolCallsCounter?.Add(1, tags);
                _ChatToolDurationMs?.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record a chat retrieval (RAG) stage.
        /// </summary>
        /// <param name="durationMs">Retrieval duration in milliseconds.</param>
        public void RecordChatRag(double durationMs)
        {
            if (!_Settings.Enable) return;

            string[] labelNames = new string[] { "component" };
            string[] labelValues = new string[] { _RestComponent };
            ChatHistogram("litegraph_chat_rag_duration_ms", "Chat retrieval stage duration in milliseconds.", _ChatDurationBucketsMs, labelNames, labelValues).Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                _ChatRagDurationMs?.Record(durationMs, new KeyValuePair<string, object>("component", _RestComponent));
            }
        }

        /// <summary>
        /// Record a chat embedding request.
        /// </summary>
        /// <param name="provider">Provider label.</param>
        /// <param name="model">Model label.</param>
        /// <param name="success">Whether the embedding request succeeded.</param>
        /// <param name="durationMs">Embedding duration in milliseconds.</param>
        public void RecordChatEmbedding(string provider, string model, bool success, double durationMs)
        {
            if (!_Settings.Enable) return;

            provider = NormalizeLabel(provider);
            model = NormalizeLabel(model);
            string[] labelNames = new string[] { "component", "provider", "model", "success" };
            string[] labelValues = new string[] { _RestComponent, provider, model, (success ? "true" : "false") };
            ChatCounter("litegraph_chat_embedding_requests_total", "Total chat embedding requests.", labelNames, labelValues).Add(1);
            ChatHistogram("litegraph_chat_embedding_duration_ms", "Chat embedding duration in milliseconds.", _ChatDurationBucketsMs, labelNames, labelValues).Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("component", _RestComponent),
                    new KeyValuePair<string, object>("provider", provider),
                    new KeyValuePair<string, object>("model", model),
                    new KeyValuePair<string, object>("success", (success ? "true" : "false"))
                };

                _ChatEmbeddingRequestsCounter?.Add(1, tags);
                _ChatEmbeddingDurationMs?.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record submitted chat feedback.
        /// </summary>
        /// <param name="rating">Feedback rating.</param>
        public void RecordChatFeedback(ChatFeedbackRatingEnum rating)
        {
            if (!_Settings.Enable) return;

            string ratingLabel = rating.ToString();
            ChatCounter("litegraph_chat_feedback_total", "Total chat feedback submissions.", new string[] { "component", "rating" }, new string[] { _RestComponent, ratingLabel }).Add(1);

            if (_Settings.EnableOpenTelemetry)
            {
                _ChatFeedbackCounter?.Add(1,
                    new KeyValuePair<string, object>("component", _RestComponent),
                    new KeyValuePair<string, object>("rating", ratingLabel));
            }
        }

        /// <summary>
        /// Record a chat endpoint health probe.
        /// </summary>
        /// <param name="endpointName">Endpoint name label.</param>
        /// <param name="endpointType">Endpoint type.</param>
        /// <param name="success">Whether the probe succeeded.</param>
        /// <param name="durationMs">Probe duration in milliseconds.</param>
        public void RecordChatHealthProbe(string endpointName, ChatEndpointTypeEnum endpointType, bool success, double durationMs)
        {
            if (!_Settings.Enable) return;

            endpointName = NormalizeLabel(endpointName);
            string[] labelNames = new string[] { "component", "endpoint", "endpoint_type", "success" };
            string[] labelValues = new string[] { _RestComponent, endpointName, endpointType.ToString(), (success ? "true" : "false") };
            ChatHistogram("litegraph_chat_healthcheck_duration_ms", "Chat endpoint health probe duration in milliseconds.", _ChatDurationBucketsMs, labelNames, labelValues).Record(durationMs);

            if (_Settings.EnableOpenTelemetry)
            {
                KeyValuePair<string, object>[] tags =
                {
                    new KeyValuePair<string, object>("component", _RestComponent),
                    new KeyValuePair<string, object>("endpoint", endpointName),
                    new KeyValuePair<string, object>("endpoint_type", endpointType.ToString()),
                    new KeyValuePair<string, object>("success", (success ? "true" : "false"))
                };

                _ChatHealthCheckDurationMs?.Record(durationMs, tags);
            }
        }

        /// <summary>
        /// Record a chat endpoint health state transition.
        /// </summary>
        /// <param name="endpointName">Endpoint name label.</param>
        /// <param name="healthy">The state transitioned to.</param>
        public void RecordChatHealthTransition(string endpointName, bool healthy)
        {
            if (!_Settings.Enable) return;

            endpointName = NormalizeLabel(endpointName);
            string toState = (healthy ? "healthy" : "unhealthy");
            ChatCounter("litegraph_chat_healthcheck_transitions_total", "Total chat endpoint health state transitions.", new string[] { "component", "endpoint", "to_state" }, new string[] { _RestComponent, endpointName, toState }).Add(1);

            if (_Settings.EnableOpenTelemetry)
            {
                _ChatHealthTransitionsCounter?.Add(1,
                    new KeyValuePair<string, object>("component", _RestComponent),
                    new KeyValuePair<string, object>("endpoint", endpointName),
                    new KeyValuePair<string, object>("to_state", toState));
            }
        }

        /// <summary>
        /// Publish a chat endpoint's health state as a gauge.  Pass null to remove the series (endpoint deleted or unmonitored).
        /// </summary>
        /// <param name="endpointName">Endpoint name label.</param>
        /// <param name="endpointType">Endpoint type.</param>
        /// <param name="healthy">Health verdict, or null to remove.</param>
        public void SetChatEndpointHealth(string endpointName, ChatEndpointTypeEnum endpointType, bool? healthy)
        {
            if (!_Settings.Enable) return;

            endpointName = NormalizeLabel(endpointName);
            string[] labelNames = new string[] { "component", "endpoint", "endpoint_type" };
            string[] labelValues = new string[] { _RestComponent, endpointName, endpointType.ToString() };
            string key = "litegraph_chat_endpoint_healthy\n" + String.Join("\n", labelValues);

            if (healthy == null)
            {
                _ChatGauges.TryRemove(key, out _);
                return;
            }

            ChatLabeledGauge gauge = _ChatGauges.GetOrAdd(key, _ => new ChatLabeledGauge("litegraph_chat_endpoint_healthy", "Chat endpoint health state (1 healthy, 0 unhealthy).", labelNames, labelValues));
            gauge.Value = (healthy.Value ? 1 : 0);
        }

        /// <summary>
        /// Increment the in-flight chat completion gauge.  Pair every call with <see cref="DecrementChatActive"/>.
        /// </summary>
        public void IncrementChatActive()
        {
            if (!_Settings.Enable) return;
            Interlocked.Increment(ref _ActiveChats);
        }

        /// <summary>
        /// Decrement the in-flight chat completion gauge.
        /// </summary>
        public void DecrementChatActive()
        {
            if (!_Settings.Enable) return;
            Interlocked.Decrement(ref _ActiveChats);
        }

        #endregion

        #region Chat-Private-Methods

        private void InitializeChatInstruments()
        {
            _ChatRequestsCounter = Meter.CreateCounter<long>("litegraph.chat.requests", "requests", "Total chat completion requests processed by LiteGraph.");
            _ChatRequestErrorsCounter = Meter.CreateCounter<long>("litegraph.chat.request.errors", "errors", "Total chat completion requests that resulted in an error.");
            _ChatRequestDurationMs = Meter.CreateHistogram<double>("litegraph.chat.request.duration", "ms", "Chat completion duration in milliseconds.");
            _ChatTtftMs = Meter.CreateHistogram<double>("litegraph.chat.ttft", "ms", "Chat time to first token in milliseconds.");
            _ChatPromptTokensCounter = Meter.CreateCounter<long>("litegraph.chat.tokens.prompt", "tokens", "Total prompt tokens consumed by chat completions.");
            _ChatCompletionTokensCounter = Meter.CreateCounter<long>("litegraph.chat.tokens.completion", "tokens", "Total completion tokens produced by chat completions.");
            _ChatTokensPerSecond = Meter.CreateHistogram<double>("litegraph.chat.tokens_per_second", "tokens/s", "Overall chat tokens per second.");
            _ChatToolCallsCounter = Meter.CreateCounter<long>("litegraph.chat.tool.calls", "calls", "Total chat tool calls executed.");
            _ChatToolDurationMs = Meter.CreateHistogram<double>("litegraph.chat.tool.duration", "ms", "Chat tool call duration in milliseconds.");
            _ChatToolIterations = Meter.CreateHistogram<double>("litegraph.chat.tool.iterations", "iterations", "Tool loop iterations per chat turn.");
            _ChatRagDurationMs = Meter.CreateHistogram<double>("litegraph.chat.rag.duration", "ms", "Chat retrieval stage duration in milliseconds.");
            _ChatEmbeddingRequestsCounter = Meter.CreateCounter<long>("litegraph.chat.embedding.requests", "requests", "Total chat embedding requests.");
            _ChatEmbeddingDurationMs = Meter.CreateHistogram<double>("litegraph.chat.embedding.duration", "ms", "Chat embedding duration in milliseconds.");
            _ChatRetriesCounter = Meter.CreateCounter<long>("litegraph.chat.retries", "retries", "Total chat completion retries.");
            _ChatFeedbackCounter = Meter.CreateCounter<long>("litegraph.chat.feedback", "submissions", "Total chat feedback submissions.");
            _ChatHealthTransitionsCounter = Meter.CreateCounter<long>("litegraph.chat.healthcheck.transitions", "transitions", "Total chat endpoint health state transitions.");
            _ChatHealthCheckDurationMs = Meter.CreateHistogram<double>("litegraph.chat.healthcheck.duration", "ms", "Chat endpoint health probe duration in milliseconds.");
            _ChatActiveGauge = Meter.CreateObservableGauge<long>("litegraph.chat.active", ObserveActiveChats, "requests", "Currently in-flight chat completions.");
        }

        private IEnumerable<Measurement<long>> ObserveActiveChats()
        {
            yield return new Measurement<long>(
                Interlocked.Read(ref _ActiveChats),
                new KeyValuePair<string, object>("component", _RestComponent));
        }

        private ChatLabeledCounter ChatCounter(string name, string help, string[] labelNames, string[] labelValues)
        {
            string key = name + "\n" + String.Join("\n", labelValues);
            return _ChatCounters.GetOrAdd(key, _ => new ChatLabeledCounter(name, help, labelNames, labelValues));
        }

        private ChatLabeledHistogram ChatHistogram(string name, string help, double[] buckets, string[] labelNames, string[] labelValues)
        {
            string key = name + "\n" + String.Join("\n", labelValues);
            return _ChatHistograms.GetOrAdd(key, _ => new ChatLabeledHistogram(name, help, buckets, labelNames, labelValues));
        }

        private void RenderPrometheusChat(StringBuilder sb)
        {
            List<IGrouping<string, ChatLabeledCounter>> counterFamilies = _ChatCounters.Values.GroupBy(c => c.Name).ToList();
            foreach (IGrouping<string, ChatLabeledCounter> family in counterFamilies)
            {
                ChatLabeledCounter first = family.First();
                sb.AppendLine("# HELP " + first.Name + " " + first.Help);
                sb.AppendLine("# TYPE " + first.Name + " counter");
                foreach (ChatLabeledCounter metric in family)
                {
                    sb.Append(metric.Name);
                    sb.Append(metric.LabelText());
                    sb.Append(' ');
                    sb.AppendLine(metric.Count.ToString(CultureInfo.InvariantCulture));
                }
            }

            List<IGrouping<string, ChatLabeledHistogram>> histogramFamilies = _ChatHistograms.Values.GroupBy(h => h.Name).ToList();
            foreach (IGrouping<string, ChatLabeledHistogram> family in histogramFamilies)
            {
                ChatLabeledHistogram first = family.First();
                sb.AppendLine("# HELP " + first.Name + " " + first.Help);
                sb.AppendLine("# TYPE " + first.Name + " histogram");
                foreach (ChatLabeledHistogram metric in family)
                {
                    metric.Render(sb);
                }
            }

            List<IGrouping<string, ChatLabeledGauge>> gaugeFamilies = _ChatGauges.Values.GroupBy(g => g.Name).ToList();
            foreach (IGrouping<string, ChatLabeledGauge> family in gaugeFamilies)
            {
                ChatLabeledGauge first = family.First();
                sb.AppendLine("# HELP " + first.Name + " " + first.Help);
                sb.AppendLine("# TYPE " + first.Name + " gauge");
                foreach (ChatLabeledGauge metric in family)
                {
                    sb.Append(metric.Name);
                    sb.Append(metric.LabelText());
                    sb.Append(' ');
                    sb.AppendLine(metric.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            sb.AppendLine("# HELP litegraph_chat_active Currently in-flight chat completions.");
            sb.AppendLine("# TYPE litegraph_chat_active gauge");
            sb.Append("litegraph_chat_active{component=\"" + _RestComponent + "\"} ");
            sb.AppendLine(Interlocked.Read(ref _ActiveChats).ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Chat-Metric-Classes

        private sealed class ChatLabeledCounter
        {
            internal readonly string Name;
            internal readonly string Help;
            private readonly string _LabelText;
            private long _Count = 0;

            internal ChatLabeledCounter(string name, string help, string[] labelNames, string[] labelValues)
            {
                Name = name;
                Help = help;
                _LabelText = BuildLabelText(labelNames, labelValues);
            }

            internal long Count
            {
                get
                {
                    return Interlocked.Read(ref _Count);
                }
            }

            internal void Add(long value)
            {
                Interlocked.Add(ref _Count, value);
            }

            internal string LabelText()
            {
                return _LabelText;
            }
        }

        private sealed class ChatLabeledHistogram
        {
            internal readonly string Name;
            internal readonly string Help;
            private readonly string _LabelText;
            private readonly string[] _LabelNames;
            private readonly string[] _LabelValues;
            private readonly double[] _Buckets;
            private readonly long[] _BucketCounts;
            private long _Count = 0;
            private double _Sum = 0;
            private readonly object _Lock = new object();

            internal ChatLabeledHistogram(string name, string help, double[] buckets, string[] labelNames, string[] labelValues)
            {
                Name = name;
                Help = help;
                _Buckets = buckets;
                _BucketCounts = new long[buckets.Length];
                _LabelNames = labelNames;
                _LabelValues = labelValues;
                _LabelText = BuildLabelText(labelNames, labelValues);
            }

            internal void Record(double value)
            {
                lock (_Lock)
                {
                    _Count++;
                    _Sum += value;
                    for (int i = 0; i < _Buckets.Length; i++)
                    {
                        if (value <= _Buckets[i]) _BucketCounts[i]++;
                    }
                }
            }

            internal void Render(StringBuilder sb)
            {
                long count;
                double sum;
                long[] bucketCounts = new long[_BucketCounts.Length];

                lock (_Lock)
                {
                    count = _Count;
                    sum = _Sum;
                    Array.Copy(_BucketCounts, bucketCounts, _BucketCounts.Length);
                }

                for (int i = 0; i < _Buckets.Length; i++)
                {
                    sb.Append(Name);
                    sb.Append("_bucket");
                    sb.Append(BuildLabelText(_LabelNames, _LabelValues, _Buckets[i].ToString(CultureInfo.InvariantCulture)));
                    sb.Append(' ');
                    sb.AppendLine(bucketCounts[i].ToString(CultureInfo.InvariantCulture));
                }

                sb.Append(Name);
                sb.Append("_bucket");
                sb.Append(BuildLabelText(_LabelNames, _LabelValues, "+Inf"));
                sb.Append(' ');
                sb.AppendLine(count.ToString(CultureInfo.InvariantCulture));

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

        private sealed class ChatLabeledGauge
        {
            internal readonly string Name;
            internal readonly string Help;
            private readonly string _LabelText;
            private long _Value = 0;

            internal ChatLabeledGauge(string name, string help, string[] labelNames, string[] labelValues)
            {
                Name = name;
                Help = help;
                _LabelText = BuildLabelText(labelNames, labelValues);
            }

            internal long Value
            {
                get
                {
                    return Interlocked.Read(ref _Value);
                }
                set
                {
                    Interlocked.Exchange(ref _Value, value);
                }
            }

            internal string LabelText()
            {
                return _LabelText;
            }
        }

        private static string BuildLabelText(string[] labelNames, string[] labelValues, string leBucket = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < labelNames.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(labelNames[i]);
                sb.Append("=\"");
                sb.Append(labelValues[i].Replace("\\", "\\\\").Replace("\"", "\\\""));
                sb.Append('"');
            }

            if (leBucket != null)
            {
                if (labelNames.Length > 0) sb.Append(',');
                sb.Append("le=\"");
                sb.Append(leBucket);
                sb.Append('"');
            }

            sb.Append('}');
            return sb.ToString();
        }

        #endregion
    }
}
