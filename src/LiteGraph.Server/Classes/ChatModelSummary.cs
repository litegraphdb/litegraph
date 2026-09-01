namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Non-privileged projection of a chat endpoint, exposing only what a chat
    /// user needs to pick a model: identity, display name, model, provider,
    /// type, and whether it is the tenant default.  Endpoint URLs, keys, and
    /// health configuration are never included.
    /// </summary>
    public class ChatModelSummary
    {
        #region Public-Members

        /// <summary>
        /// Endpoint GUID, supplied as CompletionEndpointGUID or EmbeddingEndpointGUID on completion requests.
        /// </summary>
        public Guid GUID { get; set; } = Guid.Empty;

        /// <summary>
        /// Human-readable endpoint name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Model identifier used by the provider.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Provider type.
        /// </summary>
        public ChatProviderTypeEnum Provider { get; set; } = ChatProviderTypeEnum.OpenAI;

        /// <summary>
        /// Endpoint type (Completion or Embedding).
        /// </summary>
        public ChatEndpointTypeEnum EndpointType { get; set; } = ChatEndpointTypeEnum.Completion;

        /// <summary>
        /// True when this endpoint is the tenant default for its type.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatModelSummary()
        {
        }

        /// <summary>
        /// Project a chat endpoint into a model summary.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.  Must not be null.</param>
        /// <param name="isDefault">True when the endpoint is the tenant default for its type.</param>
        /// <returns>Model summary.</returns>
        /// <exception cref="ArgumentNullException">Thrown when endpoint is null.</exception>
        public static ChatModelSummary FromEndpoint(ChatEndpoint endpoint, bool isDefault)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            return new ChatModelSummary
            {
                GUID = endpoint.GUID,
                Name = endpoint.Name,
                Model = endpoint.Model,
                Provider = endpoint.Provider,
                EndpointType = endpoint.EndpointType,
                IsDefault = isDefault
            };
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
