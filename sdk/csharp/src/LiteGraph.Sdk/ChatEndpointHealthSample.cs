namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// One chat endpoint health probe result.
    /// </summary>
    public class ChatEndpointHealthSample
    {
        #region Public-Members

        /// <summary>
        /// Probe timestamp, in UTC.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the probe succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Probe duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatEndpointHealthSample()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
