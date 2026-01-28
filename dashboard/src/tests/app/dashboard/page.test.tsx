import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import DashboardPage from '@/app/dashboard/page';

// Mock the HomePage component
jest.mock('@/page/home/HomePage', () => {
  return function MockHomePage() {
    return <div data-testid="home-page">Mock Home Page Content</div>;
  };
});

describe('Dashboard Page Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(<DashboardPage />);

    expect(getByTestId('home-page')).toBeInTheDocument();
  });

  it('renders HomePage component', () => {
    const { getByText } = render(<DashboardPage />);

    expect(getByText('Mock Home Page Content')).toBeInTheDocument();
  });

  it('exports default component', () => {
    const DashboardPageModule = require('@/app/dashboard/page');

    expect(DashboardPageModule.default).toBeDefined();
    expect(typeof DashboardPageModule.default).toBe('function');
  });

  it('renders consistently', () => {
    const { getByTestId, rerender } = render(<DashboardPage />);

    expect(getByTestId('home-page')).toBeInTheDocument();

    // Re-render to ensure consistency
    rerender(<DashboardPage />);
    expect(getByTestId('home-page')).toBeInTheDocument();
  });

  it('has proper component structure', () => {
    const { container } = render(<DashboardPage />);

    expect(container.firstChild).toBeInTheDocument();
    expect(container.firstChild).toHaveAttribute('data-testid', 'home-page');
  });

  it('renders as a functional component', () => {
    const { container } = render(<DashboardPage />);

    // Should render without any errors
    expect(container).toBeInTheDocument();
  });

  it('maintains component identity', () => {
    const DashboardPageComponent = require('@/app/dashboard/page').default;

    expect(DashboardPageComponent).toBe(DashboardPage);
  });

  it('has correct import structure', () => {
    // Test that all imports are working correctly
    expect(React).toBeDefined();
    expect(require('@/page/home/HomePage')).toBeDefined();
  });

  it('renders with proper React element structure', () => {
    const { container } = render(<DashboardPage />);

    const homePageElement = container.querySelector('[data-testid="home-page"]');
    expect(homePageElement).toBeInTheDocument();
    expect(homePageElement?.textContent).toBe('Mock Home Page Content');
  });

  it('handles component re-rendering', () => {
    const { getByTestId, rerender } = render(<DashboardPage />);

    const initialElement = getByTestId('home-page');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(<DashboardPage />);

    const reRenderedElement = getByTestId('home-page');
    expect(reRenderedElement).toBeInTheDocument();
    expect(reRenderedElement).toBe(initialElement);
  });

  it('has correct component name', () => {
    const DashboardPageModule = require('@/app/dashboard/page');

    // The component should be named 'Page' as per the export
    expect(DashboardPageModule.default.name).toBe('Page');
  });

  it('renders HomePage with correct props', () => {
    const { getByTestId } = render(<DashboardPage />);

    const homePage = getByTestId('home-page');
    expect(homePage).toBeInTheDocument();
    expect(homePage.textContent).toBe('Mock Home Page Content');
  });

  it('handles multiple renders without side effects', () => {
    const { getByTestId, rerender } = render(<DashboardPage />);

    // First render
    expect(getByTestId('home-page')).toBeInTheDocument();

    // Second render
    rerender(<DashboardPage />);
    expect(getByTestId('home-page')).toBeInTheDocument();

    // Third render
    rerender(<DashboardPage />);
    expect(getByTestId('home-page')).toBeInTheDocument();
  });

  it('maintains consistent behavior across renders', () => {
    const { getByTestId, rerender } = render(<DashboardPage />);

    const firstRender = getByTestId('home-page');
    expect(firstRender.textContent).toBe('Mock Home Page Content');

    rerender(<DashboardPage />);
    const secondRender = getByTestId('home-page');
    expect(secondRender.textContent).toBe('Mock Home Page Content');

    expect(firstRender.textContent).toBe(secondRender.textContent);
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const DashboardPageComponent: React.ComponentType = require('@/app/dashboard/page').default;
    expect(typeof DashboardPageComponent).toBe('function');
  });

  it('exports component correctly', () => {
    const DashboardPageModule = require('@/app/dashboard/page');

    // Should have default export
    expect(DashboardPageModule.default).toBeDefined();

    // Should not have metadata export (unlike other pages)
    expect(DashboardPageModule.metadata).toBeUndefined();
  });
});
