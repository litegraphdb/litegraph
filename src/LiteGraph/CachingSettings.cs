namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Caching;

    /// <summary>
    /// Caching settings.
    /// </summary>
    public class CachingSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable caching.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Maximum number of records to cache, per resource type.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _Capacity;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(Capacity));
                _Capacity = value;
            }
        }

        /// <summary>
        /// Number of records to evict when capacity pressure is encountered.
        /// It is recommended that this value be approximately 10% to 25% of the capacity.
        /// </summary>
        public int EvictCount
        {
            get
            {
                return _EvictCount;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(EvictCount));
                _EvictCount = value;
            }
        }

        #endregion

        #region Private-Members

        private int _Capacity = 1000;
        private int _EvictCount = 100;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Caching settings.
        /// </summary>
        public CachingSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
