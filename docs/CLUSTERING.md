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
| `settings` | a node writes the shared settings file | two saves never interleave; the second waits up to 10 seconds, then fails with 409 |
| `restart` | a node takes its turn in a rolling restart | nodes restart one at a time |

Keys are prefixed with `litegraph/<ClusterName>/`, so several clusters can share one Clutch deployment. Each node holds one WebSocket lock connection to Clutch through `Clutch.Sdk`, and every lock it takes rides on that connection. Leases (`Cluster.Clutch.LeaseMs`, 30 seconds by default) are renewed in the background over the connection. If the connection drops, Clutch releases every lock it held, and the node treats those locks as lost, stops the work they were protecting, and reconnects on its own. If a node dies holding a lock, Clutch releases it when the connection drops or the lease expires.

Just as important is what takes no lock: reads, writes, searches, graph transactions, and chat. None of them depend on Clutch being reachable.

## The node registry and change signals, through Redis

Nodes do not need to know about each other to serve requests, but an administrator needs to see the cluster, and a settings change or a restart request has to reach every node. Redis carries both. Nothing in Redis has to survive a Redis restart, so it runs without persistence and needs no backup.

Every `Cluster.Redis.PollIntervalMs` (2 seconds by default) each node:

1. writes its entry to the hash `litegraph:<ClusterName>:nodes`: node identifier, host name, version, start time, heartbeat time, state, and the same checks as its readiness endpoint;
2. reads `settings:version` and `settings:changed-utc`, which the node that saves settings increments and stamps;
3. reads `restart:version` and `restart:requested-utc`, which the node that receives a restart request increments and stamps.

When `settings:changed-utc` changes, every node re-reads the settings file, applies the settings that can change live, and marks itself as needing a restart if anything else changed. When `restart:requested-utc` is later than the node's own start time, the node takes its turn in a rolling restart. A node that has restarted since the request started after it, so it never restarts twice, and a Redis restart can only lose a signal, never repeat one.

`GET /v1.0/cluster/nodes` returns the registry (see [REST_API.md](REST_API.md#cluster-v100)). A node whose heartbeat is older than `Cluster.Redis.NodeTimeoutMs` (15 seconds by default) is reported `Offline`; a node that shut down cleanly is reported `Stopped`.

## Settings and rolling restarts

All nodes share one settings file. `GET /v1.0/settings` returns that file, so every node returns the same answer. `PUT /v1.0/settings` writes it under the Clutch `settings` lock and signals the change through Redis:

- `RequestTimeoutSeconds` applies on every node within a couple of seconds.
- Everything else applies when each node restarts. The node list shows which nodes still need to.
- Values supplied by environment variables (node identity, secrets, the database connection, and so on) are never written to the shared file: the save keeps the file's own value for each of them and lists them in the response's `EnvironmentOverrides`.

`POST /v1.0/cluster/restart` (or `POST /v1.0/settings/restart`, which the dashboard's restart button uses) requests a rolling restart:

1. Every node sees the request within `PollIntervalMs` and asks Clutch for the `restart` lock.
2. The node that gets it waits until no other node is `Restarting` or `Draining`, reports itself `Restarting`, reports not ready for `Cluster.RestartDrainMs` (5 seconds by default) so load balancers stop sending it requests, then shuts down cleanly and releases the lock.
3. The container restart policy starts it again. The next node, already holding the lock, waits until the restarted node reports healthy, then takes its turn.

Only one node is ever out of service. If a restarting node does not come back within `Cluster.RestartPeerTimeoutMs` (3 minutes by default), the next node goes ahead anyway. A node outside a container, with nothing to restart it, stays stopped.

## What a cluster needs

- **PostgreSQL with pgvector**, reachable from every node. SQLite cannot be shared between processes, so the server refuses to start in cluster mode on SQLite. Every node points at the same database and schema.
- **Clutch**, reachable from every node. The Docker deployment runs two Clutch nodes behind a small Nginx so Clutch itself has no single point of failure. Clutch keeps its data in its own database on the same PostgreSQL server, owned by its own role.
- **Redis**, reachable from every node (`Cluster.Redis.ConnectionString` or `LITEGRAPH_REDIS_CONNECTION_STRING`). One instance without persistence is enough: while it is down, nodes keep serving and only the node list, settings signals, and restart requests wait.
- **One settings file for all nodes**, with node identity (`LITEGRAPH_NODE_ID`) supplied per node through the environment.
- **The same encryption key and IV on every node.** Security tokens are encrypted by one node and decrypted by whichever node receives the next request.
- **A load balancer.** No session affinity is needed. The Docker deployment uses Nginx with least-connections balancing, and Switchboard as an alternative.

Turn it on with `Cluster.Enable = true` (or `LITEGRAPH_CLUSTER_ENABLE=true`) plus the Clutch endpoint and access key; [SETTINGS.md](SETTINGS.md#the-cluster-block-v100) lists every field. A cluster node refuses to start with the default encryption key or administrator token unless `Cluster.AllowInsecureDefaults` is set, which is meant only for demonstrations.

## Health checks and load balancers

Each node answers:

- `GET /v1.0/health/live`: 200 while the process is running.
- `GET /v1.0/health/ready`: 200 when the database answers and the node is not shutting down; 503 otherwise, with a body listing each check. A cluster node that cannot reach Clutch or Redis still answers 200, with `Status` `Degraded` and `Checks.Clutch` or `Checks.Redis` false, because it can still serve reads, writes, and searches; taking every node out of rotation would turn a coordination outage into a full outage.

Point the load balancer's health checks at the readiness endpoint. Every response also carries `x-litegraph-node`, which is the fastest way to see which node answered a request.

The Nginx configuration in `docker/multi-node/nginx/litegraph.conf` shows the settings that matter for LiteGraph:

- Response buffering off and a long read timeout, so streamed chat responses flow through. The server also writes a keepalive event on a silent stream every `Chat.SseKeepAliveSeconds`, for load balancers with shorter idle timeouts.
- `X-Forwarded-For` set by the load balancer. With `LITEGRAPH_TRUSTED_PROXIES` naming the load balancer's addresses, request history, audit, and traces record the real client address instead of the load balancer's. Request history also records which node handled each request (`NodeId`, filterable).
- A generous request body limit, for imports.
- Retries on connection errors and gateway errors. Nginx does not retry POST requests unless told to, and it should not be told to.
- Node names re-resolved through Docker's DNS (`resolver 127.0.0.11` and `resolve` on each upstream server). Nginx otherwise keeps the addresses it resolved at startup, and a node recreated by `docker compose up` comes back at a new address.

Open-source Nginx detects failed nodes passively, when requests to them fail. Switchboard, the alternative load balancer, probes each node's readiness endpoint actively. Its configuration is `docker/multi-node/switchboard/sb.json`: one route per HTTP method matching every path, and power-of-two-choices balancing.

## When things fail

| What fails | What happens | What recovers it |
|---|---|---|
| One LiteGraph node | The load balancer stops sending it traffic. Requests in flight on that node fail, and the client or load balancer retries idempotent ones. Nothing is lost, because the node held no data. | Restart the node. It is ready when its readiness check passes. |
| One Clutch node | Lock connections on that node close, so any lock held through it is released and the work it protected stops (a vector index build fails and can be retried; a retention job runs next cycle). Nodes reconnect through the other Clutch node within seconds. Reads, writes, and searches are unaffected. | Restart it. |
| All of Clutch | Reads, writes, searches, and chat continue. Starting a node, building a vector index, and the retention jobs wait or skip until Clutch returns. Readiness stays 200 but reports `Degraded` with `Checks.Clutch` false. | Restore Clutch. Nodes reconnect on their own. |
| Redis | Reads, writes, searches, and chat continue. Readiness stays 200 but reports `Degraded` with `Checks.Redis` false. The node list shows only the answering node, settings changes reach other nodes when they restart, and restart requests are refused with 503. | Restore Redis. Nodes reconnect on their own and re-register within `PollIntervalMs`. |
| PostgreSQL | Everything stops. Readiness reports the database unavailable. | Restore PostgreSQL. For high availability, run PostgreSQL itself highly available (a managed service, or a replication manager such as Patroni) and give LiteGraph its single connection string. |

The `docker/multi-node/failover.ps1` script checks this table under load. It keeps requests flowing while it:

- stops and restarts a LiteGraph node;
- stops each Clutch node in turn, so every node's lock connection is cut at least once, and checks that an index build succeeds and every node reconnects;
- stops Redis and checks that every node stays ready, restart requests are refused, and every node reconnects;
- requests a rolling restart and checks that every node restarts with never more than one out of service.

It fails if more than 2% of requests fail in any phase.

## Operating a cluster

**Adding capacity.** Start another node with the same settings file, database, and secrets and a new `LITEGRAPH_NODE_ID`, then add it to the load balancer. There is nothing to copy and no data to rebalance.

**Changing settings.** Save through the dashboard's settings page or `PUT /v1.0/settings`, then request a rolling restart with the page's Restart Cluster button or `POST /v1.0/cluster/restart`. The page's node list, which refreshes every few seconds, shows each node restart in turn. Editing the settings file by hand works too, but the nodes only notice it when they restart.

**Upgrading.** A rolling restart restarts the same image. To move to a new image, replace nodes one at a time (`docker compose up -d --no-deps litegraph-1`, wait until it is healthy, then the next). A release that changes the schema migrates it under the Clutch `schema` lock when the first upgraded node starts. That release's notes say whether older nodes can keep running alongside it.

**Backups.** Back up PostgreSQL with its own tools (`pg_dump`, or snapshots of the data volume). The LiteGraph backup API applies only to SQLite. There is no per-node state to back up.

**Observability.** Scrape each node's `/metrics` directly rather than through the load balancer, with a `node` label on each target, as `docker/multi-node/prometheus.yaml` does. The **LiteGraph Cluster** Grafana dashboard shows node states, per-node traffic, lock activity, and Clutch and Redis connectivity, and every other dashboard has a Node filter. Logs from every node flow to Loki through the same syslog path as a single node, labeled with the node's host name. [OBSERVABILITY.md](OBSERVABILITY.md) lists the node and cluster metrics.

**The dashboard's Cluster page** (System, system administrators only) lists every node with its state, checks, and settings version, restarts or removes one node, runs a rolling restart with a live progress view, and shows the locks the cluster holds in Clutch (`GET /v1.0/cluster/locks`) and the latest run of each singleton job (`GET /v1.0/cluster/jobs`). Restarting a single node (`POST /v1.0/cluster/nodes/{nodeId}/restart`) takes the same `restart` lock as a rolling restart, so it still waits for any node that is restarting. Removing a node only clears the registry entry of a node that is `Offline` or `Stopped`, for example after decommissioning it.

## Limits and choices worth knowing

- **Vector index parameters are shared.** pgvector builds one cosine HNSW index per vector dimensionality, shared by every graph with that dimensionality. The first graph to enable indexing for a dimensionality sets the index's `M` and `EfConstruction`. `VectorIndexEf` still applies per search.
- **Very high dimensions.** pgvector can index up to 2,000 dimensions with `vector` and up to 4,000 with `halfvec`. LiteGraph picks the right one. Above 4,000 dimensions, searches are exact, done in SQL.
- **Chat concurrency and endpoint health are per node.** `Chat.MaxConcurrentChats` limits each node, so a three-node cluster allows three times as many concurrent chats. Each node probes chat endpoint health on its own and re-reads the endpoint list every `Cluster.EndpointResyncIntervalMs`, so an endpoint added through one node is monitored by the others within that interval.
- **One MCP server.** The MCP server keeps MCP sessions in memory, so the Docker deployment runs one instance, pointed at the load balancer. It holds no LiteGraph data, so this limits capacity, not correctness.
- **The node registry is advisory.** It shows what each node last reported. Correctness never depends on it: a node missing from the list, or a lost signal, costs visibility or a repeated restart request, never data.
