import { v4 as uuidV4 } from 'uuid';

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
  constructor(thread = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = null,
      UserGUID = null,
      GraphGUID = null,
      Title = null,
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = thread;

    this.GUID = GUID; // Unique identifier for the chat thread
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.UserGUID = UserGUID; // Unique identifier for the owning user
    this.GraphGUID = GraphGUID; // Unique identifier for the bound graph
    this.Title = Title; // Title of the thread
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
