namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event carrying the completed turn's usage and timing.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamUsageEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name, always "usage".
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; } = "usage";

        /// <summary>
        /// Completion result with token counts and timings.
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionResult Usage { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamUsageEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
