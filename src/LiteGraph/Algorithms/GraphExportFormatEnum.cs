namespace LiteGraph.Algorithms
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Graph projection export format for external computation (for example rustworkx or NetworkX).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GraphExportFormatEnum
    {
        /// <summary>
        /// Node-link JSON compatible with networkx.node_link_graph.
        /// </summary>
        NodeLinkJson,
        /// <summary>
        /// Edge list (CSV) with a header row: source,target,weight.
        /// </summary>
        EdgeList,
        /// <summary>
        /// GraphML XML.
        /// </summary>
        Graphml
    }
}
