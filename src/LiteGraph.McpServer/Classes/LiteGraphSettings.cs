namespace LiteGraph.McpServer.Classes
{
    /// <summary>
    /// LiteGraph connection settings.
    /// </summary>
    public class LiteGraphSettings
    {
        #region Public-Members

        /// <summary>
        /// REST API endpoint URL for remote LiteGraph server.
        /// </summary>
        public string? Endpoint { get; set; } = "http://localhost:8701";

        /// <summary>
        /// API key for authentication with LiteGraph server.
        /// </summary>
        public string? ApiKey { get; set; } = "litegraphadmin";

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteGraphSettings"/> class.
        /// </summary>
        public LiteGraphSettings()
        {
        }

        #endregion
    }
}

