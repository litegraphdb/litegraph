namespace LiteGraph.McpServer.Classes
{
    using System;

    /// <summary>
    /// LiteGraph MCP Server settings.
    /// </summary>
    public class LiteGraphMcpServerSettings
    {
        #region Public-Members

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Created by identifier.
        /// </summary>
        public string CreatedBy { get; set; } = "Setup";

        /// <summary>
        /// Deployment type.
        /// </summary>
        public string DeploymentType { get; set; } = "Private";

        /// <summary>
        /// Software version.
        /// </summary>
        public string SoftwareVersion { get; set; } = "v1.0.0";

        /// <summary>
        /// Node information.
        /// </summary>
        public NodeSettings Node { get; set; } = new NodeSettings();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging { get; set; } = new LoggingSettings();

        /// <summary>
        /// LiteGraph connection settings.
        /// </summary>
        public LiteGraphSettings LiteGraph { get; set; } = new LiteGraphSettings();

        /// <summary>
        /// HTTP server settings.
        /// </summary>
        public HttpServerSettings Http { get; set; } = new HttpServerSettings();

        /// <summary>
        /// TCP server settings.
        /// </summary>
        public TcpServerSettings Tcp { get; set; } = new TcpServerSettings();

        /// <summary>
        /// WebSocket server settings.
        /// </summary>
        public WebSocketServerSettings WebSocket { get; set; } = new WebSocketServerSettings();

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage { get; set; } = new StorageSettings();

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings Debug { get; set; } = new DebugSettings();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteGraphMcpServerSettings"/> class.
        /// </summary>
        public LiteGraphMcpServerSettings()
        {
        }

        #endregion
    }
}

