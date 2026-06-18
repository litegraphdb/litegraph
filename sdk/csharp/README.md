<img src="../../assets/favicon.png" width="256" height="256">

# LiteGraph C# SDK

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.Sdk.svg?style=flat)](https://www.nuget.org/packages/LiteGraph.Sdk/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.Sdk.svg)](https://www.nuget.org/packages/LiteGraph.Sdk)

This SDK is part of the [LiteGraph monorepo](../../README.md). For other language SDKs, see the [SDK overview](../README.md).

LiteGraph is a property graph database with support for graph relationships, tags, labels, metadata, data, and vectors.  LiteGraph is intended to be a unified database for providing persistence and retrieval for knowledge and artificial intelligence applications.

Current release: v6.0.2.

## New in v6.0.2

- Added `BulkCreateReturnModeEnum` overloads for label, tag, vector, node, and edge `CreateMany` methods
- Added minimal bulk create responses while preserving existing full-response defaults
- Updated bulk create documentation and route coverage

## New in v6.0.0

- Native graph query, graph transaction, authorization, and request history client helpers
- v7 request/response models for query, transaction, and authorization workflows
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

Console.WriteLine($"{result.Success} {result.TransactionId} {result.DurationMs}ms");
```

`TransactionResult` includes validation-failure state, provider, isolation, commit/rollback timing, retryability, concurrency-conflict, provider error code, and whether the request used an isolated transaction repository or the legacy serialized fallback.

## Version History

Please refer to ```CHANGELOG.md``` for version history.
