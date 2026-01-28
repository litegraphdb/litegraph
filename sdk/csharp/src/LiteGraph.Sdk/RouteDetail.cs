namespace LiteGraph.Sdk
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Total cost and ordered list of edges between two nodes.
    /// </summary>
    public class RouteDetail
    {
        #region Public-Members

        /// <summary>
        /// Total cost of the route.
        /// </summary>
        public int TotalCost
        {
            get
            {
                if (_Edges == null || _Edges.Count < 1) return 0;
                return _Edges.Sum(e => e.Cost);
            }
        }

        /// <summary>
        /// Edges.
        /// </summary>
        public List<Edge> Edges
        {
            get
            {
                return _Edges;
            }
            set
            {
                if (value == null) value = new List<Edge>();
                _Edges = value;
            }
        }

        #endregion

        #region Private-Members

        private List<Edge> _Edges = new List<Edge>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public RouteDetail()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
