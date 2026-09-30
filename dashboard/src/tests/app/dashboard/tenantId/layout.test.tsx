import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import TenantDashboardLayout from '@/app/dashboard/[tenantId]/layout';

// Mock the consolidated shell so this test focuses on the layout wiring.
jest.mock('@/components/layout/ConsolidatedDashboardShell', () => {
  return function MockShell({ children, useGraphsSelector }: any) {
    return (
      <div data-testid="dashboard-shell" data-use-graphs-selector={String(useGraphsSelector)}>
        {children}
      </div>
    );
  };
});

// Mock the HOC
jest.mock('@/hoc/hoc', () => ({
  withAuth: jest.fn((Component) => Component),
}));

let mockPathname = '/dashboard/tenant-1';
jest.mock('next/navigation', () => ({
  usePathname: () => mockPathname,
}));

describe('Tenant Dashboard Layout Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    expect(getByTestId('dashboard-shell')).toBeInTheDocument();
  });

  it('renders children correctly', () => {
    const { getByText } = render(
      <TenantDashboardLayout>
        <div>Test children content</div>
      </TenantDashboardLayout>
    );

    expect(getByText('Test children content')).toBeInTheDocument();
  });

  it('shows the header graph selector on the tenant Home page', () => {
    mockPathname = '/dashboard/tenant-1';
    const { getByTestId } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    const shell = getByTestId('dashboard-shell');
    expect(shell.getAttribute('data-use-graphs-selector')).toBe('true');
  });

  it('hides the header graph selector on hub tabs, which carry their own', () => {
    mockPathname = '/dashboard/tenant-1/graphs/nodes';
    const { getByTestId } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    expect(getByTestId('dashboard-shell').getAttribute('data-use-graphs-selector')).toBe('false');
    mockPathname = '/dashboard/tenant-1';
  });

  it('exports as default component', () => {
    const mod = require('@/app/dashboard/[tenantId]/layout');
    expect(mod.default).toBeDefined();
    expect(typeof mod.default).toBe('function');
  });

  it('has correct component name', () => {
    const mod = require('@/app/dashboard/[tenantId]/layout');
    expect(mod.default.name).toBe('RootLayout');
  });
});
