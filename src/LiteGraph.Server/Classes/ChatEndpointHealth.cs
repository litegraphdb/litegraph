namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Chat endpoint health status.
    /// </summary>
    public class ChatEndpointHealth
    {
        #region Public-Members

        /// <summary>
        /// Endpoint GUID.
        /// </summary>
        public Guid EndpointGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Endpoint name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Endpoint type.
        /// </summary>
        public ChatEndpointTypeEnum EndpointType { get; set; } = ChatEndpointTypeEnum.Completion;

        /// <summary>
        /// Whether the endpoint is being monitored.  False when health checks are disabled or the endpoint is inactive.
        /// </summary>
        public bool Monitored { get; set; } = false;

        /// <summary>
        /// Whether the endpoint is healthy.  Null while monitoring has not yet reached a verdict.
        /// </summary>
        public bool? Healthy { get; set; } = null;

        /// <summary>
        /// Timestamp of the most recent probe, in UTC.  Null when no probe has run.
        /// </summary>
        public DateTime? LastCheckedUtc { get; set; } = null;

        /// <summary>
        /// Most recent probe error.  Null when the last probe succeeded.
        /// </summary>
        public string LastError { get; set; } = null;

        /// <summary>
        /// Consecutive successful probes.
        /// </summary>
        public int ConsecutiveSuccesses { get; set; } = 0;

        /// <summary>
        /// Consecutive failed probes.
        /// </summary>
        public int ConsecutiveFailures { get; set; } = 0;

        /// <summary>
        /// Percentage of successful probes over the retained history window.  Null when no probes have run.
        /// </summary>
        public double? UptimePercentage { get; set; } = null;

        /// <summary>
        /// Rolling probe history, oldest first.  Retained for 24 hours.
        /// </summary>
        public List<ChatEndpointHealthSample> CheckHistory { get; set; } = new List<ChatEndpointHealthSample>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatEndpointHealth()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
