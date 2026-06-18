namespace LiteGraph.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.API.REST;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
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

        private static Settings _Settings = new Settings();
        private static LoggingModule _Logging = null;
        private static Serializer _Serializer = new Serializer();

        private static GraphRepositoryBase _Repo = null;
        private static LiteGraphClient _LiteGraph = null;

        private static ServiceHandler _ServiceHandler = null;
        private static AuthenticationService _AuthenticationService = null;
        private static RequestHistoryService _RequestHistoryService = null;
        private static ObservabilityService _ObservabilityService = null;
        private static RestServiceHandler _RestService = null;

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
            try
            {
                RegisterShutdownHandlers();

                Welcome();
                ParseArguments(args);
                InitializeSettings();
                await InitializeGlobals().ConfigureAwait(false);

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
            ApplyObservabilityEnvironmentVariables();

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

            _Logging.Info(_Header + "initializing graph repository: " + _Settings.LiteGraph.Database.ToSafeString());
            _Repo = GraphRepositoryFactory.Create(_Settings.LiteGraph.Database);
            _Repo.InitializeRepository();

            #endregion

            #region Create-Default-Records

            if (_CreateDefaultRecords) await CreateDefaultRecords().ConfigureAwait(false);

            #endregion

            #region LiteGraph-Client

            _LiteGraph = new LiteGraphClient(_Repo, _Settings.Logging);
            _LiteGraph.Caching = _Settings.Caching;
            _LiteGraph.Logging.Enable = _Settings.Debug.DatabaseQueries;
            _LiteGraph.Logging.Logger = LiteGraphLogger;
            _LiteGraph.Logging.LogQueries = _Settings.Debug.DatabaseQueries;
            _LiteGraph.Logging.LogResults = _Settings.Debug.DatabaseQueries;

            _LiteGraph.InitializeRepository();

            #endregion

            #region Services

            _AuthenticationService = new AuthenticationService(
                _Settings,
                _Logging,
                _Serializer,
                _Repo);

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

            _ObservabilityService = new ObservabilityService(_Settings.Observability);
            _ObservabilityService.RecordStorageBackend(
                _Settings.LiteGraph.Database.Type.ToString(),
                _Settings.LiteGraph.Database.Type == DatabaseTypeEnum.Postgresql);
            _ObservabilityService.RecordStorageConnectionPool(
                _Settings.LiteGraph.Database.Type.ToString(),
                _Settings.LiteGraph.Database.MaxConnections,
                _Settings.LiteGraph.Database.CommandTimeoutSeconds);

            _RestService = new RestServiceHandler(
                _Settings,
                _Logging,
                _LiteGraph,
                _Serializer,
                _AuthenticationService,
                _ServiceHandler,
                _RequestHistoryService,
                _ObservabilityService);

            #endregion
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
                CreatedUtc = DateTime.UtcNow
            };

            if (!await _Repo.User.ExistsByGuid(tenant.GUID, user.GUID, CancellationToken.None).ConfigureAwait(false))
            {
                user = await _Repo.User.Create(user, CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("| Created user       : " + user.GUID + " email: " + user.Email + " pass: " + OperationalLogRedactor.RedactValue(user.Password));
            }

            Credential cred = new Credential
            {
                GUID = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                TenantGUID = tenant.GUID,
                UserGUID = user.GUID,
                Name = "Default credential",
                BearerToken = "default",
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

            #endregion

            Console.WriteLine("Finished creating default records");
        }

        #endregion
    }
}
