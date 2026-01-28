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
    constructor(node?: {
        GUID: string;
        GraphGUID: string;
        Name: string;
        Data: any;
        CreatedUtc: Date;
        LastUpdateUtc: Date;
        Labels: string[];
        Tags: any;
        Vectors: [];
    });
    GUID: any;
    GraphGUID: any;
    name: string;
    data: any;
    createdUtc: Date;
    lastUpdateUtc: Date;
    labels: string[];
    tags: any;
    vectors: [];
}
