# AOT Readiness Plan for LiteGraph

Status: **implemented in v10.2.0**, including Phase 6 (the REST server, MCP server, and console tools). Archived: the
current guide is [docs/AOT.md](../docs/AOT.md), and §0 below records how the implementation differs from the plan.

Goal: an application that references LiteGraph and publishes with `PublishAot=true` builds with **zero trim or AOT
warnings that come from LiteGraph** and runs correctly on SQLite and PostgreSQL. Behavior under the JIT (all current
users) does not change, and the public API has no breaking changes.

---

## 0. Implementation Notes (v10.2.0)

Results:
- `IsAotCompatible=true` on `LiteGraph` (net8.0, net10.0) and `LiteGraph.Sdk` (net8.0), zero IL warnings.
- `src/Test.Aot` publishes as Native AOT with trim and AOT warnings treated as errors, and passes 29/29 checks on
  SQLite and PostgreSQL for both net8.0 and net10.0.
- The C# SDK suite (157 cases) passes as a Native AOT binary against a live server.
- `Aot.Serialization` confirms byte-identical JSON for 98 model and data shapes against baselines captured before any
  change.
- The full Touchstone run (SQLite and PostgreSQL) passes. The only skips are the three Redis cluster cases, which need
  Redis.

Departures from the plan, and why:

1. **`DataTable.Load` was kept, not replaced (§4 Phase 3).** `DataTable.Load` uses `MissingSchemaAction.AddWithKey`:
   for single-table results it adds primary-key and unique constraints from the reader's schema, and merges rows that
   share a key. A JOIN that selects only `nodes.*` reports one base table, so a hand-written loader would return
   duplicate rows (or skip a unique-constraint failure) where `Load` does not. Copying rows by hand would therefore
   change behavior. Instead, every call goes through `GraphRepositories/DataTableLoader.cs`, which suppresses IL2026
   with a justification: the annotation exists for DataColumn expressions, which LiteGraph never creates. Native AOT
   runs on both providers confirm it works.
2. **`LiteGraphJsonContext` is public, not internal (§3.1).** Consumers, the server, and Durable.LiteGraph can add it
   to their own option chains, which they could not do with an internal type.
3. **The exception converter keeps its reflection output under the JIT (§4 Phase 1.4).** The plan accepted a shape
   change for everyone. Instead, the explicit-field writer runs only when reflection-based serialization is disabled,
   so JIT output is unchanged (pinned by `Aot.JitCompatibility`). The fixed shape applies only under Native AOT.
4. **Provider error codes keep a reflection fallback (§4 Phase 2).** `DbException.SqlState` and
   `SqliteException.SqliteErrorCode` are read directly. The full test run showed that callers rely on any exception with
   a `SqlState` property being recognized, so the original name-based lookup remains as a best-effort fallback
   (suppressed, documented) for other types.
5. **The C# SDK was included.** It had the same reflection-based serializer (15 warnings). It also sent anonymous
   objects as request bodies in `TestEndpoint`, `PreloadEndpoint`, `RebuildVectorIndex`, and `SubmitFeedback`, which
   only showed up when its suite ran as a Native AOT binary.
6. **The Phase 0 baselines were captured as planned, before the serializer changed.** Two refinements came out of
   making them stable:
   - Freshly constructed `Timestamp` values are masked in the round-trip output.
   - The GEXF fixture uses one label and one tag per object, because SQLite returns several in no fixed order.
   The SDK had no baseline commit, so its parity was checked by running one snapshot program against the 10.1.0 SDK
   (git worktree) and the new SDK: 2,126 of 2,127 lines are identical, and the remaining line deliberately uses random
   GUIDs.
7. **More shapes registered for untyped data.** `T[]` and `List<T>` of common primitives are registered in both
   contexts, so `Dictionary<string, object>` values built from them work under Native AOT.

Fixed alongside (pre-existing, unrelated to AOT): the `DateTimeConverter` in both serializers read `...Z` timestamps
with `DateTime.TryParse`, which converts them to local time; writing that value back appended `Z` to a local time, so
JSON round trips were off by the machine's UTC offset unless the process ran in UTC. The converters now parse with
`AdjustToUniversal | AssumeUniversal` (results are UTC; a value without a zone is taken as UTC) and convert local
times to UTC before writing. The cluster registry's compensating `ToUniversalTime()` calls were removed. The parity
suite now compares round trips in any time zone, `Aot.DateTimeUtc` covers the cases directly, and CI runs the suite
outside UTC.

---

## 1. Findings (measured, not inferred)

All numbers below come from builds in this repository at commit `5eac6f3`.

### 1.1 Analyzer build of the library

`dotnet build src/LiteGraph/LiteGraph.csproj -f net10.0 -p:IsAotCompatible=true` reports **56 warnings**
(32 × IL2026, 21 × IL3050, 3 × IL2075):

| Area | File(s) | Warnings | Cause |
|---|---|---|---|
| Central serializer | `Serialization/Serializer.cs` (43, 50, 76, 96, 118, 187, 307) | 13 | Reflection-mode `JsonSerializer` calls; non-generic `JsonStringEnumConverter`; `ExceptionConverter` reflects over `GetType().GetProperties()`; `NameValueCollectionConverter` and `ExpressionConverter` call back into reflection-mode `JsonSerializer` |
| `DataTable.Load` | `SqliteGraphRepository.cs` (680, 706, 736, 812, 838, 868, 945, 999, 1059, 1371, 1431), `PostgresqlGraphRepository.Execution.cs` (264) | 12 | `DataTable.Load(IDataReader)` is `[RequiresUnreferencedCode]` (expression columns) |
| Transactions | `Client/Implementations/TransactionMethods.cs` (569–572, 623, 629, 677) | 10 | `ConvertPayload<T>` / `TryExtractGuidFromPayload` use reflection-mode `JsonSerializer`; `ReadStringProperty` / `ReadIntProperty` use `exception.GetType().GetProperty(...)` to read `SqlState` / `SqliteErrorCode` |
| HNSW index files | `Indexing/Vector/HnswLiteVectorIndex.cs` (260, 793, 826) | 6 | `JsonSerializer.Serialize/Deserialize` of `VectorIndexStatistics` / `HnswIndexState` |
| Query engine | `Client/Implementations/QueryExecutionEngine.cs` (3102, 3142, 3144), `Query/Parser.cs` (1130) | 8 | `List<string>`, `Dictionary<string, object>`, and `object` through reflection-mode `JsonSerializer` |
| GEXF export | `Gexf/GexfWriter.cs` (113, 141) | 4 | `XmlSerializer`, which needs runtime code generation and **cannot work under Native AOT at all** |
| Algorithms | `AlgorithmMethods.cs` (233), `Algorithms/GraphProjectionExporter.cs` (175) | 4 | `SerializeToNode(object)` / `SerializeToUtf8Bytes(object)` on `Data` |

### 1.2 Native AOT publish of a consumer app

The probe was a console app with `PublishAot=true`, a reference to LiteGraph, and `TrimmerRootAssembly Include="LiteGraph"`,
so the compiler analyzed every code path, PostgreSQL and HNSW included. It produced **68 warnings**: the 56 above plus
**12 that appear only at publish time**. Those 12 come from `[JsonConverter(typeof(JsonStringEnumConverter))]` on these
enums: `AuthorizationPermissionEnum`, `AuthorizationResourceScopeEnum`, `AuthorizationResourceTypeEnum`, `BuiltInRoleEnum`,
`ChatEndpointTypeEnum`, `ChatFeedbackRatingEnum`, `ChatProviderTypeEnum`, `Coordination/LockModeEnum`,
`Algorithms/GraphAlgorithmTypeEnum`, `Algorithms/GraphExportAttributeLevelEnum`, `Algorithms/GraphExportFormatEnum`,
`Indexing/Vector/VectorIndexTypeEnum`.

**None of the warnings came from a dependency.** Npgsql 10 (including `NpgsqlDataSourceBuilder` + `UseVector()`),
Pgvector, Microsoft.Data.Sqlite, HnswLite, Caching, ExpressionTree, Padlock, PrettyId, RestWrapper, SyslogLogging, Timestamps
and WhatTimeIsIt all compiled clean. `Regex` use in `PostgresqlSqlTranslator.cs` produced no warnings either.

### 1.3 Runtime behavior of the AOT binary

The probe ran `InitializeRepository()` and then created a tenant, a graph, and a node.

1. With default AOT settings it fails at startup:
   `InvalidOperationException: Reflection-based serialization has been disabled for this application`, thrown from
   `Serializer.SerializeJson` ← `AuthorizationRoleQueries.InsertRole` ← `SqliteGraphRepository.EnsureBuiltInAuthorizationRoles`.
2. With `JsonSerializerIsReflectionEnabledByDefault=true` it fails at the same point with
   `ListOfTConverter<List<AuthorizationPermissionEnum>, AuthorizationPermissionEnum> is missing native code or metadata`.

So turning reflection back on is not a workaround. Source-generated metadata is required.

### 1.4 Verified building blocks

These patterns were compiled with `IsAotCompatible=true` on both `net8.0` and `net10.0` and produced **zero warnings**:

- Deserializing through `options.GetTypeInfo(typeof(T))`:
  `JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)))`.
- Serializing through `options.GetTypeInfo(obj.GetType())`.
- Filling a `DataTable` by hand: add the columns from `reader.GetName(i)` / `reader.GetFieldType(i)`, then
  `BeginLoadData()`, `GetValues` / `Rows.Add` in a loop, then `EndLoadData()`.

---

## 2. Review of the other agent's feedback

| Claim | Verdict | Notes |
|---|---|---|
| All JSON goes through `Serializer.cs`, which is reflection-only with no `JsonSerializerContext` | **Partly right** | `Serializer.cs` is the main path and has no context anywhere. But seven other files call `JsonSerializer` directly (§1.1), and the agent missed all of them: `TransactionMethods`, `HnswLiteVectorIndex`, `QueryExecutionEngine`, `Parser`, `AlgorithmMethods`, `GraphProjectionExporter`, `GexfWriter`. |
| Fails at startup while seeding built-in roles | **Correct** | Reproduced. It first throws on **write** (`AuthorizationRoleQueries.InsertRole`), not on read-back through `Converters.GetDataRowJsonListValue<T>`. The read path has the same problem, but the write fails first. |
| `ListOfTConverter<List<AuthorizationPermissionEnum>>` missing native code once reflection is re-enabled | **Correct** | Reproduced exactly. |
| `ExceptionConverter` reflects over properties | **Correct** | IL2075 + IL2026/IL3050 at `Serializer.cs:96/118`. |
| Non-generic `JsonStringEnumConverter` is not AOT-safe | **Correct but incomplete** | Beyond the options-level converter at `Serializer.cs:76`, 12 enums carry `[JsonConverter(typeof(JsonStringEnumConverter))]` attributes (§1.2). |
| `Node.Data` / `Edge.Data` typed `object` must become `JsonNode`/`JsonElement` | **Disagree: not required** | Under source generation, an `object` property is serialized by its run-time type, looked up in the resolver. Reads already produce `JsonElement` (`DeserializeJson<object>`). Registering `JsonElement`, `JsonNode`/`JsonObject`/`JsonArray`, primitives, `Dictionary<string, object>` and `List<object>` covers everything LiteGraph itself produces. Callers that put their own POCOs in `Data` can register their own resolver (§4.1). Changing the type would break every consumer to fix a problem that only AOT users have. `Graph.Data`, `TransactionOperation.Payload`, `TransactionOperationResult.Result` and `JsonlRecord.Object` fall in the same category and are handled the same way. |
| `ExpressionConverter` recursion uses reflection | **Correct, trivial fix** | Replace `JsonSerializer.Deserialize<Expr>(ref reader, …)` with a direct recursive `Read(ref reader, typeof(Expr), options)`. |
| `DataTable.Load` is not trim-safe, so rewrite reads onto `DbDataReader` | **Right diagnosis, wrong size of fix** | `DataTable`/`DataRow` appear about 1,240 times across 44 files. Only the **12** `DataTable.Load` calls carry the warning. One hand-written loader helper (§1.4, verified warning-free) fixes all 12 without touching the converters. A `DbDataReader` rewrite may still be worth doing for speed, but it is optional and not part of this plan. |
| Regex in the PostgreSQL translator, plus Npgsql, Caching and RestWrapper, add warnings | **Not reproduced** | With the whole assembly rooted, zero warnings came from any dependency or from `Regex` (§1.2). Those warnings were probably from a different app's dependency graph (e.g. Durable or the server). |
| "About 40 errors in total" | **Undercounted** | 56 at library build time, 68 at publish (§1). |
| Not mentioned | **Missing** | `XmlSerializer` in `GexfWriter` (cannot work under AOT and must be rewritten); `CopyObject<T>` swallows exceptions and returns `default`, so under AOT an unregistered `T` would **silently return null** instead of throwing (`UserMaster.Redact`, server settings copies); 46 `new Serializer()` call sites rely on shared static options, which matters for how consumers can add resolvers. |
| Set `IsAotCompatible` and work through the warnings | **Agree** | That is the exit criterion (§5). |

---

## 3. Design decisions

1. **One source-generated context, `LiteGraphJsonContext`** (`src/LiteGraph/Serialization/LiteGraphJsonContext.cs`),
   `internal sealed partial class LiteGraphJsonContext : JsonSerializerContext`, with
   `[JsonSourceGenerationOptions(UseStringEnumConverter = true)]` and `[JsonSerializable]` entries for:
   - every public model type that is persisted, returned, or copied: all public classes in the root `LiteGraph`
     namespace (≈95 files), plus `Algorithms`, `Indexing/Vector` (`HnswIndexState`, `VectorIndexStatistics`,
     `VectorIndexConfiguration`), `Coordination`, `Subgraph`, and `Query` result types;
   - the collection shapes used in persistence: `List<AuthorizationPermissionEnum>`,
     `List<AuthorizationResourceTypeEnum>`, `List<string>`, `List<Guid>`, `Dictionary<string, string>`,
     `Dictionary<string, int>`, `Dictionary<string, object>`, `List<object>`;
   - the dynamic-data types: `object`, `JsonElement`, `JsonNode`, `JsonObject`, `JsonArray`, `JsonValue`, and the primitives
     (`string`, `long`, `int`, `double`, `decimal`, `bool`, `Guid`, `DateTime`, `DateTimeOffset`);
   - `ExpressionTree.Expr` (its shape is still handled by `ExpressionConverter`).

   The context stays `internal` so the generated surface is not public API. The option shapes (`WhenWritingNull`,
   `WriteIndented`) stay on the runtime `JsonSerializerOptions`. The context is used only as the `TypeInfoResolver`, so
   the existing custom converters keep taking precedence.

2. **Keep the public `ISerializer` / `Serializer` API unchanged.** Internally:
   - `DeserializeJson<T>` → `JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)))`.
   - `SerializeJson(object)` → `JsonSerializer.Serialize(obj, options.GetTypeInfo(obj.GetType()))`.
   - An unregistered type then throws `NotSupportedException` naming the type, not a reflection-disabled error from deep
     inside STJ.

3. **Keep JIT behavior identical.** The options' resolver is a chain:
   `LiteGraphJsonContext.Default` → user-registered resolvers (§4.1) → `DefaultJsonTypeInfoResolver`, where the last one
   is added **only when** `JsonSerializer.IsReflectionEnabledByDefault` is true. Under the JIT that flag is true, so
   anonymous types and arbitrary POCOs in `Data` and in `ConvertData<T>` keep working. Under AOT the trimmer sets it to false
   and removes the fallback. The one place that constructs `DefaultJsonTypeInfoResolver` gets
   `[UnconditionalSuppressMessage("Trimming", "IL2026")]` and `[UnconditionalSuppressMessage("AOT", "IL3050")]`, with a
   justification that names the feature switch.

4. **Make `CopyObject<T>` fail loudly.** It must not swallow `NotSupportedException`: catch only `JsonException`, or
   rethrow when the type is unregistered. Otherwise AOT turns a missing registration into a silent `null`.

5. **No breaking changes to `Data` / `Payload` / `Result` / `Object`.** They stay `object`. The AOT contract is
   documented: under Native AOT these properties accept `JsonElement`, `JsonNode`, primitives, `Dictionary<string, object>`,
   `List<object>`, LiteGraph model types, or any type the application registers through §4.1.

6. **Enum handling.** Remove the non-generic converter from `AddCommonConverters`; `UseStringEnumConverter = true` on the
   context covers every registered enum. On the 12 attributed enums, change the attribute to
   `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]`, so the attribute stays correct for consumers that serialize
   these types with their own options. The wire format (enum names as strings) does not change.

---

## 4. Work plan

### Phase 0: Safety net (before any behavior change)

1. **Capture a serialization baseline under the current reflection serializer.** Add a Touchstone suite
   `AotSerializationParity` in `src/Test.Shared`. For each registered type, build a representative populated instance
   (including `Data` holding a nested `JsonElement`, enums, `NameValueCollection`, `Expr` trees, vectors, null and
   non-null optionals) and record `SerializeJson(x, false)` and `SerializeJson(x, true)` as golden strings. After the switch,
   the suite asserts byte-for-byte equality and a round trip through `DeserializeJson<T>`.
2. **Add `src/Test.Aot`**: a console project with `PublishAot=true`, `TrimmerRootAssembly Include="LiteGraph"`,
   `TrimmerSingleWarn=false`, and `WarningsAsErrors` for `IL2026;IL2046;IL2057;IL2067;IL2070;IL2072;IL2075;IL2087;IL2091;IL3050;IL3051`.
   It runs one end-to-end scenario and returns a nonzero exit code on any failure:
   - SQLite: `InitializeRepository`; tenant, user, credential and role CRUD; graph/node/edge CRUD with `Data` as `JsonElement`
     and `JsonObject`; labels, tags, vectors; vector search with and without an HNSW index; expression filters (`Expr`);
     the graph query language (parameters, list literals); a transaction batch, including one that fails with a provider error;
     GEXF export; JSONL export and import; graph projection export; one algorithm; HNSW index save and load.
   - PostgreSQL: the same scenario when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is set.

   It is expected to fail until Phase 4. Add it to the solution but not to CI yet.

### Phase 1: Central serializer (`Serialization/Serializer.cs`)

1. Add `LiteGraphJsonContext` (§3.1).
2. Build the three option sets with the resolver chain from §3.3. Keep `DateTimeConverter`, `IPAddressConverter` and
   `NameValueCollectionConverter`.
3. `DeserializeJson<T>` / `SerializeJson` → `GetTypeInfo` pattern (§3.2).
4. `ExceptionConverter`: stop reflecting. Write `Type` (`GetType().FullName`), `Message`, `Source`, `HResult`, `HelpLink`,
   `StackTrace`, `Data` (as string key/value pairs) and `InnerException` (recursively), skipping nulls under
   `WhenWritingNull`. **Shape change:** subclass-specific properties (e.g. `ArgumentException.ParamName`,
   `PostgresException.SqlState`) are no longer emitted. Search the server's error responses and the logs for anything
   that depends on them before merging, and add `ParamName` explicitly if something does.
5. `NameValueCollectionConverter.Write`: write the object by hand with `writer.WriteString` rather than
   serializing a `Dictionary<string, string>`.
6. `ExpressionConverter.ReadValue`: recurse with `Read(ref reader, typeof(Expr), options)`.
7. `CopyObject<T>`: §3.4.
8. Enum attributes: §3.6.
9. Add the extension point (§4.1).

### Phase 2: Direct `JsonSerializer` call sites

| Site | Change |
|---|---|
| `TransactionMethods.ConvertPayload<T>` (569–572) | Route through the shared `Serializer` options (the `GetTypeInfo` pattern). Every `T` here is a LiteGraph model type that is already in the context. |
| `TransactionMethods.TryExtractGuidFromPayload` (677) | Same. |
| `TransactionMethods.ReadStringProperty` / `ReadIntProperty` (623, 629) | Replace reflection with type tests: `PostgresException` / `NpgsqlException` → `SqlState`; `SqliteException` → `SqliteErrorCode`. Both packages are already referenced. |
| `HnswLiteVectorIndex` (260, 793, 826) | Use `LiteGraphJsonContext.Default.VectorIndexStatistics` / `.HnswIndexState`, or a context instance created with `WriteIndented = true`. The on-disk format does not change, and the HNSW save/load round trip in `Test.Aot` checks it. |
| `QueryExecutionEngine` (3102, 3142, 3144) | `List<string>` via the context. `JsonValueKind.Object` → build `Dictionary<string, object>` by recursing through `NormalizeJsonValue` rather than deserializing (this also keeps nested values normalized the same way as top-level ones; confirm parity with the existing query suites). The `default` branch → return the `JsonElement` clone. |
| `Query/Parser.cs` (1130) | `JsonSerializer.Serialize(values, LiteGraphJsonContext.Default.ListString)`. |
| `AlgorithmMethods.ToJsonObject` (233), `GraphProjectionExporter.WriteRawData` (175) | Handle `JsonElement`/`JsonNode` directly (`JsonObject.Create(element)`, `node.WriteTo(writer)`), and fall back to the shared `Serializer` for everything else. |
| `Gexf/GexfWriter.cs` (113, 141) | Replace `XmlSerializer` with an `XmlWriter` that writes the GEXF document explicitly. The GEXF model classes are small and fixed. Before the rewrite, add a golden-output test that captures the current XML for a representative graph, and compare against it after. |

### Phase 3: `DataTable.Load`

Add one internal helper, e.g. `GraphRepositories/DataTableLoader.cs`, with `Load(IDataReader)` and
`LoadAsync(DbDataReader, CancellationToken)`, using the verified pattern from §1.4. Replace the 11 SQLite calls and the
1 PostgreSQL call. Leave all `DataRow` converters untouched.

Points to check:
- **Column types.** `DataTable.Load` infers types the same way (`GetFieldType`), so the converters see the same CLR types.
  Spot-check SQLite columns with mixed affinity: SQLite reports a per-row type, and `GetFieldType` on the first row can
  differ from later rows. If a column has mixed types, create the column as `typeof(object)` rather than the reported type.
  The existing Touchstone suites are the regression check.
- **Multiple result sets.** `PostgresqlGraphRepository.LoadAllResultSets` relies on `Load` advancing to the next result.
  The helper must call `NextResult()` explicitly.
- **Constraints.** `DataTable.Load` merges by primary key; LiteGraph never sets one, so plain `Rows.Add` is equivalent.

### Phase 4: Turn on the gate

1. Set `<IsAotCompatible>true</IsAotCompatible>` in `src/LiteGraph/LiteGraph.csproj`. This enables the trim, AOT and
   single-file analyzers for both `net8.0` and `net10.0`.
2. Build the solution and confirm zero IL warnings, per the repository's warning-free rule.
3. Add a CI job to `.github/workflows/ci.yml`: `dotnet publish src/Test.Aot -c Release -r linux-x64`, then run the binary
   (once against SQLite, and once against the PostgreSQL service the ScaleOut job already starts).
4. Run every existing Touchstone suite on SQLite and PostgreSQL, plus `AotSerializationParity`, and confirm no regressions.

### Phase 5: Consumer story and release

1. **Extension point (§4.1).** Add a static `Serializer.AddTypeInfoResolver(IJsonTypeInfoResolver resolver)` that
   rebuilds the three option sets copy-on-write (new `JsonSerializerOptions` from the old one plus the resolver, swapped
   with `Interlocked.Exchange`), so it is thread-safe and works after the options have been used. Applications that store
   their own POCOs in `Data` or call `ConvertData<T>` under AOT register their own `JsonSerializerContext` here. Add a
   `ConvertData<T>(object data, JsonTypeInfo<T> typeInfo)` overload so typed reads work without registration.
2. **Docs.** Add an "AOT and trimming" section to `README.md`: what works, the `Data` contract (§3.5), how to register a
   context, and that SQLite needs no extra steps (the `bundle_e_sqlite3` native library ships alongside the binary).
3. **Release.** Minor version bump (v10.2.0). Release notes: AOT/trim compatible; new
   `Serializer.AddTypeInfoResolver` and `ConvertData<T>(…, JsonTypeInfo<T>)`; exception JSON now has a fixed shape; GEXF
   writer rewritten (output unchanged).
4. **Downstream.** Durable.LiteGraph adds a scenario to its AOT test app and removes its two suppression attributes.

### Phase 6 (optional): MCP server and REST server

These apps are separate from the library. Their own analyzer warnings today:
- `LiteGraph.McpServer`: 4 (`LiteGraphMcpRegistrationExtensions.cs:103`, `LiteGraphMcpServerHelpers.cs:23`).
- `LiteGraph.Server`: 8 (`ChatService.cs:322/1008`, `OperationalLogFormatter.cs:79`, two
  `Enum.GetValues(Type)` in `ObservabilityService*.cs`, which can become `Enum.GetValues<TEnum>()`).

Beyond those, both apps route hundreds of request and response types through `Serializer` (199 call sites under the
server's `API/` folder), so each needs its own `JsonSerializerContext` registered through §4.1. The third-party
dependencies (Watson, Voltaic, PolyPrompt, StackExchange.Redis, OpenTelemetry, Clutch.Sdk) have not been probed. Start
this phase with the same rooted-publish probe (§1.2) to size it. If AOT for the server is ever wanted, it is its own
project; it should not block library readiness.

**Status (MCP server, done).** Once Voltaic 2.3.0 (and Watson 7.3.0, SyslogLogging 2.4.0) shipped AOT-compatible,
the MCP server's Native AOT publish went from 84 warnings (72 of them Voltaic's) to 8, all from the two call sites
above. The work: 194 literal tool schemas rewritten mechanically (Roslyn) from anonymous objects to JSON text parsed by
`LiteGraphMcpSchema.Parse`, the dynamically built authorization schemas moved to dictionaries, the `node_routes`
request body moved to a dictionary, settings metadata in `LiteGraphMcpJsonContext`, and the two call sites fixed. The
`Mcp.Protocol.ToolsListBaseline` case, captured before any change, confirms all 211 tools are byte-identical. The
native executable passes every MCP-dependent Touchstone suite (net10.0 and net8.0) and writes the same settings file
as the JIT build.

**Status (REST server, done in 10.2.0; this note is from before the work).** With Watson 7.3.0 and PolyPrompt 3.2.0, the server's remaining warnings are its
own (6 call sites) and Clutch.Sdk's (cluster mode only, an AOT-compatible release pending). The larger job is not in
the warnings: the server crashes at startup on its `Settings` type and needs metadata for about 80 types, a review of
238 `Serializer` calls, and replacements for 83 anonymous objects (66 of them chat tool schemas).

---

## 5. Definition of done

- `IsAotCompatible=true` on `LiteGraph.csproj`; zero IL warnings on `net8.0` and `net10.0`.
- `src/Test.Aot` publishes with zero LiteGraph-originated warnings and its scenario passes on SQLite and PostgreSQL in CI.
- `AotSerializationParity` passes: JSON output is byte-identical to the pre-change baseline for every registered type
  (exception JSON excepted, by design).
- All existing Touchstone suites pass on both backends under the JIT.
- No public API removed or changed in signature.

## 6. Risks

| Risk | Mitigation |
|---|---|
| A type that gets serialized is missing from the context, and AOT fails only at run time | The `Test.Aot` scenario covers each feature area; `NotSupportedException` messages name the type; `CopyObject` no longer hides it. |
| Source-generated output differs from reflection output (property order, null handling) | The Phase 0 golden baseline is captured before any change. |
| SQLite mixed-type columns behave differently through the manual loader | `typeof(object)` columns where types are mixed; full Touchstone run on SQLite. |
| Consumers rely on subclass-specific fields in exception JSON | Search before merging; the release note says so; add specific fields back explicitly if needed. |
| GEXF output drift after removing `XmlSerializer` | Golden-output test captured before the rewrite. |
