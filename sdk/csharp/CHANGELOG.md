# Change Log

## Current Version

v4.0.x

- Refactor to group and simplify APIs
- Multiple bugfixes and QoL improvements

## Previous Versions

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