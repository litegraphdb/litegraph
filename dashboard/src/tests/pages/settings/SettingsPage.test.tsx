import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import SettingsPage from '@/page/settings/SettingsPage';

const sampleSettings = {
  RequestTimeoutSeconds: 60,
  Logging: { Enable: true, ConsoleLogging: true, MinimumSeverity: 0, LogDirectory: './logs/' },
  Caching: { Enable: true, Capacity: 1000, EvictCount: 100 },
  RequestHistory: { Enable: true, RetentionDays: 30 },
  Observability: { Enable: true, MetricsPath: '/metrics' },
  Debug: { Exceptions: true },
  Rest: { Hostname: '*', Port: 8701, Ssl: { Enable: false } },
  LiteGraph: {
    AdminBearerToken: 'secret-token',
    Database: { Type: 'Sqlite', Password: 'db-pass' },
  },
  Encryption: { Key: 'k', Iv: 'iv' },
  Cluster: {
    Enable: true,
    ClusterName: 'litegraph',
    NodeId: 'litegraph-1',
    TrustedProxies: ['10.0.0.0/8'],
    Clutch: { Endpoint: 'http://clutch-lb:8090', AccessKey: 'clutch-secret', LeaseMs: 30000 },
    Redis: { ConnectionString: 'redis:6379', PollIntervalMs: 2000 },
  },
};

const singleNodeStatus = {
  ClusterEnabled: false,
  ClusterName: null,
  AnsweredBy: 'host-1',
  RegistryAvailable: null,
  SettingsVersion: 0,
  RestartVersion: 0,
  Utc: '2026-09-30T00:00:00.000000Z',
  Nodes: [
    {
      NodeId: 'host-1',
      Version: '10.0.0',
      StartedUtc: '2026-09-30T00:00:00.000000Z',
      LastHeartbeatUtc: '2026-09-30T00:00:00.000000Z',
      HeartbeatAgeMs: 0,
      State: 'Healthy',
      Checks: { Database: true, Clutch: null, Redis: null, Draining: false },
      SettingsVersion: 0,
      RestartPending: false,
      RestartVersion: 0,
    },
  ],
};

const clusterStatus = {
  ...singleNodeStatus,
  ClusterEnabled: true,
  ClusterName: 'litegraph',
  AnsweredBy: 'litegraph-1',
  RegistryAvailable: true,
  SettingsVersion: 3,
  Nodes: ['litegraph-1', 'litegraph-2', 'litegraph-3'].map((id, i) => ({
    ...singleNodeStatus.Nodes[0],
    NodeId: id,
    HeartbeatAgeMs: 1200,
    State: i === 1 ? 'Restarting' : 'Healthy',
    Checks: { Database: true, Clutch: true, Redis: true, Draining: false },
    SettingsVersion: 3,
    RestartPending: i === 2,
  })),
};

const mockRefetch = jest.fn();
const mockRefetchCluster = jest.fn();
const mockUpdate = jest.fn();
const mockRestart = jest.fn();
const mockValidate = jest.fn().mockResolvedValue(true);
let mockClusterStatus: any = singleNodeStatus;

jest.mock('@/lib/store/slice/slice', () => ({
  useGetServerSettingsQuery: () => ({
    data: sampleSettings,
    isLoading: false,
    isFetching: false,
    error: undefined,
    refetch: mockRefetch,
  }),
  useUpdateServerSettingsMutation: () => [mockUpdate, { isLoading: false }],
  useRestartServerMutation: () => [mockRestart, { isLoading: false }],
  useGetClusterNodesQuery: () => ({ data: mockClusterStatus, refetch: mockRefetchCluster }),
}));

jest.mock('@/lib/sdk/litegraph.service', () => ({
  useValidateConnectivity: () => ({ validateConnectivity: mockValidate, isLoading: false }),
}));

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { error: jest.fn(), success: jest.fn() },
}));

describe('SettingsPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockValidate.mockResolvedValue(true);
    mockClusterStatus = singleNodeStatus;
  });

  it('renders the current settings into sectioned fields', () => {
    render(<SettingsPage />);
    expect(screen.getByTestId('settings-section-logging')).toBeInTheDocument();
    expect(screen.getByTestId('settings-section-security')).toBeInTheDocument();
    // A current value is rendered into an input.
    const logDir = screen.getByLabelText('Log directory') as HTMLInputElement;
    expect(logDir.value).toBe('./logs/');
  });

  it('marks dirty and enables Save only after an edit', () => {
    render(<SettingsPage />);
    const save = screen.getByTestId('settings-save');
    expect(save).toBeDisabled();

    fireEvent.change(screen.getByLabelText('Log directory'), { target: { value: './new-logs/' } });
    expect(save).not.toBeDisabled();
  });

  it('saves the full payload and reflects live-vs-restart status', async () => {
    mockUpdate.mockResolvedValue({
      data: { Success: true, AppliedLive: ['Logging'], RestartRequired: ['Rest'] },
    });
    render(<SettingsPage />);

    fireEvent.change(screen.getByLabelText('Log directory'), { target: { value: './new-logs/' } });
    fireEvent.click(screen.getByTestId('settings-save'));

    await waitFor(() => expect(mockUpdate).toHaveBeenCalledTimes(1));
    const payload = mockUpdate.mock.calls[0][0];
    expect(payload.Logging.LogDirectory).toBe('./new-logs/');
    // Untouched values are preserved in the full document.
    expect(payload.Rest.Port).toBe(8701);

    await waitFor(() => expect(screen.getByText('Applied live')).toBeInTheDocument());
    expect(screen.getByText('Restart required')).toBeInTheDocument();
  });

  it('requires confirmation to restart and then shows the reconnecting state', async () => {
    mockRestart.mockResolvedValue({ data: { Success: true } });
    render(<SettingsPage />);

    // No reconnecting state until confirmed.
    expect(screen.queryByTestId('settings-reconnecting')).not.toBeInTheDocument();
    fireEvent.click(screen.getByTestId('settings-restart'));

    // Confirm in the modal.
    // Generous waits: the page renders many sections, and the reconnecting state clears after the first 3 s poll.
    const okButton = await screen.findByRole('button', { name: 'OK' }, { timeout: 2500 });
    fireEvent.click(okButton);

    await waitFor(() => expect(mockRestart).toHaveBeenCalledTimes(1), { timeout: 2500 });
    await waitFor(
      () => expect(screen.getByTestId('settings-reconnecting')).toBeInTheDocument(),
      { timeout: 2500 }
    );
  }, 15000);

  it('shows the single node on a single-node server', () => {
    render(<SettingsPage />);
    expect(screen.getByTestId('settings-cluster-card')).toBeInTheDocument();
    expect(screen.getByText('This server runs as a single node.')).toBeInTheDocument();
    expect(screen.getByText('host-1')).toBeInTheDocument();
    expect(screen.getByText('Restart Server')).toBeInTheDocument();
  });

  it('lists every cluster node with its state and pending restart', () => {
    mockClusterStatus = clusterStatus;
    render(<SettingsPage />);
    expect(screen.getByText('litegraph-1')).toBeInTheDocument();
    expect(screen.getByText('litegraph-2')).toBeInTheDocument();
    expect(screen.getByText('litegraph-3')).toBeInTheDocument();
    expect(screen.getByText('Restarting')).toBeInTheDocument();
    expect(screen.getAllByText('Yes')).toHaveLength(1);
    expect(screen.getByText('Restart Cluster')).toBeInTheDocument();
  });

  it('requests a rolling restart in cluster mode without waiting to reconnect', async () => {
    mockClusterStatus = clusterStatus;
    mockRestart.mockResolvedValue({ data: { Restarting: true, Rolling: true, RestartVersion: 4 } });
    render(<SettingsPage />);

    fireEvent.click(screen.getByTestId('settings-restart'));
    expect(await screen.findByText('Restart every node?')).toBeInTheDocument();
    fireEvent.click(await screen.findByRole('button', { name: 'OK' }));

    await waitFor(() => expect(mockRestart).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(mockRefetchCluster).toHaveBeenCalled());
    expect(screen.queryByTestId('settings-reconnecting')).not.toBeInTheDocument();
    expect(mockValidate).not.toHaveBeenCalled();
  });

  it('reports settings that came from the environment after a save', async () => {
    mockUpdate.mockResolvedValue({
      data: {
        Success: true,
        AppliedLive: [],
        RestartRequired: ['Logging'],
        EnvironmentOverrides: ['Encryption.Key', 'Cluster.NodeId'],
      },
    });
    render(<SettingsPage />);
    fireEvent.change(screen.getByLabelText('Log directory'), { target: { value: './new-logs/' } });
    fireEvent.click(screen.getByTestId('settings-save'));

    await waitFor(() => expect(screen.getByTestId('settings-environment-overrides')).toBeInTheDocument());
  });

  it('renders the Cluster section with a read-only node identifier and masked secrets', () => {
    render(<SettingsPage />);
    expect(screen.getByTestId('settings-section-cluster')).toBeInTheDocument();
    const nodeId = screen.getByLabelText('Node identifier') as HTMLInputElement;
    expect(nodeId.value).toBe('litegraph-1');
    expect(nodeId).toBeDisabled();
    expect(screen.getByText('Set per node by the LITEGRAPH_NODE_ID environment variable.')).toBeInTheDocument();
    expect((screen.getByLabelText('Clutch access key') as HTMLInputElement).type).toBe('password');
    expect((screen.getByLabelText('Redis connection string') as HTMLInputElement).type).toBe('password');
    expect((screen.getByLabelText('Trusted proxies') as HTMLInputElement).value).toBe('10.0.0.0/8');
    expect(screen.getByText('Allowed range 5,000 to 300,000')).toBeInTheDocument();
  });

  it('shows the every-node banner and the caching note in cluster mode only', () => {
    mockClusterStatus = singleNodeStatus;
    const { unmount } = render(<SettingsPage />);
    expect(screen.queryByTestId('settings-cluster-banner')).not.toBeInTheDocument();
    expect(screen.queryByTestId('settings-caching-cluster-note')).not.toBeInTheDocument();
    unmount();

    mockClusterStatus = clusterStatus;
    render(<SettingsPage />);
    expect(screen.getByTestId('settings-cluster-banner')).toBeInTheDocument();
    expect(screen.getByTestId('settings-cluster-banner-link')).toHaveAttribute('href', '/dashboard/cluster');
    expect(screen.getByTestId('settings-caching-cluster-note')).toBeInTheDocument();
  });

  it('marks fields that the environment supplies after a save', async () => {
    mockUpdate.mockResolvedValue({
      data: {
        Success: true,
        AppliedLive: [],
        RestartRequired: ['Cluster'],
        EnvironmentOverrides: ['Cluster.NodeId', 'Cluster.Clutch.AccessKey'],
      },
    });
    render(<SettingsPage />);
    fireEvent.change(screen.getByLabelText('Log directory'), { target: { value: './other/' } });
    fireEvent.click(screen.getByTestId('settings-save'));
    await waitFor(() => expect(screen.getByTestId('settings-env-Cluster.NodeId')).toBeInTheDocument());
    expect(screen.getByTestId('settings-env-Cluster.Clutch.AccessKey')).toHaveTextContent('Set by environment variable');
  });
});
