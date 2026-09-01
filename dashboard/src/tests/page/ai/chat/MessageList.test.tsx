import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import MessageList, { ChatDisplayItem } from '@/page/ai/chat/components/MessageList';

beforeAll(() => {
  window.HTMLElement.prototype.scrollIntoView = jest.fn();
});

const baseItem: ChatDisplayItem = {
  key: 'turn-1',
  turnGuid: 'turn-1',
  userMessage: 'hello',
  assistant: 'world',
  thinking: '',
  tools: [],
  retrieval: [],
  error: null,
  streaming: false,
};

describe('MessageList', () => {
  it('renders the (i) stats button and opens the popover with a TTFT row', async () => {
    const item: ChatDisplayItem = {
      ...baseItem,
      stats: {
        provider: 'OpenAI',
        model: 'gpt-4o',
        promptTokens: 10,
        completionTokens: 20,
        ttftMs: 123.4,
        totalDurationMs: 1500,
      },
    };
    render(<MessageList items={[item]} onFeedback={jest.fn()} />);

    const button = screen.getByTestId('chat-turn-stats-button');
    expect(button).toBeInTheDocument();
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByTestId('chat-turn-stats')).toBeInTheDocument();
    });
    const statsTable = screen.getByTestId('chat-turn-stats');
    expect(statsTable).toHaveTextContent('Time to first token');
    expect(statsTable).toHaveTextContent('123 ms');
    expect(statsTable).toHaveTextContent('OpenAI / gpt-4o');
  });

  it('renders a system notice through Markdown instead of a user bubble', () => {
    const item: ChatDisplayItem = {
      ...baseItem,
      key: 'notice-1',
      turnGuid: null,
      userMessage: '',
      assistant: '',
      notice: '**Heads up**',
    };
    render(<MessageList items={[item]} onFeedback={jest.fn()} />);

    const notice = screen.getByTestId('chat-system-notice');
    expect(notice.querySelector('strong')).toHaveTextContent('Heads up');
    expect(screen.queryByTestId('chat-user-bubble')).not.toBeInTheDocument();
  });
});
