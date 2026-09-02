namespace LiteGraph.Server.Classes
{
    using System;
    using LiteGraph;

    /// <summary>
    /// Chat endpoint model preload result.  Preloading warms the configured model on the upstream
    /// inference endpoint so the first completion does not pay the model load cost.
    /// </summary>
    public class ChatEndpointPreloadResult
    {
        #region Public-Members

        /// <summary>
        /// Chat endpoint GUID.
        /// </summary>
        public Guid EndpointGUID { get; set; } = Guid.Empty;

        /// <summary>
        /// Model configured on the endpoint.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Provider type of the endpoint.
        /// </summary>
        public ChatProviderTypeEnum Provider { get; set; } = ChatProviderTypeEnum.OpenAI;

        /// <summary>
        /// Whether the provider supports model preloading.  False for cloud providers, whose models are always resident.
        /// </summary>
        public bool Supported { get; set; } = false;

        /// <summary>
        /// Whether a background warm-up was started by this request.
        /// </summary>
        public bool Started { get; set; } = false;

        /// <summary>
        /// Whether a warm-up for this endpoint was already in flight.
        /// </summary>
        public bool AlreadyInProgress { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatEndpointPreloadResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
