namespace LiteGraph.Sdk
{
    /// <summary>
    /// Request to generate node embeddings for a graph using the tenant's active embedding endpoint.
    /// </summary>
    public class GenerateEmbeddingsRequest
    {
        /// <summary>
        /// Optional maximum number of nodes to embed.  Null embeds all nodes.
        /// </summary>
        public int? MaxNodes { get; set; } = null;

        /// <summary>
        /// Boolean indicating whether nodes that already have a vector should be skipped.  Default true.
        /// </summary>
        public bool SkipNodesWithVectors { get; set; } = true;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GenerateEmbeddingsRequest()
        {
        }
    }
}
