namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the state of an HNSW index for serialization/deserialization.
    /// </summary>
    public class HnswIndexState
    {
        /// <summary>
        /// Current LiteGraph HNSW persistence format version.
        /// </summary>
        public const int CurrentFormatVersion = 2;

        /// <summary>
        /// LiteGraph HNSW persistence format version.
        /// </summary>
        public int FormatVersion { get; set; } = CurrentFormatVersion;

        /// <summary>
        /// HnswLite package version used when the index state was written.
        /// </summary>
        public string HnswLiteVersion { get; set; } = "2.0.1";

        /// <summary>
        /// The entry point node GUID for the HNSW index.
        /// </summary>
        public Guid? EntryPoint { get; set; }

        /// <summary>
        /// The total number of nodes in the index.
        /// </summary>
        public int NodeCount { get; set; }

        /// <summary>
        /// Timestamp when the index state was last saved.
        /// </summary>
        public DateTime LastSaved { get; set; }

        /// <summary>
        /// Collection of nodes in the index.
        /// </summary>
        public List<HnswNodeState> Node { get; set; } = new List<HnswNodeState>();
    }
}
