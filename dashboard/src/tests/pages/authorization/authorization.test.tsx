import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import AuthorizationPage from '@/page/authorization/AuthorizationPage';
import { createMockInitialState } from '@/tests/store/mockStore';
import { renderWithRedux } from '@/tests/store/utils';
import { mockCredentialData, mockGraphData, mockUserData } from '../mockData';
import {
  mockAuthorizationRoles,
  mockCredentialScopeAssignments,
  mockUserRoleAssignments,
  viewerEffectivePermissions,
} from './handler';
import { AuthorizationEffectivePermissionsResult, AuthorizationRole } from '@/lib/sdk/authorization';

jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

jest.mock('@/lib/store/slice/slice', () => ({
  useCreateAuthorizationRoleMutation: jest.fn(),
  useCreateCredentialScopeAssignmentMutation: jest.fn(),
  useCreateUserRoleAssignmentMutation: jest.fn(),
  useDeleteAuthorizationRoleMutation: jest.fn(),
  useDeleteCredentialScopeAssignmentMutation: jest.fn(),
  useDeleteUserRoleAssignmentMutation: jest.fn(),
  useGetAllCredentialsQuery: jest.fn(),
  useGetAllGraphsQuery: jest.fn(),
  useGetAllUsersQuery: jest.fn(),
  useGetCredentialEffectivePermissionsQuery: jest.fn(),
  useGetUserEffectivePermissionsQuery: jest.fn(),
  useListAuthorizationRolesQuery: jest.fn(),
  useListCredentialScopeAssignmentsQuery: jest.fn(),
  useListUserRoleAssignmentsQuery: jest.fn(),
  useUpdateAuthorizationRoleMutation: jest.fn(),
}));

const sliceHooks = jest.requireMock('@/lib/store/slice/slice');

// Authorization list routes now return the EnumerationResult envelope.
const searchResult = <T,>(objects: T[]) => ({
  Success: true,
  MaxResults: 1000,
  EndOfResults: true,
  TotalRecords: objects.length,
  RecordsRemaining: 0,
  Objects: objects,
});

const mutationHook = () => [
  jest.fn().mockReturnValue({ unwrap: jest.fn().mockResolvedValue({}) }),
  { isLoading: false },
];

const adminEffectivePermissions = {
  ...viewerEffectivePermissions,
  Grants: [
    {
      Source: 'UserRole',
      AssignmentGUID: '40000000-0000-0000-0000-000000000001',
      RoleGUID: null,
      RoleName: 'TenantAdmin',
      ResourceScope: 'Tenant',
      GraphGUID: null,
      Permissions: ['Read', 'Write', 'Delete', 'Admin'],
      ResourceTypes: ['Admin', 'Graph', 'Node', 'Edge', 'Label', 'Tag', 'Vector', 'Query'],
      InheritsToGraphs: true,
      AppliesToRequestedGraph: true,
    },
  ],
  Roles: [mockAuthorizationRoles[0] as AuthorizationRole],
} as AuthorizationEffectivePermissionsResult;

const mockPageHooks = (
  currentPermissions: AuthorizationEffectivePermissionsResult = adminEffectivePermissions
) => {
  sliceHooks.useListAuthorizationRolesQuery.mockReturnValue({
    data: searchResult(mockAuthorizationRoles),
    refetch: jest.fn(),
    isLoading: false,
    isFetching: false,
    error: null,
  });
  sliceHooks.useGetAllUsersQuery.mockReturnValue({ data: mockUserData, isLoading: false });
  sliceHooks.useGetAllCredentialsQuery.mockReturnValue({
    data: mockCredentialData,
    isLoading: false,
  });
  sliceHooks.useGetAllGraphsQuery.mockReturnValue({ data: mockGraphData, isLoading: false });
  sliceHooks.useGetUserEffectivePermissionsQuery.mockReturnValue({
    data: currentPermissions,
    isLoading: false,
  });
  sliceHooks.useListUserRoleAssignmentsQuery.mockReturnValue({
    data: searchResult(mockUserRoleAssignments),
    isLoading: false,
    isFetching: false,
  });
  sliceHooks.useListCredentialScopeAssignmentsQuery.mockReturnValue({
    data: searchResult(mockCredentialScopeAssignments),
    isLoading: false,
    isFetching: false,
  });
  sliceHooks.useGetCredentialEffectivePermissionsQuery.mockReturnValue({
    data: {
      ...adminEffectivePermissions,
      UserGUID: null,
      CredentialGUID: mockCredentialData[0].GUID,
      CredentialScopeAssignments: mockCredentialScopeAssignments,
    },
  });
  sliceHooks.useCreateAuthorizationRoleMutation.mockReturnValue(mutationHook());
  sliceHooks.useUpdateAuthorizationRoleMutation.mockReturnValue(mutationHook());
  sliceHooks.useDeleteAuthorizationRoleMutation.mockReturnValue(mutationHook());
  sliceHooks.useCreateUserRoleAssignmentMutation.mockReturnValue(mutationHook());
  sliceHooks.useDeleteUserRoleAssignmentMutation.mockReturnValue(mutationHook());
  sliceHooks.useCreateCredentialScopeAssignmentMutation.mockReturnValue(mutationHook());
  sliceHooks.useDeleteCredentialScopeAssignmentMutation.mockReturnValue(mutationHook());
};

describe('AuthorizationPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockPageHooks();
  });

  it('renders role management with built-in roles immutable', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AuthorizationPage />, initialState, undefined, true);

    expect(await screen.findByText('Tenant Admin')).toBeVisible();
    expect(screen.getByText('Graph Auditor')).toBeVisible();
    expect(screen.getAllByText('Immutable').length).toBeGreaterThan(0);
    expect(screen.getByRole('button', { name: /create role/i })).toBeEnabled();
  });

  it('shows a role action menu only for custom roles and opens role JSON', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AuthorizationPage />, initialState, undefined, true);

    expect(await screen.findByText('Graph Auditor')).toBeVisible();
    expect(screen.getAllByText('Immutable').length).toBeGreaterThan(0);

    const actionMenus = screen.getAllByRole('authorization-role-action-menu');
    expect(actionMenus).toHaveLength(1);

    fireEvent.click(actionMenus[0]);

    expect(await screen.findByRole('menuitem', { name: /edit/i })).toBeVisible();
    expect(screen.getByRole('menuitem', { name: /view json/i })).toBeVisible();
    expect(screen.getByRole('menuitem', { name: /delete/i })).toBeVisible();

    fireEvent.click(screen.getByRole('menuitem', { name: /view json/i }));

    expect(await screen.findByText('Authorization Role JSON')).toBeVisible();
    expect(screen.getByTestId('view-json-content')).toHaveTextContent('GraphAuditor');
    expect(screen.getByRole('button', { name: /copy json/i })).toBeInTheDocument();
  });

  it('shows user assignments and effective permissions', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AuthorizationPage />, initialState, undefined, true);

    fireEvent.click(await screen.findByRole('tab', { name: /user roles/i }));

    await waitFor(() => {
      expect(screen.getByText('Effective User Permissions')).toBeVisible();
      expect(screen.getAllByText('Viewer').length).toBeGreaterThan(0);
    });
  });

  it('shows credential scope assignments', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<AuthorizationPage />, initialState, undefined, true);

    fireEvent.click(await screen.findByRole('tab', { name: /credential scopes/i }));

    await waitFor(() => {
      expect(screen.getByText('Effective Credential Permissions')).toBeVisible();
      expect(screen.getAllByText('Viewer').length).toBeGreaterThan(0);
    });
  });

  it('disables mutation actions without authorization admin permission', async () => {
    mockPageHooks(viewerEffectivePermissions as AuthorizationEffectivePermissionsResult);
    const initialState = createMockInitialState();
    initialState.liteGraph.adminAccessKey = null;
    initialState.liteGraph.user = mockUserData[0];

    renderWithRedux(<AuthorizationPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(
        screen.getByText('Admin permission is required to change roles or credential scopes.')
      ).toBeVisible();
      expect(screen.getByRole('button', { name: /create role/i })).toBeDisabled();
    });
  });
});
