/**
 * ChatModelSummary class representing a non-privileged projection of a chat endpoint,
 * exposing only what a chat user needs to pick a model. Endpoint URLs, keys, and
 * health configuration are never included.
 */
export default class ChatModelSummary {
  /**
   * @param {Object} summary - Information about the model summary.
   * @param {string} [summary.GUID] - Endpoint GUID, supplied as CompletionEndpointGUID or EmbeddingEndpointGUID on completion requests.
   * @param {string|null} [summary.Name=null] - Human-readable endpoint name (default is null).
   * @param {string|null} [summary.Model=null] - Model identifier used by the provider (default is null).
   * @param {string} [summary.Provider='OpenAI'] - Provider type: OpenAI, Ollama, Gemini, Anthropic, or VoyageAI (default is OpenAI).
   * @param {string} [summary.EndpointType='Completion'] - Endpoint type: Embedding or Completion (default is Completion).
   * @param {boolean} [summary.IsDefault=false] - Indicates whether this endpoint is the tenant default for its type (default is false).
   */
  constructor(summary = {}) {
    const {
      GUID = null,
      Name = null,
      Model = null,
      Provider = 'OpenAI',
      EndpointType = 'Completion',
      IsDefault = false,
    } = summary;

    this.GUID = GUID; // Unique identifier for the chat endpoint
    this.Name = Name; // Human-readable endpoint name
    this.Model = Model; // Model identifier used by the provider
    this.Provider = Provider; // Provider type
    this.EndpointType = EndpointType; // Endpoint type (Embedding or Completion)
    this.IsDefault = IsDefault; // Indicates if this endpoint is the tenant default for its type
  }
}
