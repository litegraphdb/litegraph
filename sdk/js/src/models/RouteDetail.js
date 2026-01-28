import Edge from './Edge';

/**
 * RouteDetail class representing the total cost and ordered list of edges between two nodes.
 */
export default class RouteDetail {
  /**
   * @param {Object} detail - Information about the route detail.
   * @param {Array<Edge>} detail.Edges - Array of Edge objects (default is an empty array).
   */
  constructor(detail = {}) {
    const { Edges = [] } = detail; // Default to an empty array if not provided
    this._edges = Edges; // Private property for Edges
  }

  /**
   * Gets the total cost of the route.
   * @returns {number} The total cost of the route.
   */
  get TotalCost() {
    if (!this._edges || this._edges.length < 1) return 0;
    return this._edges.reduce((sum, edge) => sum + edge.Cost, 0); // Calculate total cost
  }

  /**
   * Gets the Edges.
   * @returns {Array<Edge>} The array of Edge objects.
   */
  get Edges() {
    return this._edges;
  }

  /**
   * Sets the Edges.
   * @param {Array<Edge>} value - The array of Edge objects to set.
   */
  set Edges(value) {
    this._edges = value || []; // Default to an empty array if null
  }
}
