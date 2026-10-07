namespace LiteGraph.Server
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Coordination;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.API.REST;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using LiteGraph.Server.Services.Cluster;
    using SyslogLogging;

    /// <summary>
    /// Orchestrator server.
    /// </summary>
    public static class LiteGraphServer
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Header = "[LiteGraphServer] ";
        private static int _ProcessId = Environment.ProcessId;
        private static bool _CreateDefaultRecords = false;
        private static bool _InitOnly = false;

        private static Settings _Settings = new Settings();
        private static LoggingModule _Logging = null;
        private static Serializer _Serializer = new Serializer();

        private static GraphRepositoryBase _Repo = null;
        private static LiteGraphClient _LiteGraph = null;

        private static ServiceHandler _ServiceHandler = null;
        private static AuthenticationService _AuthenticationService = null;
        private static Services.ChatEndpointHealthService _ChatHealthService = null;
        private static Services.Chat.ChatService _ChatService = null;
        private static RequestHistoryService _RequestHistoryService = null;
        private static ObservabilityService _ObservabilityService = null;
        private static RestServiceHandler _RestService = null;
        private static ILockProvider _LockProvider = null;
        private static ClusterContext _Cluster = null;
        private static NodeHealthService _NodeHealth = null;
        private static SettingsFileService _SettingsFile = null;
        private static ClusterRegistry _Registry = null;
        private static RollingRestartCoordinator _RollingRestart = null;

        private static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private static CancellationToken _Token;
        private static readonly TaskCompletionSource<bool> _ShutdownSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static readonly object _ShutdownLock = new object();
        private static volatile bool _ShutdownRequested = false;
        private static bool _CleanupStarted = false;

        #endregion

        #region Entrypoint

        public static async Task Main(string[] args)
        {
            // JSON metadata for the server's own types; required under Native AOT, and the same path under the JIT.
            ServerJson.Register();

            try
            {
                RegisterShutdownHandlers();

                Welcome();
                ParseArguments(args);
                InitializeSettings();
                await InitializeGlobals().ConfigureAwait(false);

                if (_InitOnly) return;

                if (!_ShutdownRequested)
                {
                    _Logging.Info(_Header + "started at " + DateTime.UtcNow + " using process ID " + _ProcessId);
                    await WaitForShutdownAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await CleanupAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Public-Methods

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
                    else if (arg.Equals("--create-default-records", StringComparison.OrdinalIgnoreCase))
                    {
                        _CreateDefaultRecords = true;
                    }
                    else if (arg.Equals("--init-only", StringComparison.OrdinalIgnoreCase))
                    {
                        _InitOnly = true;
                        _CreateDefaultRecords = true;
                    }
                }
            }
        }

        private static void RegisterShutdownHandlers()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                RequestShutdown("CTRL+C received");
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                RequestShutdown("process exit received");
                CleanupAsync().GetAwaiter().GetResult();
            };
        }

        private static void RequestShutdown(string reason)
        {
            lock (_ShutdownLock)
            {
                if (_ShutdownRequested) return;

                _ShutdownRequested = true;
            }

            _Cluster?.BeginDrain();

            LogInfo(_Header + reason + ", initiating shutdown");
            _ShutdownSignal.TrySetResult(true);
        }

        private static async Task WaitForShutdownAsync()
        {
            await _ShutdownSignal.Task.ConfigureAwait(false);
        }

        private static async Task CleanupAsync()
        {
            lock (_ShutdownLock)
            {
                if (_CleanupStarted) return;

                _CleanupStarted = true;
                _ShutdownRequested = true;
            }

            _ShutdownSignal.TrySetResult(true);
            LogInfo(_Header + "starting cleanup");

            TryCleanup("cancellation token source", () =>
            {
                if (!_TokenSource.IsCancellationRequested) _TokenSource.Cancel();
            });

            TryCleanup("REST service", () =>
            {
                _RestService?.Dispose();
                _RestService = null;
            });

            TryCleanup("cluster registry", () =>
            {
                _Registry?.Dispose();
                _Registry = null;
            });

            TryCleanup("chat service", () =>
            {
                _ChatService?.Dispose();
                _ChatService = null;
            });

            TryCleanup("chat endpoint health service", () =>
            {
                _ChatHealthService?.Dispose();
                _ChatHealthService = null;
            });

            TryCleanup("request history service", () =>
            {
                _RequestHistoryService?.Dispose();
                _RequestHistoryService = null;
            });

            TryCleanup("observability service", () =>
            {
                _ObservabilityService?.Dispose();
                _ObservabilityService = null;
            });

            await TryCleanupAsync("authentication service", async () =>
            {
                await DisposeIfNeededAsync(_AuthenticationService).ConfigureAwait(false);
                _AuthenticationService = null;
            }).ConfigureAwait(false);

            await TryCleanupAsync("service handler", async () =>
            {
                await DisposeIfNeededAsync(_ServiceHandler).ConfigureAwait(false);
                _ServiceHandler = null;
            }).ConfigureAwait(false);

            await TryCleanupAsync("LiteGraph client", async () =>
            {
                await DisposeIfNeededAsync(_LiteGraph).ConfigureAwait(false);
                _LiteGraph = null;
            }).ConfigureAwait(false);

            await TryCleanupAsync("graph repository", async () =>
            {
                await DisposeIfNeededAsync(_Repo).ConfigureAwait(false);
                _Repo = null;
            }).ConfigureAwait(false);

            TryCleanup("lock provider", () =>
            {
                _LockProvider?.Dispose();
                _LockProvider = null;
            });

            LogInfo(_Header + "stopped at " + DateTime.UtcNow);

            TryCleanup("cancellation token source", () =>
            {
                _TokenSource?.Dispose();
            });

            TryCleanup("logging", () =>
            {
                if (_Logging is IDisposable disposableLogging) disposableLogging.Dispose();
                _Logging = null;
            });
        }

        private static async Task DisposeIfNeededAsync(object obj)
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static void TryCleanup(string component, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogError(_Header + "error while cleaning up " + component + ": " + e.Message);
            }
        }

        private static async Task TryCleanupAsync(string component, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError(_Header + "error while cleaning up " + component + ": " + e.Message);
            }
        }

        private static void LogInfo(string msg)
        {
            if (_Logging != null) _Logging.Info(msg);
            else Console.WriteLine(msg);
        }

        private static void LogError(string msg)
        {
            if (_Logging != null) _Logging.Error(msg);
            else Console.WriteLine(msg);
        }

        private static void InitializeSettings()
        {
            Console.WriteLine("Using settings file '" + Constants.SettingsFile + "'");

            if (!File.Exists(Constants.SettingsFile))
            {
                Console.WriteLine("Settings file '" + Constants.SettingsFile + "' does not exist, creating");
                File.WriteAllBytes(Constants.SettingsFile, Encoding.UTF8.GetBytes(_Serializer.SerializeJson(_Settings, true)));
                _CreateDefaultRecords = true;
            }
            else
            {
                string json = File.ReadAllText(Constants.SettingsFile);
                _Settings = _Serializer.DeserializeJson<Settings>(json);
            }
        }

        private static void ApplyDatabaseEnvironmentVariables()
        {
            string dbType = Environment.GetEnvironmentVariable(Constants.DatabaseTypeEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbType))
            {
                if (Enum.TryParse<LiteGraph.DatabaseTypeEnum>(dbType, true, out LiteGraph.DatabaseTypeEnum parsed))
                {
                    _Settings.LiteGraph.Database.Type = parsed;
                }
                else
                {
                    Console.WriteLine("Invalid database type detected in environment variable " + Constants.DatabaseTypeEnvironmentVariable);
                }
            }

            string dbFilename = Environment.GetEnvironmentVariable(Constants.DatabaseFilenameEnvironmentVariable);
            if (String.IsNullOrEmpty(dbFilename))
                dbFilename = Environment.GetEnvironmentVariable(Constants.DatabaseFilenameEnvironmentVariableAlternate);
            if (!String.IsNullOrEmpty(dbFilename)) _Settings.LiteGraph.GraphRepositoryFilename = dbFilename;

            string dbHostname = Environment.GetEnvironmentVariable(Constants.DatabaseHostnameEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbHostname)) _Settings.LiteGraph.Database.Hostname = dbHostname;

            string dbPort = Environment.GetEnvironmentVariable(Constants.DatabasePortEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbPort))
            {
                if (Int32.TryParse(dbPort, out int parsedPort) && parsedPort >= 0 && parsedPort <= 65535)
                    _Settings.LiteGraph.Database.Port = parsedPort;
                else
                    Console.WriteLine("Invalid database port detected in environment variable " + Constants.DatabasePortEnvironmentVariable);
            }

            string dbName = Environment.GetEnvironmentVariable(Constants.DatabaseNameEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbName)) _Settings.LiteGraph.Database.DatabaseName = dbName;

            string dbUsername = Environment.GetEnvironmentVariable(Constants.DatabaseUsernameEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbUsername)) _Settings.LiteGraph.Database.Username = dbUsername;

            string dbPassword = Environment.GetEnvironmentVariable(Constants.DatabasePasswordEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbPassword)) _Settings.LiteGraph.Database.Password = dbPassword;

            string dbSchema = Environment.GetEnvironmentVariable(Constants.DatabaseSchemaEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbSchema)) _Settings.LiteGraph.Database.Schema = dbSchema;

            string dbConnectionString = Environment.GetEnvironmentVariable(Constants.DatabaseConnectionStringEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbConnectionString)) _Settings.LiteGraph.Database.ConnectionString = dbConnectionString;

            string dbMaxConnections = Environment.GetEnvironmentVariable(Constants.DatabaseMaxConnectionsEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbMaxConnections))
            {
                if (Int32.TryParse(dbMaxConnections, out int parsedMaxConnections) && parsedMaxConnections > 0)
                    _Settings.LiteGraph.Database.MaxConnections = parsedMaxConnections;
                else
                    Console.WriteLine("Invalid database max connections detected in environment variable " + Constants.DatabaseMaxConnectionsEnvironmentVariable);
            }

            string dbCommandTimeout = Environment.GetEnvironmentVariable(Constants.DatabaseCommandTimeoutEnvironmentVariable);
            if (!String.IsNullOrEmpty(dbCommandTimeout))
            {
                if (Int32.TryParse(dbCommandTimeout, out int parsedCommandTimeout) && parsedCommandTimeout > 0)
                    _Settings.LiteGraph.Database.CommandTimeoutSeconds = parsedCommandTimeout;
                else
                    Console.WriteLine("Invalid database command timeout detected in environment variable " + Constants.DatabaseCommandTimeoutEnvironmentVariable);
            }
        }

        private static void ApplyObservabilityEnvironmentVariables()
        {
            string serviceName = Environment.GetEnvironmentVariable(Constants.OTelServiceNameEnvironmentVariable);
            if (!String.IsNullOrEmpty(serviceName)) _Settings.Observability.ServiceName = serviceName;

            string enableOtlp = Environment.GetEnvironmentVariable(Constants.OtlpExporterEnableEnvironmentVariable);
            if (!String.IsNullOrEmpty(enableOtlp))
            {
                if (TryParseBoolean(enableOtlp, out bool enabled))
                    _Settings.Observability.EnableOtlpExporter = enabled;
                else
                    Console.WriteLine("Invalid OTLP exporter enable value detected in environment variable " + Constants.OtlpExporterEnableEnvironmentVariable);
            }

            string endpoint = Environment.GetEnvironmentVariable(Constants.OtlpEndpointEnvironmentVariable);
            if (String.IsNullOrEmpty(endpoint))
                endpoint = Environment.GetEnvironmentVariable(Constants.OTelOtlpEndpointEnvironmentVariable);
            if (!String.IsNullOrEmpty(endpoint)) _Settings.Observability.OtlpEndpoint = endpoint;

            string protocol = Environment.GetEnvironmentVariable(Constants.OtlpProtocolEnvironmentVariable);
            if (String.IsNullOrEmpty(protocol))
                protocol = Environment.GetEnvironmentVariable(Constants.OTelOtlpProtocolEnvironmentVariable);
            if (!String.IsNullOrEmpty(protocol)) _Settings.Observability.OtlpProtocol = protocol;

            string headers = Environment.GetEnvironmentVariable(Constants.OtlpHeadersEnvironmentVariable);
            if (String.IsNullOrEmpty(headers))
                headers = Environment.GetEnvironmentVariable(Constants.OTelOtlpHeadersEnvironmentVariable);
            if (!String.IsNullOrEmpty(headers)) _Settings.Observability.OtlpHeaders = headers;

            string timeout = Environment.GetEnvironmentVariable(Constants.OtlpTimeoutEnvironmentVariable);
            if (String.IsNullOrEmpty(timeout))
                timeout = Environment.GetEnvironmentVariable(Constants.OTelOtlpTimeoutEnvironmentVariable);
            if (!String.IsNullOrEmpty(timeout))
            {
                if (Int32.TryParse(timeout, out int parsedTimeout) && parsedTimeout > 0)
                    _Settings.Observability.OtlpTimeoutMilliseconds = parsedTimeout;
                else
                    Console.WriteLine("Invalid OTLP timeout detected in environment variable " + Constants.OtlpTimeoutEnvironmentVariable);
            }
        }

        private static void ApplyTransactionEnvironmentVariables()
        {
            string maxOperations = Environment.GetEnvironmentVariable(Constants.TransactionMaxOperationsEnvironmentVariable);
            if (!String.IsNullOrEmpty(maxOperations))
            {
                if (Int32.TryParse(maxOperations, out int parsedMaxOperations) && parsedMaxOperations >= 1 && parsedMaxOperations <= 10000)
                    _Settings.LiteGraph.Transactions.MaxOperations = parsedMaxOperations;
                else
                    Console.WriteLine("Invalid transaction max operations detected in environment variable " + Constants.TransactionMaxOperationsEnvironmentVariable);
            }

            string maxTimeout = Environment.GetEnvironmentVariable(Constants.TransactionMaxTimeoutEnvironmentVariable);
            if (!String.IsNullOrEmpty(maxTimeout))
            {
                if (Int32.TryParse(maxTimeout, out int parsedMaxTimeout) && parsedMaxTimeout >= 1 && parsedMaxTimeout <= 3600)
                    _Settings.LiteGraph.Transactions.MaxTimeoutSeconds = parsedMaxTimeout;
                else
                    Console.WriteLine("Invalid transaction max timeout detected in environment variable " + Constants.TransactionMaxTimeoutEnvironmentVariable);
            }
        }

        private static bool TryParseBoolean(string value, out bool parsed)
        {
            parsed = false;
            if (String.IsNullOrWhiteSpace(value)) return false;

            string normalized = value.Trim();
            if (Boolean.TryParse(normalized, out parsed)) return true;
            if (String.Equals(normalized, "1", StringComparison.Ordinal)
                || String.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "y", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase))
            {
                parsed = true;
                return true;
            }

            if (String.Equals(normalized, "0", StringComparison.Ordinal)
                || String.Equals(normalized, "no", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "n", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "off", StringComparison.OrdinalIgnoreCase))
            {
                parsed = false;
                return true;
            }

            return false;
        }

        private static async Task InitializeGlobals()
        {
            #region General-and-Environment

            _Token = _TokenSource.Token;

            string webserverPortStr = Environment.GetEnvironmentVariable(Constants.WebserverPortEnvironmentVariable);
            if (Int32.TryParse(webserverPortStr, out int webserverPort))
            {
                if (webserverPort >= 0 && webserverPort <= 65535)
                {
                    _Settings.Rest.Port = webserverPort;
                }
                else
                {
                    Console.WriteLine("Invalid webserver port detected in environment variable " + Constants.WebserverPortEnvironmentVariable);
                }
            }

            string requestTimeoutStr = Environment.GetEnvironmentVariable(Constants.RequestTimeoutEnvironmentVariable);
            if (!String.IsNullOrEmpty(requestTimeoutStr))
            {
                if (Int32.TryParse(requestTimeoutStr, out int requestTimeout) && requestTimeout > 0)
                    _Settings.RequestTimeoutSeconds = requestTimeout;
                else
                    Console.WriteLine("Invalid request timeout detected in environment variable " + Constants.RequestTimeoutEnvironmentVariable);
            }

            ApplyDatabaseEnvironmentVariables();
            ApplyTransactionEnvironmentVariables();
            ApplyInitializationEnvironmentVariables();
            ApplyObservabilityEnvironmentVariables();
            ApplySecurityEnvironmentVariables();
            ApplyClusterEnvironmentVariables();
            ValidateClusterSettings();

            #endregion

            #region Logging

            Console.WriteLine("Initializing logging");

            List<SyslogServer> syslogServers = new List<SyslogServer>();
            if (_Settings.Logging.Servers != null && _Settings.Logging.Servers.Count > 0)
            {
                foreach (LiteGraph.SyslogServer server in _Settings.Logging.Servers)
                {
                    syslogServers.Add(
                        new SyslogServer
                        {
                            Hostname = server.Hostname,
                            Port = server.Port
                        }
                    );

                    Console.WriteLine("| syslog://" + server.Hostname + ":" + server.Port);
                }
            }

            _Logging = new LoggingModule(syslogServers);
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.EnableColors = _Settings.Logging.EnableColors;

            if (!String.IsNullOrEmpty(_Settings.Logging.LogDirectory))
            {
                if (!Directory.Exists(_Settings.Logging.LogDirectory))
                    Directory.CreateDirectory(_Settings.Logging.LogDirectory);

                _Settings.Logging.LogFilename = _Settings.Logging.LogDirectory + _Settings.Logging.LogFilename;
            }

            if (!String.IsNullOrEmpty(_Settings.Logging.LogFilename))
            {
                _Logging.Settings.FileLogging = FileLoggingMode.FileWithDate;
                _Logging.Settings.LogFilename = _Settings.Logging.LogFilename;
            }

            _Logging.Debug(_Header + "logging initialized");

            #endregion

            #region Repositories

            if (_Settings.Cluster.Enable)
            {
                ClutchLockProvider clutch = new ClutchLockProvider(_Settings.Cluster.Clutch, _Settings.Cluster.ClusterName, _Logging);
                clutch.LockLost += (sender, key) => _ObservabilityService?.RecordLockLost(key);
                _LockProvider = new InstrumentedLockProvider(clutch);
                await clutch.ConnectAsync(_Token).ConfigureAwait(false);
            }
            else
            {
                _LockProvider = new InstrumentedLockProvider(new LocalLockProvider());
            }

            _Cluster = new ClusterContext(_Settings.Cluster, _LockProvider);
            _Logging.Info(
                _Header + (_Cluster.Enabled
                    ? "cluster mode: node " + _Cluster.NodeId + " in cluster " + _Cluster.ClusterName + ", locks via Clutch at " + _Settings.Cluster.Clutch.Endpoint
                    : "single-node mode: node " + _Cluster.NodeId));

            _Logging.Info(_Header + "initializing graph repository: " + _Settings.LiteGraph.Database.ToSafeString());
            _Repo = GraphRepositoryFactory.Create(_Settings.LiteGraph.Database);
            _Repo.LockProvider = _LockProvider;
            _Repo.InitializeRepository();

            #endregion

            #region Create-Default-Records

            if (_CreateDefaultRecords) await CreateDefaultRecords().ConfigureAwait(false);

            #endregion

            #region LiteGraph-Client

            // The client gets its own LoggingSettings copy: it is mutated below for
            // query-debug behavior, and sharing the instance would flip the server's
            // reported Logging.Enable (and other fields) as a side effect.
            CachingSettings caching = _Serializer.CopyObject<CachingSettings>(_Settings.Caching);
            if (_Settings.Cluster.Enable && caching.Enable)
            {
                // Client caches are invalidated only by changes made through this process; another node's delete would leave
                // them answering "exists" for an object that is gone.  Cluster nodes read through to the database instead.
                caching.Enable = false;
                _Logging.Info(_Header + "object caching disabled in cluster mode");
            }

            _LiteGraph = new LiteGraphClient(_Repo, _Serializer.CopyObject<LiteGraph.LoggingSettings>(_Settings.Logging), caching);
            _LiteGraph.Logging.Enable = _Settings.Debug.DatabaseQueries;
            _LiteGraph.Logging.Logger = LiteGraphLogger;
            _LiteGraph.Logging.LogQueries = _Settings.Debug.DatabaseQueries;
            _LiteGraph.Logging.LogResults = _Settings.Debug.DatabaseQueries;
            _LiteGraph.Algorithm.Configuration.MaxNodes = _Settings.LiteGraph.MaxAlgorithmNodes;
            _LiteGraph.Algorithm.Configuration.MaxEdges = _Settings.LiteGraph.MaxAlgorithmEdges;

            _LiteGraph.InitializeRepository();

            #endregion

            if (_InitOnly)
            {
                _Logging.Info(_Header + "initialization-only mode completed successfully");
                return;
            }

            #region Services

            _AuthenticationService = new AuthenticationService(
                _Settings,
                _Logging,
                _Serializer,
                _Repo);

            if (_Settings.Cluster.Enable)
            {
                _AuthenticationService.Authorization.EnableCache = false;
                _Logging.Info(_Header + "authorization policy caching disabled in cluster mode");
            }

            _ServiceHandler = new ServiceHandler(
                _Settings,
                _Logging,
                _LiteGraph,
                _Serializer,
                _AuthenticationService);

            _RequestHistoryService = new RequestHistoryService(
                _Settings,
                _Logging,
                _Repo);
            _RequestHistoryService.LockProvider = _LockProvider;

            _ObservabilityService = new ObservabilityService(_Settings.Observability, _Cluster.NodeId, _Cluster.Enabled ? _Cluster.ClusterName : null);
            _ObservabilityService.RecordNodeIdentity(
                _Cluster.NodeId,
                _Cluster.Enabled ? _Cluster.ClusterName : null,
                typeof(LiteGraphServer).Assembly.GetName().Version?.ToString(3),
                _Cluster.StartedUtc);
            if (_LockProvider is InstrumentedLockProvider instrumented) instrumented.Observability = _ObservabilityService;
            _RequestHistoryService.Observability = _ObservabilityService;
            _ObservabilityService.RecordStorageBackend(
                _Settings.LiteGraph.Database.Type.ToString(),
                _Settings.LiteGraph.Database.Type == DatabaseTypeEnum.Postgresql);
            _ObservabilityService.RecordStorageConnectionPool(
                _Settings.LiteGraph.Database.Type.ToString(),
                _Settings.LiteGraph.Database.MaxConnections,
                _Settings.LiteGraph.Database.CommandTimeoutSeconds);

            _ChatHealthService = new Services.ChatEndpointHealthService(
                _Logging,
                _LiteGraph,
                _ObservabilityService);

            _ChatService = new Services.Chat.ChatService(
                _Settings,
                _Logging,
                _LiteGraph,
                _ServiceHandler,
                _AuthenticationService.Authorization,
                _ObservabilityService,
                _ChatHealthService);
            _ChatService.LockProvider = _LockProvider;

            _ServiceHandler.ChatHealth = _ChatHealthService;
            _ServiceHandler.Chat = _ChatService;
            _ServiceHandler.Observability = _ObservabilityService;
            _ServiceHandler.Authorization = _AuthenticationService.Authorization;

            _NodeHealth = new NodeHealthService(_LiteGraph, _Cluster, _Logging);
            _NodeHealth.StorageProvider = _Settings.LiteGraph.Database.Type.ToString();
            _SettingsFile = new SettingsFileService(Constants.SettingsFile, _Serializer, _Settings);
            if (_SettingsFile.OverriddenPaths.Count > 0)
                _Logging.Info(_Header + "settings not taken from the settings file (kept out of settings saves): " + String.Join(", ", _SettingsFile.OverriddenPaths));

            _ServiceHandler.NodeHealth = _NodeHealth;
            _ServiceHandler.SettingsFile = _SettingsFile;
            _ServiceHandler.Cluster = _Cluster;

            if (_Settings.Cluster.Enable)
            {
                _Registry = new ClusterRegistry(_Settings.Cluster, _Cluster, _NodeHealth, _Serializer, _Logging);
                _Registry.Observability = _ObservabilityService;
                if (_LockProvider is InstrumentedLockProvider wrapped && wrapped.Inner is ClutchLockProvider clutchProvider)
                    _Registry.ClutchSessionIdProvider = () => clutchProvider.SessionId;
                _ChatService.Registry = _Registry;
                _RequestHistoryService.Registry = _Registry;
                _NodeHealth.Registry = _Registry;
                _ServiceHandler.Registry = _Registry;
                _RollingRestart = new RollingRestartCoordinator(_Settings.Cluster, _Cluster, _Registry, _Logging, RequestShutdown);
                _Registry.SettingsChanged += OnClusterSettingsChanged;
                _Registry.RestartRequested += OnClusterRestartRequested;
            }

            _RestService = new RestServiceHandler(
                _Settings,
                _Logging,
                _LiteGraph,
                _Serializer,
                _AuthenticationService,
                _ServiceHandler,
                _RequestHistoryService,
                _ObservabilityService,
                _ChatService,
                _ChatHealthService,
                _Cluster);

            // Register in the node registry last, so the first heartbeat other nodes see reports a node that is ready.
            if (_Registry != null) await _Registry.StartAsync(_Token).ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _ChatHealthService.Start(_TokenSource.Token).ConfigureAwait(false);
                    if (_Settings.Cluster.Enable) _ChatHealthService.StartResyncLoop(_Settings.Cluster.EndpointResyncIntervalMs);
                }
                catch (Exception e)
                {
                    LogError(_Header + "chat endpoint health service failed to start: " + e.Message);
                }
            });

            #endregion
        }

        private static void OnClusterSettingsChanged(object sender, long version)
        {
            try
            {
                Settings file = _SettingsFile.Read();
                if (!_SettingsFile.IsOverridden("RequestTimeoutSeconds") && _Settings.RequestTimeoutSeconds != file.RequestTimeoutSeconds)
                {
                    _Settings.RequestTimeoutSeconds = file.RequestTimeoutSeconds;
                    _Logging.Info(_Header + "applied RequestTimeoutSeconds " + file.RequestTimeoutSeconds + " from settings version " + version);
                }

                _Registry.SettingsRestartPending = _SettingsFile.RestartNeeded();
                if (_Registry.SettingsRestartPending)
                    _Logging.Info(_Header + "settings version " + version + " changes settings that apply after a restart");
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "unable to apply settings version " + version + ": " + e.Message);
            }
        }

        private static void OnClusterRestartRequested(object sender, long version)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _RollingRestart.RunAsync(version, _Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_Token.IsCancellationRequested)
                {
                }
                catch (Exception e)
                {
                    LogError(_Header + "rolling restart failed: " + e.Message);
                }
            });
        }

        private static void LiteGraphLogger(SeverityEnum sev, string msg)
        {
            switch (sev)
            {
                case SeverityEnum.Debug:
                    _Logging.Debug(msg);
                    break;
                case SeverityEnum.Info:
                    _Logging.Info(msg);
                    break;
                case SeverityEnum.Warn:
                    _Logging.Warn(msg);
                    break;
                case SeverityEnum.Error:
                    _Logging.Error(msg);
                    break;
                case SeverityEnum.Critical:
                    _Logging.Critical(msg);
                    break;
                case SeverityEnum.Alert:
                    _Logging.Alert(msg);
                    break;
                case SeverityEnum.Emergency:
                    _Logging.Emergency(msg);
                    break;
            }
        }

        private static void ApplySecurityEnvironmentVariables()
        {
            string adminToken = Environment.GetEnvironmentVariable(Constants.AdminBearerTokenEnvironmentVariable);
            if (!String.IsNullOrEmpty(adminToken)) _Settings.LiteGraph.AdminBearerToken = adminToken;

            string key = Environment.GetEnvironmentVariable(Constants.EncryptionKeyEnvironmentVariable);
            if (!String.IsNullOrEmpty(key)) _Settings.Encryption.Key = key;

            string iv = Environment.GetEnvironmentVariable(Constants.EncryptionIvEnvironmentVariable);
            if (!String.IsNullOrEmpty(iv)) _Settings.Encryption.Iv = iv;
        }

        private static void ApplyClusterEnvironmentVariables()
        {
            string enable = Environment.GetEnvironmentVariable(Constants.ClusterEnableEnvironmentVariable);
            if (!String.IsNullOrEmpty(enable))
            {
                if (TryParseBoolean(enable, out bool enabled)) _Settings.Cluster.Enable = enabled;
                else Console.WriteLine("Invalid value detected in environment variable " + Constants.ClusterEnableEnvironmentVariable);
            }

            string clusterName = Environment.GetEnvironmentVariable(Constants.ClusterNameEnvironmentVariable);
            if (!String.IsNullOrEmpty(clusterName)) _Settings.Cluster.ClusterName = clusterName;

            string nodeId = Environment.GetEnvironmentVariable(Constants.NodeIdEnvironmentVariable);
            if (!String.IsNullOrEmpty(nodeId)) _Settings.Cluster.NodeId = nodeId;

            string clutchEndpoint = Environment.GetEnvironmentVariable(Constants.ClutchEndpointEnvironmentVariable);
            if (!String.IsNullOrEmpty(clutchEndpoint)) _Settings.Cluster.Clutch.Endpoint = clutchEndpoint;

            string clutchKey = Environment.GetEnvironmentVariable(Constants.ClutchAccessKeyEnvironmentVariable);
            if (!String.IsNullOrEmpty(clutchKey)) _Settings.Cluster.Clutch.AccessKey = clutchKey;

            string redisConnection = Environment.GetEnvironmentVariable(Constants.RedisConnectionStringEnvironmentVariable);
            if (!String.IsNullOrEmpty(redisConnection)) _Settings.Cluster.Redis.ConnectionString = redisConnection;

            string trustedProxies = Environment.GetEnvironmentVariable(Constants.TrustedProxiesEnvironmentVariable);
            if (!String.IsNullOrEmpty(trustedProxies))
            {
                _Settings.Cluster.TrustedProxies = trustedProxies
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                _Settings.Cluster.TrustForwardedHeaders = _Settings.Cluster.TrustedProxies.Count > 0;
            }
        }

        private static void ValidateClusterSettings()
        {
            bool defaultKey = _Settings.Encryption.Key.Trim('0').Length == 0;
            bool defaultIv = _Settings.Encryption.Iv.Trim('0').Length == 0;
            if (defaultKey || defaultIv)
                Console.WriteLine("WARNING: the encryption key or IV is the all-zero default; set " + Constants.EncryptionKeyEnvironmentVariable + " and " + Constants.EncryptionIvEnvironmentVariable + " for any non-demonstration deployment");

            if (!_Settings.Cluster.Enable) return;

            if (_Settings.LiteGraph.Database.Type != DatabaseTypeEnum.Postgresql)
                throw new InvalidOperationException("Cluster mode requires Database.Type = Postgresql; SQLite cannot be shared between nodes. Set " + Constants.DatabaseTypeEnvironmentVariable + "=Postgresql or disable " + Constants.ClusterEnableEnvironmentVariable + ".");

            if (_Settings.LiteGraph.Database.InMemory)
                throw new InvalidOperationException("Cluster mode cannot use an in-memory database; each node would hold its own private copy.");

            if (String.IsNullOrEmpty(_Settings.Cluster.Clutch.AccessKey))
                throw new InvalidOperationException("Cluster mode requires a Clutch access key. Set Cluster.Clutch.AccessKey or " + Constants.ClutchAccessKeyEnvironmentVariable + ".");

            bool insecure = defaultKey || defaultIv || _Settings.LiteGraph.AdminBearerToken == "litegraphadmin";
            if (insecure)
            {
                if (!_Settings.Cluster.AllowInsecureDefaults)
                    throw new InvalidOperationException(
                        "Cluster mode refuses to start with the default encryption key, IV, or administrator token. Set "
                        + Constants.EncryptionKeyEnvironmentVariable + ", " + Constants.EncryptionIvEnvironmentVariable + ", and "
                        + Constants.AdminBearerTokenEnvironmentVariable + " (identical on every node), or set Cluster.AllowInsecureDefaults = true for a demonstration deployment.");

                Console.WriteLine("WARNING: cluster mode is running with default credentials because Cluster.AllowInsecureDefaults is true; do not use this configuration in production");
            }
        }

        private static void ApplyInitializationEnvironmentVariables()
        {
            string createDefaultRecords = Environment.GetEnvironmentVariable(Constants.CreateDefaultRecordsEnvironmentVariable);
            if (!String.IsNullOrEmpty(createDefaultRecords))
            {
                if (TryParseBoolean(createDefaultRecords, out bool enabled))
                    _CreateDefaultRecords = enabled;
                else
                    Console.WriteLine("Invalid default-record creation value detected in environment variable " + Constants.CreateDefaultRecordsEnvironmentVariable);
            }

            string initOnly = Environment.GetEnvironmentVariable(Constants.InitOnlyEnvironmentVariable);
            if (!String.IsNullOrEmpty(initOnly))
            {
                if (TryParseBoolean(initOnly, out bool enabled))
                {
                    _InitOnly = enabled;
                    if (enabled) _CreateDefaultRecords = true;
                }
                else
                {
                    Console.WriteLine("Invalid initialization-only value detected in environment variable " + Constants.InitOnlyEnvironmentVariable);
                }
            }
        }

        private static async Task CreateDefaultRecords()
        {
            #region Metadata-Records

            Console.WriteLine("Creating default records in database " + _Settings.LiteGraph.Database.ToSafeString());

            TenantMetadata tenant = new TenantMetadata
            {
                GUID = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                Name = "Default tenant",
                Active = true,
                CreatedUtc = DateTime.UtcNow
            };

            if (!await _Repo.Tenant.ExistsByGuid(tenant.GUID, CancellationToken.None).ConfigureAwait(false))
            {
                tenant = await _Repo.Tenant.Create(tenant, CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("| Created tenant     : " + tenant.GUID);
            }

            UserMaster user = new UserMaster
            {
                GUID = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                TenantGUID = tenant.GUID,
                FirstName = "Default",
                LastName = "User",
                Email = "default@user.com",
                Password = "password",
                Active = true,
                IsSystemAdmin = true,
                IsTenantAdmin = true,
                CreatedUtc = DateTime.UtcNow
            };

            if (!await _Repo.User.ExistsByGuid(tenant.GUID, user.GUID, CancellationToken.None).ConfigureAwait(false))
            {
                user = await _Repo.User.Create(user, CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("| Created user       : " + user.GUID + " email: " + user.Email + " pass: " + OperationalLogRedactor.RedactValue(user.Password) + " (system administrator)");
            }

            Credential cred = new Credential
            {
                GUID = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                TenantGUID = tenant.GUID,
                UserGUID = user.GUID,
                Name = "Default credential",
                BearerToken = "default",
                Scopes = new List<string> { "admin" },
                Active = true,
                CreatedUtc = DateTime.UtcNow
            };

            if (!await _Repo.Credential.ExistsByGuid(cred.TenantGUID, cred.GUID, CancellationToken.None).ConfigureAwait(false))
            {
                cred = await _Repo.Credential.Create(cred, CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("| Created credential : " + cred.GUID + " bearer token: " + OperationalLogRedactor.RedactValue(cred.BearerToken));
            }

            Graph graph = new Graph
            {
                GUID = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                TenantGUID = tenant.GUID,
                Name = "Default graph",
                CreatedUtc = DateTime.UtcNow
            };

            if (!await _Repo.Graph.ExistsByGuid(graph.TenantGUID, graph.GUID, CancellationToken.None).ConfigureAwait(false))
            {
                graph = await _Repo.Graph.Create(graph, CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("| Created graph      : " + graph.GUID + " " + graph.Name);
            }

            int nodeCount = await _Repo.Node.GetRecordCount(tenant.GUID, graph.GUID, token: CancellationToken.None).ConfigureAwait(false);
            int edgeCount = await _Repo.Edge.GetRecordCount(tenant.GUID, graph.GUID, token: CancellationToken.None).ConfigureAwait(false);
            if (nodeCount < 1 || edgeCount < 1)
            {
                await CreateDefaultGraphRecords(tenant.GUID, graph.GUID).ConfigureAwait(false);
            }

            #endregion

            Console.WriteLine("Finished creating default records");
        }

        private static async Task CreateDefaultGraphRecords(Guid tenantGuid, Guid graphGuid)
        {
            DateTime now = DateTime.UtcNow;
            Guid serverGuid = Guid.Parse("10000000-0000-0000-0000-000000000001");
            Guid storageGuid = Guid.Parse("10000000-0000-0000-0000-000000000002");
            Guid dashboardGuid = Guid.Parse("10000000-0000-0000-0000-000000000003");

            List<Node> nodes = new List<Node>
            {
                new Node
                {
                    GUID = serverGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "LiteGraph Server",
                    Labels = new List<string> { "Service" },
                    Tags = Tags(("role", "api"), ("state", "ready")),
                    Data = new Dictionary<string, object> { { "description", "Default LiteGraph API service node." } },
                    CreatedUtc = now,
                    LastUpdateUtc = now
                },
                new Node
                {
                    GUID = storageGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "PostgreSQL Storage",
                    Labels = new List<string> { "Storage" },
                    Tags = Tags(("provider", "postgresql"), ("state", "ready")),
                    Data = new Dictionary<string, object> { { "description", "Default PostgreSQL storage node." } },
                    CreatedUtc = now,
                    LastUpdateUtc = now
                },
                new Node
                {
                    GUID = dashboardGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "LiteGraph Dashboard",
                    Labels = new List<string> { "Application" },
                    Tags = Tags(("role", "dashboard"), ("state", "ready")),
                    Data = new Dictionary<string, object> { { "description", "Default dashboard application node." } },
                    CreatedUtc = now,
                    LastUpdateUtc = now
                }
            };

            foreach (Node node in nodes)
            {
                if (!await _Repo.Node.ExistsByGuid(tenantGuid, node.GUID, CancellationToken.None).ConfigureAwait(false))
                {
                    await _Repo.Node.Create(node, CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("| Created node       : " + node.GUID + " " + node.Name);
                }
            }

            List<Edge> edges = new List<Edge>
            {
                new Edge
                {
                    GUID = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "PERSISTS_TO",
                    From = serverGuid,
                    To = storageGuid,
                    Cost = 1,
                    Labels = new List<string> { "Storage" },
                    Tags = Tags(("path", "database")),
                    Data = new Dictionary<string, object> { { "description", "LiteGraph persists graph data to PostgreSQL." } },
                    CreatedUtc = now,
                    LastUpdateUtc = now
                },
                new Edge
                {
                    GUID = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "MANAGES",
                    From = dashboardGuid,
                    To = serverGuid,
                    Cost = 1,
                    Labels = new List<string> { "Dashboard" },
                    Tags = Tags(("path", "rest")),
                    Data = new Dictionary<string, object> { { "description", "The dashboard manages LiteGraph through the REST API." } },
                    CreatedUtc = now,
                    LastUpdateUtc = now
                }
            };

            foreach (Edge edge in edges)
            {
                if (!await _Repo.Edge.ExistsByGuid(tenantGuid, edge.GUID, CancellationToken.None).ConfigureAwait(false))
                {
                    await _Repo.Edge.Create(edge, CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("| Created edge       : " + edge.GUID + " " + edge.Name);
                }
            }
        }

        private static NameValueCollection Tags(params (string Key, string Value)[] tags)
        {
            NameValueCollection ret = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
            foreach ((string key, string value) in tags)
            {
                ret.Add(key, value);
            }

            return ret;
        }

        #endregion
    }
}
