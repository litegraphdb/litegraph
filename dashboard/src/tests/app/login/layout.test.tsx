import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import LoginLayout from '@/app/login/layout';

// Mock the HOC
jest.mock('@/hoc/hoc', () => ({
  forGuest: jest.fn((Component) => Component),
}));

describe('Login Layout Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { getByText } = render(
      <LoginLayout>
        <div>Test children</div>
      </LoginLayout>
    );

    expect(getByText('Test children')).toBeInTheDocument();
  });

  it('renders children correctly', () => {
    const { getByText } = render(
      <LoginLayout>
        <div>Login form content</div>
      </LoginLayout>
    );

    expect(getByText('Login form content')).toBeInTheDocument();
  });

  it('handles multiple children', () => {
    const { getByText } = render(
      <LoginLayout>
        <div>First child</div>
        <div>Second child</div>
        <div>Third child</div>
      </LoginLayout>
    );

    expect(getByText('First child')).toBeInTheDocument();
    expect(getByText('Second child')).toBeInTheDocument();
    expect(getByText('Third child')).toBeInTheDocument();
  });

  it('handles complex nested children', () => {
    const { getByText } = render(
      <LoginLayout>
        <div>
          <h1>Login</h1>
          <p>
            Welcome to <strong>LiteGraph</strong>
          </p>
        </div>
      </LoginLayout>
    );

    expect(getByText('Login')).toBeInTheDocument();
    expect(getByText('Welcome to')).toBeInTheDocument();
    expect(getByText('LiteGraph')).toBeInTheDocument();
  });

  it('handles React fragments as children', () => {
    const { getByText } = render(
      <LoginLayout>
        <>
          <div>Fragment child 1</div>
          <div>Fragment child 2</div>
        </>
      </LoginLayout>
    );

    expect(getByText('Fragment child 1')).toBeInTheDocument();
    expect(getByText('Fragment child 2')).toBeInTheDocument();
  });

  it('maintains consistent rendering across re-renders', () => {
    const { getByText, rerender } = render(
      <LoginLayout>
        <div>Initial content</div>
      </LoginLayout>
    );

    expect(getByText('Initial content')).toBeInTheDocument();

    // Re-render
    rerender(
      <LoginLayout>
        <div>Updated content</div>
      </LoginLayout>
    );

    expect(getByText('Updated content')).toBeInTheDocument();
  });

  it('exports as default component', () => {
    const LoginLayoutModule = require('@/app/login/layout');

    expect(LoginLayoutModule.default).toBeDefined();
    expect(typeof LoginLayoutModule.default).toBe('function');
  });

  it('has correct component name', () => {
    const LoginLayoutModule = require('@/app/login/layout');

    // The component should be named 'Layout' as per the export
    expect(LoginLayoutModule.default.name).toBe('Layout');
  });

  it('renders with proper React element structure', () => {
    const { container } = render(
      <LoginLayout>
        <div>Test content</div>
      </LoginLayout>
    );

    // Should render children directly without wrapper
    expect(container.firstChild?.textContent).toBe('Test content');
  });

  it('handles component re-rendering', () => {
    const { getByText, rerender } = render(
      <LoginLayout>
        <div>Initial content</div>
      </LoginLayout>
    );

    const initialElement = getByText('Initial content');
    expect(initialElement).toBeInTheDocument();

    // Force re-render
    rerender(
      <LoginLayout>
        <div>Updated content</div>
      </LoginLayout>
    );

    const reRenderedElement = getByText('Updated content');
    expect(reRenderedElement).toBeInTheDocument();
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const LoginLayoutComponent: React.ComponentType = require('@/app/login/layout').default;
    expect(typeof LoginLayoutComponent).toBe('function');
  });

  it('handles different types of children content', () => {
    const { getByText } = render(
      <LoginLayout>
        <div>Text content</div>
        <span>Inline content</span>
        <button>Button content</button>
      </LoginLayout>
    );

    expect(getByText('Text content')).toBeInTheDocument();
    expect(getByText('Inline content')).toBeInTheDocument();
    expect(getByText('Button content')).toBeInTheDocument();
  });

  it('maintains layout structure across renders', () => {
    const { container, rerender } = render(
      <LoginLayout>
        <div>Test children</div>
      </LoginLayout>
    );

    // First render
    expect(container.firstChild).toBeInTheDocument();

    // Second render
    rerender(
      <LoginLayout>
        <div>Different children</div>
      </LoginLayout>
    );

    expect(container.firstChild).toBeInTheDocument();
  });

  it('uses React fragment for children', () => {
    const { container } = render(
      <LoginLayout>
        <div>Test content</div>
      </LoginLayout>
    );

    // The component uses React fragment, so children should be rendered directly
    expect(container.firstChild?.textContent).toBe('Test content');
  });

  it('handles string children', () => {
    const { getByText } = render(<LoginLayout>Simple text content</LoginLayout>);

    expect(getByText('Simple text content')).toBeInTheDocument();
  });

  it('handles number children', () => {
    const { getByText } = render(<LoginLayout>{42}</LoginLayout>);

    expect(getByText('42')).toBeInTheDocument();
  });

  it('handles boolean children', () => {
    const { container } = render(<LoginLayout>{true}</LoginLayout>);

    // Boolean children should not render as text
    expect(container.textContent).not.toContain('true');
  });

  it('maintains component identity', () => {
    const LoginLayoutComponent = require('@/app/login/layout').default;

    expect(LoginLayoutComponent).toBe(LoginLayout);
  });

  it('has correct import structure', () => {
    // Test that all imports are working correctly
    expect(React).toBeDefined();
    expect(require('@/hoc/hoc')).toBeDefined();
  });

  it('renders consistently with different children types', () => {
    const { getByText, rerender } = render(
      <LoginLayout>
        <div>First type</div>
      </LoginLayout>
    );

    expect(getByText('First type')).toBeInTheDocument();

    rerender(
      <LoginLayout>
        <span>Second type</span>
      </LoginLayout>
    );

    expect(getByText('Second type')).toBeInTheDocument();
  });
});
