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
            server.RegisterLiteGraphTool(
                "graph_create",
                "Creates a new graph in LiteGraph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "name": { "type": "string", "description": "Graph name" }
                        },
                        "required": [ "tenantGuid", "name" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("name", out JsonElement nameProp))
                        throw new ArgumentException("Tenant GUID and name are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    string? name = nameProp.GetString();
                    Graph graph = new Graph { TenantGUID = tenantGuid, Name = name };
                    return CreateGraph(sdk, tenantGuid, graph);
                });

            server.RegisterLiteGraphTool(
                "graph_get",
                "Reads a graph by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "includeData": { "type": "boolean", "description": "Include graph data" },
                            "includeSubordinates": { "type": "boolean", "description": "Include labels, tags, vectors" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                    return ReadGraph(sdk, tenantGuid, graphGuid, includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "graph_all",
                "Lists all graphs in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadGraphs(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterLiteGraphTool(
                "graph_readallintenant",
                "Reads all graphs in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadAllGraphsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterLiteGraphTool(
                "graph_enumerate",
                "Enumerates graphs with pagination and filtering",
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
                    EnumerationResult<Graph> result = sdk.Graph.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterLiteGraphTool(
                "graph_update",
                "Updates a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "graph": { "type": "string", "description": "Graph object serialized as JSON string using Serializer" }
                        },
                        "required": [ "graph" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                        throw new ArgumentException("Graph JSON string is required");
                    string graphJson = graphProp.GetString() ?? throw new ArgumentException("Graph JSON string cannot be null");
                    Graph graph = Serializer.DeserializeJson<Graph>(graphJson);
                    return UpdateGraph(sdk, graph);
                });

            server.RegisterLiteGraphTool(
                "graph_delete",
                "Deletes a graph by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "force": { "type": "boolean", "description": "Force deletion (default: false)" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");
                    
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                    DeleteGraph(sdk, tenantGuid, graphGuid, force);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "graph_deleteallintenant",
                "Deletes all graphs in a tenant after clearing dependent objects",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");

                    sdk.Graph.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterLiteGraphTool(
                "graph_getsubgraph",
                "Retrieves a subgraph starting from a specific node, traversing up to a specified depth. Useful for graph exploration and traversal.",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Starting node GUID" },
                            "maxDepth": { "type": "integer", "description": "Maximum depth to traverse (default: 2)" },
                            "maxNodes": { "type": "integer", "description": "Maximum number of nodes (0 = unlimited)" },
                            "maxEdges": { "type": "integer", "description": "Maximum number of edges (0 = unlimited)" },
                            "includeData": { "type": "boolean", "description": "Include node/edge data" },
                            "includeSubordinates": { "type": "boolean", "description": "Include labels, tags, vectors" }
                        },
                        "required": [ "tenantGuid", "graphGuid", "nodeGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphTool(
                "graph_getsubgraphstatistics",
                "Gets statistics for a subgraph starting from a specific node, traversing up to a specified depth",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Starting node GUID" },
                            "maxDepth": { "type": "integer", "description": "Maximum depth to traverse (default: 2)" },
                            "maxNodes": { "type": "integer", "description": "Maximum number of nodes (0 = unlimited)" },
                            "maxEdges": { "type": "integer", "description": "Maximum number of edges (0 = unlimited)" }
                        },
                        "required": [ "tenantGuid", "graphGuid", "nodeGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphTool(
                "graph_exportgexf",
                "Exports a graph as GEXF",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "includeData": { "type": "boolean", "description": "Include graph data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate objects (default: false)" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    string gexf = sdk.Graph.ExportGraphToGexf(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return gexf ?? string.Empty;
                });

            server.RegisterLiteGraphTool(
                "graph_exists",
                "Checks if a graph exists by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    bool exists = sdk.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterLiteGraphTool(
                "graph_statistics",
                "Gets statistics for a graph or all graphs in a tenant",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": {
                                "type": "string",
                                "description": "Graph GUID (optional, if not provided returns all graph statistics)"
                            }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphTool(
                "graph_getmany",
                "Reads multiple graphs by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuids": {
                                "type": "array",
                                "items": { "type": "string" },
                                "description": "Array of graph GUIDs"
                            },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "includeData": { "type": "boolean", "description": "Include graph data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate objects (default: false)" }
                        },
                        "required": [ "tenantGuid", "graphGuids" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("graphGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Graph GUIDs array is required");

                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadGraphsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "graph_search",
                "Searches graphs with filters",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "searchRequest": {
                                "type": "string",
                                "description": "Search request object serialized as JSON string using Serializer"
                            }
                        },
                        "required": [ "searchRequest" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                        throw new ArgumentException("Search request is required");
                    
                    string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                    SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    SearchResult result = sdk.Graph.Search(req).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterLiteGraphTool(
                "graph_readfirst",
                "Reads the first graph matching search criteria",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "searchRequest": {
                                "type": "string",
                                "description": "Search request object serialized as JSON string using Serializer"
                            }
                        },
                        "required": [ "searchRequest" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                        throw new ArgumentException("Search request is required");
                    
                    string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                    SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    Graph graph = sdk.Graph.ReadFirst(req).GetAwaiter().GetResult();
                    return graph != null ? Serializer.SerializeJson(graph, true) : "null";
                });

            server.RegisterLiteGraphTool(
                "graph_enablevectorindexing",
                "Enables vector indexing for a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "config": {
                                "type": "string",
                                "description": "Vector index configuration object serialized as JSON string using Serializer"
                            }
                        },
                        "required": [ "tenantGuid", "graphGuid", "config" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("config", out JsonElement configProp))
                        throw new ArgumentException("Vector index configuration is required");
                    
                    string configJson = configProp.GetString() ?? throw new ArgumentException("VectorIndexConfiguration JSON string cannot be null");
                    VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(configJson);
                    sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterLiteGraphTool(
                "graph_rebuildvectorindex",
                "Rebuilds the vector index for a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterLiteGraphTool(
                "graph_deletevectorindex",
                "Deletes the vector index for a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "deleteFile": { "type": "boolean", "description": "True to delete backing index file (default: false)" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    bool deleteFile = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "deleteFile", false);
                    sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid, deleteFile).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterLiteGraphTool(
                "graph_getvectorindexconfig",
                "Reads the vector index configuration for a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    VectorIndexConfiguration config = sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(config, true);
                });

            server.RegisterLiteGraphTool(
                "graph_getvectorindexstatistics",
                "Gets vector index statistics for a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" }
                        },
                        "required": [ "tenantGuid", "graphGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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
            server.RegisterLiteGraphMethod("graph_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                string? name = nameProp.GetString();
                Graph graph = new Graph { TenantGUID = tenantGuid, Name = name };
                return CreateGraph(sdk, tenantGuid, graph);
            });

            server.RegisterLiteGraphMethod("graph_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                return ReadGraph(sdk, tenantGuid, graphGuid, includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("graph_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphs(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("graph_readallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllGraphsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("graph_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

                EnumerationResult<Graph> result = sdk.Graph.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterLiteGraphMethod("graph_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                    throw new ArgumentException("Graph JSON string is required");
                string graphJson = graphProp.GetString() ?? throw new ArgumentException("Graph JSON string cannot be null");
                Graph graph = Serializer.DeserializeJson<Graph>(graphJson);
                return UpdateGraph(sdk, graph);
            });

            server.RegisterLiteGraphMethod("graph_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                DeleteGraph(sdk, tenantGuid, graphGuid, force);
                return true;
            });

            server.RegisterLiteGraphMethod("graph_deleteallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");

                sdk.Graph.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterLiteGraphMethod("graph_getsubgraph", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphMethod("graph_getsubgraphstatistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphMethod("graph_exportgexf", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                string gexf = sdk.Graph.ExportGraphToGexf(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                return gexf ?? string.Empty;
            });

            server.RegisterLiteGraphMethod("graph_exportgexf", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                string gexf = sdk.Graph.ExportGraphToGexf(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                return gexf ?? string.Empty;
            });

            server.RegisterLiteGraphMethod("graph_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool exists = sdk.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterLiteGraphMethod("graph_statistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphMethod("graph_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("graphGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Graph GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadGraphsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("graph_search", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                SearchResult result = sdk.Graph.Search(req).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterLiteGraphMethod("graph_readfirst", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                Graph graph = sdk.Graph.ReadFirst(req).GetAwaiter().GetResult();
                return graph != null ? Serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterLiteGraphMethod("graph_enablevectorindexing", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("config", out JsonElement configProp))
                    throw new ArgumentException("Vector index configuration is required");
                
                string configJson = configProp.GetString() ?? throw new ArgumentException("VectorIndexConfiguration JSON string cannot be null");
                VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(configJson);
                sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterLiteGraphMethod("graph_rebuildvectorindex", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterLiteGraphMethod("graph_deletevectorindex", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool deleteFile = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "deleteFile", false);
                sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid, deleteFile).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterLiteGraphMethod("graph_getvectorindexconfig", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexConfiguration config = sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(config, true);
            });

            server.RegisterLiteGraphMethod("graph_getvectorindexstatistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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
            server.RegisterLiteGraphMethod("graph_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                string? name = nameProp.GetString();
                Graph graph = new Graph { TenantGUID = tenantGuid, Name = name };
                return CreateGraph(sdk, tenantGuid, graph);
            });

            server.RegisterLiteGraphMethod("graph_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                return ReadGraph(sdk, tenantGuid, graphGuid, includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("graph_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphs(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("graph_readallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllGraphsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterLiteGraphMethod("graph_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

                EnumerationResult<Graph> result = sdk.Graph.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterLiteGraphMethod("graph_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("graph", out JsonElement graphProp))
                    throw new ArgumentException("Graph JSON string is required");
                string graphJson = graphProp.GetString() ?? throw new ArgumentException("Graph JSON string cannot be null");
                Graph graph = Serializer.DeserializeJson<Graph>(graphJson);
                return UpdateGraph(sdk, graph);
            });

            server.RegisterLiteGraphMethod("graph_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                bool force = args.Value.TryGetProperty("force", out JsonElement forceProp) && forceProp.GetBoolean();
                DeleteGraph(sdk, tenantGuid, graphGuid, force);
                return true;
            });

            server.RegisterLiteGraphMethod("graph_deleteallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Graph.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterLiteGraphMethod("graph_getsubgraph", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphMethod("graph_getsubgraphstatistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphMethod("graph_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool exists = sdk.Graph.ExistsByGuid(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterLiteGraphMethod("graph_statistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
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

            server.RegisterLiteGraphMethod("graph_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("graphGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Graph GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadGraphsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("graph_search", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                SearchResult result = sdk.Graph.Search(req).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterLiteGraphMethod("graph_readfirst", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");
                
                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                Graph graph = sdk.Graph.ReadFirst(req).GetAwaiter().GetResult();
                return graph != null ? Serializer.SerializeJson(graph, true) : "null";
            });

            server.RegisterLiteGraphMethod("graph_enablevectorindexing", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("config", out JsonElement configProp))
                    throw new ArgumentException("Vector index configuration is required");
                
                string configJson = configProp.GetString() ?? throw new ArgumentException("VectorIndexConfiguration JSON string cannot be null");
                VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(configJson);
                sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterLiteGraphMethod("graph_rebuildvectorindex", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterLiteGraphMethod("graph_deletevectorindex", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool deleteFile = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "deleteFile", false);
                sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid, deleteFile).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterLiteGraphMethod("graph_getvectorindexconfig", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexConfiguration config = sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(config, true);
            });

            server.RegisterLiteGraphMethod("graph_getvectorindexstatistics", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                VectorIndexStatistics stats = sdk.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(stats, true);
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateGraph(LiteGraphSdk sdk, Guid tenantGuid, Graph graph)
        {
            string body = Serializer.SerializeJson(graph, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs",
                body);
        }

        private static string ReadGraph(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "?incldata="
                + includeData.ToString().ToLowerInvariant()
                + "&inclsub="
                + includeSubordinates.ToString().ToLowerInvariant());
        }

        private static string ReadGraphs(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken)
        {
            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs?order="
                + LiteGraphMcpRestProxy.Escape(order.ToString())
                + "&skip="
                + skip
                + "&max-keys="
                + maxResults;

            if (continuationToken != null) url += "&token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value);

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string ReadAllGraphsInTenant(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken)
        {
            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/all?order="
                + LiteGraphMcpRestProxy.Escape(order.ToString())
                + "&skip="
                + skip
                + "&max-keys="
                + maxResults;

            if (continuationToken != null) url += "&token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value);

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string ReadGraphsByGuids(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            List<Guid> graphGuids,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            if (graphGuids == null) throw new ArgumentNullException(nameof(graphGuids));
            if (graphGuids.Count == 0) throw new ArgumentException("At least one graph GUID is required.");

            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs?guids="
                + String.Join(",", graphGuids)
                + "&max-keys="
                + maxResults
                + "&incldata="
                + includeData.ToString().ToLowerInvariant()
                + "&inclsub="
                + includeSubordinates.ToString().ToLowerInvariant();

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string UpdateGraph(LiteGraphSdk sdk, Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            string body = Serializer.SerializeJson(graph, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(graph.TenantGUID)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graph.GUID),
                body);
        }

        private static void DeleteGraph(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, bool force)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + (force ? "?force" : String.Empty));
        }

        #endregion
    }
}
