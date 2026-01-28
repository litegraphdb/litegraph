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
  constructor(result = {}) {
    const { Graphs = null, Nodes = null, Edges = null } = result;

    this.graphs = Graphs; // Array of Graph objects
    this.nodes = Nodes; // Array of Node objects
    this.edges = Edges; // Array of Edge objects
  }
}
