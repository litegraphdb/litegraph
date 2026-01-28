import { v4 as uuidV4 } from 'uuid';

/**
 * LabelMetadata class representing metadata for a label.
 */
export default class LabelMetadata {
  /**
   * @param {Object} label - Information about the label.
   * @param {string} [label.GUID] - Globally unique identifier for the label (automatically generated if not provided).
   * @param {string} [label.TenantGUID] - Globally unique identifier for the tenant (automatically generated if not provided).
   * @param {string|null} [label.GraphGUID=null] - Globally unique identifier for the graph.
   * @param {string|null} [label.NodeGUID=null] - Globally unique identifier for the node.
   * @param {string|null} [label.EdgeGUID=null] - Globally unique identifier for the edge.
   * @param {string} [label.Label=''] - Label of the metadata.
   * @param {Date|string} [label.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   * @param {Date|string} [label.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
   */
  constructor(label = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = uuidV4(),
      GraphGUID = null,
      NodeGUID = null,
      EdgeGUID = null,
      Label = '',
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = label;

    this.GUID = GUID; // Unique identifier for the label
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.GraphGUID = GraphGUID; // Unique identifier for the graph
    this.NodeGUID = NodeGUID; // Unique identifier for the node
    this.EdgeGUID = EdgeGUID; // Unique identifier for the edge
    this.Label = Label; // Label of the metadata
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
