# LiteGraph Docker Deployments

Three ready-to-run deployments live here. Each is a self-contained directory: `cd` into it and run `docker compose up -d`. They publish the same host ports, so run one at a time.

| Directory | Storage | Vector search | Nodes | Use it for |
|---|---|---|---|---|
| `single-node-sqlite/` | SQLite in `./data` | HnswLite, in process | 1 | Evaluation, development, small embedded workloads |
| `single-node-postgresql/` | PostgreSQL 17 with pgvector | pgvector HNSW, in the database | 1 | Production on one server |
| `multi-node/` | PostgreSQL 17 with pgvector | pgvector HNSW, in the database | 3 behind Nginx (Switchboard optional) | High availability and horizontal read/write capacity |

The rule behind the split is simple. SQLite pairs with HnswLite and runs on one node. PostgreSQL pairs with pgvector and runs on one node or many. A cluster needs PostgreSQL because every node must see the same data, and it needs pgvector because the vector index has to live in that shared database rather than in any one process.

## Quick start

```
cd docker/single-node-postgresql
docker compose up -d
smoke.bat
```

Then open the dashboard at http://127.0.0.1:3001 and sign in with the default administrator (`default@user.com` / `password`), or call the API at http://127.0.0.1:8701 with `Authorization: Bearer litegraphadmin`. Replace `single-node-postgresql` with either of the other directories to run that deployment instead. `smoke.bat` (or `pwsh ./smoke.ps1` on Linux and macOS) checks every service, a vector write and search round trip, and the deployment-specific behavior, and exits non-zero on the first failure.

| Service | URL | Credentials |
|---|---|---|
| REST API | http://127.0.0.1:8701 | `Authorization: Bearer litegraphadmin` |
| Dashboard | http://127.0.0.1:3001 | `default@user.com` / `password` |
| MCP server | http://127.0.0.1:8702 (TCP 8703, WebSocket 8704, metrics 8705) | uses the admin token |
| Grafana | http://127.0.0.1:3000 | `admin` / `admin` |
| Prometheus | http://127.0.0.1:9090 | none |
| PostgreSQL | 127.0.0.1:15432 (single node), 127.0.0.1:15433 (cluster) | see `.env.example` |
| Switchboard (cluster, profile `switchboard`) | http://127.0.0.1:8711 | proxies the REST API |
| Clutch dashboard (cluster, profile `tools`) | http://127.0.0.1:3002 | Clutch admin |

Every host port is configurable. Copy `.env.example` to `.env` in the deployment directory and change the `LITEGRAPH_*_HOST_PORT` values; the smoke scripts read the same variables.

## The multi-node cluster

`multi-node/` runs three identical LiteGraph nodes against one PostgreSQL database, with Nginx in front on port 8701. The nodes hold no state of their own: no caches that could go stale, no in-memory vector index, no node-local settings. Any node can answer any request, so the load balancer needs no session affinity, and a node can stop at any time without losing anything.

Coordination that genuinely needs one actor at a time goes through Clutch, a distributed lock service that runs as two nodes behind its own small Nginx on the internal network. LiteGraph takes a Clutch lock to migrate the schema at startup, to build a pgvector index, and to make sure the hourly retention jobs run on one node rather than three. Ordinary reads, writes, and searches take no distributed lock, so they keep working even if Clutch is down; only those coordinated operations pause.

Redis, also on the internal network only, holds the node registry and the settings-change and restart signals. Each node writes its state to Redis every two seconds and checks whether settings were saved or a cluster restart was requested. That is what powers the dashboard's node list and the **Restart Cluster** button, which restarts the nodes one at a time while the cluster keeps serving. Redis runs without persistence: nothing in it needs to survive a Redis restart. If it is down, the nodes keep serving and only the node list and restarts wait. [docs/CLUSTERING.md](../docs/CLUSTERING.md) explains both services in full.

PostgreSQL holds two databases with two roles, created by `postgresql/init/` on first start. The `litegraph` role owns the LiteGraph database and the `clutch` role owns Clutch's. Neither can connect to the other's database, and neither is a superuser; the `postgres` superuser exists only for initialization and operators.

After `docker compose up -d`, run `smoke.bat` and then `failover.bat`. The failover script keeps traffic flowing through the load balancer while it stops and restarts a LiteGraph node, each Clutch node in turn, and Redis, then runs a rolling restart of every node. It fails if more than 2% of requests fail in any phase or if more than one node is ever out of service during the rolling restart.

Both Nginx load balancers re-resolve node names through Docker's DNS every few seconds (`resolver 127.0.0.11` and `resolve` in `nginx/*.conf`). Without that, a node recreated by `docker compose up` (for example after `update.bat` pulls a new image) comes back at a new address that Nginx never learns.

To use Switchboard instead of Nginx, start it alongside: `docker compose --profile switchboard up -d`. It listens on port 8711 and routes to the same three nodes, checking each node's readiness endpoint.

To add a fourth node, copy the `litegraph-3` service as `litegraph-4` with its own `LITEGRAPH_NODE_ID` and log directory, add it to `nginx/litegraph.conf`, `prometheus.yaml`, the `litegraph-lb` dependencies, and (if you use it) `switchboard/sb.json`, then run `docker compose up -d`.

The cluster ships with demonstration credentials and `Cluster.AllowInsecureDefaults` set to `true` so it starts with no setup. Before any real use, set every secret listed in `multi-node/.env.example`, create a dedicated Clutch access key, and set `AllowInsecureDefaults` to `false` in `multi-node/litegraph.json`. With it `false`, a node refuses to start on the default encryption key or administrator token.

## Health checks

Every LiteGraph node answers `GET /v1.0/health/live` (the process is running) and `GET /v1.0/health/ready` (the database answers and the node is not shutting down). Readiness returns 503 when either check fails. In cluster mode it also reports whether the node's Clutch lock connection is up and whether it can reach Redis; if either is not, readiness stays 200 with `Status` `Degraded`, because the node can still serve everything except coordinated work. `GET /v1.0/cluster/nodes` (administrator only) lists every node with its state. Every response also carries an `x-litegraph-node` header naming the node that answered, which is the quickest way to see load balancing at work.

## Running a build of your own

Every compose file selects the LiteGraph images through `LITEGRAPH_IMAGE_TAG`, defaulting to the release tag. To run a build you made yourself:

```
build-all.bat v10.2.0-rc1
set LITEGRAPH_IMAGE_TAG=v10.2.0-rc1
cd docker\multi-node
docker compose up -d
smoke.bat
```

Or put `LITEGRAPH_IMAGE_TAG=v10.2.0-rc1` in the deployment's `.env`. `update.bat` pulls, recreates, and lists containers using the same variable.

The build scripts push the tag you give them, and a release tag (a plain `vMAJOR.MINOR.PATCH` such as `v10.2.0`) also moves `:latest`. Any other tag, such as `v10.2.0-rc1` or a test tag, leaves `:latest` where it was, so testing a build never changes what users pulling `latest` get.

## Native AOT images

The default images run the server and MCP server on the .NET runtime (JIT). Both can instead run as Native AOT executables, with the same settings files, environment variables, ports, and behavior, in much smaller images (about 65 MB compressed for the server, against about 400 MB) that start faster. Every deployment directory has a `compose.native.yaml` override that switches the server and MCP server to `<LITEGRAPH_IMAGE_TAG>-native` images; the dashboard, PostgreSQL, Clutch, and the rest are unchanged.

Native images are not published to Docker Hub; build them from this repository (from the repository root):

```
docker build -f src/LiteGraph.Server/Dockerfile.native -t jchristn77/litegraph:v10.2.0-native src
docker build -f src/LiteGraph.McpServer/Dockerfile.native -t jchristn77/litegraph-mcp:v10.2.0-native .
cd docker/multi-node
LITEGRAPH_IMAGE_TAG=v10.2.0 docker compose -f compose.yaml -f compose.native.yaml up -d
```

The build compiles for the machine's architecture; use `docker buildx build --platform linux/amd64,linux/arm64/v8` for both. The smoke and failover scripts work the same against native images, and CI runs them against both kinds on every commit. See [Native AOT and trimming](../docs/AOT.md).

## Upgrading from LiteGraph 9.x

The 9.x `docker/compose.yaml` is now `single-node-postgresql/compose.yaml`. The Compose project is named `litegraph` rather than taking its name from the `docker` directory, so it no longer shares volume names with other projects that keep their compose files in a directory called `docker`.

To keep an existing 9.x database, back it up first, then set `LITEGRAPH_POSTGRESQL_VOLUME=docker_postgresql-data` in `single-node-postgresql/.env` before starting. PostgreSQL moves to the `pgvector/pgvector:0.8.6-pg17-trixie` image, which is `postgres:17` on the same Debian base with the pgvector extension added, so the existing data directory works unchanged. On first start LiteGraph converts stored vectors to pgvector. **There is no way back to 9.x after that conversion**, which is why the backup comes first.

PostgreSQL may log `database "litegraph" has a collation version mismatch` after the switch if your 9.x volume was created by an older `postgres:17` image built on Debian bookworm; the pinned pgvector image uses Debian trixie, like current `postgres:17`. Text indexes can order differently between the two C libraries, so if you see that warning, rebuild them once: `docker compose exec postgresql psql -U litegraph -d litegraph -c "REINDEX DATABASE litegraph; ALTER DATABASE litegraph REFRESH COLLATION VERSION;"`.

## Factory reset and updates

`factory/reset.bat` (or `factory/reset.sh`) in each deployment asks you to type `RESET`, then stops that deployment, deletes its volumes and runtime directories, and restores its configuration files from `factory/`. It touches only its own Compose project. `update.bat` is the non-destructive counterpart: it pulls the current images and recreates the containers, keeping every volume.

## Chat and local models

To reach an Ollama or other OpenAI-compatible server running on the Docker host, point chat endpoints at `http://host.docker.internal:11434`. Docker Desktop resolves that name automatically; on Linux, uncomment the `extra_hosts` line on the LiteGraph services.
