namespace LiteGraph.Algorithms
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Graph algorithm type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GraphAlgorithmTypeEnum
    {
        /// <summary>
        /// Degree centrality (in, out, and total edge counts per node).
        /// </summary>
        DegreeCentrality,
        /// <summary>
        /// PageRank.
        /// </summary>
        PageRank,
        /// <summary>
        /// Weakly connected components (treats edges as undirected for connectivity).
        /// </summary>
        WeaklyConnectedComponents,
        /// <summary>
        /// Strongly connected components (directed; Tarjan's algorithm).
        /// </summary>
        StronglyConnectedComponents,
        /// <summary>
        /// Label propagation community detection.
        /// </summary>
        LabelPropagation
    }
}
