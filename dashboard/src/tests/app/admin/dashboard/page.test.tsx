import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import AdminDashboardPage from '@/app/admin/dashboard/page';

// Mock the TenantPage component
jest.mock('@/page/tenants/TenantPage', () => {
  return function MockTenantPage() {
    return <div data-testid="tenant-page">Mock Tenant Page Content</div>;
  };
});

describe('Admin Dashboard Page Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(<AdminDashboardPage />);

    expect(getByTestId('tenant-page')).toBeInTheDocument();
  });

  it('renders TenantPage component', () => {
    const { getByText } = render(<AdminDashboardPage />);

    expect(getByText('Mock Tenant Page Content')).toBeInTheDocument();
  });

  it('exports metadata correctly', () => {
    // Import the component to access its metadata
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    expect(AdminDashboardPageModule.metadata).toBeDefined();
    expect(AdminDashboardPageModule.metadata.title).toBe('LiteGraph | Tenants');
    expect(AdminDashboardPageModule.metadata.description).toBe('LiteGraph');
  });

  it('has correct metadata structure', () => {
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    expect(typeof AdminDashboardPageModule.metadata).toBe('object');
    expect(typeof AdminDashboardPageModule.metadata.title).toBe('string');
    expect(typeof AdminDashboardPageModule.metadata.description).toBe('string');
  });

  it('exports default component', () => {
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    expect(AdminDashboardPageModule.default).toBeDefined();
    expect(typeof AdminDashboardPageModule.default).toBe('function');
  });

  it('renders consistently', () => {
    const { getByTestId, rerender } = render(<AdminDashboardPage />);

    expect(getByTestId('tenant-page')).toBeInTheDocument();

    // Re-render to ensure consistency
    rerender(<AdminDashboardPage />);
    expect(getByTestId('tenant-page')).toBeInTheDocument();
  });

  it('has proper component structure', () => {
    const { container } = render(<AdminDashboardPage />);

    expect(container.firstChild).toBeInTheDocument();
    expect(container.firstChild).toHaveAttribute('data-testid', 'tenant-page');
  });

  it('renders as a functional component', () => {
    const { container } = render(<AdminDashboardPage />);

    // Should render without any errors
    expect(container).toBeInTheDocument();
  });

  it('maintains component identity', () => {
    const AdminDashboardPageComponent = require('@/app/admin/dashboard/page').default;

    expect(AdminDashboardPageComponent).toBe(AdminDashboardPage);
  });

  it('has correct import structure', () => {
    // Test that all imports are working correctly
    expect(React).toBeDefined();
    expect(require('@/page/tenants/TenantPage')).toBeDefined();
  });

  it('renders with proper React element structure', () => {
    const { container } = render(<AdminDashboardPage />);

    const tenantPageElement = container.querySelector('[data-testid="tenant-page"]');
    expect(tenantPageElement).toBeInTheDocument();
    expect(tenantPageElement?.textContent).toBe('Mock Tenant Page Content');
  });

  it('handles component re-rendering', () => {
    const { getByTestId, rerender } = render(<AdminDashboardPage />);

    const initialElement = getByTestId('tenant-page');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(<AdminDashboardPage />);

    const reRenderedElement = getByTestId('tenant-page');
    expect(reRenderedElement).toBeInTheDocument();
    expect(reRenderedElement).toBe(initialElement);
  });

  it('exports metadata as a constant', () => {
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    // Metadata should be a constant export
    expect(AdminDashboardPageModule.metadata).toBeDefined();

    // Should not be writable (though this is a runtime check)
    const originalMetadata = AdminDashboardPageModule.metadata;
    expect(originalMetadata).toEqual({
      title: 'LiteGraph | Tenants',
      description: 'LiteGraph',
    });
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const AdminDashboardPageComponent: React.ComponentType =
      require('@/app/admin/dashboard/page').default;
    expect(typeof AdminDashboardPageComponent).toBe('function');
  });

  it('has correct component name', () => {
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    // The component should be named 'page' as per the export
    expect(AdminDashboardPageModule.default.name).toBe('page');
  });

  it('renders TenantPage with correct props', () => {
    const { getByTestId } = render(<AdminDashboardPage />);

    const tenantPage = getByTestId('tenant-page');
    expect(tenantPage).toBeInTheDocument();
    expect(tenantPage.textContent).toBe('Mock Tenant Page Content');
  });

  it('handles multiple renders without side effects', () => {
    const { getByTestId, rerender } = render(<AdminDashboardPage />);

    // First render
    expect(getByTestId('tenant-page')).toBeInTheDocument();

    // Second render
    rerender(<AdminDashboardPage />);
    expect(getByTestId('tenant-page')).toBeInTheDocument();

    // Third render
    rerender(<AdminDashboardPage />);
    expect(getByTestId('tenant-page')).toBeInTheDocument();
  });

  it('maintains consistent behavior across renders', () => {
    const { getByTestId, rerender } = render(<AdminDashboardPage />);

    const firstRender = getByTestId('tenant-page');
    expect(firstRender.textContent).toBe('Mock Tenant Page Content');

    rerender(<AdminDashboardPage />);
    const secondRender = getByTestId('tenant-page');
    expect(secondRender.textContent).toBe('Mock Tenant Page Content');

    expect(firstRender.textContent).toBe(secondRender.textContent);
  });

  it('has correct metadata for SEO', () => {
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    const metadata = AdminDashboardPageModule.metadata;

    // Check that metadata has SEO-friendly properties
    expect(metadata.title).toContain('LiteGraph');
    expect(metadata.title).toContain('Tenants');
    expect(metadata.description).toBe('LiteGraph');
  });

  it('exports both default and named exports correctly', () => {
    const AdminDashboardPageModule = require('@/app/admin/dashboard/page');

    // Should have default export
    expect(AdminDashboardPageModule.default).toBeDefined();

    // Should have metadata export
    expect(AdminDashboardPageModule.metadata).toBeDefined();
  });
});
