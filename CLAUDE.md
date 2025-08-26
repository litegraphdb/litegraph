# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

```bash
# Build the entire solution
dotnet build src/LiteGraph.sln

# Build specific projects
dotnet build src/LiteGraph/LiteGraph.csproj
dotnet build src/LiteGraph.Server/LiteGraph.Server.csproj

# Run tests
dotnet run --project src/Test/Test.csproj
dotnet run --project src/Test.VectorSearch/Test.VectorSearch.csproj
dotnet run --project src/Test.VectorIndexSearch/Test.VectorIndexSearch.csproj

# Run the server
dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj
```

## High-Level Architecture

### Layered Architecture Pattern

LiteGraph follows a strict layered architecture with clear separation of concerns:

1. **Client Layer** (`LiteGraph.Client`): Handles input validation and cross-cutting logic
2. **Repository Layer** (`GraphRepositories`): Contains primitives and data access
3. **Storage Layer**: SQLite implementation with optional in-memory operation

### Key Architectural Components

#### Client-Repository Separation
- **Client classes** (e.g., `GraphMethods` in `Client/Implementations/`) perform validation and business logic
- **Repository classes** (e.g., `GraphMethods` in `GraphRepositories/Sqlite/Implementations/`) handle raw data operations
- Client classes call repository methods via `_Repo` field

#### Multi-Tenant Design
All operations require a `tenantGuid` parameter. The hierarchy is:
```
Tenant → Graph → Nodes/Edges → Labels/Tags/Vectors
```

#### Vector Indexing Architecture
- **Vector Index Manager** (`Indexing/Vector/VectorIndexManager.cs`): Manages HNSW index lifecycle
- **HNSW Implementation** (`HnswLiteVectorIndex.cs`): Wraps HnswLite library with custom storage
- **Index Integration** (`VectorMethodsWithIndex.cs`): Extension methods for index operations
- **Critical**: `VectorMethods.SearchNode()` must check for index availability before falling back to brute-force search

### Data Model Key Points

#### Graph Objects
- **Graph**: Container with optional vector indexing (HNSW)
- **Node**: Can have multiple vectors, labels, tags, and arbitrary data
- **Edge**: Connects two nodes with cost, direction, labels, tags, and data
- **Vector**: Multi-dimensional embeddings with model metadata

#### Vector Search Performance
- **With HNSW Index**: Sub-100ms search times for large datasets
- **Without Index**: Linear scan through all vectors (very slow for large datasets)
- **Index Threshold**: Configurable via `Graph.VectorIndexThreshold` property

### Important Implementation Details

#### Memory Management
- **In-Memory Mode**: Set second parameter to `true` in `SqliteGraphRepository` constructor
- **Flushing**: Must call `client.Flush()` to persist in-memory changes to disk
- **Caching**: Uses `LRUCache` for tenant, graph, node, and edge validation

#### Vector Index Integration Bug
The `VectorMethods.SearchNode()` method was historically performing brute-force searches even when HNSW indexes were available. Always ensure:
1. Check if graph has vector indexing enabled (`graph.VectorIndexType`)
2. Use `VectorMethodsIndexExtensions.SearchWithIndexAsync()` when available
3. Fall back to brute-force only when no index or complex filtering is needed

#### Batch Operations
All entity types support batch creation via `CreateMany()` methods for performance optimization.

#### Expression Filtering
Uses `ExpressionTree` library for filtering on the `Data` property of graphs, nodes, and edges.

## Project Structure

- **LiteGraph**: Core library and NuGet package
- **LiteGraph.Server**: REST API server with authentication
- **Test projects**: Various performance and functionality tests
- **Docker**: Container deployment configuration

## Performance Considerations

- Vector searches with 10K+ vectors require HNSW indexing for reasonable performance
- Batch operations significantly outperform individual operations
- In-memory mode provides better performance but requires manual flushing
- Cache settings can be tuned via `CachingSettings` for validation performance

## Coding Style and Implementation Rules

To maximize consistency and maintainability, all code files must follow these rules:

### Code Organization and Structure
- **Namespace declaration** should always be at the top, with `using` statements contained INSIDE the namespace block
- **Using statement order**: Microsoft and standard system libraries first (alphabetical), followed by other using statements (alphabetical)
- **Class organization**: Always use exactly five regions in this order:
  1. `Public-Members`
  2. `Private-Members` 
  3. `Constructors-and-Factories`
  4. `Public-Methods`
  5. `Private-Methods`
- **Region formatting**: Extra line break before and after region statements, unless adjacent to opening/closing braces `{` or `}`
- **File structure**: Limit each file to exactly one class or exactly one enum - no nesting multiple classes/enums

### Documentation and Naming
- **Public documentation**: All public members, constructors, and public methods must have XML code documentation
- **Private documentation**: No code documentation on private members or private methods
- **Private member naming**: Must start with underscore and be Pascal cased (e.g., `_FooBar` not `_fooBar`)
- **Documentation details**: Include default values, minimum/maximum values, and effects of different values where appropriate
- **Exception documentation**: Use `/// <exception>` tags to document which exceptions public methods can throw

### Properties and Members
- **Public properties**: Must have explicit getters and setters using backing variables when value ranges require validation
- **Configurable values**: Avoid constants for values developers may want to configure - use public members with backing private members set to reasonable defaults

### Async and Threading
- **ConfigureAwait**: Use `.ConfigureAwait(false)` where appropriate
- **CancellationToken**: Every async method should accept a CancellationToken parameter, unless the class has a CancellationToken member or CancellationTokenSource member
- **Cancellation checks**: Check for cancellation requests at appropriate places in async methods
- **IEnumerable methods**: When implementing methods that return IEnumerable, also create async variants with CancellationToken
- **Thread safety**: Document thread safety guarantees in XML comments
- **Concurrency**: Use `Interlocked` operations for simple atomic operations, prefer `ReaderWriterLockSlim` over `lock` for read-heavy scenarios

### Error Handling and Exceptions
- **Specific exceptions**: Use specific exception types rather than generic `Exception`
- **Error messages**: Always include meaningful error messages with context
- **Custom exceptions**: Consider custom exception types for domain-specific errors
- **Exception filters**: Use when appropriate: `catch (SqlException ex) when (ex.Number == 2601)`

### Resource Management
- **IDisposable**: Implement `IDisposable`/`IAsyncDisposable` when holding unmanaged resources or disposable objects
- **Using statements**: Use `using` statements or declarations for IDisposable objects
- **Dispose pattern**: Follow full Dispose pattern with `protected virtual void Dispose(bool disposing)`
- **Base disposal**: Always call `base.Dispose()` in derived classes

### Null Safety and Validation
- **Nullable reference types**: Use nullable reference types (enable `<Nullable>enable</Nullable>` in project files)
- **Input validation**: Validate input parameters with guard clauses at method start
- **Null checks**: Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual null checks
- **Result patterns**: Consider using Result pattern or Option/Maybe types for methods that can fail
- **Null documentation**: Document nullability in XML comments
- **Proactive null handling**: Eliminate situations where null might cause exceptions

### LINQ and Collections
- **LINQ preference**: Prefer LINQ methods over manual loops when readability is not compromised
- **Existence checks**: Use `.Any()` instead of `.Count() > 0` for existence checks
- **Multiple enumeration**: Be aware of multiple enumeration issues - consider `.ToList()` when needed
- **Safe access**: Use `.FirstOrDefault()` with null checks rather than `.First()` when element might not exist

### General Principles
- **Variable declaration**: Do not use `var` - use actual type names
- **Assumptions**: Do not make assumptions about opaque class members or methods - ask for implementation details
- **SQL statements**: If manually prepared SQL strings exist, assume there's a good reason for the implementation
- **Tuple avoidance**: Do not use tuples unless absolutely necessary
- **Code compilation**: Always compile code and ensure it's free of errors and warnings