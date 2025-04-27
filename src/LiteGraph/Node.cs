namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Node in the graph.
    /// </summary>
    public class Node
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Globally-unique identifier.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Globally-unique identifier for the graph.
        /// </summary>
        public Guid GraphGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Number of edges connected to this node.
        /// </summary>
        public int? EdgesIn
        {
            get
            {
                return _EdgesIn;
            }
            set
            {
                if (value != null && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(EdgesIn));
                _EdgesIn = value;
            }
        }

        /// <summary>
        /// Number of edges connected from this node.
        /// </summary>
        public int? EdgesOut
        {
            get
            {
                return _EdgesOut;
            }
            set
            {
                if (value != null && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(EdgesOut));
                _EdgesOut = value;
            }
        }

        /// <summary>
        /// Number of total edges to or from this node.
        /// </summary>
        public int? EdgesTotal
        {
            get
            {
                int? total = null;
                if (EdgesIn != null || EdgesOut != null) total = 0;
                if (EdgesIn != null) total += EdgesIn.Value;
                if (EdgesOut != null) total += EdgesOut.Value;
                return total;
            }
        }

        /// <summary>
        /// Timestamp from creation, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Labels.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Tags.
        /// </summary>
        public NameValueCollection Tags { get; set; } = null;

        /// <summary>
        /// Object data.
        /// </summary>
        public object Data { get; set; } = null;

        /// <summary>
        /// Vectors.
        /// </summary>
        public List<VectorMetadata> Vectors { get; set; } = null;

        #endregion

        #region Private-Members

        private int? _EdgesIn = null;
        private int? _EdgesOut = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Node()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
