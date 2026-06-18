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

        /// <summary>
        /// Human-readable vector name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Classification labels.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Arbitrary key/value metadata.
        /// </summary>
        public Dictionary<string, object> Tags { get; set; } = null;

        /// <summary>
        /// HNSW neighbor connections keyed by layer.
        /// </summary>
        public Dictionary<int, List<Guid>> Connections { get; set; } = new Dictionary<int, List<Guid>>();
    }
}
