namespace LiteGraph.Server.Classes
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event listing the graph context retrieved for the prompt.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamRetrievalEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name, always "retrieval".
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; } = "retrieval";

        /// <summary>
        /// Retrieved nodes, most similar first.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<ChatRetrievalChunk> Chunks { get; set; } = new List<ChatRetrievalChunk>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamRetrievalEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
