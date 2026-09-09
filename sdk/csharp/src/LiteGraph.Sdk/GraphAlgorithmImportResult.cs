namespace LiteGraph.Sdk
{
    /// <summary>
    /// Result of importing externally computed algorithm results.
    /// </summary>
    public class GraphAlgorithmImportResult
    {
        /// <summary>
        /// Boolean indicating whether the import succeeded.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Number of nodes updated.
        /// </summary>
        public int NodesUpdated { get; set; } = 0;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmImportResult()
        {
        }
    }
}
