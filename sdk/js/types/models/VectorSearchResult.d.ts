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
    constructor(result?: {
        Score?: number | null;
        Distance?: number | null;
        InnerProduct?: number | null;
        GraphGUID?: string | null;
        NodeGUID?: string | null;
        EdgeGUID?: string | null;
    });
    Score: number;
    Distance: number;
    InnerProduct: number;
    GraphGUID: any;
    NodeGUID: any;
    EdgeGUID: any;
}
