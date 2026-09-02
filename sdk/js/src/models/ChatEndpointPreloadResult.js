/**
 * ChatEndpointPreloadResult class representing the result of a chat endpoint model preload (warm-up) request.
 */
export default class ChatEndpointPreloadResult {
  /**
   * @param {Object} result - Information about the preload result.
   * @param {string|null} [result.EndpointGUID=null] - Chat endpoint GUID (default is null).
   * @param {string|null} [result.Model=null] - Model configured on the endpoint (default is null).
   * @param {string} [result.Provider='OpenAI'] - Provider type of the endpoint (default is 'OpenAI').
   * @param {boolean} [result.Supported=false] - Indicates whether the provider supports model preloading (default is false).
   * @param {boolean} [result.Started=false] - Indicates whether a background warm-up was started (default is false).
   * @param {boolean} [result.AlreadyInProgress=false] - Indicates whether a warm-up was already in flight (default is false).
   */
  constructor(result = {}) {
    const {
      EndpointGUID = null,
      Model = null,
      Provider = 'OpenAI',
      Supported = false,
      Started = false,
      AlreadyInProgress = false,
    } = result;

    this.EndpointGUID = EndpointGUID; // Chat endpoint GUID
    this.Model = Model; // Model configured on the endpoint
    this.Provider = Provider; // Provider type of the endpoint
    this.Supported = Supported; // Indicates if the provider supports preloading
    this.Started = Started; // Indicates if a warm-up was started
    this.AlreadyInProgress = AlreadyInProgress; // Indicates if a warm-up was already in flight
  }
}
