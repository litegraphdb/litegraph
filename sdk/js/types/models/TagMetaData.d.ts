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
    constructor(tag?: {
        GUID?: string;
        TenantGUID?: string;
        GraphGUID?: string;
        NodeGUID?: string;
        EdgeGUID?: string;
        Key?: string;
        Value?: string;
        CreatedUtc?: Date | string;
        LastUpdateUtc?: Date | string;
    });
    GUID: any;
    TenantGUID: string;
    GraphGUID: string;
    NodeGUID: string;
    EdgeGUID: string;
    Key: string;
    Value: string;
    CreatedUtc: Date;
    LastUpdateUtc: Date;
}
