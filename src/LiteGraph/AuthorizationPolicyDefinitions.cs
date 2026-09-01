namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Built-in authorization policy definitions.
    /// </summary>
    public static class AuthorizationPolicyDefinitions
    {
        #region Public-Members

        /// <summary>
        /// Role name for tenant administrators.
        /// </summary>
        public const string TenantAdminRoleName = "TenantAdmin";

        /// <summary>
        /// Role name for graph administrators.
        /// </summary>
        public const string GraphAdminRoleName = "GraphAdmin";

        /// <summary>
        /// Role name for graph editors.
        /// </summary>
        public const string EditorRoleName = "Editor";

        /// <summary>
        /// Role name for graph viewers.
        /// </summary>
        public const string ViewerRoleName = "Viewer";

        /// <summary>
        /// Role name for chat administrators.
        /// </summary>
        public const string ChatAdminRoleName = "ChatAdmin";

        /// <summary>
        /// Role name for custom roles.
        /// </summary>
        public const string CustomRoleName = "Custom";

        /// <summary>
        /// Existing users receive effective full access in their tenant during RBAC migration.
        /// </summary>
        public static bool ExistingUsersReceiveTenantAdminEquivalentAccess { get { return true; } }

        /// <summary>
        /// Existing credentials with null or empty scopes remain unrestricted within their tenant.
        /// </summary>
        public static bool EmptyCredentialScopesPreserveFullAccess { get { return true; } }

        /// <summary>
        /// Existing credentials with null or empty graph allow-lists remain unrestricted within their tenant.
        /// </summary>
        public static bool EmptyCredentialGraphAllowListsPreserveTenantGraphAccess { get { return true; } }

        /// <summary>
        /// Administrator bearer token remains an administrator credential.
        /// </summary>
        public static bool AdminBearerTokenRemainsAdmin { get { return true; } }

        /// <summary>
        /// External identity mapping is out of scope for this release.
        /// </summary>
        public static bool ExternalIdentityMappingIsOutOfScope { get { return true; } }

        /// <summary>
        /// Built-in role definitions.
        /// </summary>
        public static IReadOnlyList<RoleDefinition> BuiltInRoles
        {
            get
            {
                return _BuiltInRoles.Select(role => role.Clone()).ToList();
            }
        }

        #endregion

        #region Private-Members

        private static readonly List<AuthorizationPermissionEnum> _AllPermissions = new List<AuthorizationPermissionEnum>
        {
            AuthorizationPermissionEnum.Read,
            AuthorizationPermissionEnum.Write,
            AuthorizationPermissionEnum.Delete,
            AuthorizationPermissionEnum.Admin
        };

        private static readonly List<AuthorizationResourceTypeEnum> _AllResourceTypes = new List<AuthorizationResourceTypeEnum>
        {
            AuthorizationResourceTypeEnum.Admin,
            AuthorizationResourceTypeEnum.Graph,
            AuthorizationResourceTypeEnum.Node,
            AuthorizationResourceTypeEnum.Edge,
            AuthorizationResourceTypeEnum.Label,
            AuthorizationResourceTypeEnum.Tag,
            AuthorizationResourceTypeEnum.Vector,
            AuthorizationResourceTypeEnum.Query,
            AuthorizationResourceTypeEnum.Transaction,
            AuthorizationResourceTypeEnum.Chat
        };

        private static readonly List<AuthorizationResourceTypeEnum> _GraphResourceTypes = new List<AuthorizationResourceTypeEnum>
        {
            AuthorizationResourceTypeEnum.Graph,
            AuthorizationResourceTypeEnum.Node,
            AuthorizationResourceTypeEnum.Edge,
            AuthorizationResourceTypeEnum.Label,
            AuthorizationResourceTypeEnum.Tag,
            AuthorizationResourceTypeEnum.Vector,
            AuthorizationResourceTypeEnum.Query,
            AuthorizationResourceTypeEnum.Transaction
        };

        private static readonly List<AuthorizationResourceTypeEnum> _ReadOnlyGraphResourceTypes = new List<AuthorizationResourceTypeEnum>
        {
            AuthorizationResourceTypeEnum.Graph,
            AuthorizationResourceTypeEnum.Node,
            AuthorizationResourceTypeEnum.Edge,
            AuthorizationResourceTypeEnum.Label,
            AuthorizationResourceTypeEnum.Tag,
            AuthorizationResourceTypeEnum.Vector,
            AuthorizationResourceTypeEnum.Query
        };

        private static readonly List<RoleDefinition> _BuiltInRoles = new List<RoleDefinition>
        {
            new RoleDefinition
            {
                Name = TenantAdminRoleName,
                DisplayName = "Tenant Admin",
                Description = "Full administrative access within one tenant, including all graphs in the tenant.",
                BuiltInRole = BuiltInRoleEnum.TenantAdmin,
                BuiltIn = true,
                ResourceScope = AuthorizationResourceScopeEnum.Tenant,
                Permissions = new List<AuthorizationPermissionEnum>(_AllPermissions),
                ResourceTypes = new List<AuthorizationResourceTypeEnum>(_AllResourceTypes),
                InheritsToGraphs = true
            },
            new RoleDefinition
            {
                Name = GraphAdminRoleName,
                DisplayName = "Graph Admin",
                Description = "Full administrative access within one graph, excluding tenant/server administration.",
                BuiltInRole = BuiltInRoleEnum.GraphAdmin,
                BuiltIn = true,
                ResourceScope = AuthorizationResourceScopeEnum.Graph,
                Permissions = new List<AuthorizationPermissionEnum>(_AllPermissions),
                ResourceTypes = new List<AuthorizationResourceTypeEnum>(_GraphResourceTypes),
                InheritsToGraphs = false
            },
            new RoleDefinition
            {
                Name = EditorRoleName,
                DisplayName = "Editor",
                Description = "Read, write, and delete graph data within the assigned graph scope.",
                BuiltInRole = BuiltInRoleEnum.Editor,
                BuiltIn = true,
                ResourceScope = AuthorizationResourceScopeEnum.Graph,
                Permissions = new List<AuthorizationPermissionEnum>
                {
                    AuthorizationPermissionEnum.Read,
                    AuthorizationPermissionEnum.Write,
                    AuthorizationPermissionEnum.Delete
                },
                ResourceTypes = new List<AuthorizationResourceTypeEnum>(_GraphResourceTypes),
                InheritsToGraphs = false
            },
            new RoleDefinition
            {
                Name = ViewerRoleName,
                DisplayName = "Viewer",
                Description = "Read graph data and execute read-only queries within the assigned graph scope.",
                BuiltInRole = BuiltInRoleEnum.Viewer,
                BuiltIn = true,
                ResourceScope = AuthorizationResourceScopeEnum.Graph,
                Permissions = new List<AuthorizationPermissionEnum>
                {
                    AuthorizationPermissionEnum.Read
                },
                ResourceTypes = new List<AuthorizationResourceTypeEnum>(_ReadOnlyGraphResourceTypes),
                InheritsToGraphs = false
            },
            new RoleDefinition
            {
                Name = ChatAdminRoleName,
                DisplayName = "Chat Admin",
                Description = "Manage chat endpoints, settings, feedback, and all-user chat history for the tenant.",
                BuiltInRole = BuiltInRoleEnum.ChatAdmin,
                BuiltIn = true,
                ResourceScope = AuthorizationResourceScopeEnum.Tenant,
                Permissions = new List<AuthorizationPermissionEnum>(_AllPermissions),
                ResourceTypes = new List<AuthorizationResourceTypeEnum>
                {
                    AuthorizationResourceTypeEnum.Chat
                },
                InheritsToGraphs = false
            },
            new RoleDefinition
            {
                Name = CustomRoleName,
                DisplayName = "Custom",
                Description = "Template marker for custom roles. Permissions and resource types are supplied by the stored role.",
                BuiltInRole = BuiltInRoleEnum.Custom,
                BuiltIn = true,
                ResourceScope = AuthorizationResourceScopeEnum.Graph,
                Permissions = new List<AuthorizationPermissionEnum>(),
                ResourceTypes = new List<AuthorizationResourceTypeEnum>(),
                InheritsToGraphs = false
            }
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve a built-in role by name.
        /// </summary>
        /// <param name="name">Role name.</param>
        /// <returns>Role definition, or null.</returns>
        public static RoleDefinition GetBuiltInRole(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return null;
            RoleDefinition role = _BuiltInRoles.FirstOrDefault(r => String.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
            return role?.Clone();
        }

        /// <summary>
        /// Retrieve a built-in role by enum value.
        /// </summary>
        /// <param name="role">Role.</param>
        /// <returns>Role definition, or null.</returns>
        public static RoleDefinition GetBuiltInRole(BuiltInRoleEnum role)
        {
            RoleDefinition definition = _BuiltInRoles.FirstOrDefault(r => r.BuiltInRole == role);
            return definition?.Clone();
        }

        #endregion
    }
}
