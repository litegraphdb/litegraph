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
        public string SoftwareVersion { get; set; } = "v10.2.0";

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
        /// Observability settings.
        /// </summary>
        public ObservabilitySettings Observability { get; set; } = new ObservabilitySettings();

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage { get; set; } = new StorageSettings();

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings Debug { get; set; } = new DebugSettings();

        /// <summary>
        /// Maximum MCP tool calls per second per client, applied on the HTTP, TCP, and WebSocket transports.
        /// Default is 0, which disables the limit (LiteGraph server authentication and request timeouts still apply).
        /// Minimum is 0, maximum is 1000000.  A positive value makes calls over the limit return a tool result
        /// with isError set to true instead of running.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set outside 0 to 1000000.</exception>
        public int ToolCallsPerSecond
        {
            get
            {
                return _ToolCallsPerSecond;
            }
            set
            {
                if (value < 0 || value > 1000000) throw new ArgumentOutOfRangeException(nameof(ToolCallsPerSecond), "ToolCallsPerSecond must be between 0 and 1000000.");
                _ToolCallsPerSecond = value;
            }
        }

        #endregion

        #region Private-Members

        private int _ToolCallsPerSecond = 0;

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
