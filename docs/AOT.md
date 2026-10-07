# Native AOT And Trimming

As of v10.2, the `LiteGraph` library and the `LiteGraph.Sdk` C# SDK work in applications published with Native AOT
(`PublishAot`) or trimming (`PublishTrimmed`). Both packages set `IsAotCompatible` and build with no trim or AOT
analyzer warnings (the library for `net8.0` and `net10.0`, the SDK for `net8.0`), and both are tested as Native AOT
binaries:

- `src/Test.Aot` runs the library end to end on SQLite and PostgreSQL: repository initialization and built-in roles,
  tenants, users, credentials, graphs, nodes, edges, labels, tags, vectors, expression filters, the graph query
  language, brute-force and indexed vector search, transactions (commit, rollback, provider error codes), GEXF, JSONL,
  and projection export, JSONL import, algorithms with write-back, and application data types.
- The C# SDK's `Test.Automated` suite (157 cases against a live server) passes when published with `PublishAot=true`.

`LiteGraph.McpServer` can also be published as a Native AOT executable (see
[The MCP Server As A Native Executable](#the-mcp-server-as-a-native-executable)). `LiteGraph.Server` and the console
tools still run on the JIT only. The Docker images run every component on the JIT.

## Publishing An Application

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

Nothing else is needed. SQLite's native library ships next to the executable, as it does for a JIT application.
PostgreSQL (Npgsql and pgvector) needs no extra configuration.

## Data, Payloads, And Other Untyped Values

`Graph.Data`, `Node.Data`, `Edge.Data`, `TransactionOperation.Payload`, `TransactionOperationResult.Result`,
`JsonlRecord.Object`, and query parameters are typed `object`. Under the JIT, any serializable object works, as before.
Under Native AOT, System.Text.Json cannot inspect types at run time, so these values must be one of:

- `JsonElement`, `JsonNode`, `JsonObject`, `JsonArray`, or `JsonValue`
- `string`, `char`, integer and floating-point types, `decimal`, `bool`, `Guid`, `DateTime`, `DateTimeOffset`, or `TimeSpan`
- arrays (`T[]`) or `List<T>` of `string`, `int`, `long`, `float`, `double`, `decimal`, `bool`, `Guid`, `DateTime`, or
  `object`, and `byte[]`
- `Dictionary<string, object>`, `Dictionary<string, string>`, or `List<object>` holding any of the above
- a LiteGraph model type (`Node`, `Edge`, `TagMetadata`, and so on)
- a type you register (next section)

Values read back from the database are always `JsonElement`, under both the JIT and Native AOT.

Anonymous types (`new { name = "x" }`) cannot be supported under Native AOT, because no metadata can be generated for
them ahead of time. Use a `Dictionary<string, object>` or a named class instead.

## Registering Your Own Types

Declare a source-generated context for your types and register it once at startup:

```csharp
using System.Text.Json.Serialization;
using LiteGraph.Serialization;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(Employee))]
public partial class AppJsonContext : JsonSerializerContext
{
}

// At startup, before using LiteGraph:
Serializer.AddTypeInfoResolver(AppJsonContext.Default);

// Now Employee instances can be stored in Data, and read back as Employee:
await client.Node.Create(new Node { TenantGUID = tenant, GraphGUID = graph, Name = "Ada", Data = new Employee { ... } });
Node node = await client.Node.ReadByGuid(tenant, graph, guid, includeData: true);
Employee employee = client.ConvertData<Employee>(node.Data);
```

Without registering a resolver, you can pass type metadata directly:

```csharp
Employee employee = client.ConvertData(node.Data, AppJsonContext.Default.Employee);
Employee parsed = new Serializer().DeserializeJson(json, AppJsonContext.Default.Employee);
```

`Serializer.AddTypeInfoResolver` affects every `Serializer` instance in the process, is thread-safe, and can be called
at any time. LiteGraph's own metadata is always consulted first.

The SDK has the same API: `LiteGraph.Sdk.Serializer.AddTypeInfoResolver` and
`LiteGraph.Sdk.Serializer.DeserializeJson(json, typeInfo)`.

## Serializing LiteGraph Types With Your Own Options

The generated metadata is public. Add it to your own `JsonSerializerOptions` to serialize LiteGraph types with your
own settings:

```csharp
JsonSerializerOptions options = new JsonSerializerOptions();
options.TypeInfoResolverChain.Add(LiteGraphJsonContext.Default);      // LiteGraph
options.TypeInfoResolverChain.Add(LiteGraphSdkJsonContext.Default);   // LiteGraph.Sdk
options.TypeInfoResolverChain.Add(AppJsonContext.Default);            // your types
```

LiteGraph's serializer adds converters on top (timestamps as `yyyy-MM-ddTHH:mm:ss.ffffffZ`, tags as JSON objects,
expressions, exceptions), so use `LiteGraph.Serialization.Serializer` when the output must match what LiteGraph
stores and returns.

## Differences Between JIT And Native AOT

| Behavior | JIT | Native AOT |
| --- | --- | --- |
| Unregistered type in `Data` or passed to `SerializeJson` | Serialized through reflection | `NotSupportedException` naming the type and pointing to `Serializer.AddTypeInfoResolver` |
| `CopyObject<T>` with an unregistered `T` | Copied through reflection | `NotSupportedException` (never a silent `null`) |
| Exception JSON | Every public property, as in 10.1 | `Message`, `ParamName` (argument exceptions), `Data`, `InnerException`, `HelpLink`, `Source`, `HResult`, `StackTrace`; other subclass properties are not written |
| Enums of unregistered types | Written as names | Not applicable (the type must be registered; `UseStringEnumConverter = true` writes names) |
| Provider error codes in `TransactionResult` | Npgsql, SQLite, and any exception with a `SqlState` or `SqliteErrorCode` property | Npgsql and SQLite; other exception types are best-effort |

For LiteGraph's own types, JSON output is identical under the JIT and Native AOT, and identical to 10.1. The
`Aot.Serialization` Touchstone suite checks every model type byte for byte against baselines captured from 10.1.

## The MCP Server As A Native Executable

`LiteGraph.McpServer` publishes as a Native AOT executable with no trim or AOT warnings, including from its
dependencies (Voltaic 2.3, Watson 7.3, SyslogLogging 2.4, and the C# SDK):

```bash
dotnet publish src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/mcp-aot
cd <directory for litegraph.json and logs> && /path/to/out/mcp-aot/LiteGraph.McpServer
```

The executable behaves like the JIT build: the same settings file (`litegraph.json`), environment variables, transports
(HTTP, TCP, WebSocket), tools, schemas, and results, and it needs no .NET runtime. A `net8.0`
build works the same way. Under `-p:PublishAot=true`, any trim or AOT warning fails the publish.

Without `-p:PublishAot=true` the project builds and publishes for the JIT exactly as before, which is what the Docker
image does.

How the MCP server stays compatible:

- Tool argument schemas are JSON text, parsed once at registration by `LiteGraphMcpSchema.Parse` (or built as
  dictionaries with `LiteGraphMcpSchema.Property` and `LiteGraphMcpSchema.Object`), never anonymous objects. Voltaic
  converts every schema to a `JsonElement` when a tool is registered, which needs metadata for the schema's type.
- The settings classes have source-generated metadata (`LiteGraphMcpJsonContext`), registered with the SDK serializer
  at startup.
- Results of TCP and WebSocket methods are serialized with Voltaic's metadata
  (`VoltaicJson.TypeInfoResolver`), and tool arguments are parsed with `JsonDocument`.
- The project turns on the trim and AOT analyzers for every build, so new reflection-based calls show up as warnings
  (and the solution must build with none).

`Mcp.Protocol.ToolsListBaseline` compares every tool in `tools/list` (211 tools) byte for byte with a baseline captured
before the schemas moved to JSON, so the published names, descriptions, and schemas are unchanged.

## Verifying

```bash
# Library: Native AOT publish (trim and AOT warnings are errors) and run
dotnet publish src/Test.Aot/Test.Aot.csproj -c Release -f net10.0 -r linux-x64 -o out/aot
LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING="Host=...;Username=...;Password=...;Database=..." out/aot/Test.Aot

# SDK: Native AOT publish of the SDK suite, run against a live server (LITEGRAPH_ENDPOINT, default http://localhost:8701)
dotnet publish sdk/csharp/src/Test.Automated/Test.Automated.csproj -c Release -r linux-x64 -p:PublishAot=true -o out/sdk-aot
out/sdk-aot/Test.Automated
```

```bash
# MCP server: Native AOT publish (any trim or AOT warning fails it), then the MCP suites against the native executable
dotnet publish src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/mcp-aot
LITEGRAPH_TEST_MCP_EXECUTABLE=out/mcp-aot/LiteGraph.McpServer \
  dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0 -- --suite Mcp.Protocol,Mcp.Server
```

`LITEGRAPH_TEST_MCP_EXECUTABLE` makes every Touchstone case that starts the MCP server start that executable instead
of the JIT build; the other suites (`Authorization`, `Onboarding`, `Observability`, `Chat.Rest`,
`Improvements.Foundation`) use it too.

Running `dotnet run --project src/Test.Aot` also works: `PublishAot` turns reflection-based serialization and dynamic
code off for JIT runs of that project, so most problems show up without a full Native AOT compile.

## For Contributors

- A new type that LiteGraph serializes or deserializes needs a `[JsonSerializable]` entry in
  `src/LiteGraph/Serialization/LiteGraphJsonContext.cs` (or `LiteGraphSdkJsonContext.cs` in the SDK), and an entry in
  the parity list in `src/Test.Shared/LiteGraphTouchstoneAotSuites.cs`. `Aot.ContextCoverage` fails when they disagree.
- Call `JsonSerializer` through `Serializer`, through options built with `Serializer.CreateResolver`, or with a
  `JsonTypeInfo<T>`; never with plain `JsonSerializerOptions`. Do not add reflection, `XmlSerializer`, or
  `JsonStringEnumConverter` without a type argument.
- The build must stay free of IL warnings; `IsAotCompatible` turns the analyzers on for every build.
- In the MCP server, write a new tool's schema as JSON with `LiteGraphMcpSchema.Parse`, add new settings classes under
  `LiteGraphMcpServerSettings` (covered by `LiteGraphMcpJsonContext`), and route JSON through the SDK `Serializer`, a
  `JsonTypeInfo`, or `JsonDocument`/`JsonNode`. A tool added or changed on purpose needs the tools baseline recaptured
  (`LITEGRAPH_CAPTURE_AOT_BASELINES=<directory>` with `--case Mcp.Protocol.ToolsListBaseline`), reviewed, and
  copied to `src/Test.Shared/Baselines/mcp-tools-baseline.json`.
- After changing serialization, run `Aot.Serialization` and `src/Test.Aot` as a Native AOT binary.
  If a model change intentionally changes JSON output, recapture the baselines with
  `LITEGRAPH_CAPTURE_AOT_BASELINES=<directory>` and review the diff before committing them.
