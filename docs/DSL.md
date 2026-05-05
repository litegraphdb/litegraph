# LiteGraph Native Graph Query Language

LiteGraph native graph query is a Cypher/GQL-inspired query profile with LiteGraph-native semantics. It is graph scoped: every query executes within one tenant and one graph. It is intended for concise graph reads, traversals, supplied-vector search, and graph child object mutations over nodes, edges, labels, tags, and vectors.

The language is not named LiteQL. Use "LiteGraph native graph query" in code and docs until a separate naming decision changes that.

## Execution Model

Queries execute through:

- C# SDK: `client.Query.Execute(tenantGuid, graphGuid, request)`
- REST: `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query`
- MCP: `graph/query` tool over HTTP, TCP, and WebSocket transports
- Console: `lg` command through `LiteGraphConsole`

Every request uses this shape:

```json
{
  "Query": "MATCH (n:Person) WHERE n.data.age >= $age RETURN n LIMIT 10",
  "Parameters": {
    "age": 21
  },
  "MaxResults": 100,
  "TimeoutSeconds": 30,
  "IncludeProfile": false
}
```

Inline literals are allowed for simple values, but parameters are preferred for user input, large arrays, objects, vectors, and values that should preserve JSON types.

The MCP `graph/query` tool accepts `tenantGuid`, `graphGuid`, and either a full `request` object/string with the shape above or the convenience fields `query`, `parameters`, `maxResults`, and `timeoutSeconds`. MCP execution is forwarded to the REST query endpoint so the same authentication, graph scoping, and credential-scope checks apply.

## Scope Rules

- A query executes within one tenant and one graph.
- Cross-tenant queries are not supported.
- Cross-graph queries are not supported.
- Query mutations can mutate graph child objects only: nodes, edges, labels, tags, and vectors.
- Query mutations must not update tenant, user, credential, admin, or graph metadata.
- Vector search uses supplied vectors. LiteGraph does not generate embeddings in this release.

## Parameters

Parameters are referenced with `$name`.

```cypher
MATCH (n:Person) WHERE n.name = $name RETURN n
```

Parameter values preserve JSON-compatible types:

- strings
- numbers
- booleans
- null
- arrays
- objects
- vectors as numeric arrays
- GUIDs as strings or GUID values

Use parameters instead of string interpolation when values come from users or programs.

Missing parameters fail execution with a clear error naming the missing parameter. Unused parameters are accepted and ignored. Type-invalid parameters fail where the query needs a specific type; for example, a non-GUID value used with a `guid` predicate fails as an invalid GUID, and a non-array value used as a vector fails as an invalid vector array.

## Comments

Comments are not part of the implemented language profile yet. Strip comments before sending query text.

## Identifiers

Identifiers name variables, labels, fields, and procedure segments.

```cypher
MATCH (a:Person)-[e:KNOWS]->(b:Person) RETURN a, e, b
```

Variables are local to the query.

## Literals

Supported literal forms:

```cypher
'string'
"string"
42
3.14
true
false
null
```

Objects and arrays should be passed as parameters.

## Node Match

Match all nodes in the graph:

```cypher
MATCH (n) RETURN n
```

Match nodes by label:

```cypher
MATCH (n:Person) RETURN n
```

Filter by GUID:

```cypher
MATCH (n:Person) WHERE n.guid = $nodeGuid RETURN n
```

Filter by name:

```cypher
MATCH (n:Person) WHERE n.name = 'Ada' RETURN n
```

Filter by nested data:

```cypher
MATCH (n:Person) WHERE n.data.profile.age >= 30 RETURN n
```

## Edge Match

Match directed edges:

```cypher
MATCH (a)-[e]->(b) RETURN a, e, b
```

Match directed edges by edge label:

```cypher
MATCH (a)-[e:KNOWS]->(b) RETURN a, e, b
```

Match source or target nodes by GUID:

```cypher
MATCH (a)-[e:KNOWS]->(b) WHERE a.guid = $from RETURN a, e, b
MATCH (a)-[e:KNOWS]->(b) WHERE b.guid = $to RETURN a, e, b
```

Match edge fields:

```cypher
MATCH (a)-[e:KNOWS]->(b) WHERE e.guid = $edgeGuid RETURN e
MATCH (a)-[e:KNOWS]->(b) WHERE e.name = 'Worked With' RETURN e
MATCH (a)-[e:KNOWS]->(b) WHERE e.data.kind = 'collaboration' RETURN e
```

Match source or target node data:

```cypher
MATCH (a:Person)-[e:KNOWS]->(b:Person)
WHERE a.data.role = 'mathematician'
RETURN a, e, b
```

## Fixed Directed Multi-Hop Match

Fixed directed paths are supported:

```cypher
MATCH (a:Person)-[e1:LINKS]->(b:Person)-[e2:LINKS]->(c:Person)
WHERE a.guid = $start
RETURN a, e1, b, e2, c
LIMIT 10
```

## Bounded Variable-Length Paths

Bounded variable-length directed paths are supported with `*min..max` syntax:

```cypher
MATCH (a:Person)-[path:LINKS*1..3]->(c:Person)
WHERE a.guid = $start AND c.guid = $end
RETURN a, path, c
LIMIT 10
```

The relationship variable for a variable-length segment returns a list of full
`Edge` objects for the matched path segment. The start and end variables return
full `Node` objects. Bounds are required in this release to keep traversal
execution predictable; unbounded `*` paths are rejected. The maximum bound is
32 hops.

`MATCH SHORTEST` returns only minimum-hop matches from the bounded candidate
set:

```cypher
MATCH SHORTEST (a:Person)-[path:LINKS*1..5]->(c:Person)
WHERE a.guid = $start AND c.guid = $end
RETURN a, path, c
LIMIT 10
```

Shortest-path execution still obeys the explicit upper bound. If multiple paths
have the same minimum hop count, all minimum-hop paths are eligible and `LIMIT`
controls the returned row count.

## Optional Match

Top-level `OPTIONAL MATCH` is supported for read queries:

```cypher
OPTIONAL MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1
OPTIONAL MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.guid = $start RETURN a, e, b LIMIT 1
```

When an optional match has no rows, LiteGraph returns one row with each returned
variable set to `null`. `OPTIONAL MATCH` does not support `SET` or `DELETE`
mutations in this release.

## Predicates

The current `WHERE` profile supports predicates joined with `AND` and `OR`.
`NOT` can be applied to a predicate or parenthesized predicate expression. `AND`
binds more tightly than `OR`; use parentheses when that is not the intended
grouping.

```cypher
MATCH (n:Person)
WHERE n.data.role = $role AND n.data.profile.age >= 30
RETURN n

MATCH (n:Person)
WHERE n.name = 'Ada' OR n.name = 'Grace'
RETURN n

MATCH (n:Person)
WHERE (n.name = 'Ada' OR n.name = 'Grace') AND NOT n.data.role = 'engineer'
RETURN n
```

`OR` and `NOT` predicates may scan candidate rows when they cannot be safely
pushed down to a repository lookup.

### Equality

Equality is supported for:

- `guid`
- `name`
- `data.<field>`
- `tags.<key>`

```cypher
MATCH (n:Person) WHERE n.guid = $guid RETURN n
MATCH (n:Person) WHERE n.name = $name RETURN n
MATCH (n:Person) WHERE n.data.role = 'engineer' RETURN n
MATCH (n:Person) WHERE n.tags.field = 'math' RETURN n
```

### Numeric Comparison

Numeric comparisons are supported for `data.<field>` values:

```cypher
MATCH (n:Person) WHERE n.data.age > 30 RETURN n
MATCH (n:Person) WHERE n.data.age >= 30 RETURN n
MATCH (n:Person) WHERE n.data.age < 65 RETURN n
MATCH (n:Person) WHERE n.data.age <= 65 RETURN n
```

Both sides must be numeric when a numeric comparison operator is used.

### String Predicates

String predicates are supported for `name` and `data.<field>` values:

```cypher
MATCH (n:Person) WHERE n.name CONTAINS 'Ada' RETURN n
MATCH (n:Person) WHERE n.data.role STARTS WITH 'math' RETURN n
MATCH (a)-[e:KNOWS]->(b) WHERE e.name ENDS WITH 'With' RETURN e
```

String comparisons are ordinal and case-sensitive in the current implementation.

### List Predicates

`IN` tests whether a field equals any value in a literal list or parameter list.

```cypher
MATCH (n:Person) WHERE n.name IN ['Ada', 'Grace'] RETURN n
MATCH (n:Person) WHERE n.guid IN [$adaGuid, $graceGuid] RETURN n
MATCH (n:Person) WHERE n.data.role IN $roles RETURN n
MATCH (n:Person) WHERE n.tags.field IN ['math', 'logic'] RETURN n
```

When a parameter is used with `IN`, it must resolve to an array or enumerable
value. `IN` uses the same equality semantics as `=`, including GUID, Boolean,
numeric, and string equality.

### Tag Predicates

Node and edge tags can be filtered with `tags.<key>` predicates. The tag key is
matched case-insensitively; the tag value uses the selected predicate operator.

```cypher
MATCH (n:Person) WHERE n.tags.field = 'math' RETURN n
MATCH (a)-[e:KNOWS]->(b) WHERE e.tags.kind = 'historical' RETURN e
MATCH (a)-[e:KNOWS]->(b) WHERE a.tags.field = 'math' RETURN a, e, b
```

Tag predicates are evaluated through LiteGraph tag records and may scan
candidate rows when no narrower seed predicate is available.

## Return

`RETURN` lists variables to include in each row.

```cypher
MATCH (n:Person) RETURN n
MATCH (a)-[e:KNOWS]->(b) RETURN a, e, b
```

Node rows return full `Node` objects. Edge rows return full `Edge` objects and can include source and target `Node` objects when those variables are returned. Label, tag, vector, and vector search rows also return full LiteGraph objects where applicable.

### Aggregates

Aggregate `RETURN` items are supported for read-only `MATCH` node, edge, and
fixed-path queries. Aggregate returns cannot be mixed with graph variable
returns in the same query in this release.

Supported aggregate functions:

- `COUNT(*)`
- `COUNT(variable)` and `COUNT(variable.field)`
- `SUM(variable.field)`
- `AVG(variable.field)`
- `MIN(variable.field)`
- `MAX(variable.field)`

Examples:

```cypher
MATCH (n:Person) RETURN COUNT(*) AS total
MATCH (n:Person) RETURN COUNT(n.data.profile.age) AS aged, AVG(n.data.profile.age) AS averageAge
MATCH (a)-[e:KNOWS]->(b) RETURN COUNT(e) AS edges, SUM(e.cost) AS totalCost
MATCH (a)-[e:KNOWS]->(b) WHERE a.tags.field = 'math' RETURN COUNT(*) AS paths, MAX(e.tags.kind) AS pathKind
```

Aggregate field paths support the same object fields used by `ORDER BY` plus
`tags.<key>` for node and edge tag values. `SUM` and `AVG` require numeric
values. `COUNT(field)` counts non-null field values. `MIN` and `MAX` use the
same ordering rules as `ORDER BY`.

Aggregate queries scan up to `LIMIT` or `MaxResults` and return one scalar row.
Use an explicit `LIMIT` or raise `MaxResults` when the aggregate needs to cover
a larger candidate set.

## Result Metadata

Query results include:

- `Profile`: query language profile name
- `Mutated`: whether graph child objects were changed
- `ExecutionTimeMs`: elapsed execution time in milliseconds
- `ExecutionProfile`: optional parse, plan, execute, and total timings when `IncludeProfile` is true
- `Warnings`: planner or execution warnings
- `Plan`: compact plan summary
- `Rows`: result rows keyed by return variable
- `Nodes`, `Edges`, `Labels`, `Tags`, `Vectors`, `VectorSearchResults`: typed object lists
- `RowCount`: number of result rows

The plan summary includes the query kind, mutation/vector/order/limit flags, estimated relative cost, and repository seed information when a predicate can start from a narrow repository read.

Set `IncludeProfile` to `true` when a caller needs phase timings for debugging or optimization:

```json
{
  "Query": "MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 10",
  "Parameters": {
    "name": "Ada Lovelace"
  },
  "IncludeProfile": true
}
```

When enabled, `ExecutionProfile` contains:

- `ParseTimeMs`
- `PlanTimeMs`
- `ExecuteTimeMs`
- `TotalTimeMs`

## Ordering and Limit

Limit result rows:

```cypher
MATCH (n:Person) RETURN n LIMIT 25
```

Order returned rows:

```cypher
MATCH (n:Person) RETURN n ORDER BY n.name ASC LIMIT 10
MATCH (n:Person) RETURN n ORDER BY n.data.profile.age DESC LIMIT 10
```

Supported object sort fields:

- node: `guid`, `name`, `data.<field>`
- edge: `guid`, `name`, `cost`, `data.<field>`
- label: `guid`, `label`
- tag: `guid`, `key`, `value`
- vector: `guid`, `model`, `content`, `dimensionality`
- vector search result: `score`, `distance`, `innerProduct`

When `ORDER BY` is present, LiteGraph scans up to `MaxResults`, sorts those rows, and then applies `LIMIT`.

## Create Nodes

Create a node:

```cypher
CREATE (n:Person { name: $name, data: $data }) RETURN n
```

Supported node properties:

- `name`
- `data`

The label in `(n:Person)` is assigned to the created node.

## Create Edges

Create an edge:

```cypher
CREATE ()-[e:KNOWS { from: $from, to: $to, name: $name, data: $data }]->() RETURN e
```

Supported edge properties:

- `from` or `fromGuid`
- `to` or `toGuid`
- `name`
- `data`

The label in `[e:KNOWS]` is assigned to the created edge.

## Create Labels

Create a label object:

```cypher
CREATE LABEL l { nodeGuid: $node, label: 'Scientist' } RETURN l
CREATE LABEL l { edgeGuid: $edge, label: 'RELATED_TO' } RETURN l
```

Supported properties:

- `nodeGuid`
- `edgeGuid`
- `label`

## Create Tags

Create a tag object:

```cypher
CREATE TAG t { nodeGuid: $node, key: 'field', value: 'math' } RETURN t
CREATE TAG t { edgeGuid: $edge, key: 'source', value: 'archive' } RETURN t
```

Supported properties:

- `nodeGuid`
- `edgeGuid`
- `key`
- `value`

## Create Vectors

Create a vector object with supplied embeddings:

```cypher
CREATE VECTOR v {
  nodeGuid: $node,
  model: 'touchstone-query',
  embeddings: $embedding,
  content: 'Ada vector'
} RETURN v
```

Supported properties:

- `nodeGuid`
- `edgeGuid`
- `model`
- `content`
- `embeddings` or `vectors`

## Update Nodes and Edges

Update a node by GUID:

```cypher
MATCH (n:Person) WHERE n.guid = $node SET n.name = $name, n.data = $data RETURN n
```

Supported node `SET` properties:

- `name`
- `data`

Update an edge by GUID:

```cypher
MATCH ()-[e:LINKS]->() WHERE e.guid = $edge SET e.name = $name, e.cost = 7 RETURN e
```

Supported edge `SET` properties:

- `name`
- `data`
- `cost`

## Delete Nodes and Edges

Delete an edge by GUID:

```cypher
MATCH ()-[e:LINKS]->() WHERE e.guid = $edge DELETE e RETURN e
```

Delete a node by GUID:

```cypher
MATCH (n:Person) WHERE n.guid = $node DELETE n RETURN n
```

## Update and Delete Labels, Tags, and Vectors

Update label:

```cypher
MATCH LABEL l WHERE l.guid = $label SET l.label = $value RETURN l
```

Update tag:

```cypher
MATCH TAG t WHERE t.guid = $tag SET t.value = $value RETURN t
```

Update vector:

```cypher
MATCH VECTOR v WHERE v.guid = $vector SET v.content = $content, v.embeddings = $embedding RETURN v
```

Delete objects:

```cypher
MATCH LABEL l WHERE l.guid = $label DELETE l RETURN l
MATCH TAG t WHERE t.guid = $tag DELETE t RETURN t
MATCH VECTOR v WHERE v.guid = $vector DELETE v RETURN v
```

Mutation queries for labels, tags, and vectors require a GUID equality predicate in this release.

## Vector Search

Search nodes with a supplied vector:

```cypher
CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 5
```

Other procedure names reserved by the parser:

```cypher
CALL litegraph.vector.searchEdges($embedding) YIELD edge, score RETURN edge, score LIMIT 5
CALL litegraph.vector.searchGraph($embedding) YIELD result RETURN result LIMIT 5
```

Node, edge, and graph vector search all use supplied embeddings. Graph vector search is still scoped to the one graph selected for the query session, even though lower-level vector APIs can search across a tenant. Embedding generation is outside the query language.

When graph vector indexing is enabled, node vector search uses the configured LiteGraph vector index for eligible searches. Searches with labels, tags, or expression filters may fall back to the repository implementation.

Return variables supported by vector search:

- `node` or `n`
- `edge` or `e`
- `graph` or `g`
- `score`
- `distance`
- `innerProduct`
- `result`

## Error Handling

Parser errors include line and column information. Execution errors describe unsupported query clauses, unsupported fields, missing parameters, invalid GUID values, nonnumeric comparison operands, and unsupported return variables.

## Benchmark Scenarios

Use these scenarios when comparing native query execution to equivalent REST or
SDK multi-call sequences:

- one-hop edge expansion from a known source node:
  `MATCH (a)-[e:LINKS]->(b) WHERE a.guid = $start RETURN a, e, b LIMIT 100`
- fixed two-hop traversal:
  `MATCH (a)-[e1:LINKS]->(b)-[e2:LINKS]->(c) WHERE a.guid = $start RETURN a, e1, b, e2, c LIMIT 100`
- bounded variable-length traversal:
  `MATCH (a)-[path:LINKS*1..3]->(c) WHERE a.guid = $start RETURN a, path, c LIMIT 100`
- data-filtered node lookup:
  `MATCH (n:Person) WHERE n.data.profile.age >= $age RETURN n LIMIT 100`
- supplied-vector search:
  `CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 10`

For each benchmark, record total elapsed time, repository operation count when
profiling is enabled, returned row count, graph size, and whether vector indexes
are enabled.

## Current Limitations

The current language profile does not yet include:

- unbounded variable-length paths
- query chaining across multiple `MATCH`/`OPTIONAL MATCH` clauses
- vector-index-aware planning beyond the current supplied-vector search path

SQLite and PostgreSQL provider execution are covered by the storage/query layers in v6.0.0. MySQL and SQL Server remain future provider work.

## Practical Examples

These examples are parser-covered in the shared automated suite.

Match all nodes:

```cypher
MATCH (n) RETURN n
```

Match nodes by label:

```cypher
MATCH (n:Person) RETURN n
```

Find a node by GUID:

```cypher
MATCH (n:Person) WHERE n.guid = $nodeGuid RETURN n
```

Find a person by name:

```cypher
MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1
```

Find people by nested numeric data:

```cypher
MATCH (n:Person) WHERE n.data.profile.age >= 30 RETURN n
```

Match directed edges:

```cypher
MATCH (a)-[e]->(b) RETURN a, e, b
```

Find collaborations from a source node:

```cypher
MATCH (a)-[e:KNOWS]->(b) WHERE a.guid = $from RETURN a, e, b LIMIT 25
```

Find collaborations to a target node:

```cypher
MATCH (a)-[e:KNOWS]->(b) WHERE b.guid = $to RETURN a, e, b LIMIT 25
```

Find an edge by GUID:

```cypher
MATCH (a)-[e:KNOWS]->(b) WHERE e.guid = $edgeGuid RETURN e
```

Find edges by data:

```cypher
MATCH (a)-[e:KNOWS]->(b) WHERE e.data.kind = 'collaboration' RETURN e
```

Find a fixed two-hop path:

```cypher
MATCH (a:Person)-[e1:LINKS]->(b:Person)-[e2:LINKS]->(c:Person)
WHERE a.guid = $start
RETURN a, e1, b, e2, c
LIMIT 10
```

Find bounded variable-length paths:

```cypher
MATCH (a:Person)-[path:LINKS*1..3]->(c:Person)
WHERE a.guid = $start AND c.guid = $end
RETURN a, path, c
LIMIT 10
```

Find shortest bounded paths:

```cypher
MATCH SHORTEST (a:Person)-[path:LINKS*1..5]->(c:Person)
WHERE a.guid = $start AND c.guid = $end
RETURN a, path, c
LIMIT 10
```

Return a null row for an absent optional match:

```cypher
OPTIONAL MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1
```

Find people by role and age:

```cypher
MATCH (n:Person)
WHERE n.data.role = 'mathematician' AND n.data.profile.age >= 30
RETURN n
ORDER BY n.name ASC
LIMIT 50
```

Find people by Boolean and list predicates:

```cypher
MATCH (n:Person)
WHERE (n.name = 'Ada' OR n.name = 'Grace') AND NOT n.data.role = 'engineer'
RETURN n

MATCH (n:Person)
WHERE n.name IN ['Ada', 'Grace']
RETURN n
```

Find people or edges by tags:

```cypher
MATCH (n:Person) WHERE n.tags.field = 'math' RETURN n
MATCH (a)-[e:KNOWS]->(b) WHERE e.tags.kind = 'historical' RETURN e
```

Count and summarize matches:

```cypher
MATCH (n:Person) RETURN COUNT(*) AS total
MATCH (n:Person) RETURN COUNT(n.data.profile.age) AS aged, AVG(n.data.profile.age) AS averageAge
MATCH (a)-[e:KNOWS]->(b) RETURN COUNT(e) AS edges, SUM(e.cost) AS totalCost
```

Create a node:

```cypher
CREATE (n:Person { name: 'Ada', data: $data }) RETURN n
```

Create an edge:

```cypher
CREATE ()-[e:KNOWS { from: $from, to: $to, name: $name, data: $data }]->() RETURN e
```

Create a label:

```cypher
CREATE LABEL l { nodeGuid: $node, label: 'Scientist' } RETURN l
```

Create a tag:

```cypher
CREATE TAG t { nodeGuid: $node, key: 'field', value: 'math' } RETURN t
```

Create a vector:

```cypher
CREATE VECTOR v { nodeGuid: $node, model: 'touchstone-query', embeddings: $embedding, content: 'Ada vector' } RETURN v
```

Update a node:

```cypher
MATCH (n:Person) WHERE n.guid = $node SET n.data = $data RETURN n
```

Update an edge:

```cypher
MATCH ()-[e:LINKS]->() WHERE e.guid = $edge SET e.name = $name, e.cost = 7 RETURN e
```

Delete an edge:

```cypher
MATCH ()-[e:KNOWS]->() WHERE e.guid = $edge DELETE e RETURN e
```

Delete a node:

```cypher
MATCH (n:Person) WHERE n.guid = $node DELETE n RETURN n
```

Update a label:

```cypher
MATCH LABEL l WHERE l.guid = $label SET l.label = $value RETURN l
```

Update a tag:

```cypher
MATCH TAG t WHERE t.guid = $tag SET t.value = $value RETURN t
```

Update a vector:

```cypher
MATCH VECTOR v WHERE v.guid = $vector SET v.content = $content, v.embeddings = $embedding RETURN v
```

Delete a label:

```cypher
MATCH LABEL l WHERE l.guid = $label DELETE l RETURN l
```

Delete a tag:

```cypher
MATCH TAG t WHERE t.guid = $tag DELETE t RETURN t
```

Delete a vector:

```cypher
MATCH VECTOR v WHERE v.guid = $vector DELETE v RETURN v
```

Search node vectors:

```cypher
CALL litegraph.vector.searchNodes($embedding) YIELD node, score RETURN node, score LIMIT 5
```

Search edge vectors:

```cypher
CALL litegraph.vector.searchEdges($embedding) YIELD edge, score RETURN edge, score LIMIT 5
```

Search graph vectors:

```cypher
CALL litegraph.vector.searchGraph($embedding) YIELD result RETURN result LIMIT 5
```
