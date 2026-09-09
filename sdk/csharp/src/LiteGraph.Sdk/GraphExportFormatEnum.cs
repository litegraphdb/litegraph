namespace LiteGraph.Sdk
{
    /// <summary>
    /// Graph projection export format for external computation (for example rustworkx or NetworkX).
    /// </summary>
    public enum GraphExportFormatEnum
    {
        /// <summary>
        /// Node-link JSON compatible with networkx.node_link_graph.
        /// </summary>
        NodeLinkJson,
        /// <summary>
        /// Edge list (CSV).
        /// </summary>
        EdgeList,
        /// <summary>
        /// GraphML XML.
        /// </summary>
        Graphml
    }
}
