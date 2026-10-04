namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for authorization role and scope operations.
    /// </summary>
    public static class AuthorizationRegistrations
    {
        #region Public-Methods

        /// <summary>
        /// Registers authorization tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            foreach (ToolDefinition definition in GetToolDefinitions())
            {
                server.RegisterLiteGraphTool(
                    definition.Name,
                    definition.Description,
                    definition.Schema,
                    (args) => definition.Handler(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            }
        }

        /// <summary>
        /// Registers authorization methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            foreach (ToolDefinition definition in GetToolDefinitions())
            {
                server.RegisterLiteGraphMethod(definition.Name, (args) => definition.Handler(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            }
        }

        /// <summary>
        /// Registers authorization methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            foreach (ToolDefinition definition in GetToolDefinitions())
            {
                server.RegisterLiteGraphMethod(definition.Name, (args) => definition.Handler(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            }
        }

        #endregion

        #region Private-Methods

        private static List<ToolDefinition> GetToolDefinitions()
        {
            return new List<ToolDefinition>
            {
                new ToolDefinition(
                    "authorization_role_create",
                    "Creates an authorization role in a tenant",
                    JsonBodySchema("role", "AuthorizationRole object serialized as JSON string using Serializer"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        string roleJson = GetStringRequired(value, "role");
                        return Send(sdk, HttpMethod.Put, TenantPath(tenantGuid) + "/roles", roleJson);
                    }),

                new ToolDefinition(
                    "authorization_role_get",
                    "Reads an authorization role by GUID",
                    TenantRoleSchema(),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid roleGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "roleGuid");
                        return Send(sdk, HttpMethod.Get, TenantPath(tenantGuid) + "/roles/" + LiteGraphMcpRestProxy.Escape(roleGuid));
                    }),

                new ToolDefinition(
                    "authorization_role_all",
                    "Lists authorization roles visible to a tenant",
                    RoleListSchema(),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        string query = BuildQuery(
                            value,
                            "includeBuiltIns",
                            "name",
                            "builtIn",
                            "builtInRole",
                            "resourceScope",
                            "permission",
                            "resourceType",
                            "fromUtc",
                            "toUtc",
                            "page",
                            "pageSize");
                        return Send(sdk, HttpMethod.Get, TenantPath(tenantGuid) + "/roles" + query);
                    }),

                new ToolDefinition(
                    "authorization_role_update",
                    "Updates an authorization role",
                    JsonBodySchema("role", "AuthorizationRole object serialized as JSON string using Serializer", "roleGuid", "Role GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid roleGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "roleGuid");
                        string roleJson = GetStringRequired(value, "role");
                        return Send(sdk, HttpMethod.Put, TenantPath(tenantGuid) + "/roles/" + LiteGraphMcpRestProxy.Escape(roleGuid), roleJson);
                    }),

                new ToolDefinition(
                    "authorization_role_delete",
                    "Deletes an authorization role",
                    TenantRoleSchema(),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid roleGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "roleGuid");
                        Send(sdk, HttpMethod.Delete, TenantPath(tenantGuid) + "/roles/" + LiteGraphMcpRestProxy.Escape(roleGuid));
                        return true;
                    }),

                new ToolDefinition(
                    "authorization_userrole_create",
                    "Assigns a role to a user",
                    JsonBodySchema("assignment", "UserRoleAssignment object serialized as JSON string using Serializer", "userGuid", "User GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "userGuid");
                        string assignmentJson = GetStringRequired(value, "assignment");
                        return Send(sdk, HttpMethod.Put, UserRolePath(tenantGuid, userGuid), assignmentJson);
                    }),

                new ToolDefinition(
                    "authorization_userrole_all",
                    "Lists role assignments for a user",
                    UserRoleListSchema(),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "userGuid");
                        string query = BuildQuery(value, "roleGuid", "roleName", "resourceScope", "graphGuid", "fromUtc", "toUtc", "page", "pageSize");
                        return Send(sdk, HttpMethod.Get, UserRolePath(tenantGuid, userGuid) + query);
                    }),

                new ToolDefinition(
                    "authorization_userrole_get",
                    "Reads a user role assignment by GUID",
                    AssignmentSchema("userGuid", "User GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "userGuid");
                        Guid assignmentGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "assignmentGuid");
                        return Send(sdk, HttpMethod.Get, UserRolePath(tenantGuid, userGuid) + "/" + LiteGraphMcpRestProxy.Escape(assignmentGuid));
                    }),

                new ToolDefinition(
                    "authorization_userrole_update",
                    "Updates a user role assignment",
                    JsonBodySchema("assignment", "UserRoleAssignment object serialized as JSON string using Serializer", "userGuid", "User GUID", "assignmentGuid", "Assignment GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "userGuid");
                        Guid assignmentGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "assignmentGuid");
                        string assignmentJson = GetStringRequired(value, "assignment");
                        return Send(sdk, HttpMethod.Put, UserRolePath(tenantGuid, userGuid) + "/" + LiteGraphMcpRestProxy.Escape(assignmentGuid), assignmentJson);
                    }),

                new ToolDefinition(
                    "authorization_userrole_delete",
                    "Revokes a user role assignment",
                    AssignmentSchema("userGuid", "User GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "userGuid");
                        Guid assignmentGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "assignmentGuid");
                        Send(sdk, HttpMethod.Delete, UserRolePath(tenantGuid, userGuid) + "/" + LiteGraphMcpRestProxy.Escape(assignmentGuid));
                        return true;
                    }),

                new ToolDefinition(
                    "authorization_user_permissions",
                    "Lists effective permissions for a user",
                    EffectivePermissionsSchema("userGuid", "User GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "userGuid");
                        string query = BuildQuery(value, "graphGuid");
                        return Send(sdk, HttpMethod.Get, TenantPath(tenantGuid) + "/users/" + LiteGraphMcpRestProxy.Escape(userGuid) + "/permissions" + query);
                    }),

                new ToolDefinition(
                    "authorization_credentialscope_create",
                    "Assigns an authorization scope to a credential",
                    JsonBodySchema("assignment", "CredentialScopeAssignment object serialized as JSON string using Serializer", "credentialGuid", "Credential GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "credentialGuid");
                        string assignmentJson = GetStringRequired(value, "assignment");
                        return Send(sdk, HttpMethod.Put, CredentialScopePath(tenantGuid, credentialGuid), assignmentJson);
                    }),

                new ToolDefinition(
                    "authorization_credentialscope_all",
                    "Lists authorization scope assignments for a credential",
                    CredentialScopeListSchema(),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "credentialGuid");
                        string query = BuildQuery(value, "roleGuid", "roleName", "resourceScope", "graphGuid", "permission", "resourceType", "fromUtc", "toUtc", "page", "pageSize");
                        return Send(sdk, HttpMethod.Get, CredentialScopePath(tenantGuid, credentialGuid) + query);
                    }),

                new ToolDefinition(
                    "authorization_credentialscope_get",
                    "Reads a credential scope assignment by GUID",
                    AssignmentSchema("credentialGuid", "Credential GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "credentialGuid");
                        Guid assignmentGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "assignmentGuid");
                        return Send(sdk, HttpMethod.Get, CredentialScopePath(tenantGuid, credentialGuid) + "/" + LiteGraphMcpRestProxy.Escape(assignmentGuid));
                    }),

                new ToolDefinition(
                    "authorization_credentialscope_update",
                    "Updates a credential scope assignment",
                    JsonBodySchema("assignment", "CredentialScopeAssignment object serialized as JSON string using Serializer", "credentialGuid", "Credential GUID", "assignmentGuid", "Assignment GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "credentialGuid");
                        Guid assignmentGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "assignmentGuid");
                        string assignmentJson = GetStringRequired(value, "assignment");
                        return Send(sdk, HttpMethod.Put, CredentialScopePath(tenantGuid, credentialGuid) + "/" + LiteGraphMcpRestProxy.Escape(assignmentGuid), assignmentJson);
                    }),

                new ToolDefinition(
                    "authorization_credentialscope_delete",
                    "Revokes a credential scope assignment",
                    AssignmentSchema("credentialGuid", "Credential GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "credentialGuid");
                        Guid assignmentGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "assignmentGuid");
                        Send(sdk, HttpMethod.Delete, CredentialScopePath(tenantGuid, credentialGuid) + "/" + LiteGraphMcpRestProxy.Escape(assignmentGuid));
                        return true;
                    }),

                new ToolDefinition(
                    "authorization_credential_permissions",
                    "Lists effective permissions for a credential",
                    EffectivePermissionsSchema("credentialGuid", "Credential GUID"),
                    (sdk, args) =>
                    {
                        JsonElement value = RequireArgs(args);
                        Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "tenantGuid");
                        Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(value, "credentialGuid");
                        string query = BuildQuery(value, "graphGuid");
                        return Send(sdk, HttpMethod.Get, TenantPath(tenantGuid) + "/credentials/" + LiteGraphMcpRestProxy.Escape(credentialGuid) + "/permissions" + query);
                    })
            };
        }

        private static string Send(LiteGraphSdk sdk, HttpMethod method, string pathAndQuery, string? body = null)
        {
            return LiteGraphMcpRestProxy.SendJson(sdk, method, pathAndQuery, body);
        }

        private static JsonElement RequireArgs(JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            return args.Value;
        }

        private static string GetStringRequired(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                throw new ArgumentException("Required parameter '" + propertyName + "' is missing");

            string? value = prop.GetString();
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("Required parameter '" + propertyName + "' cannot be empty");

            return value;
        }

        private static string BuildQuery(JsonElement element, params string[] parameterNames)
        {
            List<string> parameters = new List<string>();
            foreach (string parameterName in parameterNames)
            {
                if (!element.TryGetProperty(parameterName, out JsonElement prop)) continue;
                if (prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined) continue;

                string value = QueryValue(prop);
                if (String.IsNullOrEmpty(value)) continue;

                parameters.Add(LiteGraphMcpRestProxy.Escape(parameterName) + "=" + LiteGraphMcpRestProxy.Escape(value));
            }

            return parameters.Count > 0 ? "?" + String.Join("&", parameters) : String.Empty;
        }

        private static string QueryValue(JsonElement prop)
        {
            switch (prop.ValueKind)
            {
                case JsonValueKind.String:
                    return prop.GetString() ?? String.Empty;
                case JsonValueKind.Number:
                    return prop.GetRawText();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                default:
                    return prop.GetRawText();
            }
        }

        private static string TenantPath(Guid tenantGuid)
        {
            return "/v1.0/tenants/" + LiteGraphMcpRestProxy.Escape(tenantGuid);
        }

        private static string UserRolePath(Guid tenantGuid, Guid userGuid)
        {
            return TenantPath(tenantGuid) + "/users/" + LiteGraphMcpRestProxy.Escape(userGuid) + "/roles";
        }

        private static string CredentialScopePath(Guid tenantGuid, Guid credentialGuid)
        {
            return TenantPath(tenantGuid) + "/credentials/" + LiteGraphMcpRestProxy.Escape(credentialGuid) + "/scopes";
        }

        private static object JsonBodySchema(string bodyName, string bodyDescription, params string[] additionalRequiredNameDescriptionPairs)
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties[bodyName] = new { type = "string", description = bodyDescription };

            List<string> required = new List<string> { "tenantGuid" };
            for (int i = 0; i < additionalRequiredNameDescriptionPairs.Length; i += 2)
            {
                string name = additionalRequiredNameDescriptionPairs[i];
                string description = additionalRequiredNameDescriptionPairs[i + 1];
                properties[name] = new { type = "string", description = description };
                required.Add(name);
            }
            required.Add(bodyName);

            return ObjectSchema(properties, required);
        }

        private static object TenantRoleSchema()
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties["roleGuid"] = new { type = "string", description = "Role GUID" };
            return ObjectSchema(properties, new List<string> { "tenantGuid", "roleGuid" });
        }

        private static object AssignmentSchema(string ownerName, string ownerDescription)
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties[ownerName] = new { type = "string", description = ownerDescription };
            properties["assignmentGuid"] = new { type = "string", description = "Assignment GUID" };
            return ObjectSchema(properties, new List<string> { "tenantGuid", ownerName, "assignmentGuid" });
        }

        private static object EffectivePermissionsSchema(string ownerName, string ownerDescription)
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties[ownerName] = new { type = "string", description = ownerDescription };
            properties["graphGuid"] = new { type = "string", description = "Optional graph GUID filter" };
            return ObjectSchema(properties, new List<string> { "tenantGuid", ownerName });
        }

        private static object RoleListSchema()
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties["includeBuiltIns"] = new { type = "boolean", description = "Include built-in roles (default true)" };
            properties["name"] = new { type = "string", description = "Role name filter" };
            properties["builtIn"] = new { type = "boolean", description = "Built-in role filter" };
            properties["builtInRole"] = new { type = "string", description = "Built-in role kind" };
            properties["resourceScope"] = new { type = "string", description = "Tenant or Graph" };
            properties["permission"] = new { type = "string", description = "Read, Write, Delete, or Admin" };
            properties["resourceType"] = new { type = "string", description = "Resource type filter" };
            AddCommonSearchProperties(properties);
            return ObjectSchema(properties, new List<string> { "tenantGuid" });
        }

        private static object UserRoleListSchema()
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties["userGuid"] = new { type = "string", description = "User GUID" };
            properties["roleGuid"] = new { type = "string", description = "Role GUID filter" };
            properties["roleName"] = new { type = "string", description = "Role name filter" };
            properties["resourceScope"] = new { type = "string", description = "Tenant or Graph" };
            properties["graphGuid"] = new { type = "string", description = "Graph GUID filter" };
            AddCommonSearchProperties(properties);
            return ObjectSchema(properties, new List<string> { "tenantGuid", "userGuid" });
        }

        private static object CredentialScopeListSchema()
        {
            Dictionary<string, object> properties = BaseTenantProperties();
            properties["credentialGuid"] = new { type = "string", description = "Credential GUID" };
            properties["roleGuid"] = new { type = "string", description = "Role GUID filter" };
            properties["roleName"] = new { type = "string", description = "Role name filter" };
            properties["resourceScope"] = new { type = "string", description = "Tenant or Graph" };
            properties["graphGuid"] = new { type = "string", description = "Graph GUID filter" };
            properties["permission"] = new { type = "string", description = "Read, Write, Delete, or Admin" };
            properties["resourceType"] = new { type = "string", description = "Resource type filter" };
            AddCommonSearchProperties(properties);
            return ObjectSchema(properties, new List<string> { "tenantGuid", "credentialGuid" });
        }

        private static Dictionary<string, object> BaseTenantProperties()
        {
            return new Dictionary<string, object>
            {
                { "tenantGuid", new { type = "string", description = "Tenant GUID" } }
            };
        }

        private static void AddCommonSearchProperties(Dictionary<string, object> properties)
        {
            properties["fromUtc"] = new { type = "string", description = "Earliest creation timestamp, inclusive" };
            properties["toUtc"] = new { type = "string", description = "Latest creation timestamp, exclusive" };
            properties["page"] = new { type = "integer", description = "Page index, default 0" };
            properties["pageSize"] = new { type = "integer", description = "Page size, default 100" };
        }

        private static object ObjectSchema(Dictionary<string, object> properties, List<string> required)
        {
            return new
            {
                type = "object",
                properties = properties,
                required = required.ToArray()
            };
        }

        #endregion

        #region Private-Classes

        private sealed class ToolDefinition
        {
            public string Name { get; }

            public string Description { get; }

            public object Schema { get; }

            public Func<LiteGraphSdk, JsonElement?, object> Handler { get; }

            public ToolDefinition(string name, string description, object schema, Func<LiteGraphSdk, JsonElement?, object> handler)
            {
                Name = name;
                Description = description;
                Schema = schema;
                Handler = handler;
            }
        }

        #endregion
    }
}
