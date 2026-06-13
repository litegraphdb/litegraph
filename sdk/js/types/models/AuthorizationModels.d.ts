export class AuthorizationRole {
    constructor(role?: {});
    GUID: any;
    TenantGUID: any;
    Name: any;
    DisplayName: any;
    Description: any;
    BuiltIn: any;
    BuiltInRole: any;
    ResourceScope: any;
    Permissions: any;
    ResourceTypes: any;
    InheritsToGraphs: any;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
export class UserRoleAssignment {
    constructor(assignment?: {});
    GUID: any;
    TenantGUID: any;
    UserGUID: any;
    RoleGUID: any;
    RoleName: any;
    ResourceScope: any;
    GraphGUID: any;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
export class CredentialScopeAssignment {
    constructor(assignment?: {});
    GUID: any;
    TenantGUID: any;
    CredentialGUID: any;
    RoleGUID: any;
    RoleName: any;
    ResourceScope: any;
    GraphGUID: any;
    Permissions: any;
    ResourceTypes: any;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
export class AuthorizationEffectiveGrant {
    constructor(grant?: {});
    Source: any;
    AssignmentGUID: any;
    RoleGUID: any;
    RoleName: any;
    ResourceScope: any;
    GraphGUID: any;
    Permissions: any;
    ResourceTypes: any;
    InheritsToGraphs: any;
    AppliesToRequestedGraph: any;
}
export class AuthorizationEffectivePermissionsResult {
    constructor(result?: {});
    TenantGUID: any;
    UserGUID: any;
    CredentialGUID: any;
    GraphGUID: any;
    Grants: any;
    UserRoleAssignments: any;
    CredentialScopeAssignments: any;
    Roles: any;
}
export class AuthorizationRoleSearchResult extends AuthorizationSearchResultBase {
    constructor(result?: {});
}
export class UserRoleAssignmentSearchResult extends AuthorizationSearchResultBase {
    constructor(result?: {});
}
export class CredentialScopeAssignmentSearchResult extends AuthorizationSearchResultBase {
    constructor(result?: {});
}
declare class AuthorizationSearchResultBase {
    constructor(result: {}, model: any);
    Objects: any;
    Page: any;
    PageSize: any;
    TotalCount: any;
    TotalPages: any;
}
export {};
