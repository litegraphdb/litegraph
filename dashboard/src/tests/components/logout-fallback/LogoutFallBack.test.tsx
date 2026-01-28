import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, act } from '@testing-library/react';
import LogoutFallBack from '@/components/logout-fallback/LogoutFallBack';

// Mock the auth hooks
jest.mock('@/hooks/authHooks', () => ({
  useLogout: jest.fn(),
}));

// Mock the FallBack component
jest.mock('@/components/base/fallback/FallBack', () => {
  return function MockFallBack({ children, className }: any) {
    return (
      <div data-testid="fallback" className={className}>
        {children}
      </div>
    );
  };
});

describe('LogoutFallBack Component', () => {
  const mockLogout = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();

    const { useLogout } = require('@/hooks/authHooks');
    useLogout.mockReturnValue(mockLogout);
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders without crashing', () => {
    render(<LogoutFallBack />);

    expect(screen.getByTestId('fallback')).toBeInTheDocument();
    expect(screen.getByText(/Session invalid./)).toBeInTheDocument();
    expect(screen.getByText(/Logging out in 3 seconds.../)).toBeInTheDocument();
  });

  it('renders with default message when no message prop is provided', () => {
    render(<LogoutFallBack />);

    expect(screen.getByText('Session invalid. Logging out in 3 seconds...')).toBeInTheDocument();
  });

  it('renders with custom message when message prop is provided', () => {
    const customMessage = 'Custom logout message';
    render(<LogoutFallBack message={customMessage} />);

    expect(screen.getByText(`${customMessage} Logging out in 3 seconds...`)).toBeInTheDocument();
  });

  it('starts countdown from 3 seconds', () => {
    render(<LogoutFallBack />);

    expect(screen.getByText(/Logging out in 3 seconds.../)).toBeInTheDocument();
  });

  it('decrements countdown every second', () => {
    render(<LogoutFallBack />);

    // Initial state
    expect(screen.getByText(/Logging out in 3 seconds.../)).toBeInTheDocument();

    // After 1 second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 2 seconds.../)).toBeInTheDocument();

    // After 2 seconds
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 1 seconds.../)).toBeInTheDocument();
  });

  it('cleans up timer on unmount', () => {
    const { unmount } = render(<LogoutFallBack />);

    // Advance timer partially
    act(() => {
      jest.advanceTimersByTime(1000);
    });

    expect(screen.getByText(/Logging out in 2 seconds.../)).toBeInTheDocument();

    // Unmount component
    unmount();

    // Advance timer further - logout should not be called since component is unmounted
    act(() => {
      jest.advanceTimersByTime(2000);
    });

    expect(mockLogout).not.toHaveBeenCalled();
  });

  it('handles empty message string', () => {
    render(<LogoutFallBack message="" />);

    expect(screen.getByText(/Logging out in 3 seconds.../)).toBeInTheDocument();
  });

  it('handles message with special characters', () => {
    const specialMessage = 'Session expired! @#$%^&*()';
    render(<LogoutFallBack message={specialMessage} />);

    expect(screen.getByText(`${specialMessage} Logging out in 3 seconds...`)).toBeInTheDocument();
  });

  it('handles very long message', () => {
    const longMessage = 'A'.repeat(1000);
    render(<LogoutFallBack message={longMessage} />);

    expect(screen.getByText(`${longMessage} Logging out in 3 seconds...`)).toBeInTheDocument();
  });

  it('passes correct className to FallBack component', () => {
    render(<LogoutFallBack />);

    const fallback = screen.getByTestId('fallback');
    expect(fallback).toHaveClass('mt-12', 'pt-12');
  });

  it('handles logout function that throws error', () => {
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    mockLogout.mockImplementation(() => {
      throw new Error('Logout failed');
    });

    render(<LogoutFallBack />);

    // Advance timer to trigger logout
    act(() => {
      jest.advanceTimersByTime(3000);
    });

    // Should not crash the component
    expect(screen.getByTestId('fallback')).toBeInTheDocument();

    consoleSpy.mockRestore();
  });
});
