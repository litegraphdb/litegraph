namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
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
                    Edge created = sdk.Edge.Create(edge).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
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

                    Edge edge = sdk.Edge.ReadByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                    return edge != null ? Serializer.SerializeJson(edge, true) : "null";
                });

            server.RegisterTool(
                "edge/all",
                "Lists all edges in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<Edge> edges = sdk.Edge.ReadMany(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges, true);
                });

            server.RegisterTool(
                "edge/enumerate",
                "Enumerates edges with pagination and filtering",
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
                            query.GraphGUID = deserializedQuery.GraphGUID;
                        }
                    }
                    
                    EnumerationResult<Edge> result = sdk.Edge.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
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
                    Edge updated = sdk.Edge.Update(edge).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
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

                    sdk.Edge.DeleteByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                    return string.Empty;
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

                    bool exists = sdk.Edge.ExistsByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "edge/getmany",
                "Reads multiple edges by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuids = new { type = "array", items = new { type = "string" }, description = "Array of edge GUIDs" }
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
                    List<Edge> edges = sdk.Edge.ReadByGuids(tenantGuid, graphGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges, true);
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
                    List<Edge> created = sdk.Edge.CreateMany(tenantGuid, graphGuid, edges).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "edge/nodeedges",
                "Gets edges connected to a given node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<Edge> edges = sdk.Edge.ReadNodeEdges(tenantGuid, graphGuid, nodeGuid, null, null, null, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
                });

            server.RegisterTool(
                "edge/fromnode",
                "Gets edges from a given node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<Edge> edges = sdk.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
                });

            server.RegisterTool(
                "edge/tonode",
                "Gets edges to a given node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<Edge> edges = sdk.Edge.ReadEdgesToNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
                });

            server.RegisterTool(
                "edge/betweennodes",
                "Gets edges between two nodes",
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
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<Edge> edges = sdk.Edge.ReadEdgesBetweenNodes(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
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
                    SearchResult result = sdk.Edge.Search(request).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
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
                    Edge edge = sdk.Edge.ReadFirst(request).GetAwaiter().GetResult();
                    return edge != null ? Serializer.SerializeJson(edge, true) : "null";
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
                    
                    sdk.Edge.DeleteMany(tenantGuid, graphGuid, edgeGuids).GetAwaiter().GetResult();
                    return string.Empty;
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
                    
                    sdk.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                    return string.Empty;
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
                    sdk.Edge.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "edge/readallintenant",
                "Reads all edges in a tenant across all graphs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
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
                    List<Edge> edges = sdk.Edge.ReadAllInTenant(tenantGuid, order, skip, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges, true);
                });

            server.RegisterTool(
                "edge/readallingraph",
                "Reads all edges in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
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
                    List<Edge> edges = sdk.Edge.ReadAllInGraph(tenantGuid, graphGuid, order, skip, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(edges, true);
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
                    sdk.Edge.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                    return string.Empty;
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
                    sdk.Edge.DeleteNodeEdgesMany(tenantGuid, graphGuid, nodeGuids).GetAwaiter().GetResult();
                    return string.Empty;
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
                Edge created = sdk.Edge.Create(edge).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("edge/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                Edge edge = sdk.Edge.ReadByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return edge != null ? Serializer.SerializeJson(edge, true) : "null";
            });

            server.RegisterMethod("edge/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadMany(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
            });

            server.RegisterMethod("edge/enumerate", (args) =>
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
                        query.GraphGUID = deserializedQuery.GraphGUID;
                    }
                }
                
                EnumerationResult<Edge> result = sdk.Edge.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("edge/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                    throw new ArgumentException("Edge JSON string is required");
                string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                Edge updated = sdk.Edge.Update(edge).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("edge/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                sdk.Edge.DeleteByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("edge/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                bool exists = sdk.Edge.ExistsByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("edge/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Edge> edges = sdk.Edge.ReadByGuids(tenantGuid, graphGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
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
                List<Edge> created = sdk.Edge.CreateMany(tenantGuid, graphGuid, edges).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("edge/nodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadNodeEdges(tenantGuid, graphGuid, nodeGuid, null, null, null, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/fromnode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/tonode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadEdgesToNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/betweennodes", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadEdgesBetweenNodes(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                SearchResult result = sdk.Edge.Search(request).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("edge/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                Edge edge = sdk.Edge.ReadFirst(request).GetAwaiter().GetResult();
                return edge != null ? Serializer.SerializeJson(edge, true) : "null";
            });

            server.RegisterMethod("edge/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement edgeGuidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> edgeGuids = Serializer.DeserializeJson<List<Guid>>(edgeGuidsProp.GetRawText());
                sdk.Edge.DeleteMany(tenantGuid, graphGuid, edgeGuids).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("edge/deletenodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                
                sdk.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("edge/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Edge.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return "true";
            });

            server.RegisterMethod("edge/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                List<Edge> edges = sdk.Edge.ReadAllInTenant(tenantGuid, order, skip, includeData, includeSubordinates).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
            });

            server.RegisterMethod("edge/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                List<Edge> edges = sdk.Edge.ReadAllInGraph(tenantGuid, graphGuid, order, skip, includeData, includeSubordinates).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
            });

            server.RegisterMethod("edge/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Edge.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return "true";
            });

            server.RegisterMethod("edge/deletenodeedgesmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("Node GUIDs array is required");
                
                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());
                sdk.Edge.DeleteNodeEdgesMany(tenantGuid, graphGuid, nodeGuids).GetAwaiter().GetResult();
                return "true";
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
                Edge created = sdk.Edge.Create(edge).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("edge/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                Edge edge = sdk.Edge.ReadByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return edge != null ? Serializer.SerializeJson(edge, true) : "null";
            });

            server.RegisterMethod("edge/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                    throw new ArgumentException("Tenant GUID and graph GUID are required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadMany(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
            });

            server.RegisterMethod("edge/enumerate", (args) =>
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
                        query.GraphGUID = deserializedQuery.GraphGUID;
                    }
                }
                
                EnumerationResult<Edge> result = sdk.Edge.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("edge/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("edge", out JsonElement edgeProp))
                    throw new ArgumentException("Edge JSON string is required");
                string edgeJson = edgeProp.GetString() ?? throw new ArgumentException("Edge JSON string cannot be null");
                Edge edge = Serializer.DeserializeJson<Edge>(edgeJson);
                Edge updated = sdk.Edge.Update(edge).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("edge/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                sdk.Edge.DeleteByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("edge/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");

                bool exists = sdk.Edge.ExistsByGuid(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("edge/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Edge> edges = sdk.Edge.ReadByGuids(tenantGuid, graphGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
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
                List<Edge> created = sdk.Edge.CreateMany(tenantGuid, graphGuid, edges).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("edge/nodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadNodeEdges(tenantGuid, graphGuid, nodeGuid, null, null, null, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/fromnode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/tonode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadEdgesToNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/betweennodes", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Edge> edges = sdk.Edge.ReadEdgesBetweenNodes(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges ?? new List<Edge>(), true);
            });

            server.RegisterMethod("edge/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                SearchResult result = sdk.Edge.Search(request).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("edge/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("request", out JsonElement requestProp))
                    throw new ArgumentException("Search request object is required");
                
                string requestJson = requestProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest request = Serializer.DeserializeJson<SearchRequest>(requestJson);
                Edge edge = sdk.Edge.ReadFirst(request).GetAwaiter().GetResult();
                return edge != null ? Serializer.SerializeJson(edge, true) : "null";
            });

            server.RegisterMethod("edge/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("edgeGuids", out JsonElement edgeGuidsProp))
                    throw new ArgumentException("Edge GUIDs array is required");
                
                List<Guid> edgeGuids = Serializer.DeserializeJson<List<Guid>>(edgeGuidsProp.GetRawText());
                sdk.Edge.DeleteMany(tenantGuid, graphGuid, edgeGuids).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("edge/deletenodeedges", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                
                sdk.Edge.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("edge/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Edge.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return "true";
            });

            server.RegisterMethod("edge/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                List<Edge> edges = sdk.Edge.ReadAllInTenant(tenantGuid, order, skip, includeData, includeSubordinates).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
            });

            server.RegisterMethod("edge/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                List<Edge> edges = sdk.Edge.ReadAllInGraph(tenantGuid, graphGuid, order, skip, includeData, includeSubordinates).GetAwaiter().GetResult();
                return Serializer.SerializeJson(edges, true);
            });

            server.RegisterMethod("edge/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Edge.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return "true";
            });

            server.RegisterMethod("edge/deletenodeedgesmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("Node GUIDs array is required");
                
                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());
                sdk.Edge.DeleteNodeEdgesMany(tenantGuid, graphGuid, nodeGuids).GetAwaiter().GetResult();
                return "true";
            });
        }

        #endregion
    }
}

