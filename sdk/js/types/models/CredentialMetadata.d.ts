/**
 * CredentialMetadata class representing metadata for a credential.
 */
export default class CredentialMetadata {
    /**
     * @param {Object} credential - Information about the credential.
     * @param {string} [credential.GUID] - Globally unique identifier for the credential (automatically generated if not provided).
     * @param {string} [credential.TenantGUID] - Globally unique identifier for the tenant.
     * @param {string} [credential.UserGUID] - Globally unique identifier for the user.
     * @param {string} [credential.Name] - Name of the credential.
     * @param {string} [credential.BearerToken] - Bearer token associated with the credential.
     * @param {boolean} [credential.Active=false] - Indicates whether the credential is active (default is false).
     * @param {Date|string} [credential.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Date|string} [credential.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
     */
    constructor(credential?: {
        GUID?: string;
        TenantGUID?: string;
        UserGUID?: string;
        Name?: string;
        BearerToken?: string;
        Active?: boolean;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: string;
    TenantGUID: string;
    UserGUID: string;
    Name: string;
    BearerToken: string;
    Active: boolean;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
