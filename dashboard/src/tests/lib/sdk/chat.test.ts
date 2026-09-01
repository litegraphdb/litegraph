import '@testing-library/jest-dom';
import { setEndpoint } from '@/lib/sdk/litegraph.service';
import {
  createChatThread,
  deleteChatThread,
  getChatSettings,
  isRedactedApiKey,
  listChatEndpoints,
  listChatFeedback,
  listChatThreadTurns,
  listChatThreads,
  submitChatFeedback,
  updateChatSettings,
} from '@/lib/sdk/chat';

const TENANT = '11111111-1111-1111-1111-111111111111';

const mockSettings = {
  TenantGUID: TENANT,
  DefaultCompletionEndpointGUID: null,
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
};

const mockFetchJson = (payload: unknown, status = 200) => {
  global.fetch = jest.fn().mockResolvedValue({
    ok: status < 400,
    status,
    statusText: 'OK',
    text: jest.fn().mockResolvedValue(JSON.stringify(payload)),
    json: jest.fn().mockResolvedValue(payload),
  }) as jest.Mock;
};

const lastCall = () => {
  const calls = (global.fetch as jest.Mock).mock.calls;
  const [url, init] = calls[calls.length - 1];
  return { url: url as string, init: init as RequestInit };
};

describe('chat sdk', () => {
  beforeEach(() => {
    setEndpoint('http://localhost:8701/');
    jest.clearAllMocks();
  });

  it('reads tenant chat settings from the tenant-scoped route', async () => {
    mockFetchJson(mockSettings);
    const settings = await getChatSettings(TENANT);
    expect(settings.EnableChat).toBe(true);
    expect(settings.RagTopK).toBe(8);
    expect(lastCall().url).toBe(`http://localhost:8701/v1.0/tenants/${TENANT}/chat/settings`);
    expect(lastCall().init.method).toBe('GET');
  });

  it('upserts chat settings with a PUT of the full document', async () => {
    mockFetchJson(mockSettings);
    await updateChatSettings(TENANT, { ...mockSettings, RagTopK: 12 });
    const { url, init } = lastCall();
    expect(url).toBe(`http://localhost:8701/v1.0/tenants/${TENANT}/chat/settings`);
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body as string).RagTopK).toBe(12);
  });

  it('lists endpoints and passes the endpointType filter', async () => {
    mockFetchJson([{ GUID: 'e1', ApiKey: '********abcd' }]);
    const endpoints = await listChatEndpoints(TENANT, 'Completion');
    expect(endpoints).toHaveLength(1);
    expect(lastCall().url).toBe(
      `http://localhost:8701/v1.0/tenants/${TENANT}/chat/endpoints?endpointType=Completion`
    );
  });

  it('recognizes redacted API keys', () => {
    expect(isRedactedApiKey('********abcd')).toBe(true);
    expect(isRedactedApiKey('sk-real-key')).toBe(false);
    expect(isRedactedApiKey(null)).toBe(false);
  });

  it('lists own threads, and all threads with the admin flag', async () => {
    mockFetchJson([]);
    await listChatThreads(TENANT);
    expect(lastCall().url).toBe(`http://localhost:8701/v1.0/tenants/${TENANT}/chat/threads`);
    await listChatThreads(TENANT, true);
    expect(lastCall().url).toBe(`http://localhost:8701/v1.0/tenants/${TENANT}/chat/threads?all`);
  });

  it('creates a thread bound to a graph with PUT', async () => {
    mockFetchJson({ GUID: 't2', GraphGUID: 'g1' });
    const thread = await createChatThread(TENANT, { GraphGUID: 'g1' });
    expect(thread.GUID).toBe('t2');
    const { init } = lastCall();
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body as string).GraphGUID).toBe('g1');
  });

  it('reads turns and deletes a thread', async () => {
    mockFetchJson([{ GUID: 'u1' }]);
    const turns = await listChatThreadTurns(TENANT, 't1');
    expect(turns).toHaveLength(1);
    expect(lastCall().url).toBe(
      `http://localhost:8701/v1.0/tenants/${TENANT}/chat/threads/t1/turns`
    );
    await deleteChatThread(TENANT, 't1');
    expect(lastCall().init.method).toBe('DELETE');
  });

  it('submits turn feedback via POST', async () => {
    mockFetchJson({ GUID: 'f1', Rating: 'ThumbsUp' });
    const feedback = await submitChatFeedback(TENANT, 'u1', {
      Rating: 'ThumbsUp',
      FeedbackText: 'nice',
    });
    expect(feedback.GUID).toBe('f1');
    const { url, init } = lastCall();
    expect(url).toBe(`http://localhost:8701/v1.0/tenants/${TENANT}/chat/turns/u1/feedback`);
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toEqual({ Rating: 'ThumbsUp', FeedbackText: 'nice' });
  });

  it('lists feedback for admins', async () => {
    mockFetchJson([{ GUID: 'f1' }]);
    const feedback = await listChatFeedback(TENANT);
    expect(feedback).toHaveLength(1);
    expect(lastCall().url).toBe(`http://localhost:8701/v1.0/tenants/${TENANT}/chat/feedback`);
  });

  it('surfaces the server Description on errors', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 400,
      statusText: 'Bad Request',
      json: jest.fn().mockResolvedValue({ Description: 'VoyageAI does not offer completions.' }),
    }) as jest.Mock;
    await expect(getChatSettings(TENANT)).rejects.toThrow('VoyageAI does not offer completions.');
  });
});
