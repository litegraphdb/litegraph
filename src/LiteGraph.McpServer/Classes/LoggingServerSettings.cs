namespace LiteGraph.McpServer.Classes
{
    /// <summary>
    /// Logging server configuration.
    /// </summary>
    public class LoggingServerSettings
    {
        #region Public-Members

        /// <summary>
        /// Logging server hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// Logging server port.
        /// </summary>
        public int Port { get; set; } = 514;

        /// <summary>
        /// Whether to randomize ports.
        /// </summary>
        public bool RandomizePorts { get; set; } = false;

        /// <summary>
        /// Minimum port for randomization.
        /// </summary>
        public int MinimumPort { get; set; } = 65000;

        /// <summary>
        /// Maximum port for randomization.
        /// </summary>
        public int MaximumPort { get; set; } = 65535;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingServerSettings"/> class.
        /// </summary>
        public LoggingServerSettings()
        {
        }

        #endregion
    }
}

