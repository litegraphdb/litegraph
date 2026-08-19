# LiteGraph v8.0 — Implementation Plan

LiteGraph v8.0 is a deliberate breaking release. It collapses the two-tier "administrator vs. user" split into a single account model with capability flags, unifies the two logins and two dashboards into one, reorganizes the dashboard around a real information hierarchy, adds a form-based server-settings editor, and finishes the observability story so that every REST route and every MCP tool is measured in Prometheus and every log line is searchable in Grafana. Because the account model, the auth surface, and the dashboard routes all change shape, this cannot be a point release — the version is **8.0.0** and existing v7 databases are not upgraded in place.

This document is written to be executed and annotated. Every task is a checkbox. Check it when the work is done and the stated verification passes; leave a dated note beside anything deferred. The plan complies with the standards in `c:\code\agents\requirements` — `CODE_STYLE.md` and the CLAUDE.md coding rules for all C#, `BACKEND_TEST_ARCHITECTURE.md` (Touchstone) for backend tests, `DASHBOARD_STYLE_AND_USABILITY.md` and `I18N.md` for the dashboard, `REPOSITORY_REQUIREMENTS.md` for repo assets, and `WRITING_DOCUMENTS.md` for prose docs. Loopback is always `127.0.0.1`, never `localhost`.

Two rules run through the whole plan and are not optional. **Tests expand in both directions at every layer** — each behavior gets positive cases (it does what it should) and negative cases (it refuses, errors, or degrades correctly). And **the dashboard gets explicit, repeated UX passes** — not a single review at the end, but a usability/aesthetics/layout pass after each dashboard milestone and a full rendered walkthrough before release.

---

## Decisions that shape this release

These were settled before planning and are binding unless a task explicitly revisits them.

- **Accounts.** Keep the existing per-tenant user record keyed by `(tenantGuid, email)`. Do **not** introduce a global identity table. Add two capability flags to the user record: `IsSystemAdmin` and `IsTenantAdmin`. The same email may exist in several tenants as separate records with independent passwords.
- **Login.** One login for everyone: server URL → email → (tenant picker only when the email maps to more than one tenant) → password. The password is validated against the selected tenant's user record. The separate admin login is removed.
- **Authorization.** The flags overlay the existing RBAC rather than replacing it. `IsSystemAdmin` is a superuser and bypasses scope checks across all tenants. `IsTenantAdmin` has full rights within their own tenant. Everyone else is governed by the existing roles and credential-scope RBAC. The static `AdminBearerToken` stays as a documented break-glass/bootstrap credential.
- **Dashboard.** One dashboard. `DATA`, `METADATA`, and `MANAGE` sections are scoped to the active tenant chosen in the header. `SECURE` and `ADMINISTER` sections live at server-level routes. Visibility and edit rights are permission-filtered (see §4.2), not hidden wholesale.
- **Settings page.** A sectioned, form-based editor over `litegraph.json` (modeled on `c:\code\assistanthub\dashboard\src\views\AssistantSettingsView.jsx`), SystemAdmin only. Saving hot-reloads settings that are safe to change live and flags the rest; a **Restart Server** control exits the process so the container's restart policy brings it back with the new settings applied.
- **Observability.** Every REST route and every MCP tool emits metrics with consistent naming so Grafana treats REST and MCP as one system distinguished by a `component` label. Logs flow to Grafana via **Loki + Grafana Alloy** over syslog.
- **Migration.** Clean break. v8 creates a fresh schema. The upgrade path for existing data is a documented v7 JSONL export → v8 import using the interchange feature shipped in v7.1. `docker/factory` reset seeds v8 defaults.

---

## 0. Branch, version, and Docker baseline (do first)

- [ ] Merge `v7.1` into `main` (fast-forward or a merge commit per repo convention), confirm `main` builds and the v7.1 suites are green, then tag the merge point.
- [ ] Create and switch to branch `8.0` from the updated `main`. All v8 work commits here.
- [ ] Bump every project/package version `7.1.0 → 8.0.0`: core/server/MCP/SDK `.csproj` `<Version>`, MCP `ServerVersion`/`SoftwareVersion`, `sdk/js/package.json`, `sdk/python` `__version__`, `dashboard/package.json`.
- [ ] Bump Docker image tags `v7.1.0 → v8.0.0` in `docker/compose.yaml` and `docker/factory/compose.yaml` (all `jchristn77/litegraph*` refs), and any `SoftwareVersion` in `docker/*.json` and `docker/factory/*.json`.
- [ ] Confirm the LiteGraph server, MCP, and UI services carry a restart policy of `unless-stopped` (or `always`) in both compose files — the Settings "Restart Server" action depends on it. Note that `postgresql-init` stays `restart: no`.
- [ ] Commit: `chore: branch 8.0, bump versions and docker tags to v8.0.0`.
- [ ] **Verification:** `dotnet build src/LiteGraph.sln` clean; `docker compose -f docker/compose.yaml config` validates.

---

## 1. Account model and authorization core (`src/LiteGraph`)

The account change is small in surface but central in consequence, so it comes first and everything else builds on it. Follow the CLAUDE.md style rules exactly: namespace-scoped usings, `_PascalCase` fields, XML docs on public members, `.ConfigureAwait(false)`, `CancellationToken` on async methods, guard clauses, specific exceptions, no `var`, no tuples in domain types.

### 1.1 Model and storage

- [ ] Add `bool IsSystemAdmin` (default `false`) and `bool IsTenantAdmin` (default `false`) to `UserMaster.cs`, with XML docs stating scope: `IsSystemAdmin` grants server-wide superuser rights; `IsTenantAdmin` grants full rights within the record's own tenant.
- [ ] Add the two columns to the users table DDL in both providers (`GraphRepositories/Sqlite` and `GraphRepositories/Postgresql`), defaulting to `0/false`, and update the row converters/hydration in each provider.
- [ ] Update `IUserMethods` / client + repository `UserMethods` create/update/read paths to round-trip the flags. Keep `ReadByEmail(tenantGuid, email)` and add `ReadByEmailAcrossTenants(email)` (returns the per-tenant records for a given email) to back the unified login's tenant lookup.
- [ ] Do **not** migrate v7 rows. Confirm a fresh v8 DB seeds a default SystemAdmin user (see §1.4).
- [ ] **Tests (Touchstone, `Test.Shared`, SQLite + PostgreSQL):**
  - Positive: create user with each flag combination; read back; update flags; `ReadByEmailAcrossTenants` returns one record per tenant for a shared email.
  - Negative: flags default to false when omitted; a non-admin user record cannot be silently elevated through an unrelated update path; `ReadByEmailAcrossTenants` on an unknown email returns empty, not null.

### 1.2 Authentication (`src/LiteGraph.Server/Services/AuthenticationService.cs`)

- [ ] Rework `Authenticate` so email+password+tenant resolves the user record and loads its flags into the `AuthenticationContext` (`IsSystemAdmin`, `IsTenantAdmin`, `TenantGUID`, `UserGUID`).
- [ ] Keep the bearer-token path: a bearer equal to `AdminBearerToken` continues to yield a system-level principal (break-glass). Document it as such.
- [ ] Add/confirm an unauthenticated `GET /v1.0/token/tenants?email=` (or reuse the existing `getTenantsForEmail`) that returns the tenants a given email belongs to, for the login tenant picker. It must reveal only tenant id/name, never whether a password is correct.
- [ ] **Tests:** positive — valid email/password/tenant authenticates and carries the right flags; break-glass token authenticates as system. Negative — wrong password, wrong tenant for the email, disabled/inactive user, unknown email, and email-enumeration resistance (same response shape/timing whether or not the email exists) all fail closed.

### 1.3 Authorization overlay (`src/LiteGraph.Server/Services/AuthorizationService.cs`)

- [ ] Introduce the overlay at the top of `EvaluateRequestAccess`: if `IsSystemAdmin`, permit (superuser) and record the decision reason as `SystemAdmin`. If `IsTenantAdmin` and the request targets the principal's own tenant, permit within that tenant. Otherwise fall through to the existing role/credential-scope evaluation unchanged.
- [ ] Add explicit rules for the self-service surface (a regular user editing only their own user record and their own credentials — see §4.2): permit when the target user/credential GUID equals the principal's own; deny cross-user edits for non-admins.
- [ ] Ensure tenant-admins are confined to their own tenant for every server-level operation (they must not read or edit other tenants' users/credentials).
- [ ] **Tests (server RBAC boundary suite — this is where coverage expands most):** build a matrix across {SystemAdmin, TenantAdmin(ownTenant), TenantAdmin(otherTenant), RegularUser(self), RegularUser(other), break-glass token, unauthenticated} × {read/edit tenant, read/edit user, read/edit credential, backup, settings, data CRUD}. Every cell asserts permit or deny explicitly. This matrix is the definition of done for authorization.

### 1.4 Bootstrap and clean-break migration

- [ ] On first run against an empty database, seed a default tenant and a default **SystemAdmin** user (email/password from settings or generated and logged once), replacing the v7 "admin is a token" assumption. Keep the break-glass token available.
- [ ] Write the documented upgrade path: export each v7 graph to JSONL (v7.1 feature), stand up v8 fresh, import. Capture it in `docs/UPGRADE.md` with exact commands and the caveat that users/credentials/roles are re-created in v8, not carried.
- [ ] **Tests:** positive — fresh DB yields a working SystemAdmin login; negative — re-running init against a populated DB does not duplicate or clobber the seeded admin.

---

## 2. Observability: full REST + MCP metrics and logs in Grafana

The REST server already exports rich OpenTelemetry metrics at `/metrics` and Grafana has provisioned Prometheus dashboards. Two gaps remain: coverage is not per-route/per-tool complete, the MCP server emits nothing, and logs never reach Grafana. v8 closes all three.

### 2.1 SyslogLogging upgrade

- [ ] Bump `SyslogLogging` from `2.2.1` to the latest published `2.2.x` (`2.2.2` per `c:\code\misc\sysloglogging-2.0`, a drop-in dependency-maintenance release). If `2.2.2` is not yet on nuget.org, publish it from that source first, then reference it. Do not vendor the source — reference the package.
- [ ] Confirm the structured-logging surface (`StructuredLogBuilder`, severity, async) is used for the log lines that matter to operators, so Alloy/Loki can parse fields rather than scrape opaque text.
- [ ] **Tests:** the existing logging tests still pass; add a case asserting a structured log line serializes with the expected fields/severity.

### 2.2 REST metric completeness

- [ ] Ensure `ObservabilityService` records, for **every** route (not just the hand-picked ones), a request counter, a duration histogram, and an error counter, labeled by a low-cardinality route template name (e.g. `graph.export.jsonl`), HTTP method, and status class. Add an in-flight gauge. Avoid per-GUID/per-tenant labels that explode cardinality; if per-tenant insight is wanted, gate it behind a settings flag and document the cost.
- [ ] Drive labeling off the existing `RequestTypeEnum` / route registration so new routes are instrumented by construction, and add a startup assertion (or test) that every registered route maps to a metric label.
- [ ] **Tests:** hit a representative set of routes against a live server and scrape `/metrics`; assert the counters/histograms exist with the expected label sets, that error responses increment the error counter, and that an unmapped route would fail the startup assertion (negative).

### 2.3 MCP metrics (shared conventions, unified in Grafana)

- [ ] Give the MCP server an OpenTelemetry `Meter` using the **same metric names and label conventions** as REST, plus a `component="mcp"` label (REST carries `component="rest"`). Instrument every tool: call counter, duration histogram, error counter, and per-transport (`http`/`tcp`/`ws`) label. A single shared `/metrics` endpoint across two OS processes is not achievable, so expose the MCP server's own `/metrics` and scrape both — because names/labels match, Grafana panels show them as one system filtered by `component`.
- [ ] Add the MCP server as a second Prometheus scrape target (`docker/prometheus.yaml` and factory copy).
- [ ] Ship MCP logs to Loki via syslog on the same pipeline as REST.
- [ ] **Tests:** a Touchstone MCP-host case invokes a spread of tools across transports and asserts the MCP `/metrics` exposes matching counters with `component="mcp"`; a negative case asserts a failing tool call increments the MCP error counter.

### 2.4 Logs → Grafana (Loki + Alloy)

- [ ] Add **Grafana Loki** and **Grafana Alloy** services to `docker/compose.yaml` and `docker/factory/compose.yaml` (`.yaml`, build contexts or pinned images per `REPOSITORY_REQUIREMENTS.md`). Alloy receives LiteGraph's structured syslog and forwards to Loki.
- [ ] Point LiteGraph (server + MCP) syslog output at Alloy; keep console/file sinks intact.
- [ ] Add a Loki datasource to `docker/grafana/provisioning/datasources` (and factory copy) alongside the existing Prometheus datasource.
- [ ] **Verification:** `docker compose up`, generate traffic and an error, confirm the log line is queryable in Grafana Explore (Loki) and correlatable in time with the metrics spike.

### 2.5 Grafana dashboards

- [ ] Expand `docker/grafana/provisioning/dashboards/litegraph.yml` (and factory copy) to cover: request rate/latency/errors per REST route and per MCP tool (one system via `component`), auth outcomes, storage/transaction panels (retained from v7), and a Logs panel/row backed by Loki with a `component`/severity filter.
- [ ] **Verification:** load the provisioned dashboards against a live stack with traffic; every panel renders with data; document the dashboard layout in `docs/OBSERVABILITY.md`.

---

## 3. Server: settings API, self-service, and route consolidation

### 3.1 Settings read/write API

- [ ] Add SystemAdmin-only endpoints: `GET /v1.0/settings` (returns the current effective settings as the curated, sectioned shape the form consumes — never leaking secrets in plain text beyond what the form needs to edit) and `PUT /v1.0/settings` (validates and writes `litegraph.json`).
- [ ] On write, hot-reload the settings that can change safely at runtime (logging levels/sinks, observability toggles, request-timeout, caching knobs) and return, per field, whether the change is live or pending a restart.
- [ ] Add `POST /v1.0/settings/restart` (SystemAdmin only) that flushes, logs the intent, and exits the process cleanly so the container restart policy brings it back with the new file. Guard it behind an explicit confirmation flag in the body.
- [ ] Wire the new request types through `RequestTypeEnum`, `UrlContext`, `AuthorizationService` (admin scope), and the REST handler, following the v7.1 seven-step route pattern.
- [ ] **Tests:** positive — read returns current values; write persists to disk and hot-reloads a live-safe field; restart endpoint exits with the expected code (test the handler's decision, not an actual process kill, in unit scope). Negative — non-SystemAdmin is denied read/write/restart; invalid settings payload is rejected with a field-level error and the file is left unchanged; restart without the confirmation flag is refused.

### 3.2 Self-service and tenant-admin user/credential management

- [ ] Confirm/adjust the user and credential endpoints so the authorization overlay (§1.3) governs them: a regular user may read/update only their own user record and their own credentials; a tenant-admin may manage users/credentials within their tenant; a SystemAdmin, anywhere.
- [ ] **Tests:** extend the RBAC matrix (§1.3) to the concrete HTTP endpoints — each principal × each endpoint asserts the status code (200/403/404) explicitly.

### 3.3 Version/health surface

- [ ] Ensure `GET /` (or the health/version route) reports `8.0.0`, and that OpenAPI/API-Explorer metadata reflects the new/changed routes and the removed admin-login assumptions.

---

## 4. Dashboard: one login, one dashboard, hierarchical TOC

This is the largest surface and the one the user will feel most. Comply with `DASHBOARD_STYLE_AND_USABILITY.md`, keep everything internationalized per `I18N.md` (every new string is a `next-intl` catalog key in `en` and `es`, guarded by `npm run i18n:check`), and treat UX as a first-class deliverable with the passes in §4.6.

### 4.1 Unified login

- [ ] Replace the two login pages with one flow: server URL → email → (tenant picker only when `getTenantsForEmail` returns more than one) → password → authenticate against the selected tenant's record. Remove `/login/admin` and the `admin-login` page.
- [ ] Drop the `adminAccessKey`-vs-`token` split in the client auth/session model; the session now carries the user, the active tenant, and the capability flags returned at login. Keep break-glass token support as an "advanced" affordance if desired, clearly separated.
- [ ] **Tests (jest):** single-tenant email skips the picker; multi-tenant email shows it; wrong password surfaces a localized error; the flags returned drive what renders next.

### 4.2 Consolidated dashboard, hierarchical TOC, and permission gating

Replace the `tenantDashboardRoutes` / `adminDashboardRoutes` split in `src/constants/sidebar.tsx` with one grouped structure. Groups are section headers; items are routes. The target hierarchy:

```
HOME
  Home
DATA
  Graphs · Nodes · Edges
METADATA
  Labels · Tags · Vectors
MANAGE
  API Requests · API Explorer
SECURE
  Tenants · Users · Credentials · Authorization
ADMINISTER
  Backup · Settings
```

- [ ] Implement the grouped, i18n'd nav with section labels (`nav.section.*`) and item labels, driven by the capability flags and the active tenant.
- [ ] Route DATA/METADATA/MANAGE under the active tenant (header tenant selector sets context). Move SECURE/ADMINISTER to server-level routes. Retire the `/admin/dashboard/*` tree by folding its pages into the single dashboard.
- [ ] **Visibility and rights** (this is the crux — encode it in one place and test it hard):
  - *SystemAdmin:* sees everything; edits everything; can switch active tenant across all tenants; ADMINISTER (Backup, Settings) visible and editable.
  - *TenantAdmin:* sees only their own tenant; can edit their own tenant; sees and edits users and credentials within their tenant; sees Authorization for their tenant; ADMINISTER hidden.
  - *Regular user:* sees their own tenant read-only; under SECURE sees Users/Credentials but can view and edit **only their own** account and credentials; cannot edit the tenant; ADMINISTER hidden.
- [ ] Make gating declarative: a single capability map (`can(view|edit, resource, scope)`) consumed by nav rendering, route guards, and per-control disabled/hidden states — so the sidebar, the page, and the buttons never disagree.
- [ ] **Tests (jest):** for each of the three roles (plus SystemAdmin), assert the rendered nav sections/items, that guarded routes redirect or render read-only appropriately, and that edit controls are disabled/hidden where they must be. Negative cases: a regular user navigating directly to a tenant-edit or another user's record URL is denied client-side and (verified separately) server-side.

### 4.3 Settings page (form-based)

- [ ] Build a sectioned, form-based Settings page under ADMINISTER, modeled structurally on `c:\code\assistanthub\dashboard\src\views\AssistantSettingsView.jsx` (grouped sections, typed inputs, password inputs for secrets, dirty tracking, explicit Save). It edits the curated settings shape from §3.1 — not a raw JSON blob.
- [ ] Show, per field or section, whether a change applies live or needs a restart. Provide a **Restart Server** action (with a confirm modal) that calls `POST /v1.0/settings/restart`; after it fires, show a "reconnecting…" state and recover when the server returns.
- [ ] SystemAdmin only; the nav item and routes are hidden and guarded for everyone else.
- [ ] i18n every label, section title, helper text, and toast.
- [ ] **Tests (jest):** renders current settings; edits mark dirty and enable Save; save posts the right payload and reflects live-vs-restart status; Restart action requires confirmation and shows the reconnect state; non-admin cannot reach the page.

### 4.4 Interchange feature parity

- [ ] Re-verify the v7.1 JSONL export/import UI still works within the consolidated dashboard and new permission model (export = read, import = write), and that the request-history chart polish from v7.1 is intact.

### 4.5 Internationalization

- [ ] Every new/changed string is a catalog key in `en.json` and `es.json` with real Spanish; keys namespaced by feature (`nav.section.*`, `login.*`, `settings.*`, `secure.*`). Extend `scripts/check-i18n-literals.mjs` enforcement to the new/changed pages.
- [ ] **Verification:** `npm run i18n:check` passes; en/es key sets are identical; a locale switch visibly re-renders the new surfaces.

### 4.6 Dashboard UX / usability / aesthetics passes (explicit, repeated)

Do a pass after **each** of §4.1, §4.2, and §4.3, and a full one before release. Each pass is a rendered walkthrough (Playwright against a live build + server, both locales, at 1440px and a narrow width), not a code skim. Capture before/after screenshots in the PR.

- [ ] **Login pass:** the single flow reads cleanly; tenant picker only appears when needed; errors are legible and localized; keyboard/tab order works.
- [ ] **Navigation pass:** the grouped TOC is scannable; section headers are visually distinct but not heavy; the active item and active tenant are obvious; the header (tenant selector + language switcher + any break-glass affordance) is not crowded; nothing overflows in longer Spanish strings.
- [ ] **Settings pass:** sections are digestible; live-vs-restart affordances are unambiguous; the Restart action feels safe (confirm + clear recovery), not scary.
- [ ] **Whole-app pass before release:** consistency of spacing/typography/empty-loading-error states across every page for every role; responsive behavior; focus/aria on the new controls; no visual regressions from the consolidation. File and fix findings; re-render to confirm.

---

## 5. SDKs (C#, JavaScript, Python)

- [ ] Reflect the account-model change: user models carry `IsSystemAdmin`/`IsTenantAdmin`; add/adjust login/authentication helpers for the unified flow (email → tenants → tenant+password), and the tenants-for-email lookup.
- [ ] Add settings endpoints: read settings, update settings, restart server (SystemAdmin).
- [ ] Keep loopback base URLs at `127.0.0.1`. Keep chunked-response handling correct (the v7.1 C# `HttpClient` streaming fix stays).
- [ ] **Tests per SDK, positive and negative:** unified-login happy path and each failure (wrong password/tenant/email); settings read/update/restart as SystemAdmin succeed and as non-admin are denied; user create/update round-trips the flags. C# via the live harness; JS via msw; Python via pytest mocks.
- [ ] Update each SDK README with the new auth flow and settings methods.

---

## 6. MCP server

- [ ] Verify every existing tool still works under the new auth overlay (the MCP SDK principal is break-glass/SystemAdmin-equivalent; document that MCP operates with elevated rights and why).
- [ ] Ship the §2.3 metrics and §2.4 log routing from the MCP process.
- [ ] Add any tools needed for parity with new server capabilities only if it fits MCP's purpose (settings management via MCP is **out of scope** unless explicitly requested — note this decision).

---

## 7. Documentation

Prose sections follow `WRITING_DOCUMENTS.md` (author voice, no hollow "This ensures…" openings, varied rhythm, real prose around any list). API references stay terse and exact.

- [ ] `docs/AUTHENTICATION.md` — rewrite for the unified login, the flag model, the break-glass token, and the removal of the separate admin login.
- [ ] `docs/RBAC.md` — document the flag overlay precisely (SystemAdmin bypass, TenantAdmin scope, RBAC fall-through) and the permission matrix from §1.3/§4.2.
- [ ] `docs/OBSERVABILITY.md` — REST + MCP metric catalog with the `component` convention, the Loki/Alloy log pipeline, and the Grafana dashboards.
- [ ] `docs/SETTINGS.md` — **new**: the settings surface, which fields are live vs. restart, the Restart-Server behavior, and the security model.
- [ ] `docs/REST_API.md` — settings endpoints, changed auth/user fields, tenants-for-email lookup; remove admin-login-specific content.
- [ ] `docs/MCP_API.md` — the elevated-rights note and any tool changes; keep the tool catalog accurate.
- [ ] `docs/UPGRADE.md` — the clean-break v7→v8 story via JSONL export/import.
- [ ] `README.md` — a `## New In v8.0.0` section (unified accounts/login/dashboard, hierarchical TOC, settings editor, full REST+MCP observability, logs in Grafana) and version history.
- [ ] `CHANGELOG.md` — move v7.1.0 under Previous Versions; add a grouped v8.0.0 block (Accounts & auth; Dashboard; Observability; Settings; Docker; Docs; Validation).
- [ ] `DOCKERHUB_README.md` — confirm it exists and reflects v8 (per `REPOSITORY_REQUIREMENTS.md`); if missing, add it.
- [ ] **Verification:** diff the documented routes/tools/fields against the actual registrations and serialized shapes; correct any drift.

## 8. Postman

- [ ] Update the collection: unified-login/auth examples, tenants-for-email, settings read/update/restart, user objects with the new flags; remove admin-login-only items. Mirror across the API-version/auth folder variants as the collection already does.
- [ ] **Verification:** the collection parses as JSON; run the new/changed items against a live v8 server and confirm expected status codes.

## 9. Docker and factory assets

- [ ] `docker/compose.yaml` and `docker/factory/compose.yaml`: image tags `v8.0.0`; add Loki + Alloy; wire syslog from server + MCP to Alloy; ensure restart policies support the Settings restart action; keep PostgreSQL-backed default.
- [ ] `docker/prometheus.yaml` (+ factory): scrape both REST and MCP `/metrics`.
- [ ] Grafana provisioning (+ factory): Prometheus **and** Loki datasources; the expanded dashboards from §2.5.
- [ ] `docker/factory` reset scripts (`reset.sh`/`reset.bat`) and seed data: produce a working v8 default — a default tenant and a SystemAdmin user, with the break-glass token available.
- [ ] `docker/smoke.ps1` (+ any smoke): extend to validate REST, MCP, UI, Prometheus (both targets), Loki query, Grafana, the unified login, and a settings read.
- [ ] **Verification:** a clean `docker compose up` on both compose files yields a fully green smoke run.

## 10. Test architecture and continuous expansion

Backend tests are Touchstone descriptors in `Test.Shared`, run by `Test.Automated`/`Test.Xunit`/`Test.Nunit`, on SQLite and PostgreSQL. Every suite created below carries both positive and negative cases; treat the negative column as equally required.

- [ ] **Accounts suite:** flag round-trips, `ReadByEmailAcrossTenants`, seed/bootstrap behavior. (§1.1, §1.4)
- [ ] **Authentication suite:** unified login happy paths and every failure mode, break-glass token, email-enumeration resistance. (§1.2)
- [ ] **Authorization matrix suite:** the full principal × operation × scope matrix asserting permit/deny per cell — the single most important expansion in this release. (§1.3, §3.2)
- [ ] **Settings suite:** read/write/hot-reload/restart-decision, validation failures, non-admin denials. (§3.1)
- [ ] **Observability suite:** REST route→metric mapping assertion, `/metrics` scrape assertions for REST and MCP, error-counter increments, structured-log field assertions. (§2)
- [ ] **SDK suites:** per §5, all three SDKs, positive + negative.
- [ ] **Dashboard suites (jest):** login flow, nav/gating per role, settings page, i18n switch, permission-denied navigations. (§4)
- [ ] **Docker smoke:** per §9.
- [ ] Keep the v7.1 interchange and request-history suites green throughout; add regression cases where consolidation touched them.
- [ ] **Definition of done for testing:** no behavior ships without a negative case; the authorization matrix is exhaustive; SQLite and PostgreSQL both pass; the dashboard i18n and gating tests are green.

## 11. Release closeout (explicit final passes)

- [ ] **Full dashboard UX/aesthetic/usability walkthrough** (rendered, both locales, multiple widths, all three roles + SystemAdmin) with findings filed and fixed and before/after screenshots attached. (Ties off §4.6.)
- [ ] **API accuracy audit:** REST_API, MCP_API, and Postman reconciled against the actual registered routes/tools and serialized shapes; every new Postman item run green against a live server.
- [ ] **Observability acceptance:** on a live stack under load, confirm every REST route and MCP tool appears in Grafana with correct rates/latency/errors and that logs are searchable and time-correlated.
- [ ] **Full regression:** all backend suites (both providers), all three SDK suites, dashboard build + tests + i18n:check, and a clean docker smoke — all green.
- [ ] **Version/consistency sweep:** every artifact reads `8.0.0`; no lingering `7.x` version strings outside build output.
- [ ] Update `CHANGELOG.md` and `README.md` to final; open the PR.

---

## Suggested execution order

Bottom-up, each layer green before the next, with the dashboard's UX passes interleaved rather than deferred.

1. §0 branch/version/docker baseline.
2. §1 account model + auth overlay + bootstrap, with the accounts/authentication/authorization suites. This is load-bearing; land it first.
3. §3 server settings + self-service endpoints, with the settings and endpoint-matrix suites.
4. §2 observability (SyslogLogging bump, REST completeness, MCP metrics, Loki/Alloy, Grafana), with the observability suite. Runs largely parallel to §3.
5. §5 SDKs.
6. §4 dashboard — unified login → consolidated TOC/gating → settings page, each followed by its UX pass (§4.6).
7. §6 MCP verification, §9 docker/factory, §7 docs, §8 Postman.
8. §11 closeout: full UX walkthrough, API audit, observability acceptance, full regression, version sweep, PR.

Keep `8.0` compiling and green throughout; commit per layer with a descriptive message.
