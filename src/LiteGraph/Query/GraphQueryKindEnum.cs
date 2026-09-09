namespace LiteGraph.Query
{
    /// <summary>
    /// Supported native graph query kinds.
    /// </summary>
    public enum GraphQueryKindEnum
    {
        /// <summary>
        /// Match nodes.
        /// </summary>
        MatchNode,

        /// <summary>
        /// Match edges and adjacent nodes.
        /// </summary>
        MatchEdge,

        /// <summary>
        /// Match a directed multi-hop path.
        /// </summary>
        MatchPath,

        /// <summary>
        /// Create a node.
        /// </summary>
        CreateNode,

        /// <summary>
        /// Create an edge.
        /// </summary>
        CreateEdge,

        /// <summary>
        /// Create a label.
        /// </summary>
        CreateLabel,

        /// <summary>
        /// Create a tag.
        /// </summary>
        CreateTag,

        /// <summary>
        /// Create a vector.
        /// </summary>
        CreateVector,

        /// <summary>
        /// Update a matched node.
        /// </summary>
        UpdateNode,

        /// <summary>
        /// Update a matched edge.
        /// </summary>
        UpdateEdge,

        /// <summary>
        /// Delete a matched node.
        /// </summary>
        DeleteNode,

        /// <summary>
        /// Delete a matched edge.
        /// </summary>
        DeleteEdge,

        /// <summary>
        /// Update a matched label.
        /// </summary>
        UpdateLabel,

        /// <summary>
        /// Update a matched tag.
        /// </summary>
        UpdateTag,

        /// <summary>
        /// Update a matched vector.
        /// </summary>
        UpdateVector,

        /// <summary>
        /// Delete a matched label.
        /// </summary>
        DeleteLabel,

        /// <summary>
        /// Delete a matched tag.
        /// </summary>
        DeleteTag,

        /// <summary>
        /// Delete a matched vector.
        /// </summary>
        DeleteVector,

        /// <summary>
        /// Search vectors.
        /// </summary>
        VectorSearch,

        /// <summary>
        /// Run a graph algorithm.
        /// </summary>
        Algorithm
    }
}
