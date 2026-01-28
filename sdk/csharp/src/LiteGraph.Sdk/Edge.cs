namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Edge in the graph.
    /// </summary>
    public class Edge
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
        /// Globally-unique identifier of the from node.
        /// </summary>
        public Guid From { get; set; } = default(Guid);

        /// <summary>
        /// From node.  This property is only populated when retrieving routes.
        /// </summary>
        public Node FromNode { get; set; } = null;

        /// <summary>
        /// Globally-unique identifier of the to node.
        /// </summary>
        public Guid To { get; set; } = default(Guid);

        /// <summary>
        /// To node.  This property is only populated when retrieving routes.
        /// </summary>
        public Node ToNode { get; set; } = null;

        /// <summary>
        /// Cost.
        /// </summary>
        public int Cost
        {
            get
            {
                return _Cost;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Cost));
                _Cost = value;
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

        private int _Cost = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Edge()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
