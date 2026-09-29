namespace Test.Shared
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Identifiers of the rows created for the tied-sort-value enumeration regression tests.
    /// </summary>
    internal class TiedOrderFixture
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.Empty;

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid GraphGUID { get; set; } = Guid.Empty;

        /// <summary>
        /// GUIDs of every node created with tied timestamps.
        /// </summary>
        public HashSet<Guid> NodeGUIDs { get; set; } = new HashSet<Guid>();

        /// <summary>
        /// GUIDs of every edge created with tied timestamps and costs.
        /// </summary>
        public HashSet<Guid> EdgeGUIDs { get; set; } = new HashSet<Guid>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
