namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Chat methods.
    /// </summary>
    public class ChatMethods : IChatMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public ChatMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatEndpoint> CreateEndpoint(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + endpoint.TenantGUID + "/chat/endpoints";
            return await _Sdk.PutCreate<ChatEndpoint>(url, endpoint, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatEndpoint>> ReadEndpoints(Guid tenantGuid, ChatEndpointTypeEnum? endpointType = null, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints";
            if (endpointType != null) url += "?endpointType=" + endpointType.Value.ToString();
            return await _Sdk.GetMany<ChatEndpoint>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatEndpoint> ReadEndpoint(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints/" + endpointGuid;
            return await _Sdk.Get<ChatEndpoint>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> EndpointExists(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints/" + endpointGuid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatEndpoint> UpdateEndpoint(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + endpoint.TenantGUID + "/chat/endpoints/" + endpoint.GUID;
            return await _Sdk.PutUpdate<ChatEndpoint>(url, endpoint, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEndpoint(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints/" + endpointGuid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatEndpointTestResult> TestEndpoint(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints/" + endpointGuid + "/test";
            return await _Sdk.Post<object, ChatEndpointTestResult>(url, new { }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatEndpointHealth> ReadEndpointHealth(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints/" + endpointGuid + "/health";
            return await _Sdk.Get<ChatEndpointHealth>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatEndpointHealth>> ReadAllEndpointHealth(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/endpoints/health";
            return await _Sdk.GetMany<ChatEndpointHealth>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatModelSummary>> ReadModels(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/models";
            return await _Sdk.GetMany<ChatModelSummary>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatThread> CreateThread(ChatThread thread, CancellationToken token = default)
        {
            if (thread == null) throw new ArgumentNullException(nameof(thread));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + thread.TenantGUID + "/chat/threads";
            return await _Sdk.PutCreate<ChatThread>(url, thread, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatThread>> ReadThreads(Guid tenantGuid, bool allUsers = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/threads";
            if (allUsers) url += "?all";
            return await _Sdk.GetMany<ChatThread>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatThread> ReadThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/threads/" + threadGuid;
            return await _Sdk.Get<ChatThread>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatThread> UpdateThread(Guid tenantGuid, Guid threadGuid, ChatThread thread, CancellationToken token = default)
        {
            if (thread == null) throw new ArgumentNullException(nameof(thread));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/threads/" + threadGuid;
            return await _Sdk.PutUpdate<ChatThread>(url, thread, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/threads/" + threadGuid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatTurn>> ReadThreadTurns(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/threads/" + threadGuid + "/turns";
            return await _Sdk.GetMany<ChatTurn>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatCompletionResult> Completion(Guid tenantGuid, ChatCompletionRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Stream = false;
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/completions";
            return await _Sdk.Post<ChatCompletionRequest, ChatCompletionResult>(url, request, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatStreamEvent> CompletionStreaming(
            Guid tenantGuid,
            ChatCompletionRequest request,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Stream = true;
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/completions";

            string json;
            if (!Serializer.TrySerializeJson(request, false, out json))
                throw new ArgumentException("Supplied request is not serializable to JSON.");

            byte[] body = Encoding.UTF8.GetBytes(json);

            await foreach (string payload in _Sdk.PostServerSentEvents(url, body, "application/json", token).ConfigureAwait(false))
            {
                ChatStreamEvent ev = ParseStreamEvent(payload);
                if (ev != null) yield return ev;
            }
        }

        /// <inheritdoc />
        public async Task<ChatFeedback> SubmitFeedback(Guid tenantGuid, Guid turnGuid, ChatFeedbackRatingEnum rating, string feedbackText = null, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/turns/" + turnGuid + "/feedback";
            return await _Sdk.Post<object, ChatFeedback>(url, new { Rating = rating.ToString(), FeedbackText = feedbackText }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatFeedback>> ReadFeedback(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/feedback";
            return await _Sdk.GetMany<ChatFeedback>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatFeedback> ReadFeedback(Guid tenantGuid, Guid feedbackGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/feedback/" + feedbackGuid;
            return await _Sdk.Get<ChatFeedback>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteFeedback(Guid tenantGuid, Guid feedbackGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/feedback/" + feedbackGuid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatSettings> ReadChatSettings(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/chat/settings";
            return await _Sdk.Get<ChatSettings>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatSettings> UpdateChatSettings(ChatSettings settings, CancellationToken token = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + settings.TenantGUID + "/chat/settings";
            return await _Sdk.PutUpdate<ChatSettings>(url, settings, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static ChatStreamEvent ParseStreamEvent(string payload)
        {
            if (String.IsNullOrEmpty(payload)) return null;

            using (JsonDocument doc = JsonDocument.Parse(payload))
            {
                JsonElement root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                ChatStreamEvent ev = new ChatStreamEvent();
                ev.Event = GetString(root, "event");
                ev.Content = GetString(root, "content");
                ev.ThreadGUID = GetGuid(root, "threadGuid");
                ev.TurnGUID = GetGuid(root, "turnGuid");
                ev.Name = GetString(root, "name");
                ev.Arguments = GetString(root, "arguments");
                ev.Error = GetString(root, "error");
                ev.Message = GetString(root, "message");

                JsonElement prop;

                if (root.TryGetProperty("success", out prop) && (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
                    ev.Success = prop.GetBoolean();

                if (root.TryGetProperty("runtimeMs", out prop) && prop.ValueKind == JsonValueKind.Number)
                    ev.RuntimeMs = prop.GetDouble();

                if (root.TryGetProperty("iteration", out prop) && prop.ValueKind == JsonValueKind.Number)
                    ev.Iteration = prop.GetInt32();

                if (root.TryGetProperty("statusCode", out prop) && prop.ValueKind == JsonValueKind.Number)
                    ev.StatusCode = prop.GetInt32();

                if (root.TryGetProperty("chunks", out prop) && prop.ValueKind != JsonValueKind.Null)
                    ev.Chunks = prop.GetRawText();

                if (root.TryGetProperty("usage", out prop) && prop.ValueKind == JsonValueKind.Object)
                    ev.Usage = Serializer.DeserializeJson<ChatCompletionResult>(prop.GetRawText());

                return ev;
            }
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            JsonElement prop;
            if (element.TryGetProperty(propertyName, out prop) && prop.ValueKind == JsonValueKind.String) return prop.GetString();
            return null;
        }

        private static Guid? GetGuid(JsonElement element, string propertyName)
        {
            JsonElement prop;
            if (element.TryGetProperty(propertyName, out prop) && prop.ValueKind == JsonValueKind.String)
            {
                Guid guid;
                if (Guid.TryParse(prop.GetString(), out guid)) return guid;
            }
            return null;
        }

        #endregion
    }
}
