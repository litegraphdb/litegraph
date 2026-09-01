namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion choice.
    /// </summary>
    public class OpenAiChatChoice
    {
        #region Public-Members

        /// <summary>
        /// Choice index.  Default is 0.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        /// <summary>
        /// Assistant message.
        /// </summary>
        [JsonPropertyName("message")]
        public OpenAiChatResponseMessage Message { get; set; } = null;

        /// <summary>
        /// Finish reason, for example stop or length.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiChatChoice()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
