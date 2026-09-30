import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ClusterPage, { progressFor, summarize } from '@/page/cluster/ClusterPage';
import toast from 'react-hot-toast';

const baseNode = {
  Hostname: 'host',
  Version: '10.0.0',
  StartedUtc: '2026-09-30T00:00:00.000000Z',
  LastHeartbeatUtc: '2026-09-30T00:10:00.000000Z',
  HeartbeatAgeMs: 1200,
  State: 'Healthy',
  Checks: { Database: true, Clutch: true, Redis: true, Draining: false },
  SettingsVersion: 3,
  RestartPending: false,
  RestartVersion: 0,
};

const clusterStatus = {
  ClusterEnabled: true,
  ClusterName: 'litegraph',
  AnsweredBy: 'litegraph-1',
  RegistryAvailable: true,
  SettingsVersion: 3,
  RestartVersion: 0,
  RestartRequestedUtc: null,
  Utc: '2026-09-30T00:10:00.000000Z',
  Nodes: [
    { ...baseNode, NodeId: 'litegraph-1' },
    { ...baseNode, NodeId: 'litegraph-2', SettingsVersion: 2, RestartPending: true },
    { ...baseNode, NodeId: 'litegraph-3', State: 'Offline', HeartbeatAgeMs: 60000 },
  ],
};

const singleNodeStatus = {
  ClusterEnabled: false,
  ClusterName: null,
  AnsweredBy: 'host-1',
  RegistryAvailable: null,
  SettingsVersion: 0,
  RestartVersion: 0,
  Utc: '2026-09-30T00:10:00.000000Z',
  Nodes: [{ ...baseNode, NodeId: 'host-1', Checks: { Database: true, Clutch: null, Redis: null, Draining: false } }],
};

let mockStatus: any = clusterStatus;
let mockError: any = undefined;
const mockRefetch = jest.fn();
const mockRestartCluster = jest.fn();
const mockRestartNode = jest.fn();
const mockRemoveNode = jest.fn();
const mockQueryArgs: any[] = [];

jest.mock('@/lib/store/slice/slice', () => ({
  useGetClusterNodesQuery: (arg: unknown, options: unknown) => {
    mockQueryArgs.push(options);
    return {
      data: mockStatus,
      error: mockError,
      isLoading: false,
      isFetching: false,
      refetch: mockRefetch,
      fulfilledTimeStamp: 1759190400000,
    };
  },
  useRestartClusterMutation: () => [mockRestartCluster, { isLoading: false }],
  useRestartClusterNodeMutation: () => [mockRestartNode, { isLoading: false }],
  useDeleteClusterNodeMutation: () => [mockRemoveNode, { isLoading: false }],
}));

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { error: jest.fn(), success: jest.fn() },
}));

const confirm = async () => {
  fireEvent.click(await screen.findByRole('button', { name: 'OK' }));
};

describe('ClusterPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockStatus = clusterStatus;
    mockError = undefined;
    mockQueryArgs.length = 0;
  });

  it('renders every node with summary cards and a 5 second refresh', () => {
    render(<ClusterPage />);
    expect(screen.getByText('litegraph-1')).toBeInTheDocument();
    expect(screen.getByText('litegraph-2')).toBeInTheDocument();
    expect(screen.getByText('litegraph-3')).toBeInTheDocument();
    expect(screen.getByTestId('cluster-card-nodes')).toHaveTextContent('2 / 3');
    expect(screen.getByTestId('cluster-card-name')).toHaveTextContent('litegraph');
    expect(screen.getByTestId('cluster-card-settings')).toHaveTextContent('1 behind, 1 awaiting restart');
    expect(screen.getByTestId('cluster-refresh-interval')).toHaveTextContent('Refreshes every 5 s');
    expect(mockQueryArgs[0]).toEqual({ pollingInterval: 5000 });
  });

  it('shows the single-node state without rolling restart or remove actions', () => {
    mockStatus = singleNodeStatus;
    render(<ClusterPage />);
    expect(screen.getByTestId('cluster-single-node')).toBeInTheDocument();
    expect(screen.getByTestId('cluster-card-name')).toHaveTextContent('Single node');
    expect(screen.queryByTestId('cluster-rolling-restart')).not.toBeInTheDocument();
    expect(screen.queryByTestId('cluster-remove-host-1')).not.toBeInTheDocument();
    expect(screen.getByTestId('cluster-restart-host-1')).not.toBeDisabled();
  });

  it('warns when the node registry is unavailable and disables restarts', () => {
    mockStatus = { ...clusterStatus, RegistryAvailable: false, Nodes: [clusterStatus.Nodes[0]] };
    render(<ClusterPage />);
    expect(screen.getByTestId('cluster-registry-unavailable')).toBeInTheDocument();
    expect(screen.getByTestId('cluster-rolling-restart')).toBeDisabled();
    expect(screen.getByTestId('cluster-restart-litegraph-1')).toBeDisabled();
  });

  it('restarts a node only after confirmation and pauses refresh while the modal is open', async () => {
    mockRestartNode.mockResolvedValue({ data: { Restarting: true, Rolling: false } });
    render(<ClusterPage />);
    fireEvent.click(screen.getByTestId('cluster-restart-litegraph-1'));
    expect(await screen.findByText('Restart node litegraph-1?')).toBeInTheDocument();
    expect(mockRestartNode).not.toHaveBeenCalled();
    expect(mockQueryArgs[mockQueryArgs.length - 1]).toEqual({ pollingInterval: 0 });
    expect(screen.getByTestId('cluster-refresh-interval')).toHaveTextContent('Auto-refresh paused');

    await confirm();
    await waitFor(() => expect(mockRestartNode).toHaveBeenCalledWith('litegraph-1'));
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
  });

  it('allows removing only offline or stopped nodes', async () => {
    mockRemoveNode.mockResolvedValue({ data: undefined });
    render(<ClusterPage />);
    expect(screen.getByTestId('cluster-remove-litegraph-1')).toBeDisabled();
    expect(screen.getByTestId('cluster-restart-litegraph-3')).toBeDisabled();
    const remove = screen.getByTestId('cluster-remove-litegraph-3');
    expect(remove).not.toBeDisabled();

    fireEvent.click(remove);
    expect(await screen.findByText('Remove node litegraph-3?')).toBeInTheDocument();
    await confirm();
    await waitFor(() => expect(mockRemoveNode).toHaveBeenCalledWith('litegraph-3'));
  });

  it('requests a rolling restart and shows progress per node', async () => {
    mockRestartCluster.mockResolvedValue({
      data: { Restarting: true, Rolling: true, RestartVersion: 2, RequestedUtc: '2026-09-30T00:05:00.000000Z' },
    });
    mockStatus = {
      ...clusterStatus,
      Nodes: [
        { ...clusterStatus.Nodes[0], StartedUtc: '2026-09-30T00:06:00.000000Z' },
        { ...clusterStatus.Nodes[1], State: 'Restarting' },
        { ...clusterStatus.Nodes[0], NodeId: 'litegraph-3' },
      ],
    };
    render(<ClusterPage />);
    fireEvent.click(screen.getByTestId('cluster-rolling-restart'));
    expect(await screen.findByText('Restart every node?')).toBeInTheDocument();
    await confirm();

    await waitFor(() => expect(mockRestartCluster).toHaveBeenCalledTimes(1));
    expect(await screen.findByTestId('cluster-progress')).toBeInTheDocument();
    expect(screen.getByTestId('cluster-progress-litegraph-1')).toHaveTextContent('Restarted');
    expect(screen.getByTestId('cluster-progress-litegraph-2')).toHaveTextContent('Restarting');
    expect(screen.getByTestId('cluster-progress-litegraph-3')).toHaveTextContent('Pending');
    expect(screen.queryByTestId('cluster-progress-complete')).not.toBeInTheDocument();
  });

  it('shows an error toast when the server refuses a restart', async () => {
    mockRestartCluster.mockResolvedValue({ error: new Error('Redis is unreachable') });
    render(<ClusterPage />);
    fireEvent.click(screen.getByTestId('cluster-rolling-restart'));
    await confirm();
    await waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith('Request failed: Redis is unreachable', expect.anything())
    );
    expect(screen.queryByTestId('cluster-progress')).not.toBeInTheDocument();
  });

  it('keeps showing the last node list with a reconnecting indicator when polling fails', () => {
    mockError = new Error('Failed to fetch');
    render(<ClusterPage />);
    expect(screen.getByTestId('cluster-reconnecting')).toBeInTheDocument();
    expect(screen.getByText('litegraph-1')).toBeInTheDocument();
  });

  it('filters nodes by state', async () => {
    render(<ClusterPage />);
    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'State' }));
    fireEvent.click(await screen.findByTitle('Offline'));
    await waitFor(() => expect(screen.queryByText('litegraph-1')).not.toBeInTheDocument());
    expect(screen.getByText('litegraph-3')).toBeInTheDocument();
    expect(screen.getByText('Showing 1 of 3 nodes')).toBeInTheDocument();
  });

  it('derives progress and summary counts', () => {
    const requested = '2026-09-30T00:05:00.000000Z';
    expect(progressFor({ ...baseNode, NodeId: 'a', StartedUtc: '2026-09-30T00:06:00Z' } as any, requested)).toBe('restarted');
    expect(progressFor({ ...baseNode, NodeId: 'a', State: 'Offline' } as any, requested)).toBe('restarting');
    expect(progressFor({ ...baseNode, NodeId: 'a' } as any, requested)).toBe('pending');
    expect(summarize(clusterStatus as any)).toEqual({ total: 3, healthy: 2, lagging: 1, pending: 1 });
  });
});
