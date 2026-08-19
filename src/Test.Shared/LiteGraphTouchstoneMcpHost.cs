namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.Threading.Tasks;
    using Voltaic;

    public static partial class LiteGraphTouchstoneSuites
    {
        private static readonly HttpClient _ReadinessClient = CreateReadinessClient();
        private static readonly TimeSpan _ProcessStartupTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan _ReadinessPollInterval = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan _StartupRetryDelay = TimeSpan.FromSeconds(1);
        private const int _StartupAttemptLimit = 3;
        private static McpProcessEnvironment? _McpEnvironment = null;

        private static HttpClient CreateReadinessClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            return client;
        }

        private static async Task EnsureMcpEnvironmentAsync(CancellationToken cancellationToken = default, string apiKey = "litegraphadmin")
        {
            if (_McpEnvironment != null)
            {
                if (!String.Equals(_McpEnvironment.ApiKey, apiKey, StringComparison.Ordinal))
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
                else
                {
                    EnsureProcessIsRunning(_McpEnvironment.LiteGraphProcess);
                    EnsureProcessIsRunning(_McpEnvironment.McpProcess);
                    return;
                }
            }

            List<string> startupFailures = new List<string>();

            for (int attempt = 1; attempt <= _StartupAttemptLimit; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                McpProcessEnvironment environment = CreateMcpProcessEnvironment(apiKey);
                _McpEnvironment = environment;

                try
                {
                    await StartMcpEnvironmentAsync(environment, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    startupFailures.Add("Attempt " + attempt + ": " + ex.Message);
                    await CleanupMcpServer(deleteArtifacts: attempt < _StartupAttemptLimit).ConfigureAwait(false);

                    if (attempt < _StartupAttemptLimit)
                    {
                        await Task.Delay(_StartupRetryDelay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            throw new InvalidOperationException(
                "Unable to start the LiteGraph MCP test environment after "
                + _StartupAttemptLimit
                + " attempts."
                + Environment.NewLine
                + String.Join(Environment.NewLine + Environment.NewLine, startupFailures));
        }

        private static async Task StartMcpEnvironmentAsync(
            McpProcessEnvironment environment,
            CancellationToken cancellationToken)
        {
            environment.LiteGraphProcess = StartDotnetProcess(
                displayName: "LiteGraph.Server",
                assemblyPath: environment.LiteGraphAssemblyPath,
                workingDirectory: environment.LiteGraphWorkingDirectory,
                environmentVariables: new Dictionary<string, string>
                {
                    { "LITEGRAPH_PORT", environment.LiteGraphPort.ToString() },
                    { "LITEGRAPH_DB", environment.DatabasePath }
                });

            await WaitForLiteGraphServerAsync(environment, cancellationToken).ConfigureAwait(false);

            environment.McpProcess = StartDotnetProcess(
                displayName: "LiteGraph.McpServer",
                assemblyPath: environment.McpAssemblyPath,
                workingDirectory: environment.McpWorkingDirectory,
                environmentVariables: new Dictionary<string, string>
                {
                    { "LITEGRAPH_ENDPOINT", environment.LiteGraphEndpoint },
                    { "LITEGRAPH_API_KEY", environment.ApiKey },
                    { "MCP_HTTP_HOSTNAME", "127.0.0.1" },
                    { "MCP_HTTP_PORT", environment.McpHttpPort.ToString() },
                    { "MCP_TCP_ADDRESS", "127.0.0.1" },
                    { "MCP_TCP_PORT", environment.McpTcpPort.ToString() },
                    { "MCP_WS_HOSTNAME", "127.0.0.1" },
                    { "MCP_WS_PORT", environment.McpWebSocketPort.ToString() },
                    { "MCP_METRICS_HOSTNAME", "127.0.0.1" },
                    { "MCP_METRICS_PORT", environment.McpMetricsPort.ToString() },
                    { "MCP_CONSOLE_LOGGING", "1" }
                });

            _McpClient = await ConnectMcpClientWithRetryAsync(environment, cancellationToken).ConfigureAwait(false);
        }

        private static McpProcessEnvironment CreateMcpProcessEnvironment(string apiKey)
        {
            HashSet<int> reservedPorts = new HashSet<int>();
            int liteGraphPort = ReserveAvailablePort(reservedPorts);
            int mcpHttpPort = ReserveAvailablePort(reservedPorts);
            int mcpTcpPort = ReserveAvailablePort(reservedPorts);
            int mcpWebSocketPort = ReserveAvailablePort(reservedPorts);
            int mcpMetricsPort = ReserveAvailablePort(reservedPorts);

            string artifactDirectory = Path.Combine(
                Path.GetTempPath(),
                "LiteGraph.Touchstone",
                "McpHost",
                DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Guid.NewGuid().ToString("N"));

            string liteGraphWorkingDirectory = Path.Combine(artifactDirectory, "litegraph-server");
            string mcpWorkingDirectory = Path.Combine(artifactDirectory, "litegraph-mcp");

            Directory.CreateDirectory(liteGraphWorkingDirectory);
            Directory.CreateDirectory(mcpWorkingDirectory);

            string configuration = GetCurrentBuildConfiguration();
            string targetFramework = GetCurrentTargetFrameworkMoniker();

            return new McpProcessEnvironment
            {
                ArtifactDirectory = artifactDirectory,
                LiteGraphWorkingDirectory = liteGraphWorkingDirectory,
                McpWorkingDirectory = mcpWorkingDirectory,
                DatabasePath = Path.Combine(liteGraphWorkingDirectory, "litegraph.db"),
                LiteGraphAssemblyPath = ResolveBuildOutput("LiteGraph.Server", configuration, targetFramework, "LiteGraph.Server.dll"),
                McpAssemblyPath = ResolveBuildOutput("LiteGraph.McpServer", configuration, targetFramework, "LiteGraph.McpServer.dll"),
                ApiKey = apiKey,
                LiteGraphPort = liteGraphPort,
                McpHttpPort = mcpHttpPort,
                McpTcpPort = mcpTcpPort,
                McpWebSocketPort = mcpWebSocketPort,
                McpMetricsPort = mcpMetricsPort
            };
        }

        private static ManagedProcess StartDotnetProcess(
            string displayName,
            string assemblyPath,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environmentVariables)
        {
            Directory.CreateDirectory(workingDirectory);

            ProcessLogCapture capture = new ProcessLogCapture(
                Path.Combine(
                    workingDirectory,
                    displayName.Replace('.', '_') + ".log"));

            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add(assemblyPath);

            foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
            {
                startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
            }

            Process process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, args) => capture.Append("stdout", args.Data);
            process.ErrorDataReceived += (_, args) => capture.Append("stderr", args.Data);

            if (!process.Start())
            {
                capture.Dispose();
                throw new InvalidOperationException("Unable to start " + displayName);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return new ManagedProcess(displayName, process, capture);
        }

        private static async Task WaitForLiteGraphServerAsync(
            McpProcessEnvironment environment,
            CancellationToken cancellationToken)
        {
            await WaitForHttpSuccessAsync(
                displayName: "LiteGraph.Server",
                endpoint: environment.LiteGraphEndpoint,
                managedProcess: environment.LiteGraphProcess,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static async Task<McpHttpClient> ConnectMcpClientWithRetryAsync(
            McpProcessEnvironment environment,
            CancellationToken cancellationToken)
        {
            DateTime timeoutAt = DateTime.UtcNow.Add(_ProcessStartupTimeout);
            Exception? lastException = null;

            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnsureProcessIsRunning(environment.McpProcess);

                McpHttpClient client = new McpHttpClient();
                ConfigureMcpHttpClient(client, 30000);

                try
                {
                    bool connected = await client.ConnectAsync(
                        environment.McpHttpEndpoint,
                        "/rpc",
                        "/events",
                        cancellationToken).ConfigureAwait(false);

                    if (connected)
                    {
                        return client;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                client.Dispose();
                await Task.Delay(_ReadinessPollInterval, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                BuildProcessFailureMessage(
                    "Timed out waiting for LiteGraph.McpServer to accept MCP HTTP connections at " + environment.McpHttpEndpoint,
                    environment,
                    environment.McpProcess,
                    lastException));
        }

        private static async Task WaitForHttpSuccessAsync(
            string displayName,
            string endpoint,
            ManagedProcess? managedProcess,
            CancellationToken cancellationToken)
        {
            DateTime timeoutAt = DateTime.UtcNow.Add(_ProcessStartupTimeout);
            Exception? lastException = null;

            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (managedProcess != null)
                {
                    EnsureProcessIsRunning(managedProcess);
                }

                try
                {
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    using HttpResponseMessage response = await _ReadinessClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    lastException = new InvalidOperationException(
                        displayName + " returned HTTP " + (int)response.StatusCode + " from " + endpoint);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(_ReadinessPollInterval, cancellationToken).ConfigureAwait(false);
            }

            if (_McpEnvironment != null)
            {
                throw new TimeoutException(
                    BuildProcessFailureMessage(
                        "Timed out waiting for " + displayName + " at " + endpoint,
                        _McpEnvironment,
                        managedProcess,
                        lastException));
            }

            throw new TimeoutException("Timed out waiting for " + displayName + " at " + endpoint);
        }

        private static void ConfigureMcpHttpClient(McpHttpClient client, int timeoutMs)
        {
            client.RequestTimeoutMs = timeoutMs;

            FieldInfo? httpClientField = typeof(McpHttpClient).GetField(
                "_HttpClient",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (httpClientField?.GetValue(client) is HttpClient httpClient)
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            }
        }

        private static void EnsureProcessIsRunning(ManagedProcess? managedProcess)
        {
            if (managedProcess == null)
            {
                throw new InvalidOperationException("Managed process has not been started");
            }

            if (managedProcess.Process.HasExited)
            {
                string message = managedProcess.DisplayName
                    + " exited with code "
                    + managedProcess.Process.ExitCode
                    + Environment.NewLine
                    + "Log file: "
                    + managedProcess.Capture.LogFilePath
                    + Environment.NewLine
                    + managedProcess.Capture.GetRecentOutput();

                throw new InvalidOperationException(message.Trim());
            }
        }

        private static async Task StopManagedProcessAsync(ManagedProcess? managedProcess)
        {
            if (managedProcess == null)
            {
                return;
            }

            try
            {
                if (!managedProcess.Process.HasExited)
                {
                    managedProcess.Process.Kill(entireProcessTree: true);

                    try
                    {
                        using CancellationTokenSource waitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        await managedProcess.Process.WaitForExitAsync(waitTimeout.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            finally
            {
                managedProcess.Capture.Dispose();
                managedProcess.Process.Dispose();
            }
        }

        private static string ResolveBuildOutput(
            string projectName,
            string configuration,
            string targetFramework,
            string assemblyFileName)
        {
            string assemblyPath = ResolveRepositoryFile(
                "src",
                projectName,
                "bin",
                configuration,
                targetFramework,
                assemblyFileName);

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Unable to locate build output for " + projectName, assemblyPath);
            }

            return assemblyPath;
        }

        private static int ReserveAvailablePort(HashSet<int> reservedPorts)
        {
            while (true)
            {
                int candidate;
                using (TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 0))
                {
                    listener.Start();
                    candidate = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                }

                if (reservedPorts.Add(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string GetCurrentBuildConfiguration()
        {
            string baseDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            DirectoryInfo? frameworkDirectory = new DirectoryInfo(baseDirectory);
            DirectoryInfo? configurationDirectory = frameworkDirectory.Parent;

            if (configurationDirectory == null)
            {
                throw new InvalidOperationException("Unable to determine build configuration from " + AppContext.BaseDirectory);
            }

            return configurationDirectory.Name;
        }

        private static string GetCurrentTargetFrameworkMoniker()
        {
            string baseDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            string candidate = new DirectoryInfo(baseDirectory).Name;

            if (candidate.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            if (!String.IsNullOrEmpty(AppContext.TargetFrameworkName))
            {
                FrameworkName frameworkName = new FrameworkName(AppContext.TargetFrameworkName);
                return "net" + frameworkName.Version.Major + "." + frameworkName.Version.Minor;
            }

            throw new InvalidOperationException("Unable to determine target framework from " + AppContext.BaseDirectory);
        }

        private static string BuildProcessFailureMessage(
            string message,
            McpProcessEnvironment environment,
            ManagedProcess? managedProcess,
            Exception? exception = null)
        {
            List<string> parts = new List<string>
            {
                message,
                "Artifacts: " + environment.ArtifactDirectory
            };

            if (managedProcess != null)
            {
                parts.Add("Log file: " + managedProcess.Capture.LogFilePath);

                string recentOutput = managedProcess.Capture.GetRecentOutput();
                if (!String.IsNullOrEmpty(recentOutput))
                {
                    parts.Add("Recent output:");
                    parts.Add(recentOutput);
                }
            }

            if (exception != null && !String.IsNullOrEmpty(exception.Message))
            {
                parts.Add("Last error: " + exception.Message);
            }

            return String.Join(Environment.NewLine, parts);
        }

        private sealed class McpProcessEnvironment
        {
            public string ArtifactDirectory { get; init; } = String.Empty;
            public string LiteGraphWorkingDirectory { get; init; } = String.Empty;
            public string McpWorkingDirectory { get; init; } = String.Empty;
            public string DatabasePath { get; init; } = String.Empty;
            public string LiteGraphAssemblyPath { get; init; } = String.Empty;
            public string McpAssemblyPath { get; init; } = String.Empty;
            public string ApiKey { get; init; } = String.Empty;
            public int LiteGraphPort { get; init; }
            public int McpHttpPort { get; init; }
            public int McpTcpPort { get; init; }
            public int McpWebSocketPort { get; init; }
            public int McpMetricsPort { get; init; }
            public ManagedProcess? LiteGraphProcess { get; set; }
            public ManagedProcess? McpProcess { get; set; }

            public string LiteGraphEndpoint
            {
                get { return "http://127.0.0.1:" + LiteGraphPort; }
            }

            public string McpHttpEndpoint
            {
                get { return "http://127.0.0.1:" + McpHttpPort; }
            }

            public string McpMetricsEndpoint
            {
                get { return "http://127.0.0.1:" + McpMetricsPort; }
            }
        }

        private sealed class ManagedProcess
        {
            public ManagedProcess(string displayName, Process process, ProcessLogCapture capture)
            {
                DisplayName = displayName;
                Process = process;
                Capture = capture;
            }

            public string DisplayName { get; }
            public Process Process { get; }
            public ProcessLogCapture Capture { get; }
        }

        private sealed class ProcessLogCapture : IDisposable
        {
            private readonly object _sync = new object();
            private readonly Queue<string> _recentLines = new Queue<string>();
            private readonly StreamWriter _writer;

            public ProcessLogCapture(string logFilePath)
            {
                LogFilePath = logFilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
                _writer = new StreamWriter(
                    new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    AutoFlush = true
                };
            }

            public string LogFilePath { get; }

            public void Append(string streamName, string? line)
            {
                if (String.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                string entry = "[" + DateTime.UtcNow.ToString("O") + "] " + streamName + ": " + line;

                lock (_sync)
                {
                    _writer.WriteLine(entry);
                    _recentLines.Enqueue(entry);

                    while (_recentLines.Count > 80)
                    {
                        _recentLines.Dequeue();
                    }
                }
            }

            public string GetRecentOutput()
            {
                lock (_sync)
                {
                    return String.Join(Environment.NewLine, _recentLines);
                }
            }

            public void Dispose()
            {
                lock (_sync)
                {
                    _writer.Dispose();
                }
            }
        }
    }
}
