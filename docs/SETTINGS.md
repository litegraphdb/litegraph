# Server Settings

LiteGraph reads its configuration from `litegraph.json` at startup. Starting in v8.0 a system administrator can read and change that file over the API — and from the dashboard's **Settings** page — without editing it by hand on the server. The design is deliberately conservative: changes that are safe to apply to a running server take effect immediately, everything else is written to disk and applied on the next restart, and a system administrator can trigger that restart from the same page.

Only system administrators reach any of this. The endpoints are gated on the `IsSystemAdmin` capability (or the break-glass administrator token); a tenant administrator or a regular user receives `401`/`403`.

## Endpoints

| Purpose | Method | Route |
|---|---|---|
| Read the current settings | GET | `/v1.0/settings` |
| Update the settings | PUT | `/v1.0/settings` |
| Restart the server (every node, one at a time, in cluster mode) | POST | `/v1.0/settings/restart` |

`GET /v1.0/settings` returns the full settings object, in the same shape as `litegraph.json`, with the sections `RequestTimeoutSeconds`, `Logging`, `Caching`, `Rest`, `LiteGraph`, `Encryption`, `Storage`, `Debug`, `RequestHistory`, `Observability`, (as of v8.1) `Chat`, and (as of v9.0) `AuthorizationAudit`. The runtime-only logging callback is never serialized. As of v10.0 it returns the settings file rather than the running settings, so every node sharing the file returns the same answer and values supplied by environment variables are not echoed back.

`PUT /v1.0/settings` takes the full settings object as its body, validates it (the property setters enforce ranges and non-null sections, so a malformed payload is rejected before anything is written), writes it to `litegraph.json`, and returns a result describing what happened:

```json
{
  "Success": true,
  "AppliedLive": ["RequestTimeoutSeconds"],
  "RestartRequired": ["Logging", "Rest", "LiteGraph", "Storage", "Observability", "Encryption", "Caching", "RequestHistory", "AuthorizationAudit", "Chat"],
  "Message": "Settings saved. Restart the server to apply the settings marked as restart-required.",
  "EnvironmentOverrides": ["Encryption.Key", "LiteGraph.AdminBearerToken"],
  "SettingsVersion": null
}
```

`EnvironmentOverrides` (v10.0) lists the settings whose running value did not come from the file: environment variable overrides and values derived at startup. A save keeps the file's own value for each of them, so secrets, node identity, and database connection details supplied through the environment are never written into the file, however the request body was built. `SettingsVersion` is the cluster's settings version after the save, or null on a single node.

`AppliedLive` names the sections that changed the running server immediately. `RestartRequired` names the sections whose new values are on disk but will not take effect until the process restarts — ports, the database connection, storage paths, the logging sinks, and the observability meter are all captured by long-lived services at startup, so they belong here.

## Live vs. restart

The request pipeline reads `RequestTimeoutSeconds` on every request, so a change to it applies live. The other sections are held by services that are constructed once at startup — changing them in the file does not reach those services until they are rebuilt. Rather than pretend otherwise, the API tells you exactly which of your edits are live and which are pending, and the dashboard surfaces that per section.

## The AuthorizationAudit block (v9.0)

The `AuthorizationAudit` section controls what the server writes to the `authorizationaudit` store. Prior to v9.0 only denied authorization events were recorded; v9.0 adds auditing of *successful privileged actions* so the trail reflects who performed write/admin operations, not only who was turned away. See [RBAC.md](RBAC.md) for the record shape and query surface.

```json
{
  "AuthorizationAudit": {
    "Enable": true,
    "AuditSuccessfulActions": true
  }
}
```

| Field | Default | Meaning |
|---|---|---|
| `Enable` | `true` | Master switch. When `false`, neither denied nor permitted events are recorded |
| `AuditSuccessfulActions` | `true` | When `true`, permitted `write`/`admin` actions are recorded in addition to denials. When `false`, only denials are recorded (pre-v9.0 behavior). Read-scope requests are never audited regardless of this value |

The block is read on the request pipeline, but the running server binds the settings object at startup, so edits made through `PUT /v1.0/settings` are persisted to `litegraph.json` and reported as restart-required; they take effect after the next restart.

## The Chat block (v8.1)

The `Chat` section of `litegraph.json` is the operator's side of the chat feature: server-wide guardrails that no tenant can exceed. Per-tenant behavior — default endpoints, prompts, tool and retrieval policy — lives in the tenant chat settings record instead and is managed over `PUT /v1.0/tenants/{tenantGuid}/chat/settings` (see [CHAT.md](CHAT.md)).

```json
{
  "Chat": {
    "Enable": true,
    "MaxRetries": 2,
    "RetryBackoffMs": 500,
    "MaxToolIterationsCap": 25,
    "MaxConcurrentChats": 50,
    "SseKeepAliveSeconds": 15,
    "DefaultTimeoutMs": 120000
  }
}
```

| Field | Default | Range | Meaning |
|---|---|---|---|
| `Enable` | `true` | — | Feature kill switch. When `false`, completion requests return `503`; endpoint, thread, feedback, and settings routes stay available so configuration survives the outage |
| `MaxRetries` | `2` | 0–10 | Provider retries before the first token arrives. A stream that fails after the first token is never retried |
| `RetryBackoffMs` | `500` | 50–30000 | Base delay for exponential retry backoff; doubles per attempt |
| `MaxToolIterationsCap` | `25` | 1–100 | Hard ceiling on tool loop iterations per turn. The effective limit is the smaller of this and the tenant's `MaxToolIterations` |
| `MaxConcurrentChats` | `50` | 1–1000 | Server-wide cap on in-flight completions; requests beyond it receive `429` immediately rather than queueing |
| `SseKeepAliveSeconds` | `15` | 1–300 | After this many seconds of silence on a streaming response, the server writes a keepalive event (`retry: 3000`), so idle proxies and load balancers do not sever long generations. Applies to the native and OpenAI-format SSE streams; Ollama-format NDJSON streams have no keepalive |
| `DefaultTimeoutMs` | `120000` | >= 1000 | Upstream request timeout applied when an endpoint does not specify its own |

The block is read once at startup — the chat service, its concurrency semaphore, and its provider clients are built from it when the server boots — so every field is restart-required. Edits made through `PUT /v1.0/settings` land in the `RestartRequired` list and take effect after the next restart; none of the `Chat` fields hot-apply today. Tenant chat settings are the opposite: they are read per request and apply on the next completion without any restart.

## The Cluster block (v10.0)

The `Cluster` section turns a server into one node of a multi-node cluster: several identical nodes sharing one PostgreSQL database behind a load balancer. [CLUSTERING.md](CLUSTERING.md) explains how a cluster works; this section lists the fields.

```json
{
  "Cluster": {
    "Enable": false,
    "ClusterName": "litegraph",
    "NodeId": null,
    "TrustForwardedHeaders": false,
    "TrustedProxies": [],
    "AllowInsecureDefaults": false,
    "EndpointResyncIntervalMs": 30000,
    "Clutch": {
      "Endpoint": "http://127.0.0.1:8090",
      "AccessKey": null,
      "LeaseMs": 30000,
      "RequestTimeoutMs": 10000,
      "StartupConnectTimeoutMs": 120000
    },
    "Redis": {
      "ConnectionString": "127.0.0.1:6379",
      "PollIntervalMs": 2000,
      "NodeTimeoutMs": 15000,
      "NodeRetentionMs": 86400000
    },
    "RestartDrainMs": 5000,
    "RestartPeerTimeoutMs": 180000
  }
}
```

| Field | Default | Range | Meaning |
|---|---|---|---|
| `Enable` | `false` | | Run as a cluster node. Requires `Database.Type = Postgresql` and a Clutch access key; the server refuses to start otherwise |
| `ClusterName` | `litegraph` | 1 to 64 of `a-z`, `0-9`, `-` | Prefixes every Clutch lock key, so several clusters can share one Clutch deployment |
| `NodeId` | host name | | Unique node identifier, returned in the `x-litegraph-node` header and the health endpoints. Set it per node with `LITEGRAPH_NODE_ID` rather than in the shared file |
| `TrustForwardedHeaders` | `false` | | Trust `X-Forwarded-For` from the proxies in `TrustedProxies` when recording client addresses in request history, authorization audit, and traces. The header is read right to left, skipping trusted proxies, and the first untrusted address is the client; a request that does not come from a trusted proxy keeps its connection address, so clients cannot spoof it. Never used for access control |
| `TrustedProxies` | empty | | Proxy addresses or CIDR ranges (IPv4 or IPv6) whose forwarded headers are trusted; an invalid entry stops the server at startup |
| `AllowInsecureDefaults` | `false` | | Let a cluster node start with the all-zero encryption key or the default administrator token. Only for demonstrations; the `docker/multi-node` deployment sets it so it starts without setup |
| `EndpointResyncIntervalMs` | `30000` | 5000 to 600000 | How often each node re-reads chat endpoints from the database, so endpoints changed through another node are monitored |
| `Clutch.Endpoint` | `http://127.0.0.1:8090` | http or https URL | Clutch server, or the load balancer in front of its nodes |
| `Clutch.AccessKey` | none | | Clutch application access key (secret) |
| `Clutch.LeaseMs` | `30000` | 5000 to 300000 | Lock lease. Held locks are renewed over the node's lock connection at the interval Clutch advertises; a lock left unrenewed this long is treated as lost, and a node that dies loses its locks when its connection drops or the lease runs out |
| `Clutch.RequestTimeoutMs` | `10000` | 1000 to 120000 | Timeout for opening the lock connection and for each lock request to Clutch, not counting time spent waiting for a lock |
| `Clutch.StartupConnectTimeoutMs` | `120000` | 0 to 3600000 | How long a starting node keeps retrying Clutch before exiting |
| `Redis.ConnectionString` | `127.0.0.1:6379` | | StackExchange.Redis connection string for the node registry and change signals (secret when it carries a password). A node starts even if Redis is down and connects when it comes back |
| `Redis.PollIntervalMs` | `2000` | 500 to 60000 | How often each node writes its registry entry and checks for settings changes and restart requests |
| `Redis.NodeTimeoutMs` | `15000` | 2000 to 600000 | A node with no heartbeat for this long is reported `Offline` |
| `Redis.NodeRetentionMs` | `86400000` | 60000 to 2592000000 | Entries for nodes silent this long are removed from the registry |
| `RestartDrainMs` | `5000` | 0 to 120000 | During a rolling restart, how long a node reports not ready before it exits, so load balancers stop sending it requests |
| `RestartPeerTimeoutMs` | `180000` | 10000 to 3600000 | During a rolling restart, how long a node waits for a restarting peer to report healthy before restarting anyway |

Every node in a cluster must run with the same settings file, the same database, and the same `Encryption.Key` and `Encryption.Iv`; a security token issued by one node is decrypted by whichever node receives the next request. In cluster mode the server also forces `Caching.Enable` off and stops caching authorization policy, because those caches only learn about changes made through their own process. All `Cluster` fields are read at startup and are restart-required.

## Environment variables

Environment variables override the settings file, which keeps secrets out of it and lets cluster nodes share one file while differing in identity.

| Variable | Overrides |
|---|---|
| `LITEGRAPH_PORT` | `Rest.Port` |
| `LITEGRAPH_REQUEST_TIMEOUT_SECONDS` | `RequestTimeoutSeconds` |
| `LITEGRAPH_DB_TYPE`, `LITEGRAPH_DB_FILENAME` (or `LITEGRAPH_DB`), `LITEGRAPH_DB_HOST`, `LITEGRAPH_DB_PORT`, `LITEGRAPH_DB_NAME`, `LITEGRAPH_DB_USERNAME`, `LITEGRAPH_DB_PASSWORD`, `LITEGRAPH_DB_SCHEMA`, `LITEGRAPH_DB_CONNECTION_STRING`, `LITEGRAPH_DB_MAX_CONNECTIONS`, `LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS` | `LiteGraph.Database` |
| `LITEGRAPH_TRANSACTION_MAX_OPERATIONS`, `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS` | `LiteGraph.Transactions` |
| `LITEGRAPH_ADMIN_BEARER_TOKEN` | `LiteGraph.AdminBearerToken` (v10.0) |
| `LITEGRAPH_ENCRYPTION_KEY`, `LITEGRAPH_ENCRYPTION_IV` | `Encryption.Key`, `Encryption.Iv` (v10.0) |
| `LITEGRAPH_CLUSTER_ENABLE`, `LITEGRAPH_CLUSTER_NAME`, `LITEGRAPH_NODE_ID` | `Cluster.Enable`, `Cluster.ClusterName`, `Cluster.NodeId` (v10.0) |
| `LITEGRAPH_CLUTCH_ENDPOINT`, `LITEGRAPH_CLUTCH_ACCESS_KEY` | `Cluster.Clutch.Endpoint`, `Cluster.Clutch.AccessKey` (v10.0) |
| `LITEGRAPH_REDIS_CONNECTION_STRING` | `Cluster.Redis.ConnectionString` (v10.0) |
| `LITEGRAPH_TRUSTED_PROXIES` | `Cluster.TrustedProxies` (comma separated) and turns on `Cluster.TrustForwardedHeaders` (v10.0) |
| `LITEGRAPH_CREATE_DEFAULT_RECORDS`, `LITEGRAPH_INIT_ONLY` | Create the default tenant, user, and credential; initialize the schema and exit |
| `LITEGRAPH_OTLP_*`, `OTEL_*` | `Observability` OTLP export settings (see [OBSERVABILITY.md](OBSERVABILITY.md)) |

## Restarting

`POST /v1.0/settings/restart` (body `{"confirm": true}`) flushes the database and then exits the process. That only produces a usable "restart" when something is watching the process and will bring it back — which is why the shipped Docker Compose gives the `litegraph`, `litegraph-mcp`, and `litegraph-ui` services `restart: unless-stopped`. Under that policy the container exits and Docker starts it again, this time reading the settings you just wrote. Run the server outside a supervisor and the same call simply stops it.

The dashboard's **Restart Server** control asks for confirmation, calls this endpoint, then shows a reconnecting state and recovers once the server answers again.

In a cluster (v10.0), a save through any node is signalled to every node through Redis: each applies `RequestTimeoutSeconds` within a couple of seconds and marks itself as needing a restart if anything else changed. The restart endpoint then requests a rolling restart: every node restarts, one at a time, each after the previous one reports healthy, so the cluster keeps serving throughout. The dashboard's button reads **Restart Cluster** in cluster mode, and its node list shows each node restart in turn. [CLUSTERING.md](CLUSTERING.md#settings-and-rolling-restarts) describes the sequence.

## Security notes

The settings object includes secrets — the administrator bearer token and the database connection string among them. Reading and writing settings is therefore restricted to system administrators, and the transport should be TLS in any deployment where the network is not fully trusted. The break-glass administrator token remains valid for these endpoints so that a locked-out operator can still recover the server.
