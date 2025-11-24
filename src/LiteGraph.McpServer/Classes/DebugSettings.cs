namespace LiteGraph.McpServer.Classes
{
    /// <summary>
    /// Debug settings.
    /// </summary>
    public class DebugSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable debug for exceptions.
        /// </summary>
        public bool Exceptions { get; set; } = true;

        /// <summary>
        /// Enable debug for requests.
        /// </summary>
        public bool Requests { get; set; } = false;

        /// <summary>
        /// Enable debug for database queries.
        /// </summary>
        public bool DatabaseQueries { get; set; } = false;

        /// <summary>
        /// Enable debug for MCP operations.
        /// </summary>
        public bool McpOperations { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugSettings"/> class.
        /// </summary>
        public DebugSettings()
        {
        }

        #endregion
    }
}

