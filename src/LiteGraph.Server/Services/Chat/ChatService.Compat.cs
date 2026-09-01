namespace LiteGraph.Server.Services.Chat
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;
    using PolyPrompt.Models;
    using WatsonWebserver.Core;

    /// <summary>
    /// Graph-scoped protocol-compatible chat: OpenAI chat completions and Ollama /api/chat request and
    /// response bodies over LiteGraph URLs.  These routes are stateless at the protocol level (the client
    /// supplies the full message transcript) while each exchange is persisted as a turn in an implicit
    /// per-user, per-graph thread for telemetry and history.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    internal partial class ChatService
    {
        #region Public-Methods

        /// <summary>
        /// Process an OpenAI-format graph-scoped chat completion, writing the HTTP response itself.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="req">Request context.</param>
        /// <param name="request">OpenAI-format request body.</param>
        /// <param name="token">Cancellation token.</param>
        internal async Task ProcessOpenAiGraphCompletion(HttpContextBase ctx, RequestContext req, OpenAiChatCompletionRequest request, CancellationToken token = default)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (request == null) throw new ArgumentNullException(nameof(request));

            ChatCompatContext compat = new ChatCompatContext
            {
                Protocol = ChatCompatProtocolEnum.OpenAI,
                GraphGUID = req.GraphGUID.Value,
                ModelSelector = request.Model,
                Stream = request.Stream,
                IncludeUsage = (request.StreamOptions != null && request.StreamOptions.IncludeUsage)
            };

            if (request.Temperature != null)
            {
                if (request.Temperature.Value < 0 || request.Temperature.Value > 2)
                {
                    await SendCompatError(ctx, compat, 400, "temperature must be between 0 and 2.").ConfigureAwait(false);
                    return;
                }

                compat.Temperature = request.Temperature;
            }

            int? maxTokens = (request.MaxTokens != null ? request.MaxTokens : request.MaxCompletionTokens);
            if (maxTokens != null && maxTokens.Value >= 1) compat.MaxOutputTokens = maxTokens;

            if (request.Messages != null)
            {
                foreach (OpenAiChatMessage message in request.Messages)
                {
                    string content = message.GetContentText();
                    if (String.IsNullOrEmpty(content)) continue;
                    compat.Messages.Add(new KeyValuePair<string, string>((message.Role ?? String.Empty).ToLowerInvariant(), content));
                }
            }

            await ProcessCompatCompletion(ctx, req, compat, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Process an Ollama-format graph-scoped chat completion, writing the HTTP response itself.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="req">Request context.</param>
        /// <param name="request">Ollama-format request body.</param>
        /// <param name="token">Cancellation token.</param>
        internal async Task ProcessOllamaGraphCompletion(HttpContextBase ctx, RequestContext req, OllamaChatRequest request, CancellationToken token = default)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (request == null) throw new ArgumentNullException(nameof(request));

            ChatCompatContext compat = new ChatCompatContext
            {
                Protocol = ChatCompatProtocolEnum.Ollama,
                GraphGUID = req.GraphGUID.Value,
                ModelSelector = request.Model,
                Stream = (request.Stream == null || request.Stream.Value),
                IncludeUsage = true
            };

            if (request.Options != null)
            {
                if (request.Options.Temperature != null)
                    compat.Temperature = Math.Clamp(request.Options.Temperature.Value, 0, 2);
                if (request.Options.NumPredict != null && request.Options.NumPredict.Value >= 1)
                    compat.MaxOutputTokens = request.Options.NumPredict;
            }

            if (request.Messages != null)
            {
                foreach (OllamaChatMessage message in request.Messages)
                {
                    if (String.IsNullOrEmpty(message.Content)) continue;
                    compat.Messages.Add(new KeyValuePair<string, string>((message.Role ?? String.Empty).ToLowerInvariant(), message.Content));
                }
            }

            await ProcessCompatCompletion(ctx, req, compat, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task ProcessCompatCompletion(HttpContextBase ctx, RequestContext req, ChatCompatContext compat, CancellationToken token)
        {
            if (!_Settings.Chat.Enable)
            {
                await SendCompatError(ctx, compat, 503, "Chat is disabled on this server.").ConfigureAwait(false);
                return;
            }

            if (!compat.Messages.Any(m => m.Key == "user"))
            {
                await SendCompatError(ctx, compat, 400, "At least one user message is required.").ConfigureAwait(false);
                return;
            }

            if (!req.Authentication.UserGUID.HasValue)
            {
                await SendCompatError(ctx, compat, 400, "Chat requires a user principal.").ConfigureAwait(false);
                return;
            }

            if (!await _GlobalLimiter.WaitAsync(0, token).ConfigureAwait(false))
            {
                await SendCompatError(ctx, compat, 429, "The server is at its concurrent chat capacity.").ConfigureAwait(false);
                return;
            }

            _Observability?.IncrementChatActive();

            try
            {
                await ProcessCompatInternal(ctx, req, compat, token).ConfigureAwait(false);
            }
            finally
            {
                _Observability?.DecrementChatActive();
                _GlobalLimiter.Release();
            }
        }

        private async Task ProcessCompatInternal(HttpContextBase ctx, RequestContext req, ChatCompatContext compat, CancellationToken token)
        {
            Guid tenantGuid = req.TenantGUID.Value;
            Stopwatch overall = Stopwatch.StartNew();

            ChatSettings tenantSettings = await _LiteGraph.ChatSettings.ReadByTenant(tenantGuid, token).ConfigureAwait(false);
            if (tenantSettings == null) tenantSettings = new ChatSettings { TenantGUID = tenantGuid };

            if (!tenantSettings.EnableChat)
            {
                await SendCompatError(ctx, compat, 403, "Chat is disabled for this tenant.").ConfigureAwait(false);
                return;
            }

            #region Graph

            Graph graph = null;

            try
            {
                graph = await _LiteGraph.Graph.ReadByGuid(tenantGuid, compat.GraphGUID, token: token).ConfigureAwait(false);
            }
            catch (KeyNotFoundException)
            {
            }
            catch (ArgumentException)
            {
            }

            if (graph == null)
            {
                await SendCompatError(ctx, compat, 404, "The specified graph could not be found in this tenant.").ConfigureAwait(false);
                return;
            }

            #endregion

            #region Endpoints

            ChatEndpoint completionEndpoint = await ResolveCompatCompletionEndpoint(tenantGuid, tenantSettings, compat.ModelSelector, token).ConfigureAwait(false);
            if (completionEndpoint == null)
            {
                if (!String.IsNullOrEmpty(compat.ModelSelector))
                {
                    await SendCompatError(ctx, compat, 404, "The model '" + compat.ModelSelector + "' does not exist.  Supply a chat endpoint name, model, or GUID, or omit model to use the tenant default.").ConfigureAwait(false);
                }
                else
                {
                    await SendCompatError(ctx, compat, 400, "No usable completion endpoint is configured for this tenant.").ConfigureAwait(false);
                }

                return;
            }

            ChatEndpoint embeddingEndpoint = null;
            if (tenantSettings.DefaultEmbeddingEndpointGUID != null)
            {
                embeddingEndpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(tenantGuid, tenantSettings.DefaultEmbeddingEndpointGUID.Value, token).ConfigureAwait(false);
            }
            else
            {
                embeddingEndpoint = await FirstActiveEndpoint(tenantGuid, ChatEndpointTypeEnum.Embedding, token).ConfigureAwait(false);
            }
            if (embeddingEndpoint != null && (embeddingEndpoint.EndpointType != ChatEndpointTypeEnum.Embedding || !embeddingEndpoint.Active)) embeddingEndpoint = null;

            #endregion

            #region Thread-and-Turn

            string userMessage = compat.Messages.Last(m => m.Key == "user").Value;

            ChatThread thread = await FindOrCreateCompatThread(tenantGuid, req.Authentication.UserGUID.Value, graph, token).ConfigureAwait(false);

            ChatTurn turn = new ChatTurn
            {
                TenantGUID = tenantGuid,
                ThreadGUID = thread.GUID,
                UserMessage = userMessage,
                CompletionEndpointGUID = completionEndpoint.GUID,
                Provider = completionEndpoint.Provider,
                Model = completionEndpoint.Model,
                TraceId = Activity.Current?.TraceId.ToString(),
                Success = false
            };

            ChatCompletionRequest internalRequest = new ChatCompletionRequest
            {
                GraphGUID = compat.GraphGUID,
                Message = userMessage,
                Stream = false,
                Temperature = compat.Temperature,
                MaxOutputTokens = compat.MaxOutputTokens
            };

            List<object> toolTranscript = new List<object>();
            ChatCompletionResult result = new ChatCompletionResult
            {
                ThreadGUID = thread.GUID,
                TurnGUID = turn.GUID,
                Provider = completionEndpoint.Provider,
                Model = completionEndpoint.Model
            };

            #endregion

            ChatCompatStreamState state = new ChatCompatStreamState
            {
                CompletionId = "chatcmpl-" + turn.GUID.ToString(),
                CreatedEpochSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ModelLabel = completionEndpoint.Model
            };

            Func<string, Task> onDelta = null;
            if (compat.Stream) onDelta = (text) => SendCompatDelta(ctx, compat, state, text, token);

            using (Activity activity = _Observability?.StartActivity("chat.turn", ActivityKind.Internal))
            {
                activity?.SetTag("litegraph.tenant.guid", tenantGuid.ToString());
                activity?.SetTag("litegraph.chat.thread.guid", thread.GUID.ToString());
                activity?.SetTag("litegraph.chat.turn.guid", turn.GUID.ToString());
                activity?.SetTag("litegraph.chat.provider", completionEndpoint.Provider.ToString());
                activity?.SetTag("litegraph.chat.model", completionEndpoint.Model);
                activity?.SetTag("litegraph.chat.streamed", compat.Stream);
                activity?.SetTag("litegraph.chat.protocol", compat.Protocol.ToString());

                try
                {
                    #region Context

                    List<ChatMessage> messages = new List<ChatMessage>();
                    messages.Add(ChatMessage.System(await BuildSystemPrompt(tenantGuid, tenantSettings, internalRequest, thread, token).ConfigureAwait(false)));

                    foreach (KeyValuePair<string, string> message in compat.Messages.Where(m => m.Key == "system"))
                    {
                        messages.Add(ChatMessage.System(message.Value));
                    }

                    int lastUserIndex = compat.Messages.FindLastIndex(m => m.Key == "user");
                    for (int i = 0; i < compat.Messages.Count; i++)
                    {
                        if (i == lastUserIndex) continue;
                        if (compat.Messages[i].Key == "user") messages.Add(ChatMessage.User(compat.Messages[i].Value));
                        else if (compat.Messages[i].Key == "assistant") messages.Add(ChatMessage.Assistant(compat.Messages[i].Value));
                    }

                    #endregion

                    #region Retrieval

                    if (tenantSettings.EnableRag && embeddingEndpoint != null)
                    {
                        await RunRetrieval(ctx, req, internalRequest, tenantSettings, embeddingEndpoint, compat.GraphGUID, messages, turn, false, token).ConfigureAwait(false);
                    }

                    messages.Add(ChatMessage.User(userMessage));

                    #endregion

                    #region Tool-Loop

                    List<ToolDefinition> tools = new List<ToolDefinition>();

                    if (tenantSettings.EnableTools)
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
                        await RunToolLoop(ctx, req, internalRequest, tenantSettings, completionEndpoint, embeddingEndpoint, entry, tools, messages, maxIterations, turn, result, toolTranscript, false, token, onDelta).ConfigureAwait(false);
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
                    await SendCompatFailure(ctx, compat, state, 502, cue.Message, token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    turn.Error = e.Message;
                    activity?.SetTag("litegraph.chat.error", e.Message);
                    _Logging.Warn(_Header + "compat chat turn " + turn.GUID + " failed: " + e.Message);
                    await SendCompatFailure(ctx, compat, state, 500, e.Message, token).ConfigureAwait(false);
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
                        compat.Stream,
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
                    await SendCompatSuccess(ctx, compat, state, turn, result, token).ConfigureAwait(false);
                }
            }
        }

        private async Task<ChatEndpoint> ResolveCompatCompletionEndpoint(Guid tenantGuid, ChatSettings tenantSettings, string modelSelector, CancellationToken token)
        {
            if (!String.IsNullOrEmpty(modelSelector))
            {
                await foreach (ChatEndpoint candidate in _LiteGraph.ChatEndpoint.ReadAllInTenant(tenantGuid, ChatEndpointTypeEnum.Completion, EnumerationOrderEnum.CreatedAscending, 0, token).ConfigureAwait(false))
                {
                    if (!candidate.Active) continue;
                    if (String.Equals(candidate.Name, modelSelector, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(candidate.Model, modelSelector, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(candidate.GUID.ToString(), modelSelector, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }

                return null;
            }

            ChatEndpoint endpoint = null;

            if (tenantSettings.DefaultCompletionEndpointGUID != null)
            {
                endpoint = await _LiteGraph.ChatEndpoint.ReadByGuid(tenantGuid, tenantSettings.DefaultCompletionEndpointGUID.Value, token).ConfigureAwait(false);
            }
            else
            {
                endpoint = await FirstActiveEndpoint(tenantGuid, ChatEndpointTypeEnum.Completion, token).ConfigureAwait(false);
            }

            if (endpoint == null || endpoint.EndpointType != ChatEndpointTypeEnum.Completion || !endpoint.Active) return null;
            return endpoint;
        }

        private async Task<ChatThread> FindOrCreateCompatThread(Guid tenantGuid, Guid userGuid, Graph graph, CancellationToken token)
        {
            string title = "OpenAI-compatible: " + (!String.IsNullOrEmpty(graph.Name) ? graph.Name : graph.GUID.ToString());

            await foreach (ChatThread candidate in _LiteGraph.ChatThread.ReadAllInTenant(tenantGuid, userGuid, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false))
            {
                if (candidate.GraphGUID != null
                    && candidate.GraphGUID.Value.Equals(graph.GUID)
                    && String.Equals(candidate.Title, title, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return await _LiteGraph.ChatThread.Create(new ChatThread
            {
                TenantGUID = tenantGuid,
                UserGUID = userGuid,
                GraphGUID = graph.GUID,
                Title = title
            }, token).ConfigureAwait(false);
        }

        private async Task SendCompatDelta(HttpContextBase ctx, ChatCompatContext compat, ChatCompatStreamState state, string text, CancellationToken token)
        {
            if (compat.Protocol == ChatCompatProtocolEnum.OpenAI)
            {
                await EnsureOpenAiStreamStarted(ctx, state).ConfigureAwait(false);
                await SendOpenAiChunk(ctx, state, new OpenAiChatDelta { Content = text }, null, null, false).ConfigureAwait(false);
            }
            else
            {
                await EnsureOllamaStreamStarted(ctx, state).ConfigureAwait(false);
                OllamaChatResponse fragment = new OllamaChatResponse
                {
                    Model = state.ModelLabel,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Message = new OllamaChatMessage { Role = "assistant", Content = text },
                    Done = false
                };
                await SendOllamaLine(ctx, fragment, token).ConfigureAwait(false);
            }
        }

        private async Task SendCompatSuccess(HttpContextBase ctx, ChatCompatContext compat, ChatCompatStreamState state, ChatTurn turn, ChatCompletionResult result, CancellationToken token)
        {
            string finishReason = MapCompatFinishReason(result.FinishReason);
            int promptTokens = (turn.PromptTokens != null ? turn.PromptTokens.Value : 0);
            int completionTokens = (turn.CompletionTokens != null ? turn.CompletionTokens.Value : 0);

            if (compat.Protocol == ChatCompatProtocolEnum.OpenAI)
            {
                if (compat.Stream)
                {
                    await EnsureOpenAiStreamStarted(ctx, state).ConfigureAwait(false);
                    await SendOpenAiChunk(ctx, state, new OpenAiChatDelta(), finishReason, null, false).ConfigureAwait(false);

                    if (compat.IncludeUsage)
                    {
                        OpenAiChatUsage usage = new OpenAiChatUsage
                        {
                            PromptTokens = promptTokens,
                            CompletionTokens = completionTokens,
                            TotalTokens = promptTokens + completionTokens
                        };
                        await SendOpenAiUsageChunk(ctx, state, usage).ConfigureAwait(false);
                    }

                    await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
                }
                else
                {
                    OpenAiChatCompletionResponse response = new OpenAiChatCompletionResponse
                    {
                        Id = state.CompletionId,
                        Created = state.CreatedEpochSeconds,
                        Model = state.ModelLabel,
                        Choices = new List<OpenAiChatChoice>
                        {
                            new OpenAiChatChoice
                            {
                                Index = 0,
                                Message = new OpenAiChatResponseMessage { Role = "assistant", Content = (result.Message ?? String.Empty) },
                                FinishReason = finishReason
                            }
                        },
                        Usage = new OpenAiChatUsage
                        {
                            PromptTokens = promptTokens,
                            CompletionTokens = completionTokens,
                            TotalTokens = promptTokens + completionTokens
                        }
                    };

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(response, true)).ConfigureAwait(false);
                }
            }
            else
            {
                OllamaChatResponse response = new OllamaChatResponse
                {
                    Model = state.ModelLabel,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Message = new OllamaChatMessage { Role = "assistant", Content = (compat.Stream ? String.Empty : (result.Message ?? String.Empty)) },
                    Done = true,
                    DoneReason = finishReason,
                    TotalDuration = MillisecondsToNanoseconds(turn.TotalDurationMs),
                    EvalDuration = MillisecondsToNanoseconds(ComputeEvalMilliseconds(turn)),
                    PromptEvalCount = promptTokens,
                    EvalCount = completionTokens
                };

                if (compat.Stream)
                {
                    await EnsureOllamaStreamStarted(ctx, state).ConfigureAwait(false);
                    await SendOllamaLine(ctx, response, token).ConfigureAwait(false);
                    await ctx.Response.SendChunk(Array.Empty<byte>(), true, token).ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(response, true)).ConfigureAwait(false);
                }
            }
        }

        private async Task SendCompatFailure(HttpContextBase ctx, ChatCompatContext compat, ChatCompatStreamState state, int statusCode, string message, CancellationToken token)
        {
            if (!state.Started)
            {
                await SendCompatError(ctx, compat, statusCode, message).ConfigureAwait(false);
                return;
            }

            if (compat.Protocol == ChatCompatProtocolEnum.OpenAI)
            {
                await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
            }
            else
            {
                byte[] line = Encoding.UTF8.GetBytes(_Serializer.SerializeJson(new OpenAiErrorResponse(message, CompatErrorType(statusCode)), false) + "\n");
                await ctx.Response.SendChunk(line, true, token).ConfigureAwait(false);
            }
        }

        private async Task SendCompatError(HttpContextBase ctx, ChatCompatContext compat, int statusCode, string message)
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = Constants.JsonContentType;
            await ctx.Response.Send(_Serializer.SerializeJson(new OpenAiErrorResponse(message, CompatErrorType(statusCode)), true)).ConfigureAwait(false);
        }

        private async Task EnsureOpenAiStreamStarted(HttpContextBase ctx, ChatCompatStreamState state)
        {
            if (state.Started) return;
            state.Started = true;
            ctx.Response.StatusCode = 200;
            ctx.Response.ServerSentEvents = true;
            await SendOpenAiChunk(ctx, state, new OpenAiChatDelta { Role = "assistant" }, null, null, false).ConfigureAwait(false);
        }

        private Task EnsureOllamaStreamStarted(HttpContextBase ctx, ChatCompatStreamState state)
        {
            if (state.Started) return Task.CompletedTask;
            state.Started = true;
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.NdjsonContentType;
            ctx.Response.ChunkedTransfer = true;
            return Task.CompletedTask;
        }

        private async Task SendOpenAiChunk(HttpContextBase ctx, ChatCompatStreamState state, OpenAiChatDelta delta, string finishReason, OpenAiChatUsage usage, bool final)
        {
            OpenAiChatCompletionChunk chunk = new OpenAiChatCompletionChunk
            {
                Id = state.CompletionId,
                Created = state.CreatedEpochSeconds,
                Model = state.ModelLabel,
                Choices = new List<OpenAiChatChunkChoice>
                {
                    new OpenAiChatChunkChoice
                    {
                        Index = 0,
                        Delta = delta,
                        FinishReason = finishReason
                    }
                },
                Usage = usage
            };

            await ctx.Response.SendEvent(new ServerSentEvent { Data = _Serializer.SerializeJson(chunk, false) }, final).ConfigureAwait(false);
        }

        private async Task SendOpenAiUsageChunk(HttpContextBase ctx, ChatCompatStreamState state, OpenAiChatUsage usage)
        {
            OpenAiChatCompletionChunk chunk = new OpenAiChatCompletionChunk
            {
                Id = state.CompletionId,
                Created = state.CreatedEpochSeconds,
                Model = state.ModelLabel,
                Choices = new List<OpenAiChatChunkChoice>(),
                Usage = usage
            };

            await ctx.Response.SendEvent(new ServerSentEvent { Data = _Serializer.SerializeJson(chunk, false) }, false).ConfigureAwait(false);
        }

        private async Task SendOllamaLine(HttpContextBase ctx, OllamaChatResponse obj, CancellationToken token)
        {
            byte[] line = Encoding.UTF8.GetBytes(_Serializer.SerializeJson(obj, false) + "\n");
            await ctx.Response.SendChunk(line, false, token).ConfigureAwait(false);
        }

        private static string CompatErrorType(int statusCode)
        {
            if (statusCode == 429) return "rate_limit_error";
            if (statusCode >= 500) return "server_error";
            return "invalid_request_error";
        }

        private static string MapCompatFinishReason(string finishReason)
        {
            if (String.IsNullOrEmpty(finishReason)) return "stop";
            string lowered = finishReason.ToLowerInvariant();
            if (lowered.Contains("length") || lowered.Contains("max")) return "length";
            return "stop";
        }

        private static double ComputeEvalMilliseconds(ChatTurn turn)
        {
            if (turn.TimeToFirstTokenMs != null
                && turn.TimeToLastTokenMs != null
                && turn.TimeToLastTokenMs.Value >= turn.TimeToFirstTokenMs.Value)
            {
                return turn.TimeToLastTokenMs.Value - turn.TimeToFirstTokenMs.Value;
            }

            return turn.TotalDurationMs;
        }

        private static long MillisecondsToNanoseconds(double milliseconds)
        {
            if (milliseconds < 0) return 0;
            return (long)(milliseconds * 1000000.0);
        }

        #endregion

        #region Private-Classes

        private sealed class ChatCompatContext
        {
            internal ChatCompatProtocolEnum Protocol = ChatCompatProtocolEnum.OpenAI;
            internal Guid GraphGUID = Guid.Empty;
            internal string ModelSelector = null;
            internal List<KeyValuePair<string, string>> Messages = new List<KeyValuePair<string, string>>();
            internal bool Stream = false;
            internal bool IncludeUsage = false;
            internal double? Temperature = null;
            internal int? MaxOutputTokens = null;
        }

        private sealed class ChatCompatStreamState
        {
            internal bool Started = false;
            internal string CompletionId = null;
            internal long CreatedEpochSeconds = 0;
            internal string ModelLabel = null;
        }

        #endregion
    }
}
