import { sdk } from './litegraph.service';

/** Provider types supported by chat endpoints. */
export type ChatProviderType = 'OpenAI' | 'Ollama' | 'Gemini' | 'Anthropic' | 'VoyageAI';

/** Endpoint purpose: completion (chat) or embedding (RAG). */
export type ChatEndpointType = 'Completion' | 'Embedding';

/** Feedback rating for an assistant turn. */
export type ChatFeedbackRating = 'ThumbsUp' | 'ThumbsDown';

/** A tenant-scoped chat endpoint (LLM connection). ApiKey is always redacted in responses. */
export type ChatEndpoint = {
  GUID: string;
  TenantGUID: string;
  Name: string;
  EndpointType: ChatEndpointType;
  Provider: ChatProviderType;
  Endpoint: string;
  ApiKey?: string | null;
  Model: string;
  ContextWindowTokens?: number;
  MaxOutputTokens?: number;
  TimeoutMs?: number;
  MaxConcurrentRequests?: number;
  Active: boolean;
  HealthCheckEnabled?: boolean;
  HealthCheckUrl?: string | null;
  HealthCheckUseAuth?: boolean;
  HealthCheckIntervalMs?: number;
  HealthCheckTimeoutMs?: number;
  HealthCheckExpectedStatusCode?: number;
  HealthyThreshold?: number;
  UnhealthyThreshold?: number;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
};

/** Result of a connectivity test against a chat endpoint. */
export type ChatEndpointTestResult = {
  Reachable: boolean;
  Models?: string[] | null;
  ModelExists?: boolean | null;
  Error?: string | null;
  RuntimeMs: number;
};

/** One background health-check sample. */
export type ChatEndpointHealthSample = {
  TimestampUtc: string;
  Success: boolean;
  DurationMs: number;
};

/** Health snapshot for a chat endpoint. */
export type ChatEndpointHealth = {
  EndpointGUID: string;
  TenantGUID: string;
  Name: string;
  EndpointType: ChatEndpointType;
  Monitored: boolean;
  Healthy?: boolean | null;
  LastCheckedUtc?: string | null;
  LastError?: string | null;
  ConsecutiveSuccesses: number;
  ConsecutiveFailures: number;
  UptimePercentage?: number | null;
  CheckHistory: ChatEndpointHealthSample[];
};

/** A chat conversation thread owned by a user. */
export type ChatThread = {
  GUID: string;
  TenantGUID: string;
  UserGUID: string;
  GraphGUID?: string | null;
  Title?: string | null;
  CreatedUtc: string;
  LastUpdateUtc: string;
};

/** A single user/assistant exchange with its full metric set. */
export type ChatTurn = {
  GUID: string;
  TenantGUID: string;
  ThreadGUID: string;
  UserMessage?: string | null;
  AssistantResponse?: string | null;
  Reasoning?: string | null;
  ToolTranscriptJson?: string | null;
  TelemetryJson?: string | null;
  TraceId?: string | null;
  CompletionEndpointGUID?: string | null;
  EmbeddingEndpointGUID?: string | null;
  Provider: ChatProviderType;
  Model?: string | null;
  EmbeddingDurationMs?: number | null;
  RetrievalDurationMs?: number | null;
  RetrievedChunkCount: number;
  ToolLoopIterations: number;
  ToolCallCount: number;
  LimiterWaitMs?: number | null;
  InferenceConnectionMs?: number | null;
  TimeToFirstTokenMs?: number | null;
  TimeToLastTokenMs?: number | null;
  TotalDurationMs: number;
  PromptTokens?: number | null;
  CompletionTokens?: number | null;
  TokensPerSecondOverall?: number | null;
  TokensPerSecondGeneration?: number | null;
  RetryCount: number;
  Success: boolean;
  HttpStatus?: number | null;
  Error?: string | null;
  CreatedUtc: string;
};

/** Feedback attached to an assistant turn. */
export type ChatFeedback = {
  GUID: string;
  TenantGUID: string;
  ThreadGUID: string;
  TurnGUID: string;
  UserGUID: string;
  Rating: ChatFeedbackRating;
  FeedbackText?: string | null;
  CreatedUtc: string;
};

/** Tenant-level chat configuration. */
export type ChatSettings = {
  TenantGUID: string;
  DefaultCompletionEndpointGUID?: string | null;
  DefaultEmbeddingEndpointGUID?: string | null;
  SystemPrompt?: string | null;
  EnableChat: boolean;
  EnableTools: boolean;
  EnableMutationTools: boolean;
  MaxToolIterations: number;
  EnableRag: boolean;
  RagTopK: number;
  RagScoreThreshold: number;
  HistoryRetentionDays: number;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
};

/** Body for POST /chat/completions. */
export type ChatCompletionRequest = {
  ThreadGUID?: string | null;
  GraphGUID?: string | null;
  Message: string;
  Stream?: boolean;
  CompletionEndpointGUID?: string | null;
  EmbeddingEndpointGUID?: string | null;
  Temperature?: number | null;
  MaxOutputTokens?: number | null;
  EnableTools?: boolean | null;
  EnableRag?: boolean | null;
  RagTopK?: number | null;
  SystemPrompt?: string | null;
};

/** Final metrics for a completed (or failed) completion. */
export type ChatCompletionResult = {
  ThreadGUID: string;
  TurnGUID: string;
  Message?: string | null;
  Reasoning?: string | null;
  Provider: ChatProviderType;
  Model?: string | null;
  PromptTokens?: number | null;
  CompletionTokens?: number | null;
  TimeToFirstTokenMs?: number | null;
  TimeToLastTokenMs?: number | null;
  TotalDurationMs: number;
  TokensPerSecondOverall?: number | null;
  ToolCallCount: number;
  ToolLoopIterations: number;
  RetrievedChunkCount: number;
  RetryCount: number;
};

/** Sentinel value used by the server when redacting stored API keys. */
export const isRedactedApiKey = (value?: string | null): boolean => {
  return !!value && value.startsWith('********');
};

/**
 * Validates a provider/endpoint-type combination, mirroring the server rules:
 * Anthropic does not offer embeddings and VoyageAI does not offer completions.
 * Returns null when valid, or an error code string when invalid.
 */
export const validateProviderTypeCombo = (
  provider: ChatProviderType,
  endpointType: ChatEndpointType
): 'anthropicEmbedding' | 'voyageCompletion' | null => {
  if (provider === 'Anthropic' && endpointType === 'Embedding') return 'anthropicEmbedding';
  if (provider === 'VoyageAI' && endpointType === 'Completion') return 'voyageCompletion';
  return null;
};

/** All providers valid for a given endpoint type. */
/** Canonical base-URL example per provider, matching PolyPrompt's defaults. */
export const PROVIDER_BASE_URL_EXAMPLES: Record<ChatProviderType, string> = {
  OpenAI: 'https://api.openai.com',
  Ollama: 'http://127.0.0.1:11434',
  Gemini: 'https://generativelanguage.googleapis.com',
  Anthropic: 'https://api.anthropic.com',
  VoyageAI: 'https://api.voyageai.com',
};

export const providersForType = (endpointType: ChatEndpointType): ChatProviderType[] => {
  const all: ChatProviderType[] = ['OpenAI', 'Ollama', 'Gemini', 'Anthropic', 'VoyageAI'];
  return all.filter((p) => validateProviderTypeCombo(p, endpointType) === null);
};

const getBaseUrl = (): string => {
  const endpoint = sdk.config.endpoint || '/';
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

/** Builds auth headers from the shared SDK config (bearer token or access key). */
export const buildChatHeaders = (): Record<string, string> => {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };
  const defaults = (sdk.config as unknown as { defaultHeaders?: Record<string, string> })
    .defaultHeaders;
  if (defaults) {
    for (const key of Object.keys(defaults)) headers[key] = defaults[key];
  }
  return headers;
};

const tenantBase = (tenantGuid: string): string =>
  `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(tenantGuid)}`;

/** Absolute URL of the chat completions route for a tenant (used by streaming). */
export const chatCompletionsUrl = (tenantGuid: string): string =>
  `${tenantBase(tenantGuid)}/chat/completions`;

const request = async <T>(method: string, url: string, body?: unknown): Promise<T> => {
  const headers = buildChatHeaders();
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const response = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    let message = `HTTP ${response.status} ${response.statusText}`;
    try {
      const errorBody = await response.json();
      message = errorBody?.Description || errorBody?.Message || message;
    } catch {
      // Keep the HTTP status message when the server did not return JSON.
    }
    throw new Error(message);
  }
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
};

// region Endpoints

/** List chat endpoints, optionally filtered by type (admin only). */
export const listChatEndpoints = (
  tenantGuid: string,
  endpointType?: ChatEndpointType
): Promise<ChatEndpoint[]> => {
  const query = endpointType ? `?endpointType=${endpointType}` : '';
  return request<ChatEndpoint[]>('GET', `${tenantBase(tenantGuid)}/chat/endpoints${query}`);
};

/** Create a chat endpoint (admin only). */
export const createChatEndpoint = (
  tenantGuid: string,
  endpoint: Partial<ChatEndpoint>
): Promise<ChatEndpoint> =>
  request<ChatEndpoint>('PUT', `${tenantBase(tenantGuid)}/chat/endpoints`, endpoint);

/** Read a single chat endpoint (admin only). */
export const readChatEndpoint = (tenantGuid: string, endpointGuid: string): Promise<ChatEndpoint> =>
  request<ChatEndpoint>(
    'GET',
    `${tenantBase(tenantGuid)}/chat/endpoints/${encodeURIComponent(endpointGuid)}`
  );

/** Update a chat endpoint; sending back a redacted ApiKey preserves the stored key. */
export const updateChatEndpoint = (
  tenantGuid: string,
  endpoint: ChatEndpoint
): Promise<ChatEndpoint> =>
  request<ChatEndpoint>(
    'PUT',
    `${tenantBase(tenantGuid)}/chat/endpoints/${encodeURIComponent(endpoint.GUID)}`,
    endpoint
  );

/** Delete a chat endpoint (admin only). */
export const deleteChatEndpoint = (tenantGuid: string, endpointGuid: string): Promise<void> =>
  request<void>(
    'DELETE',
    `${tenantBase(tenantGuid)}/chat/endpoints/${encodeURIComponent(endpointGuid)}`
  );

/** Run a connectivity test against a chat endpoint (admin only). */
export const testChatEndpoint = (
  tenantGuid: string,
  endpointGuid: string
): Promise<ChatEndpointTestResult> =>
  request<ChatEndpointTestResult>(
    'POST',
    `${tenantBase(tenantGuid)}/chat/endpoints/${encodeURIComponent(endpointGuid)}/test`
  );

/** List health snapshots for every chat endpoint (admin only). */
export const listChatEndpointHealth = (tenantGuid: string): Promise<ChatEndpointHealth[]> =>
  request<ChatEndpointHealth[]>('GET', `${tenantBase(tenantGuid)}/chat/endpoints/health`);

/** Read health for one chat endpoint (admin only). */
export const readChatEndpointHealth = (
  tenantGuid: string,
  endpointGuid: string
): Promise<ChatEndpointHealth> =>
  request<ChatEndpointHealth>(
    'GET',
    `${tenantBase(tenantGuid)}/chat/endpoints/${encodeURIComponent(endpointGuid)}/health`
  );

// endregion

// region Models

/** Non-privileged model summary for the chat model selector. */
export type ChatModelSummary = {
  GUID: string;
  Name: string;
  Model: string;
  Provider: string;
  EndpointType: 'Completion' | 'Embedding';
  IsDefault: boolean;
};

/** List selectable chat models (any tenant member); active endpoints only, no secrets. */
export const listChatModels = (tenantGuid: string): Promise<ChatModelSummary[]> =>
  request<ChatModelSummary[]>('GET', `${tenantBase(tenantGuid)}/chat/models`);

// endregion

// region Threads

/** List chat threads; `all` returns every user's thread (admin only). */
export const listChatThreads = (tenantGuid: string, all?: boolean): Promise<ChatThread[]> =>
  request<ChatThread[]>('GET', `${tenantBase(tenantGuid)}/chat/threads${all ? '?all' : ''}`);

/** Create a chat thread, optionally bound to a graph. */
export const createChatThread = (
  tenantGuid: string,
  body?: { GraphGUID?: string | null; Title?: string | null }
): Promise<ChatThread> =>
  request<ChatThread>('PUT', `${tenantBase(tenantGuid)}/chat/threads`, body ?? {});

/** Read one chat thread (owner or admin). */
export const readChatThread = (tenantGuid: string, threadGuid: string): Promise<ChatThread> =>
  request<ChatThread>(
    'GET',
    `${tenantBase(tenantGuid)}/chat/threads/${encodeURIComponent(threadGuid)}`
  );

/** Read all turns of a thread, ascending by sequence. */
export const listChatThreadTurns = (tenantGuid: string, threadGuid: string): Promise<ChatTurn[]> =>
  request<ChatTurn[]>(
    'GET',
    `${tenantBase(tenantGuid)}/chat/threads/${encodeURIComponent(threadGuid)}/turns`
  );

/** Rename a chat thread (owner or admin); only Title is honored. */
export const updateChatThread = (
  tenantGuid: string,
  threadGuid: string,
  body: { Title: string }
): Promise<ChatThread> =>
  request<ChatThread>(
    'PUT',
    `${tenantBase(tenantGuid)}/chat/threads/${encodeURIComponent(threadGuid)}`,
    body
  );

/** Delete a thread together with its turns and feedback. */
export const deleteChatThread = (tenantGuid: string, threadGuid: string): Promise<void> =>
  request<void>(
    'DELETE',
    `${tenantBase(tenantGuid)}/chat/threads/${encodeURIComponent(threadGuid)}`
  );

// endregion

// region Feedback

/** Submit thumbs up/down feedback for an assistant turn. */
export const submitChatFeedback = (
  tenantGuid: string,
  turnGuid: string,
  body: { Rating: ChatFeedbackRating; FeedbackText?: string | null }
): Promise<ChatFeedback> =>
  request<ChatFeedback>(
    'POST',
    `${tenantBase(tenantGuid)}/chat/turns/${encodeURIComponent(turnGuid)}/feedback`,
    body
  );

/** List all feedback for the tenant (admin only). */
export const listChatFeedback = (tenantGuid: string): Promise<ChatFeedback[]> =>
  request<ChatFeedback[]>('GET', `${tenantBase(tenantGuid)}/chat/feedback`);

/** Read one feedback record (admin only). */
export const readChatFeedback = (tenantGuid: string, feedbackGuid: string): Promise<ChatFeedback> =>
  request<ChatFeedback>(
    'GET',
    `${tenantBase(tenantGuid)}/chat/feedback/${encodeURIComponent(feedbackGuid)}`
  );

/** Delete a feedback record (admin only). */
export const deleteChatFeedback = (tenantGuid: string, feedbackGuid: string): Promise<void> =>
  request<void>(
    'DELETE',
    `${tenantBase(tenantGuid)}/chat/feedback/${encodeURIComponent(feedbackGuid)}`
  );

// endregion

// region Settings

/** Read the tenant chat settings (any tenant principal; defaults when no record exists). */
export const getChatSettings = (tenantGuid: string): Promise<ChatSettings> =>
  request<ChatSettings>('GET', `${tenantBase(tenantGuid)}/chat/settings`);

/** Upsert the tenant chat settings (admin only). */
export const updateChatSettings = (
  tenantGuid: string,
  settings: ChatSettings
): Promise<ChatSettings> =>
  request<ChatSettings>('PUT', `${tenantBase(tenantGuid)}/chat/settings`, settings);

// endregion
