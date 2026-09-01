import { ChatCompletionResult } from '@/lib/sdk/chat';
import { ChatRetrievalChunk, ChatSseEvent } from '@/lib/sdk/chatSse';

/** A tool invocation surfaced during streaming (live, then collapsed on completion). */
export type ToolActivityEntry = {
  name: string;
  arguments?: string | null;
  iteration?: number;
  completed: boolean;
  success?: boolean;
  error?: string | null;
  runtimeMs?: number | null;
};

/** One user/assistant exchange assembled client-side while streaming. */
export type ChatExchange = {
  localId: string;
  threadGuid: string | null;
  turnGuid: string | null;
  userMessage: string;
  assistant: string;
  thinking: string;
  tools: ToolActivityEntry[];
  retrieval: ChatRetrievalChunk[];
  usage: ChatCompletionResult | null;
  error: string | null;
  status: 'pending' | 'streaming' | 'done' | 'error';
};

/** Reducer state: the in-flight exchange plus completed ones awaiting server refetch. */
export type ChatStreamState = {
  live: ChatExchange | null;
  completed: ChatExchange[];
};

export type ChatStreamAction =
  | { type: 'sendStarted'; localId: string; userMessage: string; threadGuid: string | null }
  | { type: 'sseEvent'; event: ChatSseEvent }
  | { type: 'streamFailed'; message: string }
  | { type: 'pruneAgainstServer'; serverTurnGuids: string[] }
  | { type: 'reset' };

/** Initial (idle) stream state. */
export const initialChatStreamState: ChatStreamState = {
  live: null,
  completed: [],
};

/**
 * Parses a persisted ToolTranscriptJson document (array of
 * `{ iteration, name, arguments, success, error, runtimeMs }`) into completed
 * {@link ToolActivityEntry} rows. Returns an empty array on any parse failure.
 */
export const parseToolTranscript = (json?: string | null): ToolActivityEntry[] => {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    if (!Array.isArray(parsed)) return [];
    return parsed
      .filter((entry) => entry && typeof entry === 'object' && typeof entry.name === 'string')
      .map((entry) => ({
        name: entry.name as string,
        arguments: typeof entry.arguments === 'string' ? entry.arguments : null,
        iteration: typeof entry.iteration === 'number' ? entry.iteration : undefined,
        completed: true,
        success: entry.success === true,
        error: typeof entry.error === 'string' ? entry.error : null,
        runtimeMs: typeof entry.runtimeMs === 'number' ? entry.runtimeMs : null,
      }));
  } catch {
    return [];
  }
};

const finishLive = (state: ChatStreamState, patch: Partial<ChatExchange>): ChatStreamState => {
  if (!state.live) return state;
  const finished: ChatExchange = { ...state.live, ...patch };
  return { live: null, completed: [...state.completed, finished] };
};

const patchLive = (state: ChatStreamState, patch: Partial<ChatExchange>): ChatStreamState => {
  if (!state.live) return state;
  return { ...state, live: { ...state.live, ...patch } };
};

const applyToolResult = (
  tools: ToolActivityEntry[],
  event: Extract<ChatSseEvent, { event: 'tool_result' }>
): ToolActivityEntry[] => {
  const index = tools.findIndex((tool) => !tool.completed && tool.name === event.name);
  if (index < 0) {
    return [
      ...tools,
      {
        name: event.name,
        completed: true,
        success: event.success,
        error: event.error ?? null,
        runtimeMs: event.runtimeMs ?? null,
      },
    ];
  }
  return tools.map((tool, i) =>
    i === index
      ? {
          ...tool,
          completed: true,
          success: event.success,
          error: event.error ?? null,
          runtimeMs: event.runtimeMs ?? null,
        }
      : tool
  );
};

/**
 * Pure reducer that folds SSE events into the streaming chat view state. It is
 * intentionally free of side effects so it can be unit-tested and driven from
 * `useReducer` in the chat panel.
 */
export const chatStreamReducer = (
  state: ChatStreamState,
  action: ChatStreamAction
): ChatStreamState => {
  switch (action.type) {
    case 'sendStarted':
      return {
        ...state,
        live: {
          localId: action.localId,
          threadGuid: action.threadGuid,
          turnGuid: null,
          userMessage: action.userMessage,
          assistant: '',
          thinking: '',
          tools: [],
          retrieval: [],
          usage: null,
          error: null,
          status: 'pending',
        },
      };
    case 'sseEvent': {
      const event = action.event;
      if (!state.live) return state;
      switch (event.event) {
        case 'started':
          return patchLive(state, {
            threadGuid: event.threadGuid,
            turnGuid: event.turnGuid,
            status: 'streaming',
          });
        case 'delta':
          return patchLive(state, {
            assistant: state.live.assistant + event.content,
            status: 'streaming',
          });
        case 'thinking':
          return patchLive(state, {
            thinking: state.live.thinking + event.content,
            status: 'streaming',
          });
        case 'retrieval':
          return patchLive(state, {
            retrieval: [...state.live.retrieval, ...(event.chunks || [])],
          });
        case 'tool_call':
          return patchLive(state, {
            tools: [
              ...state.live.tools,
              {
                name: event.name,
                arguments: event.arguments ?? null,
                iteration: event.iteration,
                completed: false,
              },
            ],
          });
        case 'tool_result':
          return patchLive(state, { tools: applyToolResult(state.live.tools, event) });
        case 'usage':
          return patchLive(state, { usage: event.usage });
        case 'error':
          return finishLive(state, { error: event.message, status: 'error' });
        case 'done':
          return finishLive(state, {
            status: state.live.error ? 'error' : 'done',
          });
        default:
          return state;
      }
    }
    case 'streamFailed':
      return finishLive(state, { error: action.message, status: 'error' });
    case 'pruneAgainstServer':
      return {
        ...state,
        completed: state.completed.filter(
          (exchange) =>
            !exchange.turnGuid || !action.serverTurnGuids.includes(exchange.turnGuid)
        ),
      };
    case 'reset':
      return initialChatStreamState;
    default:
      return state;
  }
};
