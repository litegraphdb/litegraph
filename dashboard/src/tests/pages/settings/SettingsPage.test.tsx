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
};

const mockRefetch = jest.fn();
const mockUpdate = jest.fn();
const mockRestart = jest.fn();
const mockValidate = jest.fn().mockResolvedValue(true);

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
    const okButton = await screen.findByRole('button', { name: 'OK' });
    fireEvent.click(okButton);

    await waitFor(() => expect(mockRestart).toHaveBeenCalledTimes(1));
    await waitFor(() =>
      expect(screen.getByTestId('settings-reconnecting')).toBeInTheDocument()
    );
  });
});
