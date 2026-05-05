# LiteGraph RBAC And Scoped Credentials

LiteGraph authorization for this release is enforced at REST and MCP boundaries. Core `LiteGraphClient` and repository APIs remain permission-agnostic for embedded use.

This document describes the currently implemented scoped credential behavior and the remaining RBAC limits.

## Current Authorization Boundary

REST requests authenticate through one of these mechanisms:

- administrator bearer token
- LiteGraph credential bearer token
- LiteGraph user email/password where supported by the route
- LiteGraph security token where supported by the route

The administrator bearer token has full administrative access.

LiteGraph credential bearer tokens can be restricted by:

- tenant isolation
- operation scope
- graph allow-list

MCP `graph/query` and `graph/transaction` route through the REST endpoints, so those tools use the same REST authentication, graph scoping, and credential-scope checks. Full per-tool authorization for legacy direct-SDK MCP tools remains future hardening.

REST authorization decisions are evaluated by `AuthorizationService`. The service currently centralizes:

- tenant boundary checks for non-admin requests
- credential graph allow-list checks
- credential operation-scope checks
- request-type to `read` or `write` scope mapping
- effective user-role assignment checks when stored assignments exist
- effective credential-scope assignment checks when stored assignments exist

## Credential Fields

Credentials include:

```json
{
  "BearerToken": "service-token",
  "Scopes": ["read", "write"],
  "GraphGUIDs": [
    "11111111-1111-1111-1111-111111111111"
  ]
}
```

`Scopes`:

- `null` or empty means full access for backward compatibility.
- `read` permits read routes.
- `write` permits write routes.
- `admin` permits all scopes.
- `*` permits all scopes.
- Scope matching is case-insensitive.

`GraphGUIDs`:

- `null` or empty means all graphs in the credential tenant.
- a non-empty list restricts the credential to those graph GUIDs.
- graph matching is exact and applies only within the credential tenant.

## Backward Compatibility

Existing credentials retain effective full access because missing or empty `Scopes` and `GraphGUIDs` are interpreted as unrestricted within the tenant.

The administrator bearer token remains an administrator credential and is not constrained by credential scopes or graph allow-lists.

Existing users receive effective full access in their tenant by migration policy definition. Existing credential access remains backward-compatible through null or empty `Scopes` and `GraphGUIDs`.

## Built-In Roles

The native permission model defines four permissions:

- `Read`
- `Write`
- `Delete`
- `Admin`

It defines two resource scopes:

- `Tenant`
- `Graph`

It defines these resource types:

- `Admin`
- `Graph`
- `Node`
- `Edge`
- `Label`
- `Tag`
- `Vector`
- `Query`
- `Transaction`

Built-in role definitions are available through `AuthorizationPolicyDefinitions.BuiltInRoles`:

- `TenantAdmin`: tenant-scoped role with `Read`, `Write`, `Delete`, and `Admin` across all resource types. Tenant assignment is intended to inherit to graphs in the tenant.
- `GraphAdmin`: graph-scoped role with `Read`, `Write`, `Delete`, and `Admin` across graph data, query, and transaction resource types. It does not apply to tenant/server `Admin` resources.
- `Editor`: graph-scoped role with `Read`, `Write`, and `Delete` across graph data, query, and transaction resource types.
- `Viewer`: graph-scoped role with `Read` across graph data and read-only query resource types. It does not apply to mutating transactions.
- `Custom`: built-in template marker for stored custom roles. It has no predefined permissions or resource types.

## Role And Assignment Storage

SQLite role and assignment storage is available through `LiteGraphClient.AuthorizationRoles` and the underlying repository surface.

SQLite initialization seeds the global built-in role records (`TenantAdmin`, `GraphAdmin`, `Editor`, `Viewer`, and `Custom`) if they are missing. Re-initialization refreshes the built-in role definition fields while preserving the existing built-in role GUIDs.

Stored role records use `AuthorizationRole` and support:

- create role
- read role by GUID
- read role by tenant/name or global name
- filtered role search with pagination
- update role
- delete role by GUID

User role assignments use `UserRoleAssignment` and support create, read by GUID, filtered search with pagination, update, delete by GUID, and bulk delete by search.

Credential scope assignments use `CredentialScopeAssignment` and support create, read by GUID, filtered search with pagination, update, delete by GUID, and bulk delete by search.

The SQLite tables are:

- `authorizationroles`
- `userroleassignments`
- `credentialscopeassignments`

The storage layer is intentionally permission-agnostic. Effective role evaluation is handled by `AuthorizationService`; permission caching and full MCP management/enforcement remain separate release work.

## REST Role Management

REST role management is available under tenant-scoped endpoints:

- `PUT /v1.0/tenants/{tenantGuid}/roles`
- `GET /v1.0/tenants/{tenantGuid}/roles`
- `GET /v1.0/tenants/{tenantGuid}/roles/{roleGuid}`
- `PUT /v1.0/tenants/{tenantGuid}/roles/{roleGuid}`
- `DELETE /v1.0/tenants/{tenantGuid}/roles/{roleGuid}`

User-role assignment endpoints are:

- `PUT /v1.0/tenants/{tenantGuid}/users/{userGuid}/roles`
- `GET /v1.0/tenants/{tenantGuid}/users/{userGuid}/roles`
- `GET /v1.0/tenants/{tenantGuid}/users/{userGuid}/roles/{assignmentGuid}`
- `PUT /v1.0/tenants/{tenantGuid}/users/{userGuid}/roles/{assignmentGuid}`
- `DELETE /v1.0/tenants/{tenantGuid}/users/{userGuid}/roles/{assignmentGuid}`
- `GET /v1.0/tenants/{tenantGuid}/users/{userGuid}/permissions`

Credential-scope assignment endpoints are:

- `PUT /v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes`
- `GET /v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes`
- `GET /v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes/{assignmentGuid}`
- `PUT /v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes/{assignmentGuid}`
- `DELETE /v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes/{assignmentGuid}`
- `GET /v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/permissions`

Role management endpoints require admin permission. The server admin bearer token can call them, and LiteGraph users can call them when stored user-role assignments grant `Admin` over the `Admin` resource type, such as `TenantAdmin`.

Global built-in roles are listed and readable from tenant role endpoints, but they are immutable through REST. REST-created roles are tenant-scoped custom roles.

## Effective Assignment Behavior

`AuthorizationService` evaluates stored assignments at REST authorization time when assignments exist for the authenticated user or credential.

Backward compatibility remains the default:

- a credential with no stored credential-scope assignments keeps the legacy `Scopes` and `GraphGUIDs` behavior
- a user with no stored user-role assignments keeps effective tenant access after authentication
- admin-class endpoints, including role management, require an explicit admin grant and do not use the unassigned-user compatibility fallback

When stored assignments exist, they become an additional permission boundary:

- credential `Scopes` and `GraphGUIDs` are still enforced first
- stored credential-scope assignments must then grant the required permission/resource type
- stored user-role assignments must grant the required permission/resource type
- graph-scoped assignments apply only to the assigned graph unless the graph GUID is omitted
- tenant-scoped assignments apply to tenant operations and inherit to graph operations only when the resolved role is marked as inheriting to graphs

Assignments can reference stored roles by GUID or name. If a stored role is not found by name, LiteGraph falls back to the built-in role definitions in `AuthorizationPolicyDefinitions`. This keeps assignments using built-in names such as `Viewer`, `Editor`, `GraphAdmin`, and `TenantAdmin` resilient even if a built-in role record is missing.

Credential-scope assignments can also grant direct permissions and resource types without a role reference.

## Identity Boundary

RBAC is based on LiteGraph-native users and credentials.

LiteGraph does not map arbitrary external identities to LiteGraph users, auto-provision users from external identities, or infer LiteGraph users from email or claim heuristics. If JWT-formatted tokens are added later, they must represent LiteGraph-controlled users or credentials by explicit LiteGraph identifiers.

External identity federation remains out of scope unless a separate design adds it.

## Operation Scope Mapping

LiteGraph currently maps REST request types to three operational scopes:

- `read`
- `write`
- `admin`

Write scope is required for create, update, delete, backup delete, flush, graph transactions, graph vector index mutation, and other mutating routes.

Read scope is required for non-mutating routes such as read, exists, enumerate, search, statistics, vector search, and request history reads.

Admin scope is required for tenant/user/credential administrative operations, role management, assignment management, effective permission inspection, backup, and flush operations. Built-in `TenantAdmin` grants this at tenant scope.

## Query Scope Mapping

Native graph query authorization is based on the parsed query kind:

- read queries require `read`
- vector search queries require `read`
- create queries require `write`
- `MATCH ... SET` mutation queries require `write`
- `MATCH ... DELETE` mutation queries require `write`

If a query cannot be parsed during scope classification, LiteGraph falls back to mutation-keyword detection and requires `write` when it finds `CREATE`, `MERGE`, `SET`, `DELETE`, or `REMOVE`.

## Transaction Scope Mapping

Graph transactions require `write` because they mutate graph child objects.

Transactions still execute within one tenant and one graph. Credential graph allow-lists are checked before transaction execution reaches the transaction executor.

## Creating A Read-Only Credential

Create or update a credential with only the `read` scope:

```json
{
  "Name": "Read only service",
  "BearerToken": "read-only-token",
  "Scopes": ["read"]
}
```

This credential can read resources in its tenant but cannot create, update, delete, execute graph transactions, or execute mutation queries.

## Creating A Graph-Scoped Credential

Restrict a credential to one graph:

```json
{
  "Name": "Graph editor",
  "BearerToken": "graph-editor-token",
  "Scopes": ["read", "write"],
  "GraphGUIDs": [
    "11111111-1111-1111-1111-111111111111"
  ]
}
```

This credential can read and write within the listed graph only. Attempts to access another graph in the same tenant are denied.

## Authorization Failures

REST authorization failures return an authorization error response. Denied credential usage is logged by the authorization service for graph allow-list and missing-scope denials.

Authorization error responses include contextual fields when LiteGraph can determine them:

- `reason`
- `requiredScope`
- `requestType`
- `tenantGuid`
- `graphGuid`

The context is intended for clients and operators. It does not map to an external identity and does not grant additional information beyond the request boundary.

Denied REST authorization responses are also written to the `authorizationaudit` repository surface when the server can build a request context. Audit records include:

- request ID, correlation ID, and trace ID
- tenant, graph, user, and credential GUIDs where available
- request type, HTTP method, path, and source IP
- authentication and authorization results
- denial reason and required scope
- response status code and description

The audit store is available through `LiteGraphClient.AuthorizationAudit` for embedded and server-side use. It supports insert, read by GUID, filtered search, pagination, delete by GUID, bulk delete by search, and delete older than a UTC cutoff.

Observability records authentication and authorization result counters through:

```text
litegraph_authentication_requests_total
```

## Current Limits

- Full MCP per-tool authorization for legacy direct-SDK tools remains future hardening; v6.0.0 query, transaction, and authorization MCP tools route through REST authorization.
- SQLite and PostgreSQL authorization role, assignment, and audit storage are implemented. MySQL and SQL Server authorization storage remains provider work.
- External identity mapping is out of scope for this release unless a separate design adds it.
