import { v4 as uuidV4 } from 'uuid';

/**
 * ChatTurn class representing a single request/response exchange within a chat thread.
 */
export default class ChatTurn {
  /**
   * @param {Object} turn - Information about the chat turn.
   * @param {string} [turn.GUID] - Globally unique identifier for the chat turn (automatically generated if not provided).
   * @param {string} [turn.TenantGUID] - Globally unique identifier for the tenant.
   * @param {string} [turn.ThreadGUID] - Globally unique identifier for the parent thread.
   * @param {string|null} [turn.UserMessage=null] - The user's message.
   * @param {string|null} [turn.AssistantResponse=null] - The assistant's response.
   * @param {string|null} [turn.Reasoning=null] - Reasoning/thinking content, if any.
   * @param {string|null} [turn.ToolTranscriptJson=null] - JSON transcript of tool calls, if any.
   * @param {string|null} [turn.TelemetryJson=null] - JSON telemetry payload, if any.
   * @param {string|null} [turn.TraceId=null] - Trace identifier.
   * @param {string|null} [turn.CompletionEndpointGUID=null] - Completion endpoint GUID used.
   * @param {string|null} [turn.EmbeddingEndpointGUID=null] - Embedding endpoint GUID used.
   * @param {string} [turn.Provider='OpenAI'] - Provider type used for the completion.
   * @param {string|null} [turn.Model=null] - Model name used.
   * @param {number|null} [turn.EmbeddingDurationMs=null] - Embedding duration in milliseconds.
   * @param {number|null} [turn.RetrievalDurationMs=null] - Retrieval duration in milliseconds.
   * @param {number} [turn.RetrievedChunkCount=0] - Number of retrieved RAG chunks.
   * @param {number} [turn.ToolLoopIterations=0] - Number of tool loop iterations.
   * @param {number} [turn.ToolCallCount=0] - Number of tool calls.
   * @param {number|null} [turn.LimiterWaitMs=null] - Concurrency limiter wait in milliseconds.
   * @param {number|null} [turn.InferenceConnectionMs=null] - Inference connection time in milliseconds.
   * @param {number|null} [turn.TimeToFirstTokenMs=null] - Time to first token in milliseconds.
   * @param {number|null} [turn.TimeToLastTokenMs=null] - Time to last token in milliseconds.
   * @param {number} [turn.TotalDurationMs=0] - Total duration in milliseconds.
   * @param {number|null} [turn.PromptTokens=null] - Prompt token count.
   * @param {number|null} [turn.CompletionTokens=null] - Completion token count.
   * @param {number|null} [turn.TokensPerSecondOverall=null] - Overall tokens per second.
   * @param {number|null} [turn.TokensPerSecondGeneration=null] - Generation tokens per second.
   * @param {number} [turn.RetryCount=0] - Number of retries performed.
   * @param {boolean} [turn.Success=true] - Indicates whether the turn succeeded (default is true).
   * @param {number|null} [turn.HttpStatus=null] - Upstream HTTP status, if any.
   * @param {string|null} [turn.Error=null] - Error message, if any.
   * @param {Date|string} [turn.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   */
  constructor(turn = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = null,
      ThreadGUID = null,
      UserMessage = null,
      AssistantResponse = null,
      Reasoning = null,
      ToolTranscriptJson = null,
      TelemetryJson = null,
      TraceId = null,
      CompletionEndpointGUID = null,
      EmbeddingEndpointGUID = null,
      Provider = 'OpenAI',
      Model = null,
      EmbeddingDurationMs = null,
      RetrievalDurationMs = null,
      RetrievedChunkCount = 0,
      ToolLoopIterations = 0,
      ToolCallCount = 0,
      LimiterWaitMs = null,
      InferenceConnectionMs = null,
      TimeToFirstTokenMs = null,
      TimeToLastTokenMs = null,
      TotalDurationMs = 0,
      PromptTokens = null,
      CompletionTokens = null,
      TokensPerSecondOverall = null,
      TokensPerSecondGeneration = null,
      RetryCount = 0,
      Success = true,
      HttpStatus = null,
      Error = null,
      CreatedUtc = new Date().toISOString(),
    } = turn;

    this.GUID = GUID; // Unique identifier for the chat turn
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.ThreadGUID = ThreadGUID; // Unique identifier for the parent thread
    this.UserMessage = UserMessage; // User message
    this.AssistantResponse = AssistantResponse; // Assistant response
    this.Reasoning = Reasoning; // Reasoning content
    this.ToolTranscriptJson = ToolTranscriptJson; // Tool call transcript JSON
    this.TelemetryJson = TelemetryJson; // Telemetry JSON
    this.TraceId = TraceId; // Trace identifier
    this.CompletionEndpointGUID = CompletionEndpointGUID; // Completion endpoint GUID
    this.EmbeddingEndpointGUID = EmbeddingEndpointGUID; // Embedding endpoint GUID
    this.Provider = Provider; // Provider type
    this.Model = Model; // Model name
    this.EmbeddingDurationMs = EmbeddingDurationMs; // Embedding duration
    this.RetrievalDurationMs = RetrievalDurationMs; // Retrieval duration
    this.RetrievedChunkCount = RetrievedChunkCount; // Retrieved chunk count
    this.ToolLoopIterations = ToolLoopIterations; // Tool loop iterations
    this.ToolCallCount = ToolCallCount; // Tool call count
    this.LimiterWaitMs = LimiterWaitMs; // Limiter wait
    this.InferenceConnectionMs = InferenceConnectionMs; // Inference connection time
    this.TimeToFirstTokenMs = TimeToFirstTokenMs; // Time to first token
    this.TimeToLastTokenMs = TimeToLastTokenMs; // Time to last token
    this.TotalDurationMs = TotalDurationMs; // Total duration
    this.PromptTokens = PromptTokens; // Prompt token count
    this.CompletionTokens = CompletionTokens; // Completion token count
    this.TokensPerSecondOverall = TokensPerSecondOverall; // Overall tokens per second
    this.TokensPerSecondGeneration = TokensPerSecondGeneration; // Generation tokens per second
    this.RetryCount = RetryCount; // Retry count
    this.Success = Success; // Indicates if the turn succeeded
    this.HttpStatus = HttpStatus; // Upstream HTTP status
    this.Error = Error; // Error message
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
  }
}
