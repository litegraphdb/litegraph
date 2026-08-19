namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using ExpressionTree;

    /// <summary>
    /// Request describing a filtered, directional subgraph to extract from a graph.
    /// The walk begins at one or more start nodes and proceeds breadth-first up to a maximum depth,
    /// following edges in the configured direction and honoring the supplied edge and node filters.
    /// </summary>
    public class SubgraphExtractionRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid GraphGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// GUIDs of the start nodes from which the walk begins.  At least one is required.
        /// Start nodes are always included in the result even if they do not satisfy the node filters.
        /// </summary>
        public List<Guid> StartNodeGUIDs { get; set; } = new List<Guid>();

        /// <summary>
        /// Maximum traversal depth from the start nodes.
        /// Default is 2.  Minimum is 0, where 0 returns only the start nodes and the edges among them.
        /// </summary>
        public int MaxDepth
        {
            get
            {
                return _MaxDepth;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxDepth));
                _MaxDepth = value;
            }
        }

        /// <summary>
        /// Direction in which edges are followed.  Default is Both.
        /// </summary>
        public GraphTraversalDirectionEnum Direction { get; set; } = GraphTraversalDirectionEnum.Both;

        /// <summary>
        /// Maximum number of nodes to include.  Default is 0 (unlimited).  Minimum is 0.
        /// </summary>
        public int MaxNodes
        {
            get
            {
                return _MaxNodes;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxNodes));
                _MaxNodes = value;
            }
        }

        /// <summary>
        /// Maximum number of edges to include.  Default is 0 (unlimited).  Minimum is 0.
        /// </summary>
        public int MaxEdges
        {
            get
            {
                return _MaxEdges;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxEdges));
                _MaxEdges = value;
            }
        }

        /// <summary>
        /// Labels an edge must carry to be traversed and included.  Null or empty applies no label filter.
        /// </summary>
        public List<string> EdgeLabels { get; set; } = null;

        /// <summary>
        /// Tags an edge must carry to be traversed and included.  Null applies no tag filter.
        /// </summary>
        public NameValueCollection EdgeTags { get; set; } = null;

        /// <summary>
        /// Expression filter applied to an edge's data object to decide traversal and inclusion.  Null applies no filter.
        /// </summary>
        public Expr EdgeFilter { get; set; } = null;

        /// <summary>
        /// Maximum cost an edge may have to be traversed.  Null applies no cost limit.  Minimum is 0.
        /// An edge whose cost exceeds this value is not traversed.
        /// </summary>
        public int? MaxEdgeCost
        {
            get
            {
                return _MaxEdgeCost;
            }
            set
            {
                if (value != null && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(MaxEdgeCost));
                _MaxEdgeCost = value;
            }
        }

        /// <summary>
        /// Labels a neighbor node must carry to be included and expanded.  Null or empty applies no label filter.
        /// Start nodes are exempt from this filter.
        /// </summary>
        public List<string> NodeLabels { get; set; } = null;

        /// <summary>
        /// Tags a neighbor node must carry to be included and expanded.  Null applies no tag filter.
        /// Start nodes are exempt from this filter.
        /// </summary>
        public NameValueCollection NodeTags { get; set; } = null;

        /// <summary>
        /// Expression filter applied to a neighbor node's data object to decide inclusion and expansion.  Null applies no filter.
        /// Start nodes are exempt from this filter.
        /// </summary>
        public Expr NodeFilter { get; set; } = null;

        /// <summary>
        /// True to include the data property of included graphs, nodes, and edges.  Default is false.
        /// </summary>
        public bool IncludeData { get; set; } = false;

        /// <summary>
        /// True to include subordinate properties (labels, tags, vectors) of included objects.  Default is false.
        /// </summary>
        public bool IncludeSubordinates { get; set; } = false;

        #endregion

        #region Private-Members

        private int _MaxDepth = 2;
        private int _MaxNodes = 0;
        private int _MaxEdges = 0;
        private int? _MaxEdgeCost = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SubgraphExtractionRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
