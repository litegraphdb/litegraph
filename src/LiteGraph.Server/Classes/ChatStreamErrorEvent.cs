namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event reporting a failed completion.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamErrorEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name, always "error".
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; } = "error";

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = null;

        /// <summary>
        /// HTTP status code returned by the upstream model provider, or null when there was none (then omitted).
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamErrorEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
