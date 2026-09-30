import '@testing-library/jest-dom';
import React from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import RequestHistoryPage from '@/page/request-history/RequestHistoryPage';
import { setEndpoint } from '@/lib/sdk/litegraph.service';
import { listRequestHistory } from '@/lib/sdk/requestHistory';
import { getClusterNodes } from '@/lib/sdk/cluster';

jest.mock('@/lib/sdk/cluster', () => ({
  getClusterNodes: jest.fn(),
}));

jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

jest.mock('@/lib/sdk/requestHistory', () => {
  const actual = jest.requireActual('@/lib/sdk/requestHistory');
  return {
    ...actual,
    deleteRequestHistory: jest.fn(),
    getRequestHistorySummary: jest.fn().mockResolvedValue({
      StartUtc: '2026-04-17T00:00:00Z',
      EndUtc: '2026-04-17T01:00:00Z',
      Interval: 'minute',
      TotalSuccess: 1,
      TotalFailure: 1,
      TotalRequests: 2,
      Data: [],
    }),
    listRequestHistory: jest.fn().mockResolvedValue({
      Objects: [
        {
          GUID: 'request-1',
          CreatedUtc: '2026-04-17T00:00:00Z',
          NodeId: 'litegraph-2',
          Method: 'GET',
          Path: '/v1.0/tenants',
          Url: 'http://localhost/v1.0/tenants',
          StatusCode: 200,
          Success: true,
          ProcessingTimeMs: 10,
          RequestBodyLength: 0,
          ResponseBodyLength: 100,
          RequestBodyTruncated: false,
          ResponseBodyTruncated: false,
        },
        {
          GUID: 'request-2',
          CreatedUtc: '2026-04-17T00:01:00Z',
          Method: 'POST',
          Path: '/v1.0/tenants/default/graphs/graph-1/transaction',
          Url: 'http://localhost/v1.0/tenants/default/graphs/graph-1/transaction',
          StatusCode: 400,
          Success: false,
          ProcessingTimeMs: 40,
          RequestBodyLength: 32,
          ResponseBodyLength: 128,
          RequestBodyTruncated: false,
          ResponseBodyTruncated: false,
          TransactionDiagnosticsJson:
            '{"TransactionId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","State":"Faulted","Success":false,"ValidationFailure":true,"RolledBack":false,"IsolationLevel":"Serializable","Provider":"Sqlite","ProviderErrorCode":"SQLITE_BUSY","Retryable":true,"ConcurrencyConflict":true}',
        },
      ],
      Success: true,
      MaxResults: 25,
      EndOfResults: true,
      TotalRecords: 2,
      RecordsRemaining: 0,
    }),
  };
});

describe('RequestHistoryPage observability', () => {
  const listRequestHistoryMock = listRequestHistory as jest.Mock;

  const getClusterNodesMock = getClusterNodes as jest.Mock;

  beforeEach(() => {
    setEndpoint('http://localhost:8701/');
    listRequestHistoryMock.mockClear();
    getClusterNodesMock.mockReset();
    getClusterNodesMock.mockRejectedValue(new Error('HTTP 401 Unauthorized'));
  });

  const openNodePicker = () => {
    const picker = screen.getByTestId('request-history-node-filter');
    const selector = picker.querySelector('.ant-select-selector') ?? picker;
    fireEvent.mouseDown(selector);
  };

  it('renders operational telemetry links and visible request statistics', async () => {
    render(<RequestHistoryPage mode="admin" />);

    expect(screen.getByText('Operational telemetry')).toBeInTheDocument();

    expect(screen.getByRole('link', { name: 'Prometheus metrics' })).toHaveAttribute(
      'href',
      'http://localhost:8701/metrics'
    );
    expect(screen.getByRole('link', { name: 'OpenTelemetry setup' })).toHaveAttribute(
      'href',
      'https://opentelemetry.io/docs/'
    );
    await waitFor(() => {
      expect(screen.getByTestId('request-history-observability-summary')).toHaveTextContent(
        'Visible requests: 2'
      );
    });
    expect(screen.getByTestId('request-history-observability-summary')).toHaveTextContent(
      'Visible errors: 1'
    );
    expect(screen.getByTestId('request-history-observability-summary')).toHaveTextContent(
      'Error rate: 50.0%'
    );
    expect(screen.getByTestId('request-history-observability-summary')).toHaveTextContent(
      'Average duration: 25.0 ms'
    );
    expect(screen.getByTestId('request-history-observability-summary')).toHaveTextContent(
      'P95 duration: 40.0 ms'
    );
    expect(screen.getByTestId('request-history-filter-controls').style.justifyContent).toBe(
      'flex-end'
    );
    expect(screen.getAllByTestId('request-history-stat-value')[0].style.fontFamily).toBe(
      'monospace'
    );
    expect(screen.getAllByTestId('request-history-time')[0].style.whiteSpace).toBe('nowrap');
    expect(screen.getAllByTestId('request-history-method')[0].style.whiteSpace).toBe('nowrap');
    expect(screen.getByTestId('request-history-transaction-id')).toHaveTextContent('aaaaaaaa');
    expect(screen.getByTestId('request-history-transaction-state')).toHaveTextContent(
      'validation'
    );
    expect(screen.getByText('Serializable')).toBeInTheDocument();
    expect(screen.getByText('Sqlite')).toBeInTheDocument();
    expect(screen.getByText('SQLITE_BUSY')).toBeInTheDocument();
    expect(screen.getByText('retryable')).toBeInTheDocument();
    expect(screen.getByText('conflict')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Transaction ID...')).toBeInTheDocument();
  });

  it('passes the transaction ID filter to request history search', async () => {
    render(<RequestHistoryPage mode="admin" />);

    await waitFor(() => {
      expect(screen.getByTestId('request-history-observability-summary')).toHaveTextContent(
        'Visible requests: 2'
      );
    });

    await act(async () => {
      fireEvent.change(screen.getByPlaceholderText('Transaction ID...'), {
        target: { value: 'aaaaaaaa' },
      });
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(listRequestHistoryMock).toHaveBeenLastCalledWith(
        expect.objectContaining({ transactionId: 'aaaaaaaa' })
      );
    });
  });

  it('uses an actions context menu with view, JSON, and delete options', async () => {
    render(<RequestHistoryPage mode="admin" />);

    await waitFor(() => {
      expect(screen.getAllByLabelText('Request actions')).toHaveLength(2);
    });

    fireEvent.click(screen.getAllByLabelText('Request actions')[0]);

    await waitFor(() => {
      expect(screen.getByRole('menuitem', { name: /View$/ })).toBeInTheDocument();
    });
    expect(screen.getByRole('menuitem', { name: /View JSON/ })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /Delete/ })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('menuitem', { name: /View JSON/ }));

    expect(screen.getByText('Request History Entry JSON')).toBeInTheDocument();
    expect(screen.getByTestId('view-json-content')).toHaveTextContent('request-1');
    expect(screen.queryByText('Request Detail')).not.toBeInTheDocument();
  });

  it('offers the cluster nodes in the node picker and filters by the selected node', async () => {
    getClusterNodesMock.mockResolvedValue({
      ClusterEnabled: true,
      Nodes: [{ NodeId: 'litegraph-1' }, { NodeId: 'litegraph-3' }],
    });
    render(<RequestHistoryPage mode="admin" />);

    await waitFor(() => {
      expect(screen.getByTestId('request-history-node')).toHaveTextContent('litegraph-2');
    });
    await waitFor(() => expect(getClusterNodesMock).toHaveBeenCalled());

    await act(async () => {
      openNodePicker();
      await Promise.resolve();
    });

    const option = await screen.findByTitle('litegraph-3');
    expect(screen.getByTitle('litegraph-1')).toBeInTheDocument();
    // The cluster list is authoritative, so the page's own node is not added.
    expect(screen.queryByTitle('litegraph-2')).not.toBeInTheDocument();

    await act(async () => {
      fireEvent.click(option);
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(listRequestHistoryMock).toHaveBeenLastCalledWith(
        expect.objectContaining({ nodeId: 'litegraph-3', page: 0 })
      );
    });
  });

  it('falls back to the nodes in the loaded page when the cluster list cannot be read', async () => {
    render(<RequestHistoryPage mode="admin" />);

    await waitFor(() => {
      expect(screen.getByTestId('request-history-node')).toHaveTextContent('litegraph-2');
    });

    await act(async () => {
      openNodePicker();
      await Promise.resolve();
    });

    const option = await screen.findByTitle('litegraph-2');
    await act(async () => {
      fireEvent.click(option);
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(listRequestHistoryMock).toHaveBeenLastCalledWith(
        expect.objectContaining({ nodeId: 'litegraph-2', page: 0 })
      );
    });
  });
});
