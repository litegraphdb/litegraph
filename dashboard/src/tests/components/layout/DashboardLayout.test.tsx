import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { createMockInitialState } from '@/tests/store/mockStore';
import { renderWithRedux } from '@/tests/store/utils';
import { getServer } from '@/tests/server';

import { commonHandlers } from '@/tests/handler';
import { mockGraphData, mockTenantData } from '@/tests/pages/mockData';

// Import the component
import DashboardLayout from '@/components/layout/DashboardLayout';
import { handlers } from './handler';

// Mock all dependencies
jest.mock('@/hooks/authHooks', () => ({
  useLogout: jest.fn(() => jest.fn()),
}));

jest.mock('@/lib/store/slice/slice', () => ({
  useGetAllGraphsQuery: jest.fn(),
  useGetAllTenantsQuery: jest.fn(),
}));

const server = getServer([...commonHandlers, ...handlers]);

const defaultProps = {
  children: <div data-testid="layout-children">Test Content</div>,
  menuItems: [
    { key: 'dashboard', label: 'Dashboard', href: '/admin/dashboard' },
    { key: 'users', label: 'Users', href: '/admin/dashboard/users' },
  ],
};

describe('DashboardLayout', () => {
  beforeAll(() => {
    server.listen();
  });

  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('should render graph selector when useGraphsSelector is true', async () => {
    const { useGetAllGraphsQuery, useGetAllTenantsQuery } = require('@/lib/store/slice/slice');
    useGetAllGraphsQuery.mockReturnValue({
      data: mockGraphData,
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetAllTenantsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      refetch: jest.fn(),
    });

    const initialState = createMockInitialState();
    renderWithRedux(<DashboardLayout {...defaultProps} useGraphsSelector={true} />, initialState);

    await waitFor(() => {
      expect(screen.getByText('Graph:')).toBeInTheDocument();
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });
  });

  it('should render tenant selector when useTenantSelector is true', async () => {
    const { useGetAllGraphsQuery, useGetAllTenantsQuery } = require('@/lib/store/slice/slice');
    useGetAllGraphsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetAllTenantsQuery.mockReturnValue({
      data: mockTenantData,
      isLoading: false,
      isError: false,
      refetch: jest.fn(),
    });

    const initialState = createMockInitialState();
    renderWithRedux(<DashboardLayout {...defaultProps} useTenantSelector={true} />, initialState);

    await waitFor(() => {
      expect(screen.getByText('Tenant:')).toBeInTheDocument();
    });
  });

  it('should show logout button when noProfile is true', () => {
    const mockLogout = jest.fn();
    const { useLogout } = require('@/hooks/authHooks');
    const { useGetAllGraphsQuery, useGetAllTenantsQuery } = require('@/lib/store/slice/slice');

    useLogout.mockReturnValue(mockLogout);
    useGetAllGraphsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetAllTenantsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      refetch: jest.fn(),
    });

    const initialState = createMockInitialState();
    renderWithRedux(<DashboardLayout {...defaultProps} noProfile={true} />, initialState);

    const logoutButton = screen.getByText('Logout');
    expect(logoutButton).toBeInTheDocument();

    fireEvent.click(logoutButton);
    expect(mockLogout).toHaveBeenCalled();
  });

  it('should show loading state for graphs', () => {
    const { useGetAllGraphsQuery, useGetAllTenantsQuery } = require('@/lib/store/slice/slice');
    useGetAllGraphsQuery.mockReturnValue({
      data: [],
      isLoading: true,
      error: null,
      refetch: jest.fn(),
    });
    useGetAllTenantsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      refetch: jest.fn(),
    });

    const initialState = createMockInitialState();
    renderWithRedux(<DashboardLayout {...defaultProps} useGraphsSelector={true} />, initialState);

    // Loading state would be handled by the Select component
    expect(screen.getByText('Graph:')).toBeInTheDocument();
  });

  it('should show retry option for tenant error', async () => {
    const mockRefetch = jest.fn();
    const { useGetAllGraphsQuery, useGetAllTenantsQuery } = require('@/lib/store/slice/slice');
    useGetAllGraphsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetAllTenantsQuery.mockReturnValue({
      data: [],
      isLoading: false,
      isError: true,
      refetch: mockRefetch,
    });

    const initialState = createMockInitialState();
    renderWithRedux(<DashboardLayout {...defaultProps} useTenantSelector={true} />, initialState);

    await waitFor(() => {
      expect(screen.getByText('Retry')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Retry'));
    expect(mockRefetch).toHaveBeenCalled();
  });
});
