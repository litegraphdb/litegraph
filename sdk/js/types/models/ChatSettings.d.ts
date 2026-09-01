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
     * @param {boolean} [settings.EnableMutationTools=true] - Indicates whether mutation tools are enabled (default is true).
     * @param {number} [settings.MaxToolIterations=50] - Maximum tool loop iterations (default is 50).
     * @param {boolean} [settings.EnableRag=true] - Indicates whether RAG is enabled (default is true).
     * @param {number} [settings.RagTopK=8] - Number of RAG chunks to retrieve (default is 8).
     * @param {number} [settings.RagScoreThreshold=0] - Minimum RAG score threshold (default is 0).
     * @param {number} [settings.HistoryRetentionDays=30] - Chat history retention in days (default is 30).
     * @param {Date|string} [settings.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Date|string} [settings.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
     */
    constructor(settings?: {
        TenantGUID?: string;
        DefaultCompletionEndpointGUID?: string | null;
        DefaultEmbeddingEndpointGUID?: string | null;
        SystemPrompt?: string | null;
        EnableChat?: boolean;
        EnableTools?: boolean;
        EnableMutationTools?: boolean;
        MaxToolIterations?: number;
        EnableRag?: boolean;
        RagTopK?: number;
        RagScoreThreshold?: number;
        HistoryRetentionDays?: number;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    TenantGUID: string;
    DefaultCompletionEndpointGUID: string;
    DefaultEmbeddingEndpointGUID: string;
    SystemPrompt: string;
    EnableChat: boolean;
    EnableTools: boolean;
    EnableMutationTools: boolean;
    MaxToolIterations: number;
    EnableRag: boolean;
    RagTopK: number;
    RagScoreThreshold: number;
    HistoryRetentionDays: number;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
