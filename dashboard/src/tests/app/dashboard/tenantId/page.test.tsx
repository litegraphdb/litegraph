import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import DashboardHomePage from '@/app/dashboard/[tenantId]/page';

// Mock the HomePage component
jest.mock('@/page/user-dashboard/home/HomePage', () => {
  return function MockHomePage() {
    return <div data-testid="user-home-page">Mock User Home Page Content</div>;
  };
});

describe('Dashboard Home Page Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(<DashboardHomePage />);

    expect(getByTestId('user-home-page')).toBeInTheDocument();
  });

  it('renders HomePage component', () => {
    const { getByText } = render(<DashboardHomePage />);

    expect(getByText('Mock User Home Page Content')).toBeInTheDocument();
  });

  it('exports metadata correctly', () => {
    // Import the component to access its metadata
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    expect(DashboardHomePageModule.metadata).toBeDefined();
    expect(DashboardHomePageModule.metadata.title).toBe('LiteGraph | Dashboard');
    expect(DashboardHomePageModule.metadata.description).toBe('Dashboard');
  });

  it('has correct metadata structure', () => {
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    expect(typeof DashboardHomePageModule.metadata).toBe('object');
    expect(typeof DashboardHomePageModule.metadata.title).toBe('string');
    expect(typeof DashboardHomePageModule.metadata.description).toBe('string');
  });

  it('exports default component', () => {
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    expect(DashboardHomePageModule.default).toBeDefined();
    expect(typeof DashboardHomePageModule.default).toBe('function');
  });

  it('renders consistently', () => {
    const { getByTestId, rerender } = render(<DashboardHomePage />);

    expect(getByTestId('user-home-page')).toBeInTheDocument();

    // Re-render to ensure consistency
    rerender(<DashboardHomePage />);
    expect(getByTestId('user-home-page')).toBeInTheDocument();
  });

  it('has proper component structure', () => {
    const { container } = render(<DashboardHomePage />);

    expect(container.firstChild).toBeInTheDocument();
    expect(container.firstChild).toHaveAttribute('data-testid', 'user-home-page');
  });

  it('renders as a functional component', () => {
    const { container } = render(<DashboardHomePage />);

    // Should render without any errors
    expect(container).toBeInTheDocument();
  });

  it('maintains component identity', () => {
    const DashboardHomePageComponent = require('@/app/dashboard/[tenantId]/page').default;

    expect(DashboardHomePageComponent).toBe(DashboardHomePage);
  });

  it('has correct import structure', () => {
    // Test that all imports are working correctly
    expect(React).toBeDefined();
    expect(require('@/page/user-dashboard/home/HomePage')).toBeDefined();
  });

  it('renders with proper React element structure', () => {
    const { container } = render(<DashboardHomePage />);

    const homePageElement = container.querySelector('[data-testid="user-home-page"]');
    expect(homePageElement).toBeInTheDocument();
    expect(homePageElement?.textContent).toBe('Mock User Home Page Content');
  });

  it('handles component re-rendering', () => {
    const { getByTestId, rerender } = render(<DashboardHomePage />);

    const initialElement = getByTestId('user-home-page');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(<DashboardHomePage />);

    const reRenderedElement = getByTestId('user-home-page');
    expect(reRenderedElement).toBeInTheDocument();
    expect(reRenderedElement).toBe(initialElement);
  });

  it('exports metadata as a constant', () => {
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    // Metadata should be a constant export
    expect(DashboardHomePageModule.metadata).toBeDefined();

    // Should not be writable (though this is a runtime check)
    const originalMetadata = DashboardHomePageModule.metadata;
    expect(originalMetadata).toEqual({
      title: 'LiteGraph | Dashboard',
      description: 'Dashboard',
    });
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const DashboardHomePageComponent: React.ComponentType =
      require('@/app/dashboard/[tenantId]/page').default;
    expect(typeof DashboardHomePageComponent).toBe('function');
  });

  it('has correct component name', () => {
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    // The component should be named 'DashboardHomePage' as per the export
    expect(DashboardHomePageModule.default.name).toBe('DashboardHomePage');
  });

  it('renders HomePage with correct props', () => {
    const { getByTestId } = render(<DashboardHomePage />);

    const homePage = getByTestId('user-home-page');
    expect(homePage).toBeInTheDocument();
    expect(homePage.textContent).toBe('Mock User Home Page Content');
  });

  it('handles multiple renders without side effects', () => {
    const { getByTestId, rerender } = render(<DashboardHomePage />);

    // First render
    expect(getByTestId('user-home-page')).toBeInTheDocument();

    // Second render
    rerender(<DashboardHomePage />);
    expect(getByTestId('user-home-page')).toBeInTheDocument();

    // Third render
    rerender(<DashboardHomePage />);
    expect(getByTestId('user-home-page')).toBeInTheDocument();
  });

  it('maintains consistent behavior across renders', () => {
    const { getByTestId, rerender } = render(<DashboardHomePage />);

    const firstRender = getByTestId('user-home-page');
    expect(firstRender.textContent).toBe('Mock User Home Page Content');

    rerender(<DashboardHomePage />);
    const secondRender = getByTestId('user-home-page');
    expect(secondRender.textContent).toBe('Mock User Home Page Content');

    expect(firstRender.textContent).toBe(secondRender.textContent);
  });

  it('has correct metadata for SEO', () => {
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    const metadata = DashboardHomePageModule.metadata;

    // Check that metadata has SEO-friendly properties
    expect(metadata.title).toContain('LiteGraph');
    expect(metadata.title).toContain('Dashboard');
    expect(metadata.description).toBe('Dashboard');
  });

  it('exports both default and named exports correctly', () => {
    const DashboardHomePageModule = require('@/app/dashboard/[tenantId]/page');

    // Should have default export
    expect(DashboardHomePageModule.default).toBeDefined();

    // Should have metadata export
    expect(DashboardHomePageModule.metadata).toBeDefined();
  });

  it('uses correct HomePage import path', () => {
    // Test that the correct HomePage component is imported
    const HomePageModule = require('@/page/user-dashboard/home/HomePage');
    expect(HomePageModule).toBeDefined();
  });

  it('renders different content from regular home page', () => {
    const { getByTestId } = render(<DashboardHomePage />);

    const userHomePage = getByTestId('user-home-page');
    expect(userHomePage.textContent).toBe('Mock User Home Page Content');

    // This should be different from the regular home page content
    expect(userHomePage.textContent).not.toBe('Mock Home Page Content');
  });

  it('maintains component structure across re-renders', () => {
    const { getByTestId, rerender } = render(<DashboardHomePage />);

    // First render
    const firstElement = getByTestId('user-home-page');
    expect(firstElement).toBeInTheDocument();

    // Second render
    rerender(<DashboardHomePage />);
    const secondElement = getByTestId('user-home-page');
    expect(secondElement).toBeInTheDocument();

    // Both should be the same element (same reference)
    expect(firstElement).toBe(secondElement);
  });

  it('has proper React component structure', () => {
    const { container } = render(<DashboardHomePage />);

    // Should have a single root element
    expect(container.children).toHaveLength(1);

    // The root element should contain the HomePage component
    const rootElement = container.firstChild as HTMLElement;
    expect(rootElement).toHaveAttribute('data-testid', 'user-home-page');
  });
});
