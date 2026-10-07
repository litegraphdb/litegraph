namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Serialization;
    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite that pins the JSON LiteGraph.Server writes, so the Native AOT work on the server cannot change it.
    /// The baselines under Baselines/ were captured from the server before that work (reflection-based serialization and
    /// anonymous objects). Set LITEGRAPH_CAPTURE_AOT_BASELINES to a directory to write fresh baselines there instead of
    /// comparing.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        #region Private-Members

        private const string _ServerSerializationBaselineFile = "server-serialization-baseline.json";
        private const string _ServerLiveBaselineFile = "server-live-baseline.json";

        private static readonly Type[] _AotServerParityTypes = new Type[]
        {
            typeof(LiteGraph.Server.Classes.ApiErrorResponse),
            typeof(LiteGraph.Server.Classes.AuthenticationToken),
            typeof(LiteGraph.Server.Classes.AuthorizationAuditSettings),
            typeof(LiteGraph.Server.Classes.BackupRequest),
            typeof(LiteGraph.Server.Classes.ChatCompletionRequest),
            typeof(LiteGraph.Server.Classes.ChatCompletionResult),
            typeof(LiteGraph.Server.Classes.ChatEndpointHealth),
            typeof(LiteGraph.Server.Classes.ChatEndpointHealthSample),
            typeof(LiteGraph.Server.Classes.ChatEndpointPreloadResult),
            typeof(LiteGraph.Server.Classes.ChatEndpointTestResult),
            typeof(LiteGraph.Server.Classes.ChatModelSummary),
            typeof(LiteGraph.Server.Classes.ChatServerSettings),
            typeof(LiteGraph.Server.Classes.ClusterJobList),
            typeof(LiteGraph.Server.Classes.ClusterJobRun),
            typeof(LiteGraph.Server.Classes.ClusterLock),
            typeof(LiteGraph.Server.Classes.ClusterLockList),
            typeof(LiteGraph.Server.Classes.ClusterNode),
            typeof(LiteGraph.Server.Classes.ClusterRestartResult),
            typeof(LiteGraph.Server.Classes.ClusterSettings),
            typeof(LiteGraph.Server.Classes.ClusterStatus),
            typeof(LiteGraph.Server.Classes.ClutchSettings),
            typeof(LiteGraph.Server.Classes.DebugSettings),
            typeof(LiteGraph.Server.Classes.EncryptionSettings),
            typeof(LiteGraph.Server.Classes.GenerateEmbeddingsRequest),
            typeof(LiteGraph.Server.Classes.GenerateEmbeddingsResult),
            typeof(LiteGraph.Server.Classes.HealthChecks),
            typeof(LiteGraph.Server.Classes.HealthResponse),
            typeof(LiteGraph.Server.Classes.LiteGraphSettings),
            typeof(LiteGraph.Server.Classes.ObservabilitySettings),
            typeof(LiteGraph.Server.Classes.OllamaChatMessage),
            typeof(LiteGraph.Server.Classes.OllamaChatOptions),
            typeof(LiteGraph.Server.Classes.OllamaChatRequest),
            typeof(LiteGraph.Server.Classes.OllamaChatResponse),
            typeof(LiteGraph.Server.Classes.OpenAiChatChoice),
            typeof(LiteGraph.Server.Classes.OpenAiChatChunkChoice),
            typeof(LiteGraph.Server.Classes.OpenAiChatCompletionChunk),
            typeof(LiteGraph.Server.Classes.OpenAiChatCompletionRequest),
            typeof(LiteGraph.Server.Classes.OpenAiChatCompletionResponse),
            typeof(LiteGraph.Server.Classes.OpenAiChatDelta),
            typeof(LiteGraph.Server.Classes.OpenAiChatMessage),
            typeof(LiteGraph.Server.Classes.OpenAiChatResponseMessage),
            typeof(LiteGraph.Server.Classes.OpenAiChatUsage),
            typeof(LiteGraph.Server.Classes.OpenAiErrorDetail),
            typeof(LiteGraph.Server.Classes.OpenAiErrorResponse),
            typeof(LiteGraph.Server.Classes.OpenAiModelEntry),
            typeof(LiteGraph.Server.Classes.OpenAiModelList),
            typeof(LiteGraph.Server.Classes.OpenAiStreamOptions),
            typeof(LiteGraph.Server.Classes.RedisSettings),
            typeof(LiteGraph.Server.Classes.RequestHistorySettings),
            typeof(LiteGraph.Server.Classes.RouteRequest),
            typeof(LiteGraph.Server.Classes.RouteResponse),
            typeof(LiteGraph.Server.Classes.Settings),
            typeof(LiteGraph.Server.Classes.SettingsUpdateResult),
            typeof(LiteGraph.Server.Classes.StorageSettings),
            typeof(LiteGraph.Server.Classes.TenantOnboardRequest),
            typeof(LiteGraph.Server.Classes.TenantOnboardResponse),
            typeof(LiteGraph.Server.Classes.TransactionSettings),
            typeof(WatsonWebserver.Core.WebserverSettings),
            typeof(EnumerationResult<AuthorizationRole>),
            typeof(EnumerationResult<CredentialScopeAssignment>),
            typeof(EnumerationResult<RequestHistoryEntry>),
            typeof(EnumerationResult<LiteGraph.Server.Classes.ChatEndpointHealth>),
            typeof(EnumerationResult<LiteGraph.Server.Classes.ChatModelSummary>),
            typeof(EnumerationResult<UserRoleAssignment>),
            typeof(EnumerationResult<VectorSearchResult>)
        };

        #endregion

        #region Private-Methods

        private static TestSuiteDescriptor CreateAotServerSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Aot.Server",
                displayName: "LiteGraph.Server JSON output is unchanged by the Native AOT work",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.TypeParity", "Every server type serializes as in the baseline (compact, indented, round trip)", TestAotServerTypeParity),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.DefaultSettings", "A default settings file is written as in the baseline", TestAotServerDefaultSettings),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.PayloadShapes", "Chat stream events, tool transcripts, and other built payloads serialize as in the baseline", TestAotServerPayloadShapes),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.Live", "OpenAPI, seed data, chat tool schemas, stream events, and tool transcripts match the baseline", TestAotServerLive)
                },
                afterSuiteAsync: CleanupMcpSuiteAsync);
        }

        private static async Task TestAotServerTypeParity(CancellationToken token)
        {
            SortedDictionary<string, string> actual = new SortedDictionary<string, string>(StringComparer.Ordinal);
            Serializer serializer = new Serializer();

            foreach (Type type in _AotServerParityTypes)
            {
                object instance = CreateAotSample(type, 0);

                // Watson's SslSettings.SslCertificate getter loads PfxCertificateFile when it is read, so a sample
                // naming a file that does not exist cannot be serialized at all. TestAotServerSslSettings covers a real
                // certificate file.
                if (instance is LiteGraph.Server.Classes.Settings sampleSettings) sampleSettings.Rest.Ssl.PfxCertificateFile = null;
                if (instance is WatsonWebserver.Core.WebserverSettings sampleWebserver) sampleWebserver.Ssl.PfxCertificateFile = null;

                SortedDictionary<string, Dictionary<string, string>> entry = new SortedDictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                AddAotSnapshotEntry(entry, AotTypeKey(type), type, instance, serializer);
                foreach (KeyValuePair<string, string> kind in entry.Values.First())
                {
                    actual[AotTypeKey(type) + "|" + kind.Key] = MaskAotServerClock(kind.Value);
                }
            }

            await CompareAotServerBaseline(_ServerSerializationBaselineFile, actual, token).ConfigureAwait(false);
        }

        private static async Task TestAotServerDefaultSettings(CancellationToken token)
        {
            Serializer serializer = new Serializer();
            LiteGraph.Server.Classes.Settings settings = new LiteGraph.Server.Classes.Settings();

            SortedDictionary<string, string> actual = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "DefaultSettings|pretty", MaskAotServerVolatile(serializer.SerializeJson(settings, true)) },
                { "DefaultSettings|compact", MaskAotServerVolatile(serializer.SerializeJson(settings, false)) }
            };

            await CompareAotServerBaseline("server-settings-baseline.json", actual, token).ConfigureAwait(false);
        }

        private static async Task TestAotServerPayloadShapes(CancellationToken token)
        {
            // The payloads LiteGraph.Server builds itself (chat stream events, tool transcripts, the request history bulk
            // delete response, and seed data), including the ones the live case cannot reach (retrieval and thinking
            // events), with values of every shape: nulls, optional numbers, and nested lists.
            Serializer serializer = new Serializer();
            Guid nodeGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");
            Guid threadGuid = Guid.Parse("66666666-7777-8888-9999-000000000000");
            LiteGraph.Server.Classes.ChatCompletionResult usage = (LiteGraph.Server.Classes.ChatCompletionResult)CreateAotSample(typeof(LiteGraph.Server.Classes.ChatCompletionResult), 0);

            SortedDictionary<string, object> payloads = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                { "started", new { @event = "started", threadGuid = threadGuid, turnGuid = nodeGuid } },
                { "error-upstream", new { @event = "error", message = "HTTP 500", statusCode = (int?)500 } },
                { "error-upstream-nostatus", new { @event = "error", message = "HTTP failure", statusCode = (int?)null } },
                { "error", new { @event = "error", message = "failure" } },
                { "usage", new { @event = "usage", usage = usage } },
                { "retrieval", new { @event = "retrieval", chunks = new List<object>
                    {
                        new { nodeGuid = (Guid?)nodeGuid, name = "first", score = (float?)0.875f },
                        new { nodeGuid = (Guid?)null, name = (string?)null, score = (float?)null }
                    } } },
                { "retrieval-empty", new { @event = "retrieval", chunks = new List<object>() } },
                { "delta", new { @event = "delta", content = "text \"quoted\" <b>" } },
                { "thinking", new { @event = "thinking", content = "reasoning" } },
                { "tool_call", new { @event = "tool_call", name = "graph_get", arguments = "{\"graphGuid\":\"x\"}", iteration = 2 } },
                { "tool_result", new { @event = "tool_result", name = "graph_get", success = true, error = (string?)null, runtimeMs = 12.5 } },
                { "tool_result-error", new { @event = "tool_result", name = "graph_get", success = false, error = "bad", runtimeMs = 0.0 } },
                { "transcript", new List<object>
                    {
                        new { iteration = 1, name = "graph_all", arguments = "{}", success = true, error = (string?)null, runtimeMs = 3.25 },
                        new { iteration = 2, name = "graph_get", arguments = "{}", success = false, error = "bad", runtimeMs = 0.0 }
                    } },
                { "tool-error-content", new { error = "bad" } },
                { "tool-error-content-null", new { error = (string?)null } },
                { "request-history-deleted", new { Deleted = 42 } },
                { "seed-data", new { description = "Default LiteGraph API service node." } }
            };

            SortedDictionary<string, string> actual = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> payload in payloads)
            {
                actual["Payload|" + payload.Key + "|compact"] = MaskAotServerClock(serializer.SerializeJson(payload.Value, false));
                actual["Payload|" + payload.Key + "|pretty"] = MaskAotServerClock(serializer.SerializeJson(payload.Value, true));
            }

            await CompareAotServerBaseline("server-payload-baseline.json", actual, token).ConfigureAwait(false);
        }

        private static async Task TestAotServerLive(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            SortedDictionary<string, string> actual = new SortedDictionary<string, string>(StringComparer.Ordinal);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string tenant = _DefaultTenantGuid;
                    string graph = "00000000-0000-0000-0000-000000000000";

                    // OpenAPI document.
                    HttpOutcome spec = await AuthRestAsync(HttpMethod.Get, endpoint + "/openapi.json", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, spec.Status, "OpenAPI document is served");
                    actual["OpenApi"] = spec.Body;

                    // First-boot seed data: the default graph's nodes and edges.
                    foreach (string node in new[] { "10000000-0000-0000-0000-000000000001", "10000000-0000-0000-0000-000000000002", "10000000-0000-0000-0000-000000000003" })
                    {
                        HttpOutcome read = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenant + "/graphs/" + graph + "/nodes/" + node + "?incldata", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                        AssertEqual(200, read.Status, "Seed node " + node + " reads");
                        actual["Seed|node|" + node] = SeedDataOf(read.Body);
                    }

                    foreach (string edge in new[] { "20000000-0000-0000-0000-000000000001", "20000000-0000-0000-0000-000000000002" })
                    {
                        HttpOutcome read = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenant + "/graphs/" + graph + "/edges/" + edge + "?incldata", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                        AssertEqual(200, read.Status, "Seed edge " + edge + " reads");
                        actual["Seed|edge|" + edge] = SeedDataOf(read.Body);
                    }

                    // Chat with every tool advertised (mutation tools on).
                    HttpOutcome settings = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + tenant + "/chat/settings", _AdminBearerToken,
                        "{\"EnableMutationTools\":true}", cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, settings.Status, "Mutation tools enabled (body " + settings.Body + ")");

                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "aot-server@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    // Native streaming: a successful tool call, a failing tool call, then the answer.
                    fake.EnqueueToolCall("graph_all", "{}");
                    fake.EnqueueToolCall("graph_get", "{\"graphGuid\":\"not-a-guid\"}");
                    fake.EnqueueText("done with tools", 12, 4);
                    string stream = await AotServerStreamAsync(endpoint + "/v1.0/tenants/" + tenant + "/chat/completions", userBearer,
                        "{\"Message\":\"use tools\",\"Stream\":true,\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);
                    actual["Stream|native-tools"] = MaskAotServerVolatile(stream);

                    string firstRequest;
                    AssertTrue(fake.CapturedCompletionBodies.TryPeek(out firstRequest!), "The upstream received the completion request");
                    using (JsonDocument request = JsonDocument.Parse(firstRequest))
                    {
                        AssertTrue(request.RootElement.TryGetProperty("tools", out JsonElement tools), "The upstream request carries tools");
                        actual["ChatTools|native"] = tools.GetRawText();
                    }

                    // The persisted tool transcript of that turn.
                    HttpOutcome threads = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenant + "/chat/threads", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, threads.Status, "Threads list");
                    string threadGuid = FirstObjectGuid(threads.Body);
                    HttpOutcome turns = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenant + "/chat/threads/" + threadGuid + "/turns", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, turns.Status, "Turns list (body " + turns.Body + ")");
                    actual["Turn|tool-transcript"] = MaskAotServerVolatile(ToolTranscriptsOf(turns.Body));

                    // Native streaming failure after the retry budget.
                    for (int i = 0; i < 3; i++) fake.EnqueueFailure(500);
                    string failed = await AotServerStreamAsync(endpoint + "/v1.0/tenants/" + tenant + "/chat/completions", userBearer,
                        "{\"Message\":\"fail\",\"Stream\":true,\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);
                    actual["Stream|native-error"] = MaskAotServerVolatile(failed);
                    while (fake.CapturedCompletionBodies.TryDequeue(out string? _)) { }

                    // OpenAI-compatible streaming with a tool call, on a graph-scoped route.
                    string compatGraph = await ChatProvisionGraphAsync(endpoint, "aot-server-compat", cancellationToken).ConfigureAwait(false);
                    fake.EnqueueToolCall("graph_all", "{}");
                    fake.EnqueueText("compat answer", 9, 4);
                    string compat = await AotServerStreamAsync(endpoint + "/v1.0/tenants/" + tenant + "/graphs/" + compatGraph + "/chat/completions", userBearer,
                        "{\"messages\":[{\"role\":\"user\",\"content\":\"stream it\"}],\"stream\":true,\"stream_options\":{\"include_usage\":true}}",
                        cancellationToken).ConfigureAwait(false);
                    actual["Stream|compat-openai"] = MaskAotServerVolatile(compat);

                    AssertTrue(fake.CapturedCompletionBodies.TryPeek(out firstRequest!), "The upstream received the compatible completion request");
                    using (JsonDocument request = JsonDocument.Parse(firstRequest))
                    {
                        AssertTrue(request.RootElement.TryGetProperty("tools", out JsonElement tools), "The compatible upstream request carries tools");
                        actual["ChatTools|compat"] = tools.GetRawText();
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }

            await CompareAotServerBaseline(_ServerLiveBaselineFile, actual, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> AotServerStreamAsync(string url, string bearer, string body, CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                client.Timeout = TimeSpan.FromSeconds(120);
                request.Headers.Add("Authorization", "Bearer " + bearer);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    // Keep the data frames only; keepalive comments depend on timing.
                    List<string> frames = text.Split('\n')
                        .Select(l => l.TrimEnd('\r'))
                        .Where(l => l.StartsWith("data:", StringComparison.Ordinal))
                        .ToList();
                    return ((int)response.StatusCode) + "\n" + String.Join("\n", frames);
                }
            }
        }

        private static string SeedDataOf(string body)
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                return document.RootElement.TryGetProperty("Data", out JsonElement data) ? data.GetRawText() : "(none)";
            }
        }

        private static string FirstObjectGuid(string enumerationBody)
        {
            using (JsonDocument document = JsonDocument.Parse(enumerationBody))
            {
                JsonElement objects = document.RootElement.GetProperty("Objects");
                AssertTrue(objects.GetArrayLength() > 0, "The enumeration has at least one object");
                return objects[0].GetProperty("GUID").GetString()!;
            }
        }

        private static string ToolTranscriptsOf(string turnsBody)
        {
            List<string> transcripts = new List<string>();
            using (JsonDocument document = JsonDocument.Parse(turnsBody))
            {
                JsonElement root = document.RootElement;
                JsonElement objects = root.ValueKind == JsonValueKind.Array ? root : root.GetProperty("Objects");
                foreach (JsonElement turn in objects.EnumerateArray())
                {
                    foreach (JsonProperty property in turn.EnumerateObject())
                    {
                        if (!property.Name.Contains("Tool", StringComparison.Ordinal)) continue;
                        string value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()! : property.Value.GetRawText();
                        transcripts.Add(property.Name + "=" + value);
                    }
                }
            }

            return String.Join("\n", transcripts);
        }

        private static string MaskAotServerClock(string json)
        {
            // Types that create a Timestamps.Timestamp in their constructor stamp the current time (compact and indented).
            json = Regex.Replace(json, "\"(Start|End)\":(\\s*)\"[^\"]*\"", "\"$1\":$2\"MASKED\"");
            return Regex.Replace(json, "\"TotalMs\":(\\s*)[-0-9.Ee+]+", "\"TotalMs\":${1}0");
        }

        private static string MaskAotServerVolatile(string text)
        {
            text = Regex.Replace(text, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "GUID");
            text = Regex.Replace(text, "\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(\\.\\d+)?Z?", "TIMESTAMP");
            text = Regex.Replace(text, "(\\\\?\"(?:[A-Za-z]*Ms|[A-Za-z]*ms|created|Created|TokensPerSecond[A-Za-z]*)\\\\?\":\\s*)[-0-9.Ee+]+", "${1}0");
            text = Regex.Replace(text, "chatcmpl-[A-Za-z0-9]+", "chatcmpl-ID");
            text = Regex.Replace(text, "call_[A-Za-z0-9]+", "call_ID");
            return text;
        }

        private static async Task CompareAotServerBaseline(string file, SortedDictionary<string, string> actual, CancellationToken token)
        {
            string? captureDirectory = Environment.GetEnvironmentVariable(_AotCaptureEnvironmentVariable);
            if (!String.IsNullOrEmpty(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                string json = JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(captureDirectory, file), json, token).ConfigureAwait(false);
                return;
            }

            string baselinePath = Path.Combine(AppContext.BaseDirectory, "Baselines", file);
            AssertTrue(File.Exists(baselinePath), "Baseline exists at " + baselinePath);
            Dictionary<string, string> expected =
                JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(baselinePath, token).ConfigureAwait(false))
                ?? new Dictionary<string, string>();

            List<string> failures = new List<string>();
            foreach (KeyValuePair<string, string> entry in expected)
            {
                if (!actual.TryGetValue(entry.Key, out string? value)) failures.Add(entry.Key + ": no longer produced");
                else if (!String.Equals(entry.Value, value, StringComparison.Ordinal)) failures.Add(entry.Key + ":\n    expected " + entry.Value + "\n    actual   " + value);
            }

            foreach (string key in actual.Keys)
            {
                if (!expected.ContainsKey(key)) failures.Add(key + ": not in baseline (recapture baselines to add it)");
            }

            AssertTrue(failures.Count == 0, file + " differs for " + failures.Count + " entr(ies):\n  " + String.Join("\n  ", failures));
        }

        #endregion
    }
}
