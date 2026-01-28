import { v4 as uuidV4 } from 'uuid';

/**
 * TagMetadata class representing metadata for a tag.
 */
export default class TagMetaData {
  /**
   * @param {Object} tag - Information about the tag.
   * @param {string} [tag.GUID] - Globally unique identifier for the tag (automatically generated if not provided).
   * @param {string} [tag.TenantGUID] - Globally unique identifier for the tenant.
   * @param {string} [tag.GraphGUID] - Globally unique identifier for the graph.
   * @param {string} [tag.NodeGUID] - Globally unique identifier for the node.
   * @param {string} [tag.EdgeGUID] - Globally unique identifier for the edge.
   * @param {string} [tag.Key] - Key of the tag.
   * @param {string} [tag.Value] - Value of the tag.
   * @param {Date|string} [tag.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   * @param {Date|string} [tag.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
   */
  constructor(tag = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = 'default',
      GraphGUID = null,
      NodeGUID = null,
      EdgeGUID = null,
      Key = '',
      Value = '',
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = tag;

    this.GUID = GUID; // Unique identifier for the tag
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.GraphGUID = GraphGUID; // Unique identifier for the graph
    this.NodeGUID = NodeGUID; // Unique identifier for the node
    this.EdgeGUID = EdgeGUID; // Unique identifier for the edge
    this.Key = Key; // Key of the tag
    this.Value = Value; // Value of the tag
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp
  }
}
