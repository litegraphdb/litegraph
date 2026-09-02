namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net.Http;
    using System.Text.Json;
    using ExpressionTree;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for Edge operations.
    /// </summary>
    public static class EdgeRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers edge tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "edge/create",
                "Creates a new edge between two nodes",
                new
                {
                    type = "object",
                    properties = new
                    {
                        edge = new { type = "string", description = "Edge object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "edge" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                        throw new ArgumentException("Edge JSON string is required");
                    string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                    Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                    return CreateEdge(sdk, edge);
                });

            server.RegisterTool(
                "edge/get",
                "Reads an edge by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" },
                        includeData = new { type = "boolean", description = "Include edge data (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate objects (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                    return ReadEdge(sdk, tenantGuid, graphGuid, edgeGuid, includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/all",
                "Lists all edges in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadEdges(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), false, false);
                });

            server.RegisterTool(
                "edge/enumerate",
                "Enumerates edges with pagination and filtering",
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
                    if (query.GraphGUID == null)
                        throw new ArgumentException("query.GraphGUID is required.");
                    
                    return EnumerateEdges(sdk, query);
                });

            server.RegisterTool(
                "edge/update",
                "Updates an existing edge",
                new
                {
                    type = "object",
                    properties = new
                    {
                        edge = new { type = "string", description = "Edge object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "edge" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                        throw new ArgumentException("Edge JSON string is required");
                    string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                    Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                    return UpdateEdge(sdk, edge);
                });

            server.RegisterTool(
                "edge/delete",
                "Deletes an edge by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                    DeleteEdge(sdk, tenantGuid, graphGuid, edgeGuid);
                    return true;
                });

            server.RegisterTool(
                "edge/exists",
                "Checks if an edge exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                    return EdgeExists(sdk, tenantGuid, graphGuid, edgeGuid).ToString().ToLowerInvariant();
                });

            server.RegisterTool(
                "edge/getmany",
                "Reads multiple edges by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuids = new { type = "array", items = new { type = "string" }, description = "Array of edge GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        includeData = new { type = "boolean", description = "Include edge data (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate objects (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("edgeGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Edge GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadEdgesByGuids(sdk, tenantGuid, graphGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/createmany",
                "Creates multiple edges in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edges = new { type = "string", description = "Array of edge objects serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edges" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("edges", out JsonElement edgesProp))
                        throw new ArgumentException("Edges array is required");
                    
                    string edgesJson = edgesProp.GetString() ?? throw new ArgumentException("Edges JSON string cannot be null");
                    List<Edge> edges = Serializer.DeserializeJson<List<Edge>>(edgesJson);
                    return CreateEdges(sdk, tenantGuid, graphGuid, edges);
                });

            server.RegisterTool(
                "edge/nodeedges",
                "Gets edges connected to a given node. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        labels = new { type = "string", description = "Array of labels serialized as JSON string using Serializer (optional)" },
                        tags = new { type = "string", description = "Name-value collection serialized as JSON string using Serializer (optional)" },
                        edgeFilter = new { type = "string", description = "Edge filter expression serialized as JSON string using Serializer (optional)" },
                        includeData = new { type = "boolean", description = "Include edge data (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate objects (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<string>? labels = null;
                    if (args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                    {
                        string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                        labels = Serializer.DeserializeJson<List<string>>(labelsJson);
                    }

                    NameValueCollection? tags = null;
                    if (args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                    {
                        string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                        tags = Serializer.DeserializeJson<NameValueCollection>(tagsJson);
                    }

                    Expr? edgeFilter = null;
                    if (args.Value.TryGetProperty("edgeFilter", out JsonElement edgeFilterProp))
                    {
                        string edgeFilterJson = edgeFilterProp.GetString() ?? throw new ArgumentException("Edge filter JSON string cannot be null");
                        edgeFilter = Serializer.DeserializeJson<Expr>(edgeFilterJson);
                    }

                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                    return ReadNodeEdges(sdk, tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/fromnode",
                "Gets edges from a given node. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        includeData = new { type = "boolean", description = "Include edge data (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate objects (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadEdgesFromNode(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/tonode",
                "Gets edges to a given node. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        includeData = new { type = "boolean", description = "Include edge data (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate objects (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadEdgesToNode(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/betweennodes",
                "Gets edges between two nodes. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        fromNodeGuid = new { type = "string", description = "From node GUID" },
                        toNodeGuid = new { type = "string", description = "To node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "fromNodeGuid", "toNodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                    Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadEdgesBetweenNodes(sdk, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "edge/search",
                "Searches for edges",
                new
                {
                    type = "object",
                    properties = new
                    {
                        request = new { type = "string", description = "Search request object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "request" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                        throw new ArgumentException("Search request object is required");
                    
                    string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                    SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                    return SearchEdges(sdk, request);
                });

            server.RegisterTool(
                "edge/readfirst",
                "Reads the first edge matching search criteria",
                new
                {
                    type = "object",
                    properties = new
                    {
                        request = new { type = "string", description = "Search request object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "request" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                        throw new ArgumentException("Search request object is required");
                    
                    string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                    SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                    return ReadFirstEdge(sdk, request);
                });

            server.RegisterTool(
                "edge/deletemany",
                "Deletes multiple edges by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuids = new { type = "array", items = new { type = "string" }, description = "Array of edge GUIDs to delete" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("edgeGuids", out JsonElement edgeGuidsProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and edgeGuids array are required");
                    
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    List<Guid> edgeGuids = Serializer.DeserializeJson<List<Guid>>(edgeGuidsProp.GetRawText());
                    
                    DeleteEdges(sdk, tenantGuid, graphGuid, edgeGuids);
                    return true;
                });

            server.RegisterTool(
                "edge/deletenodeedges",
                "Deletes all edges associated with a given node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("nodeGuid", out JsonElement nodeGuidProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and node GUID are required");
                    
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    Guid nodeGuid = Guid.Parse(nodeGuidProp.GetString()!);
                    
                    DeleteNodeEdges(sdk, tenantGuid, graphGuid, nodeGuid);
                    return true;
                });

            server.RegisterTool(
                "edge/deleteallingraph",
                "Deletes all edges in a graph",
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
                    DeleteAllEdgesInGraph(sdk, tenantGuid, graphGuid);
                    return true;
                });

            server.RegisterTool(
                "edge/readallintenant",
                "Reads all edges in a tenant across all graphs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" },
                        includeData = new { type = "boolean", description = "Include data property (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate properties (default: false)" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadAllEdgesInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/readallingraph",
                "Reads all edges in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" },
                        includeData = new { type = "boolean", description = "Include data property (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include subordinate properties (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadAllEdgesInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
                });

            server.RegisterTool(
                "edge/deleteallintenant",
                "Deletes all edges in a tenant across all graphs",
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
                    DeleteAllEdgesInTenant(sdk, tenantGuid);
                    return true;
                });

            server.RegisterTool(
                "edge/deletenodeedgesmany",
                "Deletes all edges associated with multiple nodes",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuids = new { type = "array", items = new { type = "string" }, description = "Array of node GUIDs" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                        throw new ArgumentException("Node GUIDs array is required");
                    
                    List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());
                    DeleteNodeEdges(sdk, tenantGuid, graphGuid, nodeGuids);
                    return true;
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers edge methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("edge/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                    throw new ArgumentException("Edge JSON string is required");
                string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                return CreateEdge(sdk, edge);
            });

            server.RegisterMethod("edge/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                return ReadEdge(sdk, tenantGuid, graphGuid, edgeGuid, includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdges(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), false, false);
            });

            server.RegisterMethod("edge/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                if (query.GraphGUID == null)
                    throw new ArgumentException("query.GraphGUID is required.");

                return EnumerateEdges(sdk, query);
            });

            server.RegisterMethod("edge/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                    throw new ArgumentException("Edge JSON string is required");
                string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                return UpdateEdge(sdk, edge);
            });

            server.RegisterMethod("edge/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                DeleteEdge(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
            });

            server.RegisterMethod("edge/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                return EdgeExists(sdk, tenantGuid, graphGuid, edgeGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("edge/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadEdgesByGuids(sdk, tenantGuid, graphGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edges", out JsonElement edgesProp))
                    throw new ArgumentException("Edges array is required");
                
                string edgesJson = edgesProp.GetString() ?? throw new ArgumentException("Edges JSON string cannot be null");
                List<Edge> edges = Serializer.DeserializeJson<List<Edge>>(edgesJson);
                return CreateEdges(sdk, tenantGuid, graphGuid, edges);
            });

            server.RegisterMethod("edge/nodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<string>? labels = null;
                if (args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                {
                    string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                    labels = Serializer.DeserializeJson<List<string>>(labelsJson);
                }

                NameValueCollection? tags = null;
                if (args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                {
                    string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                    tags = Serializer.DeserializeJson<NameValueCollection>(tagsJson);
                }

                Expr? edgeFilter = null;
                if (args.Value.TryGetProperty("edgeFilter", out JsonElement edgeFilterProp))
                {
                    string edgeFilterJson = edgeFilterProp.GetString() ?? throw new ArgumentException("Edge filter JSON string cannot be null");
                    edgeFilter = Serializer.DeserializeJson<Expr>(edgeFilterJson);
                }

                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                return ReadNodeEdges(sdk, tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/fromnode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadEdgesFromNode(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/tonode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadEdgesToNode(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/betweennodes", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgesBetweenNodes(sdk, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("edge/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                return SearchEdges(sdk, request);
            });

            server.RegisterMethod("edge/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                return ReadFirstEdge(sdk, request);
            });

            server.RegisterMethod("edge/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement edgeGuidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> edgeGuids = Serializer.DeserializeJson<List<Guid>>(edgeGuidsProp.GetRawText());
                DeleteEdges(sdk, tenantGuid, graphGuid, edgeGuids);
                return true;
            });

            server.RegisterMethod("edge/deletenodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                
                DeleteNodeEdges(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("edge/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllEdgesInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("edge/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadAllEdgesInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadAllEdgesInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllEdgesInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("edge/deletenodeedgesmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("Node GUIDs array is required");
                
                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());
                DeleteNodeEdges(sdk, tenantGuid, graphGuid, nodeGuids);
                return true;
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers edge methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("edge/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                    throw new ArgumentException("Edge JSON string is required");
                string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                return CreateEdge(sdk, edge);
            });

            server.RegisterMethod("edge/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                return ReadEdge(sdk, tenantGuid, graphGuid, edgeGuid, includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdges(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), false, false);
            });

            server.RegisterMethod("edge/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                if (query.GraphGUID == null)
                    throw new ArgumentException("query.GraphGUID is required.");

                return EnumerateEdges(sdk, query);
            });

            server.RegisterMethod("edge/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                    throw new ArgumentException("Edge JSON string is required");
                string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                return UpdateEdge(sdk, edge);
            });

            server.RegisterMethod("edge/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                DeleteEdge(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
            });

            server.RegisterMethod("edge/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                return EdgeExists(sdk, tenantGuid, graphGuid, edgeGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("edge/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadEdgesByGuids(sdk, tenantGuid, graphGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edges", out JsonElement edgesProp))
                    throw new ArgumentException("Edges array is required");
                
                string edgesJson = edgesProp.GetString() ?? throw new ArgumentException("Edges JSON string cannot be null");
                List<Edge> edges = Serializer.DeserializeJson<List<Edge>>(edgesJson);
                return CreateEdges(sdk, tenantGuid, graphGuid, edges);
            });

            server.RegisterMethod("edge/nodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<string>? labels = null;
                if (args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                {
                    string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                    labels = Serializer.DeserializeJson<List<string>>(labelsJson);
                }

                NameValueCollection? tags = null;
                if (args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                {
                    string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                    tags = Serializer.DeserializeJson<NameValueCollection>(tagsJson);
                }

                Expr? edgeFilter = null;
                if (args.Value.TryGetProperty("edgeFilter", out JsonElement edgeFilterProp))
                {
                    string edgeFilterJson = edgeFilterProp.GetString() ?? throw new ArgumentException("Edge filter JSON string cannot be null");
                    edgeFilter = Serializer.DeserializeJson<Expr>(edgeFilterJson);
                }

                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                return ReadNodeEdges(sdk, tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/fromnode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadEdgesFromNode(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/tonode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadEdgesToNode(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/betweennodes", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgesBetweenNodes(sdk, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("edge/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                return SearchEdges(sdk, request);
            });

            server.RegisterMethod("edge/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                return ReadFirstEdge(sdk, request);
            });

            server.RegisterMethod("edge/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement edgeGuidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> edgeGuids = Serializer.DeserializeJson<List<Guid>>(edgeGuidsProp.GetRawText());
                DeleteEdges(sdk, tenantGuid, graphGuid, edgeGuids);
                return true;
            });

            server.RegisterMethod("edge/deletenodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                
                DeleteNodeEdges(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("edge/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllEdgesInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("edge/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadAllEdgesInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadAllEdgesInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterMethod("edge/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllEdgesInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("edge/deletenodeedgesmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("Node GUIDs array is required");
                
                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());
                DeleteNodeEdges(sdk, tenantGuid, graphGuid, nodeGuids);
                return true;
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateEdge(LiteGraphSdk sdk, Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));

            string body = Serializer.SerializeJson(edge, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(edge.TenantGUID)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(edge.GraphGUID)
                + "/edges",
                body);
        }

        private static string ReadEdge(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
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
                + "/edges/"
                + LiteGraphMcpRestProxy.Escape(edgeGuid)
                + "?incldata="
                + includeData.ToString().ToLowerInvariant()
                + "&inclsub="
                + includeSubordinates.ToString().ToLowerInvariant());
        }

        private static string ReadEdges(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                EdgeCollectionPath(tenantGuid, graphGuid) + BuildReadQuery(order, skip, maxResults, continuationToken, includeData, includeSubordinates));
        }

        private static string EnumerateEdges(LiteGraphSdk sdk, EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentException("query.TenantGUID is required.");
            if (query.GraphGUID == null) throw new ArgumentException("query.GraphGUID is required.");

            string body = Serializer.SerializeJson(query, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                "/v2.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(query.GraphGUID.Value)
                + "/edges",
                body);
        }

        private static bool EdgeExists(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            return LiteGraphMcpRestProxy.HeadExists(sdk, EdgePath(tenantGuid, graphGuid, edgeGuid));
        }

        private static string ReadEdgesByGuids(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            List<Guid> edgeGuids,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            if (edgeGuids == null) throw new ArgumentNullException(nameof(edgeGuids));
            if (edgeGuids.Count == 0) throw new ArgumentException("At least one edge GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                EdgeCollectionPath(tenantGuid, graphGuid)
                + "?guids="
                + String.Join(",", edgeGuids)
                + "&max-keys="
                + maxResults
                + "&incldata="
                + includeData.ToString().ToLowerInvariant()
                + "&inclsub="
                + includeSubordinates.ToString().ToLowerInvariant());
        }

        private static string CreateEdges(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, List<Edge> edges)
        {
            string body = Serializer.SerializeJson(edges ?? new List<Edge>(), false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                EdgeCollectionPath(tenantGuid, graphGuid) + "/bulk",
                body);
        }

        private static string ReadNodeEdges(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string>? labels,
            NameValueCollection? tags,
            Expr? edgeFilter,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            string path = NodeEdgesPath(tenantGuid, graphGuid, nodeGuid);
            bool hasBodyFilters = (labels != null && labels.Count > 0) || (tags != null && tags.Count > 0) || edgeFilter != null;
            if (!hasBodyFilters)
            {
                return LiteGraphMcpRestProxy.SendJson(
                    sdk,
                    HttpMethod.Get,
                    path + BuildReadQuery(order, skip, maxResults, null, includeData, includeSubordinates));
            }

            SearchRequest request = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Labels = labels ?? new List<string>(),
                Tags = tags ?? new NameValueCollection(StringComparer.InvariantCultureIgnoreCase),
                Expr = edgeFilter,
                Ordering = order,
                Skip = skip
            };

            string body = Serializer.SerializeJson(request, false);
            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Post, path + BuildReadQuery(order, skip, maxResults, null, includeData, includeSubordinates), body);
        }

        private static string ReadEdgesFromNode(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodeEdgesPath(tenantGuid, graphGuid, nodeGuid) + "/from" + BuildReadQuery(order, skip, maxResults, null, includeData, includeSubordinates));
        }

        private static string ReadEdgesToNode(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodeEdgesPath(tenantGuid, graphGuid, nodeGuid) + "/to" + BuildReadQuery(order, skip, maxResults, null, includeData, includeSubordinates));
        }

        private static string ReadEdgesBetweenNodes(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                EdgeCollectionPath(tenantGuid, graphGuid)
                + "/between?from="
                + LiteGraphMcpRestProxy.Escape(fromNodeGuid)
                + "&to="
                + LiteGraphMcpRestProxy.Escape(toNodeGuid)
                + "&order="
                + LiteGraphMcpRestProxy.Escape(order.ToString())
                + "&skip="
                + skip
                + "&max-keys="
                + maxResults);
        }

        private static string SearchEdges(LiteGraphSdk sdk, SearchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.TenantGUID == default(Guid)) throw new ArgumentException("request.TenantGUID is required.");
            if (request.GraphGUID == default(Guid)) throw new ArgumentException("request.GraphGUID is required.");

            string body = Serializer.SerializeJson(request, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                EdgeCollectionPath(request.TenantGUID, request.GraphGUID) + "/search",
                body);
        }

        private static string ReadFirstEdge(LiteGraphSdk sdk, SearchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.TenantGUID == default(Guid)) throw new ArgumentException("request.TenantGUID is required.");
            if (request.GraphGUID == default(Guid)) throw new ArgumentException("request.GraphGUID is required.");

            string body = Serializer.SerializeJson(request, false);
            return LiteGraphMcpRestProxy.SendJsonOrNullOnNotFound(
                sdk,
                HttpMethod.Post,
                EdgeCollectionPath(request.TenantGUID, request.GraphGUID) + "/first",
                body);
        }

        private static string UpdateEdge(LiteGraphSdk sdk, Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));

            string body = Serializer.SerializeJson(edge, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(edge.TenantGUID)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(edge.GraphGUID)
                + "/edges/"
                + LiteGraphMcpRestProxy.Escape(edge.GUID),
                body);
        }

        private static void DeleteEdge(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/edges/"
                + LiteGraphMcpRestProxy.Escape(edgeGuid));
        }

        private static void DeleteEdges(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            string body = Serializer.SerializeJson(edgeGuids ?? new List<Guid>(), false);
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                EdgeCollectionPath(tenantGuid, graphGuid) + "/bulk",
                body);
        }

        private static void DeleteNodeEdges(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                NodeEdgesPath(tenantGuid, graphGuid, nodeGuid));
        }

        private static void DeleteAllEdgesInGraph(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                EdgeCollectionPath(tenantGuid, graphGuid) + "/all");
        }

        private static string ReadAllEdgesInTenant(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/edges/all"
                + BuildReadQuery(order, skip, maxResults, continuationToken, includeData, includeSubordinates));
        }

        private static string ReadAllEdgesInGraph(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                EdgeCollectionPath(tenantGuid, graphGuid) + "/all" + BuildReadQuery(order, skip, maxResults, continuationToken, includeData, includeSubordinates));
        }

        private static void DeleteAllEdgesInTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/edges/all");
        }

        private static void DeleteNodeEdges(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            string body = Serializer.SerializeJson(nodeGuids ?? new List<Guid>(), false);
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes/edges/bulk",
                body);
        }

        private static string EdgeCollectionPath(Guid tenantGuid, Guid graphGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/edges";
        }

        private static string EdgePath(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            return EdgeCollectionPath(tenantGuid, graphGuid)
                + "/"
                + LiteGraphMcpRestProxy.Escape(edgeGuid);
        }

        private static string NodeEdgesPath(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes/"
                + LiteGraphMcpRestProxy.Escape(nodeGuid)
                + "/edges";
        }

        private static string BuildReadQuery(
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken,
            bool includeData,
            bool includeSubordinates)
        {
            List<string> query = new List<string>
            {
                "order=" + LiteGraphMcpRestProxy.Escape(order.ToString()),
                "skip=" + skip,
                "max-keys=" + maxResults
            };

            if (continuationToken != null) query.Add("token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value));
            if (includeData) query.Add("incldata=true");
            if (includeSubordinates) query.Add("inclsub=true");

            return "?" + String.Join("&", query);
        }

        #endregion
    }
}
