/**
 * UserMetadata class representing metadata for a user.
 */
export default class UserMetadata {
    /**
     * @param {Object} user - Information about the user.
     * @param {string} [user.GUID] - Globally unique identifier for the user (automatically generated if not provided).
     * @param {string} [user.TenantGUID] - Globally unique identifier for the tenant (automatically generated if not provided).
     * @param {string} [user.FirstName] - First name of the user.
     * @param {string} [user.LastName] - Last name of the user.
     * @param {string} [user.Email] - Email of the user.
     * @param {string} [user.Password] - Password for the user.
     * @param {boolean} [user.Active=false] - Indicates whether the user is active (default is false).
     * @param {Date|string} [user.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Date|string} [user.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
     */
    constructor(user?: {
        GUID?: string;
        TenantGUID?: string;
        FirstName?: string;
        LastName?: string;
        Email?: string;
        Password?: string;
        Active?: boolean;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: any;
    TenantGUID: any;
    FirstName: string;
    LastName: string;
    Email: string;
    Password: string;
    Active: boolean;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
