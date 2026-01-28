import { v4 as uuidV4 } from 'uuid';

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
  constructor(tenant = {}) {
    const {
      GUID = uuidV4(),
      Name = null,
      Active = false,
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = tenant;

    this.GUID = GUID; // Unique identifier for the tenant
    this.Name = Name; // Name of the tenant
    this.Active = Active; // Indicates if the tenant is active
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
