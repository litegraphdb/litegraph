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
     * @param {boolean} [endpoint.Active=true] - Indicates whether the endpoint is active (default is true).
     * @param {boolean} [endpoint.HealthCheckEnabled=true] - Indicates whether health checks are enabled (default is true).
     * @param {string} [endpoint.HealthCheckUrl] - Optional health check URL override.
     * @param {boolean} [endpoint.HealthCheckUseAuth=false] - Indicates whether health checks send authentication (default is false).
     * @param {Date|string} [endpoint.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Date|string} [endpoint.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
     */
    constructor(endpoint?: {
        GUID?: string;
        TenantGUID?: string;
        Name?: string;
        EndpointType?: string;
        Provider?: string;
        Endpoint?: string;
        ApiKey?: string;
        Model?: string;
        ContextWindowTokens?: number;
        Active?: boolean;
        HealthCheckEnabled?: boolean;
        HealthCheckUrl?: string;
        HealthCheckUseAuth?: boolean;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: string;
    TenantGUID: string;
    Name: string;
    EndpointType: string;
    Provider: string;
    Endpoint: string;
    ApiKey: string;
    Model: string;
    ContextWindowTokens: number;
    Active: boolean;
    HealthCheckEnabled: boolean;
    HealthCheckUrl: string;
    HealthCheckUseAuth: boolean;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
