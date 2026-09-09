namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result of a graph algorithm run.
    /// </summary>
    public class GraphAlgorithmResult
    {
        #region Public-Members

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
        /// Number of iterations performed by iterative algorithms (for example PageRank, label propagation).  Zero for non-iterative algorithms.
        /// </summary>
        public int Iterations { get; set; } = 0;

        /// <summary>
        /// Boolean indicating whether an iterative algorithm converged within the iteration limit.  True for non-iterative algorithms.
        /// </summary>
        public bool Converged { get; set; } = true;

        /// <summary>
        /// Number of distinct communities or components discovered.  Null for algorithms that do not partition the graph.
        /// </summary>
        public int? CommunityCount { get; set; } = null;

        /// <summary>
        /// Elapsed compute time in milliseconds (excludes graph load time).
        /// </summary>
        public double ComputeMs { get; set; } = 0d;

        /// <summary>
        /// Elapsed graph load time in milliseconds.
        /// </summary>
        public double LoadMs { get; set; } = 0d;

        /// <summary>
        /// Boolean indicating whether results were written back onto nodes.
        /// </summary>
        public bool WrittenBack { get; set; } = false;

        /// <summary>
        /// Property name under which results were written back, when applicable.
        /// </summary>
        public string WriteBackProperty { get; set; } = null;

        /// <summary>
        /// Per-node results, ordered as produced by the algorithm (typically descending by score, or by community).
        /// </summary>
        public List<GraphAlgorithmNodeResult> Nodes
        {
            get
            {
                return _Nodes;
            }
            set
            {
                _Nodes = value ?? new List<GraphAlgorithmNodeResult>();
            }
        }

        #endregion

        #region Private-Members

        private List<GraphAlgorithmNodeResult> _Nodes = new List<GraphAlgorithmNodeResult>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
