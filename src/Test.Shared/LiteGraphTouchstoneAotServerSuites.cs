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

        // Request bodies and other JSON the server deserializes (Serializer.DeserializeJson<T> and CopyObject<T> in
        // LiteGraph.Server). Add a type here when the server starts deserializing it.
        private static readonly Type[] _AotServerDeserializedTypes = new Type[]
        {
            typeof(LiteGraph.Server.Classes.AuthenticationToken),
            typeof(AuthorizationRole),
            typeof(LiteGraph.Server.Classes.BackupRequest),
            typeof(CachingSettings),
            typeof(LiteGraph.Server.Classes.ChatCompletionRequest),
            typeof(ChatEndpoint),
            typeof(ChatFeedback),
            typeof(ChatSettings),
            typeof(ChatThread),
            typeof(LiteGraph.Server.Classes.ClusterJobRun),
            typeof(LiteGraph.Server.Classes.ClusterNode),
            typeof(Credential),
            typeof(CredentialScopeAssignment),
            typeof(Dictionary<string, object>),
            typeof(Edge),
            typeof(EnumerationRequest),
            typeof(ExistenceRequest),
            typeof(LiteGraph.Server.Classes.GenerateEmbeddingsRequest),
            typeof(Graph),
            typeof(LiteGraph.Algorithms.GraphAlgorithmImportRequest),
            typeof(LiteGraph.Algorithms.GraphAlgorithmRequest),
            typeof(GraphQueryRequest),
            typeof(LabelMetadata),
            typeof(List<Edge>),
            typeof(List<Guid>),
            typeof(List<LabelMetadata>),
            typeof(List<Node>),
            typeof(List<TagMetadata>),
            typeof(List<VectorMetadata>),
            typeof(Node),
            typeof(LiteGraph.Server.Classes.OllamaChatRequest),
            typeof(LiteGraph.Server.Classes.OpenAiChatCompletionRequest),
            typeof(LiteGraph.Server.Classes.RouteRequest),
            typeof(SearchRequest),
            typeof(LiteGraph.Server.Classes.Settings),
            typeof(SubgraphExtractionRequest),
            typeof(TagMetadata),
            typeof(TenantMetadata),
            typeof(LiteGraph.Server.Classes.TenantOnboardRequest),
            typeof(TransactionRequest),
            typeof(TransactionResult),
            typeof(UserMaster),
            typeof(UserRoleAssignment),
            typeof(VectorIndexConfiguration),
            typeof(VectorMetadata),
            typeof(VectorSearchRequest)
        };

        // Client results that are never serialized (fluent builders).
        private static readonly HashSet<Type> _AotNonSerializedResultTypes = new HashSet<Type>
        {
            typeof(TransactionRequestBuilder)
        };

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
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.ContextCoverage", "Every server type has source-generated metadata", TestAotServerContextCoverage),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.ResponseTypeCoverage", "Every LiteGraphClient result type and every type the server deserializes has source-generated metadata", TestAotServerResponseTypeCoverage),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.SslSettings", "Settings with a PFX certificate configured serialize without the certificate and round-trip", TestAotServerSslSettings),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.TypeParity", "Every server type serializes as in the baseline (compact, indented, round trip)", TestAotServerTypeParity),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.DefaultSettings", "A default settings file is written as in the baseline", TestAotServerDefaultSettings),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.PayloadShapes", "Chat stream events, tool transcripts, and other built payloads serialize as in the baseline", TestAotServerPayloadShapes),
                    new TestCaseDescriptor("Aot.Server", "Aot.Server.Live", "OpenAPI, seed data, chat tool schemas, stream events, and tool transcripts match the baseline", TestAotServerLive)
                },
                afterSuiteAsync: CleanupMcpSuiteAsync);
        }

        private static async Task TestAotServerTypeParity(CancellationToken token)
        {
            LiteGraph.Server.Classes.ServerJson.Register();
            using AotServerDirectoryGuard directories = new AotServerDirectoryGuard();
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
            LiteGraph.Server.Classes.ServerJson.Register();
            using AotServerDirectoryGuard directories = new AotServerDirectoryGuard();
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
            // events), with values of every shape: nulls, optional numbers, and nested lists. The baseline was captured from
            // the anonymous objects these types replaced.
            LiteGraph.Server.Classes.ServerJson.Register();
            Serializer serializer = new Serializer();
            Guid nodeGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");
            Guid threadGuid = Guid.Parse("66666666-7777-8888-9999-000000000000");
            LiteGraph.Server.Classes.ChatCompletionResult usage = (LiteGraph.Server.Classes.ChatCompletionResult)CreateAotSample(typeof(LiteGraph.Server.Classes.ChatCompletionResult), 0);

            SortedDictionary<string, object> payloads = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                { "started", new LiteGraph.Server.Classes.ChatStreamStartedEvent { ThreadGuid = threadGuid, TurnGuid = nodeGuid } },
                { "error-upstream", new LiteGraph.Server.Classes.ChatStreamErrorEvent { Message = "HTTP 500", StatusCode = 500 } },
                { "error-upstream-nostatus", new LiteGraph.Server.Classes.ChatStreamErrorEvent { Message = "HTTP failure", StatusCode = null } },
                { "error", new LiteGraph.Server.Classes.ChatStreamErrorEvent { Message = "failure" } },
                { "usage", new LiteGraph.Server.Classes.ChatStreamUsageEvent { Usage = usage } },
                { "retrieval", new LiteGraph.Server.Classes.ChatStreamRetrievalEvent { Chunks = new List<LiteGraph.Server.Classes.ChatRetrievalChunk>
                    {
                        new LiteGraph.Server.Classes.ChatRetrievalChunk { NodeGuid = nodeGuid, Name = "first", Score = 0.875f },
                        new LiteGraph.Server.Classes.ChatRetrievalChunk { NodeGuid = null, Name = null, Score = null }
                    } } },
                { "retrieval-empty", new LiteGraph.Server.Classes.ChatStreamRetrievalEvent { Chunks = new List<LiteGraph.Server.Classes.ChatRetrievalChunk>() } },
                { "delta", new LiteGraph.Server.Classes.ChatStreamContentEvent { Event = "delta", Content = "text \"quoted\" <b>" } },
                { "thinking", new LiteGraph.Server.Classes.ChatStreamContentEvent { Event = "thinking", Content = "reasoning" } },
                { "tool_call", new LiteGraph.Server.Classes.ChatStreamToolCallEvent { Name = "graph_get", Arguments = "{\"graphGuid\":\"x\"}", Iteration = 2 } },
                { "tool_result", new LiteGraph.Server.Classes.ChatStreamToolResultEvent { Name = "graph_get", Success = true, Error = null, RuntimeMs = 12.5 } },
                { "tool_result-error", new LiteGraph.Server.Classes.ChatStreamToolResultEvent { Name = "graph_get", Success = false, Error = "bad", RuntimeMs = 0.0 } },
                { "transcript", new List<LiteGraph.Server.Classes.ChatToolTranscriptEntry>
                    {
                        new LiteGraph.Server.Classes.ChatToolTranscriptEntry { Iteration = 1, Name = "graph_all", Arguments = "{}", Success = true, Error = null, RuntimeMs = 3.25 },
                        new LiteGraph.Server.Classes.ChatToolTranscriptEntry { Iteration = 2, Name = "graph_get", Arguments = "{}", Success = false, Error = "bad", RuntimeMs = 0.0 }
                    } },
                { "tool-error-content", new LiteGraph.Server.Classes.ChatToolErrorContent { Error = "bad" } },
                { "tool-error-content-null", new LiteGraph.Server.Classes.ChatToolErrorContent { Error = null } },
                { "request-history-deleted", new Dictionary<string, object> { { "Deleted", 42 } } },
                { "seed-data", new Dictionary<string, object> { { "description", "Default LiteGraph API service node." } } }
            };

            SortedDictionary<string, string> actual = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> payload in payloads)
            {
                actual["Payload|" + payload.Key + "|compact"] = MaskAotServerClock(serializer.SerializeJson(payload.Value, false));
                actual["Payload|" + payload.Key + "|pretty"] = MaskAotServerClock(serializer.SerializeJson(payload.Value, true));
            }

            await CompareAotServerBaseline("server-payload-baseline.json", actual, token).ConfigureAwait(false);
        }

        private static Task TestAotServerContextCoverage(CancellationToken token)
        {
            // Under Native AOT (and in the server, which turns reflection-based serialization off in every build) a type
            // without metadata fails; this catches one here, where reflection would otherwise hide it.
            JsonSerializerOptions options = new JsonSerializerOptions();
            List<string> missing = new List<string>();
            foreach (Type type in _AotServerParityTypes)
            {
                if (LiteGraphJsonContext.Default.GetTypeInfo(type) == null && LiteGraph.Server.Classes.ServerJson.Resolver.GetTypeInfo(type, options) == null)
                    missing.Add(AotTypeKey(type));
            }

            AssertTrue(missing.Count == 0, "No source-generated metadata for: " + String.Join(", ", missing));
            return Task.CompletedTask;
        }

        private static Task TestAotServerResponseTypeCoverage(CancellationToken token)
        {
            // The REST server returns what the library client methods return, often as is, so every result type needs
            // metadata (an endpoint the suites never call would otherwise fail only in production). Enumerable results are
            // returned as List<T>.
            JsonSerializerOptions options = new JsonSerializerOptions();
            SortedSet<string> missing = new SortedSet<string>(StringComparer.Ordinal);

            foreach (System.Reflection.PropertyInfo group in typeof(LiteGraphClient).GetProperties())
            {
                if (!group.PropertyType.IsInterface) continue;

                foreach (System.Reflection.MethodInfo method in group.PropertyType.GetMethods())
                {
                    Type? result = AotResultType(method.ReturnType);
                    if (result == null || _AotNonSerializedResultTypes.Contains(result)) continue;

                    List<Type> required = new List<Type> { result };

                    // The server wraps list results in an enumeration envelope (EnumerationResultBuilder.FromList).
                    if (result.IsGenericType && result.GetGenericTypeDefinition() == typeof(List<>))
                        required.Add(typeof(EnumerationResult<>).MakeGenericType(result.GetGenericArguments()[0]));

                    foreach (Type type in required)
                    {
                        if (LiteGraphJsonContext.Default.GetTypeInfo(type) != null) continue;
                        if (LiteGraph.Server.Classes.ServerJson.Resolver.GetTypeInfo(type, options) != null) continue;
                        missing.Add(AotTypeKey(type) + " (" + group.Name + "." + method.Name + ")");
                    }
                }
            }

            foreach (Type type in _AotServerDeserializedTypes)
            {
                if (LiteGraphJsonContext.Default.GetTypeInfo(type) != null) continue;
                if (LiteGraph.Server.Classes.ServerJson.Resolver.GetTypeInfo(type, options) != null) continue;
                missing.Add(AotTypeKey(type) + " (deserialized by the server)");
            }

            AssertTrue(missing.Count == 0, "No source-generated metadata for " + missing.Count + " result type(s):\n  " + String.Join("\n  ", missing));
            return Task.CompletedTask;
        }

        private static Type? AotResultType(Type type)
        {
            if (type == typeof(void) || type == typeof(Task) || type == typeof(ValueTask)) return null;

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                Type argument = type.GetGenericArguments()[0];
                if (definition == typeof(Task<>) || definition == typeof(ValueTask<>)) return AotResultType(argument);
                if (definition == typeof(IAsyncEnumerable<>) || definition == typeof(IEnumerable<>)) return typeof(List<>).MakeGenericType(argument);
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime)) return null;
            return type;
        }

        private static Task TestAotServerSslSettings(CancellationToken token)
        {
            LiteGraph.Server.Classes.ServerJson.Register();
            string directory = Path.Combine(Path.GetTempPath(), "litegraph-aot-ssl-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                string pfx = Path.Combine(directory, "server.pfx");
                using (System.Security.Cryptography.RSA rsa = System.Security.Cryptography.RSA.Create(2048))
                {
                    System.Security.Cryptography.X509Certificates.CertificateRequest request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                        "CN=litegraph-test", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                    using (System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)))
                    {
                        File.WriteAllBytes(pfx, certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, "secret"));
                    }
                }

                LiteGraph.Server.Classes.Settings settings = new LiteGraph.Server.Classes.Settings();
                settings.Rest.Ssl.Enable = true;
                settings.Rest.Ssl.PfxCertificateFile = pfx;
                settings.Rest.Ssl.PfxCertificatePassword = "secret";
                AssertNotNull(settings.Rest.Ssl.SslCertificate, "The certificate loads from the PFX file");

                Serializer serializer = new Serializer();
                string json = serializer.SerializeJson(settings, true);
                AssertFalse(json.Contains("SslCertificate", StringComparison.Ordinal), "The certificate object is not written");

                LiteGraph.Server.Classes.Settings read = serializer.DeserializeJson<LiteGraph.Server.Classes.Settings>(json);
                AssertTrue(read.Rest.Ssl.Enable, "SSL stays enabled");
                AssertEqual(pfx, read.Rest.Ssl.PfxCertificateFile, "The PFX file round-trips");
                AssertEqual("secret", read.Rest.Ssl.PfxCertificatePassword, "The PFX password round-trips");
                AssertNotNull(read.Rest.Ssl.SslCertificate, "The certificate loads from the round-tripped settings");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }

            return Task.CompletedTask;
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
            // Indented JSON uses the platform's line ending (CRLF on Windows); baselines use LF.
            foreach (string key in actual.Keys.ToList()) actual[key] = actual[key].Replace("\r\n", "\n");

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
