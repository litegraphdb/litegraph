import '@testing-library/jest-dom';
import React from 'react';
import { render, waitFor } from '@testing-library/react';
import { withAuth, withAdminAuth, forGuest } from '@/hoc/hoc';

// Mock all dependencies
jest.mock('@/components/base/loading/PageLoading', () => {
  return function MockPageLoading({ className }: { className?: string }) {
    return (
      <div data-testid="page-loading" className={className}>
        Loading...
      </div>
    );
  };
});

jest.mock('@/components/logout-fallback/LogoutFallBack', () => {
  return function MockLogoutFallBack({
    message,
    logoutPath,
  }: {
    message?: string;
    logoutPath?: string;
  }) {
    return (
      <div data-testid="logout-fallback" data-message={message} data-logout-path={logoutPath}>
        Logout Fallback
      </div>
    );
  };
});

jest.mock('@/constants/constant', () => ({
  paths: {
    adminLogin: '/admin/login',
    dashboardHome: '/dashboard',
    adminDashboard: '/admin/dashboard',
  },
}));

jest.mock('@/hooks/authHooks', () => ({
  useLogout: jest.fn(() => jest.fn()),
}));

jest.mock('@/hooks/hooks', () => ({
  useAppDynamicNavigation: jest.fn(() => ({
    serializePath: jest.fn((path) => path),
  })),
}));

jest.mock('@/lib/sdk/litegraph.service', () => ({
  setAccessKey: jest.fn(),
  setAccessToken: jest.fn(),
  setTenant: jest.fn(),
  useGetTenants: jest.fn(() => ({
    getTenants: jest.fn(),
    isLoading: false,
  })),
}));

jest.mock('@/lib/store/hooks', () => ({
  useAppDispatch: jest.fn(() => jest.fn()),
  useAppSelector: jest.fn(),
}));

jest.mock('@/lib/store/litegraph/actions', () => ({
  storeUser: jest.fn(),
}));

jest.mock('@/lib/store/slice/slice', () => ({
  useGetTokenDetailsMutation: jest.fn(() => [jest.fn(), { isLoading: false }]),
}));

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(() => ({
    push: jest.fn(),
  })),
}));

// Mock component for testing
const MockComponent = ({ testProp }: { testProp?: string }) => (
  <div data-testid="wrapped-component" data-test-prop={testProp}>
    Mock Component
  </div>
);

describe('HOC Components', () => {
  let mockUseAppSelector: jest.Mock;
  let mockUseAppDispatch: jest.Mock;
  let mockUseGetTokenDetailsMutation: jest.Mock;
  let mockUseGetTenants: jest.Mock;
  let mockUseLogout: jest.Mock;
  let mockUseRouter: jest.Mock;
  let mockDispatch: jest.Mock;
  let mockLogout: jest.Mock;
  let mockRouter: { push: jest.Mock };
  let mockFetchTokenDetails: jest.Mock;
  let mockGetTenants: jest.Mock;

  beforeEach(() => {
    jest.clearAllMocks();

    // Setup mocks
    mockDispatch = jest.fn();
    mockLogout = jest.fn();
    mockRouter = { push: jest.fn() };
    mockFetchTokenDetails = jest.fn();
    mockGetTenants = jest.fn();

    mockUseAppDispatch = require('@/lib/store/hooks').useAppDispatch as jest.Mock;
    mockUseAppDispatch.mockReturnValue(mockDispatch);

    mockUseLogout = require('@/hooks/authHooks').useLogout as jest.Mock;
    mockUseLogout.mockReturnValue(mockLogout);

    mockUseRouter = require('next/navigation').useRouter as jest.Mock;
    mockUseRouter.mockReturnValue(mockRouter);

    mockUseGetTokenDetailsMutation = require('@/lib/store/slice/slice')
      .useGetTokenDetailsMutation as jest.Mock;
    mockUseGetTokenDetailsMutation.mockReturnValue([mockFetchTokenDetails, { isLoading: false }]);

    mockUseGetTenants = require('@/lib/sdk/litegraph.service').useGetTenants as jest.Mock;
    mockUseGetTenants.mockReturnValue({
      getTenants: mockGetTenants,
      isLoading: false,
    });

    mockUseAppSelector = require('@/lib/store/hooks').useAppSelector as jest.Mock;
  });

  describe('withAuth HOC', () => {
    it('renders loading state initially', () => {
      mockUseAppSelector.mockReturnValue({ token: { Token: 'test-token' } });
      mockFetchTokenDetails.mockResolvedValue({ data: null });

      const WrappedComponent = withAuth(MockComponent);
      const { getByTestId } = render(<WrappedComponent testProp="test" />);

      expect(getByTestId('page-loading')).toBeInTheDocument();
    });

    it('calls logout when no token is present', () => {
      mockUseAppSelector.mockReturnValue({ token: null });

      const WrappedComponent = withAuth(MockComponent);
      render(<WrappedComponent />);

      expect(mockLogout).toHaveBeenCalled();
    });
  });

  describe('withAdminAuth HOC', () => {
    it('renders loading state initially', () => {
      mockUseAppSelector.mockReturnValue({ adminAccessKey: 'admin-key' });
      mockGetTenants.mockResolvedValue([]);

      const WrappedComponent = withAdminAuth(MockComponent);
      const { getByTestId } = render(<WrappedComponent />);

      expect(getByTestId('page-loading')).toBeInTheDocument();
    });

    it('renders wrapped component when admin authentication is valid', async () => {
      const mockTenants = [{ GUID: 'tenant-1' }, { GUID: 'tenant-2' }];
      mockUseAppSelector.mockReturnValue({ adminAccessKey: 'admin-key' });
      mockGetTenants.mockResolvedValue(mockTenants);

      const WrappedComponent = withAdminAuth(MockComponent);
      const { getByTestId } = render(<WrappedComponent testProp="admin-test" />);

      await waitFor(() => {
        expect(getByTestId('wrapped-component')).toBeInTheDocument();
      });

      expect(getByTestId('wrapped-component')).toHaveAttribute('data-test-prop', 'admin-test');
    });

    it('renders logout fallback with message when no tenants found', async () => {
      mockUseAppSelector.mockReturnValue({ adminAccessKey: 'admin-key' });
      mockGetTenants.mockResolvedValue([]);

      const WrappedComponent = withAdminAuth(MockComponent);
      const { getByTestId } = render(<WrappedComponent />);

      await waitFor(() => {
        expect(getByTestId('logout-fallback')).toBeInTheDocument();
      });

      expect(getByTestId('logout-fallback')).toHaveAttribute('data-message', 'No Tenant Found.');
      expect(getByTestId('logout-fallback')).toHaveAttribute('data-logout-path', '/admin/login');
    });

    it('calls logout when getTenants returns null', async () => {
      mockUseAppSelector.mockReturnValue({ adminAccessKey: 'admin-key' });
      mockGetTenants.mockResolvedValue(null);

      const WrappedComponent = withAdminAuth(MockComponent);
      render(<WrappedComponent />);

      await waitFor(() => {
        expect(mockLogout).toHaveBeenCalled();
      });
    });

    it('calls logout when getTenants throws error', async () => {
      mockUseAppSelector.mockReturnValue({ adminAccessKey: 'admin-key' });
      mockGetTenants.mockRejectedValue(new Error('Network error'));

      const WrappedComponent = withAdminAuth(MockComponent);
      render(<WrappedComponent />);

      await waitFor(() => {
        expect(mockLogout).toHaveBeenCalled();
      });
    });

    it('passes props to wrapped component', async () => {
      const mockTenants = [{ GUID: 'tenant-1' }];
      mockUseAppSelector.mockReturnValue({ adminAccessKey: 'admin-key' });
      mockGetTenants.mockResolvedValue(mockTenants);

      const WrappedComponent = withAdminAuth(MockComponent);
      const { getByTestId } = render(<WrappedComponent testProp="admin-custom-prop" />);

      await waitFor(() => {
        expect(getByTestId('wrapped-component')).toHaveAttribute(
          'data-test-prop',
          'admin-custom-prop'
        );
      });
    });
  });

  describe('forGuest HOC', () => {
    it('renders null when user is authenticated', () => {
      mockUseAppSelector.mockReturnValue({
        token: { Token: 'user-token' },
        adminAccessKey: null,
        tenant: { GUID: 'tenant-guid' },
      });

      const WrappedComponent = forGuest(MockComponent);
      const { container } = render(<WrappedComponent />);

      expect(container.firstChild).toBeNull();
    });

    it('renders null when admin is authenticated', () => {
      mockUseAppSelector.mockReturnValue({
        token: null,
        adminAccessKey: 'admin-key',
        tenant: null,
      });

      const WrappedComponent = forGuest(MockComponent);
      const { container } = render(<WrappedComponent />);

      expect(container.firstChild).toBeNull();
    });

    it('handles multiple authentication scenarios', () => {
      // Test with user token but no tenant
      mockUseAppSelector.mockReturnValue({
        token: { Token: 'user-token' },
        adminAccessKey: null,
        tenant: null,
      });

      const WrappedComponent = forGuest(MockComponent);
      const { container } = render(<WrappedComponent />);

      expect(container.firstChild).toBeNull();
    });
  });

  describe('HOC Integration', () => {
    it('exports all three HOCs', () => {
      expect(withAuth).toBeDefined();
      expect(withAdminAuth).toBeDefined();
      expect(forGuest).toBeDefined();
    });

    it('returns functions that can wrap components', () => {
      const WrappedWithAuth = withAuth(MockComponent);
      const WrappedWithAdminAuth = withAdminAuth(MockComponent);
      const WrappedForGuest = forGuest(MockComponent);

      expect(typeof WrappedWithAuth).toBe('function');
      expect(typeof WrappedWithAdminAuth).toBe('function');
      expect(typeof WrappedForGuest).toBe('function');
    });

    it('maintains component identity', () => {
      const WrappedWithAuth = withAuth(MockComponent);
      const WrappedWithAdminAuth = withAdminAuth(MockComponent);
      const WrappedForGuest = forGuest(MockComponent);

      expect(WrappedWithAuth.name).toBe('WithAuth');
      expect(WrappedWithAdminAuth.name).toBe('WithAuth');
      expect(WrappedForGuest.name).toBe('ForGuest');
    });
  });
});
