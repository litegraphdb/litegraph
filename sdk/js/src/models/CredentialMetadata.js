import { v4 as uuidV4 } from 'uuid';

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
  constructor(credential = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = 'default',
      UserGUID = 'default',
      Name = '',
      BearerToken = '',
      Active = false,
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = credential;

    this.GUID = GUID; // Unique identifier for the credential
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.UserGUID = UserGUID; // Unique identifier for the user
    this.Name = Name; // Name of the credential
    this.BearerToken = BearerToken; // Bearer token
    this.Active = Active; // Indicates if the credential is active
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
