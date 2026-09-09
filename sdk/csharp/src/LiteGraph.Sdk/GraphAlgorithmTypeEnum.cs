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
        EigenvectorCentrality,
        /// <summary>
        /// Betweenness centrality.
        /// </summary>
        BetweennessCentrality,
        /// <summary>
        /// Louvain modularity community detection.
        /// </summary>
        Louvain,
        /// <summary>
        /// Local clustering coefficient.
        /// </summary>
        ClusteringCoefficient,
        /// <summary>
        /// k-core decomposition.
        /// </summary>
        KCore
    }
}
