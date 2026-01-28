import { v4 as uuidV4 } from 'uuid';

/**
 * VectorMetadata class representing metadata for a vector.
 */
export class VectorMetadata {
  /**
   * @param {Object} vector - Information about the vector.
   * @param {string} [vector.GUID] - Globally unique identifier for the vector (automatically generated if not provided).
   * @param {string} [vector.TenantGUID] - Globally unique identifier for the tenant (automatically generated if not provided).
   * @param {string|null} [vector.GraphGUID=null] - Globally unique identifier for the graph.
   * @param {string|null} [vector.NodeGUID=null] - Globally unique identifier for the node.
   * @param {string|null} [vector.EdgeGUID=null] - Globally unique identifier for the edge.
   * @param {string|null} [vector.Model=null] - Model associated with the vector.
   * @param {number} [vector.Dimensionality=0] - Dimensionality of the vector.
   * @param {string} [vector.Content=''] - Content of the vector.
   * @param {Array<number>} [vector.Vectors=[]] - List of float values representing the vector.
   * @param {Date|string} [vector.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   * @param {Date|string} [vector.LastUpdateUtc] - Last update timestamp in UTC (defaults to current UTC time).
   */
  constructor(vector = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = uuidV4(),
      GraphGUID = null,
      NodeGUID = null,
      EdgeGUID = null,
      Model = null,
      Dimensionality = 0,
      Content = '',
      Vectors = [],
      CreatedUtc = new Date().toISOString(),
      LastUpdateUtc = new Date().toISOString(),
    } = vector;

    this.GUID = GUID; // Unique identifier for the vector
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.GraphGUID = GraphGUID; // Unique identifier for the graph
    this.NodeGUID = NodeGUID; // Unique identifier for the node
    this.EdgeGUID = EdgeGUID; // Unique identifier for the edge
    this.Model = Model; // Model associated with the vector
    this.Dimensionality = Dimensionality; // Dimensionality of the vector
    this.Content = Content; // Content of the vector
    this.Vectors = Vectors; // List of float values representing the vector
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
    this.LastUpdateUtc = new Date(LastUpdateUtc); // Last update timestamp

    // Validate dimensionality
    this._validateDimensionality();
  }

  /**
   * Validates the dimensionality of the vector.
   * @private
   * @throws {RangeError} If the dimensionality is negative.
   */
  _validateDimensionality() {
    if (this.Dimensionality < 0) {
      throw new RangeError('Dimensionality must be greater than or equal to zero.');
    }
  }
}
