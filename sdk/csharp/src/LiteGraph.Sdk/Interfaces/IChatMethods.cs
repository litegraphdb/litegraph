namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for chat methods.
    /// </summary>
    public interface IChatMethods
    {
        /// <summary>
        /// Create a chat endpoint.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat endpoint with the API key redacted.</returns>
        Task<ChatEndpoint> CreateEndpoint(ChatEndpoint endpoint, CancellationToken token = default);

        /// <summary>
        /// Read chat endpoints, optionally filtered by endpoint type.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointType">Endpoint type filter.  Null returns endpoints of every type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat endpoints with API keys redacted.</returns>
        Task<List<ChatEndpoint>> ReadEndpoints(Guid tenantGuid, ChatEndpointTypeEnum? endpointType = null, CancellationToken token = default);

        /// <summary>
        /// Read a chat endpoint by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Chat endpoint GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat endpoint with the API key redacted.</returns>
        Task<ChatEndpoint> ReadEndpoint(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a chat endpoint exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Chat endpoint GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> EndpointExists(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default);

        /// <summary>
        /// Update a chat endpoint.  Sending back a redacted API key value preserves the stored key.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated chat endpoint with the API key redacted.</returns>
        Task<ChatEndpoint> UpdateEndpoint(ChatEndpoint endpoint, CancellationToken token = default);

        /// <summary>
        /// Delete a chat endpoint.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Chat endpoint GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteEndpoint(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default);

        /// <summary>
        /// Test connectivity to a chat endpoint's upstream provider.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Chat endpoint GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Connectivity test result.</returns>
        Task<ChatEndpointTestResult> TestEndpoint(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default);

        /// <summary>
        /// Read health status for one chat endpoint.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointGuid">Chat endpoint GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Endpoint health.</returns>
        Task<ChatEndpointHealth> ReadEndpointHealth(Guid tenantGuid, Guid endpointGuid, CancellationToken token = default);

        /// <summary>
        /// Read health status for every chat endpoint in the tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Endpoint health list.</returns>
        Task<List<ChatEndpointHealth>> ReadAllEndpointHealth(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Create a chat thread.  The caller becomes the thread owner.
        /// </summary>
        /// <param name="thread">Chat thread.  GraphGUID and Title are optional.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat thread.</returns>
        Task<ChatThread> CreateThread(ChatThread thread, CancellationToken token = default);

        /// <summary>
        /// Read chat threads.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="allUsers">True to read every user's threads (admin only).  Default is false, which reads the caller's own threads.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat threads.</returns>
        Task<List<ChatThread>> ReadThreads(Guid tenantGuid, bool allUsers = false, CancellationToken token = default);

        /// <summary>
        /// Read a chat thread by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Chat thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat thread.</returns>
        Task<ChatThread> ReadThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Delete a chat thread along with its turns and feedback.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Chat thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Read the turns of a chat thread, ascending by sequence.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Chat thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat turns.</returns>
        Task<List<ChatTurn>> ReadThreadTurns(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Execute a non-streaming chat completion.  The request's Stream property is forced to false.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="request">Completion request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Completion result.</returns>
        Task<ChatCompletionResult> Completion(Guid tenantGuid, ChatCompletionRequest request, CancellationToken token = default);

        /// <summary>
        /// Execute a streaming chat completion.  The request's Stream property is forced to true.
        /// Yields one event per server-sent event frame; enumeration ends at the [DONE] sentinel.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="request">Completion request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of stream events.</returns>
        IAsyncEnumerable<ChatStreamEvent> CompletionStreaming(Guid tenantGuid, ChatCompletionRequest request, CancellationToken token = default);

        /// <summary>
        /// Submit feedback on a chat turn.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="turnGuid">Chat turn GUID.</param>
        /// <param name="rating">Rating.</param>
        /// <param name="feedbackText">Free-text feedback.  Null when no comment is supplied.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat feedback.</returns>
        Task<ChatFeedback> SubmitFeedback(Guid tenantGuid, Guid turnGuid, ChatFeedbackRatingEnum rating, string feedbackText = null, CancellationToken token = default);

        /// <summary>
        /// Read all chat feedback in the tenant (admin only).
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat feedback list.</returns>
        Task<List<ChatFeedback>> ReadFeedback(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Read a chat feedback record by GUID (admin only).
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="feedbackGuid">Chat feedback GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat feedback.</returns>
        Task<ChatFeedback> ReadFeedback(Guid tenantGuid, Guid feedbackGuid, CancellationToken token = default);

        /// <summary>
        /// Delete a chat feedback record (admin only).
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="feedbackGuid">Chat feedback GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteFeedback(Guid tenantGuid, Guid feedbackGuid, CancellationToken token = default);

        /// <summary>
        /// Read the tenant's chat settings.  Defaults are returned when no record exists.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat settings.</returns>
        Task<ChatSettings> ReadChatSettings(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Upsert the tenant's chat settings (admin only).
        /// </summary>
        /// <param name="settings">Chat settings.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat settings.</returns>
        Task<ChatSettings> UpdateChatSettings(ChatSettings settings, CancellationToken token = default);
    }
}
