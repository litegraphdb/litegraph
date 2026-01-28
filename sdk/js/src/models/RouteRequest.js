import { v4 as uuidV4 } from 'uuid';

/**
 * RouteRequest class representing a request to find a route in the graph.
 */
export default class RouteRequest {
  /**
   * @param {Object} request - Information about the route request.
   * @param {string} request.Graph - Globally unique identifier for the graph (defaults to an empty GUID).
   * @param {string} request.From - Globally unique identifier for the from node (defaults to an empty GUID).
   * @param {string} request.To - Globally unique identifier for the to node (defaults to an empty GUID).
   * @param {Object} request.EdgeFilter - Expression used to filter edges (default is null).
   * @param {Object} request.NodeFilter - Expression used to filter nodes (default is null).
   */
  constructor(request = {}) {
    const {
      Graph = uuidV4(), // Assuming default(Guid) means an empty or new GUID in C#
      From = uuidV4(), // Assuming default(Guid) means an empty or new GUID in C#
      To = uuidV4(), // Assuming default(Guid) means an empty or new GUID in C#
      EdgeFilter = null,
      NodeFilter = null,
    } = request;

    this.graph = Graph; // Unique identifier for the graph
    this.from = From; // Unique identifier for the from node
    this.to = To; // Unique identifier for the to node
    this.edgeFilter = EdgeFilter; // Expression for filtering edges
    this.nodeFilter = NodeFilter; // Expression for filtering nodes
  }
}
