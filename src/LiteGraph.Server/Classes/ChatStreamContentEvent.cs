namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event carrying a fragment of the answer (event "delta") or of the model's reasoning (event "thinking").
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamContentEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name: "delta" for answer text, "thinking" for reasoning text.
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; } = "delta";

        /// <summary>
        /// Text fragment.
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
        public ChatStreamContentEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
