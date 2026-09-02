namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

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
            server.RegisterTool(
                "user/create",
                "Creates a new user in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        user = new { type = "string", description = "User object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "user" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                        throw new ArgumentException("User JSON string is required");
                    string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                    UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                    return CreateUser(sdk, user);
                });

            server.RegisterTool(
                "user/get",
                "Reads a user by GUID",
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

                    return ReadUser(sdk, tenantGuid, userGuid);
                });

            server.RegisterTool(
                "user/all",
                "Lists all users in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadUsers(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "user/enumerate",
                "Enumerates users with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Enumeration request serialized as JSON string using Serializer" }
                    },
                    required = new[] { "query" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                        throw new ArgumentException("Enumeration query is required");

                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                    if (query.TenantGUID == null)
                        throw new ArgumentException("query.TenantGUID is required.");

                    return EnumerateUsers(sdk, query);
                });

            server.RegisterTool(
                "user/update",
                "Updates a user",
                new
                {
                    type = "object",
                    properties = new
                    {
                        user = new { type = "string", description = "User object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "user" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                        throw new ArgumentException("User JSON string is required");
                    string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                    UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                    return UpdateUser(sdk, user);
                });

            server.RegisterTool(
                "user/delete",
                "Deletes a user by GUID",
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
                    DeleteUser(sdk, tenantGuid, userGuid);
                    return true;
                });

            server.RegisterTool(
                "user/exists",
                "Checks if a user exists by GUID",
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
                    return UserExists(sdk, tenantGuid, userGuid);
                });

            server.RegisterTool(
                "user/getmany",
                "Reads multiple users by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        userGuids = new { type = "array", items = new { type = "string" }, description = "Array of user GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "userGuids" }
                },
                (args) =>
                {
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
            server.RegisterMethod("user/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return CreateUser(sdk, user);
            });

            server.RegisterMethod("user/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                return ReadUser(sdk, tenantGuid, userGuid);
            });

            server.RegisterMethod("user/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadUsers(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("user/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateUsers(sdk, query);
            });

            server.RegisterMethod("user/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return UpdateUser(sdk, user);
            });

            server.RegisterMethod("user/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                DeleteUser(sdk, tenantGuid, userGuid);
                return true;
            });

            server.RegisterMethod("user/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                return UserExists(sdk, tenantGuid, userGuid);
            });

            server.RegisterMethod("user/getmany", (args) =>
            {
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
            server.RegisterMethod("user/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return CreateUser(sdk, user);
            });

            server.RegisterMethod("user/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                return ReadUser(sdk, tenantGuid, userGuid);
            });

            server.RegisterMethod("user/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadUsers(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("user/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateUsers(sdk, query);
            });

            server.RegisterMethod("user/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                return UpdateUser(sdk, user);
            });

            server.RegisterMethod("user/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                DeleteUser(sdk, tenantGuid, userGuid);
                return true;
            });

            server.RegisterMethod("user/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                return UserExists(sdk, tenantGuid, userGuid);
            });

            server.RegisterMethod("user/getmany", (args) =>
            {
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
