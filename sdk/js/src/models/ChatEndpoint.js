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
   * @param {boolean} [endpoint.Active=true] - Indicates whether the endpoint is active (default is true).
   * @param {boolean} [endpoint.HealthCheckEnabled=true] - Indicates whether health checks are enabled (default is true).
   * @param {string} [endpoint.HealthCheckUrl] - Optional health check URL override.
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
      Active = true,
      HealthCheckEnabled = true,
      HealthCheckUrl = null,
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
    this.Active = Active; // Indicates if the endpoint is active
    this.HealthCheckEnabled = HealthCheckEnabled; // Indicates if health checks are enabled
    this.HealthCheckUrl = HealthCheckUrl; // Optional health check URL override
    this.HealthCheckUseAuth = HealthCheckUseAuth; // Indicates if health checks send authentication
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
