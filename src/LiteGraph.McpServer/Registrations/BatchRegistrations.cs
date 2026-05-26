namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for Batch operations.
    /// </summary>
    public static class BatchRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers batch tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "batch/existence",
                "Checks existence of multiple nodes, edges, vectors, or edges between nodes",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodes = new { type = "array", items = new { type = "string" }, description = "List of node GUIDs to check" },
                        edges = new { type = "array", items = new { type = "string" }, description = "List of edge GUIDs to check" },
                        vectors = new { type = "array", items = new { type = "string" }, description = "List of vector GUIDs to check" },
                        edgesBetween = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    from = new { type = "string", description = "From node GUID" },
                                    to = new { type = "string", description = "To node GUID" }
                                },
                                required = new[] { "from", "to" }
                            },
                            description = "List of edge pairs to check (from/to node GUIDs)"
                        }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");

                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

                    ExistenceRequest request = new ExistenceRequest();

                    if (args.Value.TryGetProperty("nodes", out JsonElement nodesProp) && nodesProp.ValueKind == JsonValueKind.Array)
                    {
                        request.Nodes = Serializer.DeserializeJson<List<Guid>>(nodesProp.GetRawText());
                    }

                    if (args.Value.TryGetProperty("edges", out JsonElement edgesProp) && edgesProp.ValueKind == JsonValueKind.Array)
                    {
                        request.Edges = Serializer.DeserializeJson<List<Guid>>(edgesProp.GetRawText());
                    }

                    if (args.Value.TryGetProperty("vectors", out JsonElement vectorsProp) && vectorsProp.ValueKind == JsonValueKind.Array)
                    {
                        request.Vectors = Serializer.DeserializeJson<List<Guid>>(vectorsProp.GetRawText());
                    }

                    if (args.Value.TryGetProperty("edgesBetween", out JsonElement edgesBetweenProp) && edgesBetweenProp.ValueKind == JsonValueKind.Array)
                    {
                        request.EdgesBetween = Serializer.DeserializeJson<List<EdgeBetween>>(edgesBetweenProp.GetRawText());
                    }

                    if (!request.ContainsExistenceRequest())
                        throw new ArgumentException("At least one of nodes, edges, vectors, or edgesBetween must be provided");

                    return ReadExistence(sdk, tenantGuid, graphGuid, request);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers batch methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("batch/existence", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");

                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

                ExistenceRequest request = new ExistenceRequest();

                if (args.Value.TryGetProperty("nodes", out JsonElement nodesProp) && nodesProp.ValueKind == JsonValueKind.Array)
                {
                    request.Nodes = Serializer.DeserializeJson<List<Guid>>(nodesProp.GetRawText());
                }

                if (args.Value.TryGetProperty("edges", out JsonElement edgesProp) && edgesProp.ValueKind == JsonValueKind.Array)
                {
                    request.Edges = Serializer.DeserializeJson<List<Guid>>(edgesProp.GetRawText());
                }

                if (args.Value.TryGetProperty("vectors", out JsonElement vectorsProp) && vectorsProp.ValueKind == JsonValueKind.Array)
                {
                    request.Vectors = Serializer.DeserializeJson<List<Guid>>(vectorsProp.GetRawText());
                }

                if (args.Value.TryGetProperty("edgesBetween", out JsonElement edgesBetweenProp) && edgesBetweenProp.ValueKind == JsonValueKind.Array)
                {
                    request.EdgesBetween = Serializer.DeserializeJson<List<EdgeBetween>>(edgesBetweenProp.GetRawText());
                }

                if (!request.ContainsExistenceRequest())
                    throw new ArgumentException("At least one of nodes, edges, vectors, or edgesBetween must be provided");

                return ReadExistence(sdk, tenantGuid, graphGuid, request);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers batch methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("batch/existence", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");

                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

                ExistenceRequest request = new ExistenceRequest();

                if (args.Value.TryGetProperty("nodes", out JsonElement nodesProp) && nodesProp.ValueKind == JsonValueKind.Array)
                {
                    request.Nodes = Serializer.DeserializeJson<List<Guid>>(nodesProp.GetRawText());
                }

                if (args.Value.TryGetProperty("edges", out JsonElement edgesProp) && edgesProp.ValueKind == JsonValueKind.Array)
                {
                    request.Edges = Serializer.DeserializeJson<List<Guid>>(edgesProp.GetRawText());
                }

                if (args.Value.TryGetProperty("vectors", out JsonElement vectorsProp) && vectorsProp.ValueKind == JsonValueKind.Array)
                {
                    request.Vectors = Serializer.DeserializeJson<List<Guid>>(vectorsProp.GetRawText());
                }

                if (args.Value.TryGetProperty("edgesBetween", out JsonElement edgesBetweenProp) && edgesBetweenProp.ValueKind == JsonValueKind.Array)
                {
                    request.EdgesBetween = Serializer.DeserializeJson<List<EdgeBetween>>(edgesBetweenProp.GetRawText());
                }

                if (!request.ContainsExistenceRequest())
                    throw new ArgumentException("At least one of nodes, edges, vectors, or edgesBetween must be provided");

                return ReadExistence(sdk, tenantGuid, graphGuid, request);
            });
        }

        #endregion

        #region Private-Methods

        private static string ReadExistence(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, ExistenceRequest request)
        {
            string body = Serializer.SerializeJson(request, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/existence",
                body);
        }

        #endregion
    }
}
