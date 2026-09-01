namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// One event from a streaming chat completion.
    /// Exactly which properties are populated depends on the value of Event; unused properties are null.
    /// </summary>
    public class ChatStreamEvent
    {
        #region Public-Members

        /// <summary>
        /// Event discriminator.  One of started, delta, thinking, retrieval, tool_call, tool_result, usage, or error.
        /// </summary>
        public string Event { get; set; } = null;

        /// <summary>
        /// Text content.  Populated for delta and thinking events.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Thread GUID.  Populated for started events.
        /// </summary>
        public Guid? ThreadGUID { get; set; } = null;

        /// <summary>
        /// Turn GUID.  Populated for started events.
        /// </summary>
        public Guid? TurnGUID { get; set; } = null;

        /// <summary>
        /// Tool name.  Populated for tool_call and tool_result events.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Tool arguments as a JSON string.  Populated for tool_call events.
        /// </summary>
        public string Arguments { get; set; } = null;

        /// <summary>
        /// Whether the tool call succeeded.  Populated for tool_result events.
        /// </summary>
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Error message.  Populated for failed tool_result events.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Tool runtime in milliseconds.  Populated for tool_result events.
        /// </summary>
        public double? RuntimeMs { get; set; } = null;

        /// <summary>
        /// Tool loop iteration number.  Populated for tool_call events.
        /// </summary>
        public int? Iteration { get; set; } = null;

        /// <summary>
        /// Retrieved context chunks as a raw JSON array string.  Populated for retrieval events.
        /// </summary>
        public string Chunks { get; set; } = null;

        /// <summary>
        /// Final usage and timing summary.  Populated for usage events.
        /// </summary>
        public ChatCompletionResult Usage { get; set; } = null;

        /// <summary>
        /// Error message.  Populated for error events.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// HTTP status code associated with an error event.  Null when not supplied.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatStreamEvent()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
