namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

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
            server.RegisterTool(
                "chat/endpoint/create",
                "Creates a chat endpoint (an upstream completion or embedding provider) in a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpoint = new { type = "string", description = "ChatEndpoint object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "endpoint" }
                },
                (args) => EndpointCreate(sdk, args));

            server.RegisterTool(
                "chat/endpoint/get",
                "Reads a chat endpoint by GUID; the API key is redacted to its last four characters",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpointGuid = new { type = "string", description = "Chat endpoint GUID" }
                    },
                    required = new[] { "tenantGuid", "endpointGuid" }
                },
                (args) => EndpointGet(sdk, args));

            server.RegisterTool(
                "chat/endpoint/all",
                "Lists chat endpoints in a tenant, optionally filtered by endpoint type",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpointType = new { type = "string", description = "Optional endpoint type filter: Embedding or Completion" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) => EndpointAll(sdk, args));

            server.RegisterTool(
                "chat/endpoint/update",
                "Updates a chat endpoint; sending back a redacted API key value preserves the stored key",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpoint = new { type = "string", description = "ChatEndpoint object serialized as JSON string using Serializer; GUID identifies the endpoint to update" }
                    },
                    required = new[] { "tenantGuid", "endpoint" }
                },
                (args) => EndpointUpdate(sdk, args));

            server.RegisterTool(
                "chat/endpoint/delete",
                "Deletes a chat endpoint by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpointGuid = new { type = "string", description = "Chat endpoint GUID" }
                    },
                    required = new[] { "tenantGuid", "endpointGuid" }
                },
                (args) => EndpointDelete(sdk, args));

            server.RegisterTool(
                "chat/endpoint/test",
                "Tests connectivity from the LiteGraph server to a chat endpoint's upstream provider and reports reachability, advertised models, and whether the configured model exists",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpointGuid = new { type = "string", description = "Chat endpoint GUID" }
                    },
                    required = new[] { "tenantGuid", "endpointGuid" }
                },
                (args) => EndpointTest(sdk, args));

            server.RegisterTool(
                "chat/endpoint/health",
                "Reads background health-check status for one chat endpoint",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        endpointGuid = new { type = "string", description = "Chat endpoint GUID" }
                    },
                    required = new[] { "tenantGuid", "endpointGuid" }
                },
                (args) => EndpointHealth(sdk, args));

            server.RegisterTool(
                "chat/endpoint/healthall",
                "Reads background health-check status for every chat endpoint in a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) => EndpointHealthAll(sdk, args));

            server.RegisterTool(
                "chat/completions",
                "Executes a non-streaming chat completion against a tenant's graph data; streaming is unavailable over MCP. Omitting threadGuid creates a new thread, optionally bound to graphGuid. Requires a user principal; the admin break-glass token is rejected.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        message = new { type = "string", description = "User message" },
                        threadGuid = new { type = "string", description = "Optional chat thread GUID; omit to create a new thread" },
                        graphGuid = new { type = "string", description = "Optional graph GUID to bind a newly created thread to" },
                        completionEndpointGuid = new { type = "string", description = "Optional completion endpoint GUID override; defaults to the tenant chat settings" },
                        embeddingEndpointGuid = new { type = "string", description = "Optional embedding endpoint GUID override; defaults to the tenant chat settings" },
                        enableTools = new { type = "boolean", description = "Optional tool advertisement override; defaults to the tenant chat settings" },
                        enableRag = new { type = "boolean", description = "Optional retrieval override; defaults to the tenant chat settings" }
                    },
                    required = new[] { "tenantGuid", "message" }
                },
                (args) => Completions(sdk, args));

            server.RegisterTool(
                "chat/thread/all",
                "Lists chat threads in a tenant; the caller's own threads by default, or every user's threads with allUsers (admin only)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        allUsers = new { type = "boolean", description = "True to list every user's threads (admin only, default: false)" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) => ThreadAll(sdk, args));

            server.RegisterTool(
                "chat/thread/get",
                "Reads a chat thread by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        threadGuid = new { type = "string", description = "Chat thread GUID" }
                    },
                    required = new[] { "tenantGuid", "threadGuid" }
                },
                (args) => ThreadGet(sdk, args));

            server.RegisterTool(
                "chat/thread/delete",
                "Deletes a chat thread along with its turns and feedback",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        threadGuid = new { type = "string", description = "Chat thread GUID" }
                    },
                    required = new[] { "tenantGuid", "threadGuid" }
                },
                (args) => ThreadDelete(sdk, args));

            server.RegisterTool(
                "chat/thread/turns",
                "Reads the turns of a chat thread ascending by sequence, including metrics and tool transcripts",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        threadGuid = new { type = "string", description = "Chat thread GUID" }
                    },
                    required = new[] { "tenantGuid", "threadGuid" }
                },
                (args) => ThreadTurns(sdk, args));

            server.RegisterTool(
                "chat/feedback/create",
                "Submits feedback on a chat turn. Requires a user principal; the admin break-glass token is rejected.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        turnGuid = new { type = "string", description = "Chat turn GUID" },
                        rating = new { type = "string", description = "Rating: ThumbsUp or ThumbsDown" },
                        feedbackText = new { type = "string", description = "Optional free-text feedback" }
                    },
                    required = new[] { "tenantGuid", "turnGuid", "rating" }
                },
                (args) => FeedbackCreate(sdk, args));

            server.RegisterTool(
                "chat/feedback/all",
                "Lists all chat feedback in a tenant (admin only)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) => FeedbackAll(sdk, args));

            server.RegisterTool(
                "chat/feedback/delete",
                "Deletes a chat feedback record by GUID (admin only)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        feedbackGuid = new { type = "string", description = "Chat feedback GUID" }
                    },
                    required = new[] { "tenantGuid", "feedbackGuid" }
                },
                (args) => FeedbackDelete(sdk, args));

            server.RegisterTool(
                "chat/settings/get",
                "Reads a tenant's chat settings; defaults are returned when no record exists",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) => SettingsGet(sdk, args));

            server.RegisterTool(
                "chat/settings/update",
                "Upserts a tenant's chat settings (admin only)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        settings = new { type = "string", description = "ChatSettings object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "settings" }
                },
                (args) => SettingsUpdate(sdk, args));
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
            server.RegisterMethod("chat/endpoint/create", (args) => EndpointCreate(sdk, args));
            server.RegisterMethod("chat/endpoint/get", (args) => EndpointGet(sdk, args));
            server.RegisterMethod("chat/endpoint/all", (args) => EndpointAll(sdk, args));
            server.RegisterMethod("chat/endpoint/update", (args) => EndpointUpdate(sdk, args));
            server.RegisterMethod("chat/endpoint/delete", (args) => EndpointDelete(sdk, args));
            server.RegisterMethod("chat/endpoint/test", (args) => EndpointTest(sdk, args));
            server.RegisterMethod("chat/endpoint/health", (args) => EndpointHealth(sdk, args));
            server.RegisterMethod("chat/endpoint/healthall", (args) => EndpointHealthAll(sdk, args));
            server.RegisterMethod("chat/completions", (args) => Completions(sdk, args));
            server.RegisterMethod("chat/thread/all", (args) => ThreadAll(sdk, args));
            server.RegisterMethod("chat/thread/get", (args) => ThreadGet(sdk, args));
            server.RegisterMethod("chat/thread/delete", (args) => ThreadDelete(sdk, args));
            server.RegisterMethod("chat/thread/turns", (args) => ThreadTurns(sdk, args));
            server.RegisterMethod("chat/feedback/create", (args) => FeedbackCreate(sdk, args));
            server.RegisterMethod("chat/feedback/all", (args) => FeedbackAll(sdk, args));
            server.RegisterMethod("chat/feedback/delete", (args) => FeedbackDelete(sdk, args));
            server.RegisterMethod("chat/settings/get", (args) => SettingsGet(sdk, args));
            server.RegisterMethod("chat/settings/update", (args) => SettingsUpdate(sdk, args));
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
            server.RegisterMethod("chat/endpoint/create", (args) => EndpointCreate(sdk, args));
            server.RegisterMethod("chat/endpoint/get", (args) => EndpointGet(sdk, args));
            server.RegisterMethod("chat/endpoint/all", (args) => EndpointAll(sdk, args));
            server.RegisterMethod("chat/endpoint/update", (args) => EndpointUpdate(sdk, args));
            server.RegisterMethod("chat/endpoint/delete", (args) => EndpointDelete(sdk, args));
            server.RegisterMethod("chat/endpoint/test", (args) => EndpointTest(sdk, args));
            server.RegisterMethod("chat/endpoint/health", (args) => EndpointHealth(sdk, args));
            server.RegisterMethod("chat/endpoint/healthall", (args) => EndpointHealthAll(sdk, args));
            server.RegisterMethod("chat/completions", (args) => Completions(sdk, args));
            server.RegisterMethod("chat/thread/all", (args) => ThreadAll(sdk, args));
            server.RegisterMethod("chat/thread/get", (args) => ThreadGet(sdk, args));
            server.RegisterMethod("chat/thread/delete", (args) => ThreadDelete(sdk, args));
            server.RegisterMethod("chat/thread/turns", (args) => ThreadTurns(sdk, args));
            server.RegisterMethod("chat/feedback/create", (args) => FeedbackCreate(sdk, args));
            server.RegisterMethod("chat/feedback/all", (args) => FeedbackAll(sdk, args));
            server.RegisterMethod("chat/feedback/delete", (args) => FeedbackDelete(sdk, args));
            server.RegisterMethod("chat/settings/get", (args) => SettingsGet(sdk, args));
            server.RegisterMethod("chat/settings/update", (args) => SettingsUpdate(sdk, args));
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

            List<ChatEndpoint> endpoints = sdk.Chat.ReadEndpoints(tenantGuid, endpointType).GetAwaiter().GetResult();
            return Serializer.SerializeJson(endpoints, true);
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
            List<ChatEndpointHealth> health = sdk.Chat.ReadAllEndpointHealth(tenantGuid).GetAwaiter().GetResult();
            return Serializer.SerializeJson(health, true);
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
            List<ChatThread> threads = sdk.Chat.ReadThreads(tenantGuid, allUsers).GetAwaiter().GetResult();
            return Serializer.SerializeJson(threads, true);
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
            List<ChatTurn> turns = sdk.Chat.ReadThreadTurns(tenantGuid, threadGuid).GetAwaiter().GetResult();
            return Serializer.SerializeJson(turns, true);
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
            List<ChatFeedback> feedback = sdk.Chat.ReadFeedback(tenantGuid).GetAwaiter().GetResult();
            return Serializer.SerializeJson(feedback, true);
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

        #endregion
    }
}
