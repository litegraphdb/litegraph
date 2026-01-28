# Change Log

## Current Version

v5.0.x

- Breaking changes: full API migration to async/await
  - All public methods that perform I/O operations are now async and return `Task` or `Task<T>`
  - Methods returning collections now use `IAsyncEnumerable<T>` where appropriate
  - Existing synchronous code must be updated to use `await` or `.GetAwaiter().GetResult()` for blocking calls
  - `InitializeRepository()` and `Flush()` remain synchronous
  
- New MCP (Model Context Protocol) server (`LiteGraph.McpServer`)
  - Enables AI assistants and LLMs to interact with LiteGraph
  - Exposes graph operations as MCP tools for AI integration
  - Supports HTTP, TCP, and WebSocket transport protocols
  - Docker image available at `jchristn77/litegraph-mcp`
  - Ideal for knowledge graphs, RAG applications, and AI-powered data exploration

## Previous Versions

v4.x

- Major internal refactor for both the graph repository base and the client class
- Separation of responsibilities; graph repository base owns primitives, client class owns validation and cross-cutting
- Consistency in interface API names and behaviors
- Consistency in passing of query parameters such as skip to implementations and primitives
- Consolidation of create, update, and delete actions within a single transaction
- Batch APIs for creation and deletion of labels, tags, vectors, edges, and nodes
- Enumeration APIs
- Statistics APIs
- Simple database caching to offload existence validation for tenants, graphs, nodes, and edges
- In-memory operation with controlled flushing to disk
- Additional vector search parameters including topK, minimum score, maximum distance, and minimum inner product
- Dependency updates and bug fixes
- Minor Postman fixes
- Inclusion of an optional graph-wide HNSW index for graph, node, and edge vectors

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