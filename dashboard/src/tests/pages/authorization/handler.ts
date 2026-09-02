import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import {
  mockCredentialData,
  mockGraphData,
  mockTenantGUID,
  mockUserData,
} from '../mockData';

export const mockAuthorizationRoles = [
  {
    GUID: '10000000-0000-0000-0000-000000000001',
    TenantGUID: null,
    Name: 'TenantAdmin',
    DisplayName: 'Tenant Admin',
    Description: 'Full administrative access within one tenant.',
    BuiltIn: true,
    BuiltInRole: 'TenantAdmin',
    ResourceScope: 'Tenant',
    Permissions: ['Read', 'Write', 'Delete', 'Admin'],
    ResourceTypes: [
      'Admin',
      'Graph',
      'Node',
      'Edge',
      'Label',
      'Tag',
      'Vector',
      'Query',
      'Transaction',
    ],
    InheritsToGraphs: true,
    CreatedUtc: '2025-01-01T00:00:00Z',
    LastUpdateUtc: '2025-01-01T00:00:00Z',
  },
  {
    GUID: '10000000-0000-0000-0000-000000000002',
    TenantGUID: null,
    Name: 'Viewer',
    DisplayName: 'Viewer',
    Description: 'Read graph data.',
    BuiltIn: true,
    BuiltInRole: 'Viewer',
    ResourceScope: 'Graph',
    Permissions: ['Read'],
    ResourceTypes: ['Graph', 'Node', 'Edge', 'Label', 'Tag', 'Vector', 'Query'],
    InheritsToGraphs: false,
    CreatedUtc: '2025-01-01T00:00:00Z',
    LastUpdateUtc: '2025-01-01T00:00:00Z',
  },
  {
    GUID: '10000000-0000-0000-0000-000000000003',
    TenantGUID: mockTenantGUID,
    Name: 'GraphAuditor',
    DisplayName: 'Graph Auditor',
    Description: 'Audit selected graph data.',
    BuiltIn: false,
    BuiltInRole: 'Custom',
    ResourceScope: 'Graph',
    Permissions: ['Read'],
    ResourceTypes: ['Graph', 'Node', 'Edge', 'Query'],
    InheritsToGraphs: false,
    CreatedUtc: '2025-01-01T00:00:00Z',
    LastUpdateUtc: '2025-01-01T00:00:00Z',
  },
];

export const mockUserRoleAssignments = [
  {
    GUID: '20000000-0000-0000-0000-000000000001',
    TenantGUID: mockTenantGUID,
    UserGUID: mockUserData[0].GUID,
    RoleGUID: null,
    RoleName: 'Viewer',
    ResourceScope: 'Graph',
    GraphGUID: mockGraphData[0].GUID,
    CreatedUtc: '2025-01-01T00:00:00Z',
    LastUpdateUtc: '2025-01-01T00:00:00Z',
  },
];

export const mockCredentialScopeAssignments = [
  {
    GUID: '30000000-0000-0000-0000-000000000001',
    TenantGUID: mockTenantGUID,
    CredentialGUID: mockCredentialData[0].GUID,
    RoleGUID: null,
    RoleName: 'Viewer',
    ResourceScope: 'Graph',
    GraphGUID: mockGraphData[0].GUID,
    Permissions: [],
    ResourceTypes: [],
    CreatedUtc: '2025-01-01T00:00:00Z',
    LastUpdateUtc: '2025-01-01T00:00:00Z',
  },
];

// Authorization list routes now return the EnumerationResult envelope.
const searchResult = <T,>(objects: T[]) => ({
  Success: true,
  MaxResults: 1000,
  EndOfResults: true,
  TotalRecords: objects.length,
  RecordsRemaining: 0,
  Objects: objects,
});

const adminEffectivePermissions = {
  TenantGUID: mockTenantGUID,
  UserGUID: mockUserData[0].GUID,
  CredentialGUID: null,
  GraphGUID: null,
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
  UserRoleAssignments: mockUserRoleAssignments,
  CredentialScopeAssignments: [],
  Roles: [mockAuthorizationRoles[0]],
};

export const viewerEffectivePermissions = {
  ...adminEffectivePermissions,
  Grants: [
    {
      Source: 'UserRole',
      AssignmentGUID: '40000000-0000-0000-0000-000000000002',
      RoleGUID: null,
      RoleName: 'Viewer',
      ResourceScope: 'Graph',
      GraphGUID: mockGraphData[0].GUID,
      Permissions: ['Read'],
      ResourceTypes: ['Graph', 'Node', 'Edge', 'Label', 'Tag', 'Vector', 'Query'],
      InheritsToGraphs: false,
      AppliesToRequestedGraph: true,
    },
  ],
  Roles: [mockAuthorizationRoles[1]],
};

export const handlers = [
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/roles`, () => {
    return HttpResponse.json(searchResult(mockAuthorizationRoles));
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/roles`, async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json({
      ...body,
      GUID: '10000000-0000-0000-0000-000000000004',
      TenantGUID: mockTenantGUID,
      BuiltIn: false,
      BuiltInRole: 'Custom',
      CreatedUtc: '2025-01-01T00:00:00Z',
      LastUpdateUtc: '2025-01-01T00:00:00Z',
    });
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/roles/:roleGuid`, async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json(body);
  }),
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/roles/:roleGuid`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`, () => {
    return HttpResponse.json(mockUserData);
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials`, () => {
    return HttpResponse.json(mockCredentialData);
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs`, () => {
    return HttpResponse.json(mockGraphData);
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/:userGuid/roles`, () => {
    return HttpResponse.json(searchResult(mockUserRoleAssignments));
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/:userGuid/roles`, () => {
    return HttpResponse.json(mockUserRoleAssignments[0]);
  }),
  http.delete(
    `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/:userGuid/roles/:assignmentGuid`,
    () => {
      return new HttpResponse(null, { status: 204 });
    }
  ),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials/:credentialGuid/scopes`, () => {
    return HttpResponse.json(searchResult(mockCredentialScopeAssignments));
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials/:credentialGuid/scopes`, () => {
    return HttpResponse.json(mockCredentialScopeAssignments[0]);
  }),
  http.delete(
    `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials/:credentialGuid/scopes/:assignmentGuid`,
    () => {
      return new HttpResponse(null, { status: 204 });
    }
  ),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/:userGuid/permissions`, () => {
    return HttpResponse.json(adminEffectivePermissions);
  }),
  http.get(
    `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials/:credentialGuid/permissions`,
    () => {
      return HttpResponse.json({
        ...adminEffectivePermissions,
        UserGUID: null,
        CredentialGUID: mockCredentialData[0].GUID,
        CredentialScopeAssignments: mockCredentialScopeAssignments,
      });
    }
  ),
];
