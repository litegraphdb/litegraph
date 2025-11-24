namespace LiteGraph.McpServer.Classes
{
    using System;

    /// <summary>
    /// Node information settings.
    /// </summary>
    public class NodeSettings
    {
        #region Public-Members

        /// <summary>
        /// Node GUID.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Node name.
        /// </summary>
        public string Name { get; set; } = "LiteGraph Model Context Protocol Server";

        /// <summary>
        /// Node hostname.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Instance type.
        /// </summary>
        public string InstanceType { get; set; } = "McpServer";

        /// <summary>
        /// Last start timestamp.
        /// </summary>
        public DateTime LastStartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeSettings"/> class.
        /// </summary>
        public NodeSettings()
        {
        }

        #endregion
    }
}

