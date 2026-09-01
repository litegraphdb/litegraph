namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible token usage block.
    /// </summary>
    public class OpenAiChatUsage
    {
        #region Public-Members

        /// <summary>
        /// Prompt tokens.  Default is 0.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; } = 0;

        /// <summary>
        /// Completion tokens.  Default is 0.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; } = 0;

        /// <summary>
        /// Total tokens.  Default is 0.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiChatUsage()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
