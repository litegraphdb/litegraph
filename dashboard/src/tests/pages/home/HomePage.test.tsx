import '@testing-library/jest-dom';
import React from 'react';
import { screen, waitFor } from '@testing-library/react';
import { render } from '@testing-library/react';
import { useRouter } from 'next/navigation';
import HomePage from '@/page/home/HomePage';
import { paths } from '@/constants/constant';

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}));

const mockPush = jest.fn();

describe('HomePage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({
      push: mockPush,
    });
  });

  it('should render redirecting message', () => {
    render(<HomePage />);
    expect(screen.getByText('Redirecting...')).toBeInTheDocument();
  });

  it('should redirect to login page on mount', async () => {
    render(<HomePage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith(paths.login);
    });
  });

  it('should call router.push only once', async () => {
    render(<HomePage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledTimes(1);
    });
  });

  it('should redirect to correct login path', async () => {
    render(<HomePage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should render a div element', () => {
    const { container } = render(<HomePage />);
    const divElement = container.querySelector('div');
    expect(divElement).toBeInTheDocument();
  });
});
