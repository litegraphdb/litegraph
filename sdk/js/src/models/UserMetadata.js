import { v4 as uuidV4 } from 'uuid';

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
  constructor(user = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = uuidV4(),
      FirstName = '',
      LastName = '',
      Email = '',
      Password = '',
      Active = false,
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = user;

    this.GUID = GUID; // Unique identifier for the user
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.FirstName = FirstName; // First name of the user
    this.LastName = LastName; // Last name of the user
    this.Email = Email; // Email of the user
    this.Password = Password; // Password for the user
    this.Active = Active; // Indicates if the user is active
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
