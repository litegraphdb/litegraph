namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a single node in the HNSW index.
    /// </summary>
    public class HnswNodeState
    {
        /// <summary>
        /// The unique identifier for the node.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The vector data for the node.
        /// </summary>
        public List<float> Vector { get; set; } = new List<float>();
    }
}
