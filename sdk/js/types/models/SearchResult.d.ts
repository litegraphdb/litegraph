/**
 * SearchResult class representing the result of a search operation in the graph.
 */
export default class SearchResult {
    /**
     * @param {Object} result - Information about the search result.
     * @param {Array<Graph>} result.Graphs - Array of Graph objects (default is null).
     * @param {Array<Node>} result.Nodes - Array of Node objects (default is null).
     * @param {Array<Edge>} result.Edges - Array of Edge objects (default is null).
     */
    constructor(result?: {
        Graphs: Array<Graph>;
        Nodes: Array<Node>;
        Edges: Array<Edge>;
    });
    graphs: Graph[];
    nodes: Node[];
    edges: Edge[];
}
