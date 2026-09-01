import {
  ChatCompletionRequest,
  ChatCompletionResult,
  buildChatHeaders,
  chatCompletionsUrl,
} from './chat';

/** One retrieval hit surfaced in a `retrieval` SSE frame. */
export type ChatRetrievalChunk = {
  nodeGuid?: string | null;
  name?: string | null;
  score?: number | null;
};

/** Discriminated union of every SSE frame the chat completions stream emits. */
export type ChatSseEvent =
  | { event: 'started'; threadGuid: string; turnGuid: string }
  | { event: 'delta'; content: string }
  | { event: 'thinking'; content: string }
  | { event: 'retrieval'; chunks: ChatRetrievalChunk[] }
  | { event: 'tool_call'; name: string; arguments?: string | null; iteration?: number }
  | {
      event: 'tool_result';
      name: string;
      success: boolean;
      error?: string | null;
      runtimeMs?: number | null;
    }
  | { event: 'usage'; usage: ChatCompletionResult }
  | { event: 'error'; message: string; statusCode?: number | null }
  | { event: 'done' };

/**
 * Creates a stateful, incremental SSE frame parser. Feed it raw text chunks in
 * arrival order; it buffers partial frames across chunk boundaries, ignores
 * comment/keep-alive lines, and emits one {@link ChatSseEvent} per complete
 * `data:` frame. The literal `[DONE]` frame is surfaced as `{ event: 'done' }`.
 * Frames that fail to parse as JSON are silently skipped.
 */
export const createChatSseParser = (onEvent: (event: ChatSseEvent) => void) => {
  let buffer = '';

  const processFrame = (frame: string) => {
    const dataLines = frame
      .split(/\r?\n/)
      .filter((line) => line.startsWith('data:'))
      .map((line) => line.slice(5).replace(/^\s/, ''));
    if (dataLines.length === 0) return;
    const payload = dataLines.join('\n');
    if (payload.trim() === '[DONE]') {
      onEvent({ event: 'done' });
      return;
    }
    try {
      const parsed = JSON.parse(payload);
      if (parsed && typeof parsed === 'object' && typeof parsed.event === 'string') {
        onEvent(parsed as ChatSseEvent);
      }
    } catch {
      // Ignore malformed frames; the stream terminates with [DONE] regardless.
    }
  };

  const feed = (chunk: string) => {
    buffer += chunk;
    let boundary = buffer.search(/\r?\n\r?\n/);
    while (boundary >= 0) {
      const match = buffer.match(/\r?\n\r?\n/);
      const sepLength = match ? match[0].length : 2;
      const frame = buffer.slice(0, boundary);
      buffer = buffer.slice(boundary + sepLength);
      processFrame(frame);
      boundary = buffer.search(/\r?\n\r?\n/);
    }
  };

  /** Flush any trailing frame that was not double-newline terminated. */
  const end = () => {
    if (buffer.trim().length > 0) processFrame(buffer);
    buffer = '';
  };

  return { feed, end };
};

/**
 * POSTs a streaming chat completion and pumps the SSE frames through `onEvent`
 * until the `[DONE]` sentinel or stream end. Throws on a non-2xx response with
 * the server's Description when available. Abortable via `signal`.
 */
/**
 * POSTs a buffered (non-streaming) chat completion and replays the JSON result
 * as synthetic SSE events (`started` → `thinking` → `delta` → `usage` →
 * `done`) so callers can share one event-driven code path with streaming.
 * Throws on a non-2xx response with the server's Description when available.
 */
export const completeChatCompletion = async (
  tenantGuid: string,
  body: ChatCompletionRequest,
  onEvent: (event: ChatSseEvent) => void,
  signal?: AbortSignal
): Promise<void> => {
  const headers = buildChatHeaders();
  headers['Content-Type'] = 'application/json';

  const response = await fetch(chatCompletionsUrl(tenantGuid), {
    method: 'POST',
    headers,
    body: JSON.stringify({ ...body, Stream: false }),
    signal,
  });

  if (!response.ok) {
    let message = `HTTP ${response.status} ${response.statusText}`;
    try {
      const errorBody = await response.json();
      message = errorBody?.Description || errorBody?.Message || message;
    } catch {
      // Keep the HTTP status message when the server did not return JSON.
    }
    const error = new Error(message) as Error & { statusCode?: number };
    error.statusCode = response.status;
    throw error;
  }

  const result = (await response.json()) as ChatCompletionResult;
  onEvent({
    event: 'started',
    threadGuid: result.ThreadGUID,
    turnGuid: result.TurnGUID,
  });
  if (result.Reasoning) onEvent({ event: 'thinking', content: result.Reasoning });
  if (result.Message) onEvent({ event: 'delta', content: result.Message });
  onEvent({ event: 'usage', usage: result });
  onEvent({ event: 'done' });
};

export const streamChatCompletion = async (
  tenantGuid: string,
  body: ChatCompletionRequest,
  onEvent: (event: ChatSseEvent) => void,
  signal?: AbortSignal
): Promise<void> => {
  const headers = buildChatHeaders();
  headers['Content-Type'] = 'application/json';
  headers.Accept = 'text/event-stream';

  const response = await fetch(chatCompletionsUrl(tenantGuid), {
    method: 'POST',
    headers,
    body: JSON.stringify({ ...body, Stream: true }),
    signal,
  });

  if (!response.ok) {
    let message = `HTTP ${response.status} ${response.statusText}`;
    try {
      const errorBody = await response.json();
      message = errorBody?.Description || errorBody?.Message || message;
    } catch {
      // Keep the HTTP status message when the server did not return JSON.
    }
    const error = new Error(message) as Error & { statusCode?: number };
    error.statusCode = response.status;
    throw error;
  }

  if (!response.body) {
    throw new Error('Streaming is not supported by this browser or server response.');
  }

  const parser = createChatSseParser(onEvent);
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) break;
      parser.feed(decoder.decode(value, { stream: true }));
    }
    parser.feed(decoder.decode());
    parser.end();
  } finally {
    reader.releaseLock();
  }
};
