import {
  AuthorizationEffectivePermissionsResult,
  AuthorizationRole,
  CredentialScopeAssignment,
  UserRoleAssignment,
} from '../../src/models/AuthorizationModels';
import EnumerationResult from '../../src/models/EnumerationResult';
import { api } from '../setupTest';

describe('AuthorizationRoute Tests', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('lists authorization roles with built-ins included by default', async () => {
    jest.spyOn(api, 'getMany').mockImplementation(async (url, model) => new EnumerationResult({
      Success: true,
      MaxResults: 1000,
      ContinuationToken: null,
      EndOfResults: true,
      TotalRecords: 1,
      RecordsRemaining: 0,
      Objects: [
        {
          GUID: 'role-1',
          Name: 'Viewer',
          BuiltIn: true,
          ResourceScope: 'Tenant',
          Permissions: ['Read'],
          ResourceTypes: ['Graph'],
        },
      ],
    }, model));

    const result = await api.listAuthorizationRoles();

    expect(result instanceof EnumerationResult).toBe(true);
    expect(result.TotalRecords).toBe(1);
    expect(result.Objects[0] instanceof AuthorizationRole).toBe(true);
    expect(result.Objects[0].Name).toBe('Viewer');
    expect(api.getMany).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/roles?page=0&pageSize=1000&includeBuiltIns=true`,
      AuthorizationRole,
      undefined
    );
  });

  test('creates and updates custom authorization roles', async () => {
    jest.spyOn(api, 'putCreate').mockImplementation(async (url, role, model) => new model({
      ...role,
      GUID: 'role-1',
    }));
    jest.spyOn(api, 'putUpdate').mockImplementation(async (url, role, model) => new model(role));

    const created = await api.createAuthorizationRole({
      Name: 'GraphAuditor',
      ResourceScope: 'Graph',
      Permissions: ['Read'],
      ResourceTypes: ['Graph', 'Node'],
    });
    const updated = await api.updateAuthorizationRole({
      ...created,
      Description: 'Reads graph metadata and nodes',
    });

    expect(created.GUID).toBe('role-1');
    expect(updated.Description).toBe('Reads graph metadata and nodes');
    expect(api.putCreate).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/roles`,
      expect.objectContaining({ Name: 'GraphAuditor' }),
      AuthorizationRole,
      undefined
    );
    expect(api.putUpdate).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/roles/role-1`,
      expect.objectContaining({ GUID: 'role-1' }),
      AuthorizationRole,
      undefined
    );
  });

  test('manages user role assignment routes', async () => {
    jest.spyOn(api, 'putCreate').mockImplementation(async (url, assignment, model) => new model({
      ...assignment,
      GUID: 'assignment-1',
      TenantGUID: api.tenantGuid,
      UserGUID: 'user-1',
    }));
    jest.spyOn(api, 'delete').mockImplementation(async () => undefined);

    const assignment = await api.createUserRoleAssignment('user-1', {
      RoleName: 'Viewer',
      ResourceScope: 'Graph',
      GraphGUID: 'graph-1',
    });
    await api.deleteUserRoleAssignment('user-1', 'assignment-1');

    expect(assignment instanceof UserRoleAssignment).toBe(true);
    expect(assignment.RoleName).toBe('Viewer');
    expect(api.putCreate).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/users/user-1/roles`,
      expect.objectContaining({ RoleName: 'Viewer' }),
      UserRoleAssignment,
      undefined
    );
    expect(api.delete).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/users/user-1/roles/assignment-1`,
      undefined
    );
  });

  test('manages credential scopes and effective permissions', async () => {
    jest.spyOn(api, 'putCreate').mockImplementation(async (url, assignment, model) => new model({
      ...assignment,
      GUID: 'scope-1',
      TenantGUID: api.tenantGuid,
      CredentialGUID: 'credential-1',
    }));
    jest.spyOn(api, 'get').mockImplementation(async (url, model) => new model({
      TenantGUID: api.tenantGuid,
      CredentialGUID: 'credential-1',
      GraphGUID: 'graph-1',
      Grants: [
        {
          Source: 'CredentialScope',
          AssignmentGUID: 'scope-1',
          ResourceScope: 'Graph',
          GraphGUID: 'graph-1',
          Permissions: ['Read'],
          ResourceTypes: ['Query'],
          AppliesToRequestedGraph: true,
        },
      ],
    }));

    const scope = await api.createCredentialScopeAssignment('credential-1', {
      ResourceScope: 'Graph',
      GraphGUID: 'graph-1',
      Permissions: ['Read'],
      ResourceTypes: ['Query'],
    });
    const effective = await api.getCredentialEffectivePermissions('credential-1', 'graph-1');

    expect(scope instanceof CredentialScopeAssignment).toBe(true);
    expect(effective instanceof AuthorizationEffectivePermissionsResult).toBe(true);
    expect(effective.Grants[0].AppliesToRequestedGraph).toBe(true);
    expect(api.get).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/credentials/credential-1/permissions?graphGuid=graph-1`,
      AuthorizationEffectivePermissionsResult,
      undefined
    );
  });
});
