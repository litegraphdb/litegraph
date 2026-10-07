namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat stream event sent first, naming the thread and turn the completion belongs to.
    /// </summary>
    /// <remarks>
    /// Serialized with the server's JSON settings: property names as written in <see cref="JsonPropertyNameAttribute"/>,
    /// in declaration order, and null values omitted. The JSON is pinned by the Aot.Server Touchstone suite.
    /// Thread safety: not thread-safe; create one per event.
    /// </remarks>
    public class ChatStreamStartedEvent
    {
        #region Public-Members

        /// <summary>
        /// Event name, always "started".
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; } = "started";

        /// <summary>
        /// Thread GUID.
        /// </summary>
        [JsonPropertyName("threadGuid")]
        public Guid ThreadGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Turn GUID.
        /// </summary>
        [JsonPropertyName("turnGuid")]
        public Guid TurnGuid { get; set; } = Guid.Empty;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamStartedEvent()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
