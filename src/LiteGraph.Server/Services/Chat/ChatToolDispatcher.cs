namespace LiteGraph.Server.Services.Chat
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using SyslogLogging;

    /// <summary>
    /// Executes model tool calls in-process against the agnostic service handler,
    /// always under the calling principal's tenant and authorization context.
    /// The MCP server's elevated authority is never inherited here: the tenant scope
    /// supplied by the caller overrides anything the model places in tool arguments.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    internal class ChatToolDispatcher
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Header = "[ChatToolDispatcher] ";
        private readonly Dictionary<string, ChatToolDefinition> _Tools;
        private readonly AuthorizationService _Authorization;
        private readonly ObservabilityService _Observability;
        private readonly LoggingModule _Logging;
        private readonly Serializer _Serializer = new Serializer();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="tools">Tool catalog.</param>
        /// <param name="authorization">Authorization service.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="observability">Observability service.</param>
        internal ChatToolDispatcher(
            List<ChatToolDefinition> tools,
            AuthorizationService authorization,
            LoggingModule logging,
            ObservabilityService observability)
        {
            if (tools == null) throw new ArgumentNullException(nameof(tools));
            _Authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Observability = observability;
            _Tools = tools.ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get the tools to advertise to the model.
        /// </summary>
        /// <param name="includeMutations">Whether the tenant has opted into mutation tools.</param>
        /// <returns>Tool definitions.</returns>
        internal List<ChatToolDefinition> GetAdvertisedTools(bool includeMutations)
        {
            return _Tools.Values
                .Where(t => includeMutations || !t.Mutation)
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Execute a tool call under the caller's authority.
        /// Failures surface as tool-level errors the model can read, never as exceptions.
        /// </summary>
        /// <param name="toolName">Tool name from the model's tool call.</param>
        /// <param name="argumentsJson">The model's arguments as a JSON string.</param>
        /// <param name="authentication">The calling principal's authentication context.</param>
        /// <param name="tenantGuid">The caller's tenant; forced regardless of tool arguments.</param>
        /// <param name="allowMutations">Whether mutation tools may execute.</param>
        /// <param name="embedText">Delegate that embeds text for vector search, or null when no embedding endpoint is available.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Execution result.</returns>
        internal async Task<ChatToolExecutionResult> Execute(
            string toolName,
            string argumentsJson,
            AuthenticationContext authentication,
            Guid tenantGuid,
            bool allowMutations,
            Func<string, CancellationToken, Task<List<float>>> embedText,
            CancellationToken token = default)
        {
            if (authentication == null) throw new ArgumentNullException(nameof(authentication));

            Stopwatch sw = Stopwatch.StartNew();
            ChatToolExecutionResult result = new ChatToolExecutionResult();

            try
            {
                if (String.IsNullOrEmpty(toolName) || !_Tools.TryGetValue(toolName, out ChatToolDefinition tool))
                {
                    result.Error = "Unknown tool '" + toolName + "'.";
                    return result;
                }

                if (tool.Mutation && !allowMutations)
                {
                    result.Error = "Tool '" + toolName + "' is not permitted for this conversation.";
                    return result;
                }

                JsonElement? args = null;
                if (!String.IsNullOrEmpty(argumentsJson))
                {
                    using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
                    {
                        args = doc.RootElement.Clone();
                    }
                }

                RequestContext req = new RequestContext();
                req.RequestType = tool.RequestType;
                req.Authentication = authentication;

                try
                {
                    tool.Bind?.Invoke(args, req);
                }
                catch (Exception e) when (e is FormatException || e is ArgumentException || e is JsonException)
                {
                    result.Error = "Invalid arguments for tool '" + toolName + "': " + e.Message;
                    return result;
                }

                req.TenantGUID = tenantGuid;
                if (req.SearchRequest != null)
                {
                    req.SearchRequest.TenantGUID = tenantGuid;
                    if (req.GraphGUID != null) req.SearchRequest.GraphGUID = req.GraphGUID.Value;
                }
                if (req.VectorSearchRequest != null) req.VectorSearchRequest.TenantGUID = tenantGuid;

                if (tool.RequiresEmbedding)
                {
                    if (embedText == null)
                    {
                        result.Error = "Vector search is unavailable: the tenant has no embedding endpoint configured.";
                        return result;
                    }

                    string text = GetTextArgument(args);
                    if (String.IsNullOrEmpty(text))
                    {
                        result.Error = "Vector search requires a non-empty 'text' argument.";
                        return result;
                    }

                    List<float> embeddings = await embedText(text, token).ConfigureAwait(false);
                    if (embeddings == null || embeddings.Count < 1)
                    {
                        result.Error = "The embedding endpoint returned no embedding for the supplied text.";
                        return result;
                    }

                    req.VectorSearchRequest.Embeddings = embeddings;
                }

                await _Authorization.Authorize(req, token).ConfigureAwait(false);
                if (req.Authorization == null || req.Authorization.Result != AuthorizationResultEnum.Permitted)
                {
                    result.Error = "Access denied for tool '" + toolName + "'.";
                    return result;
                }

                ResponseContext resp = await tool.Handler(req, token).ConfigureAwait(false);

                if (resp == null)
                {
                    result.Error = "Tool '" + toolName + "' produced no response.";
                    return result;
                }

                if (!resp.Success)
                {
                    result.Error = (resp.Error != null ? resp.Error.Message : "Tool '" + toolName + "' failed.");
                    return result;
                }

                result.Success = true;
                result.ResultJson = (resp.Data != null ? _Serializer.SerializeJson(resp.Data, false) : "{}");
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "tool '" + toolName + "' failed: " + e.Message);
                result.Error = "Tool '" + toolName + "' failed: " + e.Message;
                return result;
            }
            finally
            {
                sw.Stop();
                result.DurationMs = sw.Elapsed.TotalMilliseconds;
                _Observability?.RecordChatToolCall(toolName ?? "unknown", result.Success, result.DurationMs);
            }
        }

        #endregion

        #region Private-Methods

        private static string GetTextArgument(JsonElement? args)
        {
            if (args == null) return null;
            if (!args.Value.TryGetProperty("text", out JsonElement prop)) return null;
            if (prop.ValueKind != JsonValueKind.String) return null;
            return prop.GetString();
        }

        #endregion
    }
}
