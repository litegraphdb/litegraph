/**
 * ChatThread class representing a chat conversation thread.
 */
export default class ChatThread {
    /**
     * @param {Object} thread - Information about the chat thread.
     * @param {string} [thread.GUID] - Globally unique identifier for the chat thread (automatically generated if not provided).
     * @param {string} [thread.TenantGUID] - Globally unique identifier for the tenant.
     * @param {string} [thread.UserGUID] - Globally unique identifier for the owning user.
     * @param {string|null} [thread.GraphGUID=null] - Globally unique identifier for the bound graph (default is null).
     * @param {string|null} [thread.Title=null] - Title of the thread (default is null).
     * @param {Date|string} [thread.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Date|string} [thread.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
     */
    constructor(thread?: {
        GUID?: string;
        TenantGUID?: string;
        UserGUID?: string;
        GraphGUID?: string | null;
        Title?: string | null;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: string;
    TenantGUID: string;
    UserGUID: string;
    GraphGUID: string;
    Title: string;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
