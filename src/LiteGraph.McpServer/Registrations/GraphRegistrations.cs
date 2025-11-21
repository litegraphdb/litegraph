namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
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
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
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
                    Graph created = sdk.Graph.Create(graph).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
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

                    Graph graph = sdk.Graph.ReadByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return graph != null ? Serializer.SerializeJson(graph, true) : "null";
                });

            server.RegisterTool(
                "graph/all",
                "Lists all graphs in a tenant",
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
                    List<Graph> graphs = sdk.Graph.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(graphs, true);
                });

            server.RegisterTool(
                "graph/enumerate",
                "Enumerates graphs with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        query = new { type = "object", description = "Enumeration query with pagination options" }
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
                        EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                        if (deserializedQuery != null)
                        {
                            query.MaxResults = deserializedQuery.MaxResults;
                            query.Ordering = deserializedQuery.Ordering;
                            query.ContinuationToken = deserializedQuery.ContinuationToken;
                            query.IncludeData = deserializedQuery.IncludeData;
                            query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                        }
                    }
                    
                    EnumerationResult<Graph> result = sdk.Graph.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
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
                    Graph graph = Serializer.DeserializeJson<Graph>(graphProp.GetRawText());
                    Graph updated = sdk.Graph.Update(graph).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
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
                    sdk.Graph.DeleteByGuid(tenantGuid, graphGuid, force).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "graph/getsubgraph",
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
                    SearchResult result = sdk.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "graph/getsubgraphstatistics",
                "Gets statistics for a subgraph starting from a specific node, traversing up to a specified depth",
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
                        maxEdges = new { type = "integer", description = "Maximum number of edges (0 = unlimited)" }
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
                    GraphStatistics stats = sdk.Graph.GetSubgraphStatistics(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(stats, true);
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
                    bool exists = sdk.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
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
                        GraphStatistics stats = sdk.Graph.GetStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                        return Serializer.SerializeJson(stats, true);
                    }
                    else
                    {
                        Dictionary<Guid, GraphStatistics> allStats = sdk.Graph.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                        return Serializer.SerializeJson(allStats, true);
                    }
                });

            server.RegisterTool(
                "graph/getmany",
                "Reads multiple graphs by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuids = new { type = "array", items = new { type = "string" }, description = "Array of graph GUIDs" }
                    },
                    required = new[] { "tenantGuid", "graphGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("graphGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Graph GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    List<Graph> graphs = sdk.Graph.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(graphs, true);
                });

            server.RegisterTool(
                "graph/search",
                "Searches graphs with filters",
                new
                {
                    type = "object",
                    properties = new
                    {
                        searchRequest = new { type = "object", description = "Search request object" }
                    },
                    required = new[] { "searchRequest" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                        throw new ArgumentException("Search request is required");
                    
                    SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqProp.GetRawText());
                    SearchResult result = sdk.Graph.Search(req).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "graph/readfirst",
                "Reads the first graph matching search criteria",
                new
                {
                    type = "object",
                    properties = new
                    {
                        searchRequest = new { type = "object", description = "Search request object" }
                    },
                    required = new[] { "searchRequest" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                        throw new ArgumentException("Search request is required");
                    
                    SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqProp.GetRawText());
                    Graph graph = sdk.Graph.ReadFirst(req).GetAwaiter().GetResult();
                    return graph != null ? Serializer.SerializeJson(graph, true) : "null";
                });

            server.RegisterTool(
                "graph/enablevectorindexing",
                "Enables vector indexing for a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        config = new { type = "object", description = "Vector index configuration" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "config" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("config", out JsonElement configProp))
                        throw new ArgumentException("Vector index configuration is required");
                    
                    VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(configProp.GetRawText());
                    sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "graph/rebuildvectorindex",
                "Rebuilds the vector index for a graph",
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
                    sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "graph/deletevectorindex",
                "Deletes the vector index for a graph",
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
                    sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "graph/getvectorindexconfig",
                "Reads the vector index configuration for a graph",
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
                    VectorIndexConfiguration config = sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(config, true);
                });

            server.RegisterTool(
                "graph/getvectorindexstatistics",
                "Gets vector index statistics for a graph",
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
                    VectorIndexStatistics stats = sdk.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(stats, true);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers graph methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
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
                Graph created = sdk.Graph.Create(graph).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
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

                Graph graph = sdk.Graph.ReadByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return graph != null ? Serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterMethod("graph/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Graph> graphs = sdk.Graph.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(graphs, true);
            });

            server.RegisterMethod("graph/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<Graph> result = sdk.Graph.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                    throw new ArgumentException("Graph object is required");
                Graph graph = Serializer.DeserializeJson<Graph>(graphProp.GetRawText());
                Graph updated = sdk.Graph.Update(graph).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
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
                sdk.Graph.DeleteByGuid(tenantGuid, graphGuid, force).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("graph/getsubgraph", (args) =>
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
                SearchResult result = sdk.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/getsubgraphstatistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                int maxDepth = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxDepth", 2);
                int maxNodes = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxNodes", 0);
                int maxEdges = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxEdges", 0);
                GraphStatistics stats = sdk.Graph.GetSubgraphStatistics(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });

            server.RegisterMethod("graph/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool exists = sdk.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("graph/statistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                {
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    GraphStatistics stats = sdk.Graph.GetStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(stats, true);
                }
                else
                {
                    Dictionary<Guid, GraphStatistics> allStats = sdk.Graph.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(allStats, true);
                }
            });

            server.RegisterMethod("graph/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("graphGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Graph GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Graph> graphs = sdk.Graph.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(graphs, true);
            });

            server.RegisterMethod("graph/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqProp.GetRawText());
                SearchResult result = sdk.Graph.Search(req).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqProp.GetRawText());
                Graph graph = sdk.Graph.ReadFirst(req).GetAwaiter().GetResult();
                return graph != null ? Serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterMethod("graph/enablevectorindexing", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("config", out JsonElement configProp))
                    throw new ArgumentException("Vector index configuration is required");
                
                VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(configProp.GetRawText());
                sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("graph/rebuildvectorindex", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("graph/deletevectorindex", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("graph/getvectorindexconfig", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexConfiguration config = sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(config, true);
            });

            server.RegisterMethod("graph/getvectorindexstatistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexStatistics stats = sdk.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers graph methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
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
                Graph created = sdk.Graph.Create(graph).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
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

                Graph graph = sdk.Graph.ReadByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return graph != null ? Serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterMethod("graph/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Graph> graphs = sdk.Graph.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(graphs, true);
            });

            server.RegisterMethod("graph/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<Graph> result = sdk.Graph.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                    throw new ArgumentException("Graph object is required");
                Graph graph = Serializer.DeserializeJson<Graph>(graphProp.GetRawText());
                Graph updated = sdk.Graph.Update(graph).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
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
                sdk.Graph.DeleteByGuid(tenantGuid, graphGuid, force).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("graph/getsubgraph", (args) =>
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
                SearchResult result = sdk.Graph.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/getsubgraphstatistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                int maxDepth = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxDepth", 2);
                int maxNodes = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxNodes", 0);
                int maxEdges = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "maxEdges", 0);
                GraphStatistics stats = sdk.Graph.GetSubgraphStatistics(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });

            server.RegisterMethod("graph/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool exists = sdk.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("graph/statistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                {
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    GraphStatistics stats = sdk.Graph.GetStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(stats, true);
                }
                else
                {
                    Dictionary<Guid, GraphStatistics> allStats = sdk.Graph.GetStatistics(tenantGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(allStats, true);
                }
            });

            server.RegisterMethod("graph/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("graphGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Graph GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Graph> graphs = sdk.Graph.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(graphs, true);
            });

            server.RegisterMethod("graph/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqProp.GetRawText());
                SearchResult result = sdk.Graph.Search(req).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("graph/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqProp.GetRawText());
                Graph graph = sdk.Graph.ReadFirst(req).GetAwaiter().GetResult();
                return graph != null ? Serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterMethod("graph/enablevectorindexing", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("config", out JsonElement configProp))
                    throw new ArgumentException("Vector index configuration is required");
                
                VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(configProp.GetRawText());
                sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("graph/rebuildvectorindex", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("graph/deletevectorindex", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("graph/getvectorindexconfig", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexConfiguration config = sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(config, true);
            });

            server.RegisterMethod("graph/getvectorindexstatistics", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexStatistics stats = sdk.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });
        }

        #endregion
    }
}

