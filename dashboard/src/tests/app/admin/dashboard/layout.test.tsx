import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import AdminDashboardLayout from '@/app/admin/dashboard/layout';

// Mock the DashboardLayout component
jest.mock('@/components/layout/DashboardLayout', () => {
  return function MockDashboardLayout({
    children,
    menuItems,
    noProfile,
    useTenantSelector,
    isAdmin,
  }: any) {
    return (
      <div
        data-testid="dashboard-layout"
        data-menu-items={JSON.stringify(menuItems)}
        data-no-profile={noProfile}
        data-use-tenant-selector={useTenantSelector}
        data-is-admin={isAdmin}
      >
        {children}
      </div>
    );
  };
});

// Mock the constants
jest.mock('@/constants/constant', () => ({
  paths: {
    backups: '/admin/backups',
    adminDashboard: '/admin/dashboard',
  },
}));

jest.mock('@/constants/sidebar', () => ({
  adminDashboardRoutes: [
    { key: 'dashboard', label: 'Dashboard' },
    { key: 'tenants', label: 'Tenants' },
  ],
}));

// Mock the HOC
jest.mock('@/hoc/hoc', () => ({
  withAdminAuth: jest.fn((Component) => Component),
}));

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}));

describe('Admin Dashboard Layout Component', () => {
  const mockUsePathname = require('next/navigation').usePathname;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    expect(getByTestId('dashboard-layout')).toBeInTheDocument();
  });

  it('renders children correctly', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByText } = render(
      <AdminDashboardLayout>
        <div>Test children content</div>
      </AdminDashboardLayout>
    );

    expect(getByText('Test children content')).toBeInTheDocument();
  });

  it('passes adminDashboardRoutes as menuItems', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    const menuItems = JSON.parse(dashboardLayout.getAttribute('data-menu-items') || '[]');

    expect(menuItems).toEqual([
      { key: 'dashboard', label: 'Dashboard' },
      { key: 'tenants', label: 'Tenants' },
    ]);
  });

  it('sets noProfile to true', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-no-profile')).toBe('true');
  });

  it('sets isAdmin to true', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-is-admin')).toBe('true');
  });

  it('disables tenant selector when pathname is backups', () => {
    mockUsePathname.mockReturnValue('/admin/backups');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('false');
  });

  it('disables tenant selector when pathname is adminDashboard', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('false');
  });

  it('enables tenant selector for other pathnames', () => {
    mockUsePathname.mockReturnValue('/admin/tenants');

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('true');
  });

  it('handles multiple children', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByText } = render(
      <AdminDashboardLayout>
        <div>First child</div>
        <div>Second child</div>
        <div>Third child</div>
      </AdminDashboardLayout>
    );

    expect(getByText('First child')).toBeInTheDocument();
    expect(getByText('Second child')).toBeInTheDocument();
    expect(getByText('Third child')).toBeInTheDocument();
  });

  it('handles complex nested children', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByText } = render(
      <AdminDashboardLayout>
        <div>
          <h1>Admin Dashboard</h1>
          <p>
            Welcome to the <strong>admin panel</strong>
          </p>
        </div>
      </AdminDashboardLayout>
    );

    expect(getByText('Admin Dashboard')).toBeInTheDocument();
    expect(getByText('Welcome to the')).toBeInTheDocument();
    expect(getByText('admin panel')).toBeInTheDocument();
  });

  it('handles empty children', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId } = render(<AdminDashboardLayout>{null}</AdminDashboardLayout>);

    expect(getByTestId('dashboard-layout')).toBeInTheDocument();
  });

  it('handles React fragments as children', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByText } = render(
      <AdminDashboardLayout>
        <>
          <div>Fragment child 1</div>
          <div>Fragment child 2</div>
        </>
      </AdminDashboardLayout>
    );

    expect(getByText('Fragment child 1')).toBeInTheDocument();
    expect(getByText('Fragment child 2')).toBeInTheDocument();
  });

  it('updates tenant selector based on pathname changes', () => {
    const { rerender, getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    // Initial render with backups pathname
    mockUsePathname.mockReturnValue('/admin/backups');
    rerender(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    let dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('false');

    // Change to tenants pathname
    mockUsePathname.mockReturnValue('/admin/tenants');
    rerender(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('true');
  });

  it('maintains consistent props across re-renders', () => {
    mockUsePathname.mockReturnValue('/admin/dashboard');

    const { getByTestId, rerender } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const initialLayout = getByTestId('dashboard-layout');
    expect(initialLayout.getAttribute('data-no-profile')).toBe('true');
    expect(initialLayout.getAttribute('data-is-admin')).toBe('true');

    // Re-render
    rerender(
      <AdminDashboardLayout>
        <div>Updated children</div>
      </AdminDashboardLayout>
    );

    const updatedLayout = getByTestId('dashboard-layout');
    expect(updatedLayout.getAttribute('data-no-profile')).toBe('true');
    expect(updatedLayout.getAttribute('data-is-admin')).toBe('true');
  });

  it('handles undefined pathname', () => {
    mockUsePathname.mockReturnValue(undefined);

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('true');
  });

  it('handles null pathname', () => {
    mockUsePathname.mockReturnValue(null);

    const { getByTestId } = render(
      <AdminDashboardLayout>
        <div>Test children</div>
      </AdminDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-tenant-selector')).toBe('true');
  });

  it('exports as default component', () => {
    const AdminDashboardLayoutModule = require('@/app/admin/dashboard/layout');

    expect(AdminDashboardLayoutModule.default).toBeDefined();
    expect(typeof AdminDashboardLayoutModule.default).toBe('function');
  });
});
