namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Node retrieved as graph context for a chat prompt, as listed in the retrieval stream event.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatRetrievalChunk
    {
        #region Public-Members

        /// <summary>
        /// Node GUID, or null when the search result has no node (then omitted).
        /// </summary>
        [JsonPropertyName("nodeGuid")]
        public Guid? NodeGuid { get; set; } = null;

        /// <summary>
        /// Node name; null is omitted.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Similarity score; null is omitted.
        /// </summary>
        [JsonPropertyName("score")]
        public float? Score { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatRetrievalChunk()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
