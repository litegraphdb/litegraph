namespace LiteGraph.Server.Classes
{
    using System.Collections.Generic;
    using LiteGraph;

    /// <summary>
    /// Result of a tenant onboarding operation.  Contains the created tenant and any created users, credentials, and
    /// graphs.  User passwords are redacted.
    /// </summary>
    public class TenantOnboardResponse
    {
        #region Public-Members

        /// <summary>
        /// Created tenant.
        /// </summary>
        public TenantMetadata Tenant { get; set; } = null;

        /// <summary>
        /// Created users, with passwords redacted.
        /// </summary>
        public List<UserMaster> Users { get; set; } = new List<UserMaster>();

        /// <summary>
        /// Created credentials.
        /// </summary>
        public List<Credential> Credentials { get; set; } = new List<Credential>();

        /// <summary>
        /// Created graphs.
        /// </summary>
        public List<Graph> Graphs { get; set; } = new List<Graph>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TenantOnboardResponse()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
