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
    constructor(turn?: {
        GUID?: string;
        TenantGUID?: string;
        ThreadGUID?: string;
        UserMessage?: string | null;
        AssistantResponse?: string | null;
        Reasoning?: string | null;
        ToolTranscriptJson?: string | null;
        TelemetryJson?: string | null;
        TraceId?: string | null;
        CompletionEndpointGUID?: string | null;
        EmbeddingEndpointGUID?: string | null;
        Provider?: string;
        Model?: string | null;
        EmbeddingDurationMs?: number | null;
        RetrievalDurationMs?: number | null;
        RetrievedChunkCount?: number;
        ToolLoopIterations?: number;
        ToolCallCount?: number;
        LimiterWaitMs?: number | null;
        InferenceConnectionMs?: number | null;
        TimeToFirstTokenMs?: number | null;
        TimeToLastTokenMs?: number | null;
        TotalDurationMs?: number;
        PromptTokens?: number | null;
        CompletionTokens?: number | null;
        TokensPerSecondOverall?: number | null;
        TokensPerSecondGeneration?: number | null;
        RetryCount?: number;
        Success?: boolean;
        HttpStatus?: number | null;
        Error?: string | null;
        CreatedUtc?: Date | string;
    });
    GUID: string;
    TenantGUID: string;
    ThreadGUID: string;
    UserMessage: string;
    AssistantResponse: string;
    Reasoning: string;
    ToolTranscriptJson: string;
    TelemetryJson: string;
    TraceId: string;
    CompletionEndpointGUID: string;
    EmbeddingEndpointGUID: string;
    Provider: string;
    Model: string;
    EmbeddingDurationMs: number;
    RetrievalDurationMs: number;
    RetrievedChunkCount: number;
    ToolLoopIterations: number;
    ToolCallCount: number;
    LimiterWaitMs: number;
    InferenceConnectionMs: number;
    TimeToFirstTokenMs: number;
    TimeToLastTokenMs: number;
    TotalDurationMs: number;
    PromptTokens: number;
    CompletionTokens: number;
    TokensPerSecondOverall: number;
    TokensPerSecondGeneration: number;
    RetryCount: number;
    Success: boolean;
    HttpStatus: number;
    Error: string;
    CreatedUtc: Date;
}
