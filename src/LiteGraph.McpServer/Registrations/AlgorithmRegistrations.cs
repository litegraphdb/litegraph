namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for graph algorithm operations (run, export projection, import results).
    /// These tools proxy the LiteGraph REST endpoints under the caller's RBAC.
    /// </summary>
    public static class AlgorithmRegistrations
    {
        #region Private-Members

        private static readonly HttpClient _Http = new HttpClient();

        #endregion

        #region HTTP-Tools

        /// <summary>
        /// Registers graph algorithm tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "algorithm/run",
                "Runs a graph algorithm (DegreeCentrality, PageRank, WeaklyConnectedComponents, StronglyConnectedComponents, LabelPropagation) over a single graph. Set writeBack=true (requires write permission) to store per-node results into node data.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        request = new { type = "object", description = "GraphAlgorithmRequest object; used when provided" },
                        algorithmType = new { type = "string", description = "Algorithm name, used when request is omitted" },
                        writeBack = new { type = "boolean", description = "Write per-node results back into node data" },
                        maxResults = new { type = "integer", description = "Maximum per-node results to return" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) => ExecuteRun(args, sdk));

            server.RegisterTool(
                "algorithm/export",
                "Exports a graph as a portable projection (NodeLinkJson, EdgeList, or Graphml) for external computation in engines such as rustworkx or NetworkX.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        format = new { type = "string", description = "NodeLinkJson (default), EdgeList, or Graphml" },
                        attributes = new { type = "string", description = "None, Meta (default), or Full" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) => ExecuteExport(args, sdk));

            server.RegisterTool(
                "algorithm/import",
                "Imports externally computed per-node values back onto graph nodes, writing them into node data. Requires write permission.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        request = new { type = "object", description = "GraphAlgorithmImportRequest object with a Values map of node GUID to property/value pairs" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "request" }
                },
                (args) => ExecuteImport(args, sdk));
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers graph algorithm methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("algorithm/run", (args) => ExecuteRun(args, sdk));
            server.RegisterMethod("algorithm/export", (args) => ExecuteExport(args, sdk));
            server.RegisterMethod("algorithm/import", (args) => ExecuteImport(args, sdk));
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers graph algorithm methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("algorithm/run", (args) => ExecuteRun(args, sdk));
            server.RegisterMethod("algorithm/export", (args) => ExecuteExport(args, sdk));
            server.RegisterMethod("algorithm/import", (args) => ExecuteImport(args, sdk));
        }

        #endregion

        #region Private-Methods

        private static string ExecuteRun(JsonElement? args, LiteGraphSdk sdk)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

            JsonObject requestObject;
            if (args.Value.TryGetProperty("request", out JsonElement requestProp))
            {
                requestObject = ReadJsonObject(requestProp, "request");
            }
            else
            {
                requestObject = new JsonObject();
                if (args.Value.TryGetProperty("algorithmType", out JsonElement algoProp) && algoProp.ValueKind == JsonValueKind.String)
                    requestObject["AlgorithmType"] = algoProp.GetString();
                if (args.Value.TryGetProperty("writeBack", out JsonElement wbProp) &&
                    (wbProp.ValueKind == JsonValueKind.True || wbProp.ValueKind == JsonValueKind.False))
                    requestObject["WriteBack"] = wbProp.GetBoolean();
                if (args.Value.TryGetProperty("maxResults", out JsonElement mrProp) && mrProp.ValueKind == JsonValueKind.Number)
                    requestObject["MaxResults"] = mrProp.GetInt32();
            }

            string url = BuildUrl(sdk.Endpoint, tenantGuid, graphGuid, "/algorithms");
            return PostJson(url, requestObject.ToJsonString(), sdk);
        }

        private static string ExecuteImport(JsonElement? args, LiteGraphSdk sdk)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

            if (!args.Value.TryGetProperty("request", out JsonElement requestProp))
                throw new ArgumentException("request is required");

            JsonObject requestObject = ReadJsonObject(requestProp, "request");
            string url = BuildUrl(sdk.Endpoint, tenantGuid, graphGuid, "/algorithms/import");
            return PostJson(url, requestObject.ToJsonString(), sdk);
        }

        private static string ExecuteExport(JsonElement? args, LiteGraphSdk sdk)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");

            string format = "NodeLinkJson";
            string attributes = "Meta";
            if (args.Value.TryGetProperty("format", out JsonElement formatProp) && formatProp.ValueKind == JsonValueKind.String)
                format = formatProp.GetString() ?? format;
            if (args.Value.TryGetProperty("attributes", out JsonElement attrProp) && attrProp.ValueKind == JsonValueKind.String)
                attributes = attrProp.GetString() ?? attributes;

            string url = BuildUrl(sdk.Endpoint, tenantGuid, graphGuid, "/export/projection")
                + "?format=" + Uri.EscapeDataString(format)
                + "&attributes=" + Uri.EscapeDataString(attributes);

            return GetText(url, sdk);
        }

        private static string BuildUrl(string endpoint, Guid tenantGuid, Guid graphGuid, string suffix)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));

            return endpoint.TrimEnd('/')
                + "/v1.0/tenants/"
                + Uri.EscapeDataString(tenantGuid.ToString())
                + "/graphs/"
                + Uri.EscapeDataString(graphGuid.ToString())
                + suffix;
        }

        private static string PostJson(string url, string json, LiteGraphSdk sdk)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                if (!String.IsNullOrEmpty(sdk.BearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sdk.BearerToken);

                using (HttpResponseMessage response = _Http.Send(request))
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException(
                            "LiteGraph algorithm endpoint returned " + (int)response.StatusCode + " " + response.ReasonPhrase + ": " + body);
                    return body;
                }
            }
        }

        private static string GetText(string url, LiteGraphSdk sdk)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (!String.IsNullOrEmpty(sdk.BearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sdk.BearerToken);

                using (HttpResponseMessage response = _Http.Send(request))
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException(
                            "LiteGraph export endpoint returned " + (int)response.StatusCode + " " + response.ReasonPhrase + ": " + body);
                    return body;
                }
            }
        }

        private static JsonObject ReadJsonObject(JsonElement prop, string propertyName)
        {
            string json = prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? String.Empty
                : prop.GetRawText();

            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException(propertyName + " cannot be empty");

            JsonNode? node = JsonNode.Parse(json);
            if (node is JsonObject obj) return obj;

            throw new ArgumentException(propertyName + " must be a JSON object");
        }

        #endregion
    }
}
