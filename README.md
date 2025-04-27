<img src="https://github.com/jchristn/LiteGraph/blob/main/assets/favicon.png" width="256" height="256">

# LiteGraph

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.svg?style=flat)](https://www.nuget.org/packages/LiteGraph/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.svg)](https://www.nuget.org/packages/LiteGraph) 

LiteGraph is a property graph database with support for graph relationships, tags, labels, metadata, data, and vectors.  LiteGraph is intended to be a unified database for providing persistence and retrieval for knowledge and artificial intelligence applications.

LiteGraph can be run in-process (using `LiteGraphClient`) or as a standalone RESTful server (using `LiteGraph.Server`).

## New in v4.0.x

- Major internal refactor for both the graph repository base and the client class
- Separation of responsibilities; graph repository base owns primitives, client class owns validation and cross-cutting
- Consistency in interface API names and behaviors
- Consistency in passing of query parameters such as skip to implementations and primitives
- Consolidation of create, update, and delete actions within a single transaction
- Batch APIs for creation and deletion of labels, tags, vectors, edges, and nodes
- Simple database caching to offload existence validation for tenants, graphs, nodes, and edges
- Dependency updates and bug fixes
- Minor Postman fixes

## Bugs, Feedback, or Enhancement Requests

Please feel free to start an issue or a discussion!

## Simple Example, Embedded 

Embedding LiteGraph into your application is simple and requires no configuration of users or credentials.  Refer to the ```Test``` project for a full example.

```csharp
using LiteGraph;

LiteGraphClient graph = new LiteGraphClient(new SqliteRepository("litegraph.db"));
graph.InitializeRepository();

// Create a tenant
TenantMetadata tenant = graph.CreateTenant(new TenantMetadata { Name = "My tenant" });

// Create a graph
Graph graph = graph.CreateGraph(new Graph { TenantGUID = tenant.GUID, Name = "This is my graph!" });

// Create nodes
Node node1 = graph.CreateNode(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node1" });
Node node2 = graph.CreateNode(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node2" });
Node node3 = graph.CreateNode(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node3" });

// Create edges
Edge edge1 = graph.CreateEdge(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = node1.GUID, To = node2.GUID, Name = "Node 1 to node 2" });
Edge edge2 = graph.CreateEdge(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = node2.GUID, To = node3.GUID, Name = "Node 2 to node 3" });

// Find routes
foreach (RouteDetail route in graph.GetRoutes(
  SearchTypeEnum.DepthFirstSearch,
  tenant.GUID,
  graph.GUID,
  node1.GUID,
  node2.GUID))
{
  Console.WriteLine(...);
}

// Export to GEXF file
graph.ExportGraphToGexfFile(tenant.GUID, graph.GUID, "mygraph.gexf");
```

## Working with Object Labels, Tags, and Data

The `Labels` property is a `List<string>` allowing you to attach labels to any `Graph`, `Node`, or `Edge`, i.e. `[ "mylabel" ]`.

The `Tags` property is a `NameValueCollection` allowing you to attach key-value pairs to any `Graph`, `Node`, or `Edge`, i.e. `{ "foo": "bar" }`.

The `Data` property is an `object` and can be attached to any `Graph`, `Node`, or `Edge`.  `Data` supports any object serializable to JSON.  This value is retrieved when reading or searching objects, and filters can be created to retrieve only objects that have matches based on elements in the object stored in `Data`.  Refer to [ExpressionTree](https://github.com/jchristn/ExpressionTree/) for information on how to craft expressions.

The `Vectors` property can be attached to any `Graph`, `Node`, or `Edge` object, and is a `List<VectorMetadata>`.  The embeddings within can be used for a variety of different vector searches (such as `CosineSimilarity`).

All of these properties can be used in conjunction with one another when filtering for retrieval.

### Storing and Searching Labels

```csharp
List<string> labels = new List<string> 
{
  "test",
  "label1"
};

graph.CreateNode(new Node { TenantGUID = tenant.GUID, Name = "Joel", Labels = labels });

foreach (Node node in graph.ReadNodes(tenant.GUID, graph.GUID, labels))
{
  Console.WriteLine(...);
}
```

### Storing and Searching Tags

```csharp
NameValueCollection nvc = new NameValueCollection();
nvc.Add("key", "value");

graph.CreateNode(new Node { TenantGUID = tenant.GUID, Name = "Joel", Tags = nvc });

foreach (Node node in graph.ReadNodes(tenant.GUID, graph.GUID, null, nvc))
{
  Console.WriteLine(...);
}
```

### Storing and Searching Data

```csharp
using ExpressionTree;

class Person 
{
  public string Name { get; set; } = null;
  public int Age { get; set; } = 0;
  public string City { get; set; } = "San Jose";
}

Person person1 = new Person { Name = "Joel", Age = 47, City = "San Jose" };
graph.CreateNode(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Data = person1 });

Expr expr = new Expr 
{
  "Left": "City",
  "Operator": "Equals",
  "Right": "San Jose"
};

foreach (Node node in graph.ReadNodes(tenant.GUID, graph.GUID, null, expr))
{
  Console.WriteLine(...);
}
```

### Storing and Searching Vectors

It is important to note that vectors have a dimensionality (number of array elements) and vector searches are only performed against graphs, nodes, and edges where the attached vector objects have a dimensionality consistent with the input.

Further, it is strongly recommended that you make extensive use of labels, tags, and expressions (data filters) when performing a vector search to reduce the number of records against which score, distance, or inner product calculations are performed. 

```csharp
using ExpressionTree;

class Person 
{
  public string Name { get; set; } = null;
  public int Age { get; set; } = 0;
  public string City { get; set; } = "San Jose";
}

Person person1 = new Person { Name = "Joel", Age = 47, City = "San Jose" };

VectorMetadata vectors = new VectorMetadata 
{
  Model = "testmodel",
  Dimensionality = 3,
  Content = "testcontent",
  Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
};

graph.CreateNode(new Node { Name = "Joel", Data = person1, Vectors = new List<VectorMetadata> { vectors } });

foreach (VectorSearchResult result in graph.SearchVectors(
  VectorSearchDomainEnum.Node,
  VectorSearchTypeEnum.CosineSimilarity,
  new List<float> { 0.1f, 0.2f, 0.3f },
  tenant.GUID,
  graph.GUID,
  null,  // labels
  null,  // tags
  null,  // filter
))
{
  Console.WriteLine("Node " + result.Node.GUID + " score " + result.Score);
}
```

### Enumeration Ordering

A variety of `EnumerationOrderEnum` options are available when enumerating objects.

- `CreatedAscending` - sort results in ascending order by creation timestamp
- `CreatedDescending` - sort results in descending order by creation timestamp
- `NameAscending` - sort results in ascending order by name
- `NameDescending` - sort results in descending order by name
- `GuidAscending` - sort results in ascending order by GUID
- `GuidDescending` - sort results in descending order by GUID
- `CostAscending` - for edges only, sort results in ascending order by cost
- `CostDescending` - for edges only, sort results in descending order by cost
- `MostConnected` - for nodes only, sort results in descending order by total edge count
- `LeastConnected` - for nodes only, sort results in ascending order by total edge count

## REST API

LiteGraph includes a project called `LiteGraph.Server` which allows you to deploy a RESTful front-end for LiteGraph.  Refer to `REST_API.md` and also the Postman collection in the root of this repository for details.  By default, LiteGraph.Server listens on `http://localhost:8701` and is only accessible to `localhost`.  Modify the `litegraph.json` file to change settings including hostname and port.

Listening on a specific hostname should not require elevated privileges.  However, listening on any hostname (i.e. using `*` or `0.0.0.0` will require elevated privileges).

```csharp
$ cd LiteGraph.Server/bin/Debug/net8.0
$ dotnet LiteGraph.Server.dll

  _ _ _                          _
 | (_) |_ ___ __ _ _ _ __ _ _ __| |_
 | | |  _/ -_) _` | '_/ _` | '_ \ ' \
 |_|_|\__\___\__, |_| \__,_| .__/_||_|
             |___/         |_|

 LiteGraph Server
 (c)2025 Joel Christner

Using settings file './litegraph.json'
Settings file './litegraph.json' does not exist, creating
Initializing logging
| syslog://127.0.0.1:514
2025-01-27 22:09:08 joel-laptop Debug [LiteGraphServer] logging initialized
Creating default records in database litegraph.db
| Created tenant     : 00000000-0000-0000-0000-000000000000
| Created user       : 00000000-0000-0000-0000-000000000000 email: default@user.com pass: password
| Created credential : 00000000-0000-0000-0000-000000000000 bearer token: default
| Created graph      : 00000000-0000-0000-0000-000000000000 Default graph
Finished creating default records
2025-01-27 22:09:09 joel-laptop Debug [ServiceHandler] initialized service handler
2025-01-27 22:09:09 joel-laptop Info [RestServiceHandler] starting REST server on http://localhost:8701/
2025-01-27 22:09:09 joel-laptop Alert [RestServiceHandler]

NOTICE
------
LiteGraph is configured to listen on localhost and will not be externally accessible.
Modify ./litegraph.json to change the REST listener hostname to make externally accessible.

2025-01-27 22:09:09 joel-laptop Info [LiteGraphServer] started at 01/27/2025 10:09:09 PM using process ID 56556
```

## Running in Docker

A Docker image is available in [Docker Hub](https://hub.docker.com/r/jchristn/litegraph) under `jchristn/litegraph`.  Use the Docker Compose start (`compose-up.sh` and `compose-up.bat`) and stop (`compose-down.sh` and `compose-down.bat`) scripts in the `Docker` directory if you wish to run within Docker Compose.  Ensure that you have a valid database file (e.g. `litegraph.db`) and configuration file (e.g. `litegraph.json`) exposed into your container.

## Version History

Please refer to ```CHANGELOG.md``` for version history.

