# LiteGraph Client SDKs

This directory contains official client SDKs for interacting with the LiteGraph REST API.

## Available SDKs

| SDK | Language | Package | Documentation |
|-----|----------|---------|---------------|
| [C# SDK](csharp/) | C# / .NET | [![NuGet](https://img.shields.io/nuget/v/LiteGraph.Sdk.svg)](https://www.nuget.org/packages/LiteGraph.Sdk/) | [README](csharp/README.md) |
| [Python SDK](python/) | Python 3.8+ | [![PyPI](https://img.shields.io/pypi/v/litegraph-sdk.svg)](https://pypi.org/project/litegraph-sdk/) | [README](python/README.md) |
| [JavaScript SDK](js/) | Node.js / JavaScript | [![npm](https://img.shields.io/npm/v/litegraphdb.svg)](https://www.npmjs.com/package/litegraphdb) | [README](js/README.md) |

## Quick Start

### C# (.NET)

```bash
dotnet add package LiteGraph.Sdk
```

```csharp
using LiteGraph.Sdk;

LiteGraphSdk sdk = new LiteGraphSdk("http://localhost:8701", "default");
Graph graph = await sdk.Graph.Create(new Graph { TenantGUID = tenantGuid, Name = "My graph" });
```

### Python

```bash
pip install litegraph_sdk
```

```python
from litegraph_sdk import configure, Graph

configure(endpoint="http://localhost:8701", tenant_guid="your-tenant-guid", access_key="default")
graph = Graph.create(name="My Graph")
```

### JavaScript

```bash
npm install litegraphdb
```

```javascript
import { LiteGraphSdk } from 'litegraphdb';

const sdk = new LiteGraphSdk('http://localhost:8701', 'your-tenant-guid', 'default');
const graph = await sdk.createGraph('guid', 'My Graph');
```

## Features

All SDKs provide:

- Full REST API coverage for graphs, nodes, edges, labels, tags, and vectors
- Multi-tenant support
- Authentication via bearer tokens
- Search and filtering capabilities
- Route finding and graph traversal
- GEXF export support
- Error handling and retry mechanisms

## Documentation

For complete API documentation, visit [litegraph.readme.io](https://litegraph.readme.io/).

For REST API endpoint reference, see [REST_API.md](../REST_API.md) in the repository root.
