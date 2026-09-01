namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Chat completion result, returned as the non-streaming response body and inside the streaming usage event.
    /// </summary>
    public class ChatCompletionResult
    {
        #region Public-Members

        /// <summary>
        /// Thread GUID.
        /// </summary>
        public Guid ThreadGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Turn GUID.
        /// </summary>
        public Guid TurnGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Assistant message.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// Model reasoning text.  Null when the model emitted none.
        /// </summary>
        public string Reasoning { get; set; } = null;

        /// <summary>
        /// Provider of the completion endpoint.  Default is OpenAI.
        /// </summary>
        public ChatProviderTypeEnum Provider { get; set; } = ChatProviderTypeEnum.OpenAI;

        /// <summary>
        /// Model that generated the response.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Prompt tokens reported by the provider.  Null when unreported.
        /// </summary>
        public int? PromptTokens { get; set; } = null;

        /// <summary>
        /// Completion tokens reported by the provider.  Null when unreported.
        /// </summary>
        public int? CompletionTokens { get; set; } = null;

        /// <summary>
        /// Time to first token in milliseconds.  Null when unknown.
        /// </summary>
        public double? TimeToFirstTokenMs { get; set; } = null;

        /// <summary>
        /// Time to last token in milliseconds.  Null when unknown.
        /// </summary>
        public double? TimeToLastTokenMs { get; set; } = null;

        /// <summary>
        /// Total turn duration in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; set; } = 0;

        /// <summary>
        /// Overall tokens per second.  Null when unknown.
        /// </summary>
        public double? TokensPerSecondOverall { get; set; } = null;

        /// <summary>
        /// Number of tool calls executed within the turn.
        /// </summary>
        public int ToolCallCount { get; set; } = 0;

        /// <summary>
        /// Number of tool loop iterations within the turn.
        /// </summary>
        public int ToolLoopIterations { get; set; } = 0;

        /// <summary>
        /// Number of retrieved context chunks injected into the prompt.
        /// </summary>
        public int RetrievedChunkCount { get; set; } = 0;

        /// <summary>
        /// Retries performed before the response started.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatCompletionResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
