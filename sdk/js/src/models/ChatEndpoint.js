import { v4 as uuidV4 } from 'uuid';

/**
 * ChatEndpoint class representing an LLM endpoint configuration.
 */
export default class ChatEndpoint {
  /**
   * @param {Object} endpoint - Information about the chat endpoint.
   * @param {string} [endpoint.GUID] - Globally unique identifier for the chat endpoint (automatically generated if not provided).
   * @param {string} [endpoint.TenantGUID] - Globally unique identifier for the tenant.
   * @param {string} [endpoint.Name] - Name of the chat endpoint.
   * @param {string} [endpoint.EndpointType='Completion'] - Endpoint type: Embedding or Completion (default is Completion).
   * @param {string} [endpoint.Provider='OpenAI'] - Provider type: OpenAI, Ollama, Gemini, Anthropic, or VoyageAI (default is OpenAI).
   * @param {string} [endpoint.Endpoint] - Absolute http/https URL of the upstream provider endpoint.
   * @param {string} [endpoint.ApiKey] - API key for the provider (redacted in server responses).
   * @param {string} [endpoint.Model] - Model name to use with this endpoint.
   * @param {number} [endpoint.ContextWindowTokens=0] - Context window size of the model in tokens; 0 means unspecified (default is 0, minimum is 0, maximum is 100000000).
   * @param {number} [endpoint.MaxOutputTokens=4096] - Maximum output tokens per completion (default is 4096, minimum is 1, maximum is 10000000).
   * @param {number} [endpoint.Temperature=0.7] - Sampling temperature (default is 0.7, minimum is 0, maximum is 2).
   * @param {number} [endpoint.TimeoutMs=120000] - Request timeout in milliseconds (default is 120000, minimum is 1000).
   * @param {number} [endpoint.MaxConcurrentRequests=2] - Maximum concurrent requests to the upstream endpoint (default is 2, minimum is 1).
   * @param {boolean} [endpoint.Active=true] - Indicates whether the endpoint is active (default is true).
   * @param {boolean} [endpoint.HealthCheckEnabled=true] - Indicates whether health checks are enabled (default is true).
   * @param {string} [endpoint.HealthCheckUrl] - Optional health check URL override.
   * @param {string} [endpoint.HealthCheckMethod='GET'] - Health check HTTP method: GET or HEAD (default is GET).
   * @param {number} [endpoint.HealthCheckIntervalMs=30000] - Interval between health checks in milliseconds (default is 30000, minimum is 1000).
   * @param {number} [endpoint.HealthCheckTimeoutMs=10000] - Health check timeout in milliseconds (default is 10000, minimum is 1000).
   * @param {number} [endpoint.HealthCheckExpectedStatusCode=200] - HTTP status code expected from a healthy endpoint (default is 200, minimum is 100, maximum is 599).
   * @param {number} [endpoint.HealthyThreshold=2] - Consecutive successful checks required to transition to healthy (default is 2, minimum is 1).
   * @param {number} [endpoint.UnhealthyThreshold=2] - Consecutive failed checks required to transition to unhealthy (default is 2, minimum is 1).
   * @param {boolean} [endpoint.HealthCheckUseAuth=false] - Indicates whether health checks send authentication (default is false).
   * @param {Date|string} [endpoint.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   * @param {Date|string} [endpoint.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
   */
  constructor(endpoint = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = null,
      Name = null,
      EndpointType = 'Completion',
      Provider = 'OpenAI',
      Endpoint = null,
      ApiKey = null,
      Model = null,
      ContextWindowTokens = 0,
      MaxOutputTokens = 4096,
      Temperature = 0.7,
      TimeoutMs = 120000,
      MaxConcurrentRequests = 2,
      Active = true,
      HealthCheckEnabled = true,
      HealthCheckUrl = null,
      HealthCheckMethod = 'GET',
      HealthCheckIntervalMs = 30000,
      HealthCheckTimeoutMs = 10000,
      HealthCheckExpectedStatusCode = 200,
      HealthyThreshold = 2,
      UnhealthyThreshold = 2,
      HealthCheckUseAuth = false,
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = endpoint;

    this.GUID = GUID; // Unique identifier for the chat endpoint
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.Name = Name; // Name of the chat endpoint
    this.EndpointType = EndpointType; // Endpoint type (Embedding or Completion)
    this.Provider = Provider; // Provider type
    this.Endpoint = Endpoint; // Upstream provider URL
    this.ApiKey = ApiKey; // API key (redacted in responses)
    this.Model = Model; // Model name
    this.ContextWindowTokens = ContextWindowTokens; // Context window size in tokens (0 means unspecified)
    this.MaxOutputTokens = MaxOutputTokens; // Maximum output tokens per completion
    this.Temperature = Temperature; // Sampling temperature
    this.TimeoutMs = TimeoutMs; // Request timeout in milliseconds
    this.MaxConcurrentRequests = MaxConcurrentRequests; // Maximum concurrent upstream requests
    this.Active = Active; // Indicates if the endpoint is active
    this.HealthCheckEnabled = HealthCheckEnabled; // Indicates if health checks are enabled
    this.HealthCheckUrl = HealthCheckUrl; // Optional health check URL override
    this.HealthCheckMethod = HealthCheckMethod; // Health check HTTP method (GET or HEAD)
    this.HealthCheckIntervalMs = HealthCheckIntervalMs; // Interval between health checks in milliseconds
    this.HealthCheckTimeoutMs = HealthCheckTimeoutMs; // Health check timeout in milliseconds
    this.HealthCheckExpectedStatusCode = HealthCheckExpectedStatusCode; // Expected healthy HTTP status code
    this.HealthyThreshold = HealthyThreshold; // Consecutive successes required to become healthy
    this.UnhealthyThreshold = UnhealthyThreshold; // Consecutive failures required to become unhealthy
    this.HealthCheckUseAuth = HealthCheckUseAuth; // Indicates if health checks send authentication
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
