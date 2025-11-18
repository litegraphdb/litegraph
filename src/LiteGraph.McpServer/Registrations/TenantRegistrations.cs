namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph;
    using LiteGraph.Serialization;
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
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphClient client, Serializer serializer)
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

                    string name = nameProp.GetString();
                    TenantMetadata tenant = new TenantMetadata { Name = name };
                    TenantMetadata created = client.Tenant.Create(tenant).GetAwaiter().GetResult();
                    return serializer.SerializeJson(created, true);
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

                    Guid tenantGuid = Guid.Parse(guidProp.GetString());
                    TenantMetadata tenant = client.Tenant.ReadByGuid(tenantGuid).GetAwaiter().GetResult();
                    return tenant != null ? serializer.SerializeJson(tenant, true) : "null";
                });

            server.RegisterTool(
                "tenant/all",
                "Lists all tenants",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (args) =>
                {
                    var tenants = LiteGraphMcpServerHelpers.ToListSync(client.Tenant.ReadMany());
                    return serializer.SerializeJson(tenants, true);
                });

            server.RegisterTool(
                "tenant/update",
                "Updates a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenant = new { type = "object", description = "Tenant object with GUID and updated properties" }
                    },
                    required = new[] { "tenant" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                        throw new ArgumentException("Tenant object is required");
                    
                    TenantMetadata tenant = serializer.DeserializeJson<TenantMetadata>(tenantProp.GetRawText());
                    TenantMetadata updated = client.Tenant.Update(tenant).GetAwaiter().GetResult();
                    return serializer.SerializeJson(updated, true);
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
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString());
                    bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                    client.Tenant.DeleteByGuid(tenantGuid, force).GetAwaiter().GetResult();
                    return "{\"success\": true}";
                });

            server.RegisterTool(
                "tenant/enumerate",
                "Enumerates tenants with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "object", description = "Enumeration query with pagination options" }
                    },
                    required = new string[] { }
                },
                (args) =>
                {
                    EnumerationRequest query = null;
                    if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                        query = serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                    
                    EnumerationResult<TenantMetadata> result = client.Tenant.Enumerate(query).GetAwaiter().GetResult();
                    return serializer.SerializeJson(result, true);
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
                    
                    Guid tenantGuid = Guid.Parse(guidProp.GetString());
                    bool exists = client.Tenant.ExistsByGuid(tenantGuid).GetAwaiter().GetResult();
                    return $"{{\"exists\": {exists.ToString().ToLower()}}}";
                });

            server.RegisterTool(
                "tenant/statistics",
                "Gets statistics for a tenant or all tenants",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID (optional, if not provided returns all tenant statistics)" }
                    },
                    required = new string[] { }
                },
                (args) =>
                {
                    if (args.HasValue && args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    {
                        Guid tenantGuid = Guid.Parse(guidProp.GetString());
                        TenantStatistics stats = client.Tenant.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                        return serializer.SerializeJson(stats, true);
                    }
                    else
                    {
                        Dictionary<Guid, TenantStatistics> allStats = client.Tenant.GetStatistics().GetAwaiter().GetResult();
                        return serializer.SerializeJson(allStats, true);
                    }
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers tenant methods on TCP server.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphClient client, Serializer serializer)
        {
            server.RegisterMethod("tenant/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant name is required");

                string name = nameProp.GetString();
                TenantMetadata tenant = new TenantMetadata { Name = name };
                TenantMetadata created = client.Tenant.Create(tenant).GetAwaiter().GetResult();
                return serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tenant/get", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");

                Guid tenantGuid = Guid.Parse(guidProp.GetString());
                TenantMetadata tenant = client.Tenant.ReadByGuid(tenantGuid).GetAwaiter().GetResult();
                return tenant != null ? serializer.SerializeJson(tenant, true) : "null";
            });

            server.RegisterMethod("tenant/all", (args) =>
            {
                var tenants = LiteGraphMcpServerHelpers.ToListSync(client.Tenant.ReadMany());
                return serializer.SerializeJson(tenants, true);
            });

            server.RegisterMethod("tenant/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                    throw new ArgumentException("Tenant object is required");
                TenantMetadata tenant = serializer.DeserializeJson<TenantMetadata>(tenantProp.GetRawText());
                TenantMetadata updated = client.Tenant.Update(tenant).GetAwaiter().GetResult();
                return serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("tenant/delete", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString());
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                client.Tenant.DeleteByGuid(tenantGuid, force).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("tenant/enumerate", (args) =>
            {
                EnumerationRequest query = null;
                if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                    query = serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                EnumerationResult<TenantMetadata> result = client.Tenant.Enumerate(query).GetAwaiter().GetResult();
                return serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("tenant/exists", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString());
                bool exists = client.Tenant.ExistsByGuid(tenantGuid).GetAwaiter().GetResult();
                return $"{{\"exists\": {exists.ToString().ToLower()}}}";
            });

            server.RegisterMethod("tenant/statistics", (args) =>
            {
                if (args.HasValue && args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                {
                    Guid tenantGuid = Guid.Parse(guidProp.GetString());
                    TenantStatistics stats = client.Tenant.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return serializer.SerializeJson(stats, true);
                }
                else
                {
                    Dictionary<Guid, TenantStatistics> allStats = client.Tenant.GetStatistics().GetAwaiter().GetResult();
                    return serializer.SerializeJson(allStats, true);
                }
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers tenant methods on WebSocket server.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphClient client, Serializer serializer)
        {
            server.RegisterMethod("tenant/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant name is required");

                string name = nameProp.GetString();
                TenantMetadata tenant = new TenantMetadata { Name = name };
                TenantMetadata created = client.Tenant.Create(tenant).GetAwaiter().GetResult();
                return serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tenant/get", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");

                Guid tenantGuid = Guid.Parse(guidProp.GetString());
                TenantMetadata tenant = client.Tenant.ReadByGuid(tenantGuid).GetAwaiter().GetResult();
                return tenant != null ? serializer.SerializeJson(tenant, true) : "null";
            });

            server.RegisterMethod("tenant/all", (args) =>
            {
                var tenants = LiteGraphMcpServerHelpers.ToListSync(client.Tenant.ReadMany());
                return serializer.SerializeJson(tenants, true);
            });

            server.RegisterMethod("tenant/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenant", out JsonElement tenantProp))
                    throw new ArgumentException("Tenant object is required");
                TenantMetadata tenant = serializer.DeserializeJson<TenantMetadata>(tenantProp.GetRawText());
                TenantMetadata updated = client.Tenant.Update(tenant).GetAwaiter().GetResult();
                return serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("tenant/delete", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString());
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                client.Tenant.DeleteByGuid(tenantGuid, force).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("tenant/enumerate", (args) =>
            {
                EnumerationRequest query = null;
                if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                    query = serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                EnumerationResult<TenantMetadata> result = client.Tenant.Enumerate(query).GetAwaiter().GetResult();
                return serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("tenant/exists", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(guidProp.GetString());
                bool exists = client.Tenant.ExistsByGuid(tenantGuid).GetAwaiter().GetResult();
                return $"{{\"exists\": {exists.ToString().ToLower()}}}";
            });

            server.RegisterMethod("tenant/statistics", (args) =>
            {
                if (args.HasValue && args.Value.TryGetProperty("tenantGuid", out JsonElement guidProp))
                {
                    Guid tenantGuid = Guid.Parse(guidProp.GetString());
                    TenantStatistics stats = client.Tenant.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return serializer.SerializeJson(stats, true);
                }
                else
                {
                    Dictionary<Guid, TenantStatistics> allStats = client.Tenant.GetStatistics().GetAwaiter().GetResult();
                    return serializer.SerializeJson(allStats, true);
                }
            });
        }

        #endregion
    }
}

