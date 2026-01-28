<img src="../../assets/favicon.png" height="48">

# Python SDK for LiteGraph

[![PyPI](https://img.shields.io/pypi/v/litegraph-sdk.svg)](https://pypi.org/project/litegraph-sdk/)

This SDK is part of the [LiteGraph monorepo](../../README.md). For other language SDKs, see the [SDK overview](../README.md).

LiteGraph is a lightweight graph database with relational and vector support built using SQLite, designed to power knowledge and artificial intelligence persistence and retrieval.

## Features

- Multi-tenant support
- Graph, node, and edge management and retrieval
- Support for labels (strings), tags (key-value pairs), unstructured data (objects), and vectors in a single persistence layer
- Routing and traversal between nodes
- GEXF format export support
- Built-in retry mechanism and error handling
- Comprehensive logging system
- Access key authentication support

## Requirements

- Python 3.8 or higher

### Dependencies

- `httpx`: For making HTTP requests
- `pydantic`: For data validation and serialization
- `typing`: For type hints

## Installation

```bash
pip install litegraph_sdk
```

## Quick Start

```python
from litegraph_sdk import configure, Graph, Node, Edge
import uuid

# Configure the SDK with tenant GUID and access key
configure(
    endpoint="https://api.litegraph.com",
    tenant_guid="your-tenant-guid",
    access_key="your-access-key"
)

# Create a new graph
graph = Graph.create(
    name="My Graph",
    data={"description": "A sample graph"}
)

# Create Multiple Nodes
new_multiple_node = [
    {
        "Name": "Active Directory",
        "Data": {
            "Name": "Active Directory"
        }
    },
    {
        "Name": "Website",
        "Data": {
            "Name": "Website"
        }
    }
]
multiple_nodes = Node.create_multiple(new_multiple_node)

# Add nodes
node1 = Node.create(
    graph_guid=graph.guid,
    name="Start Node",
    data={"type": "entry_point"}
)

node2 = Node.create(
    graph_guid=graph.guid,
    name="End Node",
    data={"type": "exit_point"}
)

# Create an edge between nodes
edge = Edge.create(
    graph_guid=graph.guid,
    from_node=node1.guid,
    to_node=node2.guid,
    cost=1,
    name="Connection",
    data={"type": "direct"}
)
```

## API Endpoints Reference

### Tenant Operations

| Operation                | Method | Endpoint                           | Description                |
| ------------------------ | ------ | ---------------------------------- | -------------------------- |
| Check Tenant Exists      | HEAD   | `v1.0/tenants/{tenant_guid}`       | Check if a tenant exists    |
| Create Tenant            | PUT    | `v1.0/tenants`                   | Create a new tenant        |
| Get Tenant               | GET    | `v1.0/tenants/{tenant_guid}`       | Retrieve tenant details    |
| Update Tenant            | PUT    | `v1.0/tenants/{tenant_guid}`       | Update tenant details      |
| Delete Tenant            | DELETE | `v1.0/tenants/{tenant_guid}`       | Delete a tenant            |
| Delete Tenant Forcefully | DELETE | `v1.0/tenants/{tenant_guid}?force` | Delete a tenant forcefully |
| List Tenants             | GET    | `v1.0/tenants`                   | List all tenants           |

### User Operations

| Operation                | Method | Endpoint                           | Description                |
| ------------------------ | ------ | ---------------------------------- | -------------------------- |
| Check User Exists         | HEAD   | `v1.0/tenants/{tenant_guid}/users/{user_id}` | Check if a user exists    |
| Create User               | PUT    | `v1.0/tenants/{tenant_guid}/users` | Create a new user          |
| Get User                  | GET    | `v1.0/tenants/{tenant_guid}/users/{user_id}` | Retrieve user details    |
| Update User               | PUT    | `v1.0/tenants/{tenant_guid}/users/{user_id}` | Update user details    |
| Delete User               | DELETE | `v1.0/tenants/{tenant_guid}/users/{user_id}` | Delete user details    |
| List Users                | GET    | `v1.0/tenants/{tenant_guid}/users` | List all users           |

### Credential Operations

| Operation                | Method | Endpoint                           | Description                |
| ------------------------ | ------ | ---------------------------------- | -------------------------- |
| Check Credential Exists  | HEAD   | `v1.0/tenants/{tenant_guid}/credentials/{credential_id}` | Check if a credential exists    |
| Create Credential         | PUT    | `v1.0/tenants/{tenant_guid}/credentials` | Create a new credential    |
| Get Credential            | GET    | `v1.0/tenants/{tenant_guid}/credentials/{credential_id}` | Retrieve credential details |
| Update Credential         | PUT    | `v1.0/tenants/{tenant_guid}/credentials/{credential_id}` | Update credential details |
| Delete Credential         | DELETE | `v1.0/tenants/{tenant_guid}/credentials/{credential_id}` | Delete credential details |
| List Credentials          | GET    | `v1.0/tenants/{tenant_guid}/credentials` | List all credentials      |

### Label Operations

| Operation                | Method | Endpoint                           | Description                |
| ------------------------ | ------ | ---------------------------------- | -------------------------- |
| Check Label Exists       | HEAD   | `v1.0/tenants/{tenant_guid}/labels/{label_id}` | Check if a label exists    |
| Create Label             | PUT    | `v1.0/tenants/{tenant_guid}/labels` | Create a new label        |
| Get Label                | GET    | `v1.0/tenants/{tenant_guid}/labels/{label_id}` | Retrieve label details    |
| Update Label             | PUT    | `v1.0/tenants/{tenant_guid}/labels/{label_id}` | Update label details      |
| Delete Label             | DELETE | `v1.0/tenants/{tenant_guid}/labels/{label_id}` | Delete label details      |
| List Labels              | GET    | `v1.0/tenants/{tenant_guid}/labels` | List all labels           |

### Tag Operations

| Operation                | Method | Endpoint                           | Description                |
| ------------------------ | ------ | ---------------------------------- | -------------------------- |
| Check Tag Exists         | HEAD   | `v1.0/tenants/{tenant_guid}/tags/{tag_id}` | Check if a tag exists    |
| Create Tag               | PUT    | `v1.0/tenants/{tenant_guid}/tags` | Create a new tag        |
| Get Tag                  | GET    | `v1.0/tenants/{tenant_guid}/tags/{tag_id}` | Retrieve tag details    |
| Update Tag               | PUT    | `v1.0/tenants/{tenant_guid}/tags/{tag_id}` | Update tag details      |
| Delete Tag               | DELETE | `v1.0/tenants/{tenant_guid}/tags/{tag_id}` | Delete tag details      |
| List Tags                | GET    | `v1.0/tenants/{tenant_guid}/tags` | List all tags           |

### Graph Operations

| Operation            | Method | Endpoint                                                 | Description                 |
| -------------------- | ------ | -------------------------------------------------------- | --------------------------- |
| Create Graph         | PUT    | `v1.0/tenants/{tenant_guid}/graphs`                      | Create a new graph          |
| Get Graph            | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}`           | Retrieve a specific graph   |
| Update Graph         | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}`           | Update an existing graph    |
| Delete Graph         | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}`           | Delete a graph              |
| List Graphs          | GET    | `v1.0/tenants/{tenant_guid}/graphs`                      | Retrieve all graphs         |
| Search Graphs        | POST   | `v1.0/tenants/{tenant_guid}/graphs/search`               | Search for graphs           |
| Export GEXF          | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/export`    | Export graph in GEXF format |
| Exists Graph         | HEAD   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}`           | Check if a graph exists     |
| BatchExistence Graph | POST   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/existence` | BatchExistence              |

### Node Operations

| Operation            | Method | Endpoint                                                       | Description                   |
| -------------------- | ------ | -------------------------------------------------------------- | ----------------------------- |
| Create Node          | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes`           | Create a new node             |
| Create Multiple Node | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/multiple`  | Create new nodes              |
| Get Node             | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}` | Retrieve a specific node      |
| Update Node          | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}` | Update an existing node       |
| Delete Node          | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}` | Delete a node                 |
| Delete Multiple Node | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/multiple`  | Delete nodes                  |
| Delete All Node      | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/all`       | Delete all nodes              |
| List Nodes           | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes`           | Retrieve all nodes in a graph |
| Search Nodes         | POST   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/search`    | Search for nodes              |
| Exists Nodes         | HEAD   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}` | Check if a node exists        |

### Edge Operations

| Operation            | Method | Endpoint                                                       | Description                   |
| -------------------- | ------ | -------------------------------------------------------------- | ----------------------------- |
| Create Edge          | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges`           | Create a new edge             |
| Create Multiple Edge | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/multiple`  | Create new edges              |
| Get Edge             | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/{edge_id}` | Retrieve a specific edge      |
| Update Edge          | PUT    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/{edge_id}` | Update an existing edge       |
| Delete Edge          | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/{edge_id}` | Delete an edge                |
| Delete Multiple Edge | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/multiple`  | Delete edges                  |
| Delete All Edge      | DELETE | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/all`       | Delete all edges              |
| List Edges           | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges`           | Retrieve all edges in a graph |
| Search Edges         | POST   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/search`    | Search for edges              |
| Exists Edges         | HEAD   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/{edge_id}` | Check if an edge exists       |

### Route Operations

| Operation               | Method | Endpoint                                                                  | Description              |
| ----------------------- | ------ | ------------------------------------------------------------------------- | ------------------------ |
| Get Routes              | POST   | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/routes`                     | Find routes              |
| Get Edges from node     | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}/edges/from` | Find Edges from a node   |
| Get Edges to node       | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}/edges/to`   | Find Edges to a node     |
| Get Edges between nodes | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/edges/between?from=&to=`    | Find edges between nodes |
| Get Node Edges          | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}`            | Find Nodes Edges         |
| Get Node Neighbors      | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}/neighbors`  | Find neighbors of a node |
| Get Node Parents        | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}/parents`    | Find parents of a node   |
| Get Node Children       | GET    | `v1.0/tenants/{tenant_guid}/graphs/{graph_id}/nodes/{node_id}/children`   | Find children of a node  |

## Core Components

### Base Models

- `TenantModel`: Represents a tenant in the system
- `GraphModel`: Represents a graph container
- `NodeModel`: Represents a node in a graph
- `EdgeModel`: Represents a connection between nodes
- `RouteRequestModel`: Used for finding routes between nodes
- `RouteResultModel`: Contains route finding results
- `ExistenceRequestModel`: Used for checking the existence

### Search Capabilities

The SDK provides powerful search functionality through the `SearchRequest` class:

```python
from litegraph_sdk import Graph

# Search for graphs by name
search_request = {
    "Ordering":"CreateDescending",
    "Expr": {
        "Left": "Name",
        "Operator": "Equals",
        "Right": "My Graph"
    }
}

results = Graph.search(**search_request)
```

### Error Handling

The SDK includes comprehensive error handling with specific exception types:

- `AuthenticationError`: Authentication issues
- `ResourceNotFoundError`: Requested resource not found
- `BadRequestError`: Invalid request parameters
- `TimeoutError`: Request timeout
- `ServerError`: Server-side issues

## Logging

The SDK includes a built-in logging system that can be configured:

```python
from litegraph_sdk.sdk_logging import set_log_level, log_info

# Set logging level
set_log_level("DEBUG")

# Add log
log_info("INFO", "This is an info message")
```

## API Resource Operations

### Graphs

```python
from litegraph_sdk import Graph
from litegraph_sdk.configuration import configure
from litegraph_sdk.models.existence_request import ExistenceRequestModel
from litegraph_sdk.models.edge_between import EdgeBetweenModel

# Configure with tenant GUID and access key
configure(
    endpoint="https://api.litegraph.com",
    tenant_guid="your-tenant-guid",
    access_key="your-access-key"
)

# Create a graph
graph = Graph.create(name="New Graph")

# Retrieve a graph
graph = Graph.retrieve(graph_guid="graph-guid")

# Retrieve all graphs for tenant
graphs = Graph.retrieve_all()

# Update a graph
graph = Graph.update(graph_guid="graph-guid", name="Updated Graph")

# Delete a graph
Graph.delete(graph_guid="graph-guid")

# Export to GEXF
gexf_data = Graph.export_gexf(graph_guid="graph-guid")

# Check if Graph Exists
exists = Graph.exists(graph_guid="graph-guid")

# Search graphs in tenant
search_request = {
    "Ordering": "CreatedDescending",
    "Expr": {
        "Left": "Name",
        "Operator": "Equals",
        "Right": "My Graph"
    }
}
graph_results = Graph.search(**search_request)

# Batch Existence Check
request = ExistenceRequestModel(
    nodes=[
        "node-guid-1",
        "node-guid-2"
    ],
    edges=[
        "edge-guid-1"
    ],
    edges_between=[
        EdgeBetweenModel(
            from_="node-guid-1",
            to="node-guid-2"
        )
    ]
)
existence_results = Graph.batch_existence(graph_guid="graph-guid", request=request)
```

### Nodes

```python
from litegraph_sdk import Node
from litegraph_sdk.configuration import configure

# Configure with tenant GUID and access key
configure(
    endpoint="https://api.litegraph.com",
    tenant_guid="your-tenant-guid",
    access_key="your-access-key"
)

# Create Multiple Nodes
new_multiple_nodes = [
    {
        "Name": "Active Directory",
        "Data": {
            "Name": "Active Directory"
        }
    },
    {
        "Name": "Website",
        "Data": {
            "Name": "Website"
        }
    }
]
nodes = Node.create_multiple(graph_guid="graph-guid", nodes=new_multiple_nodes)

# Create a single node
node = Node.create(
    graph_guid="graph-guid",
    name="New Node",
    data={"type": "service"}
)

# Retrieve a node
node = Node.retrieve(graph_guid="graph-guid", node_guid="node-guid")

# Retrieve all nodes in a graph
nodes = Node.retrieve_all(graph_guid="graph-guid")

# Update a node
node = Node.update(
    graph_guid="graph-guid",
    node_guid="node-guid",
    name="Updated Node"
)

# Delete a node
Node.delete(graph_guid="graph-guid", node_guid="node-guid")

# Delete multiple nodes
Node.delete_multiple(graph_guid="graph-guid", node_guids=["node-guid-1", "node-guid-2"])

# Delete all nodes in a graph
Node.delete_all(graph_guid="graph-guid")

# Check if Node Exists
exists = Node.exists(graph_guid="graph-guid", node_guid="node-guid")

# Search nodes in a graph
search_request = {
    "Ordering": "CreatedDescending",
    "Expr": {
        "Left": "Name",
        "Operator": "Contains",
        "Right": "Service"
    }
}
node_results = Node.search(graph_guid="graph-guid", **search_request)
```

### Edges

```python
from litegraph_sdk import Edge
from litegraph_sdk.configuration import configure

# Configure with tenant GUID and access key
configure(
    endpoint="https://api.litegraph.com",
    tenant_guid="your-tenant-guid",
    access_key="your-access-key"
)

# Create Multiple Edges
new_multiple_edges = [
    {
        "Name": "Connection 1",
        "From": "node-guid-1",
        "To": "node-guid-2",
        "Cost": 1
    },
    {
        "Name": "Connection 2",
        "From": "node-guid-2",
        "To": "node-guid-3",
        "Cost": 1
    }
]
edges = Edge.create_multiple(graph_guid="graph-guid", edges=new_multiple_edges)

# Create a single edge
edge = Edge.create(
    graph_guid="graph-guid",
    from_node="node-guid-1",
    to_node="node-guid-2",
    name="Direct Connection",
    cost=1
)

# Retrieve an edge
edge = Edge.retrieve(graph_guid="graph-guid", edge_guid="edge-guid")

# Retrieve all edges in a graph
edges = Edge.retrieve_all(graph_guid="graph-guid")

# Update an edge
edge = Edge.update(
    graph_guid="graph-guid",
    edge_guid="edge-guid",
    name="Updated Connection",
    cost=2
)

# Delete an edge
Edge.delete(graph_guid="graph-guid", edge_guid="edge-guid")

# Delete multiple edges
Edge.delete_multiple(graph_guid="graph-guid", edge_guids=["edge-guid-1", "edge-guid-2"])

# Delete all edges in a graph
Edge.delete_all(graph_guid="graph-guid")

# Check if Edge Exists
exists = Edge.exists(graph_guid="graph-guid", edge_guid="edge-guid")

# Search edges in a graph
search_request = {
    "Ordering": "CreatedDescending",
    "Expr": {
        "Left": "Cost",
        "Operator": "LessThan",
        "Right": 2
    }
}
edge_results = Edge.search(graph_guid="graph-guid", **search_request)
```

## Route and Traversal

```python
from litegraph_sdk.resources.route_traversal import RouteNodes
from litegraph_sdk.configuration import configure

base = "URL"
configure(base_url,"graph_guid")

# Edges from node
get_edges_from_node = RouteNodes.get_edges_from("graph_guid","node_guid")

# Edges to node
get_edges_to_node = RouteNodes.get_edges_to("graph_guid","node_guid")

# Specific Edge
specific_edge = RouteNodes.edges("graph_guid","node_guid")

# Find parent of a Node
parent_node = RouteNodes.parents("graph_guid","node_guid")

# Find children of a Node
children_node = RouteNodes.children("graph_guid","node_guid")

# Find neighbors of a Node
neighbors_node = RouteNodes.neighbors("graph_guid","node_guid")



# Find Edges in between of a Node
from litegraph_sdk.resources.routes_between import RouteEdges
between_nodes = RouteEdges.between("graph_guid","node_guid(from)","node_guid(to)")

# Find Routes
from litegraph_sdk.resources.routes import Routes
routes_data = {
    "Graph": "graph_guid",
    "From": "node_guid",
    "To": "node_guid",
    "NodeFilter":{
       "GraphGUID": "graph_guid",
        "Ordering": "CreatedDescending",
        "Expr": {
            "Left": "Hello",
            "Operator": "GreaterThan",
            "Right": "World"
        }
     }
}
routes = Routes.routes("graph_guid",**routes_data)
```

## Advanced Configuration

The SDK client can be configured with custom settings:

```python
from litegraph_sdk import configure

configure(
    endpoint="https://api.litegraph.com",
    tenant_guid="your-tenant-guid",    # Required for multi-tenant support
    access_key="your-access-key",      # Required for authentication
    timeout=30,                        # Optional: 30 seconds timeout
    retries=5                         # Optional: 5 retry attempts
)
```

### Tenant Management

```python
from litegraph_sdk import Tenant
from litegraph_sdk.configuration import configure

# Configure with admin access
configure(endpoint="https://api.litegraph.com", access_key="admin-access-key")

# Create a new tenant
tenant = Tenant.create(name="New Tenant")

# Retrieve tenant details
tenant = Tenant.retrieve(tenant_guid="tenant-guid")

# Update tenant
tenant = Tenant.update(tenant_guid="tenant-guid", name="Updated Tenant")

# Delete tenant
Tenant.delete(tenant_guid="tenant-guid")

# List all tenants
tenants = Tenant.retrieve_all()
```

## Development

### Setting up Pre-commit Hooks

This project uses pre-commit hooks to ensure code quality. To set up pre-commit:

```bash
# Install pre-commit
pip install pre-commit

# Install the pre-commit hooks
pre-commit install

# (Optional) Run pre-commit on all files
pre-commit run --all-files
```

The pre-commit hooks will run automatically on `git commit`. They help maintain:

- Code formatting (using ruff)
- Import sorting
- Code quality checks
- And other project-specific checks

### Running Tests

The project uses `tox` for running tests in isolated environments. Make sure you have tox installed:

```bash
pip install tox
```

To run the default test environment:

```bash
tox
```

To run specific test environments:

```bash
# Run only the tests
tox -e default

# Run tests with coverage report
tox -- --cov litegraph_sdk --cov-report term-missing

# Build documentation
tox -e docs

# Build the package
tox -e build

# Clean build artifacts
tox -e clean
```

### Development Installation

For development, you can install the package with all test dependencies:

```bash
pip install -e ".[testing]"
```

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details

## Feedback and Issues

Have feedback or found an issue? Please file an issue in our GitHub repository.

## Version History

Please refer to [CHANGELOG.md](CHANGELOG.md) for a detailed version history.
