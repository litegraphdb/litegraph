import { v4 as uuidV4 } from 'uuid';

/**
 * Node class representing a node in the graph.
 */
export default class Node {
  /**
   * @param {Object} node - Information about the node.
   * @param {string} node.GUID - Globally unique identifier (automatically generated if not provided).
   * @param {string} node.GraphGUID - Globally unique identifier for the graph (automatically generated if not provided).
   * @param {string} node.Name - Name of the node.
   * @param {Object} node.Data - Object data associated with the node (default is null).
   * @param {Date} node.CreatedUtc - Creation timestamp in UTC (defaults to now).
   * @param {Date} node.LastUpdateUtc - Last update timestamp in UTC (defaults to now).
   * @param {string[]} node.Labels - Array of labels associated with the node.
   * @param {Object} node.Tags - Key-value pairs of tags.
   * @param {[]} node.Vectors - Array of vector embeddings.
   */
  constructor(node = {}) {
    const {
      GUID = uuidV4(),
      GraphGUID = uuidV4(),
      Name = null,
      Data = null,
      CreatedUtc = new Date(),
      LastUpdateUtc = new Date(),
      Labels = [],
      Tags = {},
      Vectors = [],
    } = node;

    this.GUID = GUID; // Unique identifier for the node
    this.GraphGUID = GraphGUID; // Unique identifier for the graph this node belongs to
    this.name = Name; // Name of the node
    this.data = Data; // Object data
    this.createdUtc = CreatedUtc; // Creation timestamp
    this.lastUpdateUtc = LastUpdateUtc; // Last update timestamp
    this.labels = Labels; // Array of labels
    this.tags = Tags; // Key-value pairs of tags
    this.vectors = Vectors; // Array of vector embeddings
  }
}
