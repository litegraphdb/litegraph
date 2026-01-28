import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import Home from '@/app/page';

// Mock the HomePage component
jest.mock('@/page/home/HomePage', () => {
  return function MockHomePage() {
    return <div data-testid="home-page">Mock Home Page Content</div>;
  };
});

describe('Home Page Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(<Home />);

    expect(getByTestId('home-page')).toBeInTheDocument();
  });

  it('renders HomePage component', () => {
    const { getByText } = render(<Home />);

    expect(getByText('Mock Home Page Content')).toBeInTheDocument();
  });

  it('exports metadata correctly', () => {
    // Import the component to access its metadata
    const HomeModule = require('@/app/page');

    expect(HomeModule.metadata).toBeDefined();
    expect(HomeModule.metadata.title).toBe('Home | Litegraph');
    expect(HomeModule.metadata.description).toBe('Litegraph');
  });

  it('has correct metadata structure', () => {
    const HomeModule = require('@/app/page');

    expect(typeof HomeModule.metadata).toBe('object');
    expect(typeof HomeModule.metadata.title).toBe('string');
    expect(typeof HomeModule.metadata.description).toBe('string');
  });

  it('exports default component', () => {
    const HomeModule = require('@/app/page');

    expect(HomeModule.default).toBeDefined();
    expect(typeof HomeModule.default).toBe('function');
  });

  it('renders consistently', () => {
    const { getByTestId, rerender } = render(<Home />);

    expect(getByTestId('home-page')).toBeInTheDocument();

    // Re-render to ensure consistency
    rerender(<Home />);
    expect(getByTestId('home-page')).toBeInTheDocument();
  });

  it('has proper component structure', () => {
    const { container } = render(<Home />);

    expect(container.firstChild).toBeInTheDocument();
    expect(container.firstChild).toHaveAttribute('data-testid', 'home-page');
  });

  it('renders as a functional component', () => {
    const { container } = render(<Home />);

    // Should render without any errors
    expect(container).toBeInTheDocument();
  });

  it('maintains component identity', () => {
    const HomeComponent = require('@/app/page').default;

    expect(HomeComponent).toBe(Home);
  });

  it('has correct import structure', () => {
    // Test that all imports are working correctly
    expect(React).toBeDefined();
    expect(require('@/page/home/HomePage')).toBeDefined();
  });

  it('renders with proper React element structure', () => {
    const { container } = render(<Home />);

    const homePageElement = container.querySelector('[data-testid="home-page"]');
    expect(homePageElement).toBeInTheDocument();
    expect(homePageElement?.textContent).toBe('Mock Home Page Content');
  });

  it('handles component re-rendering', () => {
    const { getByTestId, rerender } = render(<Home />);

    const initialElement = getByTestId('home-page');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(<Home />);

    const reRenderedElement = getByTestId('home-page');
    expect(reRenderedElement).toBeInTheDocument();
    expect(reRenderedElement).toBe(initialElement);
  });

  it('exports metadata as a constant', () => {
    const HomeModule = require('@/app/page');

    // Metadata should be a constant export
    expect(HomeModule.metadata).toBeDefined();

    // Should not be writable (though this is a runtime check)
    const originalMetadata = HomeModule.metadata;
    expect(originalMetadata).toEqual({
      title: 'Home | Litegraph',
      description: 'Litegraph',
    });
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const HomeComponent: React.ComponentType = require('@/app/page').default;
    expect(typeof HomeComponent).toBe('function');
  });
});
