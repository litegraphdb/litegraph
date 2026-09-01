namespace LiteGraph.Server.Services.Chat
{
    /// <summary>
    /// Result of one in-process chat tool call.
    /// </summary>
    public class ChatToolExecutionResult
    {
        #region Public-Members

        /// <summary>
        /// Whether the tool call succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Serialized JSON result returned to the model.  Null on failure.
        /// </summary>
        public string ResultJson { get; set; } = null;

        /// <summary>
        /// Error message returned to the model.  Null on success.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Tool execution duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatToolExecutionResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
