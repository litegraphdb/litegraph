namespace LiteGraph.Sdk
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
        /// Provider type.  Default is OpenAI.
        /// </summary>
        public ChatProviderTypeEnum Provider { get; set; } = ChatProviderTypeEnum.OpenAI;

        /// <summary>
        /// Endpoint type (Completion or Embedding).  Default is Completion.
        /// </summary>
        public ChatEndpointTypeEnum EndpointType { get; set; } = ChatEndpointTypeEnum.Completion;

        /// <summary>
        /// True when this endpoint is the tenant default for its type.  Default is false.
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

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
