namespace LiteGraph.McpServer
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Serialization;
    using Voltaic;

    /// <summary>
    /// LiteGraph MCP Server - Exposes LiteGraph operations via Model Context Protocol.
    /// Supports document processing, vector storage retrieval, graph storage retrieval, and more.
    /// </summary>
    public class LiteGraphMcpServer : IDisposable
    {
        #region Private-Members

        private bool _Disposed = false;

        private string _HttpHostname;
        private string _TcpAddress;
        private string _TcpAddressDisplay;
        private string _WebSocketHostname;
        private int _HttpPort;
        private int _TcpPort;
        private int _WebSocketPort;

        private LiteGraphClient? _Client = null;
        private McpHttpServer? _HttpServer = null;
        private McpTcpServer? _TcpServer = null;
        private McpWebsocketsServer? _WebsocketServer = null;

        private static string _Header = "[LiteGraph.McpServer] ";
        private static string _SoftwareVersion = Constants.Version;
        private static int _ProcessId = Environment.ProcessId;
        private static bool _ShowConfiguration = false;

        private static Serializer _Serializer = new Serializer();
        private static LiteGraphMcpServerSettings _Settings = new LiteGraphMcpServerSettings();
        private static LiteGraphClient? _McpClient = null;
        private static McpHttpServer? _McpHttpServer = null;
        private static McpTcpServer? _McpTcpServer = null;
        private static McpWebsocketsServer? _McpWebsocketServer = null;
        private static Task? _McpHttpServerTask = null;
        private static Task? _McpTcpServerTask = null;
        private static Task? _McpWebsocketServerTask = null;

        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static CancellationToken _Token;

        #endregion

        #region Public-Members

        /// <summary>
        /// Gets the LiteGraph client instance.
        /// </summary>
        public LiteGraphClient Client => _Client!;

        /// <summary>
        /// Occurs when a log message is generated.
        /// </summary>
        public event EventHandler<string>? Log;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LiteGraphMcpServer class with an in-memory database.
        /// </summary>
        /// <param name="httpHostname">HTTP server hostname. Default is "127.0.0.1".</param>
        /// <param name="httpPort">HTTP server port. Default is 8200.</param>
        /// <param name="tcpHostname">TCP server hostname. Default is "127.0.0.1".</param>
        /// <param name="tcpPort">TCP server port. Default is 8201.</param>
        /// <param name="websocketHostname">WebSocket server hostname. Default is "127.0.0.1".</param>
        /// <param name="websocketPort">WebSocket server port. Default is 8202.</param>
        public LiteGraphMcpServer(
            string httpHostname = "127.0.0.1",
            int httpPort = 8200,
            string tcpHostname = "127.0.0.1",
            int tcpPort = 8201,
            string websocketHostname = "127.0.0.1",
            int websocketPort = 8202)
            : this(repositoryFilename: null, httpHostname, httpPort, tcpHostname, tcpPort, websocketHostname, websocketPort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the LiteGraphMcpServer class with a repository.
        /// </summary>
        /// <param name="repositoryFilename">SQLite database filename. If null, uses in-memory database.</param>
        /// <param name="httpHostname">HTTP server hostname. Default is "127.0.0.1".</param>
        /// <param name="httpPort">HTTP server port. Default is 8200.</param>
        /// <param name="tcpHostname">TCP server hostname. Default is "127.0.0.1".</param>
        /// <param name="tcpPort">TCP server port. Default is 8201.</param>
        /// <param name="websocketHostname">WebSocket server hostname. Default is "127.0.0.1".</param>
        /// <param name="websocketPort">WebSocket server port. Default is 8202.</param>
        public LiteGraphMcpServer(
            string? repositoryFilename,
            string httpHostname = "127.0.0.1",
            int httpPort = 8200,
            string tcpHostname = "127.0.0.1",
            int tcpPort = 8201,
            string websocketHostname = "127.0.0.1",
            int websocketPort = 8202)
        {
            SqliteGraphRepository repo = repositoryFilename == null
                ? new SqliteGraphRepository("litegraph.memory", inMemory: true)
                : new SqliteGraphRepository(repositoryFilename, inMemory: false);
            _Client = new LiteGraphClient(repo);
            _Client.InitializeRepository();

            _HttpHostname = httpHostname;
            _HttpPort = httpPort;
            _TcpAddressDisplay = tcpHostname;
            _TcpAddress = tcpHostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : tcpHostname;
            _TcpPort = tcpPort;
            _WebSocketHostname = websocketHostname;
            _WebSocketPort = websocketPort;

            _HttpServer = new McpHttpServer(_HttpHostname, _HttpPort, "/rpc", "/events", includeDefaultMethods: true);
            _TcpServer = new McpTcpServer(IPAddress.Parse(_TcpAddress), _TcpPort, includeDefaultMethods: true);
            _WebsocketServer = new McpWebsocketsServer(_WebSocketHostname, _WebSocketPort, "/mcp", includeDefaultMethods: true);

            _HttpServer.ServerName = "LiteGraph.McpServer";
            _HttpServer.ServerVersion = "1.0.0";
            _TcpServer.ServerName = "LiteGraph.McpServer";
            _TcpServer.ServerVersion = "1.0.0";
            _WebsocketServer.ServerName = "LiteGraph.McpServer";
            _WebsocketServer.ServerVersion = "1.0.0";

            _HttpServer.Log += (sender, msg) => Log?.Invoke(this, $"[HTTP] {msg}");
            _TcpServer.Log += (sender, msg) => Log?.Invoke(this, $"[TCP] {msg}");
            _WebsocketServer.Log += (sender, msg) => Log?.Invoke(this, $"[WS] {msg}");

            RegisterTools();
        }

        /// <summary>
        /// Initializes a new instance of the LiteGraphMcpServer class with a REST API endpoint.
        /// </summary>
        /// <param name="endpoint">Base URL of the LiteGraph REST API server.</param>
        /// <param name="httpHostname">HTTP server hostname. Default is "127.0.0.1".</param>
        /// <param name="httpPort">HTTP server port. Default is 8200.</param>
        /// <param name="tcpHostname">TCP server hostname. Default is "127.0.0.1".</param>
        /// <param name="tcpPort">TCP server port. Default is 8201.</param>
        /// <param name="websocketHostname">WebSocket server hostname. Default is "127.0.0.1".</param>
        /// <param name="websocketPort">WebSocket server port. Default is 8202.</param>
        public LiteGraphMcpServer(
            string endpoint,
            string? httpHostname = null,
            int? httpPort = null,
            string? tcpHostname = null,
            int? tcpPort = null,
            string? websocketHostname = null,
            int? websocketPort = null)
        {
            _Client = new LiteGraphClient(endpoint);

            _HttpHostname = httpHostname ?? "127.0.0.1";
            _HttpPort = httpPort ?? 8200;
            string tcpHost = tcpHostname ?? "127.0.0.1";
            _TcpAddressDisplay = tcpHost;
            _TcpAddress = tcpHost.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : tcpHost;
            _TcpPort = tcpPort ?? 8201;
            _WebSocketHostname = websocketHostname ?? "127.0.0.1";
            _WebSocketPort = websocketPort ?? 8202;

            _HttpServer = new McpHttpServer(_HttpHostname, _HttpPort, "/rpc", "/events", includeDefaultMethods: true);
            _TcpServer = new McpTcpServer(IPAddress.Parse(_TcpAddress), _TcpPort, includeDefaultMethods: true);
            _WebsocketServer = new McpWebsocketsServer(_WebSocketHostname, _WebSocketPort, "/mcp", includeDefaultMethods: true);

            _HttpServer.ServerName = "LiteGraph.McpServer";
            _HttpServer.ServerVersion = "1.0.0";
            _TcpServer.ServerName = "LiteGraph.McpServer";
            _TcpServer.ServerVersion = "1.0.0";
            _WebsocketServer.ServerName = "LiteGraph.McpServer";
            _WebsocketServer.ServerVersion = "1.0.0";

            _HttpServer.Log += (sender, msg) => Log?.Invoke(this, $"[HTTP] {msg}");
            _TcpServer.Log += (sender, msg) => Log?.Invoke(this, $"[TCP] {msg}");
            _WebsocketServer.Log += (sender, msg) => Log?.Invoke(this, $"[WS] {msg}");

            RegisterTools();
        }

        #endregion

        #region Entrypoint

        /// <summary>
        /// Main.
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            Welcome();
            ParseArguments(args);
            InitializeSettings();
            InitializeGlobals();

            Console.WriteLine(_Header + "starting at " + DateTime.UtcNow + " using process ID " + _ProcessId);

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine(_Header + "termination signal received");
                waitHandle.Set();
                eventArgs.Cancel = true;
            };

            bool waitHandleSignal = false;
            do
            {
                waitHandleSignal = waitHandle.WaitOne(1000);
            }
            while (!waitHandleSignal);

            Console.WriteLine(_Header + "stopping at " + DateTime.UtcNow);

            _McpHttpServer?.Stop();
            _McpTcpServer?.Stop();
            _McpWebsocketServer?.Stop();
            _McpHttpServer?.Dispose();
            _McpTcpServer?.Dispose();
            _McpWebsocketServer?.Dispose();
            _McpClient?.Dispose();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Starts all MCP servers.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task StartAsync(CancellationToken token = default)
        {
            Log?.Invoke(this, "Starting LiteGraph MCP servers...");

            if (_HttpServer == null || _TcpServer == null || _WebsocketServer == null)
                throw new InvalidOperationException("Servers have not been initialized");

            Task httpTask = _HttpServer.StartAsync(token);
            Task tcpTask = _TcpServer.StartAsync(token);
            Task wsTask = _WebsocketServer.StartAsync(token);

            Log?.Invoke(this, $"HTTP Server:       http://{_HttpHostname}:{_HttpPort}/rpc");
            Log?.Invoke(this, $"TCP Server:        tcp://{_TcpAddressDisplay}:{_TcpPort}");
            Log?.Invoke(this, $"WebSocket Server:  ws://{_WebSocketHostname}:{_WebSocketPort}/mcp");

            await Task.WhenAll(httpTask, tcpTask, wsTask);
        }

        /// <summary>
        /// Stops all MCP servers.
        /// </summary>
        public void Stop()
        {
            _HttpServer?.Stop();
            _TcpServer?.Stop();
            _WebsocketServer?.Stop();
        }

        /// <summary>
        /// Releases all resources used by the LiteGraphMcpServer.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            Stop();
            _HttpServer?.Dispose();
            _TcpServer?.Dispose();
            _WebsocketServer?.Dispose();
            _Client?.Dispose();

            _Disposed = true;
        }

        #endregion

        #region Private-Methods

        private static void Welcome()
        {
            Console.WriteLine(
                Environment.NewLine +
                Constants.Logo +
                Environment.NewLine +
                Constants.ProductName +
                Environment.NewLine +
                Constants.Copyright +
                Environment.NewLine);
        }

        private static void ParseArguments(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                foreach (string arg in args)
                {
                    if (arg.StartsWith("--config="))
                    {
                        Constants.SettingsFile = arg.Substring(9);
                    }

                    if (arg.Equals("--showconfig"))
                    {
                        _ShowConfiguration = true;
                    }

                    if (arg.Equals("--help") || arg.Equals("-h"))
                    {
                        ShowHelp();
                        Environment.Exit(0);
                    }
                }
            }
        }

        private static void InitializeSettings()
        {
            Console.WriteLine("Using settings file '" + Constants.SettingsFile + "'");

            if (!File.Exists(Constants.SettingsFile))
            {
                Console.WriteLine("Settings file '" + Constants.SettingsFile + "' does not exist. Creating default configuration...");

                _Settings.SoftwareVersion = _SoftwareVersion;

                File.WriteAllBytes(Constants.SettingsFile, Encoding.UTF8.GetBytes(_Serializer.SerializeJson(_Settings, true)));
                Console.WriteLine("Created settings file '" + Constants.SettingsFile + "' with default configuration");
            }
            else
            {
                _Settings = _Serializer.DeserializeJson<LiteGraphMcpServerSettings>(File.ReadAllText(Constants.SettingsFile));
                _Settings.Node.LastStartUtc = DateTime.UtcNow;
                File.WriteAllBytes(Constants.SettingsFile, Encoding.UTF8.GetBytes(_Serializer.SerializeJson(_Settings, true)));
            }

            if (_ShowConfiguration)
            {
                Console.WriteLine();
                Console.WriteLine("Configuration:");
                Console.WriteLine(_Serializer.SerializeJson(_Settings, true));
                Console.WriteLine();
                Environment.Exit(0);
            }
        }

        private static void InitializeGlobals()
        {
            #region General

            _Token = _TokenSource.Token;

            #endregion

            #region Environment

            string? liteGraphEndpoint = Environment.GetEnvironmentVariable(Constants.LiteGraphEndpointEnvironmentVariable);
            if (!String.IsNullOrEmpty(liteGraphEndpoint)) _Settings.LiteGraph.Endpoint = liteGraphEndpoint;

            string? liteGraphRepository = Environment.GetEnvironmentVariable(Constants.LiteGraphRepositoryEnvironmentVariable);
            if (!String.IsNullOrEmpty(liteGraphRepository)) _Settings.LiteGraph.RepositoryFilename = liteGraphRepository;

            string? httpHostname = Environment.GetEnvironmentVariable(Constants.McpHttpHostnameEnvironmentVariable);
            if (!String.IsNullOrEmpty(httpHostname)) _Settings.Http.Hostname = httpHostname;

            string? httpPort = Environment.GetEnvironmentVariable(Constants.McpHttpPortEnvironmentVariable);
            if (!String.IsNullOrEmpty(httpPort))
            {
                if (Int32.TryParse(httpPort, out int val))
                {
                    if (val > 0 && val <= 65535) _Settings.Http.Port = val;
                }
            }

            string? tcpAddressEnv = Environment.GetEnvironmentVariable(Constants.McpTcpAddressEnvironmentVariable);
            if (!String.IsNullOrEmpty(tcpAddressEnv)) _Settings.Tcp.Address = tcpAddressEnv;

            string? tcpPort = Environment.GetEnvironmentVariable(Constants.McpTcpPortEnvironmentVariable);
            if (!String.IsNullOrEmpty(tcpPort))
            {
                if (Int32.TryParse(tcpPort, out int val))
                {
                    if (val > 0 && val <= 65535) _Settings.Tcp.Port = val;
                }
            }

            string? wsHostname = Environment.GetEnvironmentVariable(Constants.McpWebSocketHostnameEnvironmentVariable);
            if (!String.IsNullOrEmpty(wsHostname)) _Settings.WebSocket.Hostname = wsHostname;

            string? wsPort = Environment.GetEnvironmentVariable(Constants.McpWebSocketPortEnvironmentVariable);
            if (!String.IsNullOrEmpty(wsPort))
            {
                if (Int32.TryParse(wsPort, out int val))
                {
                    if (val > 0 && val <= 65535) _Settings.WebSocket.Port = val;
                }
            }

            string? consoleLogging = Environment.GetEnvironmentVariable(Constants.ConsoleLoggingEnvironmentVariable);
            if (!String.IsNullOrEmpty(consoleLogging))
            {
                if (Int32.TryParse(consoleLogging, out int val))
                {
                    if (val > 0) _Settings.Logging.ConsoleLogging = true;
                    else _Settings.Logging.ConsoleLogging = false;
                }
            }

            #endregion

            #region Logging

            Console.WriteLine("Initializing logging");

            if (!String.IsNullOrEmpty(_Settings.Logging.LogDirectory))
            {
                if (!Directory.Exists(_Settings.Logging.LogDirectory))
                    Directory.CreateDirectory(_Settings.Logging.LogDirectory);
            }

            #endregion

            #region Storage

            Console.WriteLine("Initializing storage");

            if (!String.IsNullOrEmpty(_Settings.Storage.BackupsDirectory))
            {
                if (!Directory.Exists(_Settings.Storage.BackupsDirectory))
                    Directory.CreateDirectory(_Settings.Storage.BackupsDirectory);
            }

            if (!String.IsNullOrEmpty(_Settings.Storage.TempDirectory))
            {
                if (!Directory.Exists(_Settings.Storage.TempDirectory))
                    Directory.CreateDirectory(_Settings.Storage.TempDirectory);
            }

            #endregion

            #region LiteGraph-Client

            Console.WriteLine("Initializing LiteGraph client");

            if (!string.IsNullOrEmpty(_Settings.LiteGraph.Endpoint))
            {
                Console.WriteLine("Connecting to LiteGraph server at: " + _Settings.LiteGraph.Endpoint);
                _McpClient = new LiteGraphClient(_Settings.LiteGraph.Endpoint);
            }
            else
            {
                if (!string.IsNullOrEmpty(_Settings.LiteGraph.RepositoryFilename))
                {
                    Console.WriteLine("Using SQLite database: " + _Settings.LiteGraph.RepositoryFilename);
                    SqliteGraphRepository repo = new SqliteGraphRepository(_Settings.LiteGraph.RepositoryFilename, inMemory: false);
                    _McpClient = new LiteGraphClient(repo);
                    _McpClient.InitializeRepository();
                }
                else
                {
                    Console.WriteLine("Using in-memory SQLite database");
                    SqliteGraphRepository repo = new SqliteGraphRepository("litegraph.memory", inMemory: true);
                    _McpClient = new LiteGraphClient(repo);
                    _McpClient.InitializeRepository();
                }
            }

            #endregion

            #region MCP-Server

            Console.WriteLine(
                "Starting MCP servers on:" + Environment.NewLine +
                "| HTTP         : http://" + _Settings.Http.Hostname + ":" + _Settings.Http.Port + "/rpc" + Environment.NewLine +
                "| TCP          : tcp://" + (_Settings.Tcp.Address.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : _Settings.Tcp.Address) + ":" + _Settings.Tcp.Port + Environment.NewLine +
                "| WebSocket    : ws://" + _Settings.WebSocket.Hostname + ":" + _Settings.WebSocket.Port + "/mcp");

            string tcpAddressForBinding = _Settings.Tcp.Address.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : _Settings.Tcp.Address;

            _McpHttpServer = new McpHttpServer(_Settings.Http.Hostname, _Settings.Http.Port, "/rpc", "/events", includeDefaultMethods: true);
            _McpTcpServer = new McpTcpServer(IPAddress.Parse(tcpAddressForBinding), _Settings.Tcp.Port, includeDefaultMethods: true);
            _McpWebsocketServer = new McpWebsocketsServer(_Settings.WebSocket.Hostname, _Settings.WebSocket.Port, "/mcp", includeDefaultMethods: true);

            _McpHttpServer.ServerName = "LiteGraph.McpServer";
            _McpHttpServer.ServerVersion = "1.0.0";
            _McpTcpServer.ServerName = "LiteGraph.McpServer";
            _McpTcpServer.ServerVersion = "1.0.0";
            _McpWebsocketServer.ServerName = "LiteGraph.McpServer";
            _McpWebsocketServer.ServerVersion = "1.0.0";

            _McpHttpServer.ClientConnected += ClientConnected;
            _McpHttpServer.ClientDisconnected += ClientDisconnected;
            _McpHttpServer.RequestReceived += ClientRequestReceived;
            _McpHttpServer.ResponseSent += ClientResponseSent;

            _McpTcpServer.ClientConnected += ClientConnected;
            _McpTcpServer.ClientDisconnected += ClientDisconnected;
            _McpTcpServer.RequestReceived += ClientRequestReceived;
            _McpTcpServer.ResponseSent += ClientResponseSent;

            _McpWebsocketServer.ClientConnected += ClientConnected;
            _McpWebsocketServer.ClientDisconnected += ClientDisconnected;
            _McpWebsocketServer.RequestReceived += ClientRequestReceived;
            _McpWebsocketServer.ResponseSent += ClientResponseSent;

            RegisterMcpTools();

            _McpHttpServerTask = _McpHttpServer.StartAsync(_Token);
            _McpTcpServerTask = _McpTcpServer.StartAsync(_Token);
            _McpWebsocketServerTask = _McpWebsocketServer.StartAsync(_Token);

            #endregion

            Console.WriteLine("");
        }

        private static void ClientConnected(object? sender, ClientConnection e)
        {
            Console.WriteLine(_Header + "client connection started with session ID " + e.SessionId + " (" + e.Type + ")");
        }

        private static void ClientDisconnected(object? sender, ClientConnection e)
        {
            Console.WriteLine(_Header + "client connection terminated with session ID " + e.SessionId + " (" + e.Type + ")");
        }

        private static void ClientRequestReceived(object? sender, JsonRpcRequestEventArgs e)
        {
            Console.WriteLine(_Header + "client session " + e.Client.SessionId + " request " + e.Method);
        }

        private static void ClientResponseSent(object? sender, JsonRpcResponseEventArgs e)
        {
            Console.WriteLine(_Header + "client session " + e.Client.SessionId + " request " + e.Method + " completed (" + e.Duration.TotalMilliseconds + "ms)");
        }

        private static void RegisterMcpTools()
        {
            if (_McpHttpServer == null || _McpTcpServer == null || _McpWebsocketServer == null || _McpClient == null)
                throw new InvalidOperationException("Servers and client have not been initialized");

            Registrations.TenantRegistrations.RegisterHttpTools(_McpHttpServer, _McpClient, _Serializer);
            Registrations.GraphRegistrations.RegisterHttpTools(_McpHttpServer, _McpClient, _Serializer);
            Registrations.NodeRegistrations.RegisterHttpTools(_McpHttpServer, _McpClient, _Serializer);

            Registrations.TenantRegistrations.RegisterTcpMethods(_McpTcpServer, _McpClient, _Serializer);
            Registrations.GraphRegistrations.RegisterTcpMethods(_McpTcpServer, _McpClient, _Serializer);
            Registrations.NodeRegistrations.RegisterTcpMethods(_McpTcpServer, _McpClient, _Serializer);

            Registrations.TenantRegistrations.RegisterWebSocketMethods(_McpWebsocketServer, _McpClient, _Serializer);
            Registrations.GraphRegistrations.RegisterWebSocketMethods(_McpWebsocketServer, _McpClient, _Serializer);
            Registrations.NodeRegistrations.RegisterWebSocketMethods(_McpWebsocketServer, _McpClient, _Serializer);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("LiteGraph MCP Server");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  LiteGraph.McpServer [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --config=<file>        Settings file path (default: ./litegraph.json)");
            Console.WriteLine("  --showconfig           Display configuration and exit");
            Console.WriteLine("  --help, -h             Show this help message");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("  Settings are read from litegraph.json file.");
            Console.WriteLine("  If the file doesn't exist, it will be created with default values.");
            Console.WriteLine();
            Console.WriteLine("  To use a remote LiteGraph server, set LiteGraph.Endpoint in the JSON file.");
            Console.WriteLine("  To use a local database, set LiteGraph.RepositoryFilename in the JSON file.");
            Console.WriteLine("  If both are empty, uses in-memory database.");
            Console.WriteLine();
        }

        private void RegisterTools()
        {
            if (_HttpServer == null || _TcpServer == null || _WebsocketServer == null || _Client == null)
                throw new InvalidOperationException("Servers and client have not been initialized");

            RegisterHttpTools(_HttpServer);

            RegisterTcpMethods(_TcpServer);
            RegisterWebSocketMethods(_WebsocketServer);
        }

        private void RegisterHttpTools(McpHttpServer server)
        {
            Registrations.TenantRegistrations.RegisterHttpTools(server, _Client!, _Serializer);
            Registrations.GraphRegistrations.RegisterHttpTools(server, _Client!, _Serializer);
            Registrations.NodeRegistrations.RegisterHttpTools(server, _Client!, _Serializer);
        }

        private void RegisterTcpMethods(McpTcpServer server)
        {
            Registrations.TenantRegistrations.RegisterTcpMethods(server, _Client!, _Serializer);
            Registrations.GraphRegistrations.RegisterTcpMethods(server, _Client!, _Serializer);
            Registrations.NodeRegistrations.RegisterTcpMethods(server, _Client!, _Serializer);
        }

        private void RegisterWebSocketMethods(McpWebsocketsServer server)
        {
            Registrations.TenantRegistrations.RegisterWebSocketMethods(server, _Client!, _Serializer);
            Registrations.GraphRegistrations.RegisterWebSocketMethods(server, _Client!, _Serializer);
            Registrations.NodeRegistrations.RegisterWebSocketMethods(server, _Client!, _Serializer);
        }

        #endregion
    }
}
