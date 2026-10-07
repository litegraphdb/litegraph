namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event reporting the outcome of a tool call.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamToolResultEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name, always "tool_result".
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; } = "tool_result";

        /// <summary>
        /// Tool name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// True when the tool succeeded.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message, or null when the tool succeeded (then omitted).
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; } = null;

        /// <summary>
        /// Tool runtime in milliseconds.
        /// </summary>
        [JsonPropertyName("runtimeMs")]
        public double RuntimeMs { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamToolResultEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
