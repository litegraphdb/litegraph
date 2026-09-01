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

    public static partial class LiteGraphTouchstoneSuites
    {
        #region Chat-Suites

        private static TestSuiteDescriptor CreateChatStorageSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Chat.Storage",
                displayName: "v8.1 chat entities: storage, validation, and cascades",
                cases: new List<TestCaseDescriptor>
                {
                    ChatCase("Chat.Storage", "Chat.Storage.EndpointCrud", "Endpoint CRUD round-trip with type filter", TestChatEndpointCrud),
                    ChatCase("Chat.Storage", "Chat.Storage.EndpointValidation", "Invalid endpoints are rejected", TestChatEndpointValidation),
                    ChatCase("Chat.Storage", "Chat.Storage.RedactedKeyPreserved", "Updating with a redacted key preserves the stored key", TestChatRedactedKeyPreserved),
                    ChatCase("Chat.Storage", "Chat.Storage.ThreadTurnLifecycle", "Threads, turn sequencing, and cascade delete", TestChatThreadTurnLifecycle),
                    ChatCase("Chat.Storage", "Chat.Storage.Feedback", "Feedback creation and rating filter", TestChatFeedbackStorage),
                    ChatCase("Chat.Storage", "Chat.Storage.Settings", "Tenant chat settings upsert and validation", TestChatSettingsStorage),
                    ChatCase("Chat.Storage", "Chat.Storage.Retention", "Turn retention pruning by cutoff", TestChatRetention),
                    ChatCase("Chat.Storage", "Chat.Storage.TenantIsolation", "Chat objects are invisible across tenants", TestChatTenantIsolation),
                    ChatCase("Chat.Storage", "Chat.Storage.TenantCascade", "Force-deleting a tenant removes its chat objects", TestChatTenantCascade)
                });
        }

        private static TestSuiteDescriptor CreateChatRestSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Chat.Rest",
                displayName: "v8.1 chat REST surface over a live server with a fake LLM upstream",
                cases: new List<TestCaseDescriptor>
                {
                    ChatCase("Chat.Rest", "Chat.Rest.EndpointCrudAndAuthz", "Endpoint CRUD, redaction, validation, and authorization", TestChatRestEndpointCrud),
                    ChatCase("Chat.Rest", "Chat.Rest.SettingsRoundTrip", "Chat settings round-trip and non-admin denial", TestChatRestSettings),
                    ChatCase("Chat.Rest", "Chat.Rest.CompletionHappyPath", "Non-streaming completion persists a successful turn", TestChatRestCompletion),
                    ChatCase("Chat.Rest", "Chat.Rest.ToolLoop", "Tool call round-trip through the in-process dispatcher", TestChatRestToolLoop),
                    ChatCase("Chat.Rest", "Chat.Rest.RetryThenSuccess", "429 responses are retried before the first token", TestChatRestRetry),
                    ChatCase("Chat.Rest", "Chat.Rest.RetriesExhausted", "Failures beyond the retry budget yield 502 and a failed turn", TestChatRestRetriesExhausted),
                    ChatCase("Chat.Rest", "Chat.Rest.Streaming", "SSE stream carries started, delta, usage, and DONE frames", TestChatRestStreaming),
                    ChatCase("Chat.Rest", "Chat.Rest.Feedback", "Feedback submit, admin list, and delete", TestChatRestFeedback),
                    ChatCase("Chat.Rest", "Chat.Rest.ThreadOwnership", "Threads are private to their owner", TestChatRestThreadOwnership),
                    ChatCase("Chat.Rest", "Chat.Rest.Metrics", "Chat metrics appear on the metrics endpoint", TestChatRestMetrics),
                    ChatCase("Chat.Rest", "Chat.Rest.ToolCatalogParity", "Every chat-advertised tool exists in the MCP catalog", TestChatToolCatalogParity),
                    ChatCase("Chat.Rest", "Chat.Rest.EndpointReadUpdateTest", "Endpoint read, exists, update, and connectivity test over HTTP", TestChatRestEndpointReadUpdateTest),
                    ChatCase("Chat.Rest", "Chat.Rest.EndpointHealthRoutes", "Endpoint health routes report monitored state and reject unknown endpoints", TestChatRestEndpointHealthRoutes),
                    ChatCase("Chat.Rest", "Chat.Rest.FeedbackReadAndNegatives", "Single feedback read, unknown-GUID deletes, and cross-user turn denial", TestChatRestFeedbackReadAndNegatives),
                    ChatCase("Chat.Rest", "Chat.Rest.McpChatTools", "MCP chat tools round-trip endpoint and settings operations", TestChatRestMcpChatTools)
                });
        }

        private static TestCaseDescriptor ChatCase(string suiteId, string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: suiteId, caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region Chat-Storage-Cases

        private static async Task TestChatEndpointCrud(CancellationToken token)
        {
            string db = ChatDbName("ep-crud");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);

                ChatEndpoint completion = await client.ChatEndpoint.Create(ChatCompletionEndpoint(tenant, "completion-a"), token).ConfigureAwait(false);
                ChatEndpoint embedding = await client.ChatEndpoint.Create(ChatEmbeddingEndpoint(tenant, "embedding-a"), token).ConfigureAwait(false);

                ChatEndpoint read = await client.ChatEndpoint.ReadByGuid(tenant, completion.GUID, token).ConfigureAwait(false);
                AssertNotNull(read, "Created endpoint reads back");
                AssertEqual("completion-a", read!.Name, "Endpoint name round-trips");
                AssertEqual(ChatProviderTypeEnum.OpenAI.ToString(), read.Provider.ToString(), "Provider round-trips");

                List<ChatEndpoint> all = new List<ChatEndpoint>();
                await foreach (ChatEndpoint e in client.ChatEndpoint.ReadAllInTenant(tenant, null, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false)) all.Add(e);
                AssertEqual(2, all.Count, "Both endpoints listed without a filter");

                List<ChatEndpoint> embeddingsOnly = new List<ChatEndpoint>();
                await foreach (ChatEndpoint e in client.ChatEndpoint.ReadAllInTenant(tenant, ChatEndpointTypeEnum.Embedding, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false)) embeddingsOnly.Add(e);
                AssertEqual(1, embeddingsOnly.Count, "Type filter returns only embedding endpoints");
                AssertEqual(embedding.GUID.ToString(), embeddingsOnly[0].GUID.ToString(), "Filtered endpoint is the embedding endpoint");

                read.Name = "completion-renamed";
                ChatEndpoint updated = await client.ChatEndpoint.Update(read, token).ConfigureAwait(false);
                AssertEqual("completion-renamed", updated.Name, "Update persists the new name");

                await client.ChatEndpoint.DeleteByGuid(tenant, completion.GUID, token).ConfigureAwait(false);
                AssertFalse(await client.ChatEndpoint.ExistsByGuid(tenant, completion.GUID, token).ConfigureAwait(false), "Deleted endpoint no longer exists");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatEndpointValidation(CancellationToken token)
        {
            string db = ChatDbName("ep-validation");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);

                await ChatAssertThrows<ArgumentException>(
                    () => client.ChatEndpoint.Create(new ChatEndpoint { TenantGUID = tenant, Name = "bad", EndpointType = ChatEndpointTypeEnum.Embedding, Provider = ChatProviderTypeEnum.Anthropic, Endpoint = "https://api.anthropic.com", Model = "claude" }, token),
                    "Anthropic embedding endpoint is rejected").ConfigureAwait(false);

                await ChatAssertThrows<ArgumentException>(
                    () => client.ChatEndpoint.Create(new ChatEndpoint { TenantGUID = tenant, Name = "bad", EndpointType = ChatEndpointTypeEnum.Completion, Provider = ChatProviderTypeEnum.VoyageAI, Endpoint = "https://api.voyageai.com", Model = "voyage-3.5" }, token),
                    "VoyageAI completion endpoint is rejected").ConfigureAwait(false);

                await ChatAssertThrows<ArgumentException>(
                    () => client.ChatEndpoint.Create(new ChatEndpoint { TenantGUID = tenant, Name = "bad", Endpoint = "not-a-url", Model = "m" }, token),
                    "Malformed endpoint URL is rejected").ConfigureAwait(false);

                await ChatAssertThrows<ArgumentException>(
                    () => client.ChatEndpoint.Create(new ChatEndpoint { TenantGUID = tenant, Name = "bad", Endpoint = "http://127.0.0.1:9", Model = null }, token),
                    "Missing model is rejected").ConfigureAwait(false);
            }
            ChatCleanup(db);
        }

        private static async Task TestChatRedactedKeyPreserved(CancellationToken token)
        {
            string db = ChatDbName("ep-redact");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);

                ChatEndpoint endpoint = ChatCompletionEndpoint(tenant, "secure");
                endpoint.ApiKey = "sk-super-secret-1234";
                ChatEndpoint created = await client.ChatEndpoint.Create(endpoint, token).ConfigureAwait(false);

                ChatEndpoint redacted = created.Redact();
                AssertTrue(ChatEndpoint.IsRedactedApiKey(redacted.ApiKey), "Redact produces a redacted placeholder");
                AssertTrue(redacted.ApiKey.EndsWith("1234", StringComparison.Ordinal), "Redacted key keeps the last four characters");

                redacted.Name = "secure-renamed";
                ChatEndpoint updated = await client.ChatEndpoint.Update(redacted, token).ConfigureAwait(false);

                ChatEndpoint reread = await client.ChatEndpoint.ReadByGuid(tenant, created.GUID, token).ConfigureAwait(false);
                AssertEqual("sk-super-secret-1234", reread!.ApiKey, "Stored key survives an update carrying the redacted placeholder");
                AssertEqual("secure-renamed", reread.Name, "Non-secret fields still update");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatThreadTurnLifecycle(CancellationToken token)
        {
            string db = ChatDbName("thread-turns");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid user = await ChatSeedUser(client, tenant).ConfigureAwait(false);

                ChatThread thread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user, Title = "lifecycle" }, token).ConfigureAwait(false);

                AssertEqual(-1, await client.ChatTurn.GetMaxSequence(tenant, thread.GUID, token).ConfigureAwait(false), "Empty thread max sequence is -1");

                for (int i = 0; i < 3; i++)
                {
                    await client.ChatTurn.Create(new ChatTurn
                    {
                        TenantGUID = tenant,
                        ThreadGUID = thread.GUID,
                        Sequence = i,
                        UserMessage = "q" + i,
                        AssistantResponse = "a" + i
                    }, token).ConfigureAwait(false);
                }

                AssertEqual(2, await client.ChatTurn.GetMaxSequence(tenant, thread.GUID, token).ConfigureAwait(false), "Max sequence reflects the last turn");
                AssertEqual(3, await client.ChatTurn.GetCountByThread(tenant, thread.GUID, token).ConfigureAwait(false), "Turn count matches");

                List<ChatTurn> turns = new List<ChatTurn>();
                await foreach (ChatTurn t in client.ChatTurn.ReadByThread(tenant, thread.GUID, true, 0, token).ConfigureAwait(false)) turns.Add(t);
                AssertEqual("q0", turns[0].UserMessage, "Turns come back in ascending sequence order");
                AssertEqual("q2", turns[2].UserMessage, "Last turn is last");

                await ChatAssertThrows<KeyNotFoundException>(
                    () => client.ChatTurn.Create(new ChatTurn { TenantGUID = tenant, ThreadGUID = Guid.NewGuid(), UserMessage = "orphan" }, token),
                    "Turn creation against an unknown thread is rejected").ConfigureAwait(false);

                await client.ChatThread.DeleteByGuid(tenant, thread.GUID, token).ConfigureAwait(false);
                AssertEqual(0, await client.ChatTurn.GetCountByThread(tenant, thread.GUID, token).ConfigureAwait(false), "Deleting the thread removes its turns");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatFeedbackStorage(CancellationToken token)
        {
            string db = ChatDbName("feedback");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid user = await ChatSeedUser(client, tenant).ConfigureAwait(false);

                ChatThread thread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user }, token).ConfigureAwait(false);
                ChatTurn turn = await client.ChatTurn.Create(new ChatTurn { TenantGUID = tenant, ThreadGUID = thread.GUID, UserMessage = "q", AssistantResponse = "a" }, token).ConfigureAwait(false);

                ChatFeedback created = await client.ChatFeedback.Create(new ChatFeedback
                {
                    TenantGUID = tenant,
                    TurnGUID = turn.GUID,
                    UserGUID = user,
                    Rating = ChatFeedbackRatingEnum.ThumbsDown,
                    FeedbackText = "wrong answer"
                }, token).ConfigureAwait(false);

                AssertEqual(thread.GUID.ToString(), created.ThreadGUID.ToString(), "Thread GUID is inferred from the turn");

                await ChatAssertThrows<KeyNotFoundException>(
                    () => client.ChatFeedback.Create(new ChatFeedback { TenantGUID = tenant, TurnGUID = Guid.NewGuid(), UserGUID = user }, token),
                    "Feedback against an unknown turn is rejected").ConfigureAwait(false);

                List<ChatFeedback> down = new List<ChatFeedback>();
                await foreach (ChatFeedback f in client.ChatFeedback.ReadAllInTenant(tenant, ChatFeedbackRatingEnum.ThumbsDown, null, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false)) down.Add(f);
                AssertEqual(1, down.Count, "Rating filter returns the thumbs-down record");

                List<ChatFeedback> up = new List<ChatFeedback>();
                await foreach (ChatFeedback f in client.ChatFeedback.ReadAllInTenant(tenant, ChatFeedbackRatingEnum.ThumbsUp, null, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false)) up.Add(f);
                AssertEqual(0, up.Count, "No thumbs-up records exist");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatSettingsStorage(CancellationToken token)
        {
            string db = ChatDbName("settings");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);

                AssertNull(await client.ChatSettings.ReadByTenant(tenant, token).ConfigureAwait(false), "No settings record exists initially");

                ChatEndpoint completion = await client.ChatEndpoint.Create(ChatCompletionEndpoint(tenant, "c"), token).ConfigureAwait(false);
                ChatEndpoint embedding = await client.ChatEndpoint.Create(ChatEmbeddingEndpoint(tenant, "e"), token).ConfigureAwait(false);

                ChatSettings settings = new ChatSettings
                {
                    TenantGUID = tenant,
                    DefaultCompletionEndpointGUID = completion.GUID,
                    DefaultEmbeddingEndpointGUID = embedding.GUID,
                    EnableMutationTools = true,
                    RagTopK = 5
                };

                ChatSettings upserted = await client.ChatSettings.Upsert(settings, token).ConfigureAwait(false);
                AssertEqual(5, upserted.RagTopK, "RagTopK round-trips");
                AssertTrue(upserted.EnableMutationTools, "Mutation opt-in round-trips");

                upserted.RagTopK = 9;
                ChatSettings second = await client.ChatSettings.Upsert(upserted, token).ConfigureAwait(false);
                AssertEqual(9, second.RagTopK, "Second upsert updates in place");

                await ChatAssertThrows<ArgumentException>(
                    () => client.ChatSettings.Upsert(new ChatSettings { TenantGUID = tenant, DefaultCompletionEndpointGUID = embedding.GUID }, token),
                    "An embedding endpoint cannot be the default completion endpoint").ConfigureAwait(false);
            }
            ChatCleanup(db);
        }

        private static async Task TestChatRetention(CancellationToken token)
        {
            string db = ChatDbName("retention");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid user = await ChatSeedUser(client, tenant).ConfigureAwait(false);
                ChatThread thread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user }, token).ConfigureAwait(false);

                ChatTurn oldTurn = new ChatTurn { TenantGUID = tenant, ThreadGUID = thread.GUID, Sequence = 0, UserMessage = "old" };
                oldTurn.CreatedUtc = DateTime.UtcNow.AddDays(-30);
                await client.ChatTurn.Create(oldTurn, token).ConfigureAwait(false);
                await client.ChatTurn.Create(new ChatTurn { TenantGUID = tenant, ThreadGUID = thread.GUID, Sequence = 1, UserMessage = "new" }, token).ConfigureAwait(false);

                await client.ChatTurn.DeleteOlderThan(tenant, DateTime.UtcNow.AddDays(-7), token).ConfigureAwait(false);

                List<ChatTurn> remaining = new List<ChatTurn>();
                await foreach (ChatTurn t in client.ChatTurn.ReadByThread(tenant, thread.GUID, true, 0, token).ConfigureAwait(false)) remaining.Add(t);
                AssertEqual(1, remaining.Count, "Only the recent turn survives the retention cutoff");
                AssertEqual("new", remaining[0].UserMessage, "The surviving turn is the recent one");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatTenantIsolation(CancellationToken token)
        {
            string db = ChatDbName("isolation");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenantA = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid tenantB = await ChatSeedTenant(client).ConfigureAwait(false);

                ChatEndpoint endpoint = await client.ChatEndpoint.Create(ChatCompletionEndpoint(tenantA, "a-only"), token).ConfigureAwait(false);

                AssertNull(await client.ChatEndpoint.ReadByGuid(tenantB, endpoint.GUID, token).ConfigureAwait(false), "Tenant B cannot read tenant A's endpoint");

                List<ChatEndpoint> bList = new List<ChatEndpoint>();
                await foreach (ChatEndpoint e in client.ChatEndpoint.ReadAllInTenant(tenantB, null, EnumerationOrderEnum.CreatedDescending, 0, token).ConfigureAwait(false)) bList.Add(e);
                AssertEqual(0, bList.Count, "Tenant B's endpoint list is empty");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatTenantCascade(CancellationToken token)
        {
            string db = ChatDbName("cascade");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid user = await ChatSeedUser(client, tenant).ConfigureAwait(false);

                ChatEndpoint endpoint = await client.ChatEndpoint.Create(ChatCompletionEndpoint(tenant, "cascade"), token).ConfigureAwait(false);
                ChatThread thread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user }, token).ConfigureAwait(false);
                ChatTurn turn = await client.ChatTurn.Create(new ChatTurn { TenantGUID = tenant, ThreadGUID = thread.GUID, UserMessage = "q" }, token).ConfigureAwait(false);
                await client.ChatSettings.Upsert(new ChatSettings { TenantGUID = tenant }, token).ConfigureAwait(false);

                await client.Tenant.DeleteByGuid(tenant, true, token).ConfigureAwait(false);

                AssertNull(await client.ChatEndpoint.ReadByGuid(tenant, endpoint.GUID, token).ConfigureAwait(false), "Endpoint removed with tenant");
                AssertNull(await client.ChatThread.ReadByGuid(tenant, thread.GUID, token).ConfigureAwait(false), "Thread removed with tenant");
                AssertNull(await client.ChatTurn.ReadByGuid(tenant, turn.GUID, token).ConfigureAwait(false), "Turn removed with tenant");
                AssertNull(await client.ChatSettings.ReadByTenant(tenant, token).ConfigureAwait(false), "Settings removed with tenant");
            }
            ChatCleanup(db);
        }

        #endregion

        #region Chat-Rest-Cases

        private static async Task TestChatRestEndpointCrud(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints";

                HttpOutcome created = await AuthRestAsync(HttpMethod.Put, baseUrl, _AdminBearerToken,
                    "{\"Name\":\"rest-crud\",\"EndpointType\":\"Completion\",\"Provider\":\"OpenAI\",\"Endpoint\":\"http://127.0.0.1:9\",\"Model\":\"m\",\"ApiKey\":\"sk-secret-9999\",\"HealthCheckEnabled\":false}",
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(200, created.Status, "Endpoint create succeeds (status " + created.Status + " body " + created.Body + ")");
                AssertTrue(created.Body.Contains("********9999"), "Create response redacts the API key");
                string endpointGuid = ExtractGuid(created.Body);

                HttpOutcome list = await AuthRestAsync(HttpMethod.Get, baseUrl, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, list.Status, "Endpoint list succeeds");
                AssertFalse(list.Body.Contains("sk-secret-9999"), "The raw API key never appears in list responses");

                HttpOutcome invalid = await AuthRestAsync(HttpMethod.Put, baseUrl, _AdminBearerToken,
                    "{\"Name\":\"bad\",\"EndpointType\":\"Completion\",\"Provider\":\"VoyageAI\",\"Endpoint\":\"https://api.voyageai.com\",\"Model\":\"voyage-3.5\"}",
                    cancellationToken).ConfigureAwait(false);
                AssertEqual(400, invalid.Status, "VoyageAI completion endpoint returns 400");
                AssertTrue(invalid.Body.Contains("embeddings-only"), "The 400 explains the provider limitation");

                string regularBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-crud@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                HttpOutcome denied = await AuthRestAsync(HttpMethod.Put, baseUrl, regularBearer,
                    "{\"Name\":\"nope\",\"EndpointType\":\"Completion\",\"Provider\":\"OpenAI\",\"Endpoint\":\"http://127.0.0.1:9\",\"Model\":\"m\"}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(denied.Status == 401 || denied.Status == 403, "Regular users cannot create endpoints (status " + denied.Status + ")");

                HttpOutcome deleted = await AuthRestAsync(HttpMethod.Delete, baseUrl + "/" + endpointGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(deleted.Status), "Endpoint delete succeeds (status " + deleted.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestSettings(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string url = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/settings";

                HttpOutcome defaults = await AuthRestAsync(HttpMethod.Get, url, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, defaults.Status, "Chat settings read succeeds with no record");
                AssertTrue(defaults.Body.Contains("\"EnableChat\":true") || defaults.Body.Contains("\"EnableChat\": true"), "Defaults report chat enabled");

                HttpOutcome updated = await AuthRestAsync(HttpMethod.Put, url, _AdminBearerToken,
                    "{\"RagTopK\":4,\"EnableMutationTools\":true}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, updated.Status, "Chat settings update succeeds (body " + updated.Body + ")");

                HttpOutcome reread = await AuthRestAsync(HttpMethod.Get, url, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(reread.Body.Contains("\"RagTopK\":4") || reread.Body.Contains("\"RagTopK\": 4"), "Updated RagTopK reads back");

                string regularBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-settings@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                HttpOutcome denied = await AuthRestAsync(HttpMethod.Put, url, regularBearer, "{\"RagTopK\":2}", cancellationToken).ConfigureAwait(false);
                AssertTrue(denied.Status == 401 || denied.Status == 403, "Regular users cannot update chat settings (status " + denied.Status + ")");

                HttpOutcome readAsUser = await AuthRestAsync(HttpMethod.Get, url, regularBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readAsUser.Status, "Regular users can read chat settings");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestCompletion(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-basic@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("The graph has 42 nodes.", 21, 7);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"how many nodes?\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(200, completion.Status, "Completion succeeds (body " + completion.Body + ")");
                    AssertTrue(completion.Body.Contains("The graph has 42 nodes."), "The fake model's answer is returned");
                    AssertTrue(completion.Body.Contains("\"PromptTokens\":") || completion.Body.Contains("\"PromptTokens\": "), "Usage is reported");

                    string threadGuid = ChatExtractJsonString(completion.Body, "ThreadGUID");
                    HttpOutcome turns = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid + "/turns",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, turns.Status, "Turns read back");
                    AssertTrue(turns.Body.Contains("\"Success\":true") || turns.Body.Contains("\"Success\": true"), "The persisted turn is successful");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestToolLoop(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-tools@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueToolCall("graph/all", "{}");
                    fake.EnqueueText("There are no graphs yet.", 30, 6);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"list graphs\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(200, completion.Status, "Tool-loop completion succeeds (body " + completion.Body + ")");
                    AssertTrue(completion.Body.Contains("There are no graphs yet."), "The final answer follows the tool call");
                    AssertTrue(completion.Body.Contains("\"ToolCallCount\":1") || completion.Body.Contains("\"ToolCallCount\": 1"), "One tool call was executed");
                    AssertTrue(completion.Body.Contains("\"ToolLoopIterations\":2") || completion.Body.Contains("\"ToolLoopIterations\": 2"), "The loop ran two iterations");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestRetry(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-retry@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueFailure(429);
                    fake.EnqueueFailure(429);
                    fake.EnqueueText("recovered", 5, 2);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"retry please\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(200, completion.Status, "Completion recovers after retries (body " + completion.Body + ")");
                    AssertTrue(completion.Body.Contains("recovered"), "The recovered answer is returned");
                    AssertTrue(completion.Body.Contains("\"RetryCount\":2") || completion.Body.Contains("\"RetryCount\": 2"), "Two retries were recorded");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestRetriesExhausted(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-exhaust@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueFailure(500);
                    fake.EnqueueFailure(500);
                    fake.EnqueueFailure(500);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"doomed\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(502, completion.Status, "Exhausted retries yield 502 (body " + completion.Body + ")");

                    HttpOutcome threads = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    string threadGuid;
                    using (JsonDocument threadsDoc = JsonDocument.Parse(threads.Body))
                    {
                        AssertTrue(threadsDoc.RootElement.GetArrayLength() > 0, "The failed completion still created a thread");
                        threadGuid = threadsDoc.RootElement[0].GetProperty("GUID").GetString() ?? String.Empty;
                    }
                    HttpOutcome turns = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid + "/turns",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(turns.Body.Contains("\"Success\":false") || turns.Body.Contains("\"Success\": false"), "The failed turn is persisted");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestStreaming(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-stream@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("streamed answer", 8, 3);

                    using (HttpClient client = new HttpClient())
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions"))
                    {
                        client.Timeout = TimeSpan.FromSeconds(60);
                        request.Headers.Add("Authorization", "Bearer " + userBearer);
                        request.Content = new StringContent(
                            "{\"Message\":\"stream it\",\"Stream\":true,\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                            Encoding.UTF8, "application/json");

                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                        {
                            AssertEqual(200, (int)response.StatusCode, "Streaming completion returns 200");
                            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                            AssertTrue(body.Contains("\"event\":\"started\""), "Stream carries a started event");
                            AssertTrue(body.Contains("\"event\":\"delta\""), "Stream carries delta events");
                            AssertTrue(body.Contains("\"event\":\"usage\""), "Stream carries a usage event");
                            AssertTrue(body.Contains("[DONE]"), "Stream terminates with DONE");
                            int startedIndex = body.IndexOf("\"event\":\"started\"", StringComparison.Ordinal);
                            int usageIndex = body.IndexOf("\"event\":\"usage\"", StringComparison.Ordinal);
                            int doneIndex = body.IndexOf("[DONE]", StringComparison.Ordinal);
                            AssertTrue(startedIndex < usageIndex && usageIndex < doneIndex, "Events arrive in order started < usage < DONE");
                        }
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestFeedback(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-fb@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("rated answer", 4, 2);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"rate me\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);
                    string turnGuid = ChatExtractJsonString(completion.Body, "TurnGUID");

                    HttpOutcome submitted = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/turns/" + turnGuid + "/feedback",
                        userBearer,
                        "{\"Rating\":\"ThumbsDown\",\"FeedbackText\":\"too short\"}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, submitted.Status, "Feedback submit succeeds (body " + submitted.Body + ")");
                    string feedbackGuid = ExtractGuid(submitted.Body);

                    HttpOutcome deniedList = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/feedback",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(deniedList.Status == 401 || deniedList.Status == 403, "Regular users cannot list feedback (status " + deniedList.Status + ")");

                    HttpOutcome adminList = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/feedback",
                        _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, adminList.Status, "Admin lists feedback");
                    AssertTrue(adminList.Body.Contains("too short"), "Feedback text is listed");

                    HttpOutcome deleted = await AuthRestAsync(HttpMethod.Delete,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/feedback/" + feedbackGuid,
                        _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(IsSuccess(deleted.Status), "Admin deletes feedback (status " + deleted.Status + ")");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestThreadOwnership(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string ownerBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatowner@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                string otherBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatother@chat.test", false, false, cancellationToken).ConfigureAwait(false);

                HttpOutcome created = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads",
                    ownerBearer, "{\"Title\":\"private\"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, created.Status, "Owner creates a thread (body " + created.Body + ")");
                string threadGuid = ExtractGuid(created.Body);

                HttpOutcome deniedRead = await AuthRestAsync(HttpMethod.Get,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid,
                    otherBearer, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(deniedRead.Status == 401 || deniedRead.Status == 403, "Another user cannot read the thread (status " + deniedRead.Status + ")");

                HttpOutcome deniedDelete = await AuthRestAsync(HttpMethod.Delete,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid,
                    otherBearer, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(deniedDelete.Status == 401 || deniedDelete.Status == 403, "Another user cannot delete the thread (status " + deniedDelete.Status + ")");

                HttpOutcome otherList = await AuthRestAsync(HttpMethod.Get,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads",
                    otherBearer, null, cancellationToken).ConfigureAwait(false);
                AssertFalse(otherList.Body.Contains(threadGuid), "The other user's thread list omits the private thread");

                HttpOutcome adminRead = await AuthRestAsync(HttpMethod.Get,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid,
                    _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, adminRead.Status, "An administrator can read any thread");

                HttpOutcome ownerDelete = await AuthRestAsync(HttpMethod.Delete,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid,
                    ownerBearer, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(ownerDelete.Status), "The owner deletes their thread (status " + ownerDelete.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestMetrics(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-metrics@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("metric answer", 3, 2);

                    await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"count me\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);

                    HttpOutcome metrics = await AuthRestAsync(HttpMethod.Get, endpoint + "/metrics", null, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, metrics.Status, "Metrics endpoint responds");
                    AssertTrue(metrics.Body.Contains("litegraph_chat_requests_total"), "Chat request counter is exported");
                    AssertTrue(metrics.Body.Contains("litegraph_chat_request_duration_ms"), "Chat duration histogram is exported");
                    AssertTrue(metrics.Body.Contains("litegraph_chat_active"), "Chat in-flight gauge is exported");
                    AssertFalse(metrics.Body.Contains(_DefaultTenantGuid), "No tenant GUID leaks into metric labels");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static readonly string[] _ChatAdvertisedToolNames = new string[]
        {
            "graph/all", "graph/get", "graph/search", "graph/statistics",
            "node/readallingraph", "node/get", "node/search", "node/neighbors", "node/children", "node/parents",
            "edge/readallingraph", "edge/get", "edge/search", "edge/betweennodes", "edge/fromnode", "edge/tonode",
            "vector/search",
            "label/readallingraph", "label/readmanynode", "label/readmanyedge",
            "tag/readallingraph", "tag/readmanynode", "tag/readmanyedge",
            "graph/create", "graph/update", "graph/delete",
            "node/create", "node/update", "node/delete",
            "edge/create", "edge/update", "edge/delete"
        };

        private static async Task TestChatToolCatalogParity(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");
                string rpcUrl = _McpEnvironment.McpHttpEndpoint + "/rpc";

                using (HttpClient client = new HttpClient())
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, rpcUrl))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    request.Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}", Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        AssertEqual(200, (int)response.StatusCode, "tools/list responds (body " + Truncate(body, 200) + ")");

                        foreach (string toolName in _ChatAdvertisedToolNames)
                        {
                            AssertTrue(body.Contains("\"" + toolName + "\""), "MCP catalog contains chat-advertised tool '" + toolName + "'");
                        }
                    }
                }
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestEndpointReadUpdateTest(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints";
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    HttpOutcome read = await AuthRestAsync(HttpMethod.Get, baseUrl + "/" + endpointGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, read.Status, "Endpoint read over HTTP succeeds");
                    AssertTrue(read.Body.Contains("fake-llm"), "The read endpoint carries its name");

                    HttpOutcome exists = await AuthRestAsync(HttpMethod.Head, baseUrl + "/" + endpointGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, exists.Status, "Endpoint HEAD returns 200 for an existing endpoint");

                    HttpOutcome missing = await AuthRestAsync(HttpMethod.Head, baseUrl + "/" + Guid.NewGuid(), _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, missing.Status, "Endpoint HEAD returns 404 for an unknown endpoint");

                    HttpOutcome updated = await AuthRestAsync(HttpMethod.Put, baseUrl + "/" + endpointGuid, _AdminBearerToken,
                        "{\"Name\":\"fake-llm-renamed\",\"EndpointType\":\"Completion\",\"Provider\":\"OpenAI\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"fake-model\",\"HealthCheckEnabled\":false}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, updated.Status, "Endpoint update over HTTP succeeds (body " + updated.Body + ")");
                    AssertTrue(updated.Body.Contains("fake-llm-renamed"), "The update response carries the new name");

                    HttpOutcome tested = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + endpointGuid + "/test", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, tested.Status, "Endpoint connectivity test succeeds (body " + tested.Body + ")");
                    AssertTrue(tested.Body.Contains("\"Reachable\":true") || tested.Body.Contains("\"Reachable\": true"), "The fake endpoint is reachable");
                    AssertTrue(tested.Body.Contains("fake-model"), "The model list contains the fake model");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestEndpointHealthRoutes(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints";

                    HttpOutcome created = await AuthRestAsync(HttpMethod.Put, baseUrl, _AdminBearerToken,
                        "{\"Name\":\"monitored\",\"EndpointType\":\"Completion\",\"Provider\":\"OpenAI\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"fake-model\",\"HealthCheckEnabled\":true,\"HealthCheckIntervalMs\":1000,\"HealthCheckUrl\":\"" + fake.Endpoint + "/v1/models\",\"HealthyThreshold\":1}",
                        cancellationToken).ConfigureAwait(false);
                    AssertTrue(IsSuccess(created.Status), "Monitored endpoint created (status " + created.Status + ")");
                    string endpointGuid = ExtractGuid(created.Body);

                    bool healthy = false;
                    for (int i = 0; i < 20 && !healthy; i++)
                    {
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                        HttpOutcome single = await AuthRestAsync(HttpMethod.Get, baseUrl + "/" + endpointGuid + "/health", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                        AssertEqual(200, single.Status, "Single-endpoint health route responds");
                        healthy = single.Body.Contains("\"Healthy\":true") || single.Body.Contains("\"Healthy\": true");
                    }
                    AssertTrue(healthy, "The monitored endpoint reaches a healthy verdict against the fake upstream");

                    HttpOutcome all = await AuthRestAsync(HttpMethod.Get, baseUrl + "/health", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, all.Status, "All-endpoint health route responds");
                    AssertTrue(all.Body.Contains("monitored"), "The health list carries the monitored endpoint");

                    HttpOutcome unknown = await AuthRestAsync(HttpMethod.Get, baseUrl + "/" + Guid.NewGuid() + "/health", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, unknown.Status, "Health for an unknown endpoint returns 404");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestFeedbackReadAndNegatives(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatfbneg-owner@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string otherBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatfbneg-other@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("read me", 3, 2);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"single feedback\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);
                    string turnGuid = ChatExtractJsonString(completion.Body, "TurnGUID");
                    string threadGuid = ChatExtractJsonString(completion.Body, "ThreadGUID");

                    HttpOutcome submitted = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/turns/" + turnGuid + "/feedback",
                        userBearer, "{\"Rating\":\"ThumbsUp\"}", cancellationToken).ConfigureAwait(false);
                    string feedbackGuid = ExtractGuid(submitted.Body);

                    HttpOutcome single = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/feedback/" + feedbackGuid,
                        _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, single.Status, "Single feedback read succeeds");
                    AssertTrue(single.Body.Contains("ThumbsUp"), "The feedback record carries its rating");

                    HttpOutcome unknownDelete = await AuthRestAsync(HttpMethod.Delete,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/feedback/" + Guid.NewGuid(),
                        _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, unknownDelete.Status, "Deleting unknown feedback returns 404");

                    HttpOutcome unknownFeedbackTurn = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/turns/" + Guid.NewGuid() + "/feedback",
                        userBearer, "{\"Rating\":\"ThumbsUp\"}", cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, unknownFeedbackTurn.Status, "Feedback against an unknown turn returns 404");

                    HttpOutcome deniedTurns = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid + "/turns",
                        otherBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(deniedTurns.Status == 401 || deniedTurns.Status == 403, "Another user cannot read the owner's turns (status " + deniedTurns.Status + ")");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestMcpChatTools(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

                string settingsJson = await _McpClient.CallAsync<string>("chat/settings/get", new { tenantGuid = _DefaultTenantGuid }).ConfigureAwait(false);
                AssertNotNull(settingsJson, "chat/settings/get returns settings");
                AssertTrue(settingsJson!.Contains("EnableChat"), "The settings payload carries EnableChat");

                string createdJson = await _McpClient.CallAsync<string>("chat/endpoint/create", new
                {
                    tenantGuid = _DefaultTenantGuid,
                    endpoint = "{\"Name\":\"mcp-created\",\"EndpointType\":\"Completion\",\"Provider\":\"Ollama\",\"Endpoint\":\"http://127.0.0.1:11434\",\"Model\":\"gemma3:4b\",\"HealthCheckEnabled\":false}"
                }).ConfigureAwait(false);
                AssertNotNull(createdJson, "chat/endpoint/create returns the endpoint");
                string endpointGuid = ExtractGuid(createdJson!);

                string listJson = await _McpClient.CallAsync<string>("chat/endpoint/all", new { tenantGuid = _DefaultTenantGuid }).ConfigureAwait(false);
                AssertTrue(listJson != null && listJson.Contains("mcp-created"), "chat/endpoint/all lists the created endpoint");

                string threadsJson = await _McpClient.CallAsync<string>("chat/thread/all", new { tenantGuid = _DefaultTenantGuid }).ConfigureAwait(false);
                AssertNotNull(threadsJson, "chat/thread/all responds");

                bool deleted = await _McpClient.CallAsync<bool>("chat/endpoint/delete", new { tenantGuid = _DefaultTenantGuid, endpointGuid = endpointGuid }).ConfigureAwait(false);
                AssertTrue(deleted, "chat/endpoint/delete reports success");

                string listAfterDelete = await _McpClient.CallAsync<string>("chat/endpoint/all", new { tenantGuid = _DefaultTenantGuid }).ConfigureAwait(false);
                AssertFalse(listAfterDelete != null && listAfterDelete.Contains("mcp-created"), "chat/endpoint/delete removes the endpoint");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (String.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
            return value.Substring(0, maxLength);
        }

        #endregion

        #region Chat-Private-Methods

        private static readonly Dictionary<string, string> _ChatPostgresqlSchemas = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly object _ChatPostgresqlSchemaLock = new object();

        private static string ChatDbName(string suffix)
        {
            return "test-chat-" + suffix + ".db";
        }

        private static LiteGraphClient ChatNewClient(string filename)
        {
            string? connectionString = Environment.GetEnvironmentVariable(PostgresqlTestConnectionStringEnvironmentVariable);

            if (!String.IsNullOrWhiteSpace(connectionString))
            {
                string schema = "litegraph_chat_" + Guid.NewGuid().ToString("N");
                lock (_ChatPostgresqlSchemaLock) _ChatPostgresqlSchemas[filename] = schema;

                LiteGraph.GraphRepositories.GraphRepositoryBase repo = LiteGraph.GraphRepositories.GraphRepositoryFactory.Create(new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgresql,
                    ConnectionString = connectionString,
                    Schema = schema
                });
                repo.InitializeRepository();
                return new LiteGraphClient(repo, null, null, null, true);
            }

            return IeNewClient(filename);
        }

        private static void ChatCleanup(string filename)
        {
            string? connectionString = Environment.GetEnvironmentVariable(PostgresqlTestConnectionStringEnvironmentVariable);
            string? schema = null;

            lock (_ChatPostgresqlSchemaLock)
            {
                if (_ChatPostgresqlSchemas.TryGetValue(filename, out schema)) _ChatPostgresqlSchemas.Remove(filename);
            }

            if (!String.IsNullOrWhiteSpace(connectionString) && !String.IsNullOrEmpty(schema))
            {
                DropPostgresqlSchemaAsync(connectionString, schema, CancellationToken.None).GetAwaiter().GetResult();
                return;
            }

            IeCleanup(filename);
        }

        private static async Task<Guid> ChatSeedTenant(LiteGraphClient client)
        {
            Guid tenantGuid = Guid.NewGuid();
            await client.Tenant.Create(new TenantMetadata { GUID = tenantGuid, Name = "Chat tenant" }).ConfigureAwait(false);
            return tenantGuid;
        }

        private static async Task<Guid> ChatSeedUser(LiteGraphClient client, Guid tenantGuid)
        {
            UserMaster user = await client.User.Create(new UserMaster
            {
                TenantGUID = tenantGuid,
                FirstName = "Chat",
                LastName = "User",
                Email = "chat-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@chat.test",
                Password = "password",
                Active = true
            }).ConfigureAwait(false);
            return user.GUID;
        }

        private static ChatEndpoint ChatCompletionEndpoint(Guid tenantGuid, string name)
        {
            return new ChatEndpoint
            {
                TenantGUID = tenantGuid,
                Name = name,
                EndpointType = ChatEndpointTypeEnum.Completion,
                Provider = ChatProviderTypeEnum.OpenAI,
                Endpoint = "http://127.0.0.1:9",
                Model = "fake-model",
                HealthCheckEnabled = false
            };
        }

        private static ChatEndpoint ChatEmbeddingEndpoint(Guid tenantGuid, string name)
        {
            return new ChatEndpoint
            {
                TenantGUID = tenantGuid,
                Name = name,
                EndpointType = ChatEndpointTypeEnum.Embedding,
                Provider = ChatProviderTypeEnum.OpenAI,
                Endpoint = "http://127.0.0.1:9",
                Model = "fake-embed",
                HealthCheckEnabled = false
            };
        }

        private static async Task<string> ChatProvisionFakeEndpoint(string endpoint, FakeLlmServer fake, CancellationToken cancellationToken)
        {
            HttpOutcome created = await AuthRestAsync(HttpMethod.Put,
                endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints",
                _AdminBearerToken,
                "{\"Name\":\"fake-llm\",\"EndpointType\":\"Completion\",\"Provider\":\"OpenAI\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"fake-model\",\"HealthCheckEnabled\":false}",
                cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(created.Status), "Provisioned fake LLM endpoint (status " + created.Status + " body " + created.Body + ")");
            return ExtractGuid(created.Body);
        }

        private static string ChatExtractJsonString(string body, string propertyName)
        {
            using (JsonDocument doc = JsonDocument.Parse(body))
            {
                return doc.RootElement.GetProperty(propertyName).GetString() ?? String.Empty;
            }
        }

        private static async Task ChatAssertThrows<TException>(Func<Task> action, string message) where TException : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TException)
            {
                return;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(message + " (expected " + typeof(TException).Name + " but got " + e.GetType().Name + ": " + e.Message + ")");
            }

            throw new InvalidOperationException(message + " (expected " + typeof(TException).Name + " but no exception was thrown)");
        }

        #endregion
    }
}
