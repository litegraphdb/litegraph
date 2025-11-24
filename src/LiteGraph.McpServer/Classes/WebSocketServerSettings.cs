namespace LiteGraph.McpServer.Classes
{
    /// <summary>
    /// WebSocket server settings.
    /// </summary>
    public class WebSocketServerSettings
    {
        #region Public-Members

        /// <summary>
        /// WebSocket server hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// WebSocket server port.
        /// </summary>
        public int Port { get; set; } = 8202;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServerSettings"/> class.
        /// </summary>
        public WebSocketServerSettings()
        {
        }

        #endregion
    }
}

