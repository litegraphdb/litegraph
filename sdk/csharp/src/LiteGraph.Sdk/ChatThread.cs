namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Chat thread.  A conversation owned by a user, optionally bound to a graph.
    /// </summary>
    public class ChatThread
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
        /// GUID of the user that owns the thread.
        /// </summary>
        public Guid UserGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// GUID of the graph the conversation explores.  Null when the thread is not bound to a graph.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Title.  Generated from the first exchange when not supplied.
        /// </summary>
        public string Title { get; set; } = null;

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

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatThread()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
