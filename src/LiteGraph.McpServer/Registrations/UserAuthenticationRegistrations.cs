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
    /// Registration methods for User Authentication operations.
    /// </summary>
    public static class UserAuthenticationRegistrations
    {
        #region Private-Members

        private static readonly HttpClient _Http = new HttpClient();

        #endregion

        #region HTTP-Tools

        /// <summary>
        /// Registers user authentication tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "userauthentication/gettenantsforemail",
                "Gets all tenants associated with an email address. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        email = new { type = "string", description = "Email address" },
                        endpoint = new { type = "string", description = "Endpoint URL (optional, uses SDK endpoint if not provided)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "email" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("email", out JsonElement emailProp))
                        throw new ArgumentException("Email is required");

                    string email = emailProp.GetString() ?? throw new ArgumentException("Email cannot be null");
                    string endpoint = sdk.Endpoint;

                    if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                    {
                        string? endpointStr = endpointProp.GetString();
                        if (!string.IsNullOrEmpty(endpointStr))
                            endpoint = endpointStr;
                    }

                    int skip = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0);
                    return GetTenantsForEmail(endpoint, email, LiteGraphMcpServerHelpers.GetMaxResults(args), skip);
                });

            server.RegisterTool(
                "userauthentication/generatetoken",
                "Generates an authentication token using email, password, and tenant GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        email = new { type = "string", description = "User email address" },
                        password = new { type = "string", description = "User password" },
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpoint = new { type = "string", description = "Endpoint URL (optional, uses SDK endpoint if not provided)" },
                        bearerToken = new { type = "string", description = "Bearer token (optional, uses SDK bearer token if not provided)" }
                    },
                    required = new[] { "email", "password", "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    if (!args.Value.TryGetProperty("email", out JsonElement emailProp))
                        throw new ArgumentException("Email is required");
                    if (!args.Value.TryGetProperty("password", out JsonElement passwordProp))
                        throw new ArgumentException("Password is required");
                    if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    string email = emailProp.GetString() ?? throw new ArgumentException("Email cannot be null");
                    string password = passwordProp.GetString() ?? throw new ArgumentException("Password cannot be null");
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString() ?? throw new ArgumentException("Tenant GUID cannot be null"));

                    string endpoint = sdk.Endpoint;
                    string bearerToken = sdk.BearerToken ?? "default";

                    if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                    {
                        string? endpointStr = endpointProp.GetString();
                        if (!string.IsNullOrEmpty(endpointStr))
                            endpoint = endpointStr;
                    }

                    if (args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                    {
                        string? bearerTokenStr = bearerTokenProp.GetString();
                        if (!string.IsNullOrEmpty(bearerTokenStr))
                            bearerToken = bearerTokenStr;
                    }

                    LiteGraphSdk tempSdk = new LiteGraphSdk(email, password, tenantGuid, endpoint, bearerToken);
                    AuthenticationToken? token = tempSdk.UserAuthentication.GenerateToken().GetAwaiter().GetResult();
                    if (token == null)
                    {
                        throw new InvalidOperationException($"Failed to generate token. User with email '{email}' may not exist in tenant '{tenantGuid}', password may be incorrect, or user/tenant may be inactive.");
                    }
                    return Serializer.SerializeJson(token, true);
                });

            server.RegisterTool(
                "userauthentication/gettokendetails",
                "Gets details for an authentication token",
                new
                {
                    type = "object",
                    properties = new
                    {
                        authToken = new { type = "string", description = "Authentication token string" },
                        endpoint = new { type = "string", description = "Endpoint URL (optional, uses SDK endpoint if not provided)" },
                        bearerToken = new { type = "string", description = "Bearer token (optional, uses SDK bearer token if not provided)" }
                    },
                    required = new[] { "authToken" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("authToken", out JsonElement authTokenProp))
                        throw new ArgumentException("Authentication token is required");

                    string authToken = authTokenProp.GetString() ?? throw new ArgumentException("Authentication token cannot be null");

                    string endpoint = sdk.Endpoint;
                    string bearerToken = sdk.BearerToken ?? "default";

                    if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                    {
                        string? endpointStr = endpointProp.GetString();
                        if (!string.IsNullOrEmpty(endpointStr))
                            endpoint = endpointStr;
                    }

                    if (args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                    {
                        string? bearerTokenStr = bearerTokenProp.GetString();
                        if (!string.IsNullOrEmpty(bearerTokenStr))
                            bearerToken = bearerTokenStr;
                    }

                    LiteGraphSdk tempSdk = new LiteGraphSdk(endpoint, bearerToken);
                    AuthenticationToken? tokenDetails = tempSdk.UserAuthentication.GetTokenDetails(authToken).GetAwaiter().GetResult();
                    if (tokenDetails == null)
                    {
                        throw new InvalidOperationException($"Failed to get token details. The authentication token may be invalid or expired.");
                    }
                    return Serializer.SerializeJson(tokenDetails, true);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers user authentication methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("userauthentication/gettenantsforemail", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("email", out JsonElement emailProp))
                    throw new ArgumentException("Email is required");

                string email = emailProp.GetString() ?? throw new ArgumentException("Email cannot be null");
                string endpoint = sdk.Endpoint;

                if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                {
                    string? endpointStr = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(endpointStr))
                        endpoint = endpointStr;
                }

                int skip = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0);
                return GetTenantsForEmail(endpoint, email, LiteGraphMcpServerHelpers.GetMaxResults(args), skip);
            });

            server.RegisterMethod("userauthentication/generatetoken", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("email", out JsonElement emailProp))
                    throw new ArgumentException("Email is required");
                if (!args.Value.TryGetProperty("password", out JsonElement passwordProp))
                    throw new ArgumentException("Password is required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");

                string email = emailProp.GetString() ?? throw new ArgumentException("Email cannot be null");
                string password = passwordProp.GetString() ?? throw new ArgumentException("Password cannot be null");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString() ?? throw new ArgumentException("Tenant GUID cannot be null"));

                string endpoint = sdk.Endpoint;
                string bearerToken = sdk.BearerToken ?? "default";

                if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                {
                    string? endpointStr = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(endpointStr))
                        endpoint = endpointStr;
                }

                if (args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                {
                    string? bearerTokenStr = bearerTokenProp.GetString();
                    if (!string.IsNullOrEmpty(bearerTokenStr))
                        bearerToken = bearerTokenStr;
                }

                using (LiteGraphSdk tempSdk = new LiteGraphSdk(email, password, tenantGuid, endpoint, bearerToken))
                {
                    AuthenticationToken token = tempSdk.UserAuthentication.GenerateToken().GetAwaiter().GetResult();
                    return Serializer.SerializeJson(token, true);
                }
            });

            server.RegisterMethod("userauthentication/gettokendetails", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("authToken", out JsonElement authTokenProp))
                    throw new ArgumentException("Authentication token is required");

                string authToken = authTokenProp.GetString() ?? throw new ArgumentException("Authentication token cannot be null");

                string endpoint = sdk.Endpoint;
                string bearerToken = sdk.BearerToken ?? "default";

                if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                {
                    string? endpointStr = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(endpointStr))
                        endpoint = endpointStr;
                }

                if (args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                {
                    string? bearerTokenStr = bearerTokenProp.GetString();
                    if (!string.IsNullOrEmpty(bearerTokenStr))
                        bearerToken = bearerTokenStr;
                }

                using (LiteGraphSdk tempSdk = new LiteGraphSdk(endpoint, bearerToken))
                {
                    AuthenticationToken tokenDetails = tempSdk.UserAuthentication.GetTokenDetails(authToken).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(tokenDetails, true);
                }
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers user authentication methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("userauthentication/gettenantsforemail", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("email", out JsonElement emailProp))
                    throw new ArgumentException("Email is required");

                string email = emailProp.GetString() ?? throw new ArgumentException("Email cannot be null");
                string endpoint = sdk.Endpoint;

                if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                {
                    string? endpointStr = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(endpointStr))
                        endpoint = endpointStr;
                }

                int skip = LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0);
                return GetTenantsForEmail(endpoint, email, LiteGraphMcpServerHelpers.GetMaxResults(args), skip);
            });

            server.RegisterMethod("userauthentication/generatetoken", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                if (!args.Value.TryGetProperty("email", out JsonElement emailProp))
                    throw new ArgumentException("Email is required");
                if (!args.Value.TryGetProperty("password", out JsonElement passwordProp))
                    throw new ArgumentException("Password is required");
                if (!args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");

                string email = emailProp.GetString() ?? throw new ArgumentException("Email cannot be null");
                string password = passwordProp.GetString() ?? throw new ArgumentException("Password cannot be null");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString() ?? throw new ArgumentException("Tenant GUID cannot be null"));

                string endpoint = sdk.Endpoint;
                string bearerToken = sdk.BearerToken ?? "default";

                if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                {
                    string? endpointStr = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(endpointStr))
                        endpoint = endpointStr;
                }

                if (args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                {
                    string? bearerTokenStr = bearerTokenProp.GetString();
                    if (!string.IsNullOrEmpty(bearerTokenStr))
                        bearerToken = bearerTokenStr;
                }

                using (LiteGraphSdk tempSdk = new LiteGraphSdk(email, password, tenantGuid, endpoint, bearerToken))
                {
                    AuthenticationToken token = tempSdk.UserAuthentication.GenerateToken().GetAwaiter().GetResult();
                    return Serializer.SerializeJson(token, true);
                }
            });

            server.RegisterMethod("userauthentication/gettokendetails", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("authToken", out JsonElement authTokenProp))
                    throw new ArgumentException("Authentication token is required");

                string authToken = authTokenProp.GetString() ?? throw new ArgumentException("Authentication token cannot be null");

                string endpoint = sdk.Endpoint;
                string bearerToken = sdk.BearerToken ?? "default";

                if (args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                {
                    string? endpointStr = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(endpointStr))
                        endpoint = endpointStr;
                }

                if (args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                {
                    string? bearerTokenStr = bearerTokenProp.GetString();
                    if (!string.IsNullOrEmpty(bearerTokenStr))
                        bearerToken = bearerTokenStr;
                }

                using (LiteGraphSdk tempSdk = new LiteGraphSdk(endpoint, bearerToken))
                {
                    AuthenticationToken tokenDetails = tempSdk.UserAuthentication.GetTokenDetails(authToken).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(tokenDetails, true);
                }
            });
        }

        #endregion

        #region Private-Methods

        private static string GetTenantsForEmail(string endpoint, string email, int maxResults, int skip)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string url = endpoint.TrimEnd('/') + "/v1.0/token/tenants?max-keys=" + maxResults + "&skip=" + skip;

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("x-email", email);

                using (HttpResponseMessage response = _Http.Send(request))
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            "LiteGraph endpoint returned "
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

        #endregion
    }
}

