import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';
import { Principal } from '@/lib/authz/capabilities';

const mockNavigate = jest.fn();
jest.mock('@/hooks/hooks', () => ({
  useAppDynamicNavigation: () => ({ navigate: mockNavigate, serializePath: (p: string) => p }),
}));

let currentPrincipal: Principal;
jest.mock('@/hooks/permissionHooks', () => {
  const actual = jest.requireActual('@/lib/authz/capabilities');
  return {
    useCan: () => ({
      principal: currentPrincipal,
      can: (action: any, resource: any, scope: any) =>
        actual.can(currentPrincipal, action, resource, scope),
      canViewSection: (section: any) => actual.canViewSection(currentPrincipal, section),
    }),
  };
});

const systemAdmin: Principal = {
  isSystemAdmin: true,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'sa',
  tenantGuid: 't',
};
const regular: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'ru',
  tenantGuid: 't',
};

describe('CapabilityRouteGuard', () => {
  it('renders children when the principal may view the resource', () => {
    currentPrincipal = systemAdmin;
    render(
      <CapabilityRouteGuard resource="settings">
        <div data-testid="protected">Settings</div>
      </CapabilityRouteGuard>
    );
    expect(screen.getByTestId('protected')).toBeInTheDocument();
    expect(screen.queryByTestId('capability-denied')).not.toBeInTheDocument();
  });

  it('denies a regular user deep-linking to Settings (ADMINISTER)', () => {
    currentPrincipal = regular;
    render(
      <CapabilityRouteGuard resource="settings">
        <div data-testid="protected">Settings</div>
      </CapabilityRouteGuard>
    );
    expect(screen.queryByTestId('protected')).not.toBeInTheDocument();
    expect(screen.getByTestId('capability-denied')).toBeInTheDocument();
  });

  it('denies a regular user deep-linking to Authorization', () => {
    currentPrincipal = regular;
    render(
      <CapabilityRouteGuard resource="authorization">
        <div data-testid="protected">Authorization</div>
      </CapabilityRouteGuard>
    );
    expect(screen.getByTestId('capability-denied')).toBeInTheDocument();
  });
});
