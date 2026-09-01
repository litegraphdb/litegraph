namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible response message with a role and plain-text content.
    /// </summary>
    public class OpenAiChatResponseMessage
    {
        #region Public-Members

        /// <summary>
        /// Role.  Always assistant for completions produced by the server.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "assistant";

        /// <summary>
        /// Message content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiChatResponseMessage()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
