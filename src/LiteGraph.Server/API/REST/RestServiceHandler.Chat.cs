namespace LiteGraph.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;
    using ApiErrorResponse = LiteGraph.Server.Classes.ApiErrorResponse;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// Chat routes: endpoint management, completions, threads, feedback, and tenant chat settings.
    /// </summary>
    internal partial class RestServiceHandler
    {
        #region Chat-Route-Registration

        private void RegisterChatRoutes()
        {
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/chat/endpoints", ChatEndpointCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create chat endpoint", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/endpoints", ChatEndpointReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List chat endpoints", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/endpoints/health", ChatEndpointHealthReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all chat endpoint health", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}/health", ChatEndpointHealthReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get chat endpoint health", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}", ChatEndpointReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read chat endpoint", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}", ChatEndpointExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if chat endpoint exists", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}", ChatEndpointUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update chat endpoint", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}", ChatEndpointDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete chat endpoint", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}/test", ChatEndpointTestRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Test chat endpoint connectivity", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/chat/endpoints/{chatEndpointGuid}/preload", ChatEndpointPreloadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Preload chat endpoint model", "Chat"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/chat/completions", ChatCompletionRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Chat completion (SSE or JSON)", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/models", ChatModelsReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List selectable chat models", "Chat"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/chat/completions", ChatGraphCompletionOpenAiRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Graph-scoped OpenAI-compatible chat completion", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/chat/ollama", ChatGraphCompletionOllamaRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Graph-scoped Ollama-compatible chat", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/chat/models", ChatGraphModelsReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Graph-scoped OpenAI-compatible model list", "Chat"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/chat/threads", ChatThreadCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create chat thread", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/threads", ChatThreadReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List chat threads", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/threads/{chatThreadGuid}/turns", ChatThreadTurnsReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List chat thread turns", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/threads/{chatThreadGuid}", ChatThreadReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read chat thread", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/chat/threads/{chatThreadGuid}", ChatThreadUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update chat thread (rename)", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/chat/threads/{chatThreadGuid}", ChatThreadDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete chat thread", "Chat"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/chat/turns/{chatTurnGuid}/feedback", ChatFeedbackCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Submit chat feedback", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/feedback", ChatFeedbackReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List chat feedback", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/feedback/{chatFeedbackGuid}", ChatFeedbackReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read chat feedback", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/chat/feedback/{chatFeedbackGuid}", ChatFeedbackDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete chat feedback", "Chat"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/chat/settings", ChatSettingsReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read tenant chat settings", "Chat"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/chat/settings", ChatSettingsUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update tenant chat settings", "Chat"));
        }

        #endregion

        #region Chat-Endpoint-Routes

        private async Task ChatEndpointCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ChatEndpoint = _Serializer.DeserializeJson<ChatEndpoint>(ctx.Request.DataAsString);
            req.ChatEndpoint.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatEndpointCreate);
        }

        private async Task ChatEndpointReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            string endpointType = ctx.Request.Query.Elements.Get("endpointType");
            if (!String.IsNullOrEmpty(endpointType)
                && Enum.TryParse<ChatEndpointTypeEnum>(endpointType, true, out ChatEndpointTypeEnum parsed))
            {
                req.ChatEndpointTypeFilter = parsed;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatEndpointReadAll);
        }

        private async Task ChatEndpointReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatEndpointRead);
        }

        private async Task ChatEndpointExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatEndpointExists);
        }

        private async Task ChatEndpointUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ChatEndpoint = _Serializer.DeserializeJson<ChatEndpoint>(ctx.Request.DataAsString);
            req.ChatEndpoint.TenantGUID = req.TenantGUID.Value;
            req.ChatEndpoint.GUID = req.ChatEndpointGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatEndpointUpdate);
        }

        private async Task ChatEndpointDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatEndpointDelete);
        }

        private async Task ChatEndpointTestRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (!IsChatAdmin(req))
            {
                await NotAdmin(ctx);
                return;
            }

            if (_ChatService == null)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Chat service is not available.")));
                return;
            }

            ChatEndpoint endpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value).ConfigureAwait(false);
            if (endpoint == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                return;
            }

            ChatEndpointTestResult result = await _ChatService.TestEndpoint(endpoint).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(result, true));
        }

        private async Task ChatEndpointPreloadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            // Any tenant member may preload a model; this is a chat-use action, not chat management.
            if (!IsChatMember(req))
            {
                await NotAdmin(ctx);
                return;
            }

            if (_ChatService == null)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Chat service is not available.")));
                return;
            }

            ChatEndpoint endpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value).ConfigureAwait(false);
            if (endpoint == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                return;
            }

            if (!endpoint.Active)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "The specified chat endpoint is not active and cannot be preloaded.")));
                return;
            }

            if (endpoint.EndpointType != ChatEndpointTypeEnum.Completion)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Only completion endpoints can be preloaded.")));
                return;
            }

            ChatEndpointPreloadResult result = _ChatService.PreloadEndpoint(endpoint);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(result, true));
        }

        private async Task ChatEndpointHealthReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (!IsChatAdmin(req))
            {
                await NotAdmin(ctx);
                return;
            }

            if (_ChatHealth == null)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Chat endpoint health service is not available.")));
                return;
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(EnumerationResultBuilder.FromList(_ChatHealth.GetTenantHealth(req.TenantGUID.Value), req.Skip, req.MaxKeys), true));
        }

        private async Task ChatEndpointHealthReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (!IsChatAdmin(req))
            {
                await NotAdmin(ctx);
                return;
            }

            if (_ChatHealth == null)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Chat endpoint health service is not available.")));
                return;
            }

            ChatEndpoint endpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value).ConfigureAwait(false);
            if (endpoint == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                return;
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(_ChatHealth.GetHealth(endpoint), true));
        }

        #endregion

        #region Chat-Completion-Routes

        private async Task ChatCompletionRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            if (_ChatService == null)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Chat service is not available.")));
                return;
            }

            req.ChatCompletionRequest = _Serializer.DeserializeJson<ChatCompletionRequest>(ctx.Request.DataAsString);

            using (System.Diagnostics.Activity activity = StartInternalActivity("litegraph.rest.chat", req))
            {
                using System.Threading.CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
                await _ChatService.ProcessCompletion(ctx, req, timeoutCts.Token).ConfigureAwait(false);
            }
        }

        #endregion

        #region Chat-Graph-Compat-Routes

        private async Task ChatGraphCompletionOpenAiRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (!await ChatCompatPreflight(ctx).ConfigureAwait(false)) return;

            OpenAiChatCompletionRequest request;
            try
            {
                request = _Serializer.DeserializeJson<OpenAiChatCompletionRequest>(ctx.Request.DataAsString);
            }
            catch (Exception e)
            {
                await SendChatCompatBadRequest(ctx, "The request body could not be parsed: " + e.Message).ConfigureAwait(false);
                return;
            }

            using (System.Diagnostics.Activity activity = StartInternalActivity("litegraph.rest.chat.compat.openai", req))
            {
                using System.Threading.CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
                await _ChatService.ProcessOpenAiGraphCompletion(ctx, req, request, timeoutCts.Token).ConfigureAwait(false);
            }
        }

        private async Task ChatGraphCompletionOllamaRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (!await ChatCompatPreflight(ctx).ConfigureAwait(false)) return;

            OllamaChatRequest request;
            try
            {
                request = _Serializer.DeserializeJson<OllamaChatRequest>(ctx.Request.DataAsString);
            }
            catch (Exception e)
            {
                await SendChatCompatBadRequest(ctx, "The request body could not be parsed: " + e.Message).ConfigureAwait(false);
                return;
            }

            using (System.Diagnostics.Activity activity = StartInternalActivity("litegraph.rest.chat.compat.ollama", req))
            {
                using System.Threading.CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
                await _ChatService.ProcessOllamaGraphCompletion(ctx, req, request, timeoutCts.Token).ConfigureAwait(false);
            }
        }

        private async Task ChatGraphModelsReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using System.Threading.CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            Graph graph = null;
            try
            {
                graph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token: timeoutCts.Token).ConfigureAwait(false);
            }
            catch (KeyNotFoundException)
            {
            }
            catch (ArgumentException)
            {
            }

            if (graph == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new OpenAiErrorResponse("The specified graph could not be found in this tenant.", "invalid_request_error"), true)).ConfigureAwait(false);
                return;
            }

            OpenAiModelList list = new OpenAiModelList();

            await foreach (ChatEndpoint endpoint in _LiteGraph.ChatEndpoint.ReadAllInTenant(req.TenantGUID.Value, ChatEndpointTypeEnum.Completion, EnumerationOrderEnum.CreatedAscending, 0, timeoutCts.Token).ConfigureAwait(false))
            {
                if (!endpoint.Active) continue;

                list.Data.Add(new OpenAiModelEntry
                {
                    Id = endpoint.Name,
                    Created = new DateTimeOffset(DateTime.SpecifyKind(endpoint.CreatedUtc, DateTimeKind.Utc)).ToUnixTimeSeconds(),
                    OwnedBy = "litegraph"
                });
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(list, true)).ConfigureAwait(false);
        }

        private async Task<bool> ChatCompatPreflight(HttpContextBase ctx)
        {
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await SendChatCompatBadRequest(ctx, "A request body is required.").ConfigureAwait(false);
                return false;
            }

            if (_ChatService == null)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new OpenAiErrorResponse("Chat service is not available.", "server_error"), true)).ConfigureAwait(false);
                return false;
            }

            return true;
        }

        private async Task SendChatCompatBadRequest(HttpContextBase ctx, string message)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new OpenAiErrorResponse(message, "invalid_request_error"), true)).ConfigureAwait(false);
        }

        #endregion

        #region Chat-Thread-Routes

        private async Task ChatModelsReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatModelsReadAll);
        }

        private async Task ChatThreadCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (!String.IsNullOrEmpty(ctx.Request.DataAsString))
                req.ChatThread = _Serializer.DeserializeJson<ChatThread>(ctx.Request.DataAsString);
            else
                req.ChatThread = new ChatThread();

            req.ChatThread.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatThreadCreate);
        }

        private async Task ChatThreadReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (ctx.Request.Query.Elements.AllKeys.Contains("all")) req.AllUsers = true;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatThreadReadAll);
        }

        private async Task ChatThreadReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatThreadRead);
        }

        private async Task ChatThreadUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ChatThread = _Serializer.DeserializeJson<ChatThread>(ctx.Request.DataAsString);
            req.ChatThread.TenantGUID = req.TenantGUID.Value;
            req.ChatThread.GUID = req.ChatThreadGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatThreadUpdate);
        }

        private async Task ChatThreadDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatThreadDelete);
        }

        private async Task ChatThreadTurnsReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatThreadTurnsRead);
        }

        #endregion

        #region Chat-Feedback-Routes

        private async Task ChatFeedbackCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ChatFeedback = _Serializer.DeserializeJson<ChatFeedback>(ctx.Request.DataAsString);
            req.ChatFeedback.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatFeedbackCreate);
        }

        private async Task ChatFeedbackReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatFeedbackReadAll);
        }

        private async Task ChatFeedbackReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatFeedbackRead);
        }

        private async Task ChatFeedbackDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatFeedbackDelete);
        }

        #endregion

        #region Chat-Settings-Routes

        private async Task ChatSettingsReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatSettingsRead);
        }

        private async Task ChatSettingsUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ChatSettings = _Serializer.DeserializeJson<ChatSettings>(ctx.Request.DataAsString);
            req.ChatSettings.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.ChatSettingsUpdate);
        }

        #endregion

        #region Chat-Private-Methods

        private bool IsChatAdmin(RequestContext req)
        {
            return req.Authentication.IsSystemAdmin
                || (req.Authentication.IsTenantAdmin
                    && req.TenantGUID.HasValue
                    && req.Authentication.TenantGUID.HasValue
                    && req.TenantGUID.Value.Equals(req.Authentication.TenantGUID.Value));
        }

        private bool IsChatMember(RequestContext req)
        {
            // Any authenticated principal of the target tenant, or a system administrator.
            return req.Authentication.IsSystemAdmin
                || (req.TenantGUID.HasValue
                    && req.Authentication.TenantGUID.HasValue
                    && req.TenantGUID.Value.Equals(req.Authentication.TenantGUID.Value));
        }

        #endregion
    }
}
