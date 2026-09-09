namespace LiteGraph.Sdk
{
    /// <summary>
    /// Graph algorithm type.
    /// </summary>
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
        /// Weakly connected components.
        /// </summary>
        WeaklyConnectedComponents,
        /// <summary>
        /// Strongly connected components.
        /// </summary>
        StronglyConnectedComponents,
        /// <summary>
        /// Label propagation community detection.
        /// </summary>
        LabelPropagation,
        /// <summary>
        /// Closeness centrality.
        /// </summary>
        ClosenessCentrality,
        /// <summary>
        /// Eigenvector centrality.
        /// </summary>
        EigenvectorCentrality
    }
}
