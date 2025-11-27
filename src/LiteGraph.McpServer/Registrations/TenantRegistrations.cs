namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for Tenant operations.
    /// </summary>
    public static class TenantRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers tenant tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "tenant/create",
                "Creates a new tenant in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Tenant name" }
                    },
                    required = new[] { "name" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                        throw new ArgumentException("Tenant name is required");

                    string? name = nameProp.GetString();
                    TenantMetadata tenant = new TenantMetadata { Name = name };
                    TenantMetadata created = sdk.Tenant.Create(tenant).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "tenant/get",
                "Reads a tenant by GUID",
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
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    TenantMetadata tenant = sdk.Tenant.ReadByGuid(tenantGuid).GetAwaiter().GetResult();
                    return tenant != null ? Serializer.SerializeJson(tenant, true) : "null";
                });

            server.RegisterTool(
                "tenant/all",
                "Lists all tenants",
                new
                {
                    type = "object",
                    properties = new
                    {
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
                    },
                    required = new string[] { }
                },
                (args) =>
                {
                    (EnumerationOrderEnum order, int skip) = args.HasValue 
                        ? LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value)
                        : (EnumerationOrderEnum.CreatedDescending, 0);
                    
                    List<TenantMetadata> tenants = sdk.Tenant.ReadMany(order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(tenants, true);
                });

            server.RegisterTool(
                "tenant/update",
                "Updates a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenant = new { type = "string", description = "Tenant object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenant" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                        throw new ArgumentException("Tenant JSON string is required");
                    string tenantJson = tenantProp.GetString() ?? throw new ArgumentException("Tenant JSON string cannot be null");
                    TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantJson);
                    TenantMetadata updated = sdk.Tenant.Update(tenant).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
                });

            server.RegisterTool(
                "tenant/delete",
                "Deletes a tenant by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        force = new { type = "boolean", description = "Force deletion (default: false)" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                    sdk.Tenant.DeleteByGuid(tenantGuid, force).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "tenant/enumerate",
                "Enumerates tenants with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Enumeration query object serialized as JSON string using Serializer" }
                    },
                    required = new string[] { }
                },
                (args) =>
                {
                    EnumerationRequest query = new EnumerationRequest();
                    if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                    {
                        string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                        query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                    }
                    
                    EnumerationResult<TenantMetadata> result = sdk.Tenant.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "tenant/exists",
                "Checks if a tenant exists by GUID",
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
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    bool exists = sdk.Tenant.ExistsByGuid(tenantGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "tenant/statistics",
                "Gets statistics for a specific tenant",
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
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    TenantStatistics stats = sdk.Tenant.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(stats, true);
                });

            server.RegisterTool(
                "tenant/statisticsall",
                "Gets statistics for all tenants",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (args) =>
                {
                    Dictionary<Guid, TenantStatistics> allStats = sdk.Tenant.GetStatistics().GetAwaiter().GetResult();
                    return Serializer.SerializeJson(allStats, true);
                });

            server.RegisterTool(
                "tenant/getmany",
                "Reads multiple tenants by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuids = new { type = "array", items = new { type = "string" }, description = "Array of tenant GUIDs" }
                    },
                    required = new[] { "tenantGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Tenant GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    List<TenantMetadata> tenants = sdk.Tenant.ReadByGuids(guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(tenants, true);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers tenant methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("tenant/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant name is required");

                string? name = nameProp.GetString();
                TenantMetadata tenant = new TenantMetadata { Name = name };
                TenantMetadata created = sdk.Tenant.Create(tenant).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tenant/get", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");

                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                TenantMetadata tenant = sdk.Tenant.ReadByGuid(tenantGuid).GetAwaiter().GetResult();
                return tenant != null ? Serializer.SerializeJson(tenant, true) : "null";
            });

            server.RegisterMethod("tenant/all", (args) =>
            {
                (EnumerationOrderEnum order, int skip) = args.HasValue 
                    ? LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value)
                    : (EnumerationOrderEnum.CreatedDescending, 0);
                
                List<TenantMetadata> tenants = sdk.Tenant.ReadMany(order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tenants, true);
            });

            server.RegisterMethod("tenant/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                    throw new ArgumentException("Tenant JSON string is required");
                string tenantJson = tenantProp.GetString() ?? throw new ArgumentException("Tenant JSON string cannot be null");
                TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantJson);
                TenantMetadata updated = sdk.Tenant.Update(tenant).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("tenant/delete", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                sdk.Tenant.DeleteByGuid(tenantGuid, force).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("tenant/enumerate", (args) =>
            {
                EnumerationRequest? query = null;
                if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    query = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                }
                EnumerationResult<TenantMetadata> result = sdk.Tenant.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("tenant/exists", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                bool exists = sdk.Tenant.ExistsByGuid(tenantGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("tenant/statistics", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                TenantStatistics stats = sdk.Tenant.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });

            server.RegisterMethod("tenant/statisticsall", (args) =>
            {
                Dictionary<Guid, TenantStatistics> allStats = sdk.Tenant.GetStatistics().GetAwaiter().GetResult();
                return Serializer.SerializeJson(allStats, true);
            });

            server.RegisterMethod("tenant/getmany", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tenant GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<TenantMetadata> tenants = sdk.Tenant.ReadByGuids(guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tenants, true);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers tenant methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("tenant/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant name is required");

                string? name = nameProp.GetString();
                TenantMetadata tenant = new TenantMetadata { Name = name };
                TenantMetadata created = sdk.Tenant.Create(tenant).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tenant/get", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");

                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                TenantMetadata tenant = sdk.Tenant.ReadByGuid(tenantGuid).GetAwaiter().GetResult();
                return tenant != null ? Serializer.SerializeJson(tenant, true) : "null";
            });

            server.RegisterMethod("tenant/all", (args) =>
            {
                (EnumerationOrderEnum order, int skip) = args.HasValue 
                    ? LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value)
                    : (EnumerationOrderEnum.CreatedDescending, 0);
                
                List<TenantMetadata> tenants = sdk.Tenant.ReadMany(order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tenants, true);
            });

            server.RegisterMethod("tenant/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                    throw new ArgumentException("Tenant JSON string is required");
                string tenantJson = tenantProp.GetString() ?? throw new ArgumentException("Tenant JSON string cannot be null");
                TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantJson);
                TenantMetadata updated = sdk.Tenant.Update(tenant).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("tenant/delete", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                sdk.Tenant.DeleteByGuid(tenantGuid, force).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("tenant/enumerate", (args) =>
            {
                EnumerationRequest query = new EnumerationRequest();
                if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                }
                EnumerationResult<TenantMetadata> result = sdk.Tenant.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("tenant/exists", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                bool exists = sdk.Tenant.ExistsByGuid(tenantGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("tenant/statistics", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                TenantStatistics stats = sdk.Tenant.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });

            server.RegisterMethod("tenant/statisticsall", (args) =>
            {
                Dictionary<Guid, TenantStatistics> allStats = sdk.Tenant.GetStatistics().GetAwaiter().GetResult();
                return Serializer.SerializeJson(allStats, true);
            });

            server.RegisterMethod("tenant/getmany", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tenant GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<TenantMetadata> tenants = sdk.Tenant.ReadByGuids(guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tenants, true);
            });
        }

        #endregion
    }
}

