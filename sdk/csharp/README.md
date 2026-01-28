<img src="../../assets/favicon.png" width="256" height="256">

# LiteGraph C# SDK

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.Sdk.svg?style=flat)](https://www.nuget.org/packages/LiteGraph.Sdk/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.Sdk.svg)](https://www.nuget.org/packages/LiteGraph.Sdk)

This SDK is part of the [LiteGraph monorepo](../../README.md). For other language SDKs, see the [SDK overview](../README.md).

LiteGraph is a property graph database with support for graph relationships, tags, labels, metadata, data, and vectors.  LiteGraph is intended to be a unified database for providing persistence and retrieval for knowledge and artificial intelligence applications.

## New in v4.0.x

- Refactor to group and simplify APIs
- Multiple bugfixes and QoL improvements

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

## Version History

Please refer to ```CHANGELOG.md``` for version history.

