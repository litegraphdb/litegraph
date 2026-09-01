namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible streaming chunk choice.
    /// </summary>
    public class OpenAiChatChunkChoice
    {
        #region Public-Members

        /// <summary>
        /// Choice index.  Default is 0.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        /// <summary>
        /// Delta payload for this chunk.
        /// </summary>
        [JsonPropertyName("delta")]
        public OpenAiChatDelta Delta { get; set; } = new OpenAiChatDelta();

        /// <summary>
        /// Finish reason, for example stop or length.  Null on non-terminal chunks.
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
        public OpenAiChatChunkChoice()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
