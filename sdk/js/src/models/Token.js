import TenantMetaData from './TenantMetaData';
import UserMetadata from './UserMetadata';

/**
 * Token class representing authentication token details.
 */
export default class Token {
  /**
   * @param {Object} token - Information about the authentication token.
   * @param {Date|string} [token.TimestampUtc] - Timestamp when the token was created in UTC.
   * @param {Date|string} [token.ExpirationUtc] - Timestamp when the token expires in UTC.
   * @param {boolean} [token.IsExpired=false] - Indicates whether the token is expired.
   * @param {string} [token.TenantGUID] - Globally unique identifier for the tenant.
   * @param {string} [token.UserGUID] - Globally unique identifier for the user.
   * @param {UserMetadata} [token.User] - User metadata
   * @param {TenantMetaData} {token.Tenant} - Tenant metadata
   * @param {string} [token.Token] - The actual token string.
   * @param {boolean} [token.Valid=true] - Indicates whether the token is valid.
   */
  constructor(token = {}) {
    const {
      TimestampUtc = new Date().toISOString(),
      ExpirationUtc = new Date().toISOString(),
      IsExpired = false,
      TenantGUID = '00000000-0000-0000-0000-000000000000',
      UserGUID = '00000000-0000-0000-0000-000000000000',
      Token = null,
      Valid = true,
      User = {},
      Tenant = {},
    } = token;

    this.TimestampUtc = new Date(TimestampUtc); // Token creation timestamp
    this.ExpirationUtc = new Date(ExpirationUtc); // Token expiration timestamp
    this.IsExpired = IsExpired; // Indicates if token is expired
    this.TenantGUID = TenantGUID; // Tenant identifier
    this.UserGUID = UserGUID; // User identifier
    this.Token = Token; // The token string
    this.Valid = Valid; // Token validity status
    this.User = new UserMetadata(User); // User metadata
    this.Tenant = new TenantMetaData(Tenant); // Tenant metadata
  }
}
