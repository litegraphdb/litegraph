namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// LiteGraph MCP Server settings.
    /// </summary>
    public class LiteGraphMcpServerSettings
    {
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
    }

    /// <summary>
    /// LiteGraph connection settings.
    /// </summary>
    public class LiteGraphSettings
    {
        /// <summary>
        /// REST API endpoint URL. If provided, connects to remote LiteGraph server.
        /// If null or empty, uses local repository.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// SQLite database filename for local repository. Used when Endpoint is null or empty.
        /// If null, uses in-memory database.
        /// </summary>
        public string? RepositoryFilename { get; set; }

        /// <summary>
        /// Whether to use in-memory database. Only used when RepositoryFilename is null.
        /// </summary>
        public bool InMemory { get; set; } = false;
    }

    /// <summary>
    /// HTTP server settings.
    /// </summary>
    public class HttpServerSettings
    {
        /// <summary>
        /// HTTP server hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// HTTP server port.
        /// </summary>
        public int Port { get; set; } = 8200;
    }

    /// <summary>
    /// TCP server settings.
    /// </summary>
    public class TcpServerSettings
    {
        /// <summary>
        /// Address on which to listen. Use 0.0.0.0 to indicate any IP address.
        /// </summary>
        public string Address
        {
            get
            {
                return _Address;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Address));
                if (!value.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    IPAddress.Parse(value).ToString();
                }
                _Address = value;
            }
        }

        /// <summary>
        /// TCP server port.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        private string _Address = "127.0.0.1";
        private int _Port = 8201;
    }

    /// <summary>
    /// WebSocket server settings.
    /// </summary>
    public class WebSocketServerSettings
    {
        /// <summary>
        /// WebSocket server hostname.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// WebSocket server port.
        /// </summary>
        public int Port { get; set; } = 8202;
    }

    /// <summary>
    /// Node information settings.
    /// </summary>
    public class NodeSettings
    {
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
    }

    /// <summary>
    /// Logging server configuration.
    /// </summary>
    public class LoggingServerSettings
    {
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
    }

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Logging servers.
        /// </summary>
        public List<LoggingServerSettings> Servers { get; set; } = new List<LoggingServerSettings>();

        /// <summary>
        /// Log directory.
        /// </summary>
        public string LogDirectory { get; set; } = "./logs/";

        /// <summary>
        /// Log filename.
        /// </summary>
        public string LogFilename { get; set; } = "litegraph-mcp.log";

        /// <summary>
        /// Enable console logging.
        /// </summary>
        public bool ConsoleLogging { get; set; } = true;

        /// <summary>
        /// Enable colors in console.
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Minimum severity level.
        /// </summary>
        public int MinimumSeverity { get; set; } = 0;
    }

    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        /// <summary>
        /// Backups directory.
        /// </summary>
        public string BackupsDirectory { get; set; } = "./backups/";

        /// <summary>
        /// Temp directory.
        /// </summary>
        public string TempDirectory { get; set; } = "./temp/";
    }

    /// <summary>
    /// Debug settings.
    /// </summary>
    public class DebugSettings
    {
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
    }
}

