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
                    ChatCase("Chat.Storage", "Chat.Storage.TenantCascade", "Force-deleting a tenant removes its chat objects", TestChatTenantCascade),
                    ChatCase("Chat.Storage", "Chat.Storage.EndpointEnumerationPaging", "Endpoint enumeration pages with MaxResults, skip, continuation, and type filter", TestChatEndpointEnumerationPaging),
                    ChatCase("Chat.Storage", "Chat.Storage.ThreadEnumerationPaging", "Thread enumeration pages and honors the user filter", TestChatThreadEnumerationPaging),
                    ChatCase("Chat.Storage", "Chat.Storage.TurnEnumerationPaging", "Turn enumeration pages within a thread in sequence order", TestChatTurnEnumerationPaging),
                    ChatCase("Chat.Storage", "Chat.Storage.FeedbackEnumerationPaging", "Feedback enumeration pages with MaxResults and skip", TestChatFeedbackEnumerationPaging)
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
                    ChatCase("Chat.Rest", "Chat.Rest.EndpointPreload", "Model preload warms Ollama endpoints, no-ops for cloud providers, and enforces validation", TestChatRestEndpointPreload),
                    ChatCase("Chat.Rest", "Chat.Rest.EndpointHealthRoutes", "Endpoint health routes report monitored state and reject unknown endpoints", TestChatRestEndpointHealthRoutes),
                    ChatCase("Chat.Rest", "Chat.Rest.FeedbackReadAndNegatives", "Single feedback read, unknown-GUID deletes, and cross-user turn denial", TestChatRestFeedbackReadAndNegatives),
                    ChatCase("Chat.Rest", "Chat.Rest.McpChatTools", "MCP chat tools round-trip endpoint and settings operations", TestChatRestMcpChatTools),
                    ChatCase("Chat.Rest", "Chat.Rest.HealthDedup", "Endpoints sharing a probe target share one healthcheck and verdict", TestChatRestHealthDedup),
                    ChatCase("Chat.Rest", "Chat.Rest.ThreadRename", "Thread rename honors ownership and validation", TestChatRestThreadRename),
                    ChatCase("Chat.Rest", "Chat.Rest.ModelsCatalog", "Non-admin users can list selectable models without secrets", TestChatRestModelsCatalog),
                    ChatCase("Chat.Rest", "Chat.Rest.ContentRoundTrip", "Stored text preserves markdown separators and comment tokens", TestChatRestContentRoundTrip),
                    ChatCase("Chat.Rest", "Chat.Rest.ContextPrompt", "System prompt carries tenant and selected graph context", TestChatRestContextPrompt),
                    ChatCase("Chat.Rest", "Chat.Rest.RbacDelegation", "An [Admin] x [Chat] role delegates chat management without tenant admin", TestChatRestRbacDelegation),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatModelsCatalog", "Graph-scoped compatible model list carries only active completion endpoints", TestChatRestCompatModels),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatOpenAiCompletion", "Graph-scoped OpenAI-format completion returns choices and usage and persists a turn", TestChatRestCompatOpenAiCompletion),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatOpenAiStreaming", "Graph-scoped OpenAI-format streaming emits role, content, and DONE frames", TestChatRestCompatOpenAiStreaming),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatModelSelection", "Compatible model selector matches name, model, and GUID; unknown models yield 404", TestChatRestCompatModelSelection),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatOllamaCompletion", "Graph-scoped Ollama-format completion reports done true with counters", TestChatRestCompatOllama),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatAuthRequired", "Compatible chat routes reject unauthenticated requests", TestChatRestCompatAuthRequired),
                    ChatCase("Chat.Rest", "Chat.Rest.CompatUnknownGraph", "Compatible chat routes return 404 for unknown graphs", TestChatRestCompatUnknownGraph),
                    ChatCase("Chat.Rest", "Chat.Rest.ZeroGetAllGuard", "Every list-shaped route in the OpenAPI spec returns an EnumerationResult envelope", TestZeroGetAllGuard)
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

        private static async Task TestChatEndpointEnumerationPaging(CancellationToken token)
        {
            string db = ChatDbName("ep-enum-paging");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);

                for (int i = 0; i < 5; i++)
                {
                    await client.ChatEndpoint.Create(ChatCompletionEndpoint(tenant, "ep-" + i), token).ConfigureAwait(false);
                }

                await client.ChatEndpoint.Create(ChatEmbeddingEndpoint(tenant, "ep-embed"), token).ConfigureAwait(false);

                EnumerationResult<ChatEndpoint> page1 = await client.ChatEndpoint.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 2,
                    Ordering = EnumerationOrderEnum.NameAscending
                }, null, token).ConfigureAwait(false);

                AssertEqual(6L, page1.TotalRecords, "TotalRecords covers all six endpoints");
                AssertEqual(2, page1.Objects.Count, "Page one carries MaxResults objects");
                AssertEqual("ep-0", page1.Objects[0].Name, "Name-ascending ordering starts at ep-0");
                AssertEqual("ep-1", page1.Objects[1].Name, "Name-ascending ordering continues at ep-1");
                AssertFalse(page1.EndOfResults, "Page one is not the end of results");
                AssertNotNull(page1.ContinuationToken, "Page one supplies a continuation token");
                AssertEqual(4L, page1.RecordsRemaining, "Four records remain after page one");

                EnumerationResult<ChatEndpoint> page2 = await client.ChatEndpoint.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 2,
                    Ordering = EnumerationOrderEnum.NameAscending,
                    ContinuationToken = page1.ContinuationToken
                }, null, token).ConfigureAwait(false);

                AssertEqual(2, page2.Objects.Count, "Continuation page carries two objects");
                AssertEqual("ep-2", page2.Objects[0].Name, "Continuation resumes at ep-2");
                AssertEqual("ep-3", page2.Objects[1].Name, "Continuation continues at ep-3");

                EnumerationResult<ChatEndpoint> skipped = await client.ChatEndpoint.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 10,
                    Skip = 4,
                    Ordering = EnumerationOrderEnum.NameAscending
                }, null, token).ConfigureAwait(false);

                AssertEqual(2, skipped.Objects.Count, "Skip-based paging returns the final two records");
                AssertEqual("ep-4", skipped.Objects[0].Name, "Skip resumes at ep-4");
                AssertTrue(skipped.EndOfResults, "Skip page reaching the end reports EndOfResults");

                EnumerationResult<ChatEndpoint> embeddings = await client.ChatEndpoint.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 10
                }, ChatEndpointTypeEnum.Embedding, token).ConfigureAwait(false);

                AssertEqual(1L, embeddings.TotalRecords, "Type filter narrows TotalRecords to embedding endpoints");
                AssertEqual(1, embeddings.Objects.Count, "Type filter returns only the embedding endpoint");
                AssertEqual("ep-embed", embeddings.Objects[0].Name, "The filtered endpoint is the embedding endpoint");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatThreadEnumerationPaging(CancellationToken token)
        {
            string db = ChatDbName("thread-enum-paging");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid userA = await ChatSeedUser(client, tenant).ConfigureAwait(false);
                Guid userB = await ChatSeedUser(client, tenant).ConfigureAwait(false);

                for (int i = 0; i < 7; i++)
                {
                    await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = userA, Title = "a-" + i }, token).ConfigureAwait(false);
                }

                for (int i = 0; i < 3; i++)
                {
                    await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = userB, Title = "b-" + i }, token).ConfigureAwait(false);
                }

                EnumerationResult<ChatThread> pageA = await client.ChatThread.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    UserGUID = userA,
                    MaxResults = 3
                }, token).ConfigureAwait(false);

                AssertEqual(7L, pageA.TotalRecords, "User filter narrows TotalRecords to user A's threads");
                AssertEqual(3, pageA.Objects.Count, "Page one carries MaxResults threads");
                AssertFalse(pageA.EndOfResults, "More of user A's threads remain");
                AssertEqual(4L, pageA.RecordsRemaining, "Four of user A's threads remain after page one");
                AssertTrue(pageA.Objects.All(t => t.UserGUID.Equals(userA)), "Every returned thread belongs to user A");

                HashSet<Guid> seen = new HashSet<Guid>();
                foreach (ChatThread t in pageA.Objects) seen.Add(t.GUID);

                EnumerationResult<ChatThread> pageA2 = await client.ChatThread.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    UserGUID = userA,
                    MaxResults = 3,
                    Skip = 3
                }, token).ConfigureAwait(false);

                EnumerationResult<ChatThread> pageA3 = await client.ChatThread.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    UserGUID = userA,
                    MaxResults = 3,
                    Skip = 6
                }, token).ConfigureAwait(false);

                foreach (ChatThread t in pageA2.Objects) seen.Add(t.GUID);
                foreach (ChatThread t in pageA3.Objects) seen.Add(t.GUID);
                AssertEqual(7, seen.Count, "Skip pages cover all of user A's threads exactly once");
                AssertTrue(pageA3.EndOfResults, "The final page reports EndOfResults");

                EnumerationResult<ChatThread> all = await client.ChatThread.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 1000
                }, token).ConfigureAwait(false);

                AssertEqual(10L, all.TotalRecords, "Without a user filter TotalRecords spans every thread");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatTurnEnumerationPaging(CancellationToken token)
        {
            string db = ChatDbName("turn-enum-paging");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid user = await ChatSeedUser(client, tenant).ConfigureAwait(false);

                ChatThread thread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user, Title = "paged" }, token).ConfigureAwait(false);
                ChatThread otherThread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user, Title = "other" }, token).ConfigureAwait(false);

                for (int i = 0; i < 6; i++)
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

                await client.ChatTurn.Create(new ChatTurn { TenantGUID = tenant, ThreadGUID = otherThread.GUID, Sequence = 0, UserMessage = "elsewhere" }, token).ConfigureAwait(false);

                EnumerationResult<ChatTurn> page1 = await client.ChatTurn.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 4,
                    Ordering = EnumerationOrderEnum.CreatedAscending
                }, thread.GUID, token).ConfigureAwait(false);

                AssertEqual(6L, page1.TotalRecords, "TotalRecords is scoped to the requested thread");
                AssertEqual(4, page1.Objects.Count, "Page one carries MaxResults turns");
                AssertEqual("q0", page1.Objects[0].UserMessage, "Ascending pages begin at sequence zero");
                AssertEqual("q3", page1.Objects[3].UserMessage, "Page one ends at sequence three");
                AssertFalse(page1.EndOfResults, "Two turns remain after page one");
                AssertNotNull(page1.ContinuationToken, "Page one supplies a continuation token");

                EnumerationResult<ChatTurn> page2 = await client.ChatTurn.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 4,
                    Ordering = EnumerationOrderEnum.CreatedAscending,
                    ContinuationToken = page1.ContinuationToken
                }, thread.GUID, token).ConfigureAwait(false);

                AssertEqual(2, page2.Objects.Count, "Continuation page carries the remaining turns");
                AssertEqual("q4", page2.Objects[0].UserMessage, "Continuation resumes at sequence four");
                AssertEqual("q5", page2.Objects[1].UserMessage, "Continuation ends at sequence five");
                AssertTrue(page2.EndOfResults, "The final page reports EndOfResults");

                EnumerationResult<ChatTurn> newestFirst = await client.ChatTurn.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 1,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                }, thread.GUID, token).ConfigureAwait(false);

                AssertEqual("q5", newestFirst.Objects[0].UserMessage, "Descending ordering yields the newest turn first");
            }
            ChatCleanup(db);
        }

        private static async Task TestChatFeedbackEnumerationPaging(CancellationToken token)
        {
            string db = ChatDbName("feedback-enum-paging");
            using (LiteGraphClient client = ChatNewClient(db))
            {
                Guid tenant = await ChatSeedTenant(client).ConfigureAwait(false);
                Guid user = await ChatSeedUser(client, tenant).ConfigureAwait(false);

                ChatThread thread = await client.ChatThread.Create(new ChatThread { TenantGUID = tenant, UserGUID = user }, token).ConfigureAwait(false);

                for (int i = 0; i < 5; i++)
                {
                    ChatTurn turn = await client.ChatTurn.Create(new ChatTurn { TenantGUID = tenant, ThreadGUID = thread.GUID, Sequence = i, UserMessage = "q" + i }, token).ConfigureAwait(false);
                    await client.ChatFeedback.Create(new ChatFeedback
                    {
                        TenantGUID = tenant,
                        TurnGUID = turn.GUID,
                        UserGUID = user,
                        Rating = (i % 2 == 0 ? ChatFeedbackRatingEnum.ThumbsUp : ChatFeedbackRatingEnum.ThumbsDown),
                        FeedbackText = "fb-" + i
                    }, token).ConfigureAwait(false);
                }

                EnumerationResult<ChatFeedback> page1 = await client.ChatFeedback.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 2
                }, null, null, token).ConfigureAwait(false);

                AssertEqual(5L, page1.TotalRecords, "TotalRecords covers all feedback records");
                AssertEqual(2, page1.Objects.Count, "Page one carries MaxResults feedback records");
                AssertFalse(page1.EndOfResults, "More feedback remains after page one");
                AssertEqual(3L, page1.RecordsRemaining, "Three feedback records remain after page one");

                HashSet<Guid> seen = new HashSet<Guid>();
                foreach (ChatFeedback f in page1.Objects) seen.Add(f.GUID);

                for (int skip = 2; skip < 5; skip += 2)
                {
                    EnumerationResult<ChatFeedback> page = await client.ChatFeedback.Enumerate(new EnumerationRequest
                    {
                        TenantGUID = tenant,
                        MaxResults = 2,
                        Skip = skip
                    }, null, null, token).ConfigureAwait(false);

                    foreach (ChatFeedback f in page.Objects) seen.Add(f.GUID);
                }

                AssertEqual(5, seen.Count, "Skip pages cover every feedback record exactly once");

                EnumerationResult<ChatFeedback> thumbsUp = await client.ChatFeedback.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant,
                    MaxResults = 10
                }, ChatFeedbackRatingEnum.ThumbsUp, null, token).ConfigureAwait(false);

                AssertEqual(3L, thumbsUp.TotalRecords, "Rating filter narrows TotalRecords to thumbs-up records");
                AssertTrue(thumbsUp.Objects.All(f => f.Rating == ChatFeedbackRatingEnum.ThumbsUp), "Every returned record is thumbs-up");
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
                        JsonElement threadObjects = threadsDoc.RootElement.GetProperty("Objects");
                        AssertTrue(threadsDoc.RootElement.GetProperty("TotalRecords").GetInt64() > 0, "The thread enumeration reports total records");
                        AssertTrue(threadObjects.GetArrayLength() > 0, "The failed completion still created a thread");
                        threadGuid = threadObjects[0].GetProperty("GUID").GetString() ?? String.Empty;
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

        private static async Task TestChatRestThreadRename(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string ownerBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatrename@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                string otherBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatrenameother@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                string threadsUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads";

                HttpOutcome created = await AuthRestAsync(HttpMethod.Put, threadsUrl, ownerBearer, "{\"Title\":\"before rename\"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, created.Status, "Owner creates a thread (body " + created.Body + ")");
                string threadGuid = ExtractGuid(created.Body);

                HttpOutcome renamed = await AuthRestAsync(HttpMethod.Put, threadsUrl + "/" + threadGuid, ownerBearer, "{\"Title\":\"after rename\"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, renamed.Status, "Owner renames the thread (body " + renamed.Body + ")");
                AssertTrue(renamed.Body.Contains("after rename"), "Rename response carries the new title");

                HttpOutcome read = await AuthRestAsync(HttpMethod.Get, threadsUrl + "/" + threadGuid, ownerBearer, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(read.Body.Contains("after rename"), "Read-back reflects the new title");

                HttpOutcome emptyTitle = await AuthRestAsync(HttpMethod.Put, threadsUrl + "/" + threadGuid, ownerBearer, "{\"Title\":\"  \"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(400, emptyTitle.Status, "Blank title is rejected (status " + emptyTitle.Status + ")");

                HttpOutcome deniedRename = await AuthRestAsync(HttpMethod.Put, threadsUrl + "/" + threadGuid, otherBearer, "{\"Title\":\"hijacked\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(deniedRename.Status == 401 || deniedRename.Status == 403, "Another user cannot rename the thread (status " + deniedRename.Status + ")");

                HttpOutcome adminRename = await AuthRestAsync(HttpMethod.Put, threadsUrl + "/" + threadGuid, _AdminBearerToken, "{\"Title\":\"admin rename\"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, adminRename.Status, "An administrator can rename any thread");

                HttpOutcome missing = await AuthRestAsync(HttpMethod.Put, threadsUrl + "/" + Guid.NewGuid(), ownerBearer, "{\"Title\":\"ghost\"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(404, missing.Status, "Renaming an unknown thread returns 404 (status " + missing.Status + ")");

                await AuthRestAsync(HttpMethod.Delete, threadsUrl + "/" + threadGuid, ownerBearer, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestModelsCatalog(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatmodels@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string modelsUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/models";

                    HttpOutcome adminDenied = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(adminDenied.Status == 401 || adminDenied.Status == 403, "Full endpoint listing stays admin-only (status " + adminDenied.Status + ")");

                    HttpOutcome models = await AuthRestAsync(HttpMethod.Get, modelsUrl, userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, models.Status, "Non-admin user lists chat models (body " + Truncate(models.Body, 200) + ")");
                    AssertTrue(models.Body.Contains(endpointGuid), "Model catalog contains the provisioned endpoint GUID");
                    AssertTrue(models.Body.Contains("\"Model\""), "Model catalog carries model identifiers");
                    AssertTrue(models.Body.Contains("\"IsDefault\""), "Model catalog flags defaults");
                    AssertFalse(models.Body.Contains("ApiKey"), "Model catalog never exposes API keys");
                    AssertFalse(models.Body.Contains(fake.Endpoint), "Model catalog never exposes endpoint URLs");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestContentRoundTrip(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatroundtrip@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                string threadsUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads";

                // Markdown table separators, horizontal rules, SQL comment tokens, and quotes
                // must all survive storage byte-for-byte.
                string title = "a --- b -- c /* d */ e 'quoted' |---|---|";
                string payload = "{\"Title\":" + System.Text.Json.JsonSerializer.Serialize(title) + "}";

                HttpOutcome created = await AuthRestAsync(HttpMethod.Put, threadsUrl, userBearer, payload, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, created.Status, "Thread with hostile-looking title created (body " + Truncate(created.Body, 200) + ")");
                string threadGuid = ExtractGuid(created.Body);

                HttpOutcome read = await AuthRestAsync(HttpMethod.Get, threadsUrl + "/" + threadGuid, userBearer, null, cancellationToken).ConfigureAwait(false);
                using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(read.Body))
                {
                    string stored = doc.RootElement.GetProperty("Title").GetString() ?? String.Empty;
                    AssertEqual(title, stored, "Title round-trips byte-for-byte (got \"" + stored + "\")");
                }

                await AuthRestAsync(HttpMethod.Delete, threadsUrl + "/" + threadGuid, userBearer, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestContextPrompt(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatcontext@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);

                    HttpOutcome graphCreated = await AuthRestAsync(HttpMethod.Put,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs",
                        _AdminBearerToken, "{\"Name\":\"context-prompt-graph\"}", cancellationToken).ConfigureAwait(false);
                    AssertTrue(IsSuccess(graphCreated.Status), "Context graph created (status " + graphCreated.Status + ")");
                    string graphGuid = ExtractGuid(graphCreated.Body);

                    fake.EnqueueText("context answer", 2, 2);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                        userBearer,
                        "{\"Message\":\"what graph am I in?\",\"CompletionEndpointGUID\":\"" + endpointGuid + "\",\"GraphGUID\":\"" + graphGuid + "\",\"EnableTools\":false,\"EnableRag\":false}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, completion.Status, "Completion with graph context succeeds (body " + Truncate(completion.Body, 200) + ")");

                    AssertTrue(fake.CapturedCompletionBodies.TryDequeue(out string? capturedBody), "Provider request body was captured");
                    capturedBody ??= String.Empty;
                    AssertTrue(capturedBody.Contains("operating in tenant"), "System prompt names the tenant");
                    AssertTrue(capturedBody.Contains(graphGuid), "System prompt references the selected graph GUID");
                    AssertTrue(capturedBody.Contains("context-prompt-graph"), "System prompt references the selected graph name");

                    await AuthRestAsync(HttpMethod.Delete,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid,
                        _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
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

        private static async Task TestChatRestEndpointPreload(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints";
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatpreload@chat.test", false, false, cancellationToken).ConfigureAwait(false);

                    #region Ollama-Preload

                    HttpOutcome ollamaCreated = await AuthRestAsync(HttpMethod.Put, baseUrl, _AdminBearerToken,
                        "{\"Name\":\"fake-ollama\",\"EndpointType\":\"Completion\",\"Provider\":\"Ollama\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"fake-model\",\"HealthCheckEnabled\":false}",
                        cancellationToken).ConfigureAwait(false);
                    AssertTrue(IsSuccess(ollamaCreated.Status), "Ollama endpoint created (status " + ollamaCreated.Status + " body " + ollamaCreated.Body + ")");
                    string ollamaGuid = ExtractGuid(ollamaCreated.Body);

                    HttpOutcome preload = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + ollamaGuid + "/preload", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, preload.Status, "Non-admin preload succeeds (body " + preload.Body + ")");
                    AssertTrue(preload.Body.Contains("\"Supported\":true") || preload.Body.Contains("\"Supported\": true"), "Ollama preload is supported (body " + preload.Body + ")");
                    AssertTrue(preload.Body.Contains("\"Started\":true") || preload.Body.Contains("\"Started\": true"), "Ollama preload starts a warm-up (body " + preload.Body + ")");

                    bool warmed = false;
                    string? generateBody = null;
                    for (int i = 0; i < 40 && !warmed; i++)
                    {
                        if (fake.CapturedGenerateBodies.TryDequeue(out generateBody)) warmed = true;
                        else await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }
                    AssertTrue(warmed, "The fake upstream received the /api/generate warm-up call");
                    AssertTrue(generateBody != null && generateBody.Contains("fake-model"), "The warm-up names the endpoint model (body " + generateBody + ")");
                    AssertTrue(generateBody != null && generateBody.Contains("keep_alive"), "The warm-up carries keep_alive (body " + generateBody + ")");

                    HttpOutcome second = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + ollamaGuid + "/preload", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, second.Status, "Second preload succeeds (body " + second.Body + ")");
                    bool secondStarted = second.Body.Contains("\"Started\":true") || second.Body.Contains("\"Started\": true");
                    bool secondInProgress = second.Body.Contains("\"AlreadyInProgress\":true") || second.Body.Contains("\"AlreadyInProgress\": true");
                    AssertTrue(secondStarted || secondInProgress, "Second preload either starts or reports already-in-progress (body " + second.Body + ")");

                    if (secondStarted)
                    {
                        // Wait for the second warm-up to land so it cannot bleed into later assertions.
                        bool drained = false;
                        for (int i = 0; i < 40 && !drained; i++)
                        {
                            if (fake.CapturedGenerateBodies.TryDequeue(out _)) drained = true;
                            else await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                        }
                        AssertTrue(drained, "The second warm-up reached the fake upstream");
                    }

                    #endregion

                    #region Cloud-Provider-Noop

                    string openAiGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    while (fake.CapturedGenerateBodies.TryDequeue(out _)) { }

                    HttpOutcome unsupported = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + openAiGuid + "/preload", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, unsupported.Status, "OpenAI preload responds 200 (body " + unsupported.Body + ")");
                    AssertTrue(unsupported.Body.Contains("\"Supported\":false") || unsupported.Body.Contains("\"Supported\": false"), "OpenAI preload is unsupported (body " + unsupported.Body + ")");

                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    AssertEqual(0, fake.CapturedGenerateBodies.Count, "No /api/generate call is made for an unsupported provider");

                    #endregion

                    #region Negatives

                    HttpOutcome unknown = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + Guid.NewGuid() + "/preload", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, unknown.Status, "Preloading an unknown endpoint returns 404 (status " + unknown.Status + ")");

                    HttpOutcome embeddingCreated = await AuthRestAsync(HttpMethod.Put, baseUrl, _AdminBearerToken,
                        "{\"Name\":\"fake-ollama-embed\",\"EndpointType\":\"Embedding\",\"Provider\":\"Ollama\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"fake-embed\",\"HealthCheckEnabled\":false}",
                        cancellationToken).ConfigureAwait(false);
                    AssertTrue(IsSuccess(embeddingCreated.Status), "Embedding endpoint created (status " + embeddingCreated.Status + " body " + embeddingCreated.Body + ")");
                    string embeddingGuid = ExtractGuid(embeddingCreated.Body);

                    HttpOutcome embeddingPreload = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + embeddingGuid + "/preload", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(400, embeddingPreload.Status, "Preloading an embedding endpoint returns 400 (body " + embeddingPreload.Body + ")");

                    HttpOutcome unauthenticated = await AuthRestAsync(HttpMethod.Post, baseUrl + "/" + ollamaGuid + "/preload", null, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(unauthenticated.Status == 401 || unauthenticated.Status == 403, "Unauthenticated preload is rejected (status " + unauthenticated.Status + ")");

                    #endregion
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

        private static async Task TestChatRestHealthDedup(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints";
                    string healthUrl = fake.Endpoint + "/v1/models";
                    List<string> endpointGuids = new List<string>();

                    for (int i = 0; i < 3; i++)
                    {
                        HttpOutcome created = await AuthRestAsync(HttpMethod.Put, baseUrl, _AdminBearerToken,
                            "{\"Name\":\"dedup-model-" + i + "\",\"EndpointType\":\"Completion\",\"Provider\":\"OpenAI\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"model-" + i + "\",\"HealthCheckEnabled\":true,\"HealthCheckIntervalMs\":1000,\"HealthCheckUrl\":\"" + healthUrl + "\",\"HealthyThreshold\":1}",
                            cancellationToken).ConfigureAwait(false);
                        AssertTrue(IsSuccess(created.Status), "Dedup endpoint " + i + " created (status " + created.Status + ")");
                        endpointGuids.Add(ExtractGuid(created.Body));
                    }

                    bool allHealthy = false;
                    string lastBody = String.Empty;

                    for (int attempt = 0; attempt < 20 && !allHealthy; attempt++)
                    {
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                        HttpOutcome all = await AuthRestAsync(HttpMethod.Get, baseUrl + "/health", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                        lastBody = all.Body;

                        using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(all.Body))
                        {
                            int healthyCount = 0;
                            foreach (System.Text.Json.JsonElement entry in doc.RootElement.GetProperty("Objects").EnumerateArray())
                            {
                                if (entry.TryGetProperty("Healthy", out System.Text.Json.JsonElement h) && h.ValueKind == System.Text.Json.JsonValueKind.True) healthyCount++;
                            }
                            allHealthy = (healthyCount >= 3);
                        }
                    }

                    AssertTrue(allHealthy, "All three endpoints report the shared healthy verdict (body " + Truncate(lastBody, 300) + ")");

                    // Shared probe evidence: every subscriber reports the same last-checked instant.
                    using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(lastBody))
                    {
                        HashSet<string> lastChecked = new HashSet<string>(StringComparer.Ordinal);
                        int entries = 0;
                        foreach (System.Text.Json.JsonElement entry in doc.RootElement.GetProperty("Objects").EnumerateArray())
                        {
                            if (!entry.GetProperty("Name").GetString()!.StartsWith("dedup-model-", StringComparison.Ordinal)) continue;
                            entries++;
                            lastChecked.Add(entry.GetProperty("LastCheckedUtc").GetRawText());
                        }
                        AssertEqual(3, entries, "All three dedup endpoints are monitored");
                        AssertEqual(1, lastChecked.Count, "All three endpoints share a single probe (identical LastCheckedUtc)");
                    }

                    foreach (string guid in endpointGuids)
                    {
                        await AuthRestAsync(HttpMethod.Delete, baseUrl + "/" + guid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (String.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
            return value.Substring(0, maxLength);
        }

        /// <summary>
        /// Permanent guard for the v8.1 zero-get-all mandate: every GET route in the live OpenAPI spec whose
        /// path does not end in a path-parameter segment (i.e. every list-shaped route) must either return the
        /// EnumerationResult envelope (a JSON object carrying Objects and TotalRecords) or appear on the
        /// explicit exception list below with a justification.  Any spec route that is neither verified nor
        /// excepted fails this test by name, so a future get-all route cannot dodge the rule silently.
        /// Known enumeration and search POST routes are additionally asserted never to return a bare array.
        /// </summary>
        private static async Task TestZeroGetAllGuard(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string tenant = _DefaultTenantGuid.ToString();

                #region Fixtures

                HttpOutcome graphCreated = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + tenant + "/graphs",
                    _AdminBearerToken, "{\"Name\":\"zero-getall-guard\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(graphCreated.Status), "Guard graph created (status " + graphCreated.Status + ")");
                string graphGuid = ExtractGuid(graphCreated.Body);

                HttpOutcome nodeACreated = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + tenant + "/graphs/" + graphGuid + "/nodes",
                    _AdminBearerToken, "{\"Name\":\"guard-node-a\"}", cancellationToken).ConfigureAwait(false);
                string nodeAGuid = ExtractGuid(nodeACreated.Body);

                HttpOutcome nodeBCreated = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + tenant + "/graphs/" + graphGuid + "/nodes",
                    _AdminBearerToken, "{\"Name\":\"guard-node-b\"}", cancellationToken).ConfigureAwait(false);
                string nodeBGuid = ExtractGuid(nodeBCreated.Body);

                HttpOutcome edgeCreated = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + tenant + "/graphs/" + graphGuid + "/edges",
                    _AdminBearerToken, "{\"Name\":\"guard-edge\",\"From\":\"" + nodeAGuid + "\",\"To\":\"" + nodeBGuid + "\"}", cancellationToken).ConfigureAwait(false);
                string edgeGuid = ExtractGuid(edgeCreated.Body);

                string? guardUserGuid = null;
                string guardUserBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "zero-getall-guard@chat.test", false, false, cancellationToken, capturedGuid => guardUserGuid = capturedGuid).ConfigureAwait(false);
                AssertNotNull(guardUserGuid, "Guard user GUID captured");

                HttpOutcome credentialCreated = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + tenant + "/credentials",
                    _AdminBearerToken,
                    "{\"UserGUID\":\"" + guardUserGuid + "\",\"Name\":\"zero-getall-guard\",\"BearerToken\":\"guard-" + Guid.NewGuid().ToString("N") + "\"}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(credentialCreated.Status), "Guard credential created (status " + credentialCreated.Status + ")");
                string credentialGuid = ExtractGuid(credentialCreated.Body);

                HttpOutcome threadCreated = await AuthRestAsync(HttpMethod.Put,
                    endpoint + "/v1.0/tenants/" + tenant + "/chat/threads",
                    guardUserBearer, "{\"Title\":\"zero-getall-guard\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(threadCreated.Status), "Guard chat thread created (status " + threadCreated.Status + ")");
                string threadGuid = ExtractGuid(threadCreated.Body);

                #endregion

                #region Route-Maps

                // Every list-shaped GET route the mandate converted, mapped from its spec template to a
                // concrete invocable URL suffix (path parameters filled from the fixtures above).
                Dictionary<string, string> verified = new Dictionary<string, string>(StringComparer.Ordinal);

                void Verify(string template, string concrete)
                {
                    verified[template] = concrete;
                }

                string t = "/v1.0/tenants/{tenantGuid}";
                string g = t + "/graphs/{graphGuid}";
                string n = g + "/nodes/{nodeGuid}";
                string e = g + "/edges/{edgeGuid}";
                string ct = "/v1.0/tenants/" + tenant;
                string cg = ct + "/graphs/" + graphGuid;
                string cn = cg + "/nodes/" + nodeAGuid;
                string ce = cg + "/edges/" + edgeGuid;

                Verify("/v1.0/backups", "/v1.0/backups");
                Verify("/v1.0/requesthistory", "/v1.0/requesthistory");
                Verify("/v1.0/tenants", "/v1.0/tenants");
                Verify("/v2.0/tenants", "/v2.0/tenants");
                Verify(t + "/users", ct + "/users");
                Verify("/v2.0/tenants/{tenantGuid}/users", "/v2.0/tenants/" + tenant + "/users");
                Verify(t + "/users/{userGuid}/roles", ct + "/users/" + guardUserGuid + "/roles");
                Verify(t + "/credentials", ct + "/credentials");
                Verify("/v2.0/tenants/{tenantGuid}/credentials", "/v2.0/tenants/" + tenant + "/credentials");
                Verify(t + "/credentials/{credentialGuid}/scopes", ct + "/credentials/" + credentialGuid + "/scopes");
                Verify(t + "/roles", ct + "/roles");
                Verify(t + "/labels", ct + "/labels");
                Verify(t + "/labels/all", ct + "/labels/all");
                Verify("/v2.0/tenants/{tenantGuid}/labels", "/v2.0/tenants/" + tenant + "/labels");
                Verify(t + "/tags", ct + "/tags");
                Verify(t + "/tags/all", ct + "/tags/all");
                Verify("/v2.0/tenants/{tenantGuid}/tags", "/v2.0/tenants/" + tenant + "/tags");
                Verify(t + "/vectors", ct + "/vectors");
                Verify(t + "/vectors/all", ct + "/vectors/all");
                Verify("/v2.0/tenants/{tenantGuid}/vectors", "/v2.0/tenants/" + tenant + "/vectors");
                Verify(t + "/graphs", ct + "/graphs");
                Verify(t + "/graphs/all", ct + "/graphs/all");
                Verify("/v2.0/tenants/{tenantGuid}/graphs", "/v2.0/tenants/" + tenant + "/graphs");
                Verify(t + "/nodes/all", ct + "/nodes/all");
                Verify(t + "/edges/all", ct + "/edges/all");
                Verify(g + "/labels", cg + "/labels");
                Verify(g + "/labels/all", cg + "/labels/all");
                Verify(g + "/tags", cg + "/tags");
                Verify(g + "/tags/all", cg + "/tags/all");
                Verify(g + "/vectors", cg + "/vectors");
                Verify(g + "/vectors/all", cg + "/vectors/all");
                Verify(g + "/nodes", cg + "/nodes");
                Verify(g + "/nodes/all", cg + "/nodes/all");
                Verify("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/nodes");
                Verify(g + "/nodes/mostconnected", cg + "/nodes/mostconnected");
                Verify(g + "/nodes/leastconnected", cg + "/nodes/leastconnected");
                Verify(g + "/edges", cg + "/edges");
                Verify(g + "/edges/all", cg + "/edges/all");
                Verify("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/edges");
                Verify(g + "/edges/between", cg + "/edges/between?from=" + nodeAGuid + "&to=" + nodeBGuid);
                Verify(n + "/labels", cn + "/labels");
                Verify(n + "/tags", cn + "/tags");
                Verify(n + "/vectors", cn + "/vectors");
                Verify(n + "/edges", cn + "/edges");
                Verify(n + "/edges/from", cn + "/edges/from");
                Verify(n + "/edges/to", cn + "/edges/to");
                Verify(n + "/neighbors", cn + "/neighbors");
                Verify(n + "/parents", cn + "/parents");
                Verify(n + "/children", cn + "/children");
                Verify(e + "/labels", ce + "/labels");
                Verify(e + "/tags", ce + "/tags");
                Verify(e + "/vectors", ce + "/vectors");
                Verify(t + "/chat/endpoints", ct + "/chat/endpoints");
                Verify(t + "/chat/endpoints/health", ct + "/chat/endpoints/health");
                Verify(t + "/chat/models", ct + "/chat/models");
                Verify(t + "/chat/threads", ct + "/chat/threads");
                Verify(t + "/chat/threads/{chatThreadGuid}/turns", ct + "/chat/threads/" + threadGuid + "/turns");
                Verify(t + "/chat/feedback", ct + "/chat/feedback");

                // List-shaped GET routes that are exempt from the envelope mandate, with justification.
                Dictionary<string, string> exceptions = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["/"] = "Server information object (pre-authentication).",
                    ["/favicon.ico"] = "Static favicon asset.",
                    ["/metrics"] = "Prometheus text exposition format by design.",
                    ["/openapi.json"] = "OpenAPI specification document.",
                    ["/swagger"] = "Swagger UI HTML page.",
                    ["/v1.0/settings"] = "Server settings object (single-object read).",
                    ["/v1.0/requesthistory/summary"] = "Aggregated summary object, not a record list.",
                    ["/v1.0/requesthistory/{requestGuid}/detail"] = "Single request-history detail object.",
                    ["/v1.0/tenants/stats"] = "Statistics dictionary object.",
                    [t + "/stats"] = "Statistics object.",
                    [t + "/graphs/stats"] = "Statistics dictionary object.",
                    [g + "/stats"] = "Statistics object.",
                    [t + "/chat/endpoints/{chatEndpointGuid}/health"] = "Single endpoint health object.",
                    [t + "/chat/settings"] = "Tenant chat settings object (single-object read).",
                    [t + "/users/{userGuid}/permissions"] = "Composite effective-permissions object (assignments, roles, and grants).",
                    [t + "/credentials/{credentialGuid}/permissions"] = "Composite effective-permissions object (assignments, roles, and grants).",
                    [g + "/chat/models"] = "OpenAI wire-format model list by design (protocol compatibility).",
                    [g + "/export/gexf"] = "GEXF XML export stream.",
                    [g + "/export/jsonl"] = "JSONL export stream.",
                    [g + "/vectorindex/config"] = "Vector index configuration object.",
                    [g + "/vectorindex/stats"] = "Vector index statistics object.",
                    ["/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config"] = "Vector index configuration object.",
                    ["/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats"] = "Vector index statistics object.",
                    [n + "/subgraph"] = "Subgraph SearchResult envelope (graph plus nodes plus edges), not a flat record list.",
                    [n + "/subgraph/stats"] = "Subgraph statistics object.",
                    ["/v1.0/token"] = "Token issuance object (header-based authentication).",
                    ["/v1.0/token/details"] = "Token details object (header-based authentication).",
                    ["/v1.0/token/tenants"] = "Returns the EnumerationResult envelope but authenticates via the email header rather than a bearer token; exercised by dedicated token tests."
                };

                #endregion

                #region Spec-Sweep

                HttpOutcome spec = await AuthRestAsync(HttpMethod.Get, endpoint + "/openapi.json", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, spec.Status, "OpenAPI specification is served");

                List<string> unaccounted = new List<string>();
                List<string> toInvoke = new List<string>();

                using (JsonDocument doc = JsonDocument.Parse(spec.Body))
                {
                    JsonElement paths = doc.RootElement.GetProperty("paths");
                    foreach (JsonProperty path in paths.EnumerateObject())
                    {
                        bool hasGet = false;
                        foreach (JsonProperty method in path.Value.EnumerateObject())
                        {
                            if (method.Name.Equals("get", StringComparison.OrdinalIgnoreCase)) hasGet = true;
                        }
                        if (!hasGet) continue;

                        string[] segments = path.Name.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        string last = segments.Length > 0 ? segments[segments.Length - 1] : String.Empty;
                        bool listShaped = !(last.StartsWith("{", StringComparison.Ordinal) && last.EndsWith("}", StringComparison.Ordinal));
                        if (!listShaped) continue;

                        if (exceptions.ContainsKey(path.Name)) continue;
                        if (verified.ContainsKey(path.Name))
                        {
                            toInvoke.Add(path.Name);
                            continue;
                        }

                        unaccounted.Add(path.Name);
                    }
                }

                AssertTrue(unaccounted.Count == 0,
                    "Every list-shaped GET route must be verified as an EnumerationResult envelope or added to the guard's exception list with a justification. Unaccounted routes: "
                    + String.Join(", ", unaccounted));

                foreach (string template in toInvoke)
                {
                    HttpOutcome outcome = await AuthRestAsync(HttpMethod.Get, endpoint + verified[template], _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEnumerationEnvelope(template, outcome);
                }

                #endregion

                #region Post-Routes

                // Known enumeration POST routes must return the envelope; search POST routes must never
                // return a bare array (they return SearchResult or single-object envelopes).
                List<string> enumerationPosts = new List<string>
                {
                    "/v2.0/tenants",
                    "/v2.0/tenants/" + tenant + "/users",
                    "/v2.0/tenants/" + tenant + "/credentials",
                    "/v2.0/tenants/" + tenant + "/labels",
                    "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/labels",
                    "/v2.0/tenants/" + tenant + "/tags",
                    "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/tags",
                    "/v2.0/tenants/" + tenant + "/vectors",
                    "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/vectors",
                    "/v2.0/tenants/" + tenant + "/graphs",
                    "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/nodes",
                    "/v2.0/tenants/" + tenant + "/graphs/" + graphGuid + "/edges"
                };

                foreach (string post in enumerationPosts)
                {
                    HttpOutcome outcome = await AuthRestAsync(HttpMethod.Post, endpoint + post, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                    AssertEnumerationEnvelope("POST " + post, outcome);
                }

                HttpOutcome nodeEdgesPost = await AuthRestAsync(HttpMethod.Post,
                    endpoint + cn + "/edges", _AdminBearerToken, "{}", cancellationToken).ConfigureAwait(false);
                AssertEnumerationEnvelope("POST " + n + "/edges", nodeEdgesPost);

                HttpOutcome vectorSearchPost = await AuthRestAsync(HttpMethod.Post,
                    endpoint + ct + "/vectors", _AdminBearerToken,
                    "{\"GraphGUID\":\"" + graphGuid + "\",\"Embeddings\":[0.1,0.2,0.3]}", cancellationToken).ConfigureAwait(false);
                AssertEnumerationEnvelope("POST " + t + "/vectors (vector search)", vectorSearchPost);

                List<string> searchPosts = new List<string>
                {
                    ct + "/graphs/search",
                    cg + "/nodes/search",
                    cg + "/edges/search"
                };

                foreach (string post in searchPosts)
                {
                    HttpOutcome outcome = await AuthRestAsync(HttpMethod.Post, endpoint + post, _AdminBearerToken, "{}", cancellationToken).ConfigureAwait(false);
                    using (JsonDocument doc = JsonDocument.Parse(outcome.Body))
                    {
                        AssertTrue(doc.RootElement.ValueKind == JsonValueKind.Object, "POST " + post + " returns a JSON object, never a bare array");
                    }
                }

                #endregion

                #region Cleanup

                await AuthRestAsync(HttpMethod.Delete, endpoint + ct + "/chat/threads/" + threadGuid, guardUserBearer, null, cancellationToken).ConfigureAwait(false);
                await AuthRestAsync(HttpMethod.Delete, endpoint + ct + "/credentials/" + credentialGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                await AuthRestAsync(HttpMethod.Delete, endpoint + cg + "?force", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);

                #endregion
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static void AssertEnumerationEnvelope(string routeTemplate, HttpOutcome outcome)
        {
            AssertEqual(200, outcome.Status, "Zero get-all guard: " + routeTemplate + " returns 200 (body " + Truncate(outcome.Body, 300) + ")");

            using (JsonDocument doc = JsonDocument.Parse(outcome.Body))
            {
                AssertTrue(doc.RootElement.ValueKind == JsonValueKind.Object,
                    "Zero get-all guard: " + routeTemplate + " returns a JSON object, never a bare array");
                AssertTrue(doc.RootElement.TryGetProperty("Objects", out JsonElement objects) && objects.ValueKind == JsonValueKind.Array,
                    "Zero get-all guard: " + routeTemplate + " carries an Objects array (body " + Truncate(outcome.Body, 300) + ")");
                AssertTrue(doc.RootElement.TryGetProperty("TotalRecords", out JsonElement total) && total.ValueKind == JsonValueKind.Number,
                    "Zero get-all guard: " + routeTemplate + " carries TotalRecords (body " + Truncate(outcome.Body, 300) + ")");
            }
        }

        private static async Task TestChatRestRbacDelegation(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string endpointsUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints";
                string settingsUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/settings";

                // (a) A non-admin user without a chat grant cannot manage chat endpoints.
                string? userAGuid = null;
                string userABearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-rbac-a@chat.test", false, false, cancellationToken, capturedGuid => userAGuid = capturedGuid).ConfigureAwait(false);

                HttpOutcome deniedBefore = await AuthRestAsync(HttpMethod.Get, endpointsUrl, userABearer, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(deniedBefore.Status == 401 || deniedBefore.Status == 403, "Without a chat grant, endpoint list is denied (status " + deniedBefore.Status + ")");

                // (b) Create a tenant-scoped [Admin] x [Chat] role and assign it to user A.
                HttpOutcome roleCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/roles", _AdminBearerToken,
                    "{\"Name\":\"ChatDelegate\",\"DisplayName\":\"Chat Delegate\",\"Description\":\"Delegated chat administration\",\"ResourceScope\":\"Tenant\",\"Permissions\":[\"Admin\",\"Read\",\"Write\",\"Delete\"],\"ResourceTypes\":[\"Chat\"]}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(roleCreated.Status), "Chat delegation role create succeeds (status " + roleCreated.Status + " body " + roleCreated.Body + ")");
                string roleGuid = ExtractGuid(roleCreated.Body);

                HttpOutcome assigned = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users/" + userAGuid + "/roles", _AdminBearerToken,
                    "{\"RoleGUID\":\"" + roleGuid + "\",\"RoleName\":\"ChatDelegate\",\"ResourceScope\":\"Tenant\"}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(assigned.Status), "Chat delegation role assignment succeeds (status " + assigned.Status + " body " + assigned.Body + ")");

                // (c) User A can now manage chat, but chat delegation confers no general admin rights.
                HttpOutcome allowedList = await AuthRestAsync(HttpMethod.Get, endpointsUrl, userABearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, allowedList.Status, "With an [Admin] x [Chat] grant, endpoint list succeeds (body " + allowedList.Body + ")");

                HttpOutcome allowedSettings = await AuthRestAsync(HttpMethod.Put, settingsUrl, userABearer, "{\"RagTopK\":3}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, allowedSettings.Status, "With an [Admin] x [Chat] grant, settings update succeeds (body " + allowedSettings.Body + ")");

                HttpOutcome usersDenied = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users", userABearer, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(usersDenied.Status == 401 || usersDenied.Status == 403, "Chat delegation does not confer general tenant administration (status " + usersDenied.Status + ")");

                // (d) Regression: graph-scoped grants must not lock a tenant member out of member-level chat.
                HttpOutcome graphCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs", _AdminBearerToken,
                    "{\"Name\":\"chat-rbac-graph\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(graphCreated.Status), "Graph create succeeds (status " + graphCreated.Status + ")");
                string graphGuid = ExtractGuid(graphCreated.Body);

                string? userBGuid = null;
                await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "chatuser-rbac-b@chat.test", false, false, cancellationToken, capturedGuid => userBGuid = capturedGuid).ConfigureAwait(false);

                string userBBearer = "chat-rbac-b-" + userBGuid;
                HttpOutcome credentialCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/credentials", _AdminBearerToken,
                    "{\"UserGUID\":\"" + userBGuid + "\",\"Name\":\"Chat RBAC scoped credential\",\"BearerToken\":\"" + userBBearer + "\",\"Active\":true}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(credentialCreated.Status), "Scoped credential create succeeds (status " + credentialCreated.Status + ")");
                string credentialGuid = ExtractGuid(credentialCreated.Body);

                HttpOutcome viewerAssigned = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users/" + userBGuid + "/roles", _AdminBearerToken,
                    "{\"RoleName\":\"Viewer\",\"ResourceScope\":\"Graph\",\"GraphGUID\":\"" + graphGuid + "\"}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(viewerAssigned.Status), "Graph-scoped Viewer role assignment succeeds (status " + viewerAssigned.Status + " body " + viewerAssigned.Body + ")");

                HttpOutcome scopeAssigned = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/credentials/" + credentialGuid + "/scopes", _AdminBearerToken,
                    "{\"RoleName\":\"Viewer\",\"ResourceScope\":\"Graph\",\"GraphGUID\":\"" + graphGuid + "\"}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(scopeAssigned.Status), "Graph-scoped credential scope assignment succeeds (status " + scopeAssigned.Status + " body " + scopeAssigned.Body + ")");

                HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                    endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/completions",
                    userBBearer,
                    "{\"Message\":\"hello\",\"EnableTools\":false,\"EnableRag\":false}",
                    cancellationToken).ConfigureAwait(false);
                AssertTrue(completion.Status != 401 && completion.Status != 403, "Graph-scoped grants do not block member-level chat completions (status " + completion.Status + " body " + Truncate(completion.Body, 300) + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestCompatModels(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "compat-models@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string graphGuid = await ChatProvisionGraphAsync(endpoint, "compat-models-graph", cancellationToken).ConfigureAwait(false);

                    HttpOutcome embeddingCreated = await AuthRestAsync(HttpMethod.Put,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/endpoints",
                        _AdminBearerToken,
                        "{\"Name\":\"fake-embed\",\"EndpointType\":\"Embedding\",\"Provider\":\"OpenAI\",\"Endpoint\":\"" + fake.Endpoint + "\",\"Model\":\"fake-embed\",\"HealthCheckEnabled\":false}",
                        cancellationToken).ConfigureAwait(false);
                    AssertTrue(IsSuccess(embeddingCreated.Status), "Provisioned embedding endpoint (status " + embeddingCreated.Status + ")");

                    HttpOutcome models = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat/models",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, models.Status, "Compat model list succeeds (body " + Truncate(models.Body, 300) + ")");

                    using (JsonDocument doc = JsonDocument.Parse(models.Body))
                    {
                        AssertEqual("list", doc.RootElement.GetProperty("object").GetString() ?? String.Empty, "Model list object is 'list'");
                        JsonElement data = doc.RootElement.GetProperty("data");
                        AssertTrue(data.GetArrayLength() >= 1, "Model list carries at least one entry");

                        bool sawCompletion = false;
                        bool sawEmbedding = false;
                        foreach (JsonElement entry in data.EnumerateArray())
                        {
                            string id = entry.GetProperty("id").GetString() ?? String.Empty;
                            AssertEqual("model", entry.GetProperty("object").GetString() ?? String.Empty, "Model entry object is 'model'");
                            AssertTrue(entry.TryGetProperty("created", out _), "Model entry carries a created epoch");
                            AssertTrue(entry.TryGetProperty("owned_by", out _), "Model entry carries owned_by");
                            if (id == "fake-llm") sawCompletion = true;
                            if (id == "fake-embed") sawEmbedding = true;
                        }

                        AssertTrue(sawCompletion, "The completion endpoint appears by name in the model list");
                        AssertFalse(sawEmbedding, "Embedding endpoints are excluded from the model list");
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestCompatOpenAiCompletion(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "compat-openai@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string graphGuid = await ChatProvisionGraphAsync(endpoint, "compat-openai-graph", cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("Compat says hello.", 12, 5);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat/completions",
                        userBearer,
                        "{\"messages\":[{\"role\":\"system\",\"content\":\"Answer briefly.\"},{\"role\":\"user\",\"content\":\"say hello\"}],\"ignored_field\":true}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(200, completion.Status, "OpenAI-format completion succeeds (body " + Truncate(completion.Body, 400) + ")");

                    using (JsonDocument doc = JsonDocument.Parse(completion.Body))
                    {
                        AssertEqual("chat.completion", doc.RootElement.GetProperty("object").GetString() ?? String.Empty, "Response object is chat.completion");
                        AssertTrue((doc.RootElement.GetProperty("id").GetString() ?? String.Empty).StartsWith("chatcmpl-", StringComparison.Ordinal), "Completion id carries the chatcmpl prefix");
                        AssertEqual("fake-model", doc.RootElement.GetProperty("model").GetString() ?? String.Empty, "Model reflects the endpoint's model");

                        JsonElement choice = doc.RootElement.GetProperty("choices")[0];
                        AssertEqual("assistant", choice.GetProperty("message").GetProperty("role").GetString() ?? String.Empty, "Choice message role is assistant");
                        AssertTrue((choice.GetProperty("message").GetProperty("content").GetString() ?? String.Empty).Contains("Compat says hello."), "Choice message carries the fake model's answer");
                        AssertEqual("stop", choice.GetProperty("finish_reason").GetString() ?? String.Empty, "Finish reason is stop");

                        JsonElement usage = doc.RootElement.GetProperty("usage");
                        AssertEqual(12, usage.GetProperty("prompt_tokens").GetInt32(), "Prompt tokens are reported");
                        AssertEqual(5, usage.GetProperty("completion_tokens").GetInt32(), "Completion tokens are reported");
                        AssertEqual(17, usage.GetProperty("total_tokens").GetInt32(), "Total tokens are the sum");
                    }

                    HttpOutcome threads = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, threads.Status, "Thread list succeeds");

                    string? threadGuid = null;
                    using (JsonDocument threadsDoc = JsonDocument.Parse(threads.Body))
                    {
                        foreach (JsonElement thread in threadsDoc.RootElement.GetProperty("Objects").EnumerateArray())
                        {
                            string title = (thread.TryGetProperty("Title", out JsonElement titleProp) ? titleProp.GetString() ?? String.Empty : String.Empty);
                            if (title.StartsWith("OpenAI-compatible:", StringComparison.Ordinal))
                            {
                                threadGuid = thread.GetProperty("GUID").GetString();
                                break;
                            }
                        }
                    }

                    AssertNotNull(threadGuid, "The exchange was persisted into an implicit OpenAI-compatible thread");

                    HttpOutcome turns = await AuthRestAsync(HttpMethod.Get,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/chat/threads/" + threadGuid + "/turns",
                        userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, turns.Status, "Turns read back for the implicit thread");
                    AssertTrue(turns.Body.Contains("say hello"), "The persisted turn carries the user message");
                    AssertTrue(turns.Body.Contains("\"Success\":true") || turns.Body.Contains("\"Success\": true"), "The persisted turn is successful");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestCompatOpenAiStreaming(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "compat-stream@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string graphGuid = await ChatProvisionGraphAsync(endpoint, "compat-stream-graph", cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("streamed compat answer", 9, 4);

                    using (HttpClient client = new HttpClient())
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat/completions"))
                    {
                        client.Timeout = TimeSpan.FromSeconds(60);
                        request.Headers.Add("Authorization", "Bearer " + userBearer);
                        request.Content = new StringContent(
                            "{\"messages\":[{\"role\":\"user\",\"content\":\"stream it\"}],\"stream\":true,\"stream_options\":{\"include_usage\":true}}",
                            Encoding.UTF8, "application/json");

                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                        {
                            AssertEqual(200, (int)response.StatusCode, "OpenAI-format streaming returns 200");
                            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                            AssertTrue(body.Contains("\"object\":\"chat.completion.chunk\""), "Stream carries chat.completion.chunk frames");
                            AssertTrue(body.Contains("\"role\":\"assistant\""), "The first chunk carries the assistant role");
                            AssertTrue(body.Contains("\"content\":\"streamed co\"") && body.Contains("\"content\":\"mpat answer\""), "Stream carries the content fragments");
                            AssertTrue(body.Contains("\"finish_reason\":\"stop\""), "The terminal chunk carries finish_reason stop");
                            AssertTrue(body.Contains("\"total_tokens\":13"), "The usage chunk reports total tokens");
                            AssertTrue(body.Contains("[DONE]"), "Stream terminates with DONE");

                            int roleIndex = body.IndexOf("\"role\":\"assistant\"", StringComparison.Ordinal);
                            int finishIndex = body.IndexOf("\"finish_reason\":\"stop\"", StringComparison.Ordinal);
                            int doneIndex = body.IndexOf("[DONE]", StringComparison.Ordinal);
                            AssertTrue(roleIndex < finishIndex && finishIndex < doneIndex, "Frames arrive in order role < finish < DONE");
                        }
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestCompatModelSelection(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "compat-select@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string graphGuid = await ChatProvisionGraphAsync(endpoint, "compat-select-graph", cancellationToken).ConfigureAwait(false);
                    string url = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat/completions";

                    fake.EnqueueText("selected by name", 4, 2);
                    HttpOutcome byName = await AuthRestAsync(HttpMethod.Post, url, userBearer,
                        "{\"model\":\"fake-llm\",\"messages\":[{\"role\":\"user\",\"content\":\"by name\"}]}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, byName.Status, "Selection by endpoint name succeeds (body " + Truncate(byName.Body, 300) + ")");
                    AssertTrue(byName.Body.Contains("selected by name"), "Name-selected completion returns the fake answer");

                    fake.EnqueueText("selected by model", 4, 2);
                    HttpOutcome byModel = await AuthRestAsync(HttpMethod.Post, url, userBearer,
                        "{\"model\":\"FAKE-MODEL\",\"messages\":[{\"role\":\"user\",\"content\":\"by model\"}]}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, byModel.Status, "Selection by model string is case-insensitive (body " + Truncate(byModel.Body, 300) + ")");

                    fake.EnqueueText("selected by guid", 4, 2);
                    HttpOutcome byGuid = await AuthRestAsync(HttpMethod.Post, url, userBearer,
                        "{\"model\":\"" + endpointGuid + "\",\"messages\":[{\"role\":\"user\",\"content\":\"by guid\"}]}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, byGuid.Status, "Selection by endpoint GUID succeeds (body " + Truncate(byGuid.Body, 300) + ")");

                    HttpOutcome unknown = await AuthRestAsync(HttpMethod.Post, url, userBearer,
                        "{\"model\":\"no-such-model\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}",
                        cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, unknown.Status, "Unknown model yields 404 (body " + Truncate(unknown.Body, 300) + ")");

                    using (JsonDocument doc = JsonDocument.Parse(unknown.Body))
                    {
                        JsonElement error = doc.RootElement.GetProperty("error");
                        AssertEqual("invalid_request_error", error.GetProperty("type").GetString() ?? String.Empty, "The 404 uses the OpenAI error envelope");
                        AssertTrue((error.GetProperty("message").GetString() ?? String.Empty).Contains("no-such-model"), "The error names the unknown model");
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestCompatOllama(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "compat-ollama@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string graphGuid = await ChatProvisionGraphAsync(endpoint, "compat-ollama-graph", cancellationToken).ConfigureAwait(false);

                    fake.EnqueueText("Ollama compat answer.", 11, 6);

                    HttpOutcome completion = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat/ollama",
                        userBearer,
                        "{\"messages\":[{\"role\":\"user\",\"content\":\"hello ollama\"}],\"stream\":false,\"options\":{\"temperature\":0.2}}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(200, completion.Status, "Ollama-format completion succeeds (body " + Truncate(completion.Body, 400) + ")");

                    using (JsonDocument doc = JsonDocument.Parse(completion.Body))
                    {
                        AssertEqual("fake-model", doc.RootElement.GetProperty("model").GetString() ?? String.Empty, "Model reflects the endpoint's model");
                        AssertTrue(doc.RootElement.GetProperty("done").GetBoolean(), "Response reports done true");
                        AssertTrue(doc.RootElement.TryGetProperty("created_at", out _), "Response carries created_at");

                        JsonElement message = doc.RootElement.GetProperty("message");
                        AssertEqual("assistant", message.GetProperty("role").GetString() ?? String.Empty, "Message role is assistant");
                        AssertTrue((message.GetProperty("content").GetString() ?? String.Empty).Contains("Ollama compat answer."), "Message carries the fake model's answer");

                        AssertEqual(11, doc.RootElement.GetProperty("prompt_eval_count").GetInt32(), "prompt_eval_count reports prompt tokens");
                        AssertEqual(6, doc.RootElement.GetProperty("eval_count").GetInt32(), "eval_count reports completion tokens");
                        AssertTrue(doc.RootElement.GetProperty("total_duration").GetInt64() > 0, "total_duration is reported in nanoseconds");
                        AssertTrue(doc.RootElement.TryGetProperty("eval_duration", out _), "eval_duration is reported");
                    }

                    fake.EnqueueText("Ollama streamed answer.", 7, 3);

                    HttpOutcome streamed = await AuthRestAsync(HttpMethod.Post,
                        endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat/ollama",
                        userBearer,
                        "{\"messages\":[{\"role\":\"user\",\"content\":\"stream by default\"}]}",
                        cancellationToken).ConfigureAwait(false);

                    AssertEqual(200, streamed.Status, "Ollama-format streaming (the protocol default) succeeds (body " + Truncate(streamed.Body, 400) + ")");
                    string[] lines = streamed.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    AssertTrue(lines.Length >= 2, "The stream carries multiple NDJSON lines (got " + lines.Length + ")");
                    AssertTrue(streamed.Body.Contains("\"done\":false"), "Fragments report done false");

                    using (JsonDocument finalDoc = JsonDocument.Parse(lines[lines.Length - 1]))
                    {
                        AssertTrue(finalDoc.RootElement.GetProperty("done").GetBoolean(), "The final NDJSON line reports done true");
                        AssertEqual(3, finalDoc.RootElement.GetProperty("eval_count").GetInt32(), "The final line carries token counters");
                    }
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task TestChatRestCompatAuthRequired(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string graphGuid = await ChatProvisionGraphAsync(endpoint, "compat-auth-graph", cancellationToken).ConfigureAwait(false);
                string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/chat";

                HttpOutcome openAi = await AuthRestAsync(HttpMethod.Post, baseUrl + "/completions", null,
                    "{\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}", cancellationToken).ConfigureAwait(false);
                AssertTrue(openAi.Status == 401 || openAi.Status == 403, "OpenAI-format completion requires authentication (status " + openAi.Status + ")");

                HttpOutcome ollama = await AuthRestAsync(HttpMethod.Post, baseUrl + "/ollama", null,
                    "{\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"stream\":false}", cancellationToken).ConfigureAwait(false);
                AssertTrue(ollama.Status == 401 || ollama.Status == 403, "Ollama-format completion requires authentication (status " + ollama.Status + ")");

                HttpOutcome models = await AuthRestAsync(HttpMethod.Get, baseUrl + "/models", null, null, cancellationToken).ConfigureAwait(false);
                AssertTrue(models.Status == 401 || models.Status == 403, "Compat model list requires authentication (status " + models.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestChatRestCompatUnknownGraph(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            using (FakeLlmServer fake = new FakeLlmServer())
            {
                try
                {
                    string endpoint = RequireEndpoint();
                    string userBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "compat-nograph@chat.test", false, false, cancellationToken).ConfigureAwait(false);
                    string endpointGuid = await ChatProvisionFakeEndpoint(endpoint, fake, cancellationToken).ConfigureAwait(false);
                    string missingGraph = Guid.NewGuid().ToString();
                    string baseUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + missingGraph + "/chat";

                    HttpOutcome openAi = await AuthRestAsync(HttpMethod.Post, baseUrl + "/completions", userBearer,
                        "{\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}", cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, openAi.Status, "OpenAI-format completion against an unknown graph yields 404 (body " + Truncate(openAi.Body, 300) + ")");
                    AssertTrue(openAi.Body.Contains("invalid_request_error"), "The unknown-graph 404 uses the OpenAI error envelope");

                    HttpOutcome ollama = await AuthRestAsync(HttpMethod.Post, baseUrl + "/ollama", userBearer,
                        "{\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"stream\":false}", cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, ollama.Status, "Ollama-format completion against an unknown graph yields 404 (body " + Truncate(ollama.Body, 300) + ")");

                    HttpOutcome models = await AuthRestAsync(HttpMethod.Get, baseUrl + "/models", userBearer, null, cancellationToken).ConfigureAwait(false);
                    AssertEqual(404, models.Status, "Compat model list against an unknown graph yields 404 (body " + Truncate(models.Body, 300) + ")");
                }
                finally
                {
                    await CleanupMcpServer().ConfigureAwait(false);
                }
            }
        }

        private static async Task<string> ChatProvisionGraphAsync(string endpoint, string name, CancellationToken cancellationToken)
        {
            HttpOutcome created = await AuthRestAsync(HttpMethod.Put,
                endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs",
                _AdminBearerToken,
                "{\"Name\":\"" + name + "\"}",
                cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(created.Status), "Provisioned graph '" + name + "' (status " + created.Status + " body " + Truncate(created.Body, 200) + ")");
            return ExtractGuid(created.Body);
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
