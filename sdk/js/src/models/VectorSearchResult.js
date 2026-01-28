import { v4 as uuidV4 } from 'uuid';

/**
 * VectorSearchResult class representing a search result for a vector.
 */
export class VectorSearchResult {
  /**
   * @param {Object} result - Information about the search result.
   * @param {number|null} [result.Score=null] - Score of the search result.
   * @param {number|null} [result.Distance=null] - Distance metric.
   * @param {number|null} [result.InnerProduct=null] - Inner product metric.
   * @param {string|null} [result.GraphGUID=null] - Unique identifier for the graph.
   * @param {string|null} [result.NodeGUID=null] - Unique identifier for the node.
   * @param {string|null} [result.EdgeGUID=null] - Unique identifier for the edge.
   */
  constructor(result = {}) {
    const {
      Score = null,
      Distance = null,
      InnerProduct = null,
      GraphGUID = null,
      NodeGUID = null,
      EdgeGUID = null,
    } = result;

    this.Score = Score; // Score of the search result
    this.Distance = Distance; // Distance metric
    this.InnerProduct = InnerProduct; // Inner product metric
    this.GraphGUID = GraphGUID || uuidV4(); // Unique identifier for the graph
    this.NodeGUID = NodeGUID || uuidV4(); // Unique identifier for the node
    this.EdgeGUID = EdgeGUID || uuidV4(); // Unique identifier for the edge
  }
}
