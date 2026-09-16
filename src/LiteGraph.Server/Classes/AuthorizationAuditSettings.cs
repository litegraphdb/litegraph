namespace LiteGraph.Server.Classes
{
    /// <summary>
    /// Settings controlling authorization audit records.
    /// </summary>
    public class AuthorizationAuditSettings
    {
        #region Public-Members

        /// <summary>
        /// Master switch for writing authorization audit records.  Default is true.  When false, neither denied nor permitted authorization events are recorded.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Boolean indicating whether permitted privileged actions (requests requiring write or admin scope that were authorized) are recorded, in addition to denials.  Default is true.  Read-scope requests are never audited.
        /// </summary>
        public bool AuditSuccessfulActions { get; set; } = true;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationAuditSettings()
        {

        }

        #endregion
    }
}
