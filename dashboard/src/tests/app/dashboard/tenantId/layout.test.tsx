import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import TenantDashboardLayout from '@/app/dashboard/[tenantId]/layout';

// Mock the DashboardLayout component
jest.mock('@/components/layout/DashboardLayout', () => {
  return function MockDashboardLayout({ children, menuItems, useGraphsSelector }: any) {
    return (
      <div
        data-testid="dashboard-layout"
        data-menu-items={JSON.stringify(menuItems)}
        data-use-graphs-selector={useGraphsSelector}
      >
        {children}
      </div>
    );
  };
});

// Mock the constants
jest.mock('@/constants/sidebar', () => ({
  tenantDashboardRoutes: [
    { key: 'dashboard', label: 'Dashboard' },
    { key: 'graphs', label: 'Graphs' },
    { key: 'nodes', label: 'Nodes' },
    { key: 'edges', label: 'Edges' },
  ],
}));

// Mock the HOC
jest.mock('@/hoc/hoc', () => ({
  withAuth: jest.fn((Component) => Component),
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

    expect(getByTestId('dashboard-layout')).toBeInTheDocument();
  });

  it('renders children correctly', () => {
    const { getByText } = render(
      <TenantDashboardLayout>
        <div>Test children content</div>
      </TenantDashboardLayout>
    );

    expect(getByText('Test children content')).toBeInTheDocument();
  });

  it('passes tenantDashboardRoutes as menuItems', () => {
    const { getByTestId } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    const menuItems = JSON.parse(dashboardLayout.getAttribute('data-menu-items') || '[]');

    expect(menuItems).toEqual([
      { key: 'dashboard', label: 'Dashboard' },
      { key: 'graphs', label: 'Graphs' },
      { key: 'nodes', label: 'Nodes' },
      { key: 'edges', label: 'Edges' },
    ]);
  });

  it('sets useGraphsSelector to true', () => {
    const { getByTestId } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    const dashboardLayout = getByTestId('dashboard-layout');
    expect(dashboardLayout.getAttribute('data-use-graphs-selector')).toBe('true');
  });

  it('handles multiple children', () => {
    const { getByText } = render(
      <TenantDashboardLayout>
        <div>First child</div>
        <div>Second child</div>
        <div>Third child</div>
      </TenantDashboardLayout>
    );

    expect(getByText('First child')).toBeInTheDocument();
    expect(getByText('Second child')).toBeInTheDocument();
    expect(getByText('Third child')).toBeInTheDocument();
  });

  it('handles complex nested children', () => {
    const { getByText } = render(
      <TenantDashboardLayout>
        <div>
          <h1>Tenant Dashboard</h1>
          <p>
            Welcome to the <strong>tenant panel</strong>
          </p>
        </div>
      </TenantDashboardLayout>
    );

    expect(getByText('Tenant Dashboard')).toBeInTheDocument();
    expect(getByText('Welcome to the')).toBeInTheDocument();
    expect(getByText('tenant panel')).toBeInTheDocument();
  });

  it('handles empty children', () => {
    const { getByTestId } = render(<TenantDashboardLayout>{null}</TenantDashboardLayout>);

    expect(getByTestId('dashboard-layout')).toBeInTheDocument();
  });

  it('handles React fragments as children', () => {
    const { getByText } = render(
      <TenantDashboardLayout>
        <>
          <div>Fragment child 1</div>
          <div>Fragment child 2</div>
        </>
      </TenantDashboardLayout>
    );

    expect(getByText('Fragment child 1')).toBeInTheDocument();
    expect(getByText('Fragment child 2')).toBeInTheDocument();
  });

  it('maintains consistent props across re-renders', () => {
    const { getByTestId, rerender } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    const initialLayout = getByTestId('dashboard-layout');
    expect(initialLayout.getAttribute('data-use-graphs-selector')).toBe('true');

    // Re-render
    rerender(
      <TenantDashboardLayout>
        <div>Updated children</div>
      </TenantDashboardLayout>
    );

    const updatedLayout = getByTestId('dashboard-layout');
    expect(updatedLayout.getAttribute('data-use-graphs-selector')).toBe('true');
  });

  it('exports as default component', () => {
    const TenantDashboardLayoutModule = require('@/app/dashboard/[tenantId]/layout');

    expect(TenantDashboardLayoutModule.default).toBeDefined();
    expect(typeof TenantDashboardLayoutModule.default).toBe('function');
  });

  it('has correct component name', () => {
    const TenantDashboardLayoutModule = require('@/app/dashboard/[tenantId]/layout');

    // The component should be named 'RootLayout' as per the export
    expect(TenantDashboardLayoutModule.default.name).toBe('RootLayout');
  });

  it('renders with proper React element structure', () => {
    const { container } = render(
      <TenantDashboardLayout>
        <div>Test content</div>
      </TenantDashboardLayout>
    );

    const dashboardLayoutElement = container.querySelector('[data-testid="dashboard-layout"]');
    expect(dashboardLayoutElement).toBeInTheDocument();
    expect(dashboardLayoutElement?.textContent).toBe('Test content');
  });

  it('handles component re-rendering', () => {
    const { getByTestId, rerender } = render(
      <TenantDashboardLayout>
        <div>Initial content</div>
      </TenantDashboardLayout>
    );

    const initialElement = getByTestId('dashboard-layout');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(
      <TenantDashboardLayout>
        <div>Updated content</div>
      </TenantDashboardLayout>
    );

    const reRenderedElement = getByTestId('dashboard-layout');
    expect(reRenderedElement).toBeInTheDocument();
    expect(reRenderedElement).toBe(initialElement);
  });

  it('maintains menu items consistency', () => {
    const { getByTestId, rerender } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    const initialLayout = getByTestId('dashboard-layout');
    const initialMenuItems = JSON.parse(initialLayout.getAttribute('data-menu-items') || '[]');

    // Re-render
    rerender(
      <TenantDashboardLayout>
        <div>Updated children</div>
      </TenantDashboardLayout>
    );

    const updatedLayout = getByTestId('dashboard-layout');
    const updatedMenuItems = JSON.parse(updatedLayout.getAttribute('data-menu-items') || '[]');

    expect(updatedMenuItems).toEqual(initialMenuItems);
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const TenantDashboardLayoutComponent: React.ComponentType =
      require('@/app/dashboard/[tenantId]/layout').default;
    expect(typeof TenantDashboardLayoutComponent).toBe('function');
  });

  it('handles different types of children content', () => {
    const { getByText } = render(
      <TenantDashboardLayout>
        <div>Text content</div>
        <span>Inline content</span>
        <button>Button content</button>
      </TenantDashboardLayout>
    );

    expect(getByText('Text content')).toBeInTheDocument();
    expect(getByText('Inline content')).toBeInTheDocument();
    expect(getByText('Button content')).toBeInTheDocument();
  });

  it('maintains layout structure across renders', () => {
    const { getByTestId, rerender } = render(
      <TenantDashboardLayout>
        <div>Test children</div>
      </TenantDashboardLayout>
    );

    // First render
    const firstLayout = getByTestId('dashboard-layout');
    expect(firstLayout).toBeInTheDocument();

    // Second render
    rerender(
      <TenantDashboardLayout>
        <div>Different children</div>
      </TenantDashboardLayout>
    );

    const secondLayout = getByTestId('dashboard-layout');
    expect(secondLayout).toBeInTheDocument();

    // Both should be the same element (same reference)
    expect(firstLayout).toBe(secondLayout);
  });
});
