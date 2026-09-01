# Server Settings

LiteGraph reads its configuration from `litegraph.json` at startup. Starting in v8.0 a system administrator can read and change that file over the API — and from the dashboard's **Settings** page — without editing it by hand on the server. The design is deliberately conservative: changes that are safe to apply to a running server take effect immediately, everything else is written to disk and applied on the next restart, and a system administrator can trigger that restart from the same page.

Only system administrators reach any of this. The endpoints are gated on the `IsSystemAdmin` capability (or the break-glass administrator token); a tenant administrator or a regular user receives `401`/`403`.

## Endpoints

| Purpose | Method | Route |
|---|---|---|
| Read the current settings | GET | `/v1.0/settings` |
| Update the settings | PUT | `/v1.0/settings` |
| Restart the server | POST | `/v1.0/settings/restart` |

`GET /v1.0/settings` returns the full settings object — the same shape as `litegraph.json`, with the sections `RequestTimeoutSeconds`, `Logging`, `Caching`, `Rest`, `LiteGraph`, `Encryption`, `Storage`, `Debug`, `RequestHistory`, `Observability`, and (as of v8.1) `Chat`. The runtime-only logging callback is never serialized.

`PUT /v1.0/settings` takes the full settings object as its body, validates it (the property setters enforce ranges and non-null sections, so a malformed payload is rejected before anything is written), writes it to `litegraph.json`, and returns a result describing what happened:

```json
{
  "Success": true,
  "AppliedLive": ["RequestTimeoutSeconds"],
  "RestartRequired": ["Logging", "Rest", "LiteGraph", "Storage", "Observability", "Encryption", "Caching", "RequestHistory", "Chat"],
  "Message": "Settings saved. Restart the server to apply the settings marked as restart-required."
}
```

`AppliedLive` names the sections that changed the running server immediately. `RestartRequired` names the sections whose new values are on disk but will not take effect until the process restarts — ports, the database connection, storage paths, the logging sinks, and the observability meter are all captured by long-lived services at startup, so they belong here.

## Live vs. restart

The request pipeline reads `RequestTimeoutSeconds` on every request, so a change to it applies live. The other sections are held by services that are constructed once at startup — changing them in the file does not reach those services until they are rebuilt. Rather than pretend otherwise, the API tells you exactly which of your edits are live and which are pending, and the dashboard surfaces that per section.

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
| `SseKeepAliveSeconds` | `15` | 1–300 | Interval between SSE keep-alive comment frames on streaming responses, so idle proxies do not sever long generations |
| `DefaultTimeoutMs` | `120000` | >= 1000 | Upstream request timeout applied when an endpoint does not specify its own |

The block is read once at startup — the chat service, its concurrency semaphore, and its provider clients are built from it when the server boots — so every field is restart-required. Edits made through `PUT /v1.0/settings` land in the `RestartRequired` list and take effect after the next restart; none of the `Chat` fields hot-apply today. Tenant chat settings are the opposite: they are read per request and apply on the next completion without any restart.

## Restarting

`POST /v1.0/settings/restart` (body `{"confirm": true}`) flushes the database and then exits the process. That only produces a usable "restart" when something is watching the process and will bring it back — which is why the shipped Docker Compose gives the `litegraph`, `litegraph-mcp`, and `litegraph-ui` services `restart: unless-stopped`. Under that policy the container exits and Docker starts it again, this time reading the settings you just wrote. Run the server outside a supervisor and the same call simply stops it.

The dashboard's **Restart Server** control asks for confirmation, calls this endpoint, then shows a reconnecting state and recovers once the server answers again.

## Security notes

The settings object includes secrets — the administrator bearer token and the database connection string among them. Reading and writing settings is therefore restricted to system administrators, and the transport should be TLS in any deployment where the network is not fully trusted. The break-glass administrator token remains valid for these endpoints so that a locked-out operator can still recover the server.
