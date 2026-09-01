namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama-compatible chat message with a role and content.
    /// </summary>
    public class OllamaChatMessage
    {
        #region Public-Members

        /// <summary>
        /// Role, for example system, user, or assistant.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null;

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
        public OllamaChatMessage()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
