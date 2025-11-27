namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

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
            server.RegisterTool(
                "node/create",
                "Creates a new node in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        name = new { type = "string", description = "Node name" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "name" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("name", out JsonElement nameProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and name are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    string name = nameProp.GetString()!;
                    Node node = new Node { TenantGUID = tenantGuid, GraphGUID = graphGuid, Name = name };
                    Node created = sdk.Node.Create(node).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "node/get",
                "Reads a node by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        includeData = new { type = "boolean", description = "Include node data" },
                        includeSubordinates = new { type = "boolean", description = "Include labels, tags, vectors" }
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

                    Node node = sdk.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                    return node != null ? Serializer.SerializeJson(node, true) : "null";
                });

            server.RegisterTool(
                "node/all",
                "Lists all nodes in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        includeData = new { type = "boolean", description = "Include node data" },
                        includeSubordinates = new { type = "boolean", description = "Include labels, tags, vectors" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<Node> nodes = sdk.Node.ReadMany(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(nodes, true);
                });

            server.RegisterTool(
                "node/traverse",
                "Finds routes/paths between two nodes in a graph using depth-first search",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        fromNodeGuid = new { type = "string", description = "Source node GUID" },
                        toNodeGuid = new { type = "string", description = "Target node GUID" },
                        searchType = new { type = "string", description = "Search type: DepthFirstSearch" },
                        edgeFilter = new { type = "string", description = "Edge filter expression serialized as JSON string using Serializer (optional)" },
                        nodeFilter = new { type = "string", description = "Node filter expression serialized as JSON string using Serializer (optional)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "fromNodeGuid", "toNodeGuid", "searchType" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                    Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                    SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                    RouteResponse routeResponse = sdk.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, null, null).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(routeResponse?.Routes ?? new List<RouteDetail>(), true);
                });

            server.RegisterTool(
                "node/parents",
                "Gets parent nodes (nodes that have edges connecting to this node)",
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
                    List<Node> parents = sdk.Node.ReadParents(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(parents ?? new List<Node>(), true);

                });

            server.RegisterTool(
                "node/children",
                "Gets child nodes (nodes to which this node has connecting edges)",
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
                    List<Node> children = sdk.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(children ?? new List<Node>(), true);
                });

            server.RegisterTool(
                "node/deleteall",
                "Deletes all nodes in a graph",
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
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp))
                        throw new ArgumentException("Tenant GUID and graph GUID are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    sdk.Node.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "node/deletemany",
                "Deletes multiple nodes by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuids = new { type = "array", items = new { type = "string" }, description = "Array of node GUIDs to delete" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                        !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                        !args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                        throw new ArgumentException("Tenant GUID, graph GUID, and nodeGuids array are required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                    List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());

                    sdk.Node.DeleteMany(tenantGuid, graphGuid, nodeGuids).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "node/neighbors",
                "Gets neighbor nodes (all connected nodes regardless of edge direction)",
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
                    List<Node> neighbors = sdk.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(neighbors ?? new List<Node>(), true);
                });

            server.RegisterTool(
                "node/createmany",
                "Creates multiple nodes in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodes = new { type = "string", description = "Array of node objects serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodes" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    if (!args.Value.TryGetProperty("nodes", out JsonElement nodesProp))
                        throw new ArgumentException("Nodes array is required");

                    string nodesJson = nodesProp.GetString() ?? throw new ArgumentException("Nodes JSON string cannot be null");
                    List<Node> nodes = Serializer.DeserializeJson<List<Node>>(nodesJson);
                    List<Node> created = sdk.Node.CreateMany(tenantGuid, graphGuid, nodes).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "node/getmany",
                "Reads multiple nodes by their GUIDs",
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
                    if (!args.Value.TryGetProperty("nodeGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Node GUIDs array is required");

                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    List<Node> nodes = sdk.Node.ReadByGuids(tenantGuid, graphGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(nodes, true);
                });

            server.RegisterTool(
                "node/update",
                "Updates a node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        node = new { type = "string", description = "Node object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "node" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("node", out JsonElement nodeProp))
                        throw new ArgumentException("Node JSON string is required");
                    string nodeJson = nodeProp.GetString() ?? throw new ArgumentException("Node JSON string cannot be null");
                    Node node = Serializer.DeserializeJson<Node>(nodeJson);
                    Node updated = sdk.Node.Update(node).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
                });

            server.RegisterTool(
                "node/delete",
                "Deletes a single node by GUID",
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
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    sdk.Node.DeleteByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "node/exists",
                "Checks if a node exists by GUID",
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
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    bool exists = sdk.Node.ExistsByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "node/search",
                "Searches nodes with filters",
                new
                {
                    type = "object",
                    properties = new
                    {
                        searchRequest = new { type = "string", description = "Search request object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "searchRequest" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                        throw new ArgumentException("Search request is required");

                    string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                    SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    SearchResult result = sdk.Node.Search(req).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "node/readfirst",
                "Reads the first node matching search criteria",
                new
                {
                    type = "object",
                    properties = new
                    {
                        searchRequest = new { type = "string", description = "Search request object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "searchRequest" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                        throw new ArgumentException("Search request is required");

                    string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                    SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                    Node node = sdk.Node.ReadFirst(req).GetAwaiter().GetResult();
                    return node != null ? Serializer.SerializeJson(node, true) : "null";
                });

            server.RegisterTool(
                "node/enumerate",
                "Enumerates nodes with pagination and filtering",
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

                    EnumerationResult<Node> result = sdk.Node.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
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
            server.RegisterMethod("node/create", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                string name = nameProp.GetString()!;
                Node node = new Node { TenantGUID = tenantGuid, GraphGUID = graphGuid, Name = name };
                Node created = sdk.Node.Create(node).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("node/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("nodeGuid", out JsonElement nodeGuidProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and node GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                Guid nodeGuid = Guid.Parse(nodeGuidProp.GetString()!);
                Node node = sdk.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return node != null ? Serializer.SerializeJson(node, true) : "null";
            });

            server.RegisterMethod("node/all", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> nodes = sdk.Node.ReadMany(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(nodes, true);
            });

            server.RegisterMethod("node/traverse", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                RouteResponse routeResponse = sdk.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, null, null).GetAwaiter().GetResult();
                return Serializer.SerializeJson(routeResponse?.Routes ?? new List<RouteDetail>(), true);
            });

            server.RegisterMethod("node/parents", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> parents = sdk.Node.ReadParents(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(parents, true);
            });

            server.RegisterMethod("node/children", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> children = sdk.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(children, true);
            });

            server.RegisterMethod("node/neighbors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> neighbors = sdk.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(neighbors, true);
            });

            server.RegisterMethod("node/deleteall", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Node.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("node/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("nodeGuids array is required");

                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());

                sdk.Node.DeleteMany(tenantGuid, graphGuid, nodeGuids).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("node/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodes", out JsonElement nodesProp))
                    throw new ArgumentException("Nodes array is required");

                string nodesJson = nodesProp.GetString() ?? throw new ArgumentException("Nodes JSON string cannot be null");
                List<Node> nodes = Serializer.DeserializeJson<List<Node>>(nodesJson);
                List<Node> created = sdk.Node.CreateMany(tenantGuid, graphGuid, nodes).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("node/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Node GUIDs array is required");

                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Node> nodes = sdk.Node.ReadByGuids(tenantGuid, graphGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(nodes, true);
            });

            server.RegisterMethod("node/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("node", out JsonElement nodeProp))
                    throw new ArgumentException("Node JSON string is required");
                string nodeJson = nodeProp.GetString() ?? throw new ArgumentException("Node JSON string cannot be null");
                Node node = Serializer.DeserializeJson<Node>(nodeJson);
                Node updated = sdk.Node.Update(node).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("node/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                sdk.Node.DeleteByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("node/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                bool exists = sdk.Node.ExistsByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("node/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                SearchResult result = sdk.Node.Search(req).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("node/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                Node node = sdk.Node.ReadFirst(req).GetAwaiter().GetResult();
                return node != null ? Serializer.SerializeJson(node, true) : "null";
            });

            server.RegisterMethod("node/enumerate", (args) =>
            {
                EnumerationRequest query = new EnumerationRequest();
                if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                }

                EnumerationResult<Node> result = sdk.Node.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
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
            server.RegisterMethod("node/create", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("name", out JsonElement nameProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and name are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                string name = nameProp.GetString()!;
                Node node = new Node { TenantGUID = tenantGuid, GraphGUID = graphGuid, Name = name };
                Node created = sdk.Node.Create(node).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("node/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp) ||
                    !args.Value.TryGetProperty("graphGuid", out JsonElement graphGuidProp) ||
                    !args.Value.TryGetProperty("nodeGuid", out JsonElement nodeGuidProp))
                    throw new ArgumentException("Tenant GUID, graph GUID, and node GUID are required");

                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid graphGuid = Guid.Parse(graphGuidProp.GetString()!);
                Guid nodeGuid = Guid.Parse(nodeGuidProp.GetString()!);
                Node node = sdk.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return node != null ? Serializer.SerializeJson(node, true) : "null";
            });

            server.RegisterMethod("node/all", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> nodes = sdk.Node.ReadMany(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(nodes, true);
            });

            server.RegisterMethod("node/traverse", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                RouteResponse routeResponse = sdk.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, null, null).GetAwaiter().GetResult();
                return Serializer.SerializeJson(routeResponse?.Routes ?? new List<RouteDetail>(), true);
            });

            server.RegisterMethod("node/parents", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> parents = sdk.Node.ReadParents(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult(); ;
                return Serializer.SerializeJson(parents, true);
            });

            server.RegisterMethod("node/children", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> children = sdk.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(children, true);
            });

            server.RegisterMethod("node/neighbors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<Node> neighbors = sdk.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(neighbors, true);
            });

            server.RegisterMethod("node/deleteall", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Node.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("node/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement nodeGuidsProp))
                    throw new ArgumentException("nodeGuids array is required");

                List<Guid> nodeGuids = Serializer.DeserializeJson<List<Guid>>(nodeGuidsProp.GetRawText());

                sdk.Node.DeleteMany(tenantGuid, graphGuid, nodeGuids).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("node/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodes", out JsonElement nodesProp))
                    throw new ArgumentException("Nodes array is required");

                string nodesJson = nodesProp.GetString() ?? throw new ArgumentException("Nodes JSON string cannot be null");
                List<Node> nodes = Serializer.DeserializeJson<List<Node>>(nodesJson);
                List<Node> created = sdk.Node.CreateMany(tenantGuid, graphGuid, nodes).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("node/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                if (!args.Value.TryGetProperty("nodeGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Node GUIDs array is required");

                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<Node> nodes = sdk.Node.ReadByGuids(tenantGuid, graphGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(nodes, true);
            });

            server.RegisterMethod("node/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("node", out JsonElement nodeProp))
                    throw new ArgumentException("Node JSON string is required");
                string nodeJson = nodeProp.GetString() ?? throw new ArgumentException("Node JSON string cannot be null");
                Node node = Serializer.DeserializeJson<Node>(nodeJson);
                Node updated = sdk.Node.Update(node).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("node/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                sdk.Node.DeleteByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("node/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                bool exists = sdk.Node.ExistsByGuid(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("node/search", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                SearchResult result = sdk.Node.Search(req).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("node/readfirst", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("searchRequest", out JsonElement reqProp))
                    throw new ArgumentException("Search request is required");

                string reqJson = reqProp.GetString() ?? throw new ArgumentException("SearchRequest JSON string cannot be null");
                SearchRequest req = Serializer.DeserializeJson<SearchRequest>(reqJson);
                Node node = sdk.Node.ReadFirst(req).GetAwaiter().GetResult();
                return node != null ? Serializer.SerializeJson(node, true) : "null";
            });

            server.RegisterMethod("node/enumerate", (args) =>
            {
                EnumerationRequest query = new EnumerationRequest();
                if (args.HasValue && args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                }

                EnumerationResult<Node> result = sdk.Node.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });
        }

        #endregion
    }
}
