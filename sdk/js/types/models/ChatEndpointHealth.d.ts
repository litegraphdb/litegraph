/**
 * ChatEndpointHealth class representing health status for a chat endpoint.
 */
export default class ChatEndpointHealth {
    /**
     * @param {Object} health - Information about the endpoint health.
     * @param {string} [health.EndpointGUID] - Globally unique identifier for the chat endpoint.
     * @param {string} [health.TenantGUID] - Globally unique identifier for the tenant.
     * @param {string|null} [health.Name=null] - Name of the chat endpoint.
     * @param {string} [health.EndpointType='Completion'] - Endpoint type: Embedding or Completion.
     * @param {boolean} [health.Monitored=false] - Indicates whether the endpoint is monitored (default is false).
     * @param {boolean|null} [health.Healthy=null] - Health status; null when not yet checked (default is null).
     * @param {Date|string|null} [health.LastCheckedUtc=null] - Last check timestamp in UTC (default is null).
     * @param {string|null} [health.LastError=null] - Last error message, if any (default is null).
     * @param {number} [health.ConsecutiveSuccesses=0] - Consecutive successful checks (default is 0).
     * @param {number} [health.ConsecutiveFailures=0] - Consecutive failed checks (default is 0).
     * @param {number|null} [health.UptimePercentage=null] - Uptime percentage over the check history (default is null).
     * @param {Array<Object>} [health.CheckHistory=[]] - Recent health check samples ({ TimestampUtc, Success, DurationMs }).
     */
    constructor(health?: {
        EndpointGUID?: string;
        TenantGUID?: string;
        Name?: string | null;
        EndpointType?: string;
        Monitored?: boolean;
        Healthy?: boolean | null;
        LastCheckedUtc?: Date | string | null;
        LastError?: string | null;
        ConsecutiveSuccesses?: number;
        ConsecutiveFailures?: number;
        UptimePercentage?: number | null;
        CheckHistory?: Array<any>;
    });
    EndpointGUID: string;
    TenantGUID: string;
    Name: string;
    EndpointType: string;
    Monitored: boolean;
    Healthy: boolean;
    LastCheckedUtc: Date;
    LastError: string;
    ConsecutiveSuccesses: number;
    ConsecutiveFailures: number;
    UptimePercentage: number;
    CheckHistory: any[];
}
