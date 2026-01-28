/**
 * RouteResult class representing the response of a route request.
 */
export default class RouteResult {
    /**
     * @param {Object} response - Information about the route response.
     * @param {Timestamp} response.Timestamp - Timestamp of the response (default is a new Timestamp).
     * @param {Array<RouteDetail>} response.Routes - Array of RouteDetail objects (default is an empty array).
     */
    constructor(response?: {
        Timestamp: any;
        Routes: Array<RouteDetail>;
    });
    _timestamp: any;
    _routes: RouteDetail[];
    /**
     * Sets the Timestamp.
     * @param {Timestamp} value - The Timestamp to set.
     * @throws {Error} If the value is null.
     */
    set Timestamp(value: Timestamp);
    /**
     * Gets the Timestamp.
     * @returns {Timestamp} The Timestamp of the response.
     */
    get Timestamp(): Timestamp;
    /**
     * Sets the Routes.
     * @param {Array<RouteDetail>} value - The array of RouteDetail objects to set.
     */
    set Routes(value: Array<RouteDetail>);
    /**
     * Gets the Routes.
     * @returns {Array<RouteDetail>} The array of RouteDetail objects.
     */
    get Routes(): Array<RouteDetail>;
}
import RouteDetail from './RouteDetail';
