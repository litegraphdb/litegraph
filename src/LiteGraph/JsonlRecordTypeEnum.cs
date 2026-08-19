namespace LiteGraph
{
    /// <summary>
    /// Type discriminator for a single JSONL record line.
    /// </summary>
    public enum JsonlRecordTypeEnum
    {
        /// <summary>
        /// The record payload is a graph.
        /// </summary>
        Graph,
        /// <summary>
        /// The record payload is a node.
        /// </summary>
        Node,
        /// <summary>
        /// The record payload is an edge.
        /// </summary>
        Edge
    }
}
