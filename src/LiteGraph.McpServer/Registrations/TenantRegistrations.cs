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
            server.RegisterLiteGraphTool(
                "tenant_create",
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
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                        throw new ArgumentException("Tenant name is required");

                    string? name = nameProp.GetString();
                    TenantMetadata tenant = new TenantMetadata { Name = name };
                    return CreateTenant(sdk, tenant);
                });

            server.RegisterLiteGraphTool(
                "tenant_get",
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
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    return ReadTenant(sdk, tenantGuid);
                });

            server.RegisterLiteGraphTool(
                "tenant_all",
                "Lists all tenants. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" }
                    },
                    required = new string[] { }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    (EnumerationOrderEnum order, int skip) = args.HasValue 
                        ? LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value)
                        : (EnumerationOrderEnum.CreatedDescending, 0);
                    
                    return ReadTenants(sdk, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterLiteGraphTool(
                "tenant_update",
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
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                        throw new ArgumentException("Tenant JSON string is required");
                    string tenantJson = tenantProp.GetString() ?? throw new ArgumentException("Tenant JSON string cannot be null");
                    TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantJson);
                    return UpdateTenant(sdk, tenant);
                });

            server.RegisterLiteGraphTool(
                "tenant_delete",
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
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                    DeleteTenant(sdk, tenantGuid, force);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "tenant_enumerate",
                "Enumerates tenants with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Enumeration request serialized as JSON string using Serializer" }
                    },
                    required = new[] { "query" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                        throw new ArgumentException("Enumeration query is required");

                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();

                    return EnumerateTenants(sdk, query);
                });

            server.RegisterLiteGraphTool(
                "tenant_exists",
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
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    return TenantExists(sdk, tenantGuid);
                });

            server.RegisterLiteGraphTool(
                "tenant_statistics",
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
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                    return ReadTenantStatistics(sdk, tenantGuid);
                });

            server.RegisterLiteGraphTool(
                "tenant_statisticsall",
                "Gets statistics for all tenants",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    return ReadAllTenantStatistics(sdk);
                });

            server.RegisterLiteGraphTool(
                "tenant_getmany",
                "Reads multiple tenants by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuids = new { type = "array", items = new { type = "string" }, description = "Array of tenant GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuids" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Tenant GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    return ReadTenantsByGuids(sdk, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
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
            server.RegisterLiteGraphMethod("tenant_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant name is required");

                string? name = nameProp.GetString();
                TenantMetadata tenant = new TenantMetadata { Name = name };
                return CreateTenant(sdk, tenant);
            });

            server.RegisterLiteGraphMethod("tenant_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");

                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                return ReadTenant(sdk, tenantGuid);
            });

            server.RegisterLiteGraphMethod("tenant_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                (EnumerationOrderEnum order, int skip) = args.HasValue 
                    ? LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value)
                    : (EnumerationOrderEnum.CreatedDescending, 0);
                
                return ReadTenants(sdk, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("tenant_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                    throw new ArgumentException("Tenant JSON string is required");
                string tenantJson = tenantProp.GetString() ?? throw new ArgumentException("Tenant JSON string cannot be null");
                TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantJson);
                return UpdateTenant(sdk, tenant);
            });

            server.RegisterLiteGraphMethod("tenant_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                DeleteTenant(sdk, tenantGuid, force);
                return true;
            });

            server.RegisterLiteGraphMethod("tenant_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();

                return EnumerateTenants(sdk, query);
            });

            server.RegisterLiteGraphMethod("tenant_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                return TenantExists(sdk, tenantGuid);
            });

            server.RegisterLiteGraphMethod("tenant_statistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                return ReadTenantStatistics(sdk, tenantGuid);
            });

            server.RegisterLiteGraphMethod("tenant_statisticsall", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return ReadAllTenantStatistics(sdk);
            });

            server.RegisterLiteGraphMethod("tenant_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tenant GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadTenantsByGuids(sdk, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
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
            server.RegisterLiteGraphMethod("tenant_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant name is required");

                string? name = nameProp.GetString();
                TenantMetadata tenant = new TenantMetadata { Name = name };
                return CreateTenant(sdk, tenant);
            });

            server.RegisterLiteGraphMethod("tenant_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");

                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                return ReadTenant(sdk, tenantGuid);
            });

            server.RegisterLiteGraphMethod("tenant_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                (EnumerationOrderEnum order, int skip) = args.HasValue 
                    ? LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value)
                    : (EnumerationOrderEnum.CreatedDescending, 0);
                
                return ReadTenants(sdk, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("tenant_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                    throw new ArgumentException("Tenant JSON string is required");
                string tenantJson = tenantProp.GetString() ?? throw new ArgumentException("Tenant JSON string cannot be null");
                TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantJson);
                return UpdateTenant(sdk, tenant);
            });

            server.RegisterLiteGraphMethod("tenant_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                DeleteTenant(sdk, tenantGuid, force);
                return true;
            });

            server.RegisterLiteGraphMethod("tenant_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();

                return EnumerateTenants(sdk, query);
            });

            server.RegisterLiteGraphMethod("tenant_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                return TenantExists(sdk, tenantGuid);
            });

            server.RegisterLiteGraphMethod("tenant_statistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(guidProp.GetString()!);
                return ReadTenantStatistics(sdk, tenantGuid);
            });

            server.RegisterLiteGraphMethod("tenant_statisticsall", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return ReadAllTenantStatistics(sdk);
            });

            server.RegisterLiteGraphMethod("tenant_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tenant GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadTenantsByGuids(sdk, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateTenant(LiteGraphSdk sdk, TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            string body = Serializer.SerializeJson(tenant, false);
            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Put, "/v1.0/tenants", body);
        }

        private static string ReadTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/" + LiteGraphMcpRestProxy.Escape(tenantGuid));
        }

        private static string ReadTenants(LiteGraphSdk sdk, EnumerationOrderEnum order, int skip, int maxResults, Guid? continuationToken)
        {
            string url = "/v1.0/tenants?order="
                + LiteGraphMcpRestProxy.Escape(order.ToString())
                + "&skip="
                + skip
                + "&max-keys="
                + maxResults;

            if (continuationToken != null) url += "&token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value);

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string EnumerateTenants(LiteGraphSdk sdk, EnumerationRequest query)
        {
            string body = Serializer.SerializeJson(query ?? new EnumerationRequest(), false);
            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Post, "/v2.0/tenants", body);
        }

        private static string TenantExists(LiteGraphSdk sdk, Guid tenantGuid)
        {
            bool exists = LiteGraphMcpRestProxy.HeadExists(
                sdk,
                "/v1.0/tenants/" + LiteGraphMcpRestProxy.Escape(tenantGuid));

            return exists.ToString().ToLowerInvariant();
        }

        private static string ReadTenantStatistics(LiteGraphSdk sdk, Guid tenantGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/" + LiteGraphMcpRestProxy.Escape(tenantGuid) + "/stats");
        }

        private static string ReadAllTenantStatistics(LiteGraphSdk sdk)
        {
            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, "/v1.0/tenants/stats");
        }

        private static string ReadTenantsByGuids(LiteGraphSdk sdk, List<Guid> guids, int maxResults)
        {
            if (guids == null) throw new ArgumentNullException(nameof(guids));
            if (guids.Count == 0) throw new ArgumentException("At least one tenant GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants?guids="
                + String.Join(",", guids)
                + "&max-keys="
                + maxResults);
        }

        private static string UpdateTenant(LiteGraphSdk sdk, TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            string body = Serializer.SerializeJson(tenant, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/" + LiteGraphMcpRestProxy.Escape(tenant.GUID),
                body);
        }

        private static void DeleteTenant(LiteGraphSdk sdk, Guid tenantGuid, bool force)
        {
            string path = "/v1.0/tenants/" + LiteGraphMcpRestProxy.Escape(tenantGuid);
            if (force) path += "?force";
            LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Delete, path);
        }

        #endregion
    }
}
