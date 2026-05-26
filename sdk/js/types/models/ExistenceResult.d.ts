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
     * @param {string[]} existenceResult.ExistingVectors - Array of existing vector GUIDs.
     * @param {string[]} existenceResult.MissingVectors - Array of missing vector GUIDs.
     * @param {EdgeBetween[]} existenceResult.ExistingEdgesBetween - Array of EdgeBetween instances for existing edges.
     * @param {EdgeBetween[]} existenceResult.MissingEdgesBetween - Array of EdgeBetween instances for missing edges.
     */
    constructor(existenceResult?: {
        ExistingNodes: string[];
        MissingNodes: string[];
        ExistingEdges: string[];
        MissingEdges: string[];
        ExistingVectors: string[];
        MissingVectors: string[];
        ExistingEdgesBetween: EdgeBetween[];
        MissingEdgesBetween: EdgeBetween[];
    });
    existingNodes: string[];
    missingNodes: string[];
    existingEdges: string[];
    missingEdges: string[];
    existingVectors: string[];
    missingVectors: string[];
    existingEdgesBetween: EdgeBetween[];
    missingEdgesBetween: EdgeBetween[];
}
import EdgeBetween from './EdgeBetween';
