namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion request body.  Unknown fields are ignored during deserialization.
    /// </summary>
    public class OpenAiChatCompletionRequest
    {
        #region Public-Members

        /// <summary>
        /// Model selector.  Matches a tenant chat endpoint by Name, Model, or GUID (case-insensitive).
        /// Null or empty selects the tenant default completion endpoint.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Conversation messages, oldest first.  Roles system, user, and assistant are supported.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; set; } = null;

        /// <summary>
        /// Sampling temperature.  Null uses the endpoint default.  Minimum is 0, maximum is 2.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; } = null;

        /// <summary>
        /// Maximum completion tokens.  Null uses the endpoint default.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; } = null;

        /// <summary>
        /// Maximum completion tokens (newer synonym for max_tokens).  Null uses the endpoint default.
        /// </summary>
        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; } = null;

        /// <summary>
        /// Stream the response as server-sent events.  Default is false.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Streaming options.  Null uses defaults (no usage chunk).
        /// </summary>
        [JsonPropertyName("stream_options")]
        public OpenAiStreamOptions StreamOptions { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiChatCompletionRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
