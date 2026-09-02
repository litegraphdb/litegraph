namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Timestamps;

    /// <summary>
    /// Object returned as the result of an enumeration.
    /// </summary>
    public class EnumerationResult<T>
    {
        #region Public-Members

        /// <summary>
        /// Indicates if the statistics operation was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Start and end timestamps.
        /// </summary>
        public Timestamp Timestamp { get; set; } = new Timestamp();

        /// <summary>
        /// Maximum number of results to retrieve.
        /// </summary>
        public int MaxResults
        {
            get
            {
                return _MaxResults;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Continuation token.
        /// </summary>
        public Guid? ContinuationToken { get; set; } = null;

        /// <summary>
        /// Boolean indicating end of results.
        /// </summary>
        public bool EndOfResults { get; set; } = true;

        /// <summary>
        /// Total number of records.  Never less than the number of objects in
        /// this page: the count and the page are read separately, so records
        /// inserted between the two under concurrency could otherwise make the
        /// reported total incoherent with the returned objects.
        /// </summary>
        public long TotalRecords
        {
            get
            {
                long pageCount = (_Objects != null ? _Objects.Count : 0);
                return (_TotalRecords < pageCount ? pageCount : _TotalRecords);
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(TotalRecords));
                _TotalRecords = value;
            }
        }

        /// <summary>
        /// Number of candidate records remaining in the enumeration.
        /// </summary>
        public long RecordsRemaining
        {
            get
            {
                return _RecordsRemaining;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(RecordsRemaining));
                _RecordsRemaining = value;
            }
        }

        /// <summary>
        /// Objects.
        /// </summary>
        [JsonPropertyOrder(999)]
        public List<T> Objects
        {
            get
            {
                return _Objects;
            }
            set
            {
                if (value == null) value = new List<T>();
                _Objects = value;
            }
        }

        #endregion

        #region Private-Members

        private int _MaxResults = 1000;
        private long _TotalRecords = 0;
        private long _RecordsRemaining = 0;
        private List<T> _Objects = new List<T>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates the object.
        /// </summary>
        public EnumerationResult()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion 
    }
}

