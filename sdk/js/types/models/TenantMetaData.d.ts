/**
 * TenantMetadata class representing metadata for a tenant.
 */
export default class TenantMetaData {
    /**
     * @param {Object} tenant - Information about the tenant.
     * @param {string} [tenant.GUID] - Globally unique identifier for the tenant (automatically generated if not provided).
     * @param {string} [tenant.Name] - Name of the tenant.
     * @param {boolean} [tenant.Active=false] - Indicates whether the tenant is active (default is false).
     * @param {Date|string} [tenant.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Date|string} [tenant.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
     */
    constructor(tenant?: {
        GUID?: string;
        Name?: string;
        Active?: boolean;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: any;
    Name: string;
    Active: boolean;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
