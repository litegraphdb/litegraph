# Using Claude with LiteGraph

LiteGraph exposes 145+ MCP tools that let Claude create, query, and manage graph data conversationally. This guide gets you from zero to a working setup in minutes.

## Prerequisites

- .NET 8.0 SDK or later (net8.0, net9.0, or net10.0)
- Node.js 18+ and npm (for the dashboard)
- Claude Code CLI installed (`npm install -g @anthropic-ai/claude-code`)
- Git

## Step 1: Clone the Repository

```bash
git clone https://github.com/litegraphdb/litegraph.git
cd litegraph
```

## Step 2: Build

```bash
cd src
dotnet build
```

The solution targets multiple frameworks. Ensure your .NET SDK is 8.0 or later.

## Step 3: Install MCP Configuration

This registers LiteGraph as an MCP server with Claude Code and creates an agent definition.

```bash
cd LiteGraph.McpServer/bin/Debug/net10.0
./LiteGraph.McpServer install
```

Adjust `net10.0` to match your SDK version (e.g., `net8.0`, `net9.0`).

Use `./LiteGraph.McpServer install --dry-run` to preview what will be written without making changes.

## Step 4: Start LiteGraph Server

The REST API server that the MCP server connects to.

**Linux/macOS:**
```bash
cd ../../../../LiteGraph.Server/bin/Debug/net10.0
./LiteGraph.Server -d
```

**Windows:**
```powershell
cd ..\..\..\..\LiteGraph.Server\bin\Debug\net10.0
Start-Process .\LiteGraph.Server.exe
```

`-d` detaches the process to run in the background on Linux/macOS. On Windows, use `Start-Process` or run in a separate terminal.

## Step 5: Start MCP Server

**Linux/macOS:**
```bash
cd ../../../../LiteGraph.McpServer/bin/Debug/net10.0
./LiteGraph.McpServer -d
```

**Windows:**
```powershell
cd ..\..\..\..\LiteGraph.McpServer\bin\Debug\net10.0
Start-Process .\LiteGraph.McpServer.exe
```

## Step 6: Start the Dashboard

```bash
cd ../../../../../dashboard
npm install
npm run dev
```

The dashboard starts on http://localhost:3000 by default.

## Step 7: Try It Out

Launch Claude with the LiteGraph agent:

```bash
claude --agent litegraph
```

Then give Claude a prompt like:

> Create a new tenant called 'demo-tenant', then create a graph called 'social-network' in it. Add five nodes named Alice, Bob, Charlie, Diana, and Eve. Connect every node to every other node with edges to form a full mesh. When you're done, show me the graph summary.

Claude will use the LiteGraph MCP tools to execute each operation and report back.

## Step 8: View in Dashboard

Open your browser to http://localhost:3000. The default admin bearer token is `litegraphadmin` (configured in `litegraph.json`). Check your server's `litegraph.json` for the current credentials if they have been changed.

Navigate to the graph you created to see all five nodes and their edges visualized.

## What's Available

145+ MCP tools are available across these categories:

| Category | Description |
|----------|-------------|
| `tenant/*` | Multi-tenant management |
| `graph/*` | Graph CRUD, search, export (GEXF), vector indexing |
| `node/*` | Node CRUD, search, traversal, routing |
| `edge/*` | Edge CRUD, search, traversal |
| `label/*` | Label metadata on graphs, nodes, and edges |
| `tag/*` | Tag metadata on graphs, nodes, and edges |
| `vector/*` | Vector storage and similarity search |
| `user/*` | User management |
| `credential/*` | Credential and authentication management |
| `admin/*` | Backup, restore, and flush operations |
| `batch/*` | Batch existence checks |

Run `claude --agent litegraph` and ask Claude what tools are available for a full listing.

## Troubleshooting

- **Connection refused**: Ensure both the LiteGraph REST server (port 8701) and MCP server (port 8200) are running.
- **Build errors**: Verify your .NET SDK version with `dotnet --version`. Must be 8.0+.
- **MCP not detected**: Re-run `./LiteGraph.McpServer install` and restart Claude Code.
- **Server config**: MCP server settings (ports, API key) are in `litegraph.json` next to the MCP server binary. The default LiteGraph endpoint is `http://localhost:8701` with API key `litegraphadmin`.
