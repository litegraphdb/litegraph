import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import RequestHistoryDetailModal from '@/page/request-history/RequestHistoryDetailModal';
import { RequestHistoryEntry, getRequestHistoryDetail } from '@/lib/sdk/requestHistory';

jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

jest.mock('@/lib/sdk/requestHistory', () => {
  const actual = jest.requireActual('@/lib/sdk/requestHistory');
  return {
    ...actual,
    getRequestHistoryDetail: jest.fn().mockResolvedValue({
      RequestHeaders: { Accept: 'application/json' },
      ResponseHeaders: { 'Content-Type': 'application/json' },
      RequestBody: null,
      ResponseBody: '{}',
      TransactionDiagnosticsJson:
        '{"TransactionId":"11111111-1111-1111-1111-111111111111","OperationCount":2}',
    }),
  };
});

const requestEntry: RequestHistoryEntry = {
  GUID: '00000000-0000-0000-0000-000000000001',
  CreatedUtc: '2026-04-17T12:34:56Z',
  Method: 'GET',
  Path: '/v1.0/tenants',
  Url: 'http://localhost:8701/v1.0/tenants',
  StatusCode: 200,
  Success: true,
  ProcessingTimeMs: 12.5,
  RequestBodyLength: 0,
  ResponseBodyLength: 128,
  RequestBodyTruncated: false,
  ResponseBodyTruncated: false,
  SourceIp: '2001:db8:85a3::8a2e:370:7334',
  TransactionDiagnosticsJson:
    '{"TransactionId":"11111111-1111-1111-1111-111111111111","OperationCount":2}',
};

describe('RequestHistoryDetailModal', () => {
  it('keeps time and source IP values on one line', async () => {
    render(<RequestHistoryDetailModal entry={requestEntry} open={true} onClose={jest.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Request Detail')).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getAllByText('1 entries')).toHaveLength(2);
    });
    await waitFor(() => {
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
    });

    expect(screen.getByTestId('request-detail-time').style.whiteSpace).toBe('nowrap');
    expect(screen.getByTestId('request-detail-source-ip').style.whiteSpace).toBe('nowrap');
    expect(screen.getByTestId('request-detail-source-ip')).toHaveTextContent(requestEntry.SourceIp!);
    fireEvent.click(screen.getByText('Transaction Diagnostics'));
    expect(screen.getByText(/11111111-1111-1111-1111-111111111111/)).toBeInTheDocument();
    expect(document.querySelector('.ant-modal')?.getAttribute('style')).toContain('1688px');
    expect(document.querySelector('.ant-modal')?.getAttribute('style')).toContain(
      'calc(100vw - 32px)'
    );
  });

  it('renders an SSE response body as parsed events with reconstructed output', async () => {
    const sseBody =
      'data: {"event":"started","threadGuid":"t1","turnGuid":"u1"}\n\n' +
      'data: {"event":"delta","content":"Hello "}\n\n' +
      'data: {"event":"delta","content":"world"}\n\n' +
      'data: [DONE]\n\n';
    (getRequestHistoryDetail as jest.Mock).mockResolvedValueOnce({
      RequestHeaders: { Accept: 'text/event-stream' },
      ResponseHeaders: { 'Content-Type': 'text/event-stream' },
      RequestBody: null,
      ResponseBody: sseBody,
    });

    render(<RequestHistoryDetailModal entry={requestEntry} open={true} onClose={jest.fn()} />);

    await waitFor(() => {
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
    });

    expect(document.querySelector('.ant-modal')?.getAttribute('style')).toContain('top: 16px');

    fireEvent.click(screen.getByText('Response Body'));

    await waitFor(() => {
      expect(screen.getByTestId('request-detail-sse')).toBeInTheDocument();
    });
    const sseView = screen.getByTestId('request-detail-sse');
    expect(sseView).toHaveTextContent('Reconstructed output');
    expect(sseView).toHaveTextContent('Hello world');
    expect(sseView).toHaveTextContent('4 streamed events');
    expect(sseView).toHaveTextContent('[DONE]');
  });
});
