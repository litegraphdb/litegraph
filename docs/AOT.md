# Native AOT

Every part of LiteGraph runs in either of two ways, with the same behavior, settings, and output:

- **JIT (the default).** The NuGet packages, `dotnet run`, `dotnet publish`, and the Docker images on Docker Hub all run
  on the .NET runtime. Nothing in this document is needed to keep doing that.
- **Native AOT.** Each executable can be published as a self-contained native binary for one operating system and
  architecture: no .NET runtime to install, a faster start, and a smaller footprint. Applications that use the library
  or the C# SDK can be published with Native AOT or trimming as well.

| Component | Default (JIT) | Native AOT |
| --- | --- | --- |
| `LiteGraph` library (NuGet) | Any .NET 8 or 10 application | Your application with `<PublishAot>true</PublishAot>` ([details](#using-the-library-or-the-c-sdk-in-a-native-aot-application)) |
| `LiteGraph.Sdk` C# SDK (NuGet) | Any .NET 8 application | Same as the library |
| `LiteGraph.Server` (REST) | `dotnet run`, Docker image `jchristn77/litegraph` | `dotnet publish ... -p:PublishAot=true`, or `Dockerfile.native` |
| `LiteGraph.McpServer` | `dotnet run`, Docker image `jchristn77/litegraph-mcp` | `dotnet publish ... -p:PublishAot=true`, or `Dockerfile.native` |
| `LiteGraphConsole` (`lg`), `LiteGraph.SampleDatabase`, `LoadGenerator` | `dotnet run`, `lg` .NET tool | `dotnet publish ... -p:PublishAot=true` |
| Dashboard, JavaScript SDK, Python SDK | Not .NET; unaffected | Not applicable |

The servers and tools do not change behavior between the two: they serialize only through source-generated JSON
metadata in every build (reflection-based System.Text.Json is turned off even under the JIT), so the default build
already runs exactly the code a native build runs, and every test run checks it.

## Building Native Executables

Native AOT compiles for the machine it runs on, so build on (or for) each operating system and architecture you deploy
to. Prerequisites:

| Build machine | Install |
| --- | --- |
| Linux | `clang` and `zlib1g-dev` (Debian and Ubuntu: `sudo apt-get install clang zlib1g-dev`) |
| macOS | Xcode command line tools (`xcode-select --install`) |
| Windows | Visual Studio 2022 or later with the "Desktop development with C++" workload |

Publish any of the executables with `-p:PublishAot=true` and a runtime identifier (`linux-x64`, `linux-arm64`,
`osx-arm64`, `osx-x64`, `win-x64`, `win-arm64`):

```bash
dotnet publish src/LiteGraph.Server/LiteGraph.Server.csproj       -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/server
dotnet publish src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/mcp
dotnet publish src/LiteGraphConsole/LiteGraphConsole.csproj       -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/lg
dotnet publish src/LoadGenerator/LoadGenerator.csproj             -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/loadgen
dotnet publish src/LiteGraph.SampleDatabase/LiteGraph.SampleDatabase.csproj -c Release -f net10.0 -r linux-x64 -p:PublishAot=true -o out/sample
```

`-f net8.0` works the same way. Any trim or AOT warning, including one from a dependency, fails the publish, so a
binary that builds is one the compiler could fully analyze.

Run the executable exactly as you would run the JIT build: from the directory that holds its settings file
(`litegraph.json`, created with defaults on first start), with the same command-line options and environment variables.
Keep the files published next to the executable (for example `libe_sqlite3.so`, `libe_sqlite3.dylib`, or
`e_sqlite3.dll` for SQLite).

```bash
cd /srv/litegraph && /opt/litegraph/LiteGraph.Server         # REST server on port 8701
cd /srv/litegraph-mcp && /opt/litegraph-mcp/LiteGraph.McpServer
```

Without `-p:PublishAot=true`, `dotnet build`, `dotnet run`, and `dotnet publish` produce the usual JIT builds.

## Native Container Images

`src/LiteGraph.Server/Dockerfile.native` and `src/LiteGraph.McpServer/Dockerfile.native` build images that hold only the
native executable on `runtime-deps` (about 65 MB compressed for the server, against about 400 MB for the default image).
Each deployment under `docker/` has a `compose.native.yaml` override that switches the server and MCP server to those
images. Native images are built from the repository, not published to Docker Hub. See
[docker/README.md](../docker/README.md#native-aot-images).

## Using The Library Or The C# SDK In A Native AOT Application

As of v10.2, the `LiteGraph` library and the `LiteGraph.Sdk` C# SDK work in applications published with Native AOT
(`PublishAot`) or trimming (`PublishTrimmed`). Both packages set `IsAotCompatible` and build with no trim or AOT
analyzer warnings (the library for `net8.0` and `net10.0`, the SDK for `net8.0`).

### Publishing An Application

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

Nothing else is needed. SQLite's native library ships next to the executable, as it does for a JIT application.
PostgreSQL (Npgsql and pgvector) needs no extra configuration.

Unlike the LiteGraph executables, the library keeps reflection-based serialization available to JIT applications, so
existing applications keep working unchanged; the differences only appear once your application turns reflection off
(Native AOT, trimming, or `JsonSerializerIsReflectionEnabledByDefault=false`).


### Data, Payloads, And Other Untyped Values

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

### Registering Your Own Types

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

### Serializing LiteGraph Types With Your Own Options

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

### Differences Between JIT And Native AOT (Library And SDK)

| Behavior | JIT | Native AOT |
| --- | --- | --- |
| Unregistered type in `Data` or passed to `SerializeJson` | Serialized through reflection | `NotSupportedException` naming the type and pointing to `Serializer.AddTypeInfoResolver` |
| `CopyObject<T>` with an unregistered `T` | Copied through reflection | `NotSupportedException` (never a silent `null`) |
| Exception JSON | Every public property, as in 10.1 | `Message`, `ParamName` (argument exceptions), `Data`, `InnerException`, `HelpLink`, `Source`, `HResult`, `StackTrace`; other subclass properties are not written |
| Enums of unregistered types | Written as names | Not applicable (the type must be registered; `UseStringEnumConverter = true` writes names) |
| Provider error codes in `TransactionResult` | Npgsql, SQLite, and any exception with a `SqlState` or `SqliteErrorCode` property | Npgsql and SQLite; other exception types are best-effort |

For LiteGraph's own types, JSON output is identical under the JIT and Native AOT, and identical to 10.1. The
`Aot.Serialization` Touchstone suite checks every model type byte for byte against baselines captured from 10.1.

## Testing

CI runs all of the following on every commit, on Linux (x64), macOS (arm64), and Windows (x64). To run them locally:

```bash
# Library as a native binary (SQLite; PostgreSQL too when LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING is set)
dotnet publish src/Test.Aot/Test.Aot.csproj -c Release -f net10.0 -r osx-arm64 -o out/aot && out/aot/Test.Aot

# The Touchstone suites against native REST and MCP servers (every case that starts a server uses these executables)
dotnet publish src/LiteGraph.Server/LiteGraph.Server.csproj -c Release -f net10.0 -r osx-arm64 -p:PublishAot=true -o out/server
dotnet publish src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -c Release -f net10.0 -r osx-arm64 -p:PublishAot=true -o out/mcp
LITEGRAPH_TEST_SERVER_EXECUTABLE=out/server/LiteGraph.Server LITEGRAPH_TEST_MCP_EXECUTABLE=out/mcp/LiteGraph.McpServer \
  dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0

# C# SDK suite as a native binary, against a running server (LITEGRAPH_ENDPOINT, default http://localhost:8701)
dotnet publish sdk/csharp/src/Test.Automated/Test.Automated.csproj -c Release -r osx-arm64 -p:PublishAot=true -o out/sdk-aot
out/sdk-aot/Test.Automated
```

The suites that pin JSON output, so that none of this changed what LiteGraph writes:

- `Aot.Serialization`: every library model type, byte for byte against baselines captured from 10.1 (compact, indented,
  and round trip), GEXF output, and timestamps in any time zone.
- `Aot.Server`: every server type, the default settings file, chat stream events and tool transcripts, the OpenAPI
  document, first-boot seed data, and the chat tool schemas as the model provider receives them, against baselines
  captured before the server work; plus metadata coverage and SSL settings with a real certificate.
- `Mcp.Protocol.ToolsListBaseline`: all 211 MCP tools in `tools/list`, byte for byte.

## For Contributors

The rules that keep everything compatible, enforced by the build and the tests:

- **Library and C# SDK** (`IsAotCompatible`): all JSON goes through `Serializer`, options built with
  `Serializer.CreateResolver`, or a `JsonTypeInfo<T>`. A new serialized type needs a `[JsonSerializable]` entry in
  `Serialization/LiteGraphJsonContext.cs` (or `LiteGraphSdkJsonContext.cs`) and in the parity list in
  `src/Test.Shared/LiteGraphTouchstoneAotSuites.cs`; `Aot.ContextCoverage` fails when they disagree.
- **REST server**: a new serialized type needs a `[JsonSerializable]` entry in
  `src/LiteGraph.Server/Classes/LiteGraphServerJsonContext.cs` and in `_AotServerParityTypes`
  (`src/Test.Shared/LiteGraphTouchstoneAotServerSuites.cs`). Chat tool property schemas are JSON text.
- **MCP server**: tool schemas are JSON text passed to `LiteGraphMcpSchema.Parse` (or dictionaries from
  `LiteGraphMcpSchema.Property` and `Object`); settings classes hang off `LiteGraphMcpServerSettings`
  (`LiteGraphMcpJsonContext`).
- **Everywhere**: no anonymous types in serialized values (use a named class or a `Dictionary<string, object>`), no
  `JsonSerializer` call with plain options, no reflection, no `XmlSerializer`, no `JsonStringEnumConverter` without a type
  argument, no `Enum.GetValues(Type)`.
- The trim and AOT analyzers run on every build of every project, and the solution must build with no warnings. The
  servers and tools turn reflection-based System.Text.Json off in every build
  (`<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>`), so a missing type
  fails the ordinary test run with a `NotSupportedException` naming it, not only a native one.
- If a change is meant to alter JSON output, recapture the baselines with `LITEGRAPH_CAPTURE_AOT_BASELINES=<directory>`
  (with `--suite Aot.Serialization,Aot.Server` or `--case Mcp.Protocol.ToolsListBaseline`), review the diff, and copy the
  files into `src/Test.Shared/Baselines/`.
