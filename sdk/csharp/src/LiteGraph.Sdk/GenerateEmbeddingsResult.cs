namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Result of a node embedding generation run.
    /// </summary>
    public class GenerateEmbeddingsResult
    {
        /// <summary>
        /// Boolean indicating whether the run succeeded.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Number of nodes embedded.
        /// </summary>
        public int NodesEmbedded { get; set; } = 0;

        /// <summary>
        /// Number of nodes skipped (empty content or already vectorized).
        /// </summary>
        public int NodesSkipped { get; set; } = 0;

        /// <summary>
        /// Embedding model used.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Embedding dimensionality produced.
        /// </summary>
        public int Dimensionality { get; set; } = 0;

        /// <summary>
        /// GUID of the embedding endpoint used.
        /// </summary>
        public Guid EndpointGUID { get; set; } = default(Guid);

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GenerateEmbeddingsResult()
        {
        }
    }
}
