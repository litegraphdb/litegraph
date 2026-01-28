import { v4 as uuidV4 } from 'uuid';

/**
 * Edge class representing an edge in a graph.
 */
export default class Edge {
  /**
   * @param {Object} edge - Information about the edge.
   * @param {string} [edge.GUID] - Globally unique identifier for the edge (automatically generated if not provided).
   * @param {string} [edge.GraphGUID] - Globally unique identifier for the graph (automatically generated if not provided).
   * @param {string} [edge.Name] - Name of the edge.
   * @param {string} [edge.From] - Globally unique identifier of the from node.
   * @param {string} [edge.To] - Globally unique identifier of the to node.
   * @param {number} [edge.Cost=0] - Cost associated with the edge (default is 0).
   * @param {Date} [edge.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   * @param {Object} [edge.Data] - Additional object data associated with the edge (default is null).
   * @param {Date} [edge.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
   * @param {Array} [edge.Labels] - Array of labels associated with the edge.
   * @param {Object} [edge.Tags] - Key-value pairs of tags associated with the edge.
   * @param {Array} [edge.Vectors] - Array of vector embeddings associated with the edge.
   */
  constructor(edge = {}) {
    const {
      GUID = uuidV4(),
      GraphGUID = uuidV4(),
      Name = null,
      From = uuidV4(),
      To = uuidV4(),
      Cost = 0,
      CreatedUtc = new Date().toISOString(),
      Data = null,
      LastUpdateUtc = new Date().toISOString(),
      Labels = [],
      Tags = {},
      Vectors = [],
    } = edge;

    this.GUID = GUID; // Unique identifier for the edge
    this.GraphGUID = GraphGUID; // Unique identifier for the graph
    this.name = Name; // Name of the edge
    this.from = From; // From node identifier
    this.to = To; // To node identifier
    this.cost = Cost; // Cost associated with the edge
    this.createdUtc = CreatedUtc; // Creation timestamp
    this.data = Data; // Additional data associated with the edge
    this.lastUpdateUtc = LastUpdateUtc; // Last update timestamp
    this.labels = Labels; // Array of labels
    this.tags = Tags; // Key-value pairs of tags
    this.vectors = Vectors; // Array of vector embeddings
  }
}
