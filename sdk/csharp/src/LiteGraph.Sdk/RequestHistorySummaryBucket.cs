namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Request counts for one interval of a request history summary.
    /// </summary>
    public class RequestHistorySummaryBucket
    {
        #region Public-Members

        /// <summary>
        /// Start of the interval (UTC).
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Successful requests in the interval.
        /// </summary>
        public long SuccessCount { get; set; } = 0;

        /// <summary>
        /// Failed requests in the interval.
        /// </summary>
        public long FailureCount { get; set; } = 0;

        /// <summary>
        /// All requests in the interval.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummaryBucket()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
