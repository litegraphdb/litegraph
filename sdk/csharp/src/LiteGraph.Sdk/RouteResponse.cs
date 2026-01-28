namespace LiteGraph.Sdk
{
    using ExpressionTree;
    using LiteGraph.Sdk;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Timestamps;

    /// <summary>
    /// Route response.
    /// </summary>
    public class RouteResponse
    {
        #region Public-Members

        /// <summary>
        /// Timestamp.
        /// </summary>
        public Timestamp Timestamp
        {
            get
            {
                return _Timestamp;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Timestamp));
                _Timestamp = value;
            }
        }

        /// <summary>
        /// Routes.
        /// </summary>
        public List<RouteDetail> Routes
        {
            get
            {
                return _Routes;
            }
            set
            {
                if (value == null) value = new List<RouteDetail>();
                _Routes = value;
            }
        }

        #endregion

        #region Private-Members

        private Timestamp _Timestamp = new Timestamp();
        private List<RouteDetail> _Routes = new List<RouteDetail>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Route request.
        /// </summary>
        public RouteResponse()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
