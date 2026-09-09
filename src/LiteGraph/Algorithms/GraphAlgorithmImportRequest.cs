namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request to import externally computed per-node values back onto graph nodes.
    /// Values are written into each node's JSON data under their property names.  Requires write permission at the REST/MCP boundary.
    /// </summary>
    public class GraphAlgorithmImportRequest
    {
        #region Public-Members

        /// <summary>
        /// Per-node values keyed by node GUID.  Each entry maps a property name to a numeric value written into the node's data.
        /// </summary>
        public Dictionary<Guid, Dictionary<string, double>> Values
        {
            get
            {
                return _Values;
            }
            set
            {
                _Values = value ?? new Dictionary<Guid, Dictionary<string, double>>();
            }
        }

        #endregion

        #region Private-Members

        private Dictionary<Guid, Dictionary<string, double>> _Values = new Dictionary<Guid, Dictionary<string, double>>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmImportRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
