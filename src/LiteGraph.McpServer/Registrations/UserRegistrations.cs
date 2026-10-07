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
    /// Registration methods for User operations.
    /// </summary>
    public static class UserRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers user tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphTool(
                "user_create",
                "Creates a new user in LiteGraph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "user": { "type": "string", "description": "User object serialized as JSON string using Serializer" }
                        },
                        "required": [ "user" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                        throw new ArgumentException("User JSON string is required");
                    string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                    UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                    return CreateUser(sdk, user);
                });

            server.RegisterLiteGraphTool(
                "user_get",
                "Reads a user by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "userGuid": { "type": "string", "description": "User GUID" }
                        },
                        "required": [ "tenantGuid", "userGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                    return ReadUser(sdk, tenantGuid, userGuid);
                });

            server.RegisterLiteGraphTool(
                "user_all",
                "Lists all users in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadUsers(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterLiteGraphTool(
                "user_enumerate",
                "Enumerates users with pagination and filtering",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "query": { "type": "string", "description": "Enumeration request serialized as JSON string using Serializer" }
                        },
                        "required": [ "query" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                        throw new ArgumentException("Enumeration query is required");

                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                    if (query.TenantGUID == null)
                        throw new ArgumentException("query.TenantGUID is required.");

                    return EnumerateUsers(sdk, query);
                });

            server.RegisterLiteGraphTool(
                "user_update",
                "Updates a user",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "user": { "type": "string", "description": "User object serialized as JSON string using Serializer" }
                        },
                        "required": [ "user" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                        throw new ArgumentException("User JSON string is required");
                    string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                    UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                    return UpdateUser(sdk, user);
                });

            server.RegisterLiteGraphTool(
                "user_delete",
                "Deletes a user by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "userGuid": { "type": "string", "description": "User GUID" }
                        },
                        "required": [ "tenantGuid", "userGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                    DeleteUser(sdk, tenantGuid, userGuid);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "user_exists",
                "Checks if a user exists by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "userGuid": { "type": "string", "description": "User GUID" }
                        },
                        "required": [ "tenantGuid", "userGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                    return UserExists(sdk, tenantGuid, userGuid);
                });

            server.RegisterLiteGraphTool(
                "user_getmany",
                "Reads multiple users by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "userGuids": {
                                "type": "array",
                                "items": { "type": "string" },
                                "description": "Array of user GUIDs"
                            },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" }
                        },
                        "required": [ "tenantGuid", "userGuids" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("userGuids", out JsonElement guidsProp))
                        throw new ArgumentException("User GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    return ReadUsersByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers user methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("user_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return CreateUser(sdk, user);
            });

            server.RegisterLiteGraphMethod("user_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                return ReadUser(sdk, tenantGuid, userGuid);
            });

            server.RegisterLiteGraphMethod("user_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadUsers(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("user_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateUsers(sdk, query);
            });

            server.RegisterLiteGraphMethod("user_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return UpdateUser(sdk, user);
            });

            server.RegisterLiteGraphMethod("user_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                DeleteUser(sdk, tenantGuid, userGuid);
                return true;
            });

            server.RegisterLiteGraphMethod("user_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                return UserExists(sdk, tenantGuid, userGuid);
            });

            server.RegisterLiteGraphMethod("user_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("userGuids", out JsonElement guidsProp))
                    throw new ArgumentException("User GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadUsersByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers user methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("user_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return CreateUser(sdk, user);
            });

            server.RegisterLiteGraphMethod("user_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                return ReadUser(sdk, tenantGuid, userGuid);
            });

            server.RegisterLiteGraphMethod("user_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadUsers(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("user_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateUsers(sdk, query);
            });

            server.RegisterLiteGraphMethod("user_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return UpdateUser(sdk, user);
            });

            server.RegisterLiteGraphMethod("user_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                DeleteUser(sdk, tenantGuid, userGuid);
                return true;
            });

            server.RegisterLiteGraphMethod("user_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                return UserExists(sdk, tenantGuid, userGuid);
            });

            server.RegisterLiteGraphMethod("user_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("userGuids", out JsonElement guidsProp))
                    throw new ArgumentException("User GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadUsersByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateUser(LiteGraphSdk sdk, UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            string body = Serializer.SerializeJson(user, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(user.TenantGUID)
                + "/users",
                body);
        }

        private static string ReadUser(LiteGraphSdk sdk, Guid tenantGuid, Guid userGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/users/"
                + LiteGraphMcpRestProxy.Escape(userGuid));
        }

        private static string ReadUsers(LiteGraphSdk sdk, Guid tenantGuid, EnumerationOrderEnum order, int skip, int maxResults, Guid? continuationToken)
        {
            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/users?order="
                + LiteGraphMcpRestProxy.Escape(order.ToString())
                + "&skip="
                + skip
                + "&max-keys="
                + maxResults;

            if (continuationToken != null) url += "&token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value);

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string EnumerateUsers(LiteGraphSdk sdk, EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentException("query.TenantGUID is required.");

            string body = Serializer.SerializeJson(query, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                "/v2.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                + "/users",
                body);
        }

        private static string UserExists(LiteGraphSdk sdk, Guid tenantGuid, Guid userGuid)
        {
            bool exists = LiteGraphMcpRestProxy.HeadExists(
                sdk,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/users/"
                + LiteGraphMcpRestProxy.Escape(userGuid));

            return exists.ToString().ToLowerInvariant();
        }

        private static string ReadUsersByGuids(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> guids, int maxResults)
        {
            if (guids == null) throw new ArgumentNullException(nameof(guids));
            if (guids.Count == 0) throw new ArgumentException("At least one user GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/users?guids="
                + String.Join(",", guids)
                + "&max-keys="
                + maxResults);
        }

        private static string UpdateUser(LiteGraphSdk sdk, UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            string body = Serializer.SerializeJson(user, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(user.TenantGUID)
                + "/users/"
                + LiteGraphMcpRestProxy.Escape(user.GUID),
                body);
        }

        private static void DeleteUser(LiteGraphSdk sdk, Guid tenantGuid, Guid userGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/users/"
                + LiteGraphMcpRestProxy.Escape(userGuid));
        }

        #endregion
    }
}
