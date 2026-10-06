# Change Log

## Current Version

v10.2.0

- Native AOT and trimming support: the package sets `IsAotCompatible` and builds with no trim or AOT warnings. See [Native AOT and trimming](../../docs/AOT.md).
- Added `LiteGraphSdkJsonContext`, source-generated metadata for every SDK model type, enumeration result, and list. `Serializer` resolves types from it first, then from resolvers added with the new `Serializer.AddTypeInfoResolver`, then from reflection only when reflection-based serialization is enabled, so JIT applications behave exactly as before.
- Added `Serializer.DeserializeJson<T>(string, JsonTypeInfo<T>)`.
- `TestEndpoint`, `PreloadEndpoint`, `RebuildVectorIndex`, and `SubmitFeedback` send dictionaries instead of anonymous objects (same JSON), so they work under Native AOT.
- Serializer options are cached instead of rebuilt on every call; `IncludeNullProperties` and `DateTimeFormat` behave as before.
- Serialized JSON for every model type is unchanged from 10.1.0.
- Fix: on machines not set to UTC, timestamps in responses were parsed into local time (with the local clock reading), so a value read from the server and sent back moved by the local UTC offset. Timestamps now parse as UTC, and local `DateTime` values are converted to UTC before they are sent.

## Previous Versions

v10.1.0

- RestWrapper 3.3.0 → 3.3.1. No API changes.

v10.0.0

- Added cluster administration: `ReadClusterNodes`, `ReadClusterNode`, `RestartCluster`, `RestartClusterNode`, `DeleteClusterNode`, and the `ClusterStatus`, `ClusterNode`, `ClusterNodeChecks`, `ClusterNodeStateEnum`, and `ClusterRestartResult` models
- `RestartServer` returns a `ClusterRestartResult` (a rolling restart in cluster mode)
- Added `HealthLive` and `HealthReady` with the `HealthResponse` model; readiness returns the body for 503 as well as 200
- Added a retry policy: `MaxRetries` (default 2), `RetryBaseDelayMs` (default 200, exponential with jitter, capped at 5000 ms), and `RetryPost` (default false); connection failures and 502/503/504 are retried for GET, HEAD, PUT, and DELETE
- Added `LastNodeId`, from the `x-litegraph-node` response header
- `SettingsUpdateResult` gains `EnvironmentOverrides` and `SettingsVersion`
- Added `ReadClusterLocks` and `ReadClusterJobs` with the `ClusterLock`, `ClusterLockList`, `ClusterJobRun`, and `ClusterJobList` models
- `HealthResponse` gains `StorageProvider` and `VectorIndexProvider`
- Added request history on `sdk.RequestHistory`: `Search` (one page, with filters including `NodeId`), `Enumerate` (all pages), `ReadByGuid`, `ReadDetail`, `ReadSummary`, `DeleteByGuid`, and `DeleteMany`, with the `RequestHistoryEntry`, `RequestHistoryDetail`, `RequestHistorySearchRequest`, `RequestHistorySummary`, `RequestHistorySummaryBucket`, and `RequestHistoryDeleteResult` models, and `SdkBase.DeleteWithResult<T>`

v7.0.0

- Added v7 graph transaction diagnostics, lifecycle state, and isolation-level models
- Added transaction execution helpers aligned with the REST v7 transaction response body
- Updated package metadata for the LiteGraph v7.0.0 release

v6.0.2

- Added minimal/full bulk create return modes for label, tag, vector, node, and edge APIs
- Added C# SDK overloads for bulk create return mode selection
- Updated documentation for bulk create response modes

v6.0.0

- Added v6 REST coverage for native graph queries, graph transactions, authorization, and request history
- Added v6 request/response models and client helpers
- Updated package metadata for the LiteGraph v6.0.0 release

## Previous Versions

v4.0.x

- Refactor to group and simplify APIs
- Multiple bugfixes and QoL improvements

v3.1.x

- Added support for labels on graphs, nodes, edges (string list)
- Added support for vector persistence and search
- Updated SDK, test, and Postman collections accordingly
- Updated GEXF export to support labels and tags
- Internal refactor to reduce code bloat
- Multiple bugfixes and QoL improvements


v3.0.x

- Major internal refactor to support multitenancy and authentication, including tenants (`TenantMetadata`), users (`UserMaster`), and credentials (`Credential`)
- Graph, node, and edge objects are now contained within a given tenant (`TenantGUID`)
- Extensible key and value metadata (`TagMetadata`) support for graphs, nodes, and edges
- Schema changes to make column names more accurate (`id` becomes `guid`)
- Setup script to create default records
- Environment variables for webserver port (`LITEGRAPH_PORT`) and database filename (`LITEGRAPH_DB`)
- Moved logic into a protocol-agnostic handler layer to support future protocols
- Added last update UTC timestamp to each object (`LastUpdateUtc`)
- Authentication using bearer tokens (`Authorization: Bearer [token]`)
- System administrator bearer token defined within the settings file (`Settings.LiteGraph.AdminBearerToken`) with default value `litegraphadmin`
- Tag-based retrieval and filtering for graphs, nodes, and edges
- Updated SDK and test project
- Updated Postman collection

v2.1.0

- Added batch APIs for existence, deletion, and creation
- Minor internal refactor 

v2.0.0

- Major overhaul, refactor, and breaking changes
- Integrated webserver and RESTful API
- Extensibility through base repository class
- Hierarchical expression support while filtering over graph, node, and edge data objects
- Removal of property constraints on nodes and edges

v1.0.0

- Initial release
