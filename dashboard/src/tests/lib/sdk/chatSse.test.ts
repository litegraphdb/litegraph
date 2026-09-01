import '@testing-library/jest-dom';
import {
  ChatSseEvent,
  completeChatCompletion,
  createChatSseParser,
  streamChatCompletion,
} from '@/lib/sdk/chatSse';
import { setEndpoint } from '@/lib/sdk/litegraph.service';

const collect = () => {
  const events: ChatSseEvent[] = [];
  const parser = createChatSseParser((event) => events.push(event));
  return { events, parser };
};

describe('createChatSseParser', () => {
  it('parses a complete single frame', () => {
    const { events, parser } = collect();
    parser.feed('data: {"event":"delta","content":"Hello"}\n\n');
    expect(events).toEqual([{ event: 'delta', content: 'Hello' }]);
  });

  it('parses multiple frames in one chunk', () => {
    const { events, parser } = collect();
    parser.feed(
      'data: {"event":"started","threadGuid":"t1","turnGuid":"u1"}\n\n' +
        'data: {"event":"delta","content":"a"}\n\n' +
        'data: {"event":"thinking","content":"hmm"}\n\n'
    );
    expect(events.map((e) => e.event)).toEqual(['started', 'delta', 'thinking']);
    expect(events[0]).toEqual({ event: 'started', threadGuid: 't1', turnGuid: 'u1' });
  });

  it('buffers frames split across chunk boundaries', () => {
    const { events, parser } = collect();
    parser.feed('data: {"event":"delta","con');
    expect(events).toHaveLength(0);
    parser.feed('tent":"split"}\n');
    expect(events).toHaveLength(0);
    parser.feed('\ndata: {"event":"delta","content":"next"}\n\n');
    expect(events).toEqual([
      { event: 'delta', content: 'split' },
      { event: 'delta', content: 'next' },
    ]);
  });

  it('handles CRLF frame delimiters', () => {
    const { events, parser } = collect();
    parser.feed('data: {"event":"delta","content":"x"}\r\n\r\n');
    expect(events).toEqual([{ event: 'delta', content: 'x' }]);
  });

  it('emits done for the [DONE] sentinel', () => {
    const { events, parser } = collect();
    parser.feed('data: [DONE]\n\n');
    expect(events).toEqual([{ event: 'done' }]);
  });

  it('surfaces every discriminator from the spec', () => {
    const { events, parser } = collect();
    const frames = [
      { event: 'started', threadGuid: 't', turnGuid: 'u' },
      { event: 'delta', content: 'text' },
      { event: 'thinking', content: 'reason' },
      { event: 'retrieval', chunks: [{ nodeGuid: 'n1', name: 'Node', score: 0.9 }] },
      { event: 'tool_call', name: 'node/search', arguments: '{}', iteration: 1 },
      { event: 'tool_result', name: 'node/search', success: true, error: null, runtimeMs: 12.3 },
      { event: 'usage', usage: { ThreadGUID: 't', TurnGUID: 'u', TotalDurationMs: 5 } },
      { event: 'error', message: 'boom', statusCode: 502 },
    ];
    for (const frame of frames) parser.feed(`data: ${JSON.stringify(frame)}\n\n`);
    parser.feed('data: [DONE]\n\n');
    expect(events.map((e) => e.event)).toEqual([
      'started',
      'delta',
      'thinking',
      'retrieval',
      'tool_call',
      'tool_result',
      'usage',
      'error',
      'done',
    ]);
  });

  it('ignores comment/keep-alive lines and malformed JSON', () => {
    const { events, parser } = collect();
    parser.feed(': keep-alive\n\n');
    parser.feed('data: {not json}\n\n');
    parser.feed('data: {"event":"delta","content":"ok"}\n\n');
    expect(events).toEqual([{ event: 'delta', content: 'ok' }]);
  });

  it('ignores frames without an event discriminator', () => {
    const { events, parser } = collect();
    parser.feed('data: {"content":"stray"}\n\n');
    expect(events).toHaveLength(0);
  });

  it('flushes a trailing unterminated frame on end()', () => {
    const { events, parser } = collect();
    parser.feed('data: {"event":"delta","content":"tail"}');
    expect(events).toHaveLength(0);
    parser.end();
    expect(events).toEqual([{ event: 'delta', content: 'tail' }]);
  });
});

describe('streamChatCompletion', () => {
  beforeEach(() => {
    setEndpoint('http://localhost:8701/');
    jest.clearAllMocks();
  });

  const streamBody = (frames: string[]) => {
    const encoder = new TextEncoder();
    let index = 0;
    return {
      getReader: () => ({
        read: async () => {
          if (index < frames.length) {
            return { done: false, value: encoder.encode(frames[index++]) };
          }
          return { done: true, value: undefined };
        },
        releaseLock: () => undefined,
      }),
    };
  };

  it('POSTs with Stream=true and pumps events until [DONE]', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      body: streamBody([
        'data: {"event":"started","threadGuid":"t1","turnGuid":"u1"}\n\n',
        'data: {"event":"delta","content":"Hi"}\n\ndata: [DONE]\n\n',
      ]),
    }) as jest.Mock;

    const events: ChatSseEvent[] = [];
    await streamChatCompletion('tenant-1', { Message: 'hello' }, (e) => events.push(e));

    const [url, init] = (global.fetch as jest.Mock).mock.calls[0];
    expect(url).toBe('http://localhost:8701/v1.0/tenants/tenant-1/chat/completions');
    expect(init.method).toBe('POST');
    const body = JSON.parse(init.body);
    expect(body.Stream).toBe(true);
    expect(body.Message).toBe('hello');
    expect(events.map((e) => e.event)).toEqual(['started', 'delta', 'done']);
  });

  it('throws with the server Description on a non-2xx response', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 400,
      statusText: 'Bad Request',
      json: async () => ({ Description: 'No completion endpoint is configured.' }),
    }) as jest.Mock;

    await expect(
      streamChatCompletion('tenant-1', { Message: 'x' }, () => undefined)
    ).rejects.toThrow('No completion endpoint is configured.');
  });
});

describe('completeChatCompletion', () => {
  beforeEach(() => {
    setEndpoint('http://localhost:8701/');
    jest.clearAllMocks();
  });

  const result = {
    ThreadGUID: 'thread-1',
    TurnGUID: 'turn-1',
    Message: 'Hello world',
    Reasoning: null,
    Provider: 'OpenAI',
    Model: 'gpt-4o',
    PromptTokens: 5,
    CompletionTokens: 7,
    TotalDurationMs: 1000,
    ToolCallCount: 0,
    ToolLoopIterations: 0,
    RetrievedChunkCount: 0,
    RetryCount: 0,
  };

  it('POSTs with Stream=false and replays the result as started/delta/usage/done', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => result,
    }) as jest.Mock;

    const events: ChatSseEvent[] = [];
    await completeChatCompletion(
      'tenant-1',
      { Message: 'hi', Stream: true },
      (e) => events.push(e)
    );

    const [url, init] = (global.fetch as jest.Mock).mock.calls[0];
    expect(url).toBe('http://localhost:8701/v1.0/tenants/tenant-1/chat/completions');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toMatchObject({ Message: 'hi', Stream: false });

    expect(events.map((e) => e.event)).toEqual(['started', 'delta', 'usage', 'done']);
    expect(events[0]).toEqual({ event: 'started', threadGuid: 'thread-1', turnGuid: 'turn-1' });
    expect(events[1]).toEqual({ event: 'delta', content: 'Hello world' });
    expect(events[2]).toMatchObject({
      event: 'usage',
      usage: { PromptTokens: 5, CompletionTokens: 7 },
    });
  });

  it('emits a thinking event before delta when Reasoning is present', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ ...result, Reasoning: 'step 1' }),
    }) as jest.Mock;

    const events: ChatSseEvent[] = [];
    await completeChatCompletion('tenant-1', { Message: 'hi' }, (e) => events.push(e));

    expect(events.map((e) => e.event)).toEqual(['started', 'thinking', 'delta', 'usage', 'done']);
  });
});
