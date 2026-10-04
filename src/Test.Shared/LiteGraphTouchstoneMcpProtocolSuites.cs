namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using Touchstone.Core;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Touchstone test cases for the MCP protocol surface exposed by LiteGraph.McpServer through Voltaic:
    /// the stateless 2026-07-28 Streamable HTTP transport, tools/call argument handling, and handshake negotiation.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        #region Private-Members

        private const string _McpStatelessVersion = "2026-07-28";
        private const string _McpNewestHandshakeVersion = "2025-11-25";
        private static readonly HashSet<string> _VoltaicDemoToolNames = new HashSet<string>(StringComparer.Ordinal) { "ping", "echo", "getTime", "getSessions", "getClients" };

        #endregion

        #region Private-Methods

        private static TestSuiteDescriptor CreateMcpProtocolSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Mcp.Protocol",
                displayName: "MCP protocol surface: stateless transport, tools/call, and handshake negotiation",
                cases: new List<TestCaseDescriptor>
                {
                    McpProtocolCase("Mcp.Protocol.StatelessDiscover", "server/discover advertises 2026-07-28 with resultType complete", TestMcpStatelessDiscover),
                    McpProtocolCase("Mcp.Protocol.StatelessToolsList", "Stateless tools/list returns LiteGraph tools with resultType and cache fields", TestMcpStatelessToolsList),
                    McpProtocolCase("Mcp.Protocol.StatelessToolCall", "Stateless tools/call reads the default tenant", TestMcpStatelessToolCall),
                    McpProtocolCase("Mcp.Protocol.StatelessToolCallRoundTrip", "Stateless tools/call creates, reads, and deletes a graph", TestMcpStatelessToolCallRoundTrip),
                    McpProtocolCase("Mcp.Protocol.StatelessToolCallMissingArgument", "Stateless tools/call without a required argument is rejected", TestMcpStatelessToolCallMissingArgument),
                    McpProtocolCase("Mcp.Protocol.StatelessUnknownTool", "Stateless tools/call for an unknown tool is rejected", TestMcpStatelessUnknownTool),
                    McpProtocolCase("Mcp.Protocol.HandshakeToolCall", "Handshake tools/call passes arguments through to the handler", TestMcpHandshakeToolCall),
                    McpProtocolCase("Mcp.Protocol.ToolCallWithoutArguments", "tools/call without arguments or without a required argument is rejected", TestMcpToolCallWithoutArguments),
                    McpProtocolCase("Mcp.Protocol.SchemaTypeMismatchRejected", "tools/call rejects an argument whose type does not match the tool schema", TestMcpSchemaTypeMismatchRejected),
                    McpProtocolCase("Mcp.Protocol.BareToolMethodRejected", "Calling an HTTP tool by its bare name returns method-not-found; tools/call succeeds", TestMcpBareToolMethodRejected),
                    McpProtocolCase("Mcp.Protocol.ToolsListExcludesDiagnosticTools", "tools/list publishes only LiteGraph tools, with no Voltaic demo or diagnostic tools", TestMcpToolsListExcludesDiagnosticTools),
                    McpProtocolCase("Mcp.Protocol.DiagnosticToolsNotCallable", "tools/call for removed Voltaic demo tools (ping, echo, getTime, getSessions, getClients) is rejected", TestMcpDiagnosticToolsNotCallable),
                    McpProtocolCase("Mcp.Protocol.PingReturnsEmptyObject", "Protocol ping returns an empty object instead of \"pong\"", TestMcpPingReturnsEmptyObject),
                    McpProtocolCase("Mcp.Protocol.StatelessPing", "Stateless ping is method-not-found and server/discover returns resultType complete", TestMcpStatelessPing),
                    McpProtocolCase("Mcp.Protocol.TcpTransport", "TCP transport answers ping, lists no demo tools, and serves LiteGraph methods", TestMcpTcpTransport),
                    McpProtocolCase("Mcp.Protocol.WebSocketTransport", "WebSocket transport answers ping and serves LiteGraph methods", TestMcpWebSocketTransport),
                    McpProtocolCase("Mcp.Protocol.DeletedGraphReadReportsCause", "Reading a deleted graph returns a specific error naming the graph, not a generic internal error", TestMcpDeletedGraphReadReportsCause),
                    McpProtocolCase("Mcp.Protocol.HandlerArgumentErrorReportsCause", "A malformed argument is reported as invalid params with a readable message", TestMcpHandlerArgumentErrorReportsCause),
                    McpProtocolCase("Mcp.Protocol.ExistingGraphReadStillSucceeds", "Error translation leaves successful tool results unchanged", TestMcpExistingGraphReadStillSucceeds),
                    McpProtocolCase("Mcp.Protocol.TcpAndWebSocketErrorsReportCause", "TCP and WebSocket report the cause of a failed LiteGraph call", TestMcpTcpAndWebSocketErrorsReportCause),
                    McpProtocolCase("Mcp.Protocol.InitializeCapsHandshakeVersion", "initialize requesting 2026-07-28 negotiates the newest handshake revision", TestMcpInitializeCapsHandshakeVersion)
                },
                afterSuiteAsync: CleanupMcpSuiteAsync);
        }

        private static TestCaseDescriptor McpProtocolCase(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(
                suiteId: "Mcp.Protocol",
                caseId: caseId,
                displayName: displayName,
                executeAsync: async ct =>
                {
                    try
                    {
                        await executeAsync(ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        _PreserveMcpArtifactsOnCleanup = true;
                        throw;
                    }
                });
        }

        private static async Task<McpHttpClient> ConnectStatelessMcpClientAsync(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            McpHttpClient client = new McpHttpClient();
            ConfigureMcpHttpClient(client, 30000);

            bool connected = await client.ConnectStatelessAsync(
                _McpEnvironment.McpHttpEndpoint,
                "/mcp",
                _McpStatelessVersion,
                autoNegotiate: false,
                token: cancellationToken).ConfigureAwait(false);

            if (!connected)
            {
                client.Dispose();
                throw new InvalidOperationException("Unable to connect a stateless MCP client to " + _McpEnvironment.McpHttpEndpoint + "/mcp");
            }

            return client;
        }

        private static async Task TestMcpStatelessDiscover(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                AssertTrue(client.IsStateless, "Client is in stateless mode");

                McpDiscoverResult discover = await client.DiscoverAsync(cancellationToken).ConfigureAwait(false);
                AssertTrue(discover.SupportedVersions.Contains(_McpStatelessVersion), "server/discover advertises " + _McpStatelessVersion);
                AssertEqual(McpResult.ResultTypeComplete, discover.ResultType, "server/discover carries resultType complete");
                AssertTrue(discover.TtlMs != null, "server/discover carries ttlMs");
                AssertTrue(!String.IsNullOrEmpty(discover.CacheScope), "server/discover carries cacheScope");
            }
        }

        private static async Task TestMcpStatelessToolsList(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
                string? cursor = null;
                int pages = 0;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Dictionary<string, object?>? parameters = cursor == null
                        ? null
                        : new Dictionary<string, object?> { { "cursor", cursor } };

                    JsonRpcResponse response = await client.SendStatelessAsync("tools/list", parameters, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(response.Error == null, "tools/list page " + (pages + 1) + " succeeds (" + DescribeRpcError(response) + ")");
                    pages++;

                    using (JsonDocument result = ParseRpcResult(response))
                    {
                        JsonElement root = result.RootElement;
                        AssertEqual(McpResult.ResultTypeComplete, GetStringProperty(root, "resultType"), "tools/list carries resultType complete");
                        AssertTrue(root.TryGetProperty("ttlMs", out JsonElement _), "tools/list carries ttlMs");
                        AssertTrue(root.TryGetProperty("cacheScope", out JsonElement _), "tools/list carries cacheScope");
                        AssertTrue(root.TryGetProperty("tools", out JsonElement tools) && tools.ValueKind == JsonValueKind.Array, "tools/list returns a tools array");

                        foreach (JsonElement tool in tools.EnumerateArray())
                        {
                            string? name = GetStringProperty(tool, "name");
                            if (!String.IsNullOrEmpty(name)) names.Add(name);
                        }

                        cursor = GetStringProperty(root, "nextCursor");
                    }
                }
                while (!String.IsNullOrEmpty(cursor) && pages < 50);

                AssertTrue(String.IsNullOrEmpty(cursor), "tools/list pagination terminates");
                AssertTrue(names.Count > 100, "tools/list pages return the full LiteGraph catalog (" + names.Count + " tools across " + pages + " pages)");
                foreach (string expected in new[] { "tenant_get", "graph_create", "node_search", "edge_create", "vector_search", "authorization_role_create" })
                {
                    AssertTrue(names.Contains(expected), "tools/list includes '" + expected + "'");
                }
            }
        }

        private static async Task TestMcpStatelessToolCall(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse response = await client.CallToolStatelessAsync(
                    "tenant_get",
                    new { tenantGuid = _DefaultTenantGuid },
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(response.Error == null, "tools/call tenant/get succeeds (" + DescribeRpcError(response) + ")");

                using (JsonDocument result = ParseRpcResult(response))
                {
                    AssertEqual(McpResult.ResultTypeComplete, GetStringProperty(result.RootElement, "resultType"), "tools/call carries resultType complete");
                    AssertFalse(IsToolError(result.RootElement), "tools/call result is not an error");

                    string text = GetToolText(result.RootElement);
                    TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(text);
                    AssertNotNull(tenant, "tools/call content deserializes to a tenant");
                    AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "tools/call returns the default tenant");
                }
            }
        }

        private static async Task TestMcpStatelessToolCallRoundTrip(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                string graphName = "mcp-stateless-" + Guid.NewGuid().ToString("N");

                JsonRpcResponse created = await client.CallToolStatelessAsync(
                    "graph_create",
                    new { tenantGuid = _DefaultTenantGuid, name = graphName },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(created.Error == null, "tools/call graph/create succeeds (" + DescribeRpcError(created) + ")");

                Graph? graph;
                using (JsonDocument result = ParseRpcResult(created))
                {
                    AssertFalse(IsToolError(result.RootElement), "graph/create result is not an error");
                    graph = _McpSerializer.DeserializeJson<Graph>(GetToolText(result.RootElement));
                }

                AssertNotNull(graph, "graph/create returns a graph");
                AssertEqual(graphName, graph!.Name, "graph/create stores the name");

                JsonRpcResponse read = await client.CallToolStatelessAsync(
                    "graph_get",
                    new { tenantGuid = _DefaultTenantGuid, graphGuid = graph.GUID.ToString() },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(read.Error == null, "tools/call graph/get succeeds (" + DescribeRpcError(read) + ")");

                using (JsonDocument result = ParseRpcResult(read))
                {
                    Graph? readGraph = _McpSerializer.DeserializeJson<Graph>(GetToolText(result.RootElement));
                    AssertNotNull(readGraph, "graph/get returns a graph");
                    AssertEqual(graph.GUID, readGraph!.GUID, "graph/get returns the created graph");
                }

                JsonRpcResponse deleted = await client.CallToolStatelessAsync(
                    "graph_delete",
                    new { tenantGuid = _DefaultTenantGuid, graphGuid = graph.GUID.ToString(), force = true },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(deleted.Error == null, "tools/call graph/delete succeeds (" + DescribeRpcError(deleted) + ")");

                using (JsonDocument result = ParseRpcResult(deleted))
                {
                    AssertFalse(IsToolError(result.RootElement), "graph/delete result is not an error");
                }
            }
        }

        private static async Task TestMcpStatelessToolCallMissingArgument(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse response = await client.CallToolStatelessAsync(
                    "tenant_get",
                    new { },
                    token: cancellationToken).ConfigureAwait(false);

                bool rejected = response.Error != null;
                if (!rejected)
                {
                    using (JsonDocument result = ParseRpcResult(response))
                    {
                        rejected = IsToolError(result.RootElement);
                    }
                }

                AssertTrue(rejected, "tools/call tenant/get without tenantGuid is rejected");
                if (response.Error != null)
                {
                    AssertTrue(
                        (response.Error.Message ?? "").Contains("tenantGuid"),
                        "The rejection names the missing argument (" + response.Error.Message + ")");
                }
            }
        }

        private static async Task TestMcpStatelessUnknownTool(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse response = await client.CallToolStatelessAsync(
                    "no/such/tool",
                    new { },
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(response.Error != null, "tools/call for an unknown tool returns a JSON-RPC error");
            }
        }

        private static async Task TestMcpHandshakeToolCall(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse response = await _McpClient.CallAsync(
                "tools/call",
                new { name = "tenant_get", arguments = new { tenantGuid = _DefaultTenantGuid } },
                token: cancellationToken).ConfigureAwait(false);

            AssertTrue(response.Error == null, "Handshake tools/call succeeds (" + DescribeRpcError(response) + ")");

            using (JsonDocument result = ParseRpcResult(response))
            {
                AssertFalse(IsToolError(result.RootElement), "Handshake tools/call result is not an error");
                TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(GetToolText(result.RootElement));
                AssertNotNull(tenant, "Handshake tools/call returns a tenant");
                AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "Handshake tools/call returns the default tenant");
            }
        }

        private static async Task TestMcpToolCallWithoutArguments(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse response = await _McpClient.CallAsync("tools/call", new { name = "tenant_get" }, token: cancellationToken).ConfigureAwait(false);
            AssertToolInputRejected(response, "tools/call tenant_get without arguments", null);

            JsonRpcResponse missingName = await _McpClient.CallAsync(
                "tools/call",
                new { name = "graph_create", arguments = new { tenantGuid = _DefaultTenantGuid } },
                token: cancellationToken).ConfigureAwait(false);
            AssertToolInputRejected(missingName, "tools/call graph_create without a name", null);

            JsonRpcResponse noName = await _McpClient.CallAsync("tools/call", new { arguments = new { tenantGuid = _DefaultTenantGuid } }, token: cancellationToken).ConfigureAwait(false);
            AssertTrue(noName.Error != null, "tools/call without a tool name returns a JSON-RPC error");
        }

        private static async Task TestMcpSchemaTypeMismatchRejected(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse stringRequest = await _McpClient.CallAsync(
                "tools/call",
                new { name = "graph_query", arguments = new { tenantGuid = _DefaultTenantGuid, graphGuid = Guid.NewGuid().ToString(), request = "{\"Query\":\"MATCH (n) RETURN n\"}" } },
                token: cancellationToken).ConfigureAwait(false);
            AssertToolInputRejected(stringRequest, "graph_query with a string 'request'", "request");

            JsonRpcResponse numericGuid = await _McpClient.CallAsync(
                "tools/call",
                new { name = "tenant_get", arguments = new { tenantGuid = 42 } },
                token: cancellationToken).ConfigureAwait(false);
            AssertToolInputRejected(numericGuid, "tenant_get with a numeric tenantGuid", "tenantGuid");
        }

        private static async Task TestMcpBareToolMethodRejected(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse bare = await _McpClient.CallAsync("tenant_get", new { tenantGuid = _DefaultTenantGuid }, token: cancellationToken).ConfigureAwait(false);
            AssertTrue(bare.Error != null, "Bare tenant/get call returns a JSON-RPC error");
            AssertEqual(-32601, bare.Error!.Code, "Bare tool call returns method-not-found (" + DescribeRpcError(bare) + ")");

            string text = await CallMcpToolAsync<string>("tenant_get", new { tenantGuid = _DefaultTenantGuid }, token: cancellationToken).ConfigureAwait(false);
            TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(text);
            AssertNotNull(tenant, "tools/call tenant/get returns a tenant");
            AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "tools/call tenant/get returns the default tenant");
        }

        private static async Task TestMcpToolsListExcludesDiagnosticTools(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            HashSet<string> names = await ListMcpToolNamesAsync(cancellationToken).ConfigureAwait(false);
            AssertTrue(names.Count > 100, "tools/list returns the LiteGraph catalog (" + names.Count + " tools)");

            foreach (string removed in _VoltaicDemoToolNames)
            {
                AssertFalse(names.Contains(removed), "tools/list does not include Voltaic demo tool '" + removed + "'");
            }

            foreach (string name in names)
            {
                AssertTrue(name.Contains('_') && !name.Contains('/'), "tools/list entry '" + name + "' is a LiteGraph resource_action tool");
            }
        }

        private static async Task TestMcpDiagnosticToolsNotCallable(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            foreach (string removed in _VoltaicDemoToolNames)
            {
                JsonRpcResponse response = await _McpClient.CallAsync(
                    "tools/call",
                    new { name = removed, arguments = new { message = "hello" } },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(response.Error != null, "tools/call " + removed + " is rejected (" + DescribeRpcError(response) + ")");
            }

            JsonRpcResponse sessions = await _McpClient.CallAsync("getSessions", null, token: cancellationToken).ConfigureAwait(false);
            AssertTrue(sessions.Error != null, "Bare getSessions is not served (" + DescribeRpcError(sessions) + ")");
            AssertEqual(-32601, sessions.Error!.Code, "Bare getSessions returns method-not-found");
        }

        private static async Task TestMcpPingReturnsEmptyObject(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            await _McpClient.PingAsync(token: cancellationToken).ConfigureAwait(false);

            JsonRpcResponse response = await _McpClient.CallAsync("ping", null, token: cancellationToken).ConfigureAwait(false);
            AssertTrue(response.Error == null, "ping succeeds (" + DescribeRpcError(response) + ")");

            using (JsonDocument result = ParseRpcResult(response))
            {
                AssertEqual(JsonValueKind.Object, result.RootElement.ValueKind, "ping result is a JSON object, not the v1 \"pong\" string");
                AssertFalse(result.RootElement.EnumerateObject().Any(), "ping result is an empty object");
            }
        }

        private static async Task TestMcpStatelessPing(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse ping = await client.SendStatelessAsync("ping", null, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(ping.Error != null, "Stateless ping is not served, since 2026-07-28 removed it (" + DescribeRpcError(ping) + ")");
                AssertEqual(-32601, ping.Error!.Code, "Stateless ping returns method-not-found");

                JsonRpcResponse response = await client.SendStatelessAsync("server/discover", null, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(response.Error == null, "Stateless server/discover succeeds (" + DescribeRpcError(response) + ")");

                using (JsonDocument result = ParseRpcResult(response))
                {
                    AssertEqual(JsonValueKind.Object, result.RootElement.ValueKind, "Stateless server/discover result is a JSON object");
                    AssertEqual(McpResult.ResultTypeComplete, GetStringProperty(result.RootElement, "resultType"), "Stateless server/discover carries resultType complete");
                }
            }
        }

        private static async Task TestMcpTcpTransport(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            using (McpTcpClient client = new McpTcpClient())
            {
                bool connected = await client.ConnectAsync("127.0.0.1", _McpEnvironment.McpTcpPort, cancellationToken).ConfigureAwait(false);
                AssertTrue(connected, "TCP client connects on port " + _McpEnvironment.McpTcpPort);

                object? ping = await client.CallAsync<object?>("ping", null, 30000, cancellationToken).ConfigureAwait(false);
                AssertFalse(ping is string, "TCP ping no longer returns the v1 \"pong\" string");

                JsonElement tools = await client.CallAsync<JsonElement>("tools/list", null, 30000, cancellationToken).ConfigureAwait(false);
                AssertTrue(tools.TryGetProperty("tools", out JsonElement toolArray) && toolArray.ValueKind == JsonValueKind.Array, "TCP tools/list returns a tools array");
                foreach (JsonElement tool in toolArray.EnumerateArray())
                {
                    string? name = GetStringProperty(tool, "name");
                    AssertFalse(name != null && _VoltaicDemoToolNames.Contains(name), "TCP tools/list does not include Voltaic demo tool '" + name + "'");
                }

                JsonElement tenantResult = await client.CallAsync<JsonElement>("tenant_get", new { tenantGuid = _DefaultTenantGuid }, 30000, cancellationToken).ConfigureAwait(false);
                AssertEqual(JsonValueKind.Object, tenantResult.ValueKind, "TCP tenant_get returns a JSON object result");
                AssertFalse(IsToolError(tenantResult), "TCP tenant_get result is not an error");
                TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(GetToolText(tenantResult));
                AssertNotNull(tenant, "TCP tenant/get returns a tenant");
                AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "TCP tenant/get returns the default tenant");

                bool rejected = false;
                try
                {
                    await client.CallAsync<object?>("getClients", null, 30000, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    rejected = true;
                }

                AssertTrue(rejected, "TCP getClients is no longer served");
            }
        }

        private static async Task TestMcpWebSocketTransport(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            using (McpWebsocketsClient client = new McpWebsocketsClient())
            {
                string url = "ws://127.0.0.1:" + _McpEnvironment.McpWebSocketPort + "/mcp";
                bool connected = await client.ConnectAsync(url, cancellationToken).ConfigureAwait(false);
                AssertTrue(connected, "WebSocket client connects to " + url);

                await client.PingAsync(30000, cancellationToken).ConfigureAwait(false);

                JsonElement tenantResult = await client.CallAsync<JsonElement>("tenant_get", new { tenantGuid = _DefaultTenantGuid }, 30000, cancellationToken).ConfigureAwait(false);
                AssertEqual(JsonValueKind.Object, tenantResult.ValueKind, "WebSocket tenant_get returns a JSON object result");
                AssertFalse(IsToolError(tenantResult), "WebSocket tenant_get result is not an error");
                TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(GetToolText(tenantResult));
                AssertNotNull(tenant, "WebSocket tenant/get returns a tenant");
                AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "WebSocket tenant/get returns the default tenant");

                bool rejected = false;
                try
                {
                    await client.CallAsync<object?>("getClients", null, 30000, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    rejected = true;
                }

                AssertTrue(rejected, "WebSocket getClients is no longer served");
            }
        }

        private static async Task<Guid> CreateAndDeleteMcpGraphAsync(CancellationToken cancellationToken)
        {
            string created = await CallMcpToolAsync<string>(
                "graph_create",
                new { tenantGuid = _DefaultTenantGuid, name = "mcp-deleted-" + Guid.NewGuid().ToString("N") },
                token: cancellationToken).ConfigureAwait(false);
            Graph? graph = _McpSerializer.DeserializeJson<Graph>(created);
            AssertNotNull(graph, "graph/create returns a graph");

            await CallMcpToolAsync<object>(
                "graph_delete",
                new { tenantGuid = _DefaultTenantGuid, graphGuid = graph!.GUID.ToString(), force = true },
                token: cancellationToken).ConfigureAwait(false);

            return graph.GUID;
        }

        private static async Task TestMcpDeletedGraphReadReportsCause(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            Guid graphGuid = await CreateAndDeleteMcpGraphAsync(cancellationToken).ConfigureAwait(false);

            JsonRpcResponse response = await _McpClient.CallAsync(
                "tools/call",
                new { name = "graph_get", arguments = new { tenantGuid = _DefaultTenantGuid, graphGuid = graphGuid.ToString() } },
                token: cancellationToken).ConfigureAwait(false);

            AssertTrue(response.Error != null, "graph/get of a deleted graph returns a JSON-RPC error");
            string message = response.Error!.Message ?? "";
            AssertTrue(response.Error.Code != -32603, "The error is not a generic internal error (" + DescribeRpcError(response) + ")");
            AssertEqual(-32602, response.Error.Code, "REST 400 maps to invalid params");
            AssertFalse(message.Equals("Internal error", StringComparison.Ordinal), "The error message is not the bare 'Internal error'");
            AssertTrue(message.Contains(graphGuid.ToString()), "The error message names the deleted graph (" + message + ")");
            AssertTrue(message.Contains("No graph", StringComparison.OrdinalIgnoreCase), "The error message carries the REST description (" + message + ")");

            using (JsonDocument data = JsonDocument.Parse(JsonSerializer.Serialize(response.Error.Data)))
            {
                AssertTrue(
                    data.RootElement.ValueKind == JsonValueKind.Object
                    && data.RootElement.TryGetProperty("statusCode", out JsonElement statusCode)
                    && statusCode.GetInt32() == 400,
                    "The error data carries the REST status code");
            }
        }

        private static async Task TestMcpHandlerArgumentErrorReportsCause(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse response = await _McpClient.CallAsync(
                "tools/call",
                new { name = "tenant_get", arguments = new { tenantGuid = "not-a-guid" } },
                token: cancellationToken).ConfigureAwait(false);

            AssertTrue(response.Error != null, "tenant/get with a malformed GUID returns a JSON-RPC error");
            AssertEqual(-32602, response.Error!.Code, "A handler argument error maps to invalid params (" + DescribeRpcError(response) + ")");
            string message = response.Error.Message ?? "";
            AssertFalse(message.Equals("Internal error", StringComparison.Ordinal), "The error message is not the bare 'Internal error'");
            AssertTrue(message.Contains("Invalid argument format") && message.Contains("Guid"), "The error message describes the malformed GUID (" + message + ")");
        }

        private static async Task TestMcpExistingGraphReadStillSucceeds(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            string created = await CallMcpToolAsync<string>(
                "graph_create",
                new { tenantGuid = _DefaultTenantGuid, name = "mcp-live-" + Guid.NewGuid().ToString("N") },
                token: cancellationToken).ConfigureAwait(false);
            Graph? graph = _McpSerializer.DeserializeJson<Graph>(created);
            AssertNotNull(graph, "graph/create returns a graph");

            try
            {
                string read = await CallMcpToolAsync<string>(
                    "graph_get",
                    new { tenantGuid = _DefaultTenantGuid, graphGuid = graph!.GUID.ToString() },
                    token: cancellationToken).ConfigureAwait(false);
                Graph? readGraph = _McpSerializer.DeserializeJson<Graph>(read);
                AssertNotNull(readGraph, "graph/get returns the graph");
                AssertEqual(graph.GUID, readGraph!.GUID, "graph/get returns the created graph");
            }
            finally
            {
                await CallMcpToolAsync<object>(
                    "graph_delete",
                    new { tenantGuid = _DefaultTenantGuid, graphGuid = graph!.GUID.ToString(), force = true },
                    token: cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task TestMcpTcpAndWebSocketErrorsReportCause(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            Guid graphGuid = await CreateAndDeleteMcpGraphAsync(cancellationToken).ConfigureAwait(false);
            object arguments = new { tenantGuid = _DefaultTenantGuid, graphGuid = graphGuid.ToString() };

            string? tcpError = null;
            using (McpTcpClient tcp = new McpTcpClient())
            {
                AssertTrue(await tcp.ConnectAsync("127.0.0.1", _McpEnvironment.McpTcpPort, cancellationToken).ConfigureAwait(false), "TCP client connects");
                try
                {
                    await tcp.CallAsync<object?>("graph_get", arguments, 30000, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    tcpError = ex.Message;
                }
            }

            AssertNotNull(tcpError, "TCP graph/get of a deleted graph fails");
            AssertFalse(tcpError!.Contains("-32603"), "TCP error is not a generic internal error (" + tcpError + ")");
            AssertTrue(tcpError.Contains(graphGuid.ToString()), "TCP error names the deleted graph (" + tcpError + ")");

            string? wsError = null;
            using (McpWebsocketsClient ws = new McpWebsocketsClient())
            {
                AssertTrue(await ws.ConnectAsync("ws://127.0.0.1:" + _McpEnvironment.McpWebSocketPort + "/mcp", cancellationToken).ConfigureAwait(false), "WebSocket client connects");
                try
                {
                    await ws.CallAsync<object?>("graph_get", arguments, 30000, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    wsError = ex.Message;
                }
            }

            AssertNotNull(wsError, "WebSocket graph/get of a deleted graph fails");
            AssertFalse(wsError!.Contains("Internal error"), "WebSocket error is not a generic internal error (" + wsError + ")");
            AssertTrue(wsError.Contains(graphGuid.ToString()), "WebSocket error names the deleted graph (" + wsError + ")");
        }

        private static async Task<HashSet<string>> ListMcpToolNamesAsync(CancellationToken cancellationToken)
        {
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            string? cursor = null;
            int pages = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                object parameters = cursor == null ? new { } : new { cursor = cursor };
                JsonRpcResponse response = await _McpClient.CallAsync("tools/list", parameters, token: cancellationToken).ConfigureAwait(false);
                AssertTrue(response.Error == null, "tools/list page " + (pages + 1) + " succeeds (" + DescribeRpcError(response) + ")");
                pages++;

                using (JsonDocument result = ParseRpcResult(response))
                {
                    foreach (JsonElement tool in result.RootElement.GetProperty("tools").EnumerateArray())
                    {
                        string? name = GetStringProperty(tool, "name");
                        if (!String.IsNullOrEmpty(name)) names.Add(name);
                    }

                    cursor = GetStringProperty(result.RootElement, "nextCursor");
                }
            }
            while (!String.IsNullOrEmpty(cursor) && pages < 50);

            return names;
        }

        private static async Task<T> CallMcpToolAsync<T>(string name, object? arguments = null, int timeoutMs = 0, CancellationToken token = default)
        {
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse response = await _McpClient.CallAsync(
                "tools/call",
                new { name = name, arguments = arguments ?? new { } },
                timeoutMs,
                token).ConfigureAwait(false);

            if (response.Error != null)
                throw new McpProtocolException(response.Error.Code, "RPC Error " + response.Error.Code + " calling tool '" + name + "': " + response.Error.Message, response.Error.Data);

            using (JsonDocument result = ParseRpcResult(response))
            {
                string text = GetToolText(result.RootElement);
                if (IsToolError(result.RootElement))
                    throw new InvalidOperationException("Tool '" + name + "' returned an error result: " + text);

                if (typeof(T) == typeof(string)) return (T)(object)text;

                T? value = JsonSerializer.Deserialize<T>(text);
                if (value == null) throw new InvalidOperationException("Tool '" + name + "' returned content that does not deserialize to " + typeof(T).Name + ": " + Truncate(text, 200));
                return value;
            }
        }

        private static async Task TestMcpInitializeCapsHandshakeVersion(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            string body =
                "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{"
                + "\"protocolVersion\":\"" + _McpStatelessVersion + "\","
                + "\"capabilities\":{},"
                + "\"clientInfo\":{\"name\":\"litegraph-touchstone\",\"version\":\"1.0.0\"}}}";

            using (HttpClient http = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _McpEnvironment.McpHttpEndpoint + "/mcp"))
            {
                http.Timeout = TimeSpan.FromSeconds(30);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.Accept.ParseAdd("text/event-stream");

                using (HttpResponseMessage response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, (int)response.StatusCode, "initialize responds (body " + Truncate(responseBody, 200) + ")");

                    using (JsonDocument document = JsonDocument.Parse(ExtractJsonRpcPayload(responseBody)))
                    {
                        AssertTrue(document.RootElement.TryGetProperty("result", out JsonElement result), "initialize returns a result (body " + Truncate(responseBody, 200) + ")");
                        AssertEqual(_McpNewestHandshakeVersion, GetStringProperty(result, "protocolVersion"), "initialize negotiates the newest handshake revision rather than " + _McpStatelessVersion);
                    }
                }
            }
        }

        private static JsonDocument ParseRpcResult(JsonRpcResponse response)
        {
            if (response.Result == null) throw new InvalidOperationException("JSON-RPC response has no result.");
            return JsonDocument.Parse(JsonSerializer.Serialize(response.Result));
        }

        private static string DescribeRpcError(JsonRpcResponse response)
        {
            if (response.Error == null) return "no error";
            return "error " + response.Error.Code + ": " + response.Error.Message;
        }

        private static string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (!element.TryGetProperty(propertyName, out JsonElement value)) return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }

        private static void AssertToolInputRejected(JsonRpcResponse response, string label, string? argumentName)
        {
            // Voltaic 2.2 follows MCP 2025-11-25: arguments that fail the input schema produce a tool result with
            // isError set, so the model can correct them.  A JSON-RPC invalid-params error is also accepted.
            if (response.Error != null)
            {
                AssertEqual(-32602, response.Error.Code, label + " is reported as invalid params");
                if (argumentName != null)
                    AssertTrue((response.Error.Message ?? "").Contains(argumentName), label + ": the rejection names the argument (" + response.Error.Message + ")");
                return;
            }

            using (JsonDocument result = ParseRpcResult(response))
            {
                AssertTrue(IsToolError(result.RootElement), label + " returns an isError tool result");
                if (argumentName != null)
                {
                    string text = GetToolText(result.RootElement);
                    AssertTrue(text.Contains(argumentName), label + ": the rejection names the argument (" + text + ")");
                }
            }
        }

        private static bool IsToolError(JsonElement result)
        {
            return result.TryGetProperty("isError", out JsonElement isError) && isError.ValueKind == JsonValueKind.True;
        }

        private static string GetToolText(JsonElement result)
        {
            if (!result.TryGetProperty("content", out JsonElement content) || content.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Tool result has no content array.");

            foreach (JsonElement item in content.EnumerateArray())
            {
                string? text = GetStringProperty(item, "text");
                if (text != null) return text;
            }

            throw new InvalidOperationException("Tool result has no text content.");
        }

        private static string ExtractJsonRpcPayload(string body)
        {
            string trimmed = body.TrimStart();
            if (trimmed.StartsWith("{", StringComparison.Ordinal)) return trimmed;

            foreach (string line in body.Split('\n'))
            {
                string candidate = line.Trim();
                if (candidate.StartsWith("data:", StringComparison.Ordinal))
                {
                    string data = candidate.Substring(5).Trim();
                    if (data.StartsWith("{", StringComparison.Ordinal)) return data;
                }
            }

            throw new InvalidOperationException("Response does not contain a JSON-RPC payload: " + Truncate(body, 200));
        }

        #endregion
    }
}
