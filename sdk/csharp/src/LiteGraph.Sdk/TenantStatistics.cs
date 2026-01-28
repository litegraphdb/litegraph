namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Tenant statistics.
    /// </summary>
    public class TenantStatistics
    {
        #region Public-Members

        /// <summary>
        /// Number of graphs.
        /// </summary>
        public int Graphs
        {
            get
            {
                return _Graphs;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Graphs));
                _Graphs = value;
            }
        }

        /// <summary>
        /// Number of nodes.
        /// </summary>
        public int Nodes
        {
            get
            {
                return _Nodes;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Nodes));
                _Nodes = value;
            }
        }

        /// <summary>
        /// Number of edges.
        /// </summary>
        public int Edges
        {
            get
            {
                return _Edges;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Edges));
                _Edges = value;
            }
        }

        /// <summary>
        /// Number of labels.
        /// </summary>
        public int Labels
        {
            get
            {
                return _Labels;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Labels));
                _Labels = value;
            }
        }

        /// <summary>
        /// Number of tags.
        /// </summary>
        public int Tags
        {
            get
            {
                return _Tags;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Tags));
                _Tags = value;
            }
        }

        /// <summary>
        /// Number of vectors.
        /// </summary>
        public int Vectors
        {
            get
            {
                return _Vectors;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Vectors));
                _Vectors = value;
            }
        }

        #endregion

        #region Private-Members

        private int _Graphs = 0;
        private int _Nodes = 0;
        private int _Edges = 0;
        private int _Labels = 0;
        private int _Tags = 0;
        private int _Vectors = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tenant statistics.
        /// </summary>
        public TenantStatistics()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}