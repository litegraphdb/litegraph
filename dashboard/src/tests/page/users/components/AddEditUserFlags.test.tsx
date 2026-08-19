import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import AddEditUser from '@/page/users/components/AddEditUser';
import { Principal } from '@/lib/authz/capabilities';

let currentPrincipal: Principal;
jest.mock('@/hooks/permissionHooks', () => ({
  usePrincipal: () => currentPrincipal,
}));

jest.mock('@/lib/store/slice/slice', () => ({
  useCreateUserMutation: () => [jest.fn(), { isLoading: false }],
  useUpdateUserMutation: () => [jest.fn(), { isLoading: false }],
}));

const base: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'u',
  tenantGuid: 't',
};

const renderForm = () =>
  render(
    <AddEditUser
      isAddEditUserVisible={true}
      setIsAddEditUserVisible={() => {}}
      user={null}
    />
  );

describe('AddEditUser capability-flag controls', () => {
  it('shows both admin switches for a SystemAdmin', () => {
    currentPrincipal = { ...base, isSystemAdmin: true };
    renderForm();
    expect(screen.getByTestId('system-admin-switch')).toBeInTheDocument();
    expect(screen.getByTestId('tenant-admin-switch')).toBeInTheDocument();
  });

  it('shows only the tenant-admin switch for a TenantAdmin', () => {
    currentPrincipal = { ...base, isTenantAdmin: true };
    renderForm();
    expect(screen.queryByTestId('system-admin-switch')).not.toBeInTheDocument();
    expect(screen.getByTestId('tenant-admin-switch')).toBeInTheDocument();
  });

  it('hides both admin switches for a regular user', () => {
    currentPrincipal = { ...base };
    renderForm();
    expect(screen.queryByTestId('system-admin-switch')).not.toBeInTheDocument();
    expect(screen.queryByTestId('tenant-admin-switch')).not.toBeInTheDocument();
  });
});
