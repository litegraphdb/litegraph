import {
  ChatStreamState,
  chatStreamReducer,
  initialChatStreamState,
  parseToolTranscript,
} from '@/page/ai/chat/chatStream';
import { ChatSseEvent } from '@/lib/sdk/chatSse';

const send = (state: ChatStreamState = initialChatStreamState): ChatStreamState =>
  chatStreamReducer(state, {
    type: 'sendStarted',
    localId: 'local-1',
    userMessage: 'hi there',
    threadGuid: null,
  });

const sse = (state: ChatStreamState, event: ChatSseEvent): ChatStreamState =>
  chatStreamReducer(state, { type: 'sseEvent', event });

describe('chatStreamReducer', () => {
  it('sendStarted creates a pending live exchange', () => {
    const state = send();
    expect(state.live).toMatchObject({
      localId: 'local-1',
      userMessage: 'hi there',
      turnGuid: null,
      status: 'pending',
      assistant: '',
    });
  });

  it('started records thread/turn guids and moves to streaming', () => {
    const state = sse(send(), { event: 'started', threadGuid: 't1', turnGuid: 'u1' });
    expect(state.live).toMatchObject({ threadGuid: 't1', turnGuid: 'u1', status: 'streaming' });
  });

  it('delta appends to the live assistant bubble', () => {
    let state = send();
    state = sse(state, { event: 'delta', content: 'Hel' });
    state = sse(state, { event: 'delta', content: 'lo' });
    expect(state.live?.assistant).toBe('Hello');
  });

  it('thinking accumulates separately from content', () => {
    let state = send();
    state = sse(state, { event: 'thinking', content: 'step 1. ' });
    state = sse(state, { event: 'thinking', content: 'step 2.' });
    state = sse(state, { event: 'delta', content: 'answer' });
    expect(state.live?.thinking).toBe('step 1. step 2.');
    expect(state.live?.assistant).toBe('answer');
  });

  it('retrieval appends source chunks', () => {
    let state = send();
    state = sse(state, {
      event: 'retrieval',
      chunks: [{ nodeGuid: 'n1', name: 'A', score: 0.8 }],
    });
    state = sse(state, {
      event: 'retrieval',
      chunks: [{ nodeGuid: 'n2', name: 'B', score: 0.7 }],
    });
    expect(state.live?.retrieval.map((c) => c.nodeGuid)).toEqual(['n1', 'n2']);
  });

  it('tool_call opens a live entry and tool_result completes it', () => {
    let state = send();
    state = sse(state, { event: 'tool_call', name: 'node/search', arguments: '{}', iteration: 1 });
    expect(state.live?.tools).toEqual([
      { name: 'node/search', arguments: '{}', iteration: 1, completed: false },
    ]);
    state = sse(state, {
      event: 'tool_result',
      name: 'node/search',
      success: true,
      error: null,
      runtimeMs: 12.3,
    });
    expect(state.live?.tools[0]).toMatchObject({
      completed: true,
      success: true,
      runtimeMs: 12.3,
    });
  });

  it('matches tool_result to the earliest uncompleted call of the same name', () => {
    let state = send();
    state = sse(state, { event: 'tool_call', name: 'node/search', arguments: '{"a":1}', iteration: 1 });
    state = sse(state, { event: 'tool_call', name: 'node/search', arguments: '{"a":2}', iteration: 1 });
    state = sse(state, {
      event: 'tool_result',
      name: 'node/search',
      success: false,
      error: 'nope',
      runtimeMs: 1,
    });
    expect(state.live?.tools[0]).toMatchObject({ completed: true, success: false, error: 'nope' });
    expect(state.live?.tools[1].completed).toBe(false);
  });

  it('a tool_result without a matching call still records a completed entry', () => {
    let state = send();
    state = sse(state, {
      event: 'tool_result',
      name: 'graph/get',
      success: true,
      error: null,
      runtimeMs: 3,
    });
    expect(state.live?.tools).toHaveLength(1);
    expect(state.live?.tools[0]).toMatchObject({ name: 'graph/get', completed: true });
  });

  it('usage stores the final metrics', () => {
    let state = send();
    state = sse(state, {
      event: 'usage',
      usage: {
        ThreadGUID: 't1',
        TurnGUID: 'u1',
        Provider: 'OpenAI',
        TotalDurationMs: 100,
        PromptTokens: 10,
        CompletionTokens: 20,
        ToolCallCount: 0,
        ToolLoopIterations: 0,
        RetrievedChunkCount: 0,
        RetryCount: 0,
      },
    });
    expect(state.live?.usage?.PromptTokens).toBe(10);
  });

  it('error finishes the live exchange with an error status', () => {
    let state = sse(send(), { event: 'started', threadGuid: 't1', turnGuid: 'u1' });
    state = sse(state, { event: 'error', message: 'upstream failed', statusCode: 502 });
    expect(state.live).toBeNull();
    expect(state.completed).toHaveLength(1);
    expect(state.completed[0]).toMatchObject({ status: 'error', error: 'upstream failed' });
  });

  it('done moves the live exchange to completed with done status', () => {
    let state = sse(send(), { event: 'started', threadGuid: 't1', turnGuid: 'u1' });
    state = sse(state, { event: 'delta', content: 'answer' });
    state = sse(state, { event: 'done' });
    expect(state.live).toBeNull();
    expect(state.completed[0]).toMatchObject({ status: 'done', assistant: 'answer', turnGuid: 'u1' });
  });

  it('streamFailed finishes the live exchange with the transport error', () => {
    const state = chatStreamReducer(send(), { type: 'streamFailed', message: 'network down' });
    expect(state.live).toBeNull();
    expect(state.completed[0]).toMatchObject({ status: 'error', error: 'network down' });
  });

  it('pruneAgainstServer drops completed exchanges that the server now returns', () => {
    let state = sse(send(), { event: 'started', threadGuid: 't1', turnGuid: 'u1' });
    state = sse(state, { event: 'done' });
    state = chatStreamReducer(state, { type: 'pruneAgainstServer', serverTurnGuids: ['u1'] });
    expect(state.completed).toHaveLength(0);
  });

  it('pruneAgainstServer keeps exchanges the server has not persisted', () => {
    let state = sse(send(), { event: 'started', threadGuid: 't1', turnGuid: 'u1' });
    state = sse(state, { event: 'done' });
    state = chatStreamReducer(state, { type: 'pruneAgainstServer', serverTurnGuids: ['other'] });
    expect(state.completed).toHaveLength(1);
  });

  it('sse events without a live exchange are ignored', () => {
    const state = sse(initialChatStreamState, { event: 'delta', content: 'ghost' });
    expect(state).toEqual(initialChatStreamState);
  });

  it('reset returns to the initial state', () => {
    const state = chatStreamReducer(send(), { type: 'reset' });
    expect(state).toEqual(initialChatStreamState);
  });
});

describe('parseToolTranscript', () => {
  it('parses persisted transcript entries as completed tool activity', () => {
    const json = JSON.stringify([
      {
        iteration: 1,
        name: 'node/search',
        arguments: '{"q":"x"}',
        success: true,
        error: null,
        runtimeMs: 4.2,
      },
    ]);
    expect(parseToolTranscript(json)).toEqual([
      {
        name: 'node/search',
        arguments: '{"q":"x"}',
        iteration: 1,
        completed: true,
        success: true,
        error: null,
        runtimeMs: 4.2,
      },
    ]);
  });

  it('returns an empty array for null, invalid JSON, and non-arrays', () => {
    expect(parseToolTranscript(null)).toEqual([]);
    expect(parseToolTranscript('not json')).toEqual([]);
    expect(parseToolTranscript('{"a":1}')).toEqual([]);
  });
});
