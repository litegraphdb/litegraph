namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Request to generate node embeddings for a graph using the tenant's active embedding endpoint.
    /// </summary>
    public class GenerateEmbeddingsRequest
    {
        #region Public-Members

        /// <summary>
        /// Optional maximum number of nodes to embed.  Null embeds all nodes.  Minimum 1 when set.
        /// </summary>
        public int? MaxNodes
        {
            get
            {
                return _MaxNodes;
            }
            set
            {
                if (value != null && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(MaxNodes));
                _MaxNodes = value;
            }
        }

        /// <summary>
        /// Boolean indicating whether nodes that already have a vector should be skipped.  Default true.
        /// </summary>
        public bool SkipNodesWithVectors { get; set; } = true;

        #endregion

        #region Private-Members

        private int? _MaxNodes = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GenerateEmbeddingsRequest()
        {

        }

        #endregion
    }
}
