namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Tool result content returned to the model when a tool call fails.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatToolErrorContent
    {
        #region Public-Members

        /// <summary>
        /// Error message; null is omitted.
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatToolErrorContent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
