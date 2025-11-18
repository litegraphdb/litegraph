namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph;
    using LiteGraph.Serialization;
    using Voltaic;

    /// <summary>
    /// Registration methods for Graph operations.
    /// </summary>
    public static class GraphRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers graph tools on HTTP server.
        /// </summary>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphClient client, Serializer serializer)
        {
            server.RegisterTool(
                "graph/create",
                "Creates a new graph in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        name = new { type = "string", description = "Graph name" }
                    },
                    required = new[] { "tenantGuid", "name" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("name", out JsonElement nameProp))
                        throw new ArgumentException("Tenant GUID and name are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    string? name = nameProp.GetString();
                    Graph graph = new Graph { TenantGUID = tenantGuid, Name = name };
                    Graph created = client.Graph.Create(graph).GetAwaiter().GetResult();
                    return serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "graph/get",
                "Reads a graph by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        includeData = new { type = "boolean", description = "Include graph data" },
                        includeSubordinates = new { type = "boolean", description = "Include labels, tags, vectors" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    bool includeData = args.Value.TryGetProperty("includeData", out JsonElement includeDataProp) && includeDataProp.GetBoolean();
                    bool includeSubordinates = args.Value.TryGetProperty("includeSubordinates", out JsonElement includeSubProp) && includeSubProp.GetBoolean();

                    Graph graph = client.Graph.ReadByGuid(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return graph != null ? serializer.SerializeJson(graph, true) : "null";
                });

            server.RegisterTool(
                "graph/all",
                "Lists all graphs in a tenant",
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
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    var graphs = LiteGraphMcpServerHelpers.ToListSync(client.Graph.ReadAllInTenant(tenantGuid));
                    return serializer.SerializeJson(graphs, true);
                });

            server.RegisterTool(
                "graph/enumerate",
                "Enumerates graphs with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "object", description = "Enumeration query with pagination options" }
                    },
                    required = new[] { "query" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                        throw new ArgumentException("Query is required");
                    EnumerationRequest query = serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                    EnumerationResult<Graph> result = client.Graph.Enumerate(query).GetAwaiter().GetResult();
                    return serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "graph/update",
                "Updates a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        graph = new { type = "object", description = "Graph object with GUID and updated properties" }
                    },
                    required = new[] { "graph" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                        throw new ArgumentException("Graph object is required");
                    Graph graph = serializer.DeserializeJson<Graph>(graphProp.GetRawText());
                    Graph updated = client.Graph.Update(graph).GetAwaiter().GetResult();
                    return serializer.SerializeJson(updated, true);
                });

            server.RegisterTool(
                "graph/delete",
                "Deletes a graph by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        force = new { type = "boolean", description = "Force deletion (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                    client.Graph.DeleteByGuid(tenantGuid, graphGuid, force).GetAwaiter().GetResult();
                    return "{\"success\": true}";
                });

            server.RegisterTool(
                "graph/getSubgraph",
                "Retrieves a subgraph starting from a specific node, traversing up to a specified depth. Useful for graph exploration and traversal.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Starting node GUID" },
                        maxDepth = new { type = "integer", description = "Maximum depth to traverse (default: 2)" },
                        maxNodes = new { type = "integer", description = "Maximum number of nodes (0 = unlimited)" },
                        maxEdges = new { type = "integer", description = "Maximum number of edges (0 = unlimited)" },
                        includeData = new { type = "boolean", description = "Include node/edge data" },
                        includeSubordinates = new { type = "boolean", description = "Include labels, tags, vectors" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    int maxDepth = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxDepth", 2);
                    int maxNodes = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxNodes", 0);
                    int maxEdges = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxEdges", 0);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    SearchResult result = client.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "graph/exists",
                "Checks if a graph exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    bool exists = client.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return $"{{\"exists\": {exists.ToString().ToLower()}}}";
                });

            server.RegisterTool(
                "graph/statistics",
                "Gets statistics for a graph or all graphs in a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID (optional, if not provided returns all graph statistics)" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    {
                        Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                        GraphStatistics stats = client.Graph.GetStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                        return serializer.SerializeJson(stats, true);
                    }
                    else
                    {
                        Dictionary<Guid, GraphStatistics> allStats = client.Graph.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                        return serializer.SerializeJson(allStats, true);
                    }
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers graph methods on TCP server.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphClient client, Serializer serializer)
        {
            server.RegisterMethod("graph/create", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                string? name = nameProp.GetString();
                Graph graph = new Graph { TenantGUID = tenantGuid, Name = name };
                Graph created = client.Graph.Create(graph).GetAwaiter().GetResult();
                return serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("graph/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool includeData = args.Value.TryGetProperty("includeData", out JsonElement includeDataProp) && includeDataProp.GetBoolean();
                bool includeSubordinates = args.Value.TryGetProperty("includeSubordinates", out JsonElement includeSubProp) && includeSubProp.GetBoolean();

                Graph graph = client.Graph.ReadByGuid(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                return graph != null ? serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterMethod("graph/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                var graphs = LiteGraphMcpServerHelpers.ToListSync(client.Graph.ReadAllInTenant(tenantGuid));
                return serializer.SerializeJson(graphs, true);
            });

            server.RegisterMethod("graph/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Query is required");
                EnumerationRequest query = serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                EnumerationResult<Graph> result = client.Graph.Enumerate(query).GetAwaiter().GetResult();
                return serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                    throw new ArgumentException("Graph object is required");
                Graph graph = serializer.DeserializeJson<Graph>(graphProp.GetRawText());
                Graph updated = client.Graph.Update(graph).GetAwaiter().GetResult();
                return serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("graph/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                client.Graph.DeleteByGuid(tenantGuid, graphGuid, force).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("graph/getSubgraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                int maxDepth = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxDepth", 2);
                int maxNodes = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxNodes", 0);
                int maxEdges = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxEdges", 0);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                SearchResult result = client.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates).GetAwaiter().GetResult();
                return serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool exists = client.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return $"{{\"exists\": {exists.ToString().ToLower()}}}";
            });

            server.RegisterMethod("graph/statistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                {
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    GraphStatistics stats = client.Graph.GetStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return serializer.SerializeJson(stats, true);
                }
                else
                {
                    Dictionary<Guid, GraphStatistics> allStats = client.Graph.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return serializer.SerializeJson(allStats, true);
                }
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers graph methods on WebSocket server.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphClient client, Serializer serializer)
        {
            server.RegisterMethod("graph/create", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                string? name = nameProp.GetString();
                Graph graph = new Graph { TenantGUID = tenantGuid, Name = name };
                Graph created = client.Graph.Create(graph).GetAwaiter().GetResult();
                return serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("graph/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool includeData = args.Value.TryGetProperty("includeData", out JsonElement includeDataProp) && includeDataProp.GetBoolean();
                bool includeSubordinates = args.Value.TryGetProperty("includeSubordinates", out JsonElement includeSubProp) && includeSubProp.GetBoolean();

                Graph graph = client.Graph.ReadByGuid(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                return graph != null ? serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterMethod("graph/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                var graphs = LiteGraphMcpServerHelpers.ToListSync(client.Graph.ReadAllInTenant(tenantGuid));
                return serializer.SerializeJson(graphs, true);
            });

            server.RegisterMethod("graph/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Query is required");
                EnumerationRequest query = serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                EnumerationResult<Graph> result = client.Graph.Enumerate(query).GetAwaiter().GetResult();
                return serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                    throw new ArgumentException("Graph object is required");
                Graph graph = serializer.DeserializeJson<Graph>(graphProp.GetRawText());
                Graph updated = client.Graph.Update(graph).GetAwaiter().GetResult();
                return serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("graph/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                client.Graph.DeleteByGuid(tenantGuid, graphGuid, force).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("graph/getSubgraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                int maxDepth = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxDepth", 2);
                int maxNodes = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxNodes", 0);
                int maxEdges = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxEdges", 0);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                SearchResult result = client.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates).GetAwaiter().GetResult();
                return serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool exists = client.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return $"{{\"exists\": {exists.ToString().ToLower()}}}";
            });

            server.RegisterMethod("graph/statistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                {
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    GraphStatistics stats = client.Graph.GetStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return serializer.SerializeJson(stats, true);
                }
                else
                {
                    Dictionary<Guid, GraphStatistics> allStats = client.Graph.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return serializer.SerializeJson(allStats, true);
                }
            });
        }

        #endregion
    }
}

