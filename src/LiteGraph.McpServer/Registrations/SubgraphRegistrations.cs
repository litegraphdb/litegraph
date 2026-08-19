namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// MCP tool registrations for subgraph JSONL import and export.
    /// </summary>
    public static class SubgraphRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Register HTTP tools.
        /// </summary>
        /// <param name="server">MCP HTTP server.</param>
        /// <param name="sdk">LiteGraph SDK.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            server.RegisterTool(
                "graph/exportjsonl",
                "Exports an entire graph as JSONL (also usable as a provider-agnostic backup)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        includeData = new { type = "boolean", description = "Include object data (default: false)" },
                        includeSubordinates = new { type = "boolean", description = "Include labels, tags, and vectors (default: false)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) => ExportJsonl(sdk, args));

            server.RegisterTool(
                "graph/exportsubgraphjsonl",
                "Exports a filtered, directional subgraph as JSONL",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        request = new { type = "string", description = "SubgraphExtractionRequest as a JSON string (StartNodeGUIDs, MaxDepth, Direction, filters, etc.)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "request" }
                },
                (args) => ExportSubgraphJsonl(sdk, args));

            server.RegisterTool(
                "graph/importjsonl",
                "Imports JSONL into a new graph (omit graphGuid) or merges into an existing graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Target graph GUID for a merge; omit to import as a new graph" },
                        jsonl = new { type = "string", description = "JSONL content" },
                        guidStrategy = new { type = "string", description = "preserve | regenerate | skip | overwrite (default: regenerate)" },
                        onError = new { type = "string", description = "abort | skip (default: abort)" },
                        batchSize = new { type = "integer", description = "Batch size (default: 1000)" }
                    },
                    required = new[] { "tenantGuid", "jsonl" }
                },
                (args) => ImportJsonl(sdk, args));
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Register TCP methods.
        /// </summary>
        /// <param name="server">MCP TCP server.</param>
        /// <param name="sdk">LiteGraph SDK.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            server.RegisterMethod("graph/exportjsonl", (args) => ExportJsonl(sdk, args));
            server.RegisterMethod("graph/exportsubgraphjsonl", (args) => ExportSubgraphJsonl(sdk, args));
            server.RegisterMethod("graph/importjsonl", (args) => ImportJsonl(sdk, args));
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Register WebSocket methods.
        /// </summary>
        /// <param name="server">MCP WebSocket server.</param>
        /// <param name="sdk">LiteGraph SDK.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            server.RegisterMethod("graph/exportjsonl", (args) => ExportJsonl(sdk, args));
            server.RegisterMethod("graph/exportsubgraphjsonl", (args) => ExportSubgraphJsonl(sdk, args));
            server.RegisterMethod("graph/importjsonl", (args) => ImportJsonl(sdk, args));
        }

        #endregion

        #region Private-Methods

        private static object ExportJsonl(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
            bool includeData = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeData", false);
            bool includeSubordinates = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "includeSubordinates", false);
            string jsonl = sdk.Graph.ExportGraphToJsonl(tenantGuid, graphGuid, includeData, includeSubordinates).GetAwaiter().GetResult();
            return jsonl ?? string.Empty;
        }

        private static object ExportSubgraphJsonl(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

            if (!args.Value.TryGetProperty("request", out JsonElement requestProp))
                throw new ArgumentException("A subgraph extraction request is required.");
            string requestJson = (requestProp.ValueKind == JsonValueKind.String ? requestProp.GetString() : requestProp.GetRawText()) ?? "{}";
            SubgraphExtractionRequest request = Serializer.DeserializeJson<SubgraphExtractionRequest>(requestJson) ?? new SubgraphExtractionRequest();

            string jsonl = sdk.Graph.ExportSubgraphToJsonl(tenantGuid, graphGuid, request).GetAwaiter().GetResult();
            return jsonl ?? string.Empty;
        }

        private static object ImportJsonl(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");

            if (!args.Value.TryGetProperty("jsonl", out JsonElement jsonlProp))
                throw new ArgumentException("JSONL content is required.");
            string jsonl = jsonlProp.GetString() ?? string.Empty;

            GraphImportGuidStrategyEnum guidStrategy = GraphImportGuidStrategyEnum.Regenerate;
            if (args.Value.TryGetProperty("guidStrategy", out JsonElement gsProp) && gsProp.ValueKind == JsonValueKind.String)
                Enum.TryParse<GraphImportGuidStrategyEnum>(gsProp.GetString(), true, out guidStrategy);

            GraphImportErrorPolicyEnum onError = GraphImportErrorPolicyEnum.Abort;
            if (args.Value.TryGetProperty("onError", out JsonElement oeProp) && oeProp.ValueKind == JsonValueKind.String)
                Enum.TryParse<GraphImportErrorPolicyEnum>(oeProp.GetString(), true, out onError);

            int batchSize = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "batchSize", 1000);
            if (batchSize < 1) batchSize = 1000;

            GraphImportResult result;
            if (graphGuid.HasValue)
                result = sdk.Graph.ImportGraphFromJsonl(tenantGuid, graphGuid.Value, jsonl, guidStrategy, onError, batchSize).GetAwaiter().GetResult();
            else
                result = sdk.Graph.ImportGraphAsNewFromJsonl(tenantGuid, jsonl, guidStrategy, onError, batchSize).GetAwaiter().GetResult();

            return Serializer.SerializeJson(result, true);
        }

        #endregion
    }
}
