namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event announcing a tool call requested by the model.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamToolCallEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name, always "tool_call".
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; } = "tool_call";

        /// <summary>
        /// Tool name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Tool arguments as JSON text.
        /// </summary>
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = null;

        /// <summary>
        /// Tool loop iteration, starting at 1.
        /// </summary>
        [JsonPropertyName("iteration")]
        public int Iteration { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamToolCallEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
