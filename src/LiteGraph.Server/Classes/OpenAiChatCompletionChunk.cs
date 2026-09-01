namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible streaming chat completion chunk.
    /// </summary>
    public class OpenAiChatCompletionChunk
    {
        #region Public-Members

        /// <summary>
        /// Completion identifier, stable across all chunks of one completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Object type.  Always chat.completion.chunk.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion.chunk";

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
        /// Choices.  A single choice is produced; the usage-only terminal chunk carries an empty list.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAiChatChunkChoice> Choices { get; set; } = new List<OpenAiChatChunkChoice>();

        /// <summary>
        /// Token usage, present only on the terminal usage chunk when stream_options.include_usage is set.
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
        public OpenAiChatCompletionChunk()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
