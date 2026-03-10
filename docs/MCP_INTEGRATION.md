# MCP Integration Guide

This guide explains how to integrate LiteGraph with AI clients using the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/).

## Overview

### What is MCP?

The Model Context Protocol (MCP) is an open standard that allows AI assistants (Claude Code, Claude Desktop, Cursor, etc.) to interact with external tools and data sources through a unified interface. LiteGraph exposes its full graph database API as MCP tools, enabling AI assistants to create, query, and manage graph data conversationally.

### Why MCP?

By exposing LiteGraph operations as MCP tools, AI assistants can:

- Build and query knowledge graphs through natural language
- Persist structured relationships between entities
- Perform vector similarity searches over embeddings
- Manage multi-tenant graph data without writing code

### Available Tool Categories

LiteGraph registers **145+ MCP tools** across the following categories:

| Category | Description |
|----------|-------------|
| `admin/*` | Backup, restore, and flush operations |
| `tenant/*` | Multi-tenant management |
| `graph/*` | Graph CRUD, search, and export |
| `node/*` | Node CRUD, search, and routing |
| `edge/*` | Edge CRUD, search, and traversal |
| `label/*` | Label metadata on nodes and edges |
| `tag/*` | Tag metadata on nodes and edges |
| `vector/*` | Vector storage and similarity search |
| `user/*` | User management |
| `userauthentication/*` | Authentication operations |
| `credential/*` | Credential management |
| `batch/*` | Batch existence checks |

---

## Recommended Installation: HTTP Transport

The recommended approach uses HTTP transport, where LiteGraph.McpServer runs as a standalone process and AI clients connect over HTTP.

### Quick Start

1. **Start the MCP server:**

   ```bash
   dotnet run --project src/LiteGraph.McpServer/LiteGraph.McpServer.csproj
   ```

   The server listens on three transports by default:
   - **HTTP** — `http://localhost:8200/rpc` (primary, recommended)
   - **TCP** — `localhost:8201`
   - **WebSocket** — `ws://localhost:8202/mcp`

2. **Run the install command** to auto-configure Claude Code:

   ```bash
   dotnet run --project src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -- install
   ```

   This will:
   - Read `~/.claude.json` (or create it if missing)
   - Add/update an `mcpServers` entry for `litegraph`
   - Create an agent definition at `~/.claude/agents/litegraph.md`
   - Print a summary of changes

   Use `--dry-run` to preview changes without writing:

   ```bash
   dotnet run --project src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -- install --dry-run
   ```

### What the Installer Writes

#### ~/.claude.json

The installer adds or updates the following entry:

```json
{
  "mcpServers": {
    "litegraph": {
      "type": "http",
      "url": "http://localhost:8200/rpc"
    }
  }
}
```

#### ~/.claude/agents/litegraph.md

The installer creates an agent definition file with the following content:

```markdown
---
name: litegraph
description: LiteGraph knowledge graph database agent — create, query, and manage graph data including nodes, edges, labels, tags, and vector embeddings.
allowedTools:
  - mcp__litegraph__*
---

You are a LiteGraph graph database assistant. Use the LiteGraph MCP tools to help users manage their graph data.

## Key Concepts

- **Tenant**: Top-level isolation boundary. All operations require a tenant GUID.
- **Graph**: A container for nodes and edges within a tenant.
- **Node**: An entity in the graph. Can have labels, tags, vectors, and arbitrary JSON data.
- **Edge**: A directed relationship between two nodes with optional cost, labels, tags, and data.
- **Vector**: A multi-dimensional embedding attached to a node for similarity search.
- **Label**: A string annotation on a node or edge.
- **Tag**: A key-value metadata pair on a node or edge.

## Workflow

1. Ensure a tenant exists (use `tenant/all` to list, `tenant/create` to create).
2. Create or select a graph within the tenant.
3. Create nodes and edges to model relationships.
4. Attach labels, tags, and vectors as needed.
5. Use search tools to query the graph.
6. Use `node/routes` to find paths between nodes.

## Guidelines

- Always confirm the tenant and graph GUIDs before performing operations.
- Use `search` tools with expression filters for targeted queries.
- Prefer batch operations (`node/createmany`, `edge/createmany`) for bulk data.
- Use vector search (`vector/search`) for semantic similarity queries.
- Check graph export (`graph/export`) for full graph snapshots.
```

### Implementation Plan for the `install` Command

The `install` command is implemented directly in the existing `LiteGraph.McpServer` executable — no separate CLI project is needed.

- [ ] Add `install` argument detection in `ParseArguments()` alongside existing `--config`, `--showconfig`, and `--help`
- [ ] Implement `RunInstall(bool dryRun)` method in `LiteGraphMcpServer.cs`
- [ ] Read and parse `~/.claude.json` (create with default structure if missing)
- [ ] Merge `mcpServers.litegraph` entry with `type: "http"` and `url` from current HTTP settings
- [ ] Create `~/.claude/agents/` directory if missing
- [ ] Write `litegraph.md` agent definition file
- [ ] Support `--dry-run` flag: print what would be written without modifying files
- [ ] Print configuration snippets for Claude Desktop, Cursor, and other clients
- [ ] Exit after install completes (do not start the MCP server)

### Configuration Snippets for Other Clients

After running `install`, the command also prints configuration for other MCP-compatible clients:

**Claude Desktop** (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "litegraph": {
      "url": "http://localhost:8200/rpc"
    }
  }
}
```

**Cursor** (`.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "litegraph": {
      "url": "http://localhost:8200/rpc"
    }
  }
}
```

---

## Alternative: Stdio Transport

Stdio transport is available for scenarios where you prefer the AI client to spawn LiteGraph.McpServer as a subprocess rather than connecting to a running server.

### How Stdio Works

With stdio transport, the AI client (e.g., Claude Code) launches `LiteGraph.McpServer` as a child process and communicates over stdin/stdout using JSON-RPC. The server starts, handles requests, and terminates when the client disconnects.

### ~/.claude.json Entry for Stdio

```json
{
  "mcpServers": {
    "litegraph": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/LiteGraph.McpServer", "--", "--stdio"]
    }
  }
}
```

Replace `path/to/LiteGraph.McpServer` with the absolute path to the `LiteGraph.McpServer` project directory.

### Trade-offs: HTTP vs Stdio

| Aspect | HTTP (Recommended) | Stdio |
|--------|-------------------|-------|
| **Startup time** | Server already running; instant connection | Cold start on each session (~2-5s for `dotnet run`) |
| **Multi-client** | Multiple AI clients share one server | One server instance per client |
| **Shared state** | All clients see the same data and caches | Each instance has independent state |
| **Server management** | Must start/stop server separately | Client manages process lifecycle |
| **Network** | Requires open port (default 8200) | No network; uses stdin/stdout |
| **Debugging** | Logs visible in server console | Logs may be hidden by client |

**Recommendation:** Use HTTP transport for most scenarios. Stdio is suitable for single-user development where you want zero server management overhead and don't need multi-client access.

---

## Manual Configuration

If you prefer to configure MCP clients manually rather than using the `install` command, follow the steps below.

### Claude Code

1. Open or create `~/.claude.json`
2. Add the `mcpServers` entry:

   ```json
   {
     "mcpServers": {
       "litegraph": {
         "type": "http",
         "url": "http://localhost:8200/rpc"
       }
     }
   }
   ```

3. Optionally create an agent at `~/.claude/agents/litegraph.md` (see content in the [Recommended Installation](#what-the-installer-writes) section above)

4. Restart Claude Code to pick up the new configuration

### Claude Desktop

1. Open Settings > Developer > Edit Config
2. Add to `claude_desktop_config.json`:

   ```json
   {
     "mcpServers": {
       "litegraph": {
         "url": "http://localhost:8200/rpc"
       }
     }
   }
   ```

3. Restart Claude Desktop

### Cursor

1. Create or edit `.cursor/mcp.json` in your project root (or `~/.cursor/mcp.json` for global config):

   ```json
   {
     "mcpServers": {
       "litegraph": {
         "url": "http://localhost:8200/rpc"
       }
     }
   }
   ```

2. Restart Cursor

### Other MCP-Compatible Clients

Any client that supports the MCP specification can connect to LiteGraph. Configure it to point to `http://localhost:8200/rpc` using HTTP transport. Refer to your client's documentation for the specific configuration format.

---

## Available MCP Tools Reference

### Admin Tools

| Tool | Description |
|------|-------------|
| `admin/backup` | Create a backup of the database |
| `admin/backups` | List all available backups |
| `admin/backupread` | Read/download a specific backup |
| `admin/backupexists` | Check if a specific backup exists |
| `admin/backupdelete` | Delete a specific backup |
| `admin/flush` | Flush in-memory changes to disk |

### Tenant Tools

| Tool | Description |
|------|-------------|
| `tenant/create` | Create a new tenant |
| `tenant/get` | Get tenant by GUID |
| `tenant/all` | List all tenants |
| `tenant/update` | Update tenant properties |
| `tenant/delete` | Delete a tenant and all its data |
| `tenant/exists` | Check if a tenant exists |
| `tenant/enumerate` | Enumerate tenants with pagination |
| `tenant/search` | Search tenants with expression filters |
| `tenant/count` | Count total tenants |
| `tenant/readmany` | Read multiple tenants by GUIDs |

### Graph Tools

| Tool | Description |
|------|-------------|
| `graph/create` | Create a new graph in a tenant |
| `graph/get` | Get graph by GUID |
| `graph/all` | List all graphs in a tenant |
| `graph/update` | Update graph properties |
| `graph/delete` | Delete a graph and all its contents |
| `graph/exists` | Check if a graph exists |
| `graph/enumerate` | Enumerate graphs with pagination |
| `graph/search` | Search graphs with expression filters |
| `graph/count` | Count graphs in a tenant |
| `graph/readmany` | Read multiple graphs by GUIDs |
| `graph/export` | Export full graph (nodes, edges, metadata) |
| `graph/exportjson` | Export graph as JSON |
| `graph/exportgexf` | Export graph in GEXF format |
| `graph/batchexistence` | Check existence of multiple graphs |
| `graph/createmany` | Create multiple graphs |
| `graph/deletemany` | Delete multiple graphs |
| `graph/labelscount` | Count distinct labels in a graph |
| `graph/tagscount` | Count distinct tags in a graph |
| `graph/nodescount` | Count nodes in a graph |
| `graph/edgescount` | Count edges in a graph |
| `graph/vectorscount` | Count vectors in a graph |

### Node Tools

| Tool | Description |
|------|-------------|
| `node/create` | Create a new node |
| `node/get` | Get node by GUID |
| `node/all` | List all nodes in a graph |
| `node/update` | Update node properties |
| `node/delete` | Delete a node |
| `node/exists` | Check if a node exists |
| `node/enumerate` | Enumerate nodes with pagination |
| `node/search` | Search nodes with expression filters |
| `node/count` | Count nodes in a graph |
| `node/readmany` | Read multiple nodes by GUIDs |
| `node/createmany` | Create multiple nodes |
| `node/deletemany` | Delete multiple nodes |
| `node/batchexistence` | Check existence of multiple nodes |
| `node/edges` | Get all edges connected to a node |
| `node/edgesfrom` | Get edges originating from a node |
| `node/edgesto` | Get edges targeting a node |
| `node/neighbors` | Get neighboring nodes |
| `node/neighborsfrom` | Get neighbors via outgoing edges |
| `node/neighborsto` | Get neighbors via incoming edges |
| `node/routes` | Find routes/paths between two nodes |
| `node/labels` | Get labels on a node |
| `node/tags` | Get tags on a node |

### Edge Tools

| Tool | Description |
|------|-------------|
| `edge/create` | Create a new edge between nodes |
| `edge/get` | Get edge by GUID |
| `edge/all` | List all edges in a graph |
| `edge/update` | Update edge properties |
| `edge/delete` | Delete an edge |
| `edge/exists` | Check if an edge exists |
| `edge/enumerate` | Enumerate edges with pagination |
| `edge/search` | Search edges with expression filters |
| `edge/count` | Count edges in a graph |
| `edge/readmany` | Read multiple edges by GUIDs |
| `edge/createmany` | Create multiple edges |
| `edge/deletemany` | Delete multiple edges |
| `edge/batchexistence` | Check existence of multiple edges |
| `edge/edgesbetween` | Find edges between two specific nodes |
| `edge/labels` | Get labels on an edge |
| `edge/tags` | Get tags on an edge |
| `edge/from` | Get edges by source node |
| `edge/to` | Get edges by target node |
| `edge/nodesfrom` | Get source nodes for edges |
| `edge/nodesto` | Get target nodes for edges |
| `edge/neighbors` | Get neighboring edges |
| `edge/routes` | Find edge routes between nodes |

### Label Tools

| Tool | Description |
|------|-------------|
| `label/create` | Create a label on a node or edge |
| `label/get` | Get label by GUID |
| `label/readmany` | Read multiple labels by GUIDs |
| `label/delete` | Delete a label |
| `label/exists` | Check if a label exists |
| `label/enumerate` | Enumerate labels with pagination |
| `label/search` | Search labels with expression filters |
| `label/count` | Count labels |
| `label/batchexistence` | Check existence of multiple labels |
| `label/createmany` | Create multiple labels |
| `label/deletemany` | Delete multiple labels |
| `label/allonnode` | Get all labels on a specific node |
| `label/allonedge` | Get all labels on a specific edge |
| `label/allingraph` | Get all labels in a graph |
| `label/allintenant` | Get all labels in a tenant |
| `label/countonnode` | Count labels on a node |
| `label/countonedge` | Count labels on an edge |
| `label/countingraph` | Count labels in a graph |
| `label/countintenant` | Count labels in a tenant |
| `label/nodeshaving` | Find nodes with a specific label |

### Tag Tools

| Tool | Description |
|------|-------------|
| `tag/create` | Create a tag on a node or edge |
| `tag/get` | Get tag by GUID |
| `tag/readmany` | Read multiple tags by GUIDs |
| `tag/delete` | Delete a tag |
| `tag/exists` | Check if a tag exists |
| `tag/enumerate` | Enumerate tags with pagination |
| `tag/search` | Search tags with expression filters |
| `tag/count` | Count tags |
| `tag/batchexistence` | Check existence of multiple tags |
| `tag/createmany` | Create multiple tags |
| `tag/deletemany` | Delete multiple tags |
| `tag/allonnode` | Get all tags on a specific node |
| `tag/allonedge` | Get all tags on a specific edge |
| `tag/allingraph` | Get all tags in a graph |
| `tag/allintenant` | Get all tags in a tenant |
| `tag/countonnode` | Count tags on a node |
| `tag/countonedge` | Count tags on an edge |
| `tag/countingraph` | Count tags in a graph |
| `tag/countintenant` | Count tags in a tenant |
| `tag/nodeshaving` | Find nodes with a specific tag |

### Vector Tools

| Tool | Description |
|------|-------------|
| `vector/create` | Store a vector embedding on a node |
| `vector/get` | Get vector by GUID |
| `vector/all` | List all vectors on a node |
| `vector/readallintenant` | Read all vectors in a tenant |
| `vector/readallingraph` | Read all vectors in a graph |
| `vector/readmanygraph` | Read multiple vectors in a graph |
| `vector/update` | Update a vector |
| `vector/delete` | Delete a vector |
| `vector/exists` | Check if a vector exists |
| `vector/enumerate` | Enumerate vectors with pagination |
| `vector/search` | Similarity search across vectors |
| `vector/count` | Count vectors |
| `vector/readmany` | Read multiple vectors by GUIDs |
| `vector/createmany` | Create multiple vectors |
| `vector/deletemany` | Delete multiple vectors |
| `vector/batchexistence` | Check existence of multiple vectors |
| `vector/countintenant` | Count vectors in a tenant |
| `vector/countingraph` | Count vectors in a graph |
| `vector/countonnode` | Count vectors on a node |
| `vector/allingraph` | Get all vectors in a graph |
| `vector/allintenant` | Get all vectors in a tenant |

### User Tools

| Tool | Description |
|------|-------------|
| `user/create` | Create a new user |
| `user/get` | Get user by GUID |
| `user/all` | List all users |
| `user/update` | Update user properties |
| `user/delete` | Delete a user |
| `user/enumerate` | Enumerate users with pagination |
| `user/exists` | Check if a user exists |
| `user/readmany` | Read multiple users by GUIDs |

### Authentication & Credential Tools

| Tool | Description |
|------|-------------|
| `userauthentication/*` | User authentication operations |
| `credential/*` | Credential management (create, read, update, delete, enumerate) |
| `batch/existence` | Batch existence checking across entity types |

---

## Server Configuration

### litegraph.json

The MCP server reads its configuration from `litegraph.json` in the working directory. Key sections:

```json
{
  "Node": {
    "Name": "LiteGraph MCP Server",
    "Description": "MCP server for LiteGraph graph database"
  },
  "LiteGraph": {
    "Endpoint": "http://localhost:8301/",
    "ApiKey": "litegraph"
  },
  "Http": {
    "Hostname": "localhost",
    "Port": 8200
  },
  "Tcp": {
    "Address": "127.0.0.1",
    "Port": 8201
  },
  "WebSocket": {
    "Hostname": "localhost",
    "Port": 8202
  }
}
```

### Environment Variable Overrides

Settings can be overridden via environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `LITEGRAPH_ENDPOINT` | LiteGraph server URL | `http://localhost:8301/` |
| `LITEGRAPH_API_KEY` | API key for authentication | `litegraph` |
| `MCP_HTTP_HOSTNAME` | HTTP listener hostname | `localhost` |
| `MCP_HTTP_PORT` | HTTP listener port | `8200` |
| `MCP_TCP_ADDRESS` | TCP listener address | `127.0.0.1` |
| `MCP_TCP_PORT` | TCP listener port | `8201` |
| `MCP_WS_HOSTNAME` | WebSocket listener hostname | `localhost` |
| `MCP_WS_PORT` | WebSocket listener port | `8202` |
| `MCP_CONSOLE_LOGGING` | Enable console logging | `false` |

---

## Troubleshooting

### Server Not Running

**Symptom:** AI client reports "connection refused" or "server unavailable."

**Fix:**
1. Start the MCP server: `dotnet run --project src/LiteGraph.McpServer/LiteGraph.McpServer.csproj`
2. Verify it's listening: `curl http://localhost:8200/rpc` (should return a JSON-RPC error, not a connection error)
3. Check console output for startup errors

### Port Conflicts

**Symptom:** Server fails to start with "address already in use."

**Fix:**
1. Check what's using the port: `netstat -ano | findstr :8200` (Windows) or `lsof -i :8200` (macOS/Linux)
2. Change the port via environment variable: `MCP_HTTP_PORT=8210 dotnet run --project src/LiteGraph.McpServer/LiteGraph.McpServer.csproj`
3. Or edit `litegraph.json` to use a different port
4. Update your `~/.claude.json` URL to match the new port

### LiteGraph Server Not Reachable

**Symptom:** MCP tools return errors about failing to connect to the LiteGraph backend.

**Fix:**
1. Ensure the LiteGraph server is running: `dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj`
2. Check the endpoint in `litegraph.json` matches the LiteGraph server address
3. Verify the API key is correct

### Tools Not Appearing in Client

**Symptom:** AI client connects but no LiteGraph tools are listed.

**Fix:**
1. Verify `~/.claude.json` has the correct `mcpServers` entry
2. Restart the AI client after configuration changes
3. Check the server name in your config matches (e.g., `litegraph`)
4. Enable console logging (`MCP_CONSOLE_LOGGING=true`) and check for registration errors

### Authentication Errors

**Symptom:** Tools return 401/403 or authentication-related errors.

**Fix:**
1. Check that `LITEGRAPH_API_KEY` or the `ApiKey` in `litegraph.json` matches the LiteGraph server's expected key
2. Verify the LiteGraph server has authentication enabled and the key is valid

### Slow Tool Responses

**Symptom:** MCP tool calls take a long time to return.

**Fix:**
1. If using stdio transport, consider switching to HTTP for persistent connections
2. For vector search operations, ensure HNSW indexing is enabled on graphs with large vector datasets
3. Check LiteGraph server performance and database size
