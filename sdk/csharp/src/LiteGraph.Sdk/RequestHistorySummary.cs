namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request counts over a time range, bucketed by interval.
    /// </summary>
    public class RequestHistorySummary
    {
        #region Public-Members

        /// <summary>
        /// Start of the summarized range (UTC).
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End of the summarized range (UTC).
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bucket interval: minute, 15minute, hour, 6hour, or day.
        /// </summary>
        public string Interval { get; set; } = null;

        /// <summary>
        /// Successful requests in the range.
        /// </summary>
        public long TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Failed requests in the range.
        /// </summary>
        public long TotalFailure { get; set; } = 0;

        /// <summary>
        /// All requests in the range.
        /// </summary>
        public long TotalRequests { get; set; } = 0;

        /// <summary>
        /// Per-interval buckets.
        /// </summary>
        public List<RequestHistorySummaryBucket> Data { get; set; } = new List<RequestHistorySummaryBucket>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummary()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
