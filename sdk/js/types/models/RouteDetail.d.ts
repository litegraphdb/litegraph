/**
 * RouteDetail class representing the total cost and ordered list of edges between two nodes.
 */
export default class RouteDetail {
    /**
     * @param {Object} detail - Information about the route detail.
     * @param {Array<Edge>} detail.Edges - Array of Edge objects (default is an empty array).
     */
    constructor(detail?: {
        Edges: Array<Edge>;
    });
    _edges: Edge[];
    /**
     * Gets the total cost of the route.
     * @returns {number} The total cost of the route.
     */
    get TotalCost(): number;
    /**
     * Sets the Edges.
     * @param {Array<Edge>} value - The array of Edge objects to set.
     */
    set Edges(value: Array<Edge>);
    /**
     * Gets the Edges.
     * @returns {Array<Edge>} The array of Edge objects.
     */
    get Edges(): Array<Edge>;
}
import Edge from './Edge';
