namespace LiteGraph.Sdk
{
    /// <summary>
    /// Request to run a graph algorithm over a single graph.
    /// </summary>
    public class GraphAlgorithmRequest
    {
        /// <summary>
        /// Algorithm to run.
        /// </summary>
        public GraphAlgorithmTypeEnum AlgorithmType { get; set; } = GraphAlgorithmTypeEnum.DegreeCentrality;

        /// <summary>
        /// PageRank damping factor (0.0 - 1.0).
        /// </summary>
        public double DampingFactor { get; set; } = 0.85d;

        /// <summary>
        /// Maximum iterations for iterative algorithms.
        /// </summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>
        /// Convergence tolerance for iterative algorithms.
        /// </summary>
        public double Tolerance { get; set; } = 0.000001d;

        /// <summary>
        /// Boolean indicating whether edges should be treated as undirected where applicable.
        /// </summary>
        public bool TreatAsUndirected { get; set; } = false;

        /// <summary>
        /// Optional maximum number of per-node results to return.
        /// </summary>
        public int? MaxResults { get; set; } = null;

        /// <summary>
        /// Boolean indicating whether per-node results should be written back into node data.  Requires write permission.
        /// </summary>
        public bool WriteBack { get; set; } = false;

        /// <summary>
        /// Property name under which results are written when WriteBack is true.  Null uses the algorithm default.
        /// </summary>
        public string WriteBackProperty { get; set; } = null;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmRequest()
        {
        }
    }
}
