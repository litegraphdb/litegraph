namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using ExpressionTree;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for Node operations.
    /// </summary>
    public static class NodeRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers node tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphTool(
                "node_create",
                "Creates a new node in a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "name": { "type": "string", "description": "Node name" }
                        },
                        "required": [ "tenantGuid", "graphGuid", "name" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("name", out JsonElement nameProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and name are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    string name = nameProp.GetString()!;
                    Node node = new Node { TenantGUID = tenantGuid, GraphGUID = graphGuid, Name = name };
                    return CreateNode(sdk, tenantGuid, graphGuid, node);
                });

            server.RegisterLiteGraphTool(
                "node_get",
                "Reads a node by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Node GUID" },
                            "includeData": { "type": "boolean", "description": "Include node data" },
                            "includeSubordinates": { "type": "boolean", "description": "Include labels, tags, vectors" }
                        },
                        "required": [ "tenantGuid", "graphGuid", "nodeGuid" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("nodeGuid", out JsonElement nodeGuidProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and node GUID are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    Guid nodeGuid = Guid.Parse(nodeGuidProp.GetString()!);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);

                    return ReadNode(sdk, tenantGuid, graphGuid, nodeGuid, includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_all",
                "Lists all nodes in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            },
                            "includeData": { "type": "boolean", "description": "Include node data" },
                            "includeSubordinates": { "type": "boolean", "description": "Include labels, tags, vectors" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_traverse",
                "Finds routes/paths between two nodes in a graph using depth-first search",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "fromNodeGuid": { "type": "string", "description": "Source node GUID" },
                            "toNodeGuid": { "type": "string", "description": "Target node GUID" },
                            "searchType": { "type": "string", "description": "Search type: DepthFirstSearch" },
                            "edgeFilter": {
                                "type": "string",
                                "description": "Edge filter expression serialized as JSON string using Serializer (optional)"
                            },
                            "nodeFilter": {
                                "type": "string",
                                "description": "Node filter expression serialized as JSON string using Serializer (optional)"
                            }
                        },
                        "required": [ "tenantGuid", "graphGuid", "fromNodeGuid", "toNodeGuid", "searchType" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                    Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                    SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                    Expr? edgeFilter = null;
                    if (args.Value.TryGetProperty("edgeFilter", out JsonElement edgeFilterProp))
                    {
                        string edgeFilterJson = edgeFilterProp.GetString() ?? throw new ArgumentException("Edge filter JSON string cannot be null");
                        edgeFilter = Serializer.DeserializeJson<Expr>(edgeFilterJson);
                    }

                    Expr? nodeFilter = null;
                    if (args.Value.TryGetProperty("nodeFilter", out JsonElement nodeFilterProp))
                    {
                        string nodeFilterJson = nodeFilterProp.GetString() ?? throw new ArgumentException("Node filter JSON string cannot be null");
                        nodeFilter = Serializer.DeserializeJson<Expr>(nodeFilterJson);
                    }

                    return ReadRoutes(sdk, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, edgeFilter, nodeFilter);
                });

            server.RegisterLiteGraphTool(
                "node_parents",
                "Gets parent nodes (nodes that have edges connecting to this node). Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Node GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadParents(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));

                });

            server.RegisterLiteGraphTool(
                "node_children",
                "Gets child nodes (nodes to which this node has connecting edges). Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Node GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadChildren(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterLiteGraphTool(
                "node_deleteall",
                "Deletes all nodes in a graph",
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
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    DeleteAllNodesInGraph(sdk, tenantGuid, graphGuid);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "node_deletemany",
                "Deletes multiple nodes by their GUIDs",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuids": {
                                "type": "array",
                                "items": { "type": "string" },
                                "description": "Array of node GUIDs to delete"
                            }
                        },
                        "required": [ "tenantGuid", "graphGuid", "nodeGuids" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and nodeGuids array are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());

                    DeleteNodes(sdk, tenantGuid, graphGuid, nodeGuids);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "node_readallintenant",
                "Reads all nodes in a tenant across all graphs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                            },
                            "includeData": { "type": "boolean", "description": "Include node data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate data (default: false)" }
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
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadAllNodesInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_readallingraph",
                "Reads all nodes in a graph with optional data/subordinate inclusion. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            },
                            "includeData": { "type": "boolean", "description": "Include node data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate data (default: false)" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadAllNodesInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_readmostconnected",
                "Reads the most connected nodes in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "includeData": { "type": "boolean", "description": "Include node data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate data (default: false)" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadMostConnectedNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_readleastconnected",
                "Reads the least connected nodes in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "includeData": { "type": "boolean", "description": "Include node data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate data (default: false)" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadLeastConnectedNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_deleteallintenant",
                "Deletes all nodes across all graphs in a tenant",
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
                    DeleteAllNodesInTenant(sdk, tenantGuid);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "node_neighbors",
                "Gets neighbor nodes (all connected nodes regardless of edge direction). Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Node GUID" },
                            "order": { "type": "string", "description": "Enumeration order (default: CreatedDescending)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" }
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
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadNeighbors(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterLiteGraphTool(
                "node_createmany",
                "Creates multiple nodes in a graph",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodes": {
                                "type": "string",
                                "description": "Array of node objects serialized as JSON string using Serializer"
                            }
                        },
                        "required": [ "tenantGuid", "graphGuid", "nodes" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("nodes", out JsonElement nodesProp))
                        throw new ArgumentException("Nodes array is required");

                    string nodesJson = nodesProp.GetString() ?? throw new ArgumentException("Nodes JSON string cannot be null");
                    List<Node> nodes = Serializer.DeserializeJson<List<Node>>(nodesJson);
                    return CreateNodes(sdk, tenantGuid, graphGuid, nodes);
                });

            server.RegisterLiteGraphTool(
                "node_getmany",
                "Reads multiple nodes by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuids": {
                                "type": "array",
                                "items": { "type": "string" },
                                "description": "Array of node GUIDs"
                            },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "includeData": { "type": "boolean", "description": "Include node data (default: false)" },
                            "includeSubordinates": { "type": "boolean", "description": "Include subordinate data (default: false)" }
                        },
                        "required": [ "tenantGuid", "graphGuid", "nodeGuids" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("nodeGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Node GUIDs array is required");

                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadNodesByGuids(sdk, tenantGuid, graphGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
                });

            server.RegisterLiteGraphTool(
                "node_update",
                "Updates a node",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "node": { "type": "string", "description": "Node object serialized as JSON string using Serializer" }
                        },
                        "required": [ "node" ]
                    }
                    """),
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("node", out JsonElement nodeProp))
                        throw new ArgumentException("Node JSON string is required");
                    string nodeJson = nodeProp.GetString() ?? throw new ArgumentException("Node JSON string cannot be null");
                    Node node = Serializer.DeserializeJson<Node>(nodeJson);
                    return UpdateNode(sdk, node);
                });

            server.RegisterLiteGraphTool(
                "node_delete",
                "Deletes a single node by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Node GUID" }
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
                    DeleteNode(sdk, tenantGuid, graphGuid, nodeGuid);
                    return true;
                });

            server.RegisterLiteGraphTool(
                "node_exists",
                "Checks if a node exists by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "graphGuid": { "type": "string", "description": "Graph GUID" },
                            "nodeGuid": { "type": "string", "description": "Node GUID" }
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
                    return NodeExists(sdk, tenantGuid, graphGuid, nodeGuid).ToString().ToLowerInvariant();
                });

            server.RegisterLiteGraphTool(
                "node_search",
                "Searches nodes with filters",
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
                    return SearchNodes(sdk, req);
                });

            server.RegisterLiteGraphTool(
                "node_readfirst",
                "Reads the first node matching search criteria",
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
                    return ReadFirstNode(sdk, req);
                });

            server.RegisterLiteGraphTool(
                "node_enumerate",
                "Enumerates nodes with pagination and filtering",
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
                    if (query.GraphGUID == null)
                        throw new ArgumentException("query.GraphGUID is required.");

                    return EnumerateNodes(sdk, query);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers node methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("node_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                string name = nameProp.GetString()!;
                Node node = new Node { TenantGUID = tenantGuid, GraphGUID = graphGuid, Name = name };
                return CreateNode(sdk, tenantGuid, graphGuid, node);
            });

            server.RegisterLiteGraphMethod("node_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("nodeGuid", out JsonElement nodeGuidProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and node GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                Guid nodeGuid = Guid.Parse(nodeGuidProp.GetString()!);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadNode(sdk, tenantGuid, graphGuid, nodeGuid, includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_traverse", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                Expr? edgeFilter = null;
                if (args.Value.TryGetProperty("edgeFilter", out JsonElement edgeFilterProp))
                {
                    string edgeFilterJson = edgeFilterProp.GetString() ?? throw new ArgumentException("Edge filter JSON string cannot be null");
                    edgeFilter = Serializer.DeserializeJson<Expr>(edgeFilterJson);
                }

                Expr? nodeFilter = null;
                if (args.Value.TryGetProperty("nodeFilter", out JsonElement nodeFilterProp))
                {
                    string nodeFilterJson = nodeFilterProp.GetString() ?? throw new ArgumentException("Node filter JSON string cannot be null");
                    nodeFilter = Serializer.DeserializeJson<Expr>(nodeFilterJson);
                }

                return ReadRoutes(sdk, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, edgeFilter, nodeFilter);
            });

            server.RegisterLiteGraphMethod("node_parents", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadParents(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterLiteGraphMethod("node_children", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadChildren(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterLiteGraphMethod("node_neighbors", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadNeighbors(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterLiteGraphMethod("node_deleteall", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    DeleteAllNodesInGraph(sdk, tenantGuid, graphGuid);
                    return true;
            });

            server.RegisterLiteGraphMethod("node_deletemany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("nodeGuids array is required");

                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());

                    DeleteNodes(sdk, tenantGuid, graphGuid, nodeGuids);
                    return true;
            });

            server.RegisterLiteGraphMethod("node_readallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadAllNodesInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_readallingraph", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadAllNodesInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_readmostconnected", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadMostConnectedNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_readleastconnected", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                    return ReadLeastConnectedNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_deleteallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    DeleteAllNodesInTenant(sdk, tenantGuid);
                    return true;
            });

            server.RegisterLiteGraphMethod("node_readallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                return ReadAllNodesInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_readallingraph", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                return ReadAllNodesInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_readmostconnected", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                return ReadMostConnectedNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_readleastconnected", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData");
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates");
                return ReadLeastConnectedNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_deleteallintenant", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllNodesInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterLiteGraphMethod("node_createmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodes", out JsonElement nodesProp))
                    throw new ArgumentException("Nodes array is required");

                string nodesJson = nodesProp.GetString() ?? throw new ArgumentException("Nodes JSON string cannot be null");
                List<Node> nodes = Serializer.DeserializeJson<List<Node>>(nodesJson);
                    return CreateNodes(sdk, tenantGuid, graphGuid, nodes);
            });

            server.RegisterLiteGraphMethod("node_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Node GUIDs array is required");

                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadNodesByGuids(sdk, tenantGuid, graphGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("node", out JsonElement nodeProp))
                    throw new ArgumentException("Node JSON string is required");
                string nodeJson = nodeProp.GetString() ?? throw new ArgumentException("Node JSON string cannot be null");
                Node node = Serializer.DeserializeJson<Node>(nodeJson);
                return UpdateNode(sdk, node);
            });

            server.RegisterLiteGraphMethod("node_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNode(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterLiteGraphMethod("node_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    return NodeExists(sdk, tenantGuid, graphGuid, nodeGuid).ToString().ToLowerInvariant();
            });

            server.RegisterLiteGraphMethod("node_search", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    return SearchNodes(sdk, req);
            });

            server.RegisterLiteGraphMethod("node_readfirst", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    return ReadFirstNode(sdk, req);
            });

            server.RegisterLiteGraphMethod("node_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                if (query.GraphGUID == null)
                    throw new ArgumentException("query.GraphGUID is required.");

                    return EnumerateNodes(sdk, query);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers node methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("node_create", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                string name = nameProp.GetString()!;
                Node node = new Node { TenantGUID = tenantGuid, GraphGUID = graphGuid, Name = name };
                return CreateNode(sdk, tenantGuid, graphGuid, node);
            });

            server.RegisterLiteGraphMethod("node_get", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("nodeGuid", out JsonElement nodeGuidProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and node GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                Guid nodeGuid = Guid.Parse(nodeGuidProp.GetString()!);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadNode(sdk, tenantGuid, graphGuid, nodeGuid, includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_all", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                return ReadNodes(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_traverse", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                Expr? edgeFilter = null;
                if (args.Value.TryGetProperty("edgeFilter", out JsonElement edgeFilterProp))
                {
                    string edgeFilterJson = edgeFilterProp.GetString() ?? throw new ArgumentException("Edge filter JSON string cannot be null");
                    edgeFilter = Serializer.DeserializeJson<Expr>(edgeFilterJson);
                }

                Expr? nodeFilter = null;
                if (args.Value.TryGetProperty("nodeFilter", out JsonElement nodeFilterProp))
                {
                    string nodeFilterJson = nodeFilterProp.GetString() ?? throw new ArgumentException("Node filter JSON string cannot be null");
                    nodeFilter = Serializer.DeserializeJson<Expr>(nodeFilterJson);
                }

                return ReadRoutes(sdk, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, edgeFilter, nodeFilter);
            });

            server.RegisterLiteGraphMethod("node_parents", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadParents(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterLiteGraphMethod("node_children", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadChildren(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterLiteGraphMethod("node_neighbors", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadNeighbors(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterLiteGraphMethod("node_deleteall", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllNodesInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterLiteGraphMethod("node_deletemany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("nodeGuids array is required");

                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());

                DeleteNodes(sdk, tenantGuid, graphGuid, nodeGuids);
                return true;
            });

            server.RegisterLiteGraphMethod("node_createmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodes", out JsonElement nodesProp))
                    throw new ArgumentException("Nodes array is required");

                string nodesJson = nodesProp.GetString() ?? throw new ArgumentException("Nodes JSON string cannot be null");
                List<Node> nodes = Serializer.DeserializeJson<List<Node>>(nodesJson);
                    return CreateNodes(sdk, tenantGuid, graphGuid, nodes);
            });

            server.RegisterLiteGraphMethod("node_getmany", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Node GUIDs array is required");

                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    return ReadNodesByGuids(sdk, tenantGuid, graphGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args), includeData, includeSubordinates);
            });

            server.RegisterLiteGraphMethod("node_update", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("node", out JsonElement nodeProp))
                    throw new ArgumentException("Node JSON string is required");
                string nodeJson = nodeProp.GetString() ?? throw new ArgumentException("Node JSON string cannot be null");
                Node node = Serializer.DeserializeJson<Node>(nodeJson);
                return UpdateNode(sdk, node);
            });

            server.RegisterLiteGraphMethod("node_delete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNode(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterLiteGraphMethod("node_exists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    return NodeExists(sdk, tenantGuid, graphGuid, nodeGuid).ToString().ToLowerInvariant();
            });

            server.RegisterLiteGraphMethod("node_search", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    return SearchNodes(sdk, req);
            });

            server.RegisterLiteGraphMethod("node_readfirst", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    return ReadFirstNode(sdk, req);
            });

            server.RegisterLiteGraphMethod("node_enumerate", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                if (query.GraphGUID == null)
                    throw new ArgumentException("query.GraphGUID is required.");

                    return EnumerateNodes(sdk, query);
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateNode(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Node node)
        {
            string body = Serializer.SerializeJson(node, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes",
                body);
        }

        private static string ReadNode(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
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
                + "/nodes/"
                + LiteGraphMcpRestProxy.Escape(nodeGuid)
                + "?incldata="
                + includeData.ToString().ToLowerInvariant()
                + "&inclsub="
                + includeSubordinates.ToString().ToLowerInvariant());
        }

        private static string ReadNodes(
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
                NodeCollectionPath(tenantGuid, graphGuid) + BuildReadQuery(order, skip, maxResults, continuationToken, includeData, includeSubordinates));
        }

        private static string ReadRoutes(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr? edgeFilter,
            Expr? nodeFilter)
        {
            // Filters are left out when not set, as the serializer leaves out null properties.
            Dictionary<string, object> request = new Dictionary<string, object>
            {
                { "From", fromNodeGuid },
                { "To", toNodeGuid }
            };
            if (edgeFilter != null) request.Add("EdgeFilter", edgeFilter);
            if (nodeFilter != null) request.Add("NodeFilter", nodeFilter);

            string body = Serializer.SerializeJson(request, false);

            string response = LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/routes",
                body);
            RouteResponse routeResponse = Serializer.DeserializeJson<RouteResponse>(response);
            return Serializer.SerializeJson(routeResponse?.Routes ?? new List<RouteDetail>(), true);
        }

        private static string ReadParents(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodePath(tenantGuid, graphGuid, nodeGuid) + "/parents" + BuildReadQuery(order, skip, maxResults, null, false, false));
        }

        private static string ReadChildren(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodePath(tenantGuid, graphGuid, nodeGuid) + "/children" + BuildReadQuery(order, skip, maxResults, null, false, false));
        }

        private static string ReadNeighbors(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodePath(tenantGuid, graphGuid, nodeGuid) + "/neighbors" + BuildReadQuery(order, skip, maxResults, null, false, false));
        }

        private static string ReadAllNodesInTenant(
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
                + "/nodes/all"
                + BuildReadQuery(order, skip, maxResults, continuationToken, includeData, includeSubordinates));
        }

        private static string ReadAllNodesInGraph(
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
                NodeCollectionPath(tenantGuid, graphGuid) + "/all" + BuildReadQuery(order, skip, maxResults, continuationToken, includeData, includeSubordinates));
        }

        private static string ReadMostConnectedNodes(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodeCollectionPath(tenantGuid, graphGuid) + "/mostconnected" + BuildReadQuery(order, skip, maxResults, null, includeData, includeSubordinates));
        }

        private static string ReadLeastConnectedNodes(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodeCollectionPath(tenantGuid, graphGuid) + "/leastconnected" + BuildReadQuery(order, skip, maxResults, null, includeData, includeSubordinates));
        }

        private static string CreateNodes(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, List<Node> nodes)
        {
            string body = Serializer.SerializeJson(nodes, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                NodeCollectionPath(tenantGuid, graphGuid) + "/bulk",
                body);
        }

        private static string ReadNodesByGuids(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            List<Guid> nodeGuids,
            int maxResults,
            bool includeData,
            bool includeSubordinates)
        {
            if (nodeGuids == null) throw new ArgumentNullException(nameof(nodeGuids));
            if (nodeGuids.Count == 0) throw new ArgumentException("At least one node GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                NodeCollectionPath(tenantGuid, graphGuid)
                + "?guids="
                + String.Join(",", nodeGuids)
                + "&max-keys="
                + maxResults
                + "&incldata="
                + includeData.ToString().ToLowerInvariant()
                + "&inclsub="
                + includeSubordinates.ToString().ToLowerInvariant());
        }

        private static string SearchNodes(LiteGraphSdk sdk, SearchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string body = Serializer.SerializeJson(request, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                NodeCollectionPath(request.TenantGUID, request.GraphGUID) + "/search",
                body);
        }

        private static string ReadFirstNode(LiteGraphSdk sdk, SearchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string body = Serializer.SerializeJson(request, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                NodeCollectionPath(request.TenantGUID, request.GraphGUID) + "/first",
                body);
        }

        private static string EnumerateNodes(LiteGraphSdk sdk, EnumerationRequest query)
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
                + "/nodes",
                body);
        }

        private static string UpdateNode(LiteGraphSdk sdk, Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            string body = Serializer.SerializeJson(node, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(node.TenantGUID)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(node.GraphGUID)
                + "/nodes/"
                + LiteGraphMcpRestProxy.Escape(node.GUID),
                body);
        }

        private static void DeleteNode(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes/"
                + LiteGraphMcpRestProxy.Escape(nodeGuid));
        }

        private static bool NodeExists(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return LiteGraphMcpRestProxy.HeadExists(sdk, NodePath(tenantGuid, graphGuid, nodeGuid));
        }

        private static void DeleteAllNodesInGraph(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                NodeCollectionPath(tenantGuid, graphGuid) + "/all");
        }

        private static void DeleteNodes(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            string body = Serializer.SerializeJson(nodeGuids, false);
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                NodeCollectionPath(tenantGuid, graphGuid) + "/bulk",
                body);
        }

        private static void DeleteAllNodesInTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/nodes/all");
        }

        private static string NodeCollectionPath(Guid tenantGuid, Guid graphGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes";
        }

        private static string NodePath(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return NodeCollectionPath(tenantGuid, graphGuid)
                + "/"
                + LiteGraphMcpRestProxy.Escape(nodeGuid);
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
