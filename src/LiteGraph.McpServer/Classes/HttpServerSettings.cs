namespace LiteGraph.McpServer.Classes
{
    /// <summary>
    /// HTTP server settings.
    /// </summary>
    public class HttpServerSettings
    {
        #region Public-Members

        /// <summary>
        /// HTTP server hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// HTTP server port.
        /// </summary>
        public int Port { get; set; } = 8200;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServerSettings"/> class.
        /// </summary>
        public HttpServerSettings()
        {
        }

        #endregion
    }
}

