namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama-compatible chat response object.  Used both for streamed fragments (done false)
    /// and for the terminal or non-streaming object (done true, counters populated).
    /// </summary>
    public class OllamaChatResponse
    {
        #region Public-Members

        /// <summary>
        /// Model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Creation timestamp in ISO 8601 format.
        /// </summary>
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = null;

        /// <summary>
        /// Assistant message.  Carries a content fragment on streamed objects.
        /// </summary>
        [JsonPropertyName("message")]
        public OllamaChatMessage Message { get; set; } = null;

        /// <summary>
        /// Whether the response is complete.  Default is false.
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; } = false;

        /// <summary>
        /// Reason the response finished, for example stop or length.  Present only when done is true.
        /// </summary>
        [JsonPropertyName("done_reason")]
        public string DoneReason { get; set; } = null;

        /// <summary>
        /// Total request duration in nanoseconds.  Present only when done is true.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; } = null;

        /// <summary>
        /// Generation duration in nanoseconds.  Present only when done is true.
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; } = null;

        /// <summary>
        /// Prompt token count.  Present only when done is true.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; } = null;

        /// <summary>
        /// Completion token count.  Present only when done is true.
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OllamaChatResponse()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
