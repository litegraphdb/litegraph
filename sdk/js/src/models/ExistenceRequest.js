import { v4 as uuidV4 } from 'uuid';
import EdgeBetween from './EdgeBetween';

/**
 * ExistenceRequest class representing an existence check for multiple identifiers.
 */
export default class ExistenceRequest {
  /**
   * @param {Object} existenceRequest - Optional initial data for the existence request.
   * @param {string[]} existenceRequest.Nodes - Array of node GUIDs.
   * @param {string[]} existenceRequest.Edges - Array of edge GUIDs.
   * @param {EdgeBetween[]} existenceRequest.EdgesBetween - Array of EdgeBetween instances.
   */
  constructor(existenceRequest = {}) {
    const { Nodes = [], Edges = [], EdgesBetween = [] } = existenceRequest;

    this.nodes = Nodes; // Array of node GUIDs
    this.edges = Edges; // Array of edge GUIDs
    this.edgesBetween = EdgesBetween.map((edge) => new EdgeBetween(edge)); // Array of EdgeBetween objects
  }

  /**
   * Verify if the object contains at least one existence request.
   * @returns {boolean} True if any existence request is present.
   */
  containsExistenceRequest() {
    return (
      (this.nodes && this.nodes.length > 0) ||
      (this.edges && this.edges.length > 0) ||
      (this.edgesBetween && this.edgesBetween.length > 0)
    );
  }
}
