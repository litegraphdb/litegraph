import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import RenameThreadModal from '@/page/ai/chat/components/RenameThreadModal';
import { ChatThread } from '@/lib/sdk/chat';

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { error: jest.fn(), success: jest.fn() },
}));

const mockUpdateThread = jest.fn();

jest.mock('@/lib/store/slice/slice', () => ({
  useUpdateChatThreadMutation: () => [mockUpdateThread, { isLoading: false }],
}));

const thread: ChatThread = {
  GUID: 'thread-1',
  TenantGUID: 'tenant-1',
  UserGUID: 'user-1',
  Title: 'Old title',
  CreatedUtc: '2026-01-01T00:00:00Z',
  LastUpdateUtc: '2026-01-01T00:00:00Z',
};

describe('RenameThreadModal', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockUpdateThread.mockResolvedValue({ data: { ...thread, Title: 'New title' } });
  });

  it('renders with the current title and submits the trimmed new title', async () => {
    const onClose = jest.fn();
    render(<RenameThreadModal tenantGuid="tenant-1" thread={thread} onClose={onClose} />);

    expect(screen.getByText('Rename conversation')).toBeInTheDocument();
    const input = screen.getByTestId('chat-rename-thread-input') as HTMLInputElement;
    expect(input.value).toBe('Old title');

    fireEvent.change(input, { target: { value: '  New title  ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(mockUpdateThread).toHaveBeenCalledWith({
        tenantGuid: 'tenant-1',
        threadGuid: 'thread-1',
        body: { Title: 'New title' },
      });
    });
    await waitFor(() => {
      expect(onClose).toHaveBeenCalled();
    });
  });
});
