import '@testing-library/jest-dom';
import React from 'react';
import { screen, waitFor } from '@testing-library/react';
import FeedbackPage from '@/page/ai/feedback/FeedbackPage';
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

jest.mock('@/lib/store/slice/slice', () => ({
  useListChatFeedbackQuery: () => ({
    data: [
      {
        GUID: 'feedback-1',
        TenantGUID: 'tenant-1',
        ThreadGUID: 'thread-1',
        TurnGUID: 'turn-1',
        UserGUID: 'user-1',
        Rating: 'ThumbsUp',
        FeedbackText: 'Great answer',
        CreatedUtc: '2026-01-15T10:00:00Z',
      },
    ],
    isLoading: false,
    isFetching: false,
    error: undefined,
    refetch: jest.fn(),
  }),
  useDeleteChatFeedbackMutation: () => [jest.fn(), { isLoading: false }],
  useGetAllUsersQuery: () => ({
    data: [
      {
        GUID: 'user-1',
        Email: 'jane@example.com',
        FirstName: 'Jane',
        LastName: 'Doe',
      },
    ],
  }),
  useListChatThreadTurnsQuery: () => ({ data: [], isLoading: false }),
}));

describe('FeedbackPage', () => {
  it('puts Created first and maps the user GUID to the user email', async () => {
    renderWithRedux(<FeedbackPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('feedback-user-user-1')).toBeInTheDocument();
    });

    const headerCells = document.querySelectorAll('thead th');
    expect(headerCells.length).toBeGreaterThan(1);
    expect(headerCells[0]).toHaveTextContent('Created');
    expect(headerCells[1]).toHaveTextContent('User');

    expect(screen.getByTestId('feedback-user-user-1')).toHaveTextContent('jane@example.com');
  });
});
