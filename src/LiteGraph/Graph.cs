namespace LiteGraph
{
    using LiteGraph.Indexing.Vector;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Graph.
    /// </summary>
    public class Graph
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Globally-unique identifier.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Type of vector indexing to use.
        /// Default is None.
        /// </summary>
        public VectorIndexTypeEnum? VectorIndexType { get; set; } = VectorIndexTypeEnum.None;

        /// <summary>
        /// When vector indexing is enabled, the name of the file used to hold the index.
        /// </summary>
        public string VectorIndexFile { get; set; } = null;

        /// <summary>
        /// When vector indexing is enabled, the number of vectors required to use the index.
        /// Brute force computation is often faster than use of an index for smaller batches of vectors.
        /// Default is null.  When set, the minimum value is 1.
        /// </summary>
        public int? VectorIndexThreshold
        {
            get
            {
                return _VectorIndexThreshold;
            }
            set
            {
                if (value != null && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(VectorIndexThreshold));
                _VectorIndexThreshold = value;
            }
        }

        /// <summary>
        /// When vector indexing is enabled, the dimensionality of vectors that will be stored in this graph.
        /// Default is null.  When set, the minimum value is 1.
        /// </summary>
        public int? VectorDimensionality
        {
            get
            {
                return _VectorDimensionality;
            }
            set
            {
                if (value != null && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(VectorDimensionality));
                _VectorDimensionality = value;
            }
        }

        /// <summary>
        /// HNSW M parameter - number of bi-directional links created for each new element during construction.
        /// Higher values lead to better recall but higher memory consumption and slower insertion.
        /// Default is 16. Valid range is 2-100.
        /// </summary>
        public int? VectorIndexM
        {
            get
            {
                return _VectorIndexM;
            }
            set
            {
                if (value != null && (value.Value < 2 || value.Value > 100)) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexM), "M must be between 2 and 100.");
                _VectorIndexM = value;
            }
        }

        /// <summary>
        /// HNSW Ef parameter - size of the dynamic list used during k-NN search.
        /// Higher values lead to better recall but slower search.
        /// Default is 50. Valid range is k to 10000, where k is the number of nearest neighbors requested.
        /// </summary>
        public int? VectorIndexEf
        {
            get
            {
                return _VectorIndexEf;
            }
            set
            {
                if (value != null && (value.Value < 1 || value.Value > 10000)) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexEf), "Ef must be between 1 and 10000.");
                _VectorIndexEf = value;
            }
        }

        /// <summary>
        /// HNSW EfConstruction parameter - size of the dynamic list used during index construction.
        /// Higher values lead to better index quality but slower construction.
        /// Default is 200. Valid range is 1 to 10000.
        /// </summary>
        public int? VectorIndexEfConstruction
        {
            get
            {
                return _VectorIndexEfConstruction;
            }
            set
            {
                if (value != null && (value.Value < 1 || value.Value > 10000)) 
                    throw new ArgumentOutOfRangeException(nameof(VectorIndexEfConstruction), "EfConstruction must be between 1 and 10000");
                _VectorIndexEfConstruction = value;
            }
        }

        /// <summary>
        /// Timestamp from creation, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Labels.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Tags.
        /// </summary>
        public NameValueCollection Tags { get; set; } = null;

        /// <summary>
        /// Object data.
        /// </summary>
        public object Data { get; set; } = null;

        /// <summary>
        /// Vectors.
        /// </summary>
        public List<VectorMetadata> Vectors { get; set; } = null;

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
        public Graph()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
