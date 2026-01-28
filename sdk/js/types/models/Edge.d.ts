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
    constructor(edge?: {
        GUID?: string;
        GraphGUID?: string;
        Name?: string;
        From?: string;
        To?: string;
        Cost?: number;
        CreatedUtc?: Date;
        Data?: any;
        LastUpdateUtc?: Date;
        Labels?: any[];
        Tags?: any;
        Vectors?: any[];
    });
    GUID: any;
    GraphGUID: any;
    name: string;
    from: any;
    to: any;
    cost: number;
    createdUtc: string | Date;
    data: any;
    lastUpdateUtc: string | Date;
    labels: any[];
    tags: any;
    vectors: any[];
}
