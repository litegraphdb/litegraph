<img src="../../assets/favicon.png" width="256" height="256">

# LiteGraph C# SDK

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.Sdk.svg?style=flat)](https://www.nuget.org/packages/LiteGraph.Sdk/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.Sdk.svg)](https://www.nuget.org/packages/LiteGraph.Sdk)

This SDK is part of the [LiteGraph monorepo](../../README.md). For other language SDKs, see the [SDK overview](../README.md).

LiteGraph is a property graph database with support for graph relationships, tags, labels, metadata, data, and vectors.  LiteGraph is intended to be a unified database for providing persistence and retrieval for knowledge and artificial intelligence applications.

Current release: v8.1.0.

## New in v8.1.0

- Added `sdk.Chat` methods covering the LiteGraph chat surface: endpoint management, completions (non-streaming and SSE streaming), threads, turns, feedback, endpoint health, and per-tenant chat settings
- **Breaking**: every list-returning read method (`ReadMany`, `ReadAllInTenant`, `ReadAllInGraph`, `ReadByGuids`, `ReadNodeEdges`, `ReadParents`, `ReadChildren`, `ReadNeighbors`, `ReadMostConnected`, `ReadLeastConnected`, `ReadEdgesFromNode`, `ReadEdgesToNode`, `ReadEdgesBetweenNodes`, chat reads, `ListBackups`, `GetTenantsForEmail`, and `SearchVectors`) now returns an `EnumerationResult<T>` pagination envelope instead of a raw `List<T>`.  Access records via `.Objects`, and use `TotalRecords`, `RecordsRemaining`, `EndOfResults`, and `ContinuationToken` for paging.  Most read methods accept optional `maxKeys` (1-1000, default 1000), `skip`, `order`, and `continuationToken` parameters.

### Paginated reads

All list-shaped GET routes return the enumeration envelope:

```csharp
EnumerationResult<Node> page = await sdk.Node.ReadMany(
    tenantGuid,
    graphGuid,
    order: EnumerationOrderEnum.CreatedDescending,
    skip: 0,
    maxKeys: 100);

foreach (Node node in page.Objects) Console.WriteLine(node.Name);
Console.WriteLine($"{page.TotalRecords} total, {page.RecordsRemaining} remaining, end: {page.EndOfResults}");

// Continue the enumeration with the returned token
if (!page.EndOfResults && page.ContinuationToken != null)
{
    EnumerationResult<Node> next = await sdk.Node.ReadMany(
        tenantGuid,
        graphGuid,
        maxKeys: 100,
        continuationToken: page.ContinuationToken);
}
```

## New in v7.0.0

- Added v7 graph transaction diagnostics, lifecycle state, and isolation-level models
- Added transaction execution helpers aligned with the REST v7 transaction response body
- Updated SDK metadata for the LiteGraph v7.0.0 release

## New in v6.0.2

- Added `BulkCreateReturnModeEnum` overloads for label, tag, vector, node, and edge `CreateMany` methods
- Added minimal bulk create responses while preserving existing full-response defaults
- Updated bulk create documentation and route coverage

## New in v6.0.0

- Native graph query, graph transaction, authorization, and request history client helpers
- v6 request/response models for query, transaction, and authorization workflows
- API coverage aligned with the LiteGraph v6.0.0 REST surface

## Bugs, Feedback, or Enhancement Requests

Please feel free to start an issue or a discussion!

## Example

Refer to the `Test.Sdk` project for a full example.

```csharp
using LiteGraph.Sdk;

LiteGraphSdk sdk = new LiteGraphSdk("http://localhost:8701", "default");
Guid tenantGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");

Graph graph = sdk.Graph.Create(new Graph { TenantGUID = tenantGuid, Name = "My graph" });
Node node1 = sdk.Node.Create(new Node { TenantGUID = tenantGuid, GraphGUID = graph.GUID, Name = "My node 1" });
Node node2 = sdk.Node.Create(new Node { TenantGUID = tenantGuid, GraphGUID = graph.GUID, Name = "My node 2" });
Edge edgeFrom1To2 = sdk.Edge.Create(new Edge { TenantGUID = tenantGuid, GraphGUID = graph.GUID, From = node1.GUID, To = node2.GUID });
```

## Graph Transactions

Graph transactions execute create, update, delete, attach, detach, and upsert operations atomically inside one tenant and graph. Failed execution returns a `TransactionResult` with `Success = false` and diagnostics. Request validation failures set `ValidationFailure = true`; provider execution failures set `RolledBack = true`.

```csharp
Guid adaGuid = Guid.NewGuid();
Guid graceGuid = Guid.NewGuid();

TransactionRequest request = sdk.Transaction.CreateRequestBuilder()
    .WithMaxOperations(10)
    .WithTimeoutSeconds(30)
    .WithIsolationLevel(TransactionIsolationLevelEnum.Default)
    .CreateNode(new Node { GUID = adaGuid, Name = "Ada" })
    .CreateNode(new Node { GUID = graceGuid, Name = "Grace" })
    .CreateEdge(new Edge { From = adaGuid, To = graceGuid, Name = "Worked With" })
    .Build();

TransactionResult result = await sdk.Transaction.Execute(tenantGuid, graph.GUID, request);

Console.WriteLine($"{result.Success} {result.State} {result.TransactionId} {result.DurationMs}ms");
```

`TransactionResult` includes lifecycle state, validation-failure state, provider, isolation, commit/rollback timing, retryability, concurrency-conflict, provider error code, and whether the request used an isolated transaction repository or the legacy serialized fallback.

## Chat

The `sdk.Chat` property wraps the LiteGraph chat surface: chat endpoints (upstream completion and embedding providers), the model catalog, completions, threads, feedback, endpoint health, and per-tenant chat settings. Completions, thread creation, and feedback submission require a user principal, so instantiate the SDK with email, password, and tenant GUID (or a user-linked credential) for those calls.

### Endpoint management

```csharp
ChatEndpoint endpoint = await sdk.Chat.CreateEndpoint(new ChatEndpoint
{
    TenantGUID = tenantGuid,
    Name = "Local Ollama",
    EndpointType = ChatEndpointTypeEnum.Completion,
    Provider = ChatProviderTypeEnum.Ollama,
    Endpoint = "http://127.0.0.1:11434",
    Model = "gemma3:4b",
    ContextWindowTokens = 131072    // optional; caps the conversation-history budget
});

ChatEndpoint embedding = await sdk.Chat.CreateEndpoint(new ChatEndpoint
{
    TenantGUID = tenantGuid,
    Name = "Embeddings",
    EndpointType = ChatEndpointTypeEnum.Embedding,
    Provider = ChatProviderTypeEnum.OpenAI,
    Endpoint = "https://api.openai.com",
    ApiKey = "sk-...",
    Model = "text-embedding-3-small"
});

EnumerationResult<ChatEndpoint> completionEndpoints = await sdk.Chat.ReadEndpoints(tenantGuid, ChatEndpointTypeEnum.Completion);
Console.WriteLine($"{completionEndpoints.TotalRecords} completion endpoints");
ChatEndpoint read = await sdk.Chat.ReadEndpoint(tenantGuid, endpoint.GUID);
ChatEndpointTestResult test = await sdk.Chat.TestEndpoint(tenantGuid, endpoint.GUID);
Console.WriteLine($"Reachable: {test.Reachable}, model exists: {test.ModelExists}");

EnumerationResult<ChatEndpointHealth> health = await sdk.Chat.ReadAllEndpointHealth(tenantGuid);
ChatEndpointHealth one = await sdk.Chat.ReadEndpointHealth(tenantGuid, endpoint.GUID);
```

API keys are redacted to their last four characters in every response; sending a redacted value back on update preserves the stored key. `UpdateEndpoint` and `DeleteEndpoint` complete the management surface; `EndpointExists` performs a lightweight HEAD check.

### Model catalog

Non-admin callers can enumerate the active endpoints available to them, projected down to what a model picker needs (no URLs, keys, or health configuration):

```csharp
EnumerationResult<ChatModelSummary> models = await sdk.Chat.ReadModels(tenantGuid);
foreach (ChatModelSummary model in models.Objects)
{
    Console.WriteLine($"{model.Name} ({model.Provider}/{model.Model}, {model.EndpointType}) default: {model.IsDefault}");
}
```

Pass a summary's `GUID` as `CompletionEndpointGUID` or `EmbeddingEndpointGUID` on a completion request to select it.

### Non-streaming completion

```csharp
ChatCompletionResult result = await sdk.Chat.Completion(tenantGuid, new ChatCompletionRequest
{
    Message = "What are the most connected nodes in this graph?",
    GraphGUID = graph.GUID
});

Console.WriteLine(result.Message);
Console.WriteLine($"Thread {result.ThreadGUID}, {result.CompletionTokens} tokens, {result.TotalDurationMs}ms");
```

Omitting `ThreadGUID` creates a new thread bound to `GraphGUID`; pass the returned `ThreadGUID` on the next call to continue the conversation.

### Streaming completion

```csharp
await foreach (ChatStreamEvent ev in sdk.Chat.CompletionStreaming(tenantGuid, new ChatCompletionRequest
{
    Message = "Summarize this graph.",
    GraphGUID = graph.GUID
}))
{
    if (ev.Event == "delta") Console.Write(ev.Content);
    else if (ev.Event == "tool_call") Console.WriteLine($"[tool: {ev.Name}]");
    else if (ev.Event == "usage") Console.WriteLine($"\n{ev.Usage.CompletionTokens} tokens");
    else if (ev.Event == "error") Console.WriteLine($"error: {ev.Message}");
}
```

### Threads, feedback, and settings

```csharp
List<ChatThread> threads = (await sdk.Chat.ReadThreads(tenantGuid)).Objects;
List<ChatTurn> turns = (await sdk.Chat.ReadThreadTurns(tenantGuid, threads[0].GUID)).Objects;

// Rename a thread (only Title is honored)
await sdk.Chat.UpdateThread(tenantGuid, threads[0].GUID, new ChatThread { Title = "Renamed thread" });
await sdk.Chat.DeleteThread(tenantGuid, threads[0].GUID);

// Feedback: submit as a user; read and delete require admin
ChatFeedback feedback = await sdk.Chat.SubmitFeedback(tenantGuid, turns[0].GUID, ChatFeedbackRatingEnum.ThumbsUp, "Great answer");
EnumerationResult<ChatFeedback> allFeedback = await sdk.Chat.ReadFeedback(tenantGuid);
await sdk.Chat.DeleteFeedback(tenantGuid, feedback.GUID);

ChatSettings settings = await sdk.Chat.ReadChatSettings(tenantGuid);
settings.DefaultCompletionEndpointGUID = endpoint.GUID;
settings.DefaultEmbeddingEndpointGUID = embedding.GUID;
await sdk.Chat.UpdateChatSettings(settings);
```

## Version History

Please refer to ```CHANGELOG.md``` for version history.
