import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import DefaultCredentialsBanner, { defaultCredentialsDismissalKey } from '@/components/layout/DefaultCredentialsBanner';

const mockRequest = jest.fn();
const mockPrincipal = { isSystemAdmin: true, isBreakGlass: false };
jest.mock('@/hooks/permissionHooks', () => ({ usePrincipal: () => mockPrincipal }));
jest.mock('@/lib/sdk/settings', () => ({ getBaseUrl: () => 'http://localhost:8701', request: (...args: unknown[]) => mockRequest(...args) }));
jest.mock('next-intl', () => ({ useTranslations: () => (key: string) => key }));

describe('DefaultCredentialsBanner', () => {
  beforeEach(() => {
    sessionStorage.clear();
    mockRequest.mockReset();
    mockPrincipal.isSystemAdmin = true;
    mockPrincipal.isBreakGlass = false;
  });

  it('warns an administrator, preserves dismissal across routes, and returns after a new login', async () => {
    mockRequest.mockResolvedValue({ HasDefaultCredentials: true });
    const first = render(<DefaultCredentialsBanner />);
    await screen.findByText('title');
    fireEvent.click(screen.getByRole('button'));
    expect(sessionStorage.getItem(defaultCredentialsDismissalKey)).toBe('true');
    first.unmount();
    const nextRoute = render(<DefaultCredentialsBanner />);
    expect(screen.queryByText('title')).toBeNull();
    expect(mockRequest).toHaveBeenCalledTimes(1);
    nextRoute.unmount();
    sessionStorage.removeItem(defaultCredentialsDismissalKey);
    render(<DefaultCredentialsBanner />);
    await screen.findByText('title');
    expect(mockRequest).toHaveBeenCalledTimes(2);
  });

  it('does not query or warn non-administrators', () => {
    mockPrincipal.isSystemAdmin = false;
    render(<DefaultCredentialsBanner />);
    expect(mockRequest).not.toHaveBeenCalled();
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('does not warn when every default has been removed', async () => {
    mockRequest.mockResolvedValue({ HasDefaultCredentials: false });
    render(<DefaultCredentialsBanner />);
    await waitFor(() => expect(mockRequest).toHaveBeenCalledTimes(1));
    expect(screen.queryByText('title')).toBeNull();
  });

  it('checks defaults for a break-glass login and tolerates an unavailable diagnostic', async () => {
    mockPrincipal.isSystemAdmin = false;
    mockPrincipal.isBreakGlass = true;
    mockRequest.mockRejectedValue(new Error('offline'));
    render(<DefaultCredentialsBanner />);
    await waitFor(() => expect(mockRequest).toHaveBeenCalledTimes(1));
    expect(screen.queryByText('title')).toBeNull();
  });
});
