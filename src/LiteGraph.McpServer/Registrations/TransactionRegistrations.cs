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
    /// Registration methods for graph-scoped transaction operations.
    /// </summary>
    public static class TransactionRegistrations
    {
        #region Private-Members

        private static readonly HttpClient _Http = new HttpClient();

        #endregion

        #region HTTP-Tools

        /// <summary>
        /// Registers graph transaction tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "graph/transaction",
                "Executes an atomic graph-scoped transaction against a single tenant and graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        request = new { type = "object", description = "TransactionRequest object with Operations, MaxOperations, and TimeoutSeconds" },
                        transaction = new { type = "object", description = "Alias for request" },
                        operations = new { type = "array", description = "Transaction operations, used when request is omitted" },
                        maxOperations = new { type = "integer", description = "Maximum operations allowed for this request" },
                        timeoutSeconds = new { type = "integer", description = "Transaction timeout in seconds" },
                        isolationLevel = new { type = "string", description = "Transaction isolation level: Default, ReadCommitted, RepeatableRead, or Serializable" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) => ExecuteTransaction(args, sdk));
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers graph transaction methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("graph/transaction", (args) => ExecuteTransaction(args, sdk));
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers graph transaction methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("graph/transaction", (args) => ExecuteTransaction(args, sdk));
        }

        #endregion

        #region Private-Methods

        private static string ExecuteTransaction(JsonElement? args, LiteGraphSdk sdk)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));

            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
            string requestJson = BuildRequestJson(args.Value);
            string url = BuildTransactionUrl(sdk.Endpoint, tenantGuid, graphGuid);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                if (!String.IsNullOrEmpty(sdk.BearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sdk.BearerToken);

                using (HttpResponseMessage response = _Http.Send(request))
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode && !IsDiagnosticTransactionResult(response.StatusCode, body))
                    {
                        throw new InvalidOperationException(
                            "LiteGraph transaction endpoint returned "
                            + (int)response.StatusCode
                            + " "
                            + response.ReasonPhrase
                            + ": "
                            + body);
                    }

                    return body;
                }
            }
        }

        private static bool IsDiagnosticTransactionResult(System.Net.HttpStatusCode statusCode, string body)
        {
            if (statusCode != System.Net.HttpStatusCode.BadRequest
                && statusCode != System.Net.HttpStatusCode.Conflict)
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(body)) return false;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(body))
                {
                    JsonElement root = document.RootElement;
                    return root.ValueKind == JsonValueKind.Object
                        && root.TryGetProperty("Success", out _)
                        && root.TryGetProperty("TransactionId", out _);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string BuildTransactionUrl(string endpoint, Guid tenantGuid, Guid graphGuid)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));

            return endpoint.TrimEnd('/')
                + "/v1.0/tenants/"
                + Uri.EscapeDataString(tenantGuid.ToString())
                + "/graphs/"
                + Uri.EscapeDataString(graphGuid.ToString())
                + "/transaction";
        }

        private static string BuildRequestJson(JsonElement args)
        {
            if (args.TryGetProperty("request", out JsonElement requestProp))
                return ReadJsonObject(requestProp, "request").ToJsonString();

            if (args.TryGetProperty("transaction", out JsonElement transactionProp))
                return ReadJsonObject(transactionProp, "transaction").ToJsonString();

            if (!args.TryGetProperty("operations", out JsonElement operationsProp))
                throw new ArgumentException("Either request, transaction, or operations is required");

            JsonNode operations = ReadJsonNode(operationsProp, "operations");
            if (operations is not JsonArray)
                throw new ArgumentException("operations must be a JSON array");

            JsonObject requestObject = new JsonObject
            {
                ["Operations"] = operations
            };

            if (args.TryGetProperty("maxOperations", out JsonElement maxOperationsProp))
                requestObject["MaxOperations"] = maxOperationsProp.GetInt32();

            if (args.TryGetProperty("timeoutSeconds", out JsonElement timeoutSecondsProp))
                requestObject["TimeoutSeconds"] = timeoutSecondsProp.GetInt32();

            if (args.TryGetProperty("isolationLevel", out JsonElement isolationLevelProp))
                requestObject["IsolationLevel"] = isolationLevelProp.GetString();

            return requestObject.ToJsonString();
        }

        private static JsonObject ReadJsonObject(JsonElement prop, string propertyName)
        {
            JsonNode node = ReadJsonNode(prop, propertyName);
            if (node is JsonObject obj) return obj;

            throw new ArgumentException(propertyName + " must be a JSON object");
        }

        private static JsonNode ReadJsonNode(JsonElement prop, string propertyName)
        {
            string json = prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? String.Empty
                : prop.GetRawText();

            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException(propertyName + " cannot be empty");

            return JsonNode.Parse(json) ?? throw new ArgumentException(propertyName + " must be valid JSON");
        }

        #endregion
    }
}
