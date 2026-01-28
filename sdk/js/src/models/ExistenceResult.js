import EdgeBetween from './EdgeBetween';

/**
 * ExistenceResult class representing the result of an existence check for multiple identifiers.
 */
export default class ExistenceResult {
  /**
   * @param {Object} existenceResult - Optional initial data for the existence result.
   * @param {string[]} existenceResult.ExistingNodes - Array of existing node GUIDs.
   * @param {string[]} existenceResult.MissingNodes - Array of missing node GUIDs.
   * @param {string[]} existenceResult.ExistingEdges - Array of existing edge GUIDs.
   * @param {string[]} existenceResult.MissingEdges - Array of missing edge GUIDs.
   * @param {EdgeBetween[]} existenceResult.ExistingEdgesBetween - Array of EdgeBetween instances for existing edges.
   * @param {EdgeBetween[]} existenceResult.MissingEdgesBetween - Array of EdgeBetween instances for missing edges.
   */
  constructor(existenceResult = {}) {
    const {
      ExistingNodes = [],
      MissingNodes = [],
      ExistingEdges = [],
      MissingEdges = [],
      ExistingEdgesBetween = [],
      MissingEdgesBetween = [],
    } = existenceResult;

    this.existingNodes = ExistingNodes; // Array of existing node GUIDs
    this.missingNodes = MissingNodes; // Array of missing node GUIDs
    this.existingEdges = ExistingEdges; // Array of existing edge GUIDs
    this.missingEdges = MissingEdges; // Array of missing edge GUIDs
    this.existingEdgesBetween = ExistingEdgesBetween.map((edge) => new EdgeBetween(edge)); // Array of existing EdgeBetween instances
    this.missingEdgesBetween = MissingEdgesBetween.map((edge) => new EdgeBetween(edge)); // Array of missing EdgeBetween instances
  }
}
