export class AuthorizationRole {
  constructor(role = {}) {
    const {
      GUID = null,
      TenantGUID = null,
      Name = '',
      DisplayName = null,
      Description = null,
      BuiltIn = false,
      BuiltInRole = 'Custom',
      ResourceScope = 'Tenant',
      Permissions = [],
      ResourceTypes = [],
      InheritsToGraphs = false,
      CreatedUtc = null,
      LastUpdateUtc = null,
    } = role || {};

    this.GUID = GUID;
    this.TenantGUID = TenantGUID;
    this.Name = Name;
    this.DisplayName = DisplayName;
    this.Description = Description;
    this.BuiltIn = BuiltIn;
    this.BuiltInRole = BuiltInRole;
    this.ResourceScope = ResourceScope;
    this.Permissions = Permissions;
    this.ResourceTypes = ResourceTypes;
    this.InheritsToGraphs = InheritsToGraphs;
    this.CreatedUtc = CreatedUtc ? new Date(CreatedUtc) : null;
    this.LastUpdateUtc = LastUpdateUtc ? new Date(LastUpdateUtc) : null;
  }
}

export class UserRoleAssignment {
  constructor(assignment = {}) {
    const {
      GUID = null,
      TenantGUID = null,
      UserGUID = null,
      RoleGUID = null,
      RoleName = null,
      ResourceScope = 'Tenant',
      GraphGUID = null,
      CreatedUtc = null,
      LastUpdateUtc = null,
    } = assignment || {};

    this.GUID = GUID;
    this.TenantGUID = TenantGUID;
    this.UserGUID = UserGUID;
    this.RoleGUID = RoleGUID;
    this.RoleName = RoleName;
    this.ResourceScope = ResourceScope;
    this.GraphGUID = GraphGUID;
    this.CreatedUtc = CreatedUtc ? new Date(CreatedUtc) : null;
    this.LastUpdateUtc = LastUpdateUtc ? new Date(LastUpdateUtc) : null;
  }
}

export class CredentialScopeAssignment {
  constructor(assignment = {}) {
    const {
      GUID = null,
      TenantGUID = null,
      CredentialGUID = null,
      RoleGUID = null,
      RoleName = null,
      ResourceScope = 'Tenant',
      GraphGUID = null,
      Permissions = [],
      ResourceTypes = [],
      CreatedUtc = null,
      LastUpdateUtc = null,
    } = assignment || {};

    this.GUID = GUID;
    this.TenantGUID = TenantGUID;
    this.CredentialGUID = CredentialGUID;
    this.RoleGUID = RoleGUID;
    this.RoleName = RoleName;
    this.ResourceScope = ResourceScope;
    this.GraphGUID = GraphGUID;
    this.Permissions = Permissions;
    this.ResourceTypes = ResourceTypes;
    this.CreatedUtc = CreatedUtc ? new Date(CreatedUtc) : null;
    this.LastUpdateUtc = LastUpdateUtc ? new Date(LastUpdateUtc) : null;
  }
}

export class AuthorizationEffectiveGrant {
  constructor(grant = {}) {
    const {
      Source = '',
      AssignmentGUID = null,
      RoleGUID = null,
      RoleName = null,
      ResourceScope = 'Tenant',
      GraphGUID = null,
      Permissions = [],
      ResourceTypes = [],
      InheritsToGraphs = false,
      AppliesToRequestedGraph = false,
    } = grant || {};

    this.Source = Source;
    this.AssignmentGUID = AssignmentGUID;
    this.RoleGUID = RoleGUID;
    this.RoleName = RoleName;
    this.ResourceScope = ResourceScope;
    this.GraphGUID = GraphGUID;
    this.Permissions = Permissions;
    this.ResourceTypes = ResourceTypes;
    this.InheritsToGraphs = InheritsToGraphs;
    this.AppliesToRequestedGraph = AppliesToRequestedGraph;
  }
}

export class AuthorizationEffectivePermissionsResult {
  constructor(result = {}) {
    const {
      TenantGUID = null,
      UserGUID = null,
      CredentialGUID = null,
      GraphGUID = null,
      Grants = [],
      UserRoleAssignments = [],
      CredentialScopeAssignments = [],
      Roles = [],
    } = result || {};

    this.TenantGUID = TenantGUID;
    this.UserGUID = UserGUID;
    this.CredentialGUID = CredentialGUID;
    this.GraphGUID = GraphGUID;
    this.Grants = Grants.map((grant) => new AuthorizationEffectiveGrant(grant));
    this.UserRoleAssignments = UserRoleAssignments.map((assignment) => new UserRoleAssignment(assignment));
    this.CredentialScopeAssignments = CredentialScopeAssignments.map(
      (assignment) => new CredentialScopeAssignment(assignment)
    );
    this.Roles = Roles.map((role) => new AuthorizationRole(role));
  }
}

class AuthorizationSearchResultBase {
  constructor(result = {}, model) {
    const { Objects = [], Page = 0, PageSize = 100, TotalCount = Objects.length, TotalPages = 0 } = result || {};

    this.Objects = Objects.map((object) => new model(object));
    this.Page = Page;
    this.PageSize = PageSize;
    this.TotalCount = TotalCount;
    this.TotalPages = TotalPages;
  }
}

export class AuthorizationRoleSearchResult extends AuthorizationSearchResultBase {
  constructor(result = {}) {
    super(result, AuthorizationRole);
  }
}

export class UserRoleAssignmentSearchResult extends AuthorizationSearchResultBase {
  constructor(result = {}) {
    super(result, UserRoleAssignment);
  }
}

export class CredentialScopeAssignmentSearchResult extends AuthorizationSearchResultBase {
  constructor(result = {}) {
    super(result, CredentialScopeAssignment);
  }
}
