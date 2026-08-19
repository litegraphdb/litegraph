# Server Settings

LiteGraph reads its configuration from `litegraph.json` at startup. Starting in v8.0 a system administrator can read and change that file over the API — and from the dashboard's **Settings** page — without editing it by hand on the server. The design is deliberately conservative: changes that are safe to apply to a running server take effect immediately, everything else is written to disk and applied on the next restart, and a system administrator can trigger that restart from the same page.

Only system administrators reach any of this. The endpoints are gated on the `IsSystemAdmin` capability (or the break-glass administrator token); a tenant administrator or a regular user receives `401`/`403`.

## Endpoints

| Purpose | Method | Route |
|---|---|---|
| Read the current settings | GET | `/v1.0/settings` |
| Update the settings | PUT | `/v1.0/settings` |
| Restart the server | POST | `/v1.0/settings/restart` |

`GET /v1.0/settings` returns the full settings object — the same shape as `litegraph.json`, with the sections `RequestTimeoutSeconds`, `Logging`, `Caching`, `Rest`, `LiteGraph`, `Encryption`, `Storage`, `Debug`, `RequestHistory`, and `Observability`. The runtime-only logging callback is never serialized.

`PUT /v1.0/settings` takes the full settings object as its body, validates it (the property setters enforce ranges and non-null sections, so a malformed payload is rejected before anything is written), writes it to `litegraph.json`, and returns a result describing what happened:

```json
{
  "Success": true,
  "AppliedLive": ["RequestTimeoutSeconds"],
  "RestartRequired": ["Logging", "Rest", "LiteGraph", "Storage", "Observability", "Encryption", "Caching", "RequestHistory"],
  "Message": "Settings saved. Restart the server to apply the settings marked as restart-required."
}
```

`AppliedLive` names the sections that changed the running server immediately. `RestartRequired` names the sections whose new values are on disk but will not take effect until the process restarts — ports, the database connection, storage paths, the logging sinks, and the observability meter are all captured by long-lived services at startup, so they belong here.

## Live vs. restart

The request pipeline reads `RequestTimeoutSeconds` on every request, so a change to it applies live. The other sections are held by services that are constructed once at startup — changing them in the file does not reach those services until they are rebuilt. Rather than pretend otherwise, the API tells you exactly which of your edits are live and which are pending, and the dashboard surfaces that per section.

## Restarting

`POST /v1.0/settings/restart` (body `{"confirm": true}`) flushes the database and then exits the process. That only produces a usable "restart" when something is watching the process and will bring it back — which is why the shipped Docker Compose gives the `litegraph`, `litegraph-mcp`, and `litegraph-ui` services `restart: unless-stopped`. Under that policy the container exits and Docker starts it again, this time reading the settings you just wrote. Run the server outside a supervisor and the same call simply stops it.

The dashboard's **Restart Server** control asks for confirmation, calls this endpoint, then shows a reconnecting state and recovers once the server answers again.

## Security notes

The settings object includes secrets — the administrator bearer token and the database connection string among them. Reading and writing settings is therefore restricted to system administrators, and the transport should be TLS in any deployment where the network is not fully trusted. The break-glass administrator token remains valid for these endpoints so that a locked-out operator can still recover the server.
