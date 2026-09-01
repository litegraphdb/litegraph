import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import ChatPage from '@/page/ai/chat/ChatPage';
import { completeChatCompletion, streamChatCompletion } from '@/lib/sdk/chatSse';
import { createMockInitialState } from '../../../store/mockStore';
import { renderWithRedux } from '../../../store/utils';

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn(), prefetch: jest.fn() }),
  usePathname: () => '',
  useParams: () => ({ tenantId: 'tenant-1' }),
}));

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { error: jest.fn(), success: jest.fn() },
}));

jest.mock('@/lib/sdk/chatSse', () => ({
  streamChatCompletion: jest.fn().mockResolvedValue(undefined),
  completeChatCompletion: jest.fn().mockResolvedValue(undefined),
}));

const mockRefetchThreads = jest.fn();
const mockRefetchTurns = jest.fn();

jest.mock('@/lib/store/slice/slice', () => ({
  useGetChatSettingsQuery: () => ({
    data: { EnableChat: true, DefaultCompletionEndpointGUID: 'endpoint-1' },
    isLoading: false,
  }),
  useListChatThreadsQuery: () => ({
    data: [],
    isLoading: false,
    refetch: mockRefetchThreads,
  }),
  useListChatThreadTurnsQuery: () => ({ data: [], refetch: mockRefetchTurns }),
  useListChatModelsQuery: () => ({
    data: [
      {
        GUID: 'model-1',
        Name: 'Primary',
        Model: 'gpt-4o',
        Provider: 'OpenAI',
        EndpointType: 'Completion',
        IsDefault: true,
      },
      {
        GUID: 'model-2',
        Name: 'Embedder',
        Model: 'text-embedding-3-small',
        Provider: 'OpenAI',
        EndpointType: 'Embedding',
        IsDefault: false,
      },
    ],
  }),
  useDeleteChatThreadMutation: () => [jest.fn(), { isLoading: false }],
  useUpdateChatThreadMutation: () => [jest.fn(), { isLoading: false }],
  useCreateChatThreadMutation: () => [jest.fn(), { isLoading: false }],
  useSubmitChatFeedbackMutation: () => [jest.fn(), { isLoading: false }],
}));

const streamChatCompletionMock = streamChatCompletion as jest.Mock;
const completeChatCompletionMock = completeChatCompletion as jest.Mock;

beforeAll(() => {
  window.HTMLElement.prototype.scrollIntoView = jest.fn();
});

const renderChatPage = () => renderWithRedux(<ChatPage />, createMockInitialState());

const sendMessage = (text: string) => {
  fireEvent.change(screen.getByTestId('chat-input-textarea'), { target: { value: text } });
  fireEvent.click(screen.getByTestId('chat-send'));
};

describe('ChatPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('shows the disclaimer and defaults the streaming toggle to on', () => {
    renderChatPage();

    expect(screen.getByTestId('chat-disclaimer')).toHaveTextContent(
      'AI can make mistakes. Please verify all answers.'
    );
    expect(screen.getByTestId('chat-streaming-toggle')).toHaveAttribute('aria-checked', 'true');
  });

  it('lists only Completion endpoints in the model selector', () => {
    renderChatPage();

    // LitegraphSelect is globally mocked as a native <select> in jest.setup.js.
    const select = screen.getByTestId('chat-model-select');
    const optionLabels = Array.from(select.querySelectorAll('option')).map(
      (option) => (option as HTMLOptionElement).textContent
    );
    expect(optionLabels).toContain('Primary (gpt-4o) — default');
    expect(optionLabels.join('|')).not.toMatch(/Embedder/);
  });

  it('handles /help as a local slash command without a network call', async () => {
    renderChatPage();

    sendMessage('/help');

    await waitFor(() => {
      expect(screen.getByTestId('chat-system-notice')).toBeInTheDocument();
    });
    expect(screen.getByTestId('chat-system-notice')).toHaveTextContent('/clear');
    expect(streamChatCompletionMock).not.toHaveBeenCalled();
    expect(completeChatCompletionMock).not.toHaveBeenCalled();
  });

  it('shows an unknown-command notice for an unrecognized slash command', async () => {
    renderChatPage();

    sendMessage('/x');

    await waitFor(() => {
      expect(screen.getByTestId('chat-system-notice')).toHaveTextContent(/Unknown command/);
    });
    expect(streamChatCompletionMock).not.toHaveBeenCalled();
  });
});
