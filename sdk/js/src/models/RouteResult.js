import RouteDetail from './RouteDetail';
/**
 * RouteResult class representing the response of a route request.
 */
export default class RouteResult {
  /**
   * @param {Object} response - Information about the route response.
   * @param {Timestamp} response.Timestamp - Timestamp of the response (default is a new Timestamp).
   * @param {Array<RouteDetail>} response.Routes - Array of RouteDetail objects (default is an empty array).
   */
  constructor(response = {}) {
    const {
      Timestamp = Date.now(), // Default to a new Timestamp object
      Routes = [], // Default to an empty array
    } = response;

    this._timestamp = Timestamp; // Private property for Timestamp
    this._routes = Routes; // Private property for Routes
  }

  /**
   * Gets the Timestamp.
   * @returns {Timestamp} The Timestamp of the response.
   */
  get Timestamp() {
    return this._timestamp;
  }

  /**
   * Sets the Timestamp.
   * @param {Timestamp} value - The Timestamp to set.
   * @throws {Error} If the value is null.
   */
  set Timestamp(value) {
    if (value === null) throw new Error('Timestamp cannot be null');
    this._timestamp = value;
  }

  /**
   * Gets the Routes.
   * @returns {Array<RouteDetail>} The array of RouteDetail objects.
   */
  get Routes() {
    return this._routes;
  }

  /**
   * Sets the Routes.
   * @param {Array<RouteDetail>} value - The array of RouteDetail objects to set.
   */
  set Routes(value) {
    this._routes = value || []; // Default to an empty array if null
  }
}
