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
    constructor(vector?: {
        GUID?: string;
        TenantGUID?: string;
        GraphGUID?: string | null;
        NodeGUID?: string | null;
        EdgeGUID?: string | null;
        Model?: string | null;
        Dimensionality?: number;
        Content?: string;
        Vectors?: Array<number>;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: any;
    TenantGUID: any;
    GraphGUID: string;
    NodeGUID: string;
    EdgeGUID: string;
    Model: string;
    Dimensionality: number;
    Content: string;
    Vectors: number[];
    CreatedUtc: Date;
    LastUpdateUtc: Date;
    /**
     * Validates the dimensionality of the vector.
     * @private
     * @throws {RangeError} If the dimensionality is negative.
     */
    private _validateDimensionality;
}
