<img src="https://github.com/jchristn/LiteGraph/blob/main/assets/favicon.png" width="256" height="256">

# LiteGraph C# SDK

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.Sdk.svg?style=flat)](https://www.nuget.org/packages/LiteGraph.Sdk/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.Sdk.svg)](https://www.nuget.org/packages/LiteGraph.Sdk/)

C# SDK for interacting with the LiteGraph REST API.

LiteGraph is a lightweight graph database with both relational and vector support, built using SQLite, with support for exporting to GEXF. LiteGraph is intended to be a multi-modal database primarily for providing persistence and retrieval for knowledge and artificial intelligence applications.

For more information, visit the [LiteGraph repository](https://github.com/litegraphdb/litegraph) or the [documentation](https://litegraph.readme.io/).

## New in v3.1.x

- Added support for labels on graphs, nodes, edges (string list)
- Added support for vector persistence and search
- Updated SDK, test, and Postman collections accordingly
- Updated GEXF export to support labels and tags
- Internal refactor to reduce code bloat
- Multiple bugfixes and QoL improvements

## Bugs, Feedback, or Enhancement Requests

Please feel free to start an issue or a discussion!

## Simple Example, Embedded 

Embedding LiteGraph into your application is simple and requires no configuration of users or credentials.  Refer to the ```Test``` project for a full example.

```csharp
using LiteGraph;

LiteGraphClient graph = new LiteGraphClient(); // using Sqlite file litegraph.db
LiteGraphClient graph = new LiteGraphClient(
  new SqliteRepository("mydatabase.db")
  ); // use a specific file

graph.InitializeRepository();

// Create a tenant
TenantMetadata tenant = graph.CreateTenant(Guid.NewGuid(), "My tenant");

// Create a graph
Graph graph = graph.CreateGraph(tenant.GUID, "This is my graph!");

// Create nodes
Node node1 = graph.CreateNode(tenant.GUID, graph.GUID, new Node { Name = "node1" });
Node node2 = graph.CreateNode(tenant.GUID, graph.GUID, new Node { Name = "node2" });
Node node3 = graph.CreateNode(tenant.GUID, graph.GUID, new Node { Name = "node3" });

// Create edges
Edge edge1 = graph.CreateEdge(tenant.GUID, graph.GUID, node1.GUID, node2.GUID, "Node 1 to node 2");
Edge edge2 = graph.CreateEdge(tenant.GUID, graph.GUID, node2.GUID, node3.GUID, "Node 2 to node 3");

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

The `Labels` property is a `List<string>` allowing to attach labels to any `Graph`, `Node`, or `Edge`.  The `Tags` property is a `NameValueCollection` allowing you to attach key-value pairs to any `Graph`, `Node`, or `Edge`.  These objects are stored in separate look-aside tables which are consulted or modified upon creation, update, deletion, retrieval, or search of the aforementioned types.

The `Data` property can also be attached to any `Graph`, `Node`, or `Edge` object and supports any object serializable to JSON.  This value is retrieved when reading or searching objects, and filters can be created to retrieve only objects that have matches based on elements in the object stored in `Data`.  Refer to [ExpressionTree](https://github.com/jchristn/ExpressionTree/) for information on how to craft expressions.

All of these properties can be used in conjunction with one another.

### Storing and Searching Labels

```csharp
List<string> labels = new List<string> 
{
  "test",
  "label1"
};

graph.CreateNode(tenant.GUID, new Node { Name = "Joel", Labels = labels });

foreach (Node node in graph.ReadNodes(tenant.GUID, graph.GUID, labels))
{
  Console.WriteLine(...);
}
```

### Storing and Searching Tags

```csharp
NameValueCollection nvc = new NameValueCollection();
nvc.Add("key", "value");

graph.CreateNode(tenant.GUID, new Node { Name = "Joel", Tags = nvc });

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
graph.CreateNode(tenant.GUID, graph.GUID, new Node { Name = "Joel", Data = person1 });

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

Further, it is strongly recommended that you make extensive use of labels, tags, and expressions (data filters) when performing a vector search to reduce the number of calculations to compute score, distance, and inner product.

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

graph.CreateNode(new Node { Name = "Joel", Data = person1 }, Vectors = new List<VectorMetadata> { vectors });

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

A Docker image is available in [Docker Hub](https://hub.docker.com/r/jchristn77/litegraph) under `jchristn77/litegraph`.  Use the Docker Compose start (`compose-up.sh` and `compose-up.bat`) and stop (`compose-down.sh` and `compose-down.bat`) scripts in the `Docker` directory if you wish to run within Docker Compose.  Ensure that you have a valid database file (e.g. `litegraph.db`) and configuration file (e.g. `litegraph.json`) exposed into your container.

## Version History

Please refer to ```CHANGELOG.md``` for version history.

