namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible non-streaming chat completion response.
    /// </summary>
    public class OpenAiChatCompletionResponse
    {
        #region Public-Members

        /// <summary>
        /// Completion identifier, for example chatcmpl-&lt;turn GUID&gt;.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Object type.  Always chat.completion.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Creation time as a Unix epoch in seconds.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; } = 0;

        /// <summary>
        /// Model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Choices.  A single choice is produced.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAiChatChoice> Choices { get; set; } = new List<OpenAiChatChoice>();

        /// <summary>
        /// Token usage.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAiChatUsage Usage { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiChatCompletionResponse()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
