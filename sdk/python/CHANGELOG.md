# Change Log

## Current Version

v10.0.0

- Added cluster administration on `Admin`: `read_cluster_nodes`, `read_cluster_node`, `restart_cluster`, `restart_cluster_node`, and `delete_cluster_node`; `restart_server` returns the restart result
- Added `Admin.health_live` and `Admin.health_ready`; readiness returns the body for 503 as well as 200
- Replaced the fixed retry loop with a retry policy: `max_retries` (default 2), `retry_base_delay_ms` (default 200, exponential with jitter, capped at 5000 ms), and `retry_post` (default False), also accepted by `configure`; connection failures and 502/503/504 are retried for GET, HEAD, PUT, and DELETE, and streams are never retried after the first byte. `retries` remains as total attempts (`max_retries + 1`)
- Added `last_node_id` on the client and `node_id` and `status_code` on SDK exceptions
- Added the `Unavailable` error code and `ServiceUnavailableError`
- Added `Admin.read_cluster_locks` and `Admin.read_cluster_jobs`; health bodies document `StorageProvider`, `VectorIndexProvider`, and the `Redis` check

v7.0.0

- Added v7 graph transaction diagnostics, lifecycle state, and isolation-level response fields
- Added isolation-level request support for transaction resources
- Updated SDK documentation for LiteGraph v7.0.0

v6.0.2

- Added `return_mode` support for bulk create helpers
- Updated bulk create helpers to use `/bulk` create routes
- Updated documentation for bulk create response modes

v6.0.0

- Added v6 REST coverage for native graph queries, graph transactions, authorization, and request history
- Added v6 request/response models for query, transaction, and authorization workflows
- Updated SDK documentation for LiteGraph v6.0.0


## Previous Versions

Notes from previous versions will be shown here.

v1.0.0

- Initial release, compatibility with LiteGraph v3.1.0
