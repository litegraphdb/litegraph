namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible streaming delta payload.
    /// </summary>
    public class OpenAiChatDelta
    {
        #region Public-Members

        /// <summary>
        /// Role, present only on the first chunk of a completion.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null;

        /// <summary>
        /// Content fragment, present on content-bearing chunks.
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
        public OpenAiChatDelta()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
