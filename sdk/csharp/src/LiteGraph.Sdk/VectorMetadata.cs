namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Vector metadata.
    /// </summary>
    public class VectorMetadata
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid GraphGUID { get; set; } = default(Guid);

        /// <summary>
        /// Node GUID.
        /// </summary>
        public Guid? NodeGUID { get; set; } = null;

        /// <summary>
        /// Edge GUID.
        /// </summary>
        public Guid? EdgeGUID { get; set; } = null;

        /// <summary>
        /// Model.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Dimensionality.
        /// </summary>
        public int Dimensionality
        {
            get
            {
                return _Dimensionality;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Dimensionality));
                _Dimensionality = value;
            }
        }

        /// <summary>
        /// Content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Content.
        /// </summary>
        public List<float> Vectors { get; set; } = null;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private int _Dimensionality = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public VectorMetadata()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}