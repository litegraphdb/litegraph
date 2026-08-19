namespace LiteGraph
{
    /// <summary>
    /// Direction in which to traverse edges when extracting a subgraph.
    /// </summary>
    public enum GraphTraversalDirectionEnum
    {
        /// <summary>
        /// Follow edges that originate from the current frontier node (from-node to to-node).
        /// </summary>
        Outbound,
        /// <summary>
        /// Follow edges that terminate at the current frontier node (to-node from from-node).
        /// </summary>
        Inbound,
        /// <summary>
        /// Follow edges in either direction.  This is the default.
        /// </summary>
        Both
    }
}
