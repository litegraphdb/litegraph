<img src="https://github.com/jchristn/LiteGraph/blob/main/assets/favicon.png" width="256" height="256">

# LiteGraph

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.svg?style=flat)](https://www.nuget.org/packages/LiteGraph/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.svg)](https://www.nuget.org/packages/LiteGraph) [![Documentation](https://img.shields.io/badge/docs-litegraph.readme.io-blue)](https://litegraph.readme.io/) [![Dashboard](https://img.shields.io/badge/dashboard-github.com/litegraphdb/ui-green)](https://github.com/litegraphdb/ui) 

LiteGraph is a property graph database with support for graph relationships, tags, labels, metadata, data, and vectors.  LiteGraph is intended to be a unified database for providing persistence and retrieval for knowledge and artificial intelligence applications.

LiteGraph can be run in-process (using `LiteGraphClient`) or as a standalone RESTful server (using `LiteGraph.Server`). For comprehensive documentation, visit [litegraph.readme.io](https://litegraph.readme.io/). A web-based dashboard UI is available at [github.com/litegraphdb/ui](https://github.com/litegraphdb/ui).

## New in v5.x

- Breaking changes: migrated all APIs to full async/await

## Bugs, Feedback, or Enhancement Requests

Please feel free to start an issue or a discussion! For detailed documentation and guides, visit [litegraph.readme.io](https://litegraph.readme.io/).

## Simple Example, Embedded

Embedding LiteGraph into your application is simple and requires no configuration of users or credentials.  Refer to the ```Test``` project for a full example, or visit the [documentation](https://litegraph.readme.io/) for more comprehensive examples and guides.

```csharp
using LiteGraph;

LiteGraphClient client = new LiteGraphClient(new SqliteRepository("litegraph.db"));
client.InitializeRepository();

// Create a tenant
TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "My tenant" });

// Create a graph
Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "This is my graph!" });

// Create nodes
Node node1 = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node1" });
Node node2 = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node2" });
Node node3 = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node3" });

// Create edges
Edge edge1 = await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = node1.GUID, To = node2.GUID, Name = "Node 1 to node 2" });
Edge edge2 = await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = node2.GUID, To = node3.GUID, Name = "Node 2 to node 3" });

// Find routes
await foreach (RouteDetail route in client.Node.ReadRoutes(
  SearchTypeEnum.DepthFirstSearch,
  tenant.GUID,
  graph.GUID,
  node1.GUID,
  node2.GUID))
{
  Console.WriteLine(...);
}

// Export to GEXF file
await client.ExportGraphToGexfFile(tenant.GUID, graph.GUID, "mygraph.gexf", false, false);
```

## Simple Example, In-Memory

LiteGraph can be configured to run in-memory, with a specified database filename.  If the database exists, it will be fully loaded into memory, and then **must** later be `Flush()`ed out to disk when done.  If the database does not exist, it will be created.

```csharp
using LiteGraph;

LiteGraphClient client = new LiteGraphClient(new SqliteRepository("litegraph.db", true)); // true to run in-memory
client.InitializeRepository();

// Create a tenant
TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { Name = "My tenant" });

// Create a graph
Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, Name = "This is my graph!" });

// Create nodes
Node node1 = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node1" });
Node node2 = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node2" });
Node node3 = await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "node3" });

// Create edges
Edge edge1 = await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = node1.GUID, To = node2.GUID, Name = "Node 1 to node 2" });
Edge edge2 = await client.Edge.Create(new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = node2.GUID, To = node3.GUID, Name = "Node 2 to node 3" });

// Flush to disk
client.Flush();
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

await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Labels = labels });

await foreach (Node node in client.Node.ReadMany(tenant.GUID, graph.GUID, labels: labels))
{
  Console.WriteLine(...);
}
```

### Storing and Searching Tags

```csharp
NameValueCollection nvc = new NameValueCollection();
nvc.Add("key", "value");

await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Tags = nvc });

await foreach (Node node in client.Node.ReadMany(tenant.GUID, graph.GUID, tags: nvc))
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
await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Data = person1 });

Expr expr = new Expr
{
  "Left": "City",
  "Operator": "Equals",
  "Right": "San Jose"
};

await foreach (Node node in client.Node.ReadMany(tenant.GUID, graph.GUID, nodeFilter: expr))
{
  Console.WriteLine(...);
}
```

### Storing and Searching Vectors

It is important to note that vectors have a dimensionality (number of array elements) and vector searches are only performed against graphs, nodes, and edges where the attached vector objects have a dimensionality consistent with the input.

Further, it is strongly recommended that you make extensive use of labels, tags, and expressions (data filters) when performing a vector search to reduce the number of records against which score, distance, or inner product calculations are performed. 

`VectorSearchResult` objects have three properties used to weigh the similarity or distance of the result to the supplied query:
- `Score` - a higher score indicates a greater degree of similarity to the query
- `Distance` - a lower distance indicates a greater proximity to the query
- `InnerProduct` - a higher inner product indicates a greater degree of similarity to the query

When searching vectors, you can supply one of three requirements thresholds that must be met:
- `MinimumScore` - only return results with this score or higher
- `MaximumDistance` - only return results with distance less than the supplied value
- `MinimumInnerProduct` - only return results with this inner product or higher

Your requirements threshold should match with the `VectorSearchTypeEnum` you supply to the search.

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

await client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Data = person1, Vectors = new List<VectorMetadata> { vectors } });

VectorSearchRequest req = new VectorSearchRequest
{
  TenantGUID = tenant.GUID,
  GraphGUID = graph.GUID,
  Domain = VectorSearchDomainEnum.Node,
  SearchType = VectorSearchTypeEnum.CosineSimilarity,
  Vectors = new List<float> { 0.1f, 0.2f, 0.3f },
  TopK = 10,
  MinimumScore = 0.1,
  MaximumDistance = 100,
  MinimumInnerProduct = 0.1
};

await foreach (VectorSearchResult result in client.Vector.Search(req))
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

To enumerate, use the enumeration API for the resource you wish.

```csharp
EnumerationRequest query = new EnumerationRequest
{
  TenantGUID = tenant.GUID,
  Ordering = EnumerationOrderEnum.CreatedDescending,
  IncludeData = true,
  IncludeSubordinates = true,
  MaxResults = 5,
  ContinuationToken = null,         // set to the continuation token from the last results to paginate
  Labels = new List<string>(),      // labels on which to match
  Tags = new NameValueCollection(), // tags on which to match
  Filter = null,                    // expression on which to match from data property
};

EnumerationResult<Node> result = await client.Node.Enumerate(query);
// returns
{
    "Success": true,
    "Timestamp": {
        "Start": "2025-06-22T01:17:42.984885Z",
        "End": "2025-06-22T01:17:43.066948Z",
        "TotalMs": 82.06,
        "Messages": {}
    },
    "MaxResults": 5,
    "ContinuationToken": "ca10f6ca-f4c2-4040-adfe-9de3a81b9f55",
    "EndOfResults": false,    // whether or not the end of the results has been reached
    "TotalRecords": 17,       // total number of matching records
    "RecordsRemaining": 12,   // records remaining should you enumerate again
    "Objects": [
        {
            "TenantGUID": "00000000-0000-0000-0000-000000000000",
            "GUID": "ebefc55b-6f74-4997-8c87-e95e40cb83d3",
            "GraphGUID": "00000000-0000-0000-0000-000000000000",
            "Name": "Active Directory",
            "CreatedUtc": "2025-06-21T05:23:14.100128Z",
            "LastUpdateUtc": "2025-06-21T05:23:14.100128Z",
            "Labels": [],
            "Tags": {},
            "Data": {
                "Name": "Active Directory"
            },
            "Vectors": []
        }, ...
    ]
}
```

### Gathering Statistics

Statistics are available both at the tenant level and at the graph level.

```csharp
Dictionary<Guid, TenantStatistics> allTenantsStats = await client.Tenant.GetStatistics();
TenantStatistics tenantStatistics = await client.Tenant.GetStatistics(myTenantGuid);

Dictionary<Guid, GraphStatistics> allGraphStatistics = await client.Graph.GetStatistics(myTenantGuid);
GraphStatistics graphStatistics = await client.Graph.GetStatistics(myTenantGuid, myGraphGuid);
```

## REST API

LiteGraph includes a project called `LiteGraph.Server` which allows you to deploy a RESTful front-end for LiteGraph.  Refer to `REST_API.md` and also the Postman collection in the root of this repository for details. For comprehensive API documentation, visit [litegraph.readme.io](https://litegraph.readme.io/). A web-based dashboard UI for managing your LiteGraph instances is available at [github.com/litegraphdb/ui](https://github.com/litegraphdb/ui).

By default, LiteGraph.Server listens on `http://localhost:8701` and is only accessible to `localhost`.  Modify the `litegraph.json` file to change settings including hostname and port.

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

