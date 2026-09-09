namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result of a graph algorithm run.
    /// </summary>
    public class GraphAlgorithmResult
    {
        /// <summary>
        /// Boolean indicating whether the run succeeded.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = default(Guid);

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid GraphGUID { get; set; } = default(Guid);

        /// <summary>
        /// Algorithm that produced this result.
        /// </summary>
        public GraphAlgorithmTypeEnum AlgorithmType { get; set; } = GraphAlgorithmTypeEnum.DegreeCentrality;

        /// <summary>
        /// Number of nodes processed.
        /// </summary>
        public int NodeCount { get; set; } = 0;

        /// <summary>
        /// Number of edges processed.
        /// </summary>
        public int EdgeCount { get; set; } = 0;

        /// <summary>
        /// Number of iterations performed by iterative algorithms.
        /// </summary>
        public int Iterations { get; set; } = 0;

        /// <summary>
        /// Boolean indicating whether an iterative algorithm converged within the iteration limit.
        /// </summary>
        public bool Converged { get; set; } = true;

        /// <summary>
        /// Number of distinct communities or components discovered.
        /// </summary>
        public int? CommunityCount { get; set; } = null;

        /// <summary>
        /// Elapsed compute time in milliseconds.
        /// </summary>
        public double ComputeMs { get; set; } = 0d;

        /// <summary>
        /// Elapsed graph load time in milliseconds.
        /// </summary>
        public double LoadMs { get; set; } = 0d;

        /// <summary>
        /// Boolean indicating whether this result was served from the algorithm result cache.
        /// </summary>
        public bool FromCache { get; set; } = false;

        /// <summary>
        /// Boolean indicating whether results were written back onto nodes.
        /// </summary>
        public bool WrittenBack { get; set; } = false;

        /// <summary>
        /// Property name under which results were written back, when applicable.
        /// </summary>
        public string WriteBackProperty { get; set; } = null;

        /// <summary>
        /// Per-node results.
        /// </summary>
        public List<GraphAlgorithmNodeResult> Nodes { get; set; } = new List<GraphAlgorithmNodeResult>();

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmResult()
        {
        }
    }
}
