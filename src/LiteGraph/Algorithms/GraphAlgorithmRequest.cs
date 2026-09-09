namespace LiteGraph.Algorithms
{
    using System;

    /// <summary>
    /// Request to run a graph algorithm over a single graph.
    /// </summary>
    public class GraphAlgorithmRequest
    {
        #region Public-Members

        /// <summary>
        /// Algorithm to run.
        /// </summary>
        public GraphAlgorithmTypeEnum AlgorithmType { get; set; } = GraphAlgorithmTypeEnum.DegreeCentrality;

        /// <summary>
        /// PageRank damping factor.  Minimum 0.0, maximum 1.0, default 0.85.
        /// </summary>
        public double DampingFactor
        {
            get
            {
                return _DampingFactor;
            }
            set
            {
                if (value < 0d || value > 1d) throw new ArgumentOutOfRangeException(nameof(DampingFactor));
                _DampingFactor = value;
            }
        }

        /// <summary>
        /// Maximum number of iterations for iterative algorithms (PageRank, label propagation).  Minimum 1, default 100.
        /// </summary>
        public int MaxIterations
        {
            get
            {
                return _MaxIterations;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxIterations));
                _MaxIterations = value;
            }
        }

        /// <summary>
        /// Convergence tolerance for iterative algorithms.  Minimum 0.0, default 0.000001.
        /// </summary>
        public double Tolerance
        {
            get
            {
                return _Tolerance;
            }
            set
            {
                if (value < 0d) throw new ArgumentOutOfRangeException(nameof(Tolerance));
                _Tolerance = value;
            }
        }

        /// <summary>
        /// Boolean indicating whether the algorithm should treat edges as undirected.  Applies to algorithms where directionality is meaningful (for example label propagation).  Weakly connected components always treat edges as undirected; strongly connected components always treat edges as directed.
        /// </summary>
        public bool TreatAsUndirected { get; set; } = false;

        /// <summary>
        /// Optional maximum number of per-node results to return.  Null returns all nodes.  Minimum 1 when set.
        /// </summary>
        public int? MaxResults
        {
            get
            {
                return _MaxResults;
            }
            set
            {
                if (value != null && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Boolean indicating whether per-node results should be written back into node data.  Requires write permission at the REST/MCP boundary.
        /// </summary>
        public bool WriteBack { get; set; } = false;

        /// <summary>
        /// Property name under which results are written into node data when <see cref="WriteBack"/> is true.  Null uses the algorithm's default property name.
        /// </summary>
        public string WriteBackProperty { get; set; } = null;

        #endregion

        #region Private-Members

        private double _DampingFactor = 0.85d;
        private int _MaxIterations = 100;
        private double _Tolerance = 0.000001d;
        private int? _MaxResults = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
