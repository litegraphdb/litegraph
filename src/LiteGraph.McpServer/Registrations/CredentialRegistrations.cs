namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for Credential operations.
    /// </summary>
    public static class CredentialRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers credential tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "credential/create",
                "Creates a new credential in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        credential = new { type = "string", description = "Credential object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "credential" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                        throw new ArgumentException("Credential JSON string is required");
                    string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                    Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                    Credential created = sdk.Credential.Create(credential).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "credential/get",
                "Reads a credential by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuid = new { type = "string", description = "Credential GUID" }
                    },
                    required = new[] { "tenantGuid", "credentialGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");

                    Credential credential = sdk.Credential.ReadByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                    return credential != null ? Serializer.SerializeJson(credential, true) : "null";
                });

            server.RegisterTool(
                "credential/all",
                "Lists all credentials in a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<Credential> credentials = sdk.Credential.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(credentials, true);
                });

            server.RegisterTool(
                "credential/enumerate",
                "Enumerates credentials with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        query = new { type = "string", description = "Enumeration query object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                    
                    if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                    {
                        string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                        EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                        if (deserializedQuery != null)
                        {
                            query.MaxResults = deserializedQuery.MaxResults;
                            query.Ordering = deserializedQuery.Ordering;
                            query.ContinuationToken = deserializedQuery.ContinuationToken;
                            query.IncludeData = deserializedQuery.IncludeData;
                            query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                        }
                    }
                    
                    EnumerationResult<Credential> result = sdk.Credential.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "credential/update",
                "Updates a credential",
                new
                {
                    type = "object",
                    properties = new
                    {
                        credential = new { type = "string", description = "Credential object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "credential" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                        throw new ArgumentException("Credential JSON string is required");
                    string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                    Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                    Credential updated = sdk.Credential.Update(credential).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
                });

            server.RegisterTool(
                "credential/delete",
                "Deletes a credential by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuid = new { type = "string", description = "Credential GUID" }
                    },
                    required = new[] { "tenantGuid", "credentialGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                    sdk.Credential.DeleteByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "credential/exists",
                "Checks if a credential exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuid = new { type = "string", description = "Credential GUID" }
                    },
                    required = new[] { "tenantGuid", "credentialGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                    bool exists = sdk.Credential.ExistsByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "credential/getmany",
                "Reads multiple credentials by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuids = new { type = "array", items = new { type = "string" }, description = "Array of credential GUIDs" }
                    },
                    required = new[] { "tenantGuid", "credentialGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("credentialGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Credential GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    List<Credential> credentials = sdk.Credential.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(credentials, true);
                });

            server.RegisterTool(
                "credential/getbybearertoken",
                "Reads a credential by bearer token",
                new
                {
                    type = "object",
                    properties = new
                    {
                        bearerToken = new { type = "string", description = "Bearer token" }
                    },
                    required = new[] { "bearerToken" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                        throw new ArgumentException("Bearer token is required");
                    string bearerToken = bearerTokenProp.GetString() ?? throw new ArgumentException("Bearer token cannot be null");
                    
                    Credential credential = sdk.Credential.ReadByBearerToken(bearerToken).GetAwaiter().GetResult();
                    return credential != null ? Serializer.SerializeJson(credential, true) : "null";
                });

            server.RegisterTool(
                "credential/deleteallintenant",
                "Deletes all credentials in a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    sdk.Credential.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "credential/deletebyuser",
                "Deletes all credentials for a user",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        userGuid = new { type = "string", description = "User GUID" }
                    },
                    required = new[] { "tenantGuid", "userGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                    sdk.Credential.DeleteByUser(tenantGuid, userGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers credential methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("credential/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                Credential created = sdk.Credential.Create(credential).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("credential/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");

                Credential credential = sdk.Credential.ReadByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                return credential != null ? Serializer.SerializeJson(credential, true) : "null";
            });

            server.RegisterMethod("credential/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Credential> credentials = sdk.Credential.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(credentials, true);
            });

            server.RegisterMethod("credential/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<Credential> result = sdk.Credential.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("credential/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                Credential updated = sdk.Credential.Update(credential).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("credential/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                sdk.Credential.DeleteByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("credential/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                bool exists = sdk.Credential.ExistsByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("credential/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("credentialGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Credential GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Credential> credentials = sdk.Credential.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(credentials, true);
            });

            server.RegisterMethod("credential/getbybearertoken", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                    throw new ArgumentException("Bearer token is required");
                string bearerToken = bearerTokenProp.GetString() ?? throw new ArgumentException("Bearer token cannot be null");
                
                Credential credential = sdk.Credential.ReadByBearerToken(bearerToken).GetAwaiter().GetResult();
                return credential != null ? Serializer.SerializeJson(credential, true) : "null";
            });

            server.RegisterMethod("credential/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Credential.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("credential/deletebyuser", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                sdk.Credential.DeleteByUser(tenantGuid, userGuid).GetAwaiter().GetResult();
                return true;
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers credential methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("credential/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                Credential created = sdk.Credential.Create(credential).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("credential/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");

                Credential credential = sdk.Credential.ReadByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                return credential != null ? Serializer.SerializeJson(credential, true) : "null";
            });

            server.RegisterMethod("credential/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Credential> credentials = sdk.Credential.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(credentials, true);
            });

            server.RegisterMethod("credential/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<Credential> result = sdk.Credential.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("credential/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                Credential updated = sdk.Credential.Update(credential).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("credential/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                sdk.Credential.DeleteByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("credential/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                bool exists = sdk.Credential.ExistsByGuid(tenantGuid, credentialGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("credential/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("credentialGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Credential GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Credential> credentials = sdk.Credential.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(credentials, true);
            });

            server.RegisterMethod("credential/getbybearertoken", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                    throw new ArgumentException("Bearer token is required");
                string bearerToken = bearerTokenProp.GetString() ?? throw new ArgumentException("Bearer token cannot be null");
                
                Credential credential = sdk.Credential.ReadByBearerToken(bearerToken).GetAwaiter().GetResult();
                return credential != null ? Serializer.SerializeJson(credential, true) : "null";
            });

            server.RegisterMethod("credential/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Credential.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("credential/deletebyuser", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                sdk.Credential.DeleteByUser(tenantGuid, userGuid).GetAwaiter().GetResult();
                return true;
            });
        }

        #endregion
    }
}

