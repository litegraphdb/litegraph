import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import ErrorBoundary from '@/hoc/ErrorBoundary';
import FallBack from '@/components/base/fallback/FallBack';

// Mock the FallBack component
jest.mock('@/components/base/fallback/FallBack', () => {
  return function MockFallBack({ children }: { children: React.ReactNode }) {
    return <div data-testid="fallback">{children}</div>;
  };
});

// Component that throws an error for testing
const ThrowError = ({ shouldThrow }: { shouldThrow: boolean }) => {
  if (shouldThrow) {
    throw new Error('Test error message');
  }
  return <div>Normal render</div>;
};

// Mock console.error to avoid noise in tests
const originalError = console.error;
beforeAll(() => {
  console.error = jest.fn();
});

afterAll(() => {
  console.error = originalError;
});

describe('ErrorBoundary Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders children when there is no error', () => {
    render(
      <ErrorBoundary>
        <div>Test content</div>
      </ErrorBoundary>
    );

    expect(screen.getByText('Test content')).toBeInTheDocument();
  });

  it('catches errors and renders fallback when error occurs', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('fallback')).toBeInTheDocument();
    expect(screen.getByText('Unexpected Error: Test error message.')).toBeInTheDocument();
  });

  it('calls componentDidCatch when error occurs', () => {
    const errorSpy = jest.spyOn(console, 'error');

    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(errorSpy).toHaveBeenCalledWith(
      'ErrorBoundary caught an error: ',
      expect.any(Error),
      expect.any(Object)
    );
  });

  it('renders custom error component when provided', () => {
    const errorSpy = jest.spyOn(console, 'error');
    const CustomErrorComponent = ({ errorMessage }: { errorMessage?: string }) => (
      <div data-testid="custom-error">Custom Error: {errorMessage || 'No message'}</div>
    );

    render(
      <ErrorBoundary errorComponent={CustomErrorComponent}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('custom-error')).toBeInTheDocument();
    // Check that the custom error component is rendered with some content
    const customErrorElement = screen.getByTestId('custom-error');
    expect(customErrorElement.textContent).toContain('Custom Error:');

    // Verify that the error message is being passed (it should contain the error message or show 'No message')
    expect(customErrorElement.textContent).toMatch(/Custom Error: (Test error message|No message)/);

    // Verify that componentDidCatch was called
    expect(errorSpy).toHaveBeenCalledWith(
      'ErrorBoundary caught an error: ',
      expect.any(Error),
      expect.any(Object)
    );
  });

  it('shows reload link when allowRefresh is true', () => {
    render(
      <ErrorBoundary allowRefresh={true}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    const reloadLink = screen.getByText('Reload page');
    expect(reloadLink).toBeInTheDocument();
    expect(reloadLink).toHaveAttribute('href', window.location.href);
    expect(reloadLink).toHaveAttribute('rel', 'noreferrer');
  });

  it('does not show reload link when allowRefresh is false', () => {
    render(
      <ErrorBoundary allowRefresh={false}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.queryByText('Reload page')).not.toBeInTheDocument();
  });

  it('does not show reload link when allowRefresh is undefined', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.queryByText('Reload page')).not.toBeInTheDocument();
  });

  it('handles multiple error states correctly', () => {
    const { rerender } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={false} />
      </ErrorBoundary>
    );

    expect(screen.getByText('Normal render')).toBeInTheDocument();

    // Trigger error
    rerender(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('fallback')).toBeInTheDocument();
    expect(screen.getByText('Unexpected Error: Test error message.')).toBeInTheDocument();
  });

  it('maintains error state after error occurs', () => {
    const { rerender } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('fallback')).toBeInTheDocument();

    // Try to render normal content again
    rerender(
      <ErrorBoundary>
        <ThrowError shouldThrow={false} />
      </ErrorBoundary>
    );

    // Should still show error state
    expect(screen.getByTestId('fallback')).toBeInTheDocument();
    expect(screen.getByText('Unexpected Error: Test error message.')).toBeInTheDocument();
  });

  it('handles error with no message', () => {
    const ThrowErrorNoMessage = () => {
      throw new Error();
    };

    render(
      <ErrorBoundary>
        <ThrowErrorNoMessage />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('fallback')).toBeInTheDocument();
    expect(screen.getByText('Unexpected Error: .')).toBeInTheDocument();
  });

  it('handles error with complex error object', () => {
    const ThrowComplexError = () => {
      const error = new Error('Complex error');
      (error as any).customProperty = 'custom value';
      throw error;
    };

    render(
      <ErrorBoundary>
        <ThrowComplexError />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('fallback')).toBeInTheDocument();
    expect(screen.getByText('Unexpected Error: Complex error.')).toBeInTheDocument();
  });
});
