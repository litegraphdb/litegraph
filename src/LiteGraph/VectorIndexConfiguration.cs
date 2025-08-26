namespace LiteGraph
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Configuration for vector indexing.
    /// </summary>
    public class VectorIndexConfiguration
    {
        #region Public-Members

        /// <summary>
        /// Type of vector indexing to use.
        /// </summary>
        [Required]
        public VectorIndexTypeEnum VectorIndexType { get; set; } = VectorIndexTypeEnum.HnswRam;

        /// <summary>
        /// When vector indexing is enabled, the name of the file used to hold the index.
        /// Required for SQLite-based indices.
        /// </summary>
        public string VectorIndexFile { get; set; } = null;

        /// <summary>
        /// When vector indexing is enabled, the number of vectors required to use the index.
        /// Brute force computation is often faster than use of an index for smaller batches of vectors.
        /// Default is null. When set, the minimum value is 1.
        /// </summary>
        public int? VectorIndexThreshold
        {
            get
            {
                return _VectorIndexThreshold;
            }
            set
            {
                if (value != null && value.Value < 1) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexThreshold), "VectorIndexThreshold must be greater than 0.");
                _VectorIndexThreshold = value;
            }
        }

        /// <summary>
        /// When vector indexing is enabled, the dimensionality of vectors that will be stored in this graph.
        /// Required for vector indexing. When set, the minimum value is 1.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "VectorDimensionality must be greater than 0.")]
        public int? VectorDimensionality
        {
            get
            {
                return _VectorDimensionality;
            }
            set
            {
                if (value != null && value.Value < 1) 
                    throw new ArgumentOutOfRangeException(nameof(VectorDimensionality), "VectorDimensionality must be greater than 0.");
                _VectorDimensionality = value;
            }
        }

        /// <summary>
        /// HNSW M parameter - number of bi-directional links created for each new element during construction.
        /// Higher values lead to better recall but higher memory consumption and slower insertion.
        /// Default is 16. Valid range is 2-100.
        /// </summary>
        [Range(2, 100, ErrorMessage = "VectorIndexM must be between 2 and 100.")]
        public int? VectorIndexM
        {
            get
            {
                return _VectorIndexM;
            }
            set
            {
                if (value != null && (value.Value < 2 || value.Value > 100)) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexM), "VectorIndexM must be between 2 and 100.");
                _VectorIndexM = value;
            }
        }

        /// <summary>
        /// HNSW Ef parameter - size of the dynamic list used during k-NN search.
        /// Higher values lead to better recall but slower search.
        /// Default is 50. Valid range is 1 to 10000.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "VectorIndexEf must be between 1 and 10000.")]
        public int? VectorIndexEf
        {
            get
            {
                return _VectorIndexEf;
            }
            set
            {
                if (value != null && (value.Value < 1 || value.Value > 10000)) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexEf), "VectorIndexEf must be between 1 and 10000.");
                _VectorIndexEf = value;
            }
        }

        /// <summary>
        /// HNSW EfConstruction parameter - size of the dynamic list used during index construction.
        /// Higher values lead to better index quality but slower construction.
        /// Default is 200. Valid range is 1 to 10000.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "VectorIndexEfConstruction must be between 1 and 10000.")]
        public int? VectorIndexEfConstruction
        {
            get
            {
                return _VectorIndexEfConstruction;
            }
            set
            {
                if (value != null && (value.Value < 1 || value.Value > 10000)) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexEfConstruction), "VectorIndexEfConstruction must be between 1 and 10000.");
                _VectorIndexEfConstruction = value;
            }
        }

        #endregion

        #region Private-Members

        private int? _VectorIndexThreshold = null;
        private int? _VectorDimensionality = null;
        private int? _VectorIndexM = 16;
        private int? _VectorIndexEf = 50;
        private int? _VectorIndexEfConstruction = 200;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public VectorIndexConfiguration()
        {
        }

        /// <summary>
        /// Instantiate the object from a Graph.
        /// </summary>
        /// <param name="graph">Graph object.</param>
        public VectorIndexConfiguration(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            VectorIndexType = graph.VectorIndexType ?? VectorIndexTypeEnum.None;
            VectorIndexFile = graph.VectorIndexFile;
            VectorIndexThreshold = graph.VectorIndexThreshold;
            VectorDimensionality = graph.VectorDimensionality;
            VectorIndexM = graph.VectorIndexM ?? _VectorIndexM;
            VectorIndexEf = graph.VectorIndexEf ?? _VectorIndexEf;
            VectorIndexEfConstruction = graph.VectorIndexEfConstruction ?? _VectorIndexEfConstruction;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Apply this configuration to a Graph object.
        /// </summary>
        /// <param name="graph">Graph to update.</param>
        public void ApplyToGraph(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            graph.VectorIndexType = VectorIndexType;
            graph.VectorIndexFile = VectorIndexFile;
            graph.VectorIndexThreshold = VectorIndexThreshold;
            graph.VectorDimensionality = VectorDimensionality;
            graph.VectorIndexM = VectorIndexM;
            graph.VectorIndexEf = VectorIndexEf;
            graph.VectorIndexEfConstruction = VectorIndexEfConstruction;
            graph.LastUpdateUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Validate the configuration.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = null;

            // Check if VectorIndexType is set to None
            if (VectorIndexType == VectorIndexTypeEnum.None)
            {
                errorMessage = "VectorIndexType cannot be None when configuring vector indexing.";
                return false;
            }

            // Check required fields
            if (!VectorDimensionality.HasValue || VectorDimensionality.Value < 1)
            {
                errorMessage = "VectorDimensionality is required and must be greater than 0.";
                return false;
            }

            // Check SQLite-specific requirements
            if (VectorIndexType == VectorIndexTypeEnum.HnswSqlite && string.IsNullOrWhiteSpace(VectorIndexFile))
            {
                errorMessage = "VectorIndexFile is required for SQLite-based vector indices.";
                return false;
            }

            // Validate ranges
            if (VectorIndexThreshold.HasValue && VectorIndexThreshold.Value < 1)
            {
                errorMessage = "VectorIndexThreshold must be greater than 0.";
                return false;
            }

            if (VectorIndexM.HasValue && (VectorIndexM.Value < 2 || VectorIndexM.Value > 100))
            {
                errorMessage = "VectorIndexM must be between 2 and 100.";
                return false;
            }

            if (VectorIndexEf.HasValue && (VectorIndexEf.Value < 1 || VectorIndexEf.Value > 10000))
            {
                errorMessage = "VectorIndexEf must be between 1 and 10000.";
                return false;
            }

            if (VectorIndexEfConstruction.HasValue && (VectorIndexEfConstruction.Value < 1 || VectorIndexEfConstruction.Value > 10000))
            {
                errorMessage = "VectorIndexEfConstruction must be between 1 and 10000.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create a configuration for disabling vector indexing.
        /// </summary>
        /// <returns>VectorIndexConfiguration with indexing disabled.</returns>
        public static VectorIndexConfiguration CreateDisabled()
        {
            return new VectorIndexConfiguration
            {
                VectorIndexType = VectorIndexTypeEnum.None,
                VectorIndexFile = null,
                VectorIndexThreshold = null,
                VectorDimensionality = null,
                VectorIndexM = null,
                VectorIndexEf = null,
                VectorIndexEfConstruction = null
            };
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}