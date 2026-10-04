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
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for native graph query operations.
    /// </summary>
    public static class QueryRegistrations
    {
        #region Private-Members

        private static readonly HttpClient _Http = new HttpClient();

        #endregion

        #region HTTP-Tools

        /// <summary>
        /// Registers native graph query tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphTool(
                "graph_query",
                "Executes a native LiteGraph graph query against a single tenant and graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        request = new { type = "object", description = "GraphQueryRequest object with Query, Parameters, MaxResults, and TimeoutSeconds" },
                        query = new { type = "string", description = "Query text, used when request is omitted" },
                        parameters = new { type = "object", description = "Query parameters, used when request is omitted" },
                        maxResults = new { type = "integer", description = "Maximum rows when the query omits LIMIT" },
                        timeoutSeconds = new { type = "integer", description = "Query timeout in seconds" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) => ExecuteQuery(LiteGraphMcpServerHelpers.ToJsonElement(args), sdk));
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers native graph query methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("graph_query", (args) => ExecuteQuery(LiteGraphMcpServerHelpers.ToJsonElement(args), sdk));
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers native graph query methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("graph_query", (args) => ExecuteQuery(LiteGraphMcpServerHelpers.ToJsonElement(args), sdk));
        }

        #endregion

        #region Private-Methods

        private static string ExecuteQuery(JsonElement? args, LiteGraphSdk sdk)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
            string requestJson = BuildRequestJson(args.Value);
            string url = BuildQueryUrl(sdk.Endpoint, tenantGuid, graphGuid);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                if (!String.IsNullOrEmpty(sdk.BearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sdk.BearerToken);

                using (HttpResponseMessage response = _Http.Send(request))
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new LiteGraphRestException("LiteGraph query endpoint", (int)response.StatusCode, response.ReasonPhrase, body);
                    }

                    return body;
                }
            }
        }

        private static string BuildQueryUrl(string endpoint, Guid tenantGuid, Guid graphGuid)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));

            return endpoint.TrimEnd('/')
                + "/v1.0/tenants/"
                + Uri.EscapeDataString(tenantGuid.ToString())
                + "/graphs/"
                + Uri.EscapeDataString(graphGuid.ToString())
                + "/query";
        }

        private static string BuildRequestJson(JsonElement args)
        {
            if (args.TryGetProperty("request", out JsonElement requestProp))
            {
                JsonObject request = ReadJsonObject(requestProp, "request");
                return request.ToJsonString();
            }

            if (!args.TryGetProperty("query", out JsonElement queryProp))
                throw new ArgumentException("Either request or query is required");

            string? query = queryProp.GetString();
            if (String.IsNullOrEmpty(query)) throw new ArgumentException("Query cannot be empty");

            JsonObject requestObject = new JsonObject
            {
                ["Query"] = query
            };

            if (args.TryGetProperty("parameters", out JsonElement parametersProp))
                requestObject["Parameters"] = ReadJsonObject(parametersProp, "parameters");

            if (args.TryGetProperty("maxResults", out JsonElement maxResultsProp))
                requestObject["MaxResults"] = maxResultsProp.GetInt32();

            if (args.TryGetProperty("timeoutSeconds", out JsonElement timeoutSecondsProp))
                requestObject["TimeoutSeconds"] = timeoutSecondsProp.GetInt32();

            return requestObject.ToJsonString();
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
