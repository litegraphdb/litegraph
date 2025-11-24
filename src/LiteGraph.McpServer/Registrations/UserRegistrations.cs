namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
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
                    UserMaster created = sdk.User.Create(user).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
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

                    UserMaster user = sdk.User.ReadByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                    return user != null ? Serializer.SerializeJson(user, true) : "null";
                });

            server.RegisterTool(
                "user/all",
                "Lists all users in a tenant",
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
                    List<UserMaster> users = sdk.User.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(users, true);
                });

            server.RegisterTool(
                "user/enumerate",
                "Enumerates users with pagination and filtering",
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
                    
                    EnumerationResult<UserMaster> result = sdk.User.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
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
                    UserMaster updated = sdk.User.Update(user).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
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
                    sdk.User.DeleteByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                    return string.Empty;
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
                    bool exists = sdk.User.ExistsByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "user/getmany",
                "Reads multiple users by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        userGuids = new { type = "array", items = new { type = "string" }, description = "Array of user GUIDs" }
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
                    List<UserMaster> users = sdk.User.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(users, true);
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
                UserMaster created = sdk.User.Create(user).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("user/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                UserMaster user = sdk.User.ReadByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                return user != null ? Serializer.SerializeJson(user, true) : "null";
            });

            server.RegisterMethod("user/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<UserMaster> users = sdk.User.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(users, true);
            });

            server.RegisterMethod("user/enumerate", (args) =>
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
                
                EnumerationResult<UserMaster> result = sdk.User.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("user/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                UserMaster updated = sdk.User.Update(user).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("user/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                sdk.User.DeleteByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("user/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                bool exists = sdk.User.ExistsByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("user/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("userGuids", out JsonElement guidsProp))
                    throw new ArgumentException("User GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<UserMaster> users = sdk.User.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(users, true);
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
                UserMaster created = sdk.User.Create(user).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("user/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");

                UserMaster user = sdk.User.ReadByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                return user != null ? Serializer.SerializeJson(user, true) : "null";
            });

            server.RegisterMethod("user/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<UserMaster> users = sdk.User.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(users, true);
            });

            server.RegisterMethod("user/enumerate", (args) =>
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
                
                EnumerationResult<UserMaster> result = sdk.User.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("user/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("user", out JsonElement userProp))
                    throw new ArgumentException("User JSON string is required");
                string userJson = userProp.GetString() ?? throw new ArgumentException("User JSON string cannot be null");
                UserMaster user = Serializer.DeserializeJson<UserMaster>(userJson);
                UserMaster updated = sdk.User.Update(user).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("user/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                sdk.User.DeleteByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("user/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                bool exists = sdk.User.ExistsByGuid(tenantGuid, userGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("user/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("userGuids", out JsonElement guidsProp))
                    throw new ArgumentException("User GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<UserMaster> users = sdk.User.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(users, true);
            });
        }

        #endregion
    }
}

