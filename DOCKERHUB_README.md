<p align="center">
  <img src="https://raw.githubusercontent.com/litegraphdb/litegraph/main/assets/favicon.png" alt="LiteGraph" width="160" height="160" />
</p>

# LiteGraph

LiteGraph is a property graph database for applications that need graph relationships, labels, tags, JSON data, and vector search in one persistence layer. Run it as a REST server, manage it from the dashboard, drive it from the C#, Python, and JavaScript SDKs, or let AI agents work with it through the Model Context Protocol (MCP). It runs as a single node on SQLite or PostgreSQL, or as a cluster of identical nodes behind a load balancer on PostgreSQL with pgvector.

## Images

| Image | What it is | Ports |
|---|---|---|
| `jchristn77/litegraph:v10.2.0` | REST server, including chat, graph algorithms, the query language, and health endpoints | 8701 |
| `jchristn77/litegraph-mcp:v10.2.0` | MCP server (HTTP, TCP, and WebSocket transports), a thin layer over the REST API | 8702, 8703, 8704, metrics 8705 |
| `jchristn77/litegraph-ui:v10.2.0` | Next.js dashboard | 3000 |

Every image includes `curl` for health checks. Pin an exact version tag in production.

## Use cases

- **Knowledge graphs for AI applications.** Store entities and relationships with their embeddings, search by vector similarity, then traverse the graph from the results.
- **Chat over your data.** Point LiteGraph at an OpenAI-compatible, Ollama, Anthropic, or Gemini model (with embeddings from those or VoyageAI) and ask questions answered through graph tool calls and vector retrieval, streamed over server-sent events.
- **Agent memory and tools.** Give Claude, Cursor, or any MCP client a graph database as a set of tools.
- **Graph analytics.** Run PageRank, centrality, connected components, and community detection with results written back to the graph.

## Architecture

The REST server is the only component that touches storage; the MCP server and dashboard call it over HTTP.

- **Single node, SQLite.** One server with a SQLite file and an in-process HNSW vector index. Simple, fast, and ideal for evaluation and embedded workloads.
- **Single node, PostgreSQL.** One server on PostgreSQL with the pgvector extension; vector search and its HNSW index live in the database.
- **Cluster, PostgreSQL.** Several identical server nodes share one PostgreSQL database behind a load balancer. Nodes keep no state of their own, so any node can answer any request and a node can stop at any time without losing anything. [Clutch](https://hub.docker.com/r/jchristn77/clutch-server) provides the distributed locks that let one node at a time migrate the schema, build a vector index, run a background job, or take its turn in a rolling restart. Redis carries the node registry and the signals that tell every node about a settings change or a restart request. Reads, writes, and searches need neither Clutch nor Redis.

Every node answers `GET /v1.0/health/live` and `GET /v1.0/health/ready`; point load balancer and container health checks at readiness. Every response carries an `x-litegraph-node` header naming the node that answered.

## Getting started

The repository's `docker/` directory holds three ready-to-run Compose deployments, each with the MCP server, the dashboard, Prometheus, Loki, and Grafana:

```bash
git clone https://github.com/litegraphdb/litegraph
cd litegraph/docker/single-node-postgresql     # or single-node-sqlite, or multi-node
docker compose up -d
```

Then open the dashboard at http://127.0.0.1:3001 and sign in with `default@user.com` / `password`, or call the API at http://127.0.0.1:8701 with `Authorization: Bearer litegraphadmin`. Grafana is at http://127.0.0.1:3000 (`admin` / `admin`). Each directory has a `smoke.bat` / `smoke.ps1` that checks every service, and the cluster adds `failover.bat`.

The cluster deployment (`multi-node`) runs three LiteGraph nodes behind Nginx (Switchboard optional), two Clutch nodes, Redis, and PostgreSQL with pgvector. Its dashboard has a **Cluster** page for watching nodes and running rolling restarts, and Grafana ships a **LiteGraph Cluster** dashboard.

To run the server image on its own against an existing PostgreSQL database with pgvector:

```bash
docker run -d --name litegraph -p 8701:8701 \
  -e LITEGRAPH_DB_TYPE=Postgresql \
  -e LITEGRAPH_DB_HOST=your-postgres-host \
  -e LITEGRAPH_DB_NAME=litegraph \
  -e LITEGRAPH_DB_USERNAME=litegraph \
  -e LITEGRAPH_DB_PASSWORD=your-password \
  -e LITEGRAPH_ADMIN_BEARER_TOKEN=choose-a-long-random-token \
  -e LITEGRAPH_ENCRYPTION_KEY=64-hex-characters \
  -e LITEGRAPH_ENCRYPTION_IV=32-hex-characters \
  -e LITEGRAPH_CREATE_DEFAULT_RECORDS=true \
  jchristn77/litegraph:v10.2.0
```

## Configuration

The server reads `/app/litegraph.json` and lets environment variables override it, which keeps secrets out of the file and lets cluster nodes share one file while differing in identity:

| Variable | Purpose |
|---|---|
| `LITEGRAPH_DB_TYPE`, `LITEGRAPH_DB_HOST`, `LITEGRAPH_DB_PORT`, `LITEGRAPH_DB_NAME`, `LITEGRAPH_DB_USERNAME`, `LITEGRAPH_DB_PASSWORD`, `LITEGRAPH_DB_SCHEMA` | Database connection (`Sqlite` or `Postgresql`) |
| `LITEGRAPH_ADMIN_BEARER_TOKEN`, `LITEGRAPH_ENCRYPTION_KEY`, `LITEGRAPH_ENCRYPTION_IV` | Administrator token and the key used for security tokens; identical on every node of a cluster |
| `LITEGRAPH_CLUSTER_ENABLE`, `LITEGRAPH_CLUSTER_NAME`, `LITEGRAPH_NODE_ID` | Cluster mode and node identity |
| `LITEGRAPH_CLUTCH_ENDPOINT`, `LITEGRAPH_CLUTCH_ACCESS_KEY` | Clutch lock service (cluster mode) |
| `LITEGRAPH_REDIS_CONNECTION_STRING` | Redis node registry (cluster mode) |
| `LITEGRAPH_TRUSTED_PROXIES` | Load balancer addresses or CIDR ranges whose `X-Forwarded-For` is trusted for client addresses |
| `LITEGRAPH_PORT` | REST port (default 8701) |

A cluster node refuses to start with the default encryption key or administrator token unless `Cluster.AllowInsecureDefaults` is set, which the demonstration Compose deployment does.

## Upgrading from 9.x

PostgreSQL deployments now require the pgvector extension, and existing vectors are converted to pgvector on first start with no way back to 9.x. Back up first; the [upgrade guide](https://github.com/litegraphdb/litegraph/blob/main/docs/UPGRADE.md) walks through it. SQLite deployments upgrade in place.

## Documentation

- [README and quick start](https://github.com/litegraphdb/litegraph)
- [Docker deployments](https://github.com/litegraphdb/litegraph/blob/main/docker/README.md)
- [Clustering](https://github.com/litegraphdb/litegraph/blob/main/docs/CLUSTERING.md)
- [REST API](https://github.com/litegraphdb/litegraph/blob/main/docs/REST_API.md) and [MCP API](https://github.com/litegraphdb/litegraph/blob/main/docs/MCP_API.md)
- [Settings](https://github.com/litegraphdb/litegraph/blob/main/docs/SETTINGS.md) and [Observability](https://github.com/litegraphdb/litegraph/blob/main/docs/OBSERVABILITY.md)

## License

MIT.
