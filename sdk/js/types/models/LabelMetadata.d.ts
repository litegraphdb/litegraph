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
    constructor(label?: {
        GUID?: string;
        TenantGUID?: string;
        GraphGUID?: string | null;
        NodeGUID?: string | null;
        EdgeGUID?: string | null;
        Label?: string;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: string;
    TenantGUID: string;
    GraphGUID: string;
    NodeGUID: string;
    EdgeGUID: string;
    Label: string;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
