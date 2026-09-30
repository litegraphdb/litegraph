# Clustering

LiteGraph 10 can run as several identical server nodes behind a load balancer, all sharing one PostgreSQL database. This document explains how that works, what it needs, and what happens when parts of it fail. To try it, start the [`docker/multi-node`](../docker/multi-node/) deployment; [`docker/README.md`](../docker/README.md) walks through it.

## The idea: nodes keep nothing of their own

The whole design follows from one rule. PostgreSQL is the only place state lives, so a node holds nothing that another node could disagree with.

That rule is why a cluster needs no replication protocol, no cache invalidation messages, and no cooperation from clients. Every request can go to any node, and the answer is the same because every node asks the same database. A load balancer can spread requests however it likes, a node can be stopped at any moment, and a new node can join by starting up. Plain REST callers, SDKs, the dashboard, and the MCP server all get consistent results without doing anything special.

Earlier releases kept three kinds of state in each process, and v10 removes all three in cluster mode:

- **The vector index.** On PostgreSQL, vectors are stored in a pgvector column and searched with a pgvector HNSW index inside the database. There is no per-process index to fall behind.
- **Object caches.** The tenant, graph, node, and edge caches answered "does this exist?" checks, and learned about deletes only when the delete came through the same process. Cluster nodes turn them off and read through to the database.
- **Authorization caches.** Effective permissions were cached and invalidated only by changes made through the same process, so a revoked permission could stay in force on another node. Cluster nodes resolve permissions from the database on every request.

A few indexed reads per request is the price. For a service already reading credentials from the database on every call, it is a small one.

## Coordination through Clutch

Some work genuinely has to happen on one node at a time: migrating the schema at startup, building a pgvector index, and running the hourly retention jobs that trim chat history and request history. For those, LiteGraph takes a lock in [Clutch](https://github.com/jchristn/clutch), a distributed lock service backed by the same PostgreSQL server.

| Lock | Held while | Effect of waiting or losing it |
|---|---|---|
| `schema` | a node initializes or migrates the schema | nodes starting together migrate one at a time; the rest wait, then find the work done |
| `vectorindex/cosine/<dimensions>` | a pgvector index is created, rebuilt, or dropped | two nodes never build the same index at once |
| `job/chat-retention`, `job/request-history-purge` | an hourly retention pass runs | exactly one node runs each pass; the others skip it |

Keys are prefixed with `litegraph/<ClusterName>/`, so several clusters can share one Clutch deployment. Each node holds one WebSocket lock connection to Clutch through `Clutch.Sdk`, and every lock it takes rides on that connection. Leases (`Cluster.Clutch.LeaseMs`, 30 seconds by default) are renewed in the background over the connection. If the connection drops, Clutch releases every lock it held, and the node treats those locks as lost, stops the work they were protecting, and reconnects on its own. If a node dies holding a lock, Clutch releases it when the connection drops or the lease expires.

Just as important is what takes no lock: reads, writes, searches, graph transactions, and chat. None of them depend on Clutch being reachable.

## What a cluster needs

- **PostgreSQL with pgvector**, reachable from every node. SQLite cannot be shared between processes, so the server refuses to start in cluster mode on SQLite. Every node points at the same database and schema.
- **Clutch**, reachable from every node. The Docker deployment runs two Clutch nodes behind a small Nginx so Clutch itself has no single point of failure. Clutch keeps its data in its own database on the same PostgreSQL server, owned by its own role.
- **One settings file for all nodes**, with node identity (`LITEGRAPH_NODE_ID`) supplied per node through the environment.
- **The same encryption key and IV on every node.** Security tokens are encrypted by one node and decrypted by whichever node receives the next request.
- **A load balancer.** No session affinity is needed. The Docker deployment uses Nginx with least-connections balancing, and Switchboard as an alternative.

Turn it on with `Cluster.Enable = true` (or `LITEGRAPH_CLUSTER_ENABLE=true`) plus the Clutch endpoint and access key; [SETTINGS.md](SETTINGS.md#the-cluster-block-v100) lists every field. A cluster node refuses to start with the default encryption key or administrator token unless `Cluster.AllowInsecureDefaults` is set, which is meant only for demonstrations.

## Health checks and load balancers

Each node answers:

- `GET /v1.0/health/live`: 200 while the process is running.
- `GET /v1.0/health/ready`: 200 when the database answers and the node is not shutting down; 503 otherwise, with a body listing each check. A cluster node whose Clutch connection is down still answers 200, with `Status` `Degraded` and `Checks.Clutch` false, because it can still serve reads, writes, and searches; taking every node out of rotation would turn a Clutch outage into a full outage.

Point the load balancer's health checks at the readiness endpoint. Every response also carries `x-litegraph-node`, which is the fastest way to see which node answered a request.

The Nginx configuration in `docker/multi-node/nginx/litegraph.conf` shows the settings that matter for LiteGraph:

- Response buffering off and a long read timeout, so streamed chat responses flow through.
- A generous request body limit, for imports.
- Retries on connection errors and gateway errors. Nginx does not retry POST requests unless told to, and it should not be told to.

Open-source Nginx detects failed nodes passively, when requests to them fail. Switchboard, the alternative load balancer, probes each node's readiness endpoint actively. Its configuration is `docker/multi-node/switchboard/sb.json`: one route per HTTP method matching every path, and power-of-two-choices balancing.

## When things fail

| What fails | What happens | What recovers it |
|---|---|---|
| One LiteGraph node | The load balancer stops sending it traffic. Requests in flight on that node fail, and the client or load balancer retries idempotent ones. Nothing is lost, because the node held no data. | Restart the node. It is ready when its readiness check passes. |
| One Clutch node | Lock connections on that node close, so any lock held through it is released and the work it protected stops (a vector index build fails and can be retried; a retention job runs next cycle). Nodes reconnect through the other Clutch node within seconds. Reads, writes, and searches are unaffected. | Restart it. |
| All of Clutch | Reads, writes, searches, and chat continue. Starting a node, building a vector index, and the retention jobs wait or skip until Clutch returns. Readiness stays 200 but reports `Degraded` with `Checks.Clutch` false. | Restore Clutch. Nodes reconnect on their own. |
| PostgreSQL | Everything stops. Readiness reports the database unavailable. | Restore PostgreSQL. For high availability, run PostgreSQL itself highly available (a managed service, or a replication manager such as Patroni) and give LiteGraph its single connection string. |

The `docker/multi-node/failover.ps1` script checks the first two rows under load: it keeps requests flowing while it stops and restarts a LiteGraph node and then each Clutch node in turn (so every node's lock connection is cut at least once), checks that an index build succeeds and that every node reconnects while a Clutch node is down, and fails if more than 2% of requests fail.

## Operating a cluster

**Adding capacity.** Start another node with the same settings file, database, and secrets and a new `LITEGRAPH_NODE_ID`, then add it to the load balancer. There is nothing to copy and no data to rebalance.

**Changing settings.** Every node reads the shared settings file at startup. After changing a restart-required setting, restart the nodes one at a time, each only after the previous one reports ready, and the cluster keeps serving throughout. The settings page's restart button restarts only the node that happened to receive the request.

**Upgrading.** Stop and replace nodes one at a time the same way. A release that changes the schema migrates it under the Clutch `schema` lock when the first upgraded node starts. That release's notes say whether older nodes can keep running alongside it.

**Backups.** Back up PostgreSQL with its own tools (`pg_dump`, or snapshots of the data volume). The LiteGraph backup API applies only to SQLite. There is no per-node state to back up.

**Observability.** Scrape each node's `/metrics` directly rather than through the load balancer, so every node's counters are visible. The `docker/multi-node/prometheus.yaml` configuration does this. Logs from every node flow to Loki through the same syslog path as a single node.

## Limits and choices worth knowing

- **Vector index parameters are shared.** pgvector builds one cosine HNSW index per vector dimensionality, shared by every graph with that dimensionality. The first graph to enable indexing for a dimensionality sets the index's `M` and `EfConstruction`. `VectorIndexEf` still applies per search.
- **Very high dimensions.** pgvector can index up to 2,000 dimensions with `vector` and up to 4,000 with `halfvec`. LiteGraph picks the right one. Above 4,000 dimensions, searches are exact, done in SQL.
- **Chat concurrency and endpoint health are per node.** `Chat.MaxConcurrentChats` limits each node, so a three-node cluster allows three times as many concurrent chats. Each node probes chat endpoint health on its own and re-reads the endpoint list every `Cluster.EndpointResyncIntervalMs`, so an endpoint added through one node is monitored by the others within that interval.
- **One MCP server.** The MCP server keeps MCP sessions in memory, so the Docker deployment runs one instance, pointed at the load balancer. It holds no LiteGraph data, so this limits capacity, not correctness.
- **Settings edits through the API reach one node.** The settings API writes the shared file, but only the receiving node applies live settings immediately; the other nodes pick changes up when they restart.
