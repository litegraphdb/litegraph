import type { EnumerateResponse } from 'litegraphdb/dist/types/types';
import { sdk } from './litegraph.service';

export type AuthorizationPermission = 'Read' | 'Write' | 'Delete' | 'Admin';
export type AuthorizationResourceScope = 'Tenant' | 'Graph';
export type AuthorizationResourceType =
  | 'Admin'
  | 'Graph'
  | 'Node'
  | 'Edge'
  | 'Label'
  | 'Tag'
  | 'Vector'
  | 'Query'
  | 'Transaction'
  | 'Chat';
export type BuiltInRole = 'TenantAdmin' | 'GraphAdmin' | 'Editor' | 'Viewer' | 'ChatAdmin' | 'Custom';

export type AuthorizationRole = {
  GUID: string;
  TenantGUID?: string | null;
  Name: string;
  DisplayName?: string | null;
  Description?: string | null;
  BuiltIn: boolean;
  BuiltInRole?: BuiltInRole;
  ResourceScope: AuthorizationResourceScope;
  Permissions: AuthorizationPermission[];
  ResourceTypes: AuthorizationResourceType[];
  InheritsToGraphs: boolean;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
};

export type UserRoleAssignment = {
  GUID: string;
  TenantGUID: string;
  UserGUID: string;
  RoleGUID?: string | null;
  RoleName?: string | null;
  ResourceScope: AuthorizationResourceScope;
  GraphGUID?: string | null;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
};

export type CredentialScopeAssignment = {
  GUID: string;
  TenantGUID: string;
  CredentialGUID: string;
  RoleGUID?: string | null;
  RoleName?: string | null;
  ResourceScope: AuthorizationResourceScope;
  GraphGUID?: string | null;
  Permissions: AuthorizationPermission[];
  ResourceTypes: AuthorizationResourceType[];
  CreatedUtc?: string;
  LastUpdateUtc?: string;
};

export type AuthorizationEffectiveGrant = {
  Source: string;
  AssignmentGUID: string;
  RoleGUID?: string | null;
  RoleName?: string | null;
  ResourceScope: AuthorizationResourceScope;
  GraphGUID?: string | null;
  Permissions: AuthorizationPermission[];
  ResourceTypes: AuthorizationResourceType[];
  InheritsToGraphs: boolean;
  AppliesToRequestedGraph: boolean;
};

export type AuthorizationEffectivePermissionsResult = {
  TenantGUID: string;
  UserGUID?: string | null;
  CredentialGUID?: string | null;
  GraphGUID?: string | null;
  Grants: AuthorizationEffectiveGrant[];
  UserRoleAssignments: UserRoleAssignment[];
  CredentialScopeAssignments: CredentialScopeAssignment[];
  Roles: AuthorizationRole[];
};

/**
 * Authorization list routes now return the standard EnumerationResult envelope
 * ({ Success, Timestamp, MaxResults, ContinuationToken, EndOfResults,
 * TotalRecords, RecordsRemaining, Objects }).
 */
export type AuthorizationSearchResult<T> = EnumerateResponse<T>;

export type AuthorizationListParams = {
  page?: number;
  pageSize?: number;
};

export type RoleListParams = AuthorizationListParams & {
  includeBuiltIns?: boolean;
  builtIn?: boolean;
};

const getBaseUrl = (): string => {
  const endpoint = sdk.config.endpoint || '/';
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

const buildHeaders = (): Record<string, string> => {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };
  const defaults = (sdk.config as unknown as { defaultHeaders?: Record<string, string> })
    .defaultHeaders;
  if (defaults) {
    for (const key of Object.keys(defaults)) headers[key] = defaults[key];
  }
  return headers;
};

const buildQuery = (params: Record<string, string | number | boolean | undefined>): string => {
  const entries = Object.entries(params).filter(
    ([, value]) => value !== undefined && value !== null && value !== ''
  );
  if (entries.length === 0) return '';
  return (
    '?' +
    entries
      .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
      .join('&')
  );
};

const request = async <T>(method: string, url: string, body?: unknown): Promise<T> => {
  const headers = buildHeaders();
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const response = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    let message = `HTTP ${response.status} ${response.statusText}`;
    try {
      const errorBody = await response.json();
      message = errorBody?.Description || errorBody?.Message || message;
    } catch {
      // Keep the HTTP status message when the server did not return JSON.
    }
    throw new Error(message);
  }
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
};

export const listAuthorizationRoles = (tenantGuid: string, params: RoleListParams = {}) => {
  const pageSize = params.pageSize ?? 1000;
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(tenantGuid)}/roles${buildQuery({
    'max-keys': pageSize,
    skip: (params.page ?? 0) * pageSize || undefined,
    includeBuiltIns: params.includeBuiltIns ?? true,
    builtIn: params.builtIn,
  })}`;
  return request<AuthorizationSearchResult<AuthorizationRole>>('GET', url);
};

export const createAuthorizationRole = (tenantGuid: string, role: Partial<AuthorizationRole>) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(tenantGuid)}/roles`;
  return request<AuthorizationRole>('PUT', url, role);
};

export const updateAuthorizationRole = (tenantGuid: string, role: AuthorizationRole) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/roles/${encodeURIComponent(role.GUID)}`;
  return request<AuthorizationRole>('PUT', url, role);
};

export const deleteAuthorizationRole = (tenantGuid: string, roleGuid: string) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/roles/${encodeURIComponent(roleGuid)}`;
  return request<void>('DELETE', url);
};

export const listUserRoleAssignments = (
  tenantGuid: string,
  userGuid: string,
  params: AuthorizationListParams = {}
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/users/${encodeURIComponent(userGuid)}/roles${buildQuery({
    'max-keys': params.pageSize ?? 1000,
    skip: (params.page ?? 0) * (params.pageSize ?? 1000) || undefined,
  })}`;
  return request<AuthorizationSearchResult<UserRoleAssignment>>('GET', url);
};

export const createUserRoleAssignment = (
  tenantGuid: string,
  userGuid: string,
  assignment: Partial<UserRoleAssignment>
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/users/${encodeURIComponent(userGuid)}/roles`;
  return request<UserRoleAssignment>('PUT', url, assignment);
};

export const deleteUserRoleAssignment = (
  tenantGuid: string,
  userGuid: string,
  assignmentGuid: string
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/users/${encodeURIComponent(userGuid)}/roles/${encodeURIComponent(assignmentGuid)}`;
  return request<void>('DELETE', url);
};

export const listCredentialScopeAssignments = (
  tenantGuid: string,
  credentialGuid: string,
  params: AuthorizationListParams = {}
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/credentials/${encodeURIComponent(credentialGuid)}/scopes${buildQuery({
    'max-keys': params.pageSize ?? 1000,
    skip: (params.page ?? 0) * (params.pageSize ?? 1000) || undefined,
  })}`;
  return request<AuthorizationSearchResult<CredentialScopeAssignment>>('GET', url);
};

export const createCredentialScopeAssignment = (
  tenantGuid: string,
  credentialGuid: string,
  assignment: Partial<CredentialScopeAssignment>
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/credentials/${encodeURIComponent(credentialGuid)}/scopes`;
  return request<CredentialScopeAssignment>('PUT', url, assignment);
};

export const deleteCredentialScopeAssignment = (
  tenantGuid: string,
  credentialGuid: string,
  assignmentGuid: string
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/credentials/${encodeURIComponent(credentialGuid)}/scopes/${encodeURIComponent(
    assignmentGuid
  )}`;
  return request<void>('DELETE', url);
};

export const getUserEffectivePermissions = (
  tenantGuid: string,
  userGuid: string,
  graphGuid?: string
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/users/${encodeURIComponent(userGuid)}/permissions${buildQuery({ graphGuid })}`;
  return request<AuthorizationEffectivePermissionsResult>('GET', url);
};

export const getCredentialEffectivePermissions = (
  tenantGuid: string,
  credentialGuid: string,
  graphGuid?: string
) => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/credentials/${encodeURIComponent(credentialGuid)}/permissions${buildQuery({ graphGuid })}`;
  return request<AuthorizationEffectivePermissionsResult>('GET', url);
};
