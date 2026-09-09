namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request to import externally computed per-node values back onto graph nodes.
    /// </summary>
    public class GraphAlgorithmImportRequest
    {
        /// <summary>
        /// Per-node values keyed by node GUID.  Each entry maps a property name to a numeric value written into the node's data.
        /// </summary>
        public Dictionary<Guid, Dictionary<string, double>> Values { get; set; } = new Dictionary<Guid, Dictionary<string, double>>();

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmImportRequest()
        {
        }
    }
}
