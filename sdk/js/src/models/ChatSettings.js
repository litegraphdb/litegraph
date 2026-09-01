/**
 * ChatSettings class representing tenant-level chat configuration.
 */
export default class ChatSettings {
  /**
   * @param {Object} settings - Information about the chat settings.
   * @param {string} [settings.TenantGUID] - Globally unique identifier for the tenant.
   * @param {string|null} [settings.DefaultCompletionEndpointGUID=null] - Default completion endpoint GUID (default is null).
   * @param {string|null} [settings.DefaultEmbeddingEndpointGUID=null] - Default embedding endpoint GUID (default is null).
   * @param {string|null} [settings.SystemPrompt=null] - Default system prompt (default is null).
   * @param {boolean} [settings.EnableChat=true] - Indicates whether chat is enabled for the tenant (default is true).
   * @param {boolean} [settings.EnableTools=true] - Indicates whether tool use is enabled (default is true).
   * @param {boolean} [settings.EnableMutationTools=false] - Indicates whether mutation tools are enabled (default is false).
   * @param {number} [settings.MaxToolIterations=10] - Maximum tool loop iterations (default is 10).
   * @param {boolean} [settings.EnableRag=true] - Indicates whether RAG is enabled (default is true).
   * @param {number} [settings.RagTopK=8] - Number of RAG chunks to retrieve (default is 8).
   * @param {number} [settings.RagScoreThreshold=0] - Minimum RAG score threshold (default is 0).
   * @param {number} [settings.HistoryRetentionDays=90] - Chat history retention in days (default is 90).
   * @param {Date|string} [settings.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   * @param {Date|string} [settings.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
   */
  constructor(settings = {}) {
    const {
      TenantGUID = null,
      DefaultCompletionEndpointGUID = null,
      DefaultEmbeddingEndpointGUID = null,
      SystemPrompt = null,
      EnableChat = true,
      EnableTools = true,
      EnableMutationTools = true,
      MaxToolIterations = 50,
      EnableRag = true,
      RagTopK = 8,
      RagScoreThreshold = 0,
      HistoryRetentionDays = 30,
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = settings;

    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.DefaultCompletionEndpointGUID = DefaultCompletionEndpointGUID; // Default completion endpoint GUID
    this.DefaultEmbeddingEndpointGUID = DefaultEmbeddingEndpointGUID; // Default embedding endpoint GUID
    this.SystemPrompt = SystemPrompt; // Default system prompt
    this.EnableChat = EnableChat; // Indicates if chat is enabled
    this.EnableTools = EnableTools; // Indicates if tool use is enabled
    this.EnableMutationTools = EnableMutationTools; // Indicates if mutation tools are enabled
    this.MaxToolIterations = MaxToolIterations; // Maximum tool loop iterations
    this.EnableRag = EnableRag; // Indicates if RAG is enabled
    this.RagTopK = RagTopK; // Number of RAG chunks to retrieve
    this.RagScoreThreshold = RagScoreThreshold; // Minimum RAG score threshold
    this.HistoryRetentionDays = HistoryRetentionDays; // History retention in days
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
