# LiteGraph Graph Transactions

LiteGraph graph transactions execute a list of graph child object operations atomically inside one tenant and one graph. A transaction can create, update, delete, attach, detach, and upsert nodes, edges, labels, tags, and vectors where the operation applies to that object type.

Transactions do not mutate tenant metadata, graph metadata, users, credentials, or admin resources.

## Execution Surfaces

Transactions execute through:

- C# SDK: `client.Transaction.Execute(tenantGuid, graphGuid, request)`
- REST: `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/transaction`
- MCP: `graph/transaction` tool over HTTP, TCP, and WebSocket transports

## C# Request Builder

The C# SDK exposes a fluent request builder for common transaction operations:

```csharp
TransactionRequest request = client.Transaction
    .CreateRequestBuilder()
    .WithMaxOperations(10)
    .WithTimeoutSeconds(30)
    .CreateNode(new Node { GUID = adaGuid, Name = "Ada" })
    .CreateNode(new Node { GUID = graceGuid, Name = "Grace" })
    .CreateEdge(new Edge
    {
        Name = "Worked With",
        From = adaGuid,
        To = graceGuid
    })
    .Build();

TransactionResult result = await client.Transaction.Execute(
    tenantGuid,
    graphGuid,
    request,
    cancellationToken);
```

The builder supports generic `Create`, `Update`, `Delete`, `Attach`, `Detach`, and `Upsert` methods plus typed helpers for nodes, edges, labels, tags, and vectors.

## Request Shape

```json
{
  "Operations": [
    {
      "OperationType": "Create",
      "ObjectType": "Node",
      "Payload": {
        "Name": "Ada"
      }
    }
  ],
  "MaxOperations": 1000,
  "TimeoutSeconds": 60
}
```

`MaxOperations` defaults to `1000`. `TimeoutSeconds` defaults to `60`.

## Operation Types

Supported `OperationType` values:

- `Create`
- `Update`
- `Delete`
- `Attach`
- `Detach`
- `Upsert`

Supported `ObjectType` values:

- `Node`
- `Edge`
- `Label`
- `Tag`
- `Vector`

## Payload Rules

Create, update, attach, and upsert operations use `Payload`.

Delete and detach operations use `GUID`. They can also use a payload containing the object GUID, but `GUID` is clearer and preferred.

Attach and detach operations apply to subordinate graph child objects only: labels, tags, and vectors. Attach creates a subordinate object on a node or edge target. Attach payloads must set exactly one of `NodeGUID` or `EdgeGUID`. Detach deletes the subordinate object by GUID and does not delete the node or edge target.

Upsert operations apply to nodes, edges, labels, tags, and vectors. Upsert reads by GUID inside the transaction scope. If the object exists in the same graph, LiteGraph updates it; otherwise, LiteGraph creates it. `GUID` overrides any payload GUID.

LiteGraph applies the route tenant and graph to transaction child objects. Payload tenant and graph values are overwritten where appropriate so a request cannot escape the transaction scope.

LiteGraph validates operation shape before opening the graph transaction. A malformed operation, such as a delete without a GUID, fails before earlier operations are written.

## Create Nodes

```json
{
  "Operations": [
    {
      "OperationType": "Create",
      "ObjectType": "Node",
      "Payload": {
        "GUID": "11111111-1111-1111-1111-111111111111",
        "Name": "Ada",
        "Data": {
          "role": "mathematician"
        }
      }
    },
    {
      "OperationType": "Create",
      "ObjectType": "Node",
      "Payload": {
        "GUID": "22222222-2222-2222-2222-222222222222",
        "Name": "Grace"
      }
    }
  ]
}
```

## Create Edges

```json
{
  "Operations": [
    {
      "OperationType": "Create",
      "ObjectType": "Edge",
      "Payload": {
        "Name": "Worked With",
        "From": "11111111-1111-1111-1111-111111111111",
        "To": "22222222-2222-2222-2222-222222222222",
        "Cost": 1
      }
    }
  ]
}
```

## Create Labels And Tags

```json
{
  "Operations": [
    {
      "OperationType": "Create",
      "ObjectType": "Label",
      "Payload": {
        "NodeGUID": "11111111-1111-1111-1111-111111111111",
        "Label": "Person"
      }
    },
    {
      "OperationType": "Create",
      "ObjectType": "Tag",
      "Payload": {
        "NodeGUID": "11111111-1111-1111-1111-111111111111",
        "Key": "field",
        "Value": "math"
      }
    }
  ]
}
```

## Create Vectors

```json
{
  "Operations": [
    {
      "OperationType": "Create",
      "ObjectType": "Vector",
      "Payload": {
        "NodeGUID": "11111111-1111-1111-1111-111111111111",
        "Model": "example-model",
        "Dimensionality": 3,
        "Content": "Ada vector",
        "Vectors": [0.1, 0.2, 0.3]
      }
    }
  ]
}
```

LiteGraph stores supplied vectors. It does not generate embeddings as part of transaction execution.

## Vector Index Consistency

Database writes are the atomic boundary. Vector index updates are recovery-oriented and are not rolled back as part of the database transaction.

If a vector index update fails after the database mutation has committed, LiteGraph marks the graph's vector index dirty. Dirty state includes `VectorIndexDirty`, `VectorIndexDirtyUtc`, and `VectorIndexDirtyReason` on the graph record, and `IsDirty`, `DirtySinceUtc`, and `DirtyReason` in vector index statistics.

When a graph index is dirty, indexed node-vector search is bypassed and LiteGraph falls back to persisted-vector search so results remain correct. Rebuild the index with `client.Graph.RebuildVectorIndex(...)`, `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild`, or the MCP `graph/rebuild_vector_index` tool. A successful rebuild clears dirty state.

If a graph transaction mutates a vector index and later rolls back, or if transaction commit fails after an index mutation, LiteGraph marks the graph index dirty because the index may no longer match persisted vectors.

## Update

```json
{
  "Operations": [
    {
      "OperationType": "Update",
      "ObjectType": "Node",
      "GUID": "11111111-1111-1111-1111-111111111111",
      "Payload": {
        "Name": "Ada Lovelace",
        "Data": {
          "role": "programmer"
        }
      }
    }
  ]
}
```

For update operations, `GUID` overrides any GUID in the payload.

## Delete

```json
{
  "Operations": [
    {
      "OperationType": "Delete",
      "ObjectType": "Tag",
      "GUID": "33333333-3333-3333-3333-333333333333"
    }
  ]
}
```

## Attach And Detach

```json
{
  "Operations": [
    {
      "OperationType": "Attach",
      "ObjectType": "Label",
      "Payload": {
        "GUID": "33333333-3333-3333-3333-333333333333",
        "NodeGUID": "11111111-1111-1111-1111-111111111111",
        "Label": "Person"
      }
    },
    {
      "OperationType": "Detach",
      "ObjectType": "Label",
      "GUID": "33333333-3333-3333-3333-333333333333"
    }
  ]
}
```

Attach supports `Label`, `Tag`, and `Vector`. Use create/delete for nodes and edges.

## Upsert

```json
{
  "Operations": [
    {
      "OperationType": "Upsert",
      "ObjectType": "Node",
      "GUID": "11111111-1111-1111-1111-111111111111",
      "Payload": {
        "Name": "Ada Lovelace"
      }
    },
    {
      "OperationType": "Upsert",
      "ObjectType": "Tag",
      "GUID": "44444444-4444-4444-4444-444444444444",
      "Payload": {
        "NodeGUID": "11111111-1111-1111-1111-111111111111",
        "Key": "field",
        "Value": "computing"
      }
    }
  ]
}
```

## Response Shape

```json
{
  "Success": true,
  "RolledBack": false,
  "FailedOperationIndex": null,
  "Error": null,
  "Operations": [
    {
      "Index": 0,
      "OperationType": "Create",
      "ObjectType": "Node",
      "GUID": "11111111-1111-1111-1111-111111111111",
      "Success": true,
      "Result": {
        "Name": "Ada"
      },
      "Error": null
    }
  ],
  "DurationMs": 12.5
}
```

If any operation fails, LiteGraph rolls back the transaction and returns `Success: false`, `RolledBack: true`, `FailedOperationIndex`, and an error message. Successful operation results before the failed operation may appear in the response, but their database writes are rolled back.

## MCP Usage

Use a full request:

```json
{
  "tenantGuid": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "graphGuid": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "request": {
    "Operations": [
      {
        "OperationType": "Create",
        "ObjectType": "Node",
        "Payload": {
          "Name": "Ada"
        }
      }
    ]
  }
}
```

Or pass `operations`, `maxOperations`, and `timeoutSeconds` directly:

```json
{
  "tenantGuid": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "graphGuid": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "operations": [
    {
      "OperationType": "Create",
      "ObjectType": "Node",
      "Payload": {
        "Name": "Ada"
      }
    }
  ],
  "maxOperations": 10,
  "timeoutSeconds": 30
}
```

The MCP tool forwards to the REST transaction endpoint so the configured MCP credential is evaluated by the same REST authentication, graph scoping, and credential-scope checks.

## Current Limits

- One transaction executes within one tenant and one graph.
- Nested transactions are not a public API.
- Vector index rollback is not guaranteed; LiteGraph marks uncertain index state dirty and rebuild remains the repair path.
- SQLite and PostgreSQL graph transactions are implemented. MySQL and SQL Server transaction behavior remains provider work.
