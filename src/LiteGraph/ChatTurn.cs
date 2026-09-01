namespace LiteGraph
{
    using System;

    /// <summary>
    /// Chat turn.  One user message and its assistant response within a thread, with per-stage telemetry.
    /// </summary>
    public class ChatTurn
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Thread GUID.
        /// </summary>
        public Guid ThreadGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Zero-based position of the turn within its thread.  Minimum is 0.
        /// </summary>
        public int Sequence
        {
            get
            {
                return _Sequence;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Sequence));
                _Sequence = value;
            }
        }

        /// <summary>
        /// User message.
        /// </summary>
        public string UserMessage { get; set; } = null;

        /// <summary>
        /// Assistant response.  Partial content is preserved when a turn fails mid-stream.
        /// </summary>
        public string AssistantResponse { get; set; } = null;

        /// <summary>
        /// Model reasoning ("thinking") text.  Null when the model emitted none.
        /// </summary>
        public string Reasoning { get; set; } = null;

        /// <summary>
        /// Ordered record of tool calls and results within the turn, serialized as JSON.
        /// Deliberate unmanaged-JSON exception: variable-shape display payload, never queried relationally.
        /// </summary>
        public string ToolTranscriptJson { get; set; } = null;

        /// <summary>
        /// Full per-stage timing detail, serialized as JSON.
        /// Deliberate unmanaged-JSON exception: variable-shape display payload, never queried relationally.
        /// </summary>
        public string TelemetryJson { get; set; } = null;

        /// <summary>
        /// Trace identifier correlating the turn with distributed traces and request history.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// GUID of the completion endpoint used.  Null when the turn failed before endpoint resolution.
        /// </summary>
        public Guid? CompletionEndpointGUID { get; set; } = null;

        /// <summary>
        /// GUID of the embedding endpoint used for retrieval.  Null when retrieval did not run.
        /// </summary>
        public Guid? EmbeddingEndpointGUID { get; set; } = null;

        /// <summary>
        /// Provider of the completion endpoint.
        /// </summary>
        public ChatProviderTypeEnum Provider { get; set; } = ChatProviderTypeEnum.OpenAI;

        /// <summary>
        /// Model that generated the response.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Time spent generating the query embedding, in milliseconds.  Null when retrieval did not run.
        /// </summary>
        public double? EmbeddingDurationMs { get; set; } = null;

        /// <summary>
        /// Time spent on vector retrieval, in milliseconds.  Null when retrieval did not run.
        /// </summary>
        public double? RetrievalDurationMs { get; set; } = null;

        /// <summary>
        /// Number of retrieved context chunks injected into the prompt.
        /// </summary>
        public int RetrievedChunkCount { get; set; } = 0;

        /// <summary>
        /// Number of tool loop iterations (model calls) within the turn.
        /// </summary>
        public int ToolLoopIterations { get; set; } = 0;

        /// <summary>
        /// Number of tool calls executed within the turn.
        /// </summary>
        public int ToolCallCount { get; set; } = 0;

        /// <summary>
        /// Time spent waiting on the endpoint concurrency limiter, in milliseconds.
        /// </summary>
        public double? LimiterWaitMs { get; set; } = null;

        /// <summary>
        /// Time from request start to response headers on the final inference call, in milliseconds.
        /// </summary>
        public double? InferenceConnectionMs { get; set; } = null;

        /// <summary>
        /// Time to first token on the final inference call, in milliseconds.
        /// </summary>
        public double? TimeToFirstTokenMs { get; set; } = null;

        /// <summary>
        /// Time to last token on the final inference call, in milliseconds.
        /// </summary>
        public double? TimeToLastTokenMs { get; set; } = null;

        /// <summary>
        /// Total wall-clock duration of the turn, in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; set; } = 0;

        /// <summary>
        /// Prompt tokens reported by the provider.  Null when the provider reported no usage.
        /// </summary>
        public int? PromptTokens { get; set; } = null;

        /// <summary>
        /// Completion tokens reported by the provider.  Null when the provider reported no usage.
        /// </summary>
        public int? CompletionTokens { get; set; } = null;

        /// <summary>
        /// Tokens per second across the whole response, including time to first token.
        /// </summary>
        public double? TokensPerSecondOverall { get; set; } = null;

        /// <summary>
        /// Tokens per second between the first and last token.
        /// </summary>
        public double? TokensPerSecondGeneration { get; set; } = null;

        /// <summary>
        /// Number of retries performed before the response started.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Whether the turn completed successfully.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// HTTP status returned by the upstream provider on failure.  Null on success.
        /// </summary>
        public int? HttpStatus { get; set; } = null;

        /// <summary>
        /// Error message.  Null on success.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private int _Sequence = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatTurn()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
