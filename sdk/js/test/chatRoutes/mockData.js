export const mockChatEndpointGuid = '10000000-0000-0000-0000-000000000001';
export const mockThreadGuid = '20000000-0000-0000-0000-000000000002';
export const mockTurnGuid = '30000000-0000-0000-0000-000000000003';
export const mockFeedbackGuid = '40000000-0000-0000-0000-000000000004';

export const chatEndpointData = {
  GUID: mockChatEndpointGuid,
  TenantGUID: '00000000-0000-0000-0000-000000000000',
  Name: 'OpenAI completions',
  EndpointType: 'Completion',
  Provider: 'OpenAI',
  Endpoint: 'https://api.openai.com/v1/',
  ApiKey: '********cdef',
  Model: 'gpt-4o-mini',
  Active: true,
  HealthCheckEnabled: true,
  HealthCheckUrl: null,
  HealthCheckUseAuth: false,
  CreatedUtc: '2026-08-31T09:12:09.761247Z',
  LastUpdateUtc: '2026-08-31T09:12:09.761247Z',
};

export const chatEndpointsMockApiResponse = [chatEndpointData];

export const chatEndpointHealthData = {
  EndpointGUID: mockChatEndpointGuid,
  TenantGUID: '00000000-0000-0000-0000-000000000000',
  Name: 'OpenAI completions',
  EndpointType: 'Completion',
  Monitored: true,
  Healthy: true,
  LastCheckedUtc: '2026-08-31T09:15:00.000000Z',
  LastError: null,
  ConsecutiveSuccesses: 5,
  ConsecutiveFailures: 0,
  UptimePercentage: 100,
  CheckHistory: [{ TimestampUtc: '2026-08-31T09:15:00.000000Z', Success: true, DurationMs: 42.5 }],
};

export const chatEndpointHealthMockApiResponse = [chatEndpointHealthData];

export const chatEndpointTestResultData = {
  Reachable: true,
  Models: ['gpt-4o-mini', 'gpt-4o'],
  ModelExists: true,
  Error: null,
  RuntimeMs: 123.4,
};

export const chatThreadData = {
  GUID: mockThreadGuid,
  TenantGUID: '00000000-0000-0000-0000-000000000000',
  UserGUID: '00000000-0000-0000-0000-000000000000',
  GraphGUID: null,
  Title: 'My chat thread',
  CreatedUtc: '2026-08-31T09:12:09.761247Z',
  LastUpdateUtc: '2026-08-31T09:12:09.761247Z',
};

export const chatThreadsMockApiResponse = [chatThreadData];

export const chatTurnData = {
  GUID: mockTurnGuid,
  TenantGUID: '00000000-0000-0000-0000-000000000000',
  ThreadGUID: mockThreadGuid,
  UserMessage: 'What nodes exist?',
  AssistantResponse: 'There are 3 nodes.',
  Reasoning: null,
  ToolTranscriptJson: null,
  TelemetryJson: null,
  TraceId: 'trace-1',
  CompletionEndpointGUID: mockChatEndpointGuid,
  EmbeddingEndpointGUID: null,
  Provider: 'OpenAI',
  Model: 'gpt-4o-mini',
  RetrievedChunkCount: 0,
  ToolLoopIterations: 1,
  ToolCallCount: 1,
  TotalDurationMs: 1234.5,
  PromptTokens: 100,
  CompletionTokens: 25,
  RetryCount: 0,
  Success: true,
  HttpStatus: 200,
  Error: null,
  CreatedUtc: '2026-08-31T09:12:09.761247Z',
};

export const chatTurnsMockApiResponse = [chatTurnData];

export const chatFeedbackData = {
  GUID: mockFeedbackGuid,
  TenantGUID: '00000000-0000-0000-0000-000000000000',
  ThreadGUID: mockThreadGuid,
  TurnGUID: mockTurnGuid,
  UserGUID: '00000000-0000-0000-0000-000000000000',
  Rating: 'ThumbsUp',
  FeedbackText: 'Great answer',
  CreatedUtc: '2026-08-31T09:12:09.761247Z',
};

export const chatFeedbackMockApiResponse = [chatFeedbackData];

export const chatSettingsData = {
  TenantGUID: '00000000-0000-0000-0000-000000000000',
  DefaultCompletionEndpointGUID: mockChatEndpointGuid,
  DefaultEmbeddingEndpointGUID: null,
  SystemPrompt: null,
  EnableChat: true,
  EnableTools: true,
  EnableMutationTools: false,
  MaxToolIterations: 10,
  EnableRag: true,
  RagTopK: 8,
  RagScoreThreshold: 0,
  MaxContextTokens: 16384,
  HistoryRetentionDays: 90,
  CreatedUtc: '2026-08-31T09:12:09.761247Z',
  LastUpdateUtc: '2026-08-31T09:12:09.761247Z',
};

export const chatCompletionResultData = {
  ThreadGUID: mockThreadGuid,
  TurnGUID: mockTurnGuid,
  Message: 'There are 3 nodes.',
  Reasoning: null,
  Provider: 'OpenAI',
  Model: 'gpt-4o-mini',
  PromptTokens: 100,
  CompletionTokens: 25,
  TimeToFirstTokenMs: 120.5,
  TimeToLastTokenMs: 900.1,
  TotalDurationMs: 1234.5,
  TokensPerSecondOverall: 20.2,
  ToolCallCount: 1,
  ToolLoopIterations: 1,
  RetrievedChunkCount: 0,
  RetryCount: 0,
};

const sseFrame = (obj) => `data: ${JSON.stringify(obj)}\n\n`;

export const sseStreamBody =
  sseFrame({ event: 'started', threadGuid: mockThreadGuid, turnGuid: mockTurnGuid }) +
  sseFrame({ event: 'delta', content: 'There are ' }) +
  sseFrame({ event: 'delta', content: '3 nodes.' }) +
  sseFrame({ event: 'tool_call', name: 'node/search', arguments: '{"GraphGUID":"g"}', iteration: 1 }) +
  sseFrame({ event: 'tool_result', name: 'node/search', success: true, error: null, runtimeMs: 12.3 }) +
  sseFrame({ event: 'usage', usage: chatCompletionResultData }) +
  'data: [DONE]\n\n';

export const sseStreamBodyWithMalformedFrame =
  sseFrame({ event: 'started', threadGuid: mockThreadGuid, turnGuid: mockTurnGuid }) +
  'data: {not-valid-json\n\n' +
  sseFrame({ event: 'delta', content: 'Hello' }) +
  'data: [DONE]\n\n';
