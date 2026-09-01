/**
 * ChatCompletionResult class representing the result of a non-streaming chat completion.
 */
export default class ChatCompletionResult {
  /**
   * @param {Object} result - Information about the completion result.
   * @param {string} [result.ThreadGUID] - Globally unique identifier for the thread.
   * @param {string} [result.TurnGUID] - Globally unique identifier for the turn.
   * @param {string|null} [result.Message=null] - The assistant's message (default is null).
   * @param {string|null} [result.Reasoning=null] - Reasoning/thinking content, if any (default is null).
   * @param {string} [result.Provider='OpenAI'] - Provider type used for the completion (default is OpenAI).
   * @param {string|null} [result.Model=null] - Model name used (default is null).
   * @param {number|null} [result.PromptTokens=null] - Prompt token count (default is null).
   * @param {number|null} [result.CompletionTokens=null] - Completion token count (default is null).
   * @param {number|null} [result.TimeToFirstTokenMs=null] - Time to first token in milliseconds (default is null).
   * @param {number|null} [result.TimeToLastTokenMs=null] - Time to last token in milliseconds (default is null).
   * @param {number} [result.TotalDurationMs=0] - Total duration in milliseconds (default is 0).
   * @param {number|null} [result.TokensPerSecondOverall=null] - Overall tokens per second (default is null).
   * @param {number} [result.ToolCallCount=0] - Number of tool calls (default is 0).
   * @param {number} [result.ToolLoopIterations=0] - Number of tool loop iterations (default is 0).
   * @param {number} [result.RetrievedChunkCount=0] - Number of retrieved RAG chunks (default is 0).
   * @param {number} [result.RetryCount=0] - Number of retries performed (default is 0).
   */
  constructor(result = {}) {
    const {
      ThreadGUID = null,
      TurnGUID = null,
      Message = null,
      Reasoning = null,
      Provider = 'OpenAI',
      Model = null,
      PromptTokens = null,
      CompletionTokens = null,
      TimeToFirstTokenMs = null,
      TimeToLastTokenMs = null,
      TotalDurationMs = 0,
      TokensPerSecondOverall = null,
      ToolCallCount = 0,
      ToolLoopIterations = 0,
      RetrievedChunkCount = 0,
      RetryCount = 0,
    } = result;

    this.ThreadGUID = ThreadGUID; // Unique identifier for the thread
    this.TurnGUID = TurnGUID; // Unique identifier for the turn
    this.Message = Message; // Assistant message
    this.Reasoning = Reasoning; // Reasoning content
    this.Provider = Provider; // Provider type
    this.Model = Model; // Model name
    this.PromptTokens = PromptTokens; // Prompt token count
    this.CompletionTokens = CompletionTokens; // Completion token count
    this.TimeToFirstTokenMs = TimeToFirstTokenMs; // Time to first token
    this.TimeToLastTokenMs = TimeToLastTokenMs; // Time to last token
    this.TotalDurationMs = TotalDurationMs; // Total duration
    this.TokensPerSecondOverall = TokensPerSecondOverall; // Overall tokens per second
    this.ToolCallCount = ToolCallCount; // Tool call count
    this.ToolLoopIterations = ToolLoopIterations; // Tool loop iterations
    this.RetrievedChunkCount = RetrievedChunkCount; // Retrieved chunk count
    this.RetryCount = RetryCount; // Retry count
  }
}
