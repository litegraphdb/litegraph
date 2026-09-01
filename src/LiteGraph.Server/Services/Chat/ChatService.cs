namespace LiteGraph.Server.Services.Chat
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using PolyPrompt.Clients;
    using PolyPrompt.Models;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = LiteGraph.Server.Classes.ApiErrorResponse;

    /// <summary>
    /// Chat orchestration: resolves endpoints, rebuilds conversation context, retrieves graph context,
    /// runs the tool loop against PolyPrompt, persists the turn with telemetry, and writes the
    /// streamed or buffered response.  Providers are always consumed via streaming internally so token
    /// usage and time-to-first-token are captured even for buffered client responses.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    internal class ChatService : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Header = "[ChatService] ";
        private const int _DefaultHistoryBudgetTokens = 16384;
        private readonly Settings _Settings;
        private readonly LoggingModule _Logging;
        private readonly LiteGraphClient _LiteGraph;
        private readonly ObservabilityService _Observability;
        private readonly ChatToolDispatcher _Dispatcher;
        private readonly ChatEndpointHealthService _Health;
        private readonly Serializer _Serializer = new Serializer();
        private readonly ConcurrentDictionary<Guid, ClientCacheEntry> _Clients = new ConcurrentDictionary<Guid, ClientCacheEntry>();
        private readonly SemaphoreSlim _GlobalLimiter;
        private readonly Timer _RetentionTimer;
        private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Server settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="liteGraph">LiteGraph client.</param>
        /// <param name="serviceHandler">Agnostic service handler backing tool dispatch.</param>
        /// <param name="authorization">Authorization service.</param>
        /// <param name="observability">Observability service.</param>
        /// <param name="health">Endpoint health service, notified on endpoint changes.</param>
        internal ChatService(
            Settings settings,
            LoggingModule logging,
            LiteGraphClient liteGraph,
            ServiceHandler serviceHandler,
            AuthorizationService authorization,
            ObservabilityService observability,
            ChatEndpointHealthService health)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = liteGraph ?? throw new ArgumentNullException(nameof(liteGraph));
            if (serviceHandler == null) throw new ArgumentNullException(nameof(serviceHandler));
            if (authorization == null) throw new ArgumentNullException(nameof(authorization));
            _Observability = observability;
            _Health = health;
            _Dispatcher = new ChatToolDispatcher(ChatToolCatalog.Build(serviceHandler), authorization, logging, observability);
            _GlobalLimiter = new SemaphoreSlim(_Settings.Chat.MaxConcurrentChats, _Settings.Chat.MaxConcurrentChats);
            _RetentionTimer = new Timer(RetentionSweep, null, TimeSpan.FromMinutes(10), TimeSpan.FromHours(1));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Invalidate the cached provider client for an endpoint after update or delete.
        /// </summary>
        /// <param name="endpointGuid">Endpoint GUID.</param>
        internal void InvalidateEndpoint(Guid endpointGuid)
        {
            if (_Clients.TryRemove(endpointGuid, out ClientCacheEntry entry)) entry.Dispose();
        }

        /// <summary>
        /// Test connectivity to an endpoint and, where the provider supports it, list its models.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Test result.</returns>
        internal async Task<ChatEndpointTestResult> TestEndpoint(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            ChatEndpointTestResult result = new ChatEndpointTestResult();
            Stopwatch sw = Stopwatch.StartNew();

            using (CompletionClientBase client = CreateClient(endpoint))
            {
                try
                {
                    result.Reachable = await client.ValidateConnectivityAsync(token).ConfigureAwait(false);

                    if (result.Reachable && endpoint.Provider != ChatProviderTypeEnum.VoyageAI)
                    {
                        List<string> models = new List<string>();
                        await foreach (ModelInformation model in client.ListModelsAsync(token).ConfigureAwait(false))
                        {
                            if (!String.IsNullOrEmpty(model.Name)) models.Add(model.Name);
                        }

                        result.Models = models;
                        result.ModelExists = models.Any(m =>
                            String.Equals(m, endpoint.Model, StringComparison.OrdinalIgnoreCase)
                            || m.StartsWith(endpoint.Model + ":", StringComparison.OrdinalIgnoreCase));
                    }

                    if (!result.Reachable) result.Error = "The endpoint did not respond to the connectivity probe.";
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (NotSupportedException)
                {
                    // Model listing unsupported for this provider; connectivity verdict stands.
                }
                catch (Exception e)
                {
                    result.Reachable = false;
                    result.Error = e.Message;
                }
            }

            sw.Stop();
            result.RuntimeMs = sw.Elapsed.TotalMilliseconds;
            return result;
        }

        /// <summary>
        /// Process a chat completion request end to end, writing the HTTP response (SSE or JSON) itself.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="req">Request context.</param>
        /// <param name="token">Cancellation token.</param>
        internal async Task ProcessCompletion(HttpContextBase ctx, RequestContext req, CancellationToken token = default)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ChatCompletionRequest == null) throw new ArgumentNullException(nameof(req.ChatCompletionRequest));

            ChatCompletionRequest request = req.ChatCompletionRequest;

            if (!_Settings.Chat.Enable)
            {
                await SendJsonError(ctx, 503, ApiErrorEnum.BadRequest, "Chat is disabled on this server.").ConfigureAwait(false);
                return;
            }

            if (String.IsNullOrEmpty(request.Message))
            {
                await SendJsonError(ctx, 400, ApiErrorEnum.BadRequest, "A message is required.").ConfigureAwait(false);
                return;
            }

            if (!req.Authentication.UserGUID.HasValue)
            {
                await SendJsonError(ctx, 400, ApiErrorEnum.BadRequest, "Chat requires a user principal.").ConfigureAwait(false);
                return;
            }

            if (!await _GlobalLimiter.WaitAsync(0, token).ConfigureAwait(false))
            {
                await SendJsonError(ctx, 429, ApiErrorEnum.BadRequest, "The server is at its concurrent chat capacity.").ConfigureAwait(false);
                return;
            }

            _Observability?.IncrementChatActive();

            try
            {
                await ProcessCompletionInternal(ctx, req, request, token).ConfigureAwait(false);
            }
            finally
            {
                _Observability?.DecrementChatActive();
                _GlobalLimiter.Release();
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                _TokenSource.Cancel();
                _RetentionTimer.Dispose();
                foreach (ClientCacheEntry entry in _Clients.Values) entry.Dispose();
                _Clients.Clear();
                _GlobalLimiter.Dispose();
                _TokenSource.Dispose();
            }

            _Disposed = true;
        }

        private async Task ProcessCompletionInternal(HttpContextBase ctx, RequestContext req, ChatCompletionRequest request, CancellationToken token)
        {
            Guid tenantGuid = req.TenantGUID.Value;
            Stopwatch overall = Stopwatch.StartNew();

            ChatSettings tenantSettings = await _LiteGraph.ChatSettings.ReadByTenant(tenantGuid, token).ConfigureAwait(false);
            if (tenantSettings == null) tenantSettings = new ChatSettings { TenantGUID = tenantGuid };

            if (!tenantSettings.EnableChat)
            {
                await SendJsonError(ctx, 403, ApiErrorEnum.AuthorizationFailed, "Chat is disabled for this tenant.").ConfigureAwait(false);
                return;
            }

            #region Thread

            ChatThread thread = null;

            if (request.ThreadGUID != null)
            {
                thread = await _LiteGraph.ChatThread.ReadByGuid(tenantGuid, request.ThreadGUID.Value, token).ConfigureAwait(false);
                if (thread == null)
                {
                    await SendJsonError(ctx, 404, ApiErrorEnum.NotFound, "The specified chat thread could not be found.").ConfigureAwait(false);
                    return;
                }

                bool isAdmin = req.Authentication.IsSystemAdmin
                    || (req.Authentication.IsTenantAdmin && req.Authentication.TenantGUID.HasValue && req.Authentication.TenantGUID.Value.Equals(tenantGuid));
                if (!isAdmin && !thread.UserGUID.Equals(req.Authentication.UserGUID.Value))
                {
                    await SendJsonError(ctx, 403, ApiErrorEnum.AuthorizationFailed, "The specified chat thread belongs to another user.").ConfigureAwait(false);
                    return;
                }
            }
            else
            {
                thread = await _LiteGraph.ChatThread.Create(new ChatThread
                {
                    TenantGUID = tenantGuid,
                    UserGUID = req.Authentication.UserGUID.Value,
                    GraphGUID = request.GraphGUID
                }, token).ConfigureAwait(false);
            }

            #endregion

            #region Endpoints

            Guid? completionGuid = (request.CompletionEndpointGUID != null ? request.CompletionEndpointGUID : tenantSettings.DefaultCompletionEndpointGUID);
            ChatEndpoint completionEndpoint = null;

            if (completionGuid != null)
            {
                completionEndpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(tenantGuid, completionGuid.Value, token).ConfigureAwait(false);
            }
            else
            {
                completionEndpoint = await FirstActiveEndpoint(tenantGuid, ChatEndpointTypeEnum.Completion, token).ConfigureAwait(false);
            }

            if (completionEndpoint == null || completionEndpoint.EndpointType != ChatEndpointTypeEnum.Completion || !completionEndpoint.Active)
            {
                await SendJsonError(ctx, 400, ApiErrorEnum.BadRequest, "No usable completion endpoint is configured.  Create one, set a tenant default, or supply CompletionEndpointGUID.").ConfigureAwait(false);
                return;
            }

            ChatEndpoint embeddingEndpoint = null;
            Guid? embeddingGuid = (request.EmbeddingEndpointGUID != null ? request.EmbeddingEndpointGUID : tenantSettings.DefaultEmbeddingEndpointGUID);
            if (embeddingGuid != null)
            {
                embeddingEndpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(tenantGuid, embeddingGuid.Value, token).ConfigureAwait(false);
            }
            else
            {
                embeddingEndpoint = await FirstActiveEndpoint(tenantGuid, ChatEndpointTypeEnum.Embedding, token).ConfigureAwait(false);
            }
            if (embeddingEndpoint != null && (embeddingEndpoint.EndpointType != ChatEndpointTypeEnum.Embedding || !embeddingEndpoint.Active)) embeddingEndpoint = null;

            #endregion

            ChatTurn turn = new ChatTurn
            {
                TenantGUID = tenantGuid,
                ThreadGUID = thread.GUID,
                UserMessage = request.Message,
                CompletionEndpointGUID = completionEndpoint.GUID,
                Provider = completionEndpoint.Provider,
                Model = completionEndpoint.Model,
                TraceId = Activity.Current?.TraceId.ToString(),
                Success = false
            };

            bool streaming = request.Stream;
            List<object> toolTranscript = new List<object>();
            ChatCompletionResult result = new ChatCompletionResult
            {
                ThreadGUID = thread.GUID,
                TurnGUID = turn.GUID,
                Provider = completionEndpoint.Provider,
                Model = completionEndpoint.Model
            };

            if (streaming)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ServerSentEvents = true;
                await SendSse(ctx, new { @event = "started", threadGuid = thread.GUID, turnGuid = turn.GUID }).ConfigureAwait(false);
            }

            using (Activity activity = _Observability?.StartActivity("chat.turn", ActivityKind.Internal))
            {
                activity?.SetTag("litegraph.tenant.guid", tenantGuid.ToString());
                activity?.SetTag("litegraph.chat.thread.guid", thread.GUID.ToString());
                activity?.SetTag("litegraph.chat.turn.guid", turn.GUID.ToString());
                activity?.SetTag("litegraph.chat.provider", completionEndpoint.Provider.ToString());
                activity?.SetTag("litegraph.chat.model", completionEndpoint.Model);
                activity?.SetTag("litegraph.chat.streamed", streaming);

                try
                {
                    #region Context

                    List<ChatMessage> messages = new List<ChatMessage>();
                    messages.Add(ChatMessage.System(BuildSystemPrompt(tenantSettings, request, thread)));
                    int historyBudgetTokens = _DefaultHistoryBudgetTokens;
                    if (completionEndpoint.ContextWindowTokens > 0)
                        historyBudgetTokens = Math.Max(1024, completionEndpoint.ContextWindowTokens - completionEndpoint.MaxOutputTokens);
                    await AppendHistory(messages, tenantGuid, thread.GUID, historyBudgetTokens, token).ConfigureAwait(false);

                    #endregion

                    #region Retrieval

                    bool ragEnabled = (request.EnableRag != null ? request.EnableRag.Value : tenantSettings.EnableRag);
                    Guid? ragGraphGuid = (thread.GraphGUID != null ? thread.GraphGUID : request.GraphGUID);

                    if (ragEnabled && ragGraphGuid != null && embeddingEndpoint != null)
                    {
                        await RunRetrieval(ctx, req, request, tenantSettings, embeddingEndpoint, ragGraphGuid.Value, messages, turn, streaming, token).ConfigureAwait(false);
                    }

                    messages.Add(ChatMessage.User(request.Message));

                    #endregion

                    #region Tool-Loop

                    bool toolsEnabled = (request.EnableTools != null ? request.EnableTools.Value : tenantSettings.EnableTools);
                    List<ToolDefinition> tools = new List<ToolDefinition>();

                    if (toolsEnabled)
                    {
                        foreach (ChatToolDefinition tool in _Dispatcher.GetAdvertisedTools(tenantSettings.EnableMutationTools))
                        {
                            string schemaJson = _Serializer.SerializeJson(tool.Schema, false);
                            Dictionary<string, object> parameters = _Serializer.DeserializeJson<Dictionary<string, object>>(schemaJson);
                            tools.Add(ToolDefinition.Function(tool.Name, tool.Description, parameters));
                        }
                    }

                    ClientCacheEntry entry = GetClient(completionEndpoint);
                    int maxIterations = Math.Min(tenantSettings.MaxToolIterations, _Settings.Chat.MaxToolIterationsCap);

                    Stopwatch limiterWait = Stopwatch.StartNew();
                    await entry.Limiter.WaitAsync(token).ConfigureAwait(false);
                    limiterWait.Stop();
                    turn.LimiterWaitMs = limiterWait.Elapsed.TotalMilliseconds;

                    try
                    {
                        await RunToolLoop(ctx, req, request, tenantSettings, completionEndpoint, embeddingEndpoint, entry, tools, messages, maxIterations, turn, result, toolTranscript, streaming, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        entry.Limiter.Release();
                    }

                    #endregion

                    turn.Success = true;
                }
                catch (OperationCanceledException)
                {
                    turn.Error = "The request was cancelled.";
                    throw;
                }
                catch (ChatUpstreamException cue)
                {
                    turn.Error = cue.Message;
                    turn.HttpStatus = cue.StatusCode;
                    activity?.SetTag("litegraph.chat.error", cue.Message);

                    if (streaming) await SendSse(ctx, new { @event = "error", message = cue.Message, statusCode = cue.StatusCode }).ConfigureAwait(false);
                    else await SendJsonError(ctx, 502, ApiErrorEnum.BadRequest, cue.Message).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    turn.Error = e.Message;
                    activity?.SetTag("litegraph.chat.error", e.Message);
                    _Logging.Warn(_Header + "chat turn " + turn.GUID + " failed: " + e.Message);

                    if (streaming) await SendSse(ctx, new { @event = "error", message = e.Message }).ConfigureAwait(false);
                    else await SendJsonError(ctx, 500, ApiErrorEnum.InternalError, e.Message).ConfigureAwait(false);
                }
                finally
                {
                    overall.Stop();
                    turn.TotalDurationMs = overall.Elapsed.TotalMilliseconds;
                    result.TotalDurationMs = turn.TotalDurationMs;
                    if (toolTranscript.Count > 0) turn.ToolTranscriptJson = _Serializer.SerializeJson(toolTranscript, false);
                    turn.TelemetryJson = _Serializer.SerializeJson(result, false);

                    await PersistTurn(thread, turn, token).ConfigureAwait(false);

                    activity?.SetTag("litegraph.chat.success", turn.Success);
                    activity?.SetTag("litegraph.chat.tool.calls", turn.ToolCallCount);
                    activity?.SetTag("litegraph.chat.tokens.prompt", turn.PromptTokens);
                    activity?.SetTag("litegraph.chat.tokens.completion", turn.CompletionTokens);
                    activity?.SetTag("litegraph.chat.retries", turn.RetryCount);

                    _Observability?.RecordChatRequest(
                        completionEndpoint.Provider.ToString(),
                        completionEndpoint.Model,
                        streaming,
                        (turn.Success ? 200 : (turn.HttpStatus != null ? turn.HttpStatus.Value : 500)),
                        turn.TotalDurationMs,
                        turn.TimeToFirstTokenMs,
                        turn.PromptTokens,
                        turn.CompletionTokens,
                        turn.TokensPerSecondOverall,
                        turn.ToolLoopIterations,
                        turn.RetryCount);
                }

                if (turn.Success)
                {
                    if (streaming)
                    {
                        await SendSse(ctx, new { @event = "usage", usage = result }).ConfigureAwait(false);
                        await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 200;
                        ctx.Response.ContentType = Constants.JsonContentType;
                        await ctx.Response.Send(_Serializer.SerializeJson(result, true)).ConfigureAwait(false);
                    }

                    await GenerateTitleIfNeeded(thread, request.Message, result.Message, completionEndpoint, token).ConfigureAwait(false);
                }
                else if (streaming)
                {
                    await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
                }
            }
        }

        private async Task RunRetrieval(
            HttpContextBase ctx,
            RequestContext req,
            ChatCompletionRequest request,
            ChatSettings tenantSettings,
            ChatEndpoint embeddingEndpoint,
            Guid graphGuid,
            List<ChatMessage> messages,
            ChatTurn turn,
            bool streaming,
            CancellationToken token)
        {
            using (Activity activity = _Observability?.StartActivity("chat.rag.search", ActivityKind.Internal))
            {
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    turn.EmbeddingEndpointGUID = embeddingEndpoint.GUID;

                    Stopwatch embedSw = Stopwatch.StartNew();
                    List<float> embeddings = await EmbedText(embeddingEndpoint, request.Message, token).ConfigureAwait(false);
                    embedSw.Stop();
                    turn.EmbeddingDurationMs = embedSw.Elapsed.TotalMilliseconds;

                    if (embeddings == null || embeddings.Count < 1) return;

                    VectorSearchRequest vsr = new VectorSearchRequest
                    {
                        TenantGUID = turn.TenantGUID,
                        GraphGUID = graphGuid,
                        TopK = (request.RagTopK != null ? request.RagTopK.Value : tenantSettings.RagTopK),
                        MinimumScore = (float)tenantSettings.RagScoreThreshold,
                        Embeddings = embeddings
                    };

                    List<VectorSearchResult> results = new List<VectorSearchResult>();
                    await foreach (VectorSearchResult vres in _LiteGraph.Vector.Search(vsr, token).WithCancellation(token).ConfigureAwait(false))
                    {
                        results.Add(vres);
                    }

                    sw.Stop();
                    turn.RetrievalDurationMs = sw.Elapsed.TotalMilliseconds;
                    turn.RetrievedChunkCount = results.Count;
                    _Observability?.RecordChatRag(sw.Elapsed.TotalMilliseconds);
                    activity?.SetTag("litegraph.chat.rag.results", results.Count);

                    if (results.Count < 1) return;

                    System.Text.StringBuilder context = new System.Text.StringBuilder();
                    context.AppendLine("Relevant graph context retrieved by semantic search (most similar first):");
                    List<object> chunkSummaries = new List<object>();

                    foreach (VectorSearchResult vres in results)
                    {
                        string nodeName = (vres.Node != null ? vres.Node.Name : null);
                        Guid? nodeGuid = (vres.Node != null ? vres.Node.GUID : (Guid?)null);
                        context.AppendLine("- Node " + (nodeName ?? nodeGuid?.ToString() ?? "unknown")
                            + " (guid " + nodeGuid + ", score " + (vres.Score != null ? vres.Score.Value.ToString("F4") : "n/a") + ")"
                            + (vres.Node != null && vres.Node.Data != null ? ": " + Truncate(_Serializer.SerializeJson(vres.Node.Data, false), 500) : String.Empty));
                        chunkSummaries.Add(new { nodeGuid = nodeGuid, name = nodeName, score = vres.Score });
                    }

                    messages.Add(ChatMessage.System(context.ToString()));

                    if (streaming) await SendSse(ctx, new { @event = "retrieval", chunks = chunkSummaries }).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "retrieval failed (continuing without context): " + e.Message);
                    activity?.SetTag("litegraph.chat.rag.error", e.Message);
                }
            }
        }

        private async Task RunToolLoop(
            HttpContextBase ctx,
            RequestContext req,
            ChatCompletionRequest request,
            ChatSettings tenantSettings,
            ChatEndpoint completionEndpoint,
            ChatEndpoint embeddingEndpoint,
            ClientCacheEntry entry,
            List<ToolDefinition> tools,
            List<ChatMessage> messages,
            int maxIterations,
            ChatTurn turn,
            ChatCompletionResult result,
            List<object> toolTranscript,
            bool streaming,
            CancellationToken token)
        {
            System.Text.StringBuilder content = new System.Text.StringBuilder();
            System.Text.StringBuilder reasoning = new System.Text.StringBuilder();
            Func<string, CancellationToken, Task<List<float>>> embedText = null;
            if (embeddingEndpoint != null) embedText = (text, ct) => EmbedText(embeddingEndpoint, text, ct);

            int iteration = 0;

            while (true)
            {
                iteration++;
                turn.ToolLoopIterations = iteration;
                bool finalIteration = (iteration >= maxIterations);

                ToolChatRequest toolRequest = new ToolChatRequest
                {
                    Messages = messages,
                    Tools = (finalIteration ? new List<ToolDefinition>() : tools),
                    ToolChoice = (finalIteration || tools.Count < 1 ? "none" : "auto"),
                    Temperature = (request.Temperature != null ? request.Temperature : completionEndpoint.Temperature),
                    MaxTokens = (request.MaxOutputTokens != null ? request.MaxOutputTokens : completionEndpoint.MaxOutputTokens)
                };

                ToolChatStreamingResponse response = await CallProviderWithRetry(entry, toolRequest, turn, token).ConfigureAwait(false);

                bool sawContent = false;

                await foreach (ToolChatStreamingChunk chunk in response.Chunks.WithCancellation(token).ConfigureAwait(false))
                {
                    if (!String.IsNullOrEmpty(chunk.Text))
                    {
                        sawContent = true;
                        content.Append(chunk.Text);
                        if (streaming) await SendSse(ctx, new { @event = "delta", content = chunk.Text }).ConfigureAwait(false);
                    }

                    if (!String.IsNullOrEmpty(chunk.ReasoningText))
                    {
                        reasoning.Append(chunk.ReasoningText);
                        if (streaming) await SendSse(ctx, new { @event = "thinking", content = chunk.ReasoningText }).ConfigureAwait(false);
                    }
                }

                if (response.Usage != null)
                {
                    turn.PromptTokens = response.Usage.PromptTokens;
                    turn.CompletionTokens = response.Usage.CompletionTokens;
                }

                if (response.TimeToFirstTokenMs >= 0) turn.TimeToFirstTokenMs = response.TimeToFirstTokenMs;
                if (response.TimeToLastTokenMs >= 0) turn.TimeToLastTokenMs = response.TimeToLastTokenMs;
                if (response.OverallTokensPerSecond > 0) turn.TokensPerSecondOverall = response.OverallTokensPerSecond;
                if (response.InterTokenTokensPerSecond > 0) turn.TokensPerSecondGeneration = response.InterTokenTokensPerSecond;

                if (response.ToolCalls != null && response.ToolCalls.Count > 0 && !finalIteration)
                {
                    messages.Add(response.ToAssistantMessage());

                    foreach (ToolCall call in response.ToolCalls)
                    {
                        token.ThrowIfCancellationRequested();
                        turn.ToolCallCount++;

                        if (streaming) await SendSse(ctx, new { @event = "tool_call", name = call.Name, arguments = call.ArgumentsJson, iteration = iteration }).ConfigureAwait(false);

                        ChatToolExecutionResult toolResult;
                        using (Activity toolActivity = _Observability?.StartActivity("chat.tool.execute", ActivityKind.Internal))
                        {
                            toolActivity?.SetTag("litegraph.chat.tool", call.Name);
                            toolResult = await _Dispatcher.Execute(
                                call.Name,
                                call.ArgumentsJson,
                                req.Authentication,
                                turn.TenantGUID,
                                tenantSettings.EnableMutationTools,
                                embedText,
                                token).ConfigureAwait(false);
                            toolActivity?.SetTag("litegraph.chat.tool.success", toolResult.Success);
                        }

                        toolTranscript.Add(new
                        {
                            iteration = iteration,
                            name = call.Name,
                            arguments = call.ArgumentsJson,
                            success = toolResult.Success,
                            error = toolResult.Error,
                            runtimeMs = toolResult.DurationMs
                        });

                        if (streaming) await SendSse(ctx, new { @event = "tool_result", name = call.Name, success = toolResult.Success, error = toolResult.Error, runtimeMs = toolResult.DurationMs }).ConfigureAwait(false);

                        string toolContent = (toolResult.Success ? toolResult.ResultJson : _Serializer.SerializeJson(new { error = toolResult.Error }, false));
                        messages.Add(ChatMessage.ToolResult(call.Id, call.Name, Truncate(toolContent, 65536)));
                    }

                    continue;
                }

                if (sawContent || response.ToolCalls == null || response.ToolCalls.Count < 1 || finalIteration)
                {
                    break;
                }
            }

            result.Message = content.ToString();
            result.Reasoning = (reasoning.Length > 0 ? reasoning.ToString() : null);
            result.PromptTokens = turn.PromptTokens;
            result.CompletionTokens = turn.CompletionTokens;
            result.TimeToFirstTokenMs = turn.TimeToFirstTokenMs;
            result.TimeToLastTokenMs = turn.TimeToLastTokenMs;
            result.TokensPerSecondOverall = turn.TokensPerSecondOverall;
            result.ToolCallCount = turn.ToolCallCount;
            result.ToolLoopIterations = turn.ToolLoopIterations;
            result.RetrievedChunkCount = turn.RetrievedChunkCount;
            result.RetryCount = turn.RetryCount;

            turn.AssistantResponse = result.Message;
            turn.Reasoning = result.Reasoning;
        }

        private async Task<ToolChatStreamingResponse> CallProviderWithRetry(ClientCacheEntry entry, ToolChatRequest toolRequest, ChatTurn turn, CancellationToken token)
        {
            int attempt = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                ToolChatStreamingResponse response = null;
                string error = null;
                int? statusCode = null;

                try
                {
                    using (Activity activity = _Observability?.StartActivity("chat.llm.request", ActivityKind.Client))
                    {
                        activity?.SetTag("litegraph.chat.provider", turn.Provider.ToString());
                        activity?.SetTag("litegraph.chat.model", turn.Model);
                        activity?.SetTag("litegraph.chat.attempt", attempt + 1);
                        response = await entry.Client.ToolChatStreamingAsync(toolRequest, token).ConfigureAwait(false);
                    }

                    if (response != null && response.Success)
                    {
                        if (turn.InferenceConnectionMs == null && response.OverallRuntimeMs >= 0) turn.InferenceConnectionMs = response.OverallRuntimeMs;
                        return response;
                    }

                    error = (response != null && !String.IsNullOrEmpty(response.Error) ? response.Error : "The provider returned an unsuccessful response.");
                    statusCode = response?.StatusCode;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    error = e.Message;
                }

                attempt++;
                bool retryable = (statusCode == null || statusCode.Value == 429 || statusCode.Value >= 500);

                if (!retryable || attempt > _Settings.Chat.MaxRetries)
                {
                    throw new ChatUpstreamException(error, statusCode);
                }

                turn.RetryCount = attempt;
                int backoffMs = _Settings.Chat.RetryBackoffMs * (1 << (attempt - 1));
                _Logging.Warn(_Header + "provider call failed (attempt " + attempt + ", retrying in " + backoffMs + "ms): " + error);
                await Task.Delay(backoffMs, token).ConfigureAwait(false);
            }
        }

        private async Task<List<float>> EmbedText(ChatEndpoint embeddingEndpoint, string text, CancellationToken token)
        {
            ClientCacheEntry entry = GetClient(embeddingEndpoint);
            Stopwatch sw = Stopwatch.StartNew();
            bool success = false;

            try
            {
                using (Activity activity = _Observability?.StartActivity("chat.rag.embed", ActivityKind.Client))
                {
                    activity?.SetTag("litegraph.chat.provider", embeddingEndpoint.Provider.ToString());
                    activity?.SetTag("litegraph.chat.model", embeddingEndpoint.Model);

                    EmbeddingResponse response = await entry.Client.EmbedAsync(text, null, token).ConfigureAwait(false);
                    if (!response.Success || response.Embeddings == null || response.Embeddings.Count < 1)
                    {
                        throw new ChatUpstreamException(
                            "Embedding request failed: " + (response.Error ?? "no embeddings returned."),
                            (response.StatusCode > 0 ? response.StatusCode : (int?)null));
                    }

                    success = true;
                    return response.Embeddings[0].Embedding.ToList();
                }
            }
            finally
            {
                sw.Stop();
                _Observability?.RecordChatEmbedding(embeddingEndpoint.Provider.ToString(), embeddingEndpoint.Model, success, sw.Elapsed.TotalMilliseconds);
            }
        }

        private async Task<ChatEndpoint> FirstActiveEndpoint(Guid tenantGuid, ChatEndpointTypeEnum endpointType, CancellationToken token)
        {
            await foreach (ChatEndpoint candidate in _LiteGraph.ChatEndpoint.ReadAllInTenant(tenantGuid, endpointType, EnumerationOrderEnum.CreatedAscending, 0, token).ConfigureAwait(false))
            {
                if (candidate.Active) return candidate;
            }

            return null;
        }

        private ClientCacheEntry GetClient(ChatEndpoint endpoint)
        {
            ClientCacheEntry entry = _Clients.GetOrAdd(endpoint.GUID, _ => new ClientCacheEntry(CreateClient(endpoint), endpoint));

            if (entry.LastUpdateUtc != endpoint.LastUpdateUtc)
            {
                InvalidateEndpoint(endpoint.GUID);
                entry = _Clients.GetOrAdd(endpoint.GUID, _ => new ClientCacheEntry(CreateClient(endpoint), endpoint));
            }

            return entry;
        }

        private CompletionClientBase CreateClient(ChatEndpoint endpoint)
        {
            CompletionClientBase client;

            switch (endpoint.Provider)
            {
                case ChatProviderTypeEnum.Ollama:
                    client = new OllamaClient(endpoint.Endpoint, endpoint.ApiKey, _Logging);
                    break;
                case ChatProviderTypeEnum.Gemini:
                    client = new GeminiClient(endpoint.Endpoint, endpoint.ApiKey, _Logging);
                    break;
                case ChatProviderTypeEnum.Anthropic:
                    client = new AnthropicClient(endpoint.Endpoint, endpoint.ApiKey, _Logging);
                    break;
                case ChatProviderTypeEnum.VoyageAI:
                    client = new VoyageAiClient(endpoint.Endpoint, endpoint.ApiKey, _Logging);
                    break;
                default:
                    client = new OpenAiClient(endpoint.Endpoint, endpoint.ApiKey, _Logging);
                    break;
            }

            client.Model = endpoint.Model;
            client.MaxTokens = endpoint.MaxOutputTokens;
            client.TimeoutMs = (endpoint.TimeoutMs > 0 ? endpoint.TimeoutMs : _Settings.Chat.DefaultTimeoutMs);
            return client;
        }

        private string BuildSystemPrompt(ChatSettings tenantSettings, ChatCompletionRequest request, ChatThread thread)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("You are the LiteGraph assistant.  You help users explore and understand their graph data.");
            sb.AppendLine("Use the available tools to query graphs, nodes, edges, labels, tags, and vectors before answering questions about the data; do not guess.");
            if (thread.GraphGUID != null) sb.AppendLine("This conversation is bound to graph " + thread.GraphGUID + ".  Prefer that graph unless the user asks otherwise.");
            sb.AppendLine("Answer concisely and cite node or edge identifiers when referencing specific graph objects.");

            string tenantPrompt = (!String.IsNullOrEmpty(request.SystemPrompt) ? request.SystemPrompt : tenantSettings.SystemPrompt);
            if (!String.IsNullOrEmpty(tenantPrompt))
            {
                sb.AppendLine();
                sb.AppendLine(tenantPrompt);
            }

            return sb.ToString();
        }

        private async Task AppendHistory(List<ChatMessage> messages, Guid tenantGuid, Guid threadGuid, int maxContextTokens, CancellationToken token)
        {
            List<ChatTurn> turns = new List<ChatTurn>();
            await foreach (ChatTurn turn in _LiteGraph.ChatTurn.ReadByThread(tenantGuid, threadGuid, true, 0, token).ConfigureAwait(false))
            {
                turns.Add(turn);
            }

            if (turns.Count < 1) return;

            // Estimate tokens at four characters per token and keep the newest turns that fit the budget.
            List<ChatTurn> retained = new List<ChatTurn>();
            int budgetChars = maxContextTokens * 4;
            int usedChars = 0;

            for (int i = turns.Count - 1; i >= 0; i--)
            {
                int turnChars = (turns[i].UserMessage?.Length ?? 0) + (turns[i].AssistantResponse?.Length ?? 0);
                if (usedChars + turnChars > budgetChars && retained.Count > 0) break;
                retained.Insert(0, turns[i]);
                usedChars += turnChars;
            }

            foreach (ChatTurn turn in retained)
            {
                if (!String.IsNullOrEmpty(turn.UserMessage)) messages.Add(ChatMessage.User(turn.UserMessage));
                if (!String.IsNullOrEmpty(turn.AssistantResponse)) messages.Add(ChatMessage.Assistant(turn.AssistantResponse));
            }
        }

        private async Task PersistTurn(ChatThread thread, ChatTurn turn, CancellationToken token)
        {
            try
            {
                int maxSequence = await _LiteGraph.ChatTurn.GetMaxSequence(turn.TenantGUID, turn.ThreadGUID, CancellationToken.None).ConfigureAwait(false);
                turn.Sequence = maxSequence + 1;
                await _LiteGraph.ChatTurn.Create(turn, CancellationToken.None).ConfigureAwait(false);
                await _LiteGraph.ChatThread.Update(thread, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to persist chat turn " + turn.GUID + ": " + e.Message);
            }
        }

        private async Task GenerateTitleIfNeeded(ChatThread thread, string userMessage, string assistantMessage, ChatEndpoint completionEndpoint, CancellationToken token)
        {
            if (!String.IsNullOrEmpty(thread.Title)) return;

            try
            {
                ClientCacheEntry entry = GetClient(completionEndpoint);
                ChatResponse response = await entry.Client.ChatAsync(
                    "Produce a title of at most six words for a conversation that began with this message; respond with the title only:\n\n" + Truncate(userMessage, 2000),
                    null,
                    token).ConfigureAwait(false);

                if (response.Success && !String.IsNullOrEmpty(response.Text))
                {
                    thread.Title = Truncate(response.Text.Trim().Trim('"'), 120);
                    await _LiteGraph.ChatThread.Update(thread, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _Logging.Debug(_Header + "title generation failed (ignored): " + e.Message);
            }
        }

        private void RetentionSweep(object state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (TenantMetadata tenant in _LiteGraph.Tenant.ReadMany(EnumerationOrderEnum.CreatedDescending, 0, _TokenSource.Token).ConfigureAwait(false))
                    {
                        ChatSettings settings = await _LiteGraph.ChatSettings.ReadByTenant(tenant.GUID, _TokenSource.Token).ConfigureAwait(false);
                        int retentionDays = (settings != null ? settings.HistoryRetentionDays : new ChatSettings().HistoryRetentionDays);
                        if (retentionDays < 1) continue;
                        await _LiteGraph.ChatTurn.DeleteOlderThan(tenant.GUID, DateTime.UtcNow.AddDays(-retentionDays), _TokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "retention sweep failed: " + e.Message);
                }
            });
        }

        private async Task SendSse(HttpContextBase ctx, object payload)
        {
            await ctx.Response.SendEvent(new ServerSentEvent { Data = _Serializer.SerializeJson(payload, false) }, false).ConfigureAwait(false);
        }

        private async Task SendJsonError(HttpContextBase ctx, int statusCode, ApiErrorEnum error, string description)
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(error, null, description), true)).ConfigureAwait(false);
        }

        private static string Truncate(string val, int maxLength)
        {
            if (String.IsNullOrEmpty(val)) return val;
            if (val.Length <= maxLength) return val;
            return val.Substring(0, maxLength) + "…";
        }

        #endregion

        #region Private-Classes

        private sealed class ClientCacheEntry : IDisposable
        {
            internal readonly CompletionClientBase Client;
            internal readonly SemaphoreSlim Limiter;
            internal readonly DateTime LastUpdateUtc;

            internal ClientCacheEntry(CompletionClientBase client, ChatEndpoint endpoint)
            {
                Client = client;
                Limiter = new SemaphoreSlim(endpoint.MaxConcurrentRequests, endpoint.MaxConcurrentRequests);
                LastUpdateUtc = endpoint.LastUpdateUtc;
            }

            public void Dispose()
            {
                Client.Dispose();
                Limiter.Dispose();
            }
        }

        private sealed class ChatUpstreamException : Exception
        {
            internal readonly int? StatusCode;

            internal ChatUpstreamException(string message, int? statusCode) : base(message)
            {
                StatusCode = statusCode;
            }
        }

        #endregion
    }
}
