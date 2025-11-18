namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph;
    using LiteGraph.Serialization;
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
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphClient client, Serializer serializer)
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
                    Node created = client.Node.Create(node).GetAwaiter().GetResult();
                    return serializer.SerializeJson(created, true);
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
                    bool includeData = args.Value.TryGetProperty("includeData", out JsonElement includeDataProp) && includeDataProp.GetBoolean();
                    bool includeSubordinates = args.Value.TryGetProperty("includeSubordinates", out JsonElement includeSubProp) && includeSubProp.GetBoolean();

                    Node node = client.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                    return node != null ? serializer.SerializeJson(node, true) : "null";
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
                    bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                    bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                    List<Node> nodes = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadAllInGraph(tenantGuid, graphGuid, EnumerationOrderEnum.CreatedDescending, 0, includeData, includeSubordinates));
                    return serializer.SerializeJson(nodes, true);
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
                        edgeFilter = new { type = "object", description = "Edge filter expression (optional)" },
                        nodeFilter = new { type = "object", description = "Node filter expression (optional)" }
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
                    List<RouteDetail> routes = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, null, null));
                    return serializer.SerializeJson(routes, true);
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
                    List<Node> parents = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadParents(tenantGuid, graphGuid, nodeGuid));
                    return serializer.SerializeJson(parents, true);
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
                    List<Node> children = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid));
                    return serializer.SerializeJson(children, true);
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
                    List<Node> neighbors = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid));
                    return serializer.SerializeJson(neighbors, true);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers node methods on TCP server.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphClient client, Serializer serializer)
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
                Node created = client.Node.Create(node).GetAwaiter().GetResult();
                return serializer.SerializeJson(created, true);
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
                bool includeData = args.Value.TryGetProperty("includeData", out JsonElement includeDataProp) && includeDataProp.GetBoolean();
                bool includeSubordinates = args.Value.TryGetProperty("includeSubordinates", out JsonElement includeSubProp) && includeSubProp.GetBoolean();

                Node node = client.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                return node != null ? serializer.SerializeJson(node, true) : "null";
            });

            server.RegisterMethod("node/all", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                List<Node> nodes = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadAllInGraph(tenantGuid, graphGuid, EnumerationOrderEnum.CreatedDescending, 0, includeData, includeSubordinates));
                return serializer.SerializeJson(nodes, true);
            });

            server.RegisterMethod("node/traverse", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                List<RouteDetail> routes = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, null, null));
                return serializer.SerializeJson(routes, true);
            });

            server.RegisterMethod("node/parents", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> parents = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadParents(tenantGuid, graphGuid, nodeGuid));
                return serializer.SerializeJson(parents, true);
            });

            server.RegisterMethod("node/children", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> children = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid));
                return serializer.SerializeJson(children, true);
            });

            server.RegisterMethod("node/neighbors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> neighbors = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid));
                return serializer.SerializeJson(neighbors, true);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers node methods on WebSocket server.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphClient client, Serializer serializer)
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
                Node created = client.Node.Create(node).GetAwaiter().GetResult();
                return serializer.SerializeJson(created, true);
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
                bool includeData = args.Value.TryGetProperty("includeData", out JsonElement includeDataProp) && includeDataProp.GetBoolean();
                bool includeSubordinates = args.Value.TryGetProperty("includeSubordinates", out JsonElement includeSubProp) && includeSubProp.GetBoolean();

                Node node = client.Node.ReadByGuid(tenantGuid, graphGuid, nodeGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
                return node != null ? serializer.SerializeJson(node, true) : "null";
            });

            server.RegisterMethod("node/all", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
                bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
                List<Node> nodes = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadAllInGraph(tenantGuid, graphGuid, EnumerationOrderEnum.CreatedDescending, 0, includeData, includeSubordinates));
                return serializer.SerializeJson(nodes, true);
            });

            server.RegisterMethod("node/traverse", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid fromNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "fromNodeGuid");
                Guid toNodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "toNodeGuid");
                SearchTypeEnum searchType = Enum.Parse<SearchTypeEnum>(args.Value.GetProperty("searchType").GetString()!);
                List<RouteDetail> routes = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadRoutes(searchType, tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, null, null));
                return serializer.SerializeJson(routes, true);
            });

            server.RegisterMethod("node/parents", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> parents = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadParents(tenantGuid, graphGuid, nodeGuid));
                return serializer.SerializeJson(parents, true);
            });

            server.RegisterMethod("node/children", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> children = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadChildren(tenantGuid, graphGuid, nodeGuid));
                return serializer.SerializeJson(children, true);
            });

            server.RegisterMethod("node/neighbors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<Node> neighbors = LiteGraphMcpServerHelpers.ToListSync(client.Node.ReadNeighbors(tenantGuid, graphGuid, nodeGuid));
                return serializer.SerializeJson(neighbors, true);
            });
        }

        #endregion
    }
}
