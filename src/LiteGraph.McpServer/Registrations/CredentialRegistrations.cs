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
    /// Registration methods for Credential operations.
    /// </summary>
    public static class CredentialRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers credential tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "credential/create",
                "Creates a new credential in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        credential = new { type = "string", description = "Credential object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "credential" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                        throw new ArgumentException("Credential JSON string is required");
                    string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                    Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                    return CreateCredential(sdk, credential);
                });

            server.RegisterTool(
                "credential/get",
                "Reads a credential by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuid = new { type = "string", description = "Credential GUID" }
                    },
                    required = new[] { "tenantGuid", "credentialGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");

                    return ReadCredential(sdk, tenantGuid, credentialGuid);
                });

            server.RegisterTool(
                "credential/all",
                "Lists all credentials in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadCredentials(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "credential/enumerate",
                "Enumerates credentials with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Enumeration request serialized as JSON string using Serializer" }
                    },
                    required = new[] { "query" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                        throw new ArgumentException("Enumeration query is required");

                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                    if (query.TenantGUID == null)
                        throw new ArgumentException("query.TenantGUID is required.");
                    
                    return EnumerateCredentials(sdk, query);
                });

            server.RegisterTool(
                "credential/update",
                "Updates a credential",
                new
                {
                    type = "object",
                    properties = new
                    {
                        credential = new { type = "string", description = "Credential object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "credential" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                        throw new ArgumentException("Credential JSON string is required");
                    string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                    Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                    return UpdateCredential(sdk, credential);
                });

            server.RegisterTool(
                "credential/delete",
                "Deletes a credential by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuid = new { type = "string", description = "Credential GUID" }
                    },
                    required = new[] { "tenantGuid", "credentialGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                    DeleteCredential(sdk, tenantGuid, credentialGuid);
                    return true;
                });

            server.RegisterTool(
                "credential/exists",
                "Checks if a credential exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuid = new { type = "string", description = "Credential GUID" }
                    },
                    required = new[] { "tenantGuid", "credentialGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                    return CredentialExists(sdk, tenantGuid, credentialGuid);
                });

            server.RegisterTool(
                "credential/getmany",
                "Reads multiple credentials by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        credentialGuids = new { type = "array", items = new { type = "string" }, description = "Array of credential GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "credentialGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("credentialGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Credential GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    return ReadCredentialsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "credential/getbybearertoken",
                "Reads a credential by bearer token",
                new
                {
                    type = "object",
                    properties = new
                    {
                        bearerToken = new { type = "string", description = "Bearer token" }
                    },
                    required = new[] { "bearerToken" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                        throw new ArgumentException("Bearer token is required");
                    string bearerToken = bearerTokenProp.GetString() ?? throw new ArgumentException("Bearer token cannot be null");
                    
                    return ReadCredentialByBearerToken(sdk, bearerToken);
                });

            server.RegisterTool(
                "credential/deleteallintenant",
                "Deletes all credentials in a tenant",
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
                    DeleteAllCredentialsInTenant(sdk, tenantGuid);
                    return true;
                });

            server.RegisterTool(
                "credential/deletebyuser",
                "Deletes all credentials for a user",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        userGuid = new { type = "string", description = "User GUID" }
                    },
                    required = new[] { "tenantGuid", "userGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                    DeleteCredentialsByUser(sdk, tenantGuid, userGuid);
                    return true;
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers credential methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("credential/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                return CreateCredential(sdk, credential);
            });

            server.RegisterMethod("credential/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");

                return ReadCredential(sdk, tenantGuid, credentialGuid);
            });

            server.RegisterMethod("credential/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadCredentials(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("credential/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

                return EnumerateCredentials(sdk, query);
            });

            server.RegisterMethod("credential/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                return UpdateCredential(sdk, credential);
            });

            server.RegisterMethod("credential/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                DeleteCredential(sdk, tenantGuid, credentialGuid);
                return true;
            });

            server.RegisterMethod("credential/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                return CredentialExists(sdk, tenantGuid, credentialGuid);
            });

            server.RegisterMethod("credential/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("credentialGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Credential GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadCredentialsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("credential/getbybearertoken", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                    throw new ArgumentException("Bearer token is required");
                string bearerToken = bearerTokenProp.GetString() ?? throw new ArgumentException("Bearer token cannot be null");
                
                return ReadCredentialByBearerToken(sdk, bearerToken);
            });

            server.RegisterMethod("credential/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllCredentialsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("credential/deletebyuser", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                DeleteCredentialsByUser(sdk, tenantGuid, userGuid);
                return true;
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers credential methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("credential/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                return CreateCredential(sdk, credential);
            });

            server.RegisterMethod("credential/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");

                return ReadCredential(sdk, tenantGuid, credentialGuid);
            });

            server.RegisterMethod("credential/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadCredentials(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("credential/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

                return EnumerateCredentials(sdk, query);
            });

            server.RegisterMethod("credential/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("credential", out JsonElement credentialProp))
                    throw new ArgumentException("Credential JSON string is required");
                string credentialJson = credentialProp.GetString() ?? throw new ArgumentException("Credential JSON string cannot be null");
                Credential credential = Serializer.DeserializeJson<Credential>(credentialJson);
                return UpdateCredential(sdk, credential);
            });

            server.RegisterMethod("credential/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                DeleteCredential(sdk, tenantGuid, credentialGuid);
                return "{\"success\": true}";
            });

            server.RegisterMethod("credential/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid credentialGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "credentialGuid");
                return CredentialExists(sdk, tenantGuid, credentialGuid);
            });

            server.RegisterMethod("credential/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("credentialGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Credential GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadCredentialsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("credential/getbybearertoken", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("bearerToken", out JsonElement bearerTokenProp))
                    throw new ArgumentException("Bearer token is required");
                string bearerToken = bearerTokenProp.GetString() ?? throw new ArgumentException("Bearer token cannot be null");
                
                return ReadCredentialByBearerToken(sdk, bearerToken);
            });

            server.RegisterMethod("credential/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllCredentialsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("credential/deletebyuser", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid userGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "userGuid");
                DeleteCredentialsByUser(sdk, tenantGuid, userGuid);
                return true;
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateCredential(LiteGraphSdk sdk, Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));

            string body = Serializer.SerializeJson(credential, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(credential.TenantGUID)
                + "/credentials",
                body);
        }

        private static string ReadCredential(LiteGraphSdk sdk, Guid tenantGuid, Guid credentialGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/credentials/"
                + LiteGraphMcpRestProxy.Escape(credentialGuid));
        }

        private static string ReadCredentials(LiteGraphSdk sdk, Guid tenantGuid, EnumerationOrderEnum order, int skip, int maxResults, Guid? continuationToken)
        {
            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/credentials?order="
                + LiteGraphMcpRestProxy.Escape(order.ToString())
                + "&skip="
                + skip
                + "&max-keys="
                + maxResults;

            if (continuationToken != null) url += "&token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value);

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string EnumerateCredentials(LiteGraphSdk sdk, EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentException("query.TenantGUID is required.");

            string body = Serializer.SerializeJson(query, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                "/v2.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                + "/credentials",
                body);
        }

        private static string CredentialExists(LiteGraphSdk sdk, Guid tenantGuid, Guid credentialGuid)
        {
            bool exists = LiteGraphMcpRestProxy.HeadExists(
                sdk,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/credentials/"
                + LiteGraphMcpRestProxy.Escape(credentialGuid));

            return exists.ToString().ToLowerInvariant();
        }

        private static string ReadCredentialsByGuids(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> guids, int maxResults)
        {
            if (guids == null) throw new ArgumentNullException(nameof(guids));
            if (guids.Count == 0) throw new ArgumentException("At least one credential GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/credentials?guids="
                + String.Join(",", guids)
                + "&max-keys="
                + maxResults);
        }

        private static string ReadCredentialByBearerToken(LiteGraphSdk sdk, string bearerToken)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/credentials/bearer/"
                + LiteGraphMcpRestProxy.Escape(bearerToken));
        }

        private static string UpdateCredential(LiteGraphSdk sdk, Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));

            string body = Serializer.SerializeJson(credential, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(credential.TenantGUID)
                + "/credentials/"
                + LiteGraphMcpRestProxy.Escape(credential.GUID),
                body);
        }

        private static void DeleteCredential(LiteGraphSdk sdk, Guid tenantGuid, Guid credentialGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/credentials/"
                + LiteGraphMcpRestProxy.Escape(credentialGuid));
        }

        private static void DeleteAllCredentialsInTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/credentials");
        }

        private static void DeleteCredentialsByUser(LiteGraphSdk sdk, Guid tenantGuid, Guid userGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/users/"
                + LiteGraphMcpRestProxy.Escape(userGuid)
                + "/credentials");
        }

        #endregion
    }
}
