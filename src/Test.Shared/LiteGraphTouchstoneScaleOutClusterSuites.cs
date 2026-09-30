namespace Test.Shared
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Coordination;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using LiteGraph.Server.Services.Cluster;
    using SyslogLogging;

    /// <summary>
    /// Coverage for cluster settings handling and the Redis node registry: the settings file service, registry heartbeats
    /// and signals, and the rolling restart coordinator.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        private const string RedisTestConnectionStringEnvironmentVariable = "LITEGRAPH_TEST_REDIS_CONNECTION_STRING";

        private static Task ExecuteScaleOutClientAddressAsync(CancellationToken token)
        {
            ClientAddressResolver off = new ClientAddressResolver(false, new[] { "10.0.0.0/8" });
            AssertEqual("10.0.0.5", off.Resolve("10.0.0.5", "203.0.113.9"), "Forwarded headers are ignored when trust is off");

            ClientAddressResolver resolver = new ClientAddressResolver(true, new[] { "10.0.0.0/8", "192.168.1.7", "fd00::/8" });
            AssertEqual("203.0.113.9", resolver.Resolve("10.1.2.3", "203.0.113.9"), "A trusted proxy's forwarded client is used");
            AssertEqual("198.51.100.4", resolver.Resolve("10.1.2.3", "203.0.113.9, 198.51.100.4, 10.9.9.9"), "The rightmost untrusted hop is the client");
            AssertEqual("172.20.0.9", resolver.Resolve("172.20.0.9", "203.0.113.9"), "An untrusted peer cannot spoof its address");
            AssertEqual("203.0.113.9", resolver.Resolve("::ffff:10.1.2.3", "203.0.113.9"), "IPv4-mapped IPv6 peers match IPv4 ranges");
            AssertEqual("192.168.1.7", resolver.Resolve("192.168.1.7", "not-an-address"), "A malformed header falls back to the peer");
            AssertEqual("10.2.2.2", resolver.Resolve("10.1.2.3", "10.2.2.2"), "When every hop is trusted the leftmost is used");
            AssertTrue(resolver.IsTrusted("fd12::1") && !resolver.IsTrusted("192.168.1.8"), "Single addresses and IPv6 ranges are matched exactly");
            AssertSyncThrows<ArgumentException>(() => new ClientAddressResolver(true, new[] { "10.0.0.0/33" }), "An out-of-range prefix is rejected");
            AssertSyncThrows<ArgumentException>(() => new ClientAddressResolver(true, new[] { "proxy.local" }), "A host name is rejected");
            return Task.CompletedTask;
        }

        private static async Task ExecuteScaleOutRequestHistoryNodeAsync(CancellationToken token)
        {
            Directory.CreateDirectory(_ScaleOutArtifactDirectory);
            string databasePath = Path.Combine(_ScaleOutArtifactDirectory, "request-history-node-" + Guid.NewGuid().ToString("N") + ".db");
            using (SqliteGraphRepository repo = new SqliteGraphRepository(databasePath, false))
            {
                repo.InitializeRepository();
                foreach (string node in new[] { "litegraph-1", "litegraph-2", "litegraph-2" })
                {
                    await repo.RequestHistory.Insert(new RequestHistoryDetail
                    {
                        GUID = Guid.NewGuid(),
                        CreatedUtc = DateTime.UtcNow,
                        CompletedUtc = DateTime.UtcNow,
                        Method = "GET",
                        Path = "/v1.0/tenants",
                        Url = "http://127.0.0.1/v1.0/tenants",
                        SourceIp = "127.0.0.1",
                        NodeId = node,
                        StatusCode = 200,
                        Success = true
                    }, token).ConfigureAwait(false);
                }

                RequestHistorySearchResult two = await repo.RequestHistory.Search(new RequestHistorySearchRequest { NodeId = "litegraph-2" }, token).ConfigureAwait(false);
                AssertEqual(2, two.Objects.Count, "The node filter returns only that node's requests");
                AssertTrue(two.Objects.All(o => o.NodeId == "litegraph-2"), "Each record carries the node that handled it");

                RequestHistoryEntry one = await repo.RequestHistory.ReadByGuid(two.Objects[0].GUID, token).ConfigureAwait(false);
                AssertEqual("litegraph-2", one.NodeId, "A single read returns the node");
            }
        }

        private static async Task ExecuteScaleOutSettingsFileAsync(CancellationToken token)
        {
            Directory.CreateDirectory(_ScaleOutArtifactDirectory);
            string path = Path.Combine(_ScaleOutArtifactDirectory, "settings-" + Guid.NewGuid().ToString("N") + ".json");
            Serializer serializer = new Serializer();

            Settings file = new Settings();
            file.RequestTimeoutSeconds = 30;
            file.Logging.LogDirectory = "./logs/";
            file.Logging.LogFilename = "litegraph.log";
            file.Encryption.Key = new string('0', 64);
            file.Cluster.ClusterName = "file-cluster";
            File.WriteAllText(path, serializer.SerializeJson(file, true));

            // Simulate the server's startup: environment overrides and values derived at startup.
            Settings running = serializer.DeserializeJson<Settings>(File.ReadAllText(path));
            running.Encryption.Key = new string('a', 64);
            running.Cluster.NodeId = "litegraph-2";
            running.Cluster.Clutch.AccessKey = "env-secret";
            running.Logging.LogFilename = running.Logging.LogDirectory + running.Logging.LogFilename;

            SettingsFileService service = new SettingsFileService(path, serializer, running);
            List<string> overridden = service.OverriddenPaths.ToList();
            AssertTrue(overridden.Contains("Encryption.Key"), "Environment-supplied encryption key is detected");
            AssertTrue(overridden.Contains("Cluster.NodeId"), "Environment-supplied node identifier is detected");
            AssertTrue(overridden.Contains("Cluster.Clutch.AccessKey"), "Environment-supplied Clutch key is detected");
            AssertTrue(overridden.Contains("Logging.LogFilename"), "Log file name derived at startup is detected");
            AssertTrue(!overridden.Contains("RequestTimeoutSeconds"), "A value taken from the file is not reported as overridden");
            AssertTrue(service.IsOverridden("Encryption", "Key") && !service.IsOverridden("RequestTimeoutSeconds"), "IsOverridden matches the detected paths");

            Settings read = service.Read();
            AssertEqual(new string('0', 64), read.Encryption.Key, "Read returns the file, not the running settings");
            AssertTrue(String.IsNullOrEmpty(read.Cluster.NodeId), "Read does not return this node's identifier");
            AssertTrue(!service.RestartNeeded(), "No restart is needed before any save");

            // A save built from the running settings, as a pre-10.0 dashboard would send, plus one live and one restart change.
            Settings incoming = serializer.DeserializeJson<Settings>(serializer.SerializeJson(running, false));
            incoming.RequestTimeoutSeconds = 45;
            await service.WriteAsync(incoming, token).ConfigureAwait(false);

            Settings written = service.Read();
            AssertEqual(45, written.RequestTimeoutSeconds, "A value from the file is saved");
            AssertEqual(new string('0', 64), written.Encryption.Key, "The environment-supplied key is not written to the file");
            AssertTrue(String.IsNullOrEmpty(written.Cluster.NodeId), "The node identifier is not written to the shared file");
            AssertTrue(String.IsNullOrEmpty(written.Cluster.Clutch.AccessKey), "The environment-supplied Clutch key is not written to the file");
            AssertEqual("litegraph.log", written.Logging.LogFilename, "The derived log file name is not written back, so the directory is not prefixed again");
            AssertTrue(!service.RestartNeeded(), "A change to a live setting needs no restart (changed: " + String.Join(", ", service.RestartRequiredChanges()) + ")");

            incoming.Logging.MinimumSeverity = 3;
            await service.WriteAsync(incoming, token).ConfigureAwait(false);
            AssertTrue(service.RestartNeeded(), "A change to a restart-required setting needs a restart");
        }

        private static async Task ExecuteScaleOutClusterRegistryAsync(CancellationToken token)
        {
            string clusterName = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);

            using (ScaleOutClusterNode a = ScaleOutClusterNode.Create(clusterName, "node-a", new LocalLockProvider()))
            using (ScaleOutClusterNode b = ScaleOutClusterNode.Create(clusterName, "node-b", new LocalLockProvider()))
            {
                await a.StartAsync(token).ConfigureAwait(false);
                await b.StartAsync(token).ConfigureAwait(false);
                AssertTrue(a.Registry.IsAvailable && b.Registry.IsAvailable, "Both nodes reach Redis");

                ClusterStatus status = await a.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertEqual(2, status.Nodes.Count, "Both nodes are registered");
                AssertTrue(status.Nodes.All(n => n.State == ClusterNodeStateEnum.Healthy && n.Checks.Database && n.Checks.Redis == true), "Both nodes report healthy");
                AssertEqual("node-a", status.Nodes[0].NodeId, "Nodes are ordered by identifier");
                AssertEqual(clusterName, status.ClusterName, "Status names the cluster");

                long settingsVersion = await b.Registry.SignalSettingsChangedAsync(token).ConfigureAwait(false);
                AssertTrue(settingsVersion == 1, "The first settings change is version 1");
                await ScaleOutWaitForAsync(() => a.SettingsChanges.Contains(1) && b.SettingsChanges.Contains(1), 5000, "Every node, including the one that saved, notices the settings change", token).ConfigureAwait(false);

                long restartVersion = await a.Registry.RequestRestartAsync(token).ConfigureAwait(false);
                await ScaleOutWaitForAsync(() => a.RestartRequests.Contains(restartVersion) && b.RestartRequests.Contains(restartVersion), 5000, "Every node notices the restart request", token).ConfigureAwait(false);

                status = await a.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertTrue(status.Nodes.All(n => n.RestartPending), "Nodes waiting to restart report a pending restart");
                AssertEqual(restartVersion, status.RestartVersion, "Status reports the restart version");
                AssertTrue(status.RestartRequestedUtc.HasValue && status.SettingsUpdatedUtc.HasValue, "Status reports when settings changed and when the restart was requested");

                using (ScaleOutClusterNode late = ScaleOutClusterNode.Create(clusterName, "node-c", new LocalLockProvider()))
                {
                    await late.StartAsync(token).ConfigureAwait(false);
                    await Task.Delay(1500, token).ConfigureAwait(false);
                    AssertTrue(late.RestartRequests.Count == 0, "A node started after the restart request does not restart");
                    AssertTrue(late.SettingsChanges.Count == 0, "A node started after the settings change is not told about it");
                }

                status = await a.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertEqual(ClusterNodeStateEnum.Stopped, status.Nodes.Single(n => n.NodeId == "node-c").State, "A node that shut down cleanly is reported stopped");

                await b.Registry.PublishStateAsync(ClusterNodeStateEnum.Draining, token).ConfigureAwait(false);
                status = await a.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertEqual(ClusterNodeStateEnum.Draining, status.Nodes.Single(n => n.NodeId == "node-b").State, "A published state is visible to other nodes immediately");
            }
        }

        private static async Task ExecuteScaleOutNodeRestartAsync(CancellationToken token)
        {
            string clusterName = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);

            using (ScaleOutClusterNode x = ScaleOutClusterNode.Create(clusterName, "node-x", new LocalLockProvider()))
            using (ScaleOutClusterNode y = ScaleOutClusterNode.Create(clusterName, "node-y", new LocalLockProvider()))
            {
                await x.StartAsync(token).ConfigureAwait(false);
                await y.StartAsync(token).ConfigureAwait(false);

                await x.Registry.RequestNodeRestartAsync("node-y", token).ConfigureAwait(false);
                await ScaleOutWaitForAsync(() => y.RestartRequests.Count == 1, 5000, "The addressed node notices its restart request", token).ConfigureAwait(false);
                await Task.Delay(1500, token).ConfigureAwait(false);
                AssertTrue(x.RestartRequests.Count == 0, "Other nodes do not restart");

                ScaleOutClusterNode z = ScaleOutClusterNode.Create(clusterName, "node-z", new LocalLockProvider());
                await z.StartAsync(token).ConfigureAwait(false);
                z.Dispose();
                ClusterStatus status = await x.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertEqual(ClusterNodeStateEnum.Stopped, status.Nodes.Single(n => n.NodeId == "node-z").State, "The stopped node is listed");

                AssertTrue(await x.Registry.RemoveNodeAsync("node-z", token).ConfigureAwait(false), "A stopped node's entry is removed");
                status = await x.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertTrue(status.Nodes.All(n => n.NodeId != "node-z"), "The removed node is no longer listed");
                AssertTrue(!await x.Registry.RemoveNodeAsync("node-z", token).ConfigureAwait(false), "Removing an absent entry reports false");
            }
        }

        private static async Task ExecuteScaleOutRollingRestartAsync(CancellationToken token)
        {
            string clusterName = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            LocalLockProvider sharedLocks = new LocalLockProvider();
            ConcurrentQueue<string> exits = new ConcurrentQueue<string>();

            ScaleOutClusterNode a = ScaleOutClusterNode.Create(clusterName, "node-a", sharedLocks, exits);
            ScaleOutClusterNode b = ScaleOutClusterNode.Create(clusterName, "node-b", sharedLocks, exits);
            ScaleOutClusterNode? restarted = null;

            try
            {
                await a.StartAsync(token).ConfigureAwait(false);
                await b.StartAsync(token).ConfigureAwait(false);

                await a.Registry.RequestRestartAsync(token).ConfigureAwait(false);
                await ScaleOutWaitForAsync(() => exits.Count >= 1, 10000, "One node takes its turn and exits", token).ConfigureAwait(false);
                exits.TryPeek(out string? first);
                ScaleOutClusterNode firstNode = first == "node-a" ? a : b;
                ScaleOutClusterNode secondNode = first == "node-a" ? b : a;

                ClusterStatus status = await secondNode.Registry.GetStatusAsync(token).ConfigureAwait(false);
                AssertEqual(ClusterNodeStateEnum.Restarting, status.Nodes.Single(n => n.NodeId == first).State, "The exiting node reports Restarting");

                await Task.Delay(2500, token).ConfigureAwait(false);
                AssertEqual(1, exits.Count, "The second node waits while the first is restarting");

                // The first node comes back as a new process.
                firstNode.Dispose();
                restarted = ScaleOutClusterNode.Create(clusterName, first!, sharedLocks, exits);
                await restarted.StartAsync(token).ConfigureAwait(false);

                await ScaleOutWaitForAsync(() => exits.Count >= 2, 10000, "The second node restarts once the first is healthy again", token).ConfigureAwait(false);
                AssertEqual(secondNode.NodeId, exits.ToArray()[1], "The other node restarts second");
                await Task.Delay(1500, token).ConfigureAwait(false);
                AssertEqual(2, exits.Count, "The restarted node does not restart again");
            }
            finally
            {
                a.Dispose();
                b.Dispose();
                restarted?.Dispose();
                sharedLocks.Dispose();
            }
        }

        private static async Task ScaleOutWaitForAsync(Func<bool> condition, int timeoutMs, string message, CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline) AssertTrue(false, message + " (timed out after " + timeoutMs + "ms)");
                await Task.Delay(100, token).ConfigureAwait(false);
            }
            AssertTrue(true, message);
        }

        /// <summary>
        /// An in-process stand-in for one cluster node: a SQLite client for the health check, a cluster context, a node
        /// registry against the test Redis, and a rolling restart coordinator whose exit is recorded instead of performed.
        /// </summary>
        private sealed class ScaleOutClusterNode : IDisposable
        {
            internal string NodeId { get; }

            internal ClusterRegistry Registry { get; }

            internal ConcurrentBag<long> SettingsChanges { get; } = new ConcurrentBag<long>();

            internal ConcurrentBag<long> RestartRequests { get; } = new ConcurrentBag<long>();

            private readonly LiteGraphClient _Client;
            private readonly LoggingModule _Logging;
            private readonly RollingRestartCoordinator _Coordinator;
            private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
            private bool _Disposed = false;

            private ScaleOutClusterNode(string nodeId, LiteGraphClient client, LoggingModule logging, ClusterRegistry registry, RollingRestartCoordinator coordinator)
            {
                NodeId = nodeId;
                _Client = client;
                _Logging = logging;
                Registry = registry;
                _Coordinator = coordinator;
            }

            internal static ScaleOutClusterNode Create(string clusterName, string nodeId, ILockProvider locks, ConcurrentQueue<string>? exits = null)
            {
                Directory.CreateDirectory(_ScaleOutArtifactDirectory);
                string databasePath = Path.Combine(_ScaleOutArtifactDirectory, "cluster-" + nodeId + "-" + Guid.NewGuid().ToString("N") + ".db");
                LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false), null, new CachingSettings { Enable = false });
                client.InitializeRepository();

                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;

                ClusterSettings settings = new ClusterSettings
                {
                    Enable = true,
                    ClusterName = clusterName,
                    NodeId = nodeId,
                    RestartDrainMs = 0,
                    RestartPeerTimeoutMs = 10000
                };
                settings.Redis.ConnectionString = Environment.GetEnvironmentVariable(RedisTestConnectionStringEnvironmentVariable)!;
                settings.Redis.PollIntervalMs = 500;

                ClusterContext cluster = new ClusterContext(settings, locks);
                NodeHealthService health = new NodeHealthService(client, cluster, logging);
                ClusterRegistry registry = new ClusterRegistry(settings, cluster, health, new Serializer(), logging);
                health.Registry = registry;

                RollingRestartCoordinator coordinator = new RollingRestartCoordinator(settings, cluster, registry, logging, reason => exits?.Enqueue(nodeId));
                ScaleOutClusterNode node = new ScaleOutClusterNode(nodeId, client, logging, registry, coordinator);
                registry.SettingsChanged += (sender, version) => node.SettingsChanges.Add(version);
                registry.RestartRequested += (sender, version) =>
                {
                    node.RestartRequests.Add(version);
                    if (exits != null) _ = Task.Run(() => coordinator.RunAsync(version, node._Cts.Token));
                };
                return node;
            }

            internal Task StartAsync(CancellationToken token)
            {
                return Registry.StartAsync(token);
            }

            public void Dispose()
            {
                if (_Disposed) return;
                _Disposed = true;
                _Cts.Cancel();
                Registry.Dispose();
                _Client.Dispose();
                _Cts.Dispose();
            }
        }
    }
}
