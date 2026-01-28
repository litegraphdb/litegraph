namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Statistics for a vector index.
    /// </summary>
    public class VectorIndexStatistics
    {
        /// <summary>
        /// Total number of vectors in the index.
        /// </summary>
        public int VectorCount { get; set; }

        /// <summary>
        /// Dimensionality of vectors in the index.
        /// </summary>
        public int Dimensions { get; set; }

        /// <summary>
        /// Type of index being used.
        /// </summary>
        public VectorIndexTypeEnum IndexType { get; set; }

        /// <summary>
        /// M parameter (number of bi-directional links per node).
        /// </summary>
        public int M { get; set; }

        /// <summary>
        /// EfConstruction parameter (size of dynamic candidate list during index construction).
        /// </summary>
        public int EfConstruction { get; set; }

        /// <summary>
        /// Default Ef parameter (size of dynamic candidate list during search).
        /// </summary>
        public int DefaultEf { get; set; }

        /// <summary>
        /// Index file path if using persistent storage.
        /// </summary>
        public string IndexFile { get; set; }

        /// <summary>
        /// Size of the index file in bytes (if applicable).
        /// </summary>
        public long? IndexFileSizeBytes { get; set; }

        /// <summary>
        /// Estimated memory usage in bytes.
        /// </summary>
        public long EstimatedMemoryBytes { get; set; }

        /// <summary>
        /// Last time the index was rebuilt.
        /// </summary>
        public DateTime? LastRebuildUtc { get; set; }

        /// <summary>
        /// Last time a vector was added.
        /// </summary>
        public DateTime? LastAddUtc { get; set; }

        /// <summary>
        /// Last time a vector was removed.
        /// </summary>
        public DateTime? LastRemoveUtc { get; set; }

        /// <summary>
        /// Last time a search was performed.
        /// </summary>
        public DateTime? LastSearchUtc { get; set; }

        /// <summary>
        /// Whether the index is currently loaded in memory.
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// Distance metric being used.
        /// </summary>
        public string DistanceMetric { get; set; }
    }
}