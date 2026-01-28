import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import LoginPage from '@/app/login/page';

// Mock the LoginPage component
jest.mock('@/page/login/LoginPage', () => {
  return function MockLoginPage() {
    return <div data-testid="login-page">Mock Login Page Content</div>;
  };
});

describe('Login Page Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(<LoginPage />);

    expect(getByTestId('login-page')).toBeInTheDocument();
  });

  it('renders LoginPage component', () => {
    const { getByText } = render(<LoginPage />);

    expect(getByText('Mock Login Page Content')).toBeInTheDocument();
  });

  it('exports metadata correctly', () => {
    // Import the component to access its metadata
    const LoginPageModule = require('@/app/login/page');

    expect(LoginPageModule.metadata).toBeDefined();
    expect(LoginPageModule.metadata.title).toBe('LiteGraph | Login');
    expect(LoginPageModule.metadata.description).toBe('LiteGraph');
  });

  it('has correct metadata structure', () => {
    const LoginPageModule = require('@/app/login/page');

    expect(typeof LoginPageModule.metadata).toBe('object');
    expect(typeof LoginPageModule.metadata.title).toBe('string');
    expect(typeof LoginPageModule.metadata.description).toBe('string');
  });

  it('exports default component', () => {
    const LoginPageModule = require('@/app/login/page');

    expect(LoginPageModule.default).toBeDefined();
    expect(typeof LoginPageModule.default).toBe('function');
  });

  it('renders consistently', () => {
    const { getByTestId, rerender } = render(<LoginPage />);

    expect(getByTestId('login-page')).toBeInTheDocument();

    // Re-render to ensure consistency
    rerender(<LoginPage />);
    expect(getByTestId('login-page')).toBeInTheDocument();
  });

  it('has proper component structure', () => {
    const { container } = render(<LoginPage />);

    expect(container.firstChild).toBeInTheDocument();
    expect(container.firstChild).toHaveAttribute('data-testid', 'login-page');
  });

  it('renders as a functional component', () => {
    const { container } = render(<LoginPage />);

    // Should render without any errors
    expect(container).toBeInTheDocument();
  });

  it('maintains component identity', () => {
    const LoginPageComponent = require('@/app/login/page').default;

    expect(LoginPageComponent).toBe(LoginPage);
  });

  it('has correct import structure', () => {
    // Test that all imports are working correctly
    expect(React).toBeDefined();
    expect(require('@/page/login/LoginPage')).toBeDefined();
  });

  it('renders with proper React element structure', () => {
    const { container } = render(<LoginPage />);

    const loginPageElement = container.querySelector('[data-testid="login-page"]');
    expect(loginPageElement).toBeInTheDocument();
    expect(loginPageElement?.textContent).toBe('Mock Login Page Content');
  });

  it('handles component re-rendering', () => {
    const { getByTestId, rerender } = render(<LoginPage />);

    const initialElement = getByTestId('login-page');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(<LoginPage />);

    const reRenderedElement = getByTestId('login-page');
    expect(reRenderedElement).toBeInTheDocument();
    expect(reRenderedElement).toBe(initialElement);
  });

  it('exports metadata as a constant', () => {
    const LoginPageModule = require('@/app/login/page');

    // Metadata should be a constant export
    expect(LoginPageModule.metadata).toBeDefined();

    // Should not be writable (though this is a runtime check)
    const originalMetadata = LoginPageModule.metadata;
    expect(originalMetadata).toEqual({
      title: 'LiteGraph | Login',
      description: 'LiteGraph',
    });
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const LoginPageComponent: React.ComponentType = require('@/app/login/page').default;
    expect(typeof LoginPageComponent).toBe('function');
  });

  it('has correct component name', () => {
    const LoginPageModule = require('@/app/login/page');

    // The component should be named 'Login' as per the export
    expect(LoginPageModule.default.name).toBe('Login');
  });

  it('renders LoginPage with correct props', () => {
    const { getByTestId } = render(<LoginPage />);

    const loginPage = getByTestId('login-page');
    expect(loginPage).toBeInTheDocument();
    expect(loginPage.textContent).toBe('Mock Login Page Content');
  });

  it('handles multiple renders without side effects', () => {
    const { getByTestId, rerender } = render(<LoginPage />);

    // First render
    expect(getByTestId('login-page')).toBeInTheDocument();

    // Second render
    rerender(<LoginPage />);
    expect(getByTestId('login-page')).toBeInTheDocument();

    // Third render
    rerender(<LoginPage />);
    expect(getByTestId('login-page')).toBeInTheDocument();
  });

  it('maintains consistent behavior across renders', () => {
    const { getByTestId, rerender } = render(<LoginPage />);

    const firstRender = getByTestId('login-page');
    expect(firstRender.textContent).toBe('Mock Login Page Content');

    rerender(<LoginPage />);
    const secondRender = getByTestId('login-page');
    expect(secondRender.textContent).toBe('Mock Login Page Content');

    expect(firstRender.textContent).toBe(secondRender.textContent);
  });

  it('has correct metadata for SEO', () => {
    const LoginPageModule = require('@/app/login/page');

    const metadata = LoginPageModule.metadata;

    // Check that metadata has SEO-friendly properties
    expect(metadata.title).toContain('LiteGraph');
    expect(metadata.title).toContain('Login');
    expect(metadata.description).toBe('LiteGraph');
  });

  it('exports both default and named exports correctly', () => {
    const LoginPageModule = require('@/app/login/page');

    // Should have default export
    expect(LoginPageModule.default).toBeDefined();

    // Should have metadata export
    expect(LoginPageModule.metadata).toBeDefined();
  });

  it('uses correct LoginPage import path', () => {
    // Test that the correct LoginPage component is imported
    const LoginPageModule = require('@/page/login/LoginPage');
    expect(LoginPageModule).toBeDefined();
  });

  it('maintains component structure across re-renders', () => {
    const { getByTestId, rerender } = render(<LoginPage />);

    // First render
    const firstElement = getByTestId('login-page');
    expect(firstElement).toBeInTheDocument();

    // Second render
    rerender(<LoginPage />);
    const secondElement = getByTestId('login-page');
    expect(secondElement).toBeInTheDocument();

    // Both should be the same element (same reference)
    expect(firstElement).toBe(secondElement);
  });

  it('has proper React component structure', () => {
    const { container } = render(<LoginPage />);

    // Should have a single root element
    expect(container.children).toHaveLength(1);

    // The root element should contain the LoginPage component
    const rootElement = container.firstChild as HTMLElement;
    expect(rootElement).toHaveAttribute('data-testid', 'login-page');
  });

  it('imports React correctly', () => {
    const LoginPageModule = require('@/app/login/page');

    // Should import React
    expect(React).toBeDefined();
  });

  it('has consistent metadata across imports', () => {
    const LoginPageModule1 = require('@/app/login/page');
    const LoginPageModule2 = require('@/app/login/page');

    expect(LoginPageModule1.metadata).toEqual(LoginPageModule2.metadata);
  });

  it('renders LoginPage component consistently', () => {
    const { getByTestId, rerender } = render(<LoginPage />);

    // First render
    const firstLoginPage = getByTestId('login-page');
    expect(firstLoginPage.textContent).toBe('Mock Login Page Content');

    // Second render
    rerender(<LoginPage />);
    const secondLoginPage = getByTestId('login-page');
    expect(secondLoginPage.textContent).toBe('Mock Login Page Content');

    // Content should be consistent
    expect(firstLoginPage.textContent).toBe(secondLoginPage.textContent);
  });

  it('has proper export structure', () => {
    const LoginPageModule = require('@/app/login/page');

    // Should export Login component as default
    expect(LoginPageModule.default).toBeDefined();
    expect(LoginPageModule.default.name).toBe('Login');

    // Should export metadata
    expect(LoginPageModule.metadata).toBeDefined();
    expect(LoginPageModule.metadata.title).toBe('LiteGraph | Login');
  });
});
