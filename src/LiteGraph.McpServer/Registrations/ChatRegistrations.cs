namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for Chat operations.
    /// </summary>
    public static class ChatRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers chat tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphTool(
                "chat_endpoint_create",
                "Creates a chat endpoint (an upstream completion or embedding provider) in a tenant",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpoint": { "type": "string", "description": "ChatEndpoint object serialized as JSON string using Serializer" }
                        },
                        "required": [ "tenantGuid", "endpoint" ]
                    }
                    """),
                (args) => EndpointCreate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_get",
                "Reads a chat endpoint by GUID; the API key is redacted to its last four characters",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpointGuid": { "type": "string", "description": "Chat endpoint GUID" }
                        },
                        "required": [ "tenantGuid", "endpointGuid" ]
                    }
                    """),
                (args) => EndpointGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_all",
                "Lists chat endpoints in a tenant, optionally filtered by endpoint type. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpointType": { "type": "string", "description": "Optional endpoint type filter: Embedding or Completion" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (args) => EndpointAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_update",
                "Updates a chat endpoint; sending back a redacted API key value preserves the stored key",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpoint": {
                                "type": "string",
                                "description": "ChatEndpoint object serialized as JSON string using Serializer; GUID identifies the endpoint to update"
                            }
                        },
                        "required": [ "tenantGuid", "endpoint" ]
                    }
                    """),
                (args) => EndpointUpdate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_delete",
                "Deletes a chat endpoint by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpointGuid": { "type": "string", "description": "Chat endpoint GUID" }
                        },
                        "required": [ "tenantGuid", "endpointGuid" ]
                    }
                    """),
                (args) => EndpointDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_test",
                "Tests connectivity from the LiteGraph server to a chat endpoint's upstream provider and reports reachability, advertised models, and whether the configured model exists",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpointGuid": { "type": "string", "description": "Chat endpoint GUID" }
                        },
                        "required": [ "tenantGuid", "endpointGuid" ]
                    }
                    """),
                (args) => EndpointTest(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_health",
                "Reads background health-check status for one chat endpoint",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "endpointGuid": { "type": "string", "description": "Chat endpoint GUID" }
                        },
                        "required": [ "tenantGuid", "endpointGuid" ]
                    }
                    """),
                (args) => EndpointHealth(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_endpoint_healthall",
                "Reads background health-check status for every chat endpoint in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (args) => EndpointHealthAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_completions",
                "Executes a non-streaming chat completion against a tenant's graph data; streaming is unavailable over MCP. Omitting threadGuid creates a new thread, optionally bound to graphGuid. Requires a user principal; the admin break-glass token is rejected.",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "message": { "type": "string", "description": "User message" },
                            "threadGuid": { "type": "string", "description": "Optional chat thread GUID; omit to create a new thread" },
                            "graphGuid": { "type": "string", "description": "Optional graph GUID to bind a newly created thread to" },
                            "completionEndpointGuid": {
                                "type": "string",
                                "description": "Optional completion endpoint GUID override; defaults to the tenant chat settings"
                            },
                            "embeddingEndpointGuid": {
                                "type": "string",
                                "description": "Optional embedding endpoint GUID override; defaults to the tenant chat settings"
                            },
                            "enableTools": {
                                "type": "boolean",
                                "description": "Optional tool advertisement override; defaults to the tenant chat settings"
                            },
                            "enableRag": {
                                "type": "boolean",
                                "description": "Optional retrieval override; defaults to the tenant chat settings"
                            }
                        },
                        "required": [ "tenantGuid", "message" ]
                    }
                    """),
                (args) => Completions(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_thread_all",
                "Lists chat threads in a tenant; the caller's own threads by default, or every user's threads with allUsers (admin only). Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "allUsers": { "type": "boolean", "description": "True to list every user's threads (admin only, default: false)" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (args) => ThreadAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_thread_get",
                "Reads a chat thread by GUID",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "threadGuid": { "type": "string", "description": "Chat thread GUID" }
                        },
                        "required": [ "tenantGuid", "threadGuid" ]
                    }
                    """),
                (args) => ThreadGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_thread_delete",
                "Deletes a chat thread along with its turns and feedback",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "threadGuid": { "type": "string", "description": "Chat thread GUID" }
                        },
                        "required": [ "tenantGuid", "threadGuid" ]
                    }
                    """),
                (args) => ThreadDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_thread_turns",
                "Reads the turns of a chat thread ascending by sequence, including metrics and tool transcripts. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "threadGuid": { "type": "string", "description": "Chat thread GUID" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            }
                        },
                        "required": [ "tenantGuid", "threadGuid" ]
                    }
                    """),
                (args) => ThreadTurns(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_feedback_create",
                "Submits feedback on a chat turn. Requires a user principal; the admin break-glass token is rejected.",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "turnGuid": { "type": "string", "description": "Chat turn GUID" },
                            "rating": { "type": "string", "description": "Rating: ThumbsUp or ThumbsDown" },
                            "feedbackText": { "type": "string", "description": "Optional free-text feedback" }
                        },
                        "required": [ "tenantGuid", "turnGuid", "rating" ]
                    }
                    """),
                (args) => FeedbackCreate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_feedback_all",
                "Lists all chat feedback in a tenant (admin only). Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "skip": { "type": "integer", "description": "Number of records to skip (default: 0)" },
                            "maxResults": { "type": "integer", "description": "Maximum results to return, 1-1000, default 1000" },
                            "continuationToken": {
                                "type": "string",
                                "description": "Continuation token (GUID) from a previous response for marker-based pagination"
                            }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (args) => FeedbackAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_feedback_delete",
                "Deletes a chat feedback record by GUID (admin only)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "feedbackGuid": { "type": "string", "description": "Chat feedback GUID" }
                        },
                        "required": [ "tenantGuid", "feedbackGuid" ]
                    }
                    """),
                (args) => FeedbackDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_settings_get",
                "Reads a tenant's chat settings; defaults are returned when no record exists",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" }
                        },
                        "required": [ "tenantGuid" ]
                    }
                    """),
                (args) => SettingsGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));

            server.RegisterLiteGraphTool(
                "chat_settings_update",
                "Upserts a tenant's chat settings (admin only)",
                LiteGraphMcpSchema.Parse("""
                    {
                        "type": "object",
                        "properties": {
                            "tenantGuid": { "type": "string", "description": "Tenant GUID" },
                            "settings": { "type": "string", "description": "ChatSettings object serialized as JSON string using Serializer" }
                        },
                        "required": [ "tenantGuid", "settings" ]
                    }
                    """),
                (args) => SettingsUpdate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers chat methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("chat_endpoint_create", (args) => EndpointCreate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_get", (args) => EndpointGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_all", (args) => EndpointAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_update", (args) => EndpointUpdate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_delete", (args) => EndpointDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_test", (args) => EndpointTest(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_health", (args) => EndpointHealth(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_healthall", (args) => EndpointHealthAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_completions", (args) => Completions(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_all", (args) => ThreadAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_get", (args) => ThreadGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_delete", (args) => ThreadDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_turns", (args) => ThreadTurns(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_feedback_create", (args) => FeedbackCreate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_feedback_all", (args) => FeedbackAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_feedback_delete", (args) => FeedbackDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_settings_get", (args) => SettingsGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_settings_update", (args) => SettingsUpdate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers chat methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("chat_endpoint_create", (args) => EndpointCreate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_get", (args) => EndpointGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_all", (args) => EndpointAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_update", (args) => EndpointUpdate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_delete", (args) => EndpointDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_test", (args) => EndpointTest(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_health", (args) => EndpointHealth(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_endpoint_healthall", (args) => EndpointHealthAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_completions", (args) => Completions(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_all", (args) => ThreadAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_get", (args) => ThreadGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_delete", (args) => ThreadDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_thread_turns", (args) => ThreadTurns(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_feedback_create", (args) => FeedbackCreate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_feedback_all", (args) => FeedbackAll(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_feedback_delete", (args) => FeedbackDelete(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_settings_get", (args) => SettingsGet(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
            server.RegisterLiteGraphMethod("chat_settings_update", (args) => SettingsUpdate(sdk, LiteGraphMcpServerHelpers.ToJsonElement(args)));
        }

        #endregion

        #region Private-Methods

        private static string EndpointCreate(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            if (!args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                throw new ArgumentException("Chat endpoint JSON string is required");

            string endpointJson = endpointProp.GetString() ?? throw new ArgumentException("ChatEndpoint JSON string cannot be null");
            ChatEndpoint endpoint = Serializer.DeserializeJson<ChatEndpoint>(endpointJson);
            endpoint.TenantGUID = tenantGuid;
            ChatEndpoint created = sdk.Chat.CreateEndpoint(endpoint).GetAwaiter().GetResult();
            return Serializer.SerializeJson(created, true);
        }

        private static string EndpointGet(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid endpointGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "endpointGuid");
            ChatEndpoint endpoint = sdk.Chat.ReadEndpoint(tenantGuid, endpointGuid).GetAwaiter().GetResult();
            return endpoint != null ? Serializer.SerializeJson(endpoint, true) : "null";
        }

        private static string EndpointAll(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");

            ChatEndpointTypeEnum? endpointType = null;
            if (args.Value.TryGetProperty("endpointType", out JsonElement typeProp))
            {
                string? typeStr = typeProp.GetString();
                if (!string.IsNullOrEmpty(typeStr))
                {
                    if (!Enum.TryParse<ChatEndpointTypeEnum>(typeStr, true, out ChatEndpointTypeEnum parsed))
                        throw new ArgumentException("Endpoint type must be Embedding or Completion");
                    endpointType = parsed;
                }
            }

            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/chat/endpoints"
                + BuildChatListQuery(args);

            if (endpointType != null) url += "&endpointType=" + LiteGraphMcpRestProxy.Escape(endpointType.Value.ToString());

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string EndpointUpdate(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            if (!args.Value.TryGetProperty("endpoint", out JsonElement endpointProp))
                throw new ArgumentException("Chat endpoint JSON string is required");

            string endpointJson = endpointProp.GetString() ?? throw new ArgumentException("ChatEndpoint JSON string cannot be null");
            ChatEndpoint endpoint = Serializer.DeserializeJson<ChatEndpoint>(endpointJson);
            endpoint.TenantGUID = tenantGuid;
            ChatEndpoint updated = sdk.Chat.UpdateEndpoint(endpoint).GetAwaiter().GetResult();
            return Serializer.SerializeJson(updated, true);
        }

        private static bool EndpointDelete(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid endpointGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "endpointGuid");
            sdk.Chat.DeleteEndpoint(tenantGuid, endpointGuid).GetAwaiter().GetResult();
            return true;
        }

        private static string EndpointTest(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid endpointGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "endpointGuid");
            ChatEndpointTestResult result = sdk.Chat.TestEndpoint(tenantGuid, endpointGuid).GetAwaiter().GetResult();
            return Serializer.SerializeJson(result, true);
        }

        private static string EndpointHealth(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid endpointGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "endpointGuid");
            ChatEndpointHealth health = sdk.Chat.ReadEndpointHealth(tenantGuid, endpointGuid).GetAwaiter().GetResult();
            return health != null ? Serializer.SerializeJson(health, true) : "null";
        }

        private static string EndpointHealthAll(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/chat/endpoints/health"
                + BuildChatListQuery(args));
        }

        private static string Completions(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            if (!args.Value.TryGetProperty("message", out JsonElement messageProp))
                throw new ArgumentException("Message is required");

            string message = messageProp.GetString() ?? throw new ArgumentException("Message cannot be null");

            ChatCompletionRequest request = new ChatCompletionRequest();
            request.Message = message;
            request.Stream = false;
            request.ThreadGUID = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "threadGuid");
            request.GraphGUID = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
            request.CompletionEndpointGUID = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "completionEndpointGuid");
            request.EmbeddingEndpointGUID = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "embeddingEndpointGuid");

            if (args.Value.TryGetProperty("enableTools", out JsonElement toolsProp)
                && (toolsProp.ValueKind == JsonValueKind.True || toolsProp.ValueKind == JsonValueKind.False))
                request.EnableTools = toolsProp.GetBoolean();

            if (args.Value.TryGetProperty("enableRag", out JsonElement ragProp)
                && (ragProp.ValueKind == JsonValueKind.True || ragProp.ValueKind == JsonValueKind.False))
                request.EnableRag = ragProp.GetBoolean();

            ChatCompletionResult result = sdk.Chat.Completion(tenantGuid, request).GetAwaiter().GetResult();
            return Serializer.SerializeJson(result, true);
        }

        private static string ThreadAll(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            bool allUsers = LiteGraphMcpServerHelpers.GetBoolOrDefault(args.Value, "allUsers", false);

            string url = "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/chat/threads"
                + BuildChatListQuery(args);

            if (allUsers) url += "&all=true";

            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, url);
        }

        private static string ThreadGet(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid threadGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "threadGuid");
            ChatThread thread = sdk.Chat.ReadThread(tenantGuid, threadGuid).GetAwaiter().GetResult();
            return thread != null ? Serializer.SerializeJson(thread, true) : "null";
        }

        private static bool ThreadDelete(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid threadGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "threadGuid");
            sdk.Chat.DeleteThread(tenantGuid, threadGuid).GetAwaiter().GetResult();
            return true;
        }

        private static string ThreadTurns(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid threadGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "threadGuid");
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/chat/threads/"
                + LiteGraphMcpRestProxy.Escape(threadGuid)
                + "/turns"
                + BuildChatListQuery(args));
        }

        private static string FeedbackCreate(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid turnGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "turnGuid");
            if (!args.Value.TryGetProperty("rating", out JsonElement ratingProp))
                throw new ArgumentException("Rating is required");

            string? ratingStr = ratingProp.GetString();
            if (string.IsNullOrEmpty(ratingStr) || !Enum.TryParse<ChatFeedbackRatingEnum>(ratingStr, true, out ChatFeedbackRatingEnum rating))
                throw new ArgumentException("Rating must be ThumbsUp or ThumbsDown");

            string? feedbackText = null;
            if (args.Value.TryGetProperty("feedbackText", out JsonElement textProp) && textProp.ValueKind == JsonValueKind.String)
                feedbackText = textProp.GetString();

            ChatFeedback feedback = sdk.Chat.SubmitFeedback(tenantGuid, turnGuid, rating, feedbackText).GetAwaiter().GetResult();
            return Serializer.SerializeJson(feedback, true);
        }

        private static string FeedbackAll(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/chat/feedback"
                + BuildChatListQuery(args));
        }

        private static bool FeedbackDelete(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            Guid feedbackGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "feedbackGuid");
            sdk.Chat.DeleteFeedback(tenantGuid, feedbackGuid).GetAwaiter().GetResult();
            return true;
        }

        private static string SettingsGet(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            ChatSettings settings = sdk.Chat.ReadChatSettings(tenantGuid).GetAwaiter().GetResult();
            return Serializer.SerializeJson(settings, true);
        }

        private static string SettingsUpdate(LiteGraphSdk sdk, JsonElement? args)
        {
            if (!args.HasValue) throw new ArgumentException("Parameters required");
            Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
            if (!args.Value.TryGetProperty("settings", out JsonElement settingsProp))
                throw new ArgumentException("Chat settings JSON string is required");

            string settingsJson = settingsProp.GetString() ?? throw new ArgumentException("ChatSettings JSON string cannot be null");
            ChatSettings settings = Serializer.DeserializeJson<ChatSettings>(settingsJson);
            settings.TenantGUID = tenantGuid;
            ChatSettings updated = sdk.Chat.UpdateChatSettings(settings).GetAwaiter().GetResult();
            return Serializer.SerializeJson(updated, true);
        }

        private static string BuildChatListQuery(JsonElement? args)
        {
            int skip = args.HasValue ? LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0) : 0;
            int maxResults = LiteGraphMcpServerHelpers.GetMaxResults(args);
            Guid? continuationToken = LiteGraphMcpServerHelpers.GetContinuationToken(args);

            string query = "?skip=" + skip + "&max-keys=" + maxResults;
            if (continuationToken != null) query += "&token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value);
            return query;
        }

        #endregion
    }
}
