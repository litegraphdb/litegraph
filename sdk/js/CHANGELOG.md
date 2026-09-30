# Change Log

## Current Version

v10.0.0

- Added cluster administration: `readClusterNodes`, `readClusterNode`, `restartCluster`, `restartClusterNode`, and `deleteClusterNode`; `restartServer` returns the restart result
- Added `healthLive` and `healthReady`; readiness resolves with the body for 503 as well as 200
- Added a retry policy: `maxRetries` (default 2), `retryBaseDelayMs` (default 200, exponential with jitter, capped at 5000 ms), and `retryPost` (default false); connection failures and 502/503/504 are retried for GET, HEAD, PUT, and DELETE, and streams are never retried after the first byte
- Added `lastNodeId`, from the `x-litegraph-node` response header, and `nodeId` on `ApiErrorResponse`
- Added the `Unavailable` error code (503)
- Added `readClusterLocks` and `readClusterJobs`; health bodies document `StorageProvider`, `VectorIndexProvider`, and the `Redis` check
- Added request history: `listRequestHistory` (filters including `nodeId`, plus `maxKeys` and `skip`), `readRequestHistory`, `readRequestHistoryDetail`, `readRequestHistorySummary`, `deleteRequestHistory`, and `deleteRequestHistoryMany`, and `SdkBase.deleteForJson`

v7.0.0

- Added v7 graph transaction diagnostics, lifecycle state, and isolation-level response fields
- Added isolation-level request support for transaction helpers
- Updated package metadata for the LiteGraph v7.0.0 release

v6.0.2

- Added minimal/full bulk create return modes for labels, tags, vectors, nodes, and edges
- Added `returnMode` options for bulk create helpers while preserving cancellation-token compatibility
- Updated documentation for bulk create response modes

v6.0.0

- Added v6 REST coverage for native graph queries, graph transactions, authorization, and request history
- Added v6 request/response models for query, transaction, and authorization workflows
- Updated package metadata for the LiteGraph v6.0.0 release

## Previous Versions

v1.0.0

- Initial release

v1.0.1

- Updated the package name to `litegraphdb`
- Updated the README.md
- Updated the CONTRIBUTING.md
- Updated the CHANGELOG.md
- Updated the LICENSE.md
- Updated the package.json
