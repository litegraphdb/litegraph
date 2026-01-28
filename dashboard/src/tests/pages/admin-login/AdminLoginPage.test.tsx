import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { createMockInitialState } from '../../store/mockStore';
import { mockTenantData } from '../mockData';
import AdminLoginPage from '@/app/login/admin/page';
import { renderWithRedux } from '@/tests/store/utils';
import { getServer } from '@/tests/server';
import { commonHandlers } from '@/tests/handler';
import { handlers } from './handler';

jest.mock('@/lib/sdk/litegraph.service', () => ({
  setAccessKey: jest.fn(),
  setEndpoint: jest.fn(),
  useGetTenants: jest.fn(() => ({
    getTenants: jest.fn(),
    isLoading: false,
  })),
  useValidateConnectivity: jest.fn(() => ({
    validateConnectivity: jest.fn(),
    isLoading: false,
  })),
}));

const server = getServer([...commonHandlers, ...handlers]);

describe('AdminLoginPage', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('should render the AdminLoginPage with form fields', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    expect(screen.getByLabelText('LiteGraph Server URL')).toBeVisible();
    expect(screen.getByLabelText('Access Key')).toBeVisible();
    expect(screen.getByRole('button', { name: /login/i })).toBeVisible();
    expect(screen.getByPlaceholderText('https://your-litegraph-server.com')).toBeVisible();
    expect(screen.getByPlaceholderText('Enter your access key')).toBeVisible();
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should display validation errors for required fields', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const submitButton = screen.getByRole('button', { name: /login/i });
    fireEvent.click(submitButton);

    await waitFor(
      () => {
        expect(screen.getByText('Please enter the LiteGraph Server URL!')).toBeVisible();
        expect(screen.getByText('Please input your access key!')).toBeVisible();
      },
      { timeout: 10000 }
    );
  }, 15000);

  it('should validate URL format and show error for invalid URL', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    fireEvent.change(urlInput, { target: { value: 'invalid-url' } });

    const submitButton = screen.getByRole('button', { name: /login/i });
    fireEvent.click(submitButton);

    await waitFor(
      () => {
        expect(screen.getByText('Please enter a valid URL!')).toBeVisible();
      },
      { timeout: 10000 }
    );
  }, 15000);

  it('should reject non-HTTP/HTTPS URLs', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    fireEvent.change(urlInput, { target: { value: 'ftp://example.com' } });

    const submitButton = screen.getByRole('button', { name: /login/i });
    fireEvent.click(submitButton);

    await waitFor(
      () => {
        expect(screen.getByText('Only HTTP or HTTPS URLs are allowed!')).toBeVisible();
      },
      { timeout: 10000 }
    );
  }, 20000);

  it('should validate server URL on blur', async () => {
    const mockValidateConnectivity = jest.fn().mockResolvedValue(true);
    const mockSetEndpoint = jest.fn();

    require('@/lib/sdk/litegraph.service').useValidateConnectivity.mockReturnValue({
      validateConnectivity: mockValidateConnectivity,
      isLoading: false,
    });
    require('@/lib/sdk/litegraph.service').setEndpoint = mockSetEndpoint;

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    fireEvent.change(urlInput, { target: { value: 'https://example.com' } });
    fireEvent.blur(urlInput);

    await waitFor(
      () => {
        expect(mockSetEndpoint).toHaveBeenCalledWith('https://example.com');
        expect(mockValidateConnectivity).toHaveBeenCalled();
      },
      { timeout: 10000 }
    );
  });

  it('should submit form when Enter key is pressed', async () => {
    const mockGetTenants = jest.fn().mockResolvedValue(mockTenantData);

    require('@/lib/sdk/litegraph.service').useGetTenants.mockReturnValue({
      getTenants: mockGetTenants,
      isLoading: false,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    const accessKeyInput = screen.getByPlaceholderText('Enter your access key');

    fireEvent.change(urlInput, { target: { value: 'https://example.com' } });
    fireEvent.change(accessKeyInput, { target: { value: 'test-access-key' } });

    fireEvent.keyPress(accessKeyInput, { key: 'Enter', charCode: 13 });

    await waitFor(
      () => {
        expect(mockGetTenants).toHaveBeenCalled();
      },
      { timeout: 10000 }
    );
  });

  it('should show loading state during server validation', async () => {
    require('@/lib/sdk/litegraph.service').useValidateConnectivity.mockReturnValue({
      validateConnectivity: jest.fn(),
      isLoading: true,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    const submitButton = screen.getByRole('button', { name: /login/i });

    expect(urlInput).toBeDisabled();
    expect(submitButton).toBeDisabled();
  });

  it('should show loading state during login', async () => {
    require('@/lib/sdk/litegraph.service').useGetTenants.mockReturnValue({
      getTenants: jest.fn(),
      isLoading: true,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const submitButton = screen.getByRole('button', { name: /login/i });
    expect(submitButton).toBeDisabled();
    expect(submitButton).toHaveClass('ant-btn-loading');
  });

  it('should disable access key field when server is not validated', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState);

    const accessKeyInput = screen.getByPlaceholderText('Enter your access key');
    expect(accessKeyInput).toBeDisabled();
  });

  it('should handle Enter key press in access key field', async () => {
    const mockGetTenants = jest.fn().mockResolvedValue(mockTenantData);

    require('@/lib/sdk/litegraph.service').useGetTenants.mockReturnValue({
      getTenants: mockGetTenants,
      isLoading: false,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    const accessKeyInput = screen.getByPlaceholderText('Enter your access key');

    fireEvent.change(urlInput, { target: { value: 'https://example.com' } });
    fireEvent.change(accessKeyInput, { target: { value: 'test-access-key' } });

    fireEvent.keyDown(accessKeyInput, { key: 'Enter', code: 'Enter' });

    await waitFor(
      () => {
        expect(mockGetTenants).toHaveBeenCalled();
      },
      { timeout: 10000 }
    );
  });

  it('should handle form submission with both loading states disabled', async () => {
    const mockGetTenants = jest.fn().mockResolvedValue(mockTenantData);

    require('@/lib/sdk/litegraph.service').useGetTenants.mockReturnValue({
      getTenants: mockGetTenants,
      isLoading: false,
    });
    require('@/lib/sdk/litegraph.service').useValidateConnectivity.mockReturnValue({
      validateConnectivity: jest.fn(),
      isLoading: false,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const submitButton = screen.getByRole('button', { name: /login/i });
    expect(submitButton).not.toBeDisabled();
    expect(submitButton).not.toHaveClass('ant-btn-loading');
  });

  it('should not call validateConnectivity if URL field is empty on blur', async () => {
    const mockValidateConnectivity = jest.fn();

    require('@/lib/sdk/litegraph.service').useValidateConnectivity.mockReturnValue({
      validateConnectivity: mockValidateConnectivity,
      isLoading: false,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    fireEvent.blur(urlInput);

    expect(mockValidateConnectivity).not.toHaveBeenCalled();
  });

  it('should handle successful form submission and dispatch tenant to store', async () => {
    const mockGetTenants = jest.fn().mockResolvedValue(mockTenantData);
    const mockDispatch = jest.fn();

    require('@/lib/sdk/litegraph.service').useGetTenants.mockReturnValue({
      getTenants: mockGetTenants,
      isLoading: false,
    });

    // Mock useAppDispatch
    const mockUseAppDispatch = jest.fn(() => mockDispatch);
    jest.doMock('@/lib/store/hooks', () => ({
      useAppDispatch: mockUseAppDispatch,
    }));

    const initialState = createMockInitialState();
    renderWithRedux(<AdminLoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    const accessKeyInput = screen.getByPlaceholderText('Enter your access key');

    fireEvent.change(urlInput, { target: { value: 'https://example.com' } });
    fireEvent.change(accessKeyInput, { target: { value: 'test-access-key' } });

    const submitButton = screen.getByRole('button', { name: /login/i });
    fireEvent.click(submitButton);

    await waitFor(
      () => {
        expect(mockGetTenants).toHaveBeenCalled();
      },
      { timeout: 10000 }
    );
  });
});
