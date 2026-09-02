import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import toast from 'react-hot-toast';
import LoginPage from '@/page/login/LoginPage';

// Isolate the page from the branded layout + context.
jest.mock('@/components/layout/LoginLayout', () => {
  return function MockLoginLayout({ children }: any) {
    return <div data-testid="login-layout">{children}</div>;
  };
});

const mockValidateConnectivity = jest.fn().mockResolvedValue(true);
jest.mock('@/lib/sdk/litegraph.service', () => ({
  setEndpoint: jest.fn(),
  setAccessKey: jest.fn(),
  useValidateConnectivity: () => ({
    validateConnectivity: mockValidateConnectivity,
    isLoading: false,
  }),
  useGetTenants: () => ({ getTenants: jest.fn().mockResolvedValue([]), isLoading: false }),
}));

jest.mock('@/hooks/appHooks', () => ({
  useCurrentlyHostedDomainAsServerUrl: () => '',
}));

jest.mock('@/hooks/authHooks', () => ({
  useCredentialsToLogin: () => jest.fn(),
  useAdminCredentialsToLogin: () => jest.fn(),
}));

jest.mock('@/lib/store/hooks', () => ({
  useAppDispatch: () => jest.fn(),
}));

const mockGetTenantsForEmail = jest.fn();
const mockGenerateToken = jest.fn();
jest.mock('@/lib/store/slice/slice', () => ({
  useGetTenantsForEmailMutation: () => [mockGetTenantsForEmail, { isLoading: false }],
  useGenerateTokenMutation: () => [mockGenerateToken, { isLoading: false }],
}));

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { error: jest.fn(), success: jest.fn() },
}));

const tenantA = { GUID: 't-1', Name: 'Tenant One' };
const tenantB = { GUID: 't-2', Name: 'Tenant Two' };

// GET /v1.0/token/tenants now returns the EnumerationResult envelope.
const envelope = (objects: unknown[]) => ({
  Success: true,
  MaxResults: 1000,
  EndOfResults: true,
  TotalRecords: objects.length,
  RecordsRemaining: 0,
  Objects: objects,
});

const clickNext = () => fireEvent.click(screen.getByRole('button', { name: /next/i }));

const advanceToTenantResolution = async () => {
  fireEvent.change(screen.getByPlaceholderText('https://your-litegraph-server.com'), {
    target: { value: 'https://server.example' },
  });
  clickNext();
  await waitFor(() => expect(screen.getByPlaceholderText('Email')).not.toBeDisabled());
  fireEvent.change(screen.getByPlaceholderText('Email'), {
    target: { value: 'user@example.com' },
  });
  clickNext();
};

describe('Unified login flow', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockValidateConnectivity.mockResolvedValue(true);
  });

  it('skips the tenant picker for a single-tenant email', async () => {
    mockGetTenantsForEmail.mockResolvedValue({ data: envelope([tenantA]) });
    render(<LoginPage />);

    await advanceToTenantResolution();

    await waitFor(() => expect(screen.getByPlaceholderText('Password')).not.toBeDisabled());
    expect(screen.queryByText('Tenant One')).not.toBeInTheDocument();
  });

  it('shows the tenant picker for a multi-tenant email', async () => {
    mockGetTenantsForEmail.mockResolvedValue({ data: envelope([tenantA, tenantB]) });
    render(<LoginPage />);

    await advanceToTenantResolution();

    await waitFor(() => expect(screen.getByText('Tenant Two')).toBeInTheDocument());
    expect(screen.getByText('Tenant One')).toBeInTheDocument();
  });

  it('surfaces a localized error on wrong password', async () => {
    mockGetTenantsForEmail.mockResolvedValue({ data: envelope([tenantA]) });
    mockGenerateToken.mockResolvedValue({ error: { status: 401 } });
    render(<LoginPage />);

    await advanceToTenantResolution();
    await waitFor(() => expect(screen.getByPlaceholderText('Password')).not.toBeDisabled());

    fireEvent.change(screen.getByPlaceholderText('Password'), {
      target: { value: 'wrong-password' },
    });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith('Incorrect email or password.'));
  });
});
