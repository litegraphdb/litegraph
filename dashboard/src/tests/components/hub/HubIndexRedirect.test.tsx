import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import HubIndexRedirect from '@/components/hub/HubIndexRedirect';
import { Principal } from '@/lib/authz/capabilities';

let mockPathname = '';
const mockReplace = jest.fn();
jest.mock('next/navigation', () => ({
  usePathname: () => mockPathname,
  useRouter: () => ({ push: jest.fn(), replace: mockReplace, prefetch: jest.fn() }),
}));

let mockPrincipal: Principal;
jest.mock('@/hooks/permissionHooks', () => {
  const actual = jest.requireActual('@/lib/authz/capabilities');
  return {
    usePrincipal: () => mockPrincipal,
    useCan: () => ({
      principal: mockPrincipal,
      can: (action: any, resource: any, scope: any) =>
        actual.can(mockPrincipal, action, resource, scope),
    }),
  };
});

jest.mock('@/hooks/hooks', () => ({
  useAppDynamicNavigation: () => ({ navigate: jest.fn(), serializePath: (p: string) => p }),
}));

jest.mock('@/hooks/entityHooks', () => ({
  useCurrentTenant: () => ({ GUID: 'redux-tenant' }),
}));

const principal = (overrides: Partial<Principal>): Principal => ({
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'u',
  tenantGuid: 't1',
  ...overrides,
});

const setSearch = (search: string) => window.history.replaceState({}, '', `/${search}`);

describe('HubIndexRedirect (bare hub path)', () => {
  beforeEach(() => {
    mockReplace.mockClear();
    setSearch('');
  });

  it('opens the first tab of a tenant hub, keeping the query string', () => {
    mockPrincipal = principal({});
    mockPathname = '/dashboard/url-tenant/graphs';
    setSearch('?graph=g1');
    render(<HubIndexRedirect hubId="graphs" />);
    expect(mockReplace).toHaveBeenCalledWith('/dashboard/url-tenant/graphs/graphs?graph=g1');
  });

  it('opens the first tab the principal may view', () => {
    mockPrincipal = principal({ isSystemAdmin: true });
    mockPathname = '/dashboard/system';
    render(<HubIndexRedirect hubId="system" />);
    expect(mockReplace).toHaveBeenCalledWith('/dashboard/system/settings');

    mockReplace.mockClear();
    mockPrincipal = principal({});
    mockPathname = '/dashboard/access';
    render(<HubIndexRedirect hubId="access" />);
    expect(mockReplace).toHaveBeenCalledWith('/dashboard/access/tenants');
  });

  it('uses the active tenant when the hub path carries none', () => {
    mockPrincipal = principal({});
    mockPathname = '';
    render(<HubIndexRedirect hubId="chat" />);
    expect(mockReplace).toHaveBeenCalledWith('/dashboard/redux-tenant/chat/chat');
  });

  it('denies access when no tab of the hub is viewable', () => {
    mockPrincipal = principal({});
    mockPathname = '/dashboard/system';
    render(<HubIndexRedirect hubId="system" />);
    expect(mockReplace).not.toHaveBeenCalled();
    expect(screen.getByTestId('capability-denied')).toBeInTheDocument();
  });
});
