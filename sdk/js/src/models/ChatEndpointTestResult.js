/**
 * ChatEndpointTestResult class representing the result of a chat endpoint connectivity test.
 */
export default class ChatEndpointTestResult {
  /**
   * @param {Object} result - Information about the test result.
   * @param {boolean} [result.Reachable=false] - Indicates whether the endpoint is reachable (default is false).
   * @param {string[]|null} [result.Models=null] - Models advertised by the provider, if available (default is null).
   * @param {boolean|null} [result.ModelExists=null] - Indicates whether the configured model exists, if determinable (default is null).
   * @param {string|null} [result.Error=null] - Error message, if any (default is null).
   * @param {number} [result.RuntimeMs=0] - Test runtime in milliseconds (default is 0).
   */
  constructor(result = {}) {
    const { Reachable = false, Models = null, ModelExists = null, Error = null, RuntimeMs = 0 } = result;

    this.Reachable = Reachable; // Indicates if the endpoint is reachable
    this.Models = Models; // Models advertised by the provider
    this.ModelExists = ModelExists; // Indicates if the configured model exists
    this.Error = Error; // Error message
    this.RuntimeMs = RuntimeMs; // Test runtime in milliseconds
  }
}
