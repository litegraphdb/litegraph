# LiteGraph Performance And Scalability Testing

This guide explains how to run the `Test.PerformanceAndScalability` CLI harness.

The harness creates deterministic LiteGraph datasets, applies configurable load, captures throughput and latency, subscribes to LiteGraph repository/vector telemetry, samples process and storage metrics, and writes JSON, CSV, and Markdown artifacts.

## Prerequisites

- .NET SDK capable of building the solution targets.
- A writable working directory for artifacts.
- Optional: PostgreSQL database credentials when testing PostgreSQL.

Build the solution:

```powershell
dotnet build src/LiteGraph.sln
```

Show CLI help:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --help
```

Preview effective configuration without creating a database:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --dry-run true
```

## Quick Start

Run the default smoke benchmark. This creates a temporary SQLite file, runs a short smoke profile, writes artifacts, and removes the temporary database.

```powershell
dotnet run --project src/Test.PerformanceAndScalability
```

Artifacts are written under:

```text
artifacts/perf-scale/{run-id}/
```

## SQLite

Use an explicit SQLite file and keep it after the run:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --db-type sqlite --sqlite-file ./artifacts/perf-scale/local.db --keep-database true
```

Use SQLite in-memory mode:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --db-type sqlite --in-memory true --profile small
```

Run only read and traversal workloads on SQLite:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --db-type sqlite --workloads reads,traversal --profile small
```

## PostgreSQL

Use a PostgreSQL connection string:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --db-type postgresql --connection-string "<redacted>" --profile small
```

Use PostgreSQL connection properties:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --db-type postgresql --host localhost --port 5432 --database litegraph_perf --username litegraph --password "<redacted>" --schema perf --profile small
```

Tune PostgreSQL pool and command timeout:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --db-type postgresql --connection-string "<redacted>" --max-connections 64 --command-timeout-seconds 120 --profile medium
```

Passwords and connection strings are redacted in console output and artifacts.

## Profiles

Profiles set default dataset sizes and run durations.

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --profile smoke
dotnet run --project src/Test.PerformanceAndScalability -- --profile small
dotnet run --project src/Test.PerformanceAndScalability -- --profile medium
dotnet run --project src/Test.PerformanceAndScalability -- --profile large
dotnet run --project src/Test.PerformanceAndScalability -- --profile soak
```

Override profile dimensions:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --profile custom --tenants 2 --graphs-per-tenant 4 --nodes-per-graph 5000 --edges-per-graph 15000 --vectors-per-graph 1000
```

Apply a scale factor:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --profile small --scale-factor 2
```

## Topologies

Use topology-specific datasets:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --topology random
dotnet run --project src/Test.PerformanceAndScalability -- --topology power-law
dotnet run --project src/Test.PerformanceAndScalability -- --topology chain
dotnet run --project src/Test.PerformanceAndScalability -- --topology tree
dotnet run --project src/Test.PerformanceAndScalability -- --topology hub
dotnet run --project src/Test.PerformanceAndScalability -- --topology communities
dotnet run --project src/Test.PerformanceAndScalability -- --topology dense
```

Use `hub` and `power-law` to stress high-degree nodes. Use `chain`, `tree`, and `grid` to focus on route and traversal behavior.

## Workloads

Run all default workload families:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads all
```

Run selected workload families:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads ingest,reads,search
dotnet run --project src/Test.PerformanceAndScalability -- --workloads traversal,query
dotnet run --project src/Test.PerformanceAndScalability -- --workloads vector,transactions
dotnet run --project src/Test.PerformanceAndScalability -- --workloads updates,deletes,maintenance
```

Available workload groups:

- `ingest`
- `reads`
- `search`
- `traversal`
- `query`
- `vector`
- `transactions`
- `updates`
- `deletes`
- `maintenance`
- `mixed`
- `stress`

## Concurrency

Run a fixed concurrency:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads reads --concurrency 16
```

Run a concurrency ramp:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads reads,search,traversal --concurrency 1,2,4,8,16
```

Use range syntax:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --concurrency 1..64x2
```

The scalability knee is where throughput flattens while p95 or p99 latency rises sharply.

## Open-Loop Load

Closed-loop mode is the default. Each worker starts the next operation after the previous operation completes.

Open-loop mode schedules operations at a target arrival rate:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads reads --closed-loop false --target-rate 500 --concurrency 64
```

Use open-loop mode to expose overload behavior and queueing that closed-loop tests can hide.

## Batch And Transaction Sizes

Sweep batch size for ingestion:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads ingest --batch-size 10
dotnet run --project src/Test.PerformanceAndScalability -- --workloads ingest --batch-size 100
dotnet run --project src/Test.PerformanceAndScalability -- --workloads ingest --batch-size 1000
```

Test graph-scoped transaction sizes:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads transactions --transaction-size 10
dotnet run --project src/Test.PerformanceAndScalability -- --workloads transactions --transaction-size 100
```

Select a transaction isolation level for transaction workloads:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads transactions --transaction-isolation serializable
dotnet run --project src/Test.PerformanceAndScalability -- --db-type postgresql --connection-string "<redacted>" --workloads transactions --transaction-isolation repeatable-read
```

SQLite supports `default` and `serializable`. PostgreSQL supports `default`, `read-committed`, `repeatable-read`, and `serializable`.

The `transactions` workload currently includes:

- `transaction.create.nodes`: creates new nodes in graph-scoped transactions.
- `transaction.create-update.nodes`: creates nodes and updates them in the same transaction to exercise read-your-writes behavior.
- `transaction.mixed.children`: creates nodes and attaches labels, tags, and vectors in the same transaction.
- `transaction.rollback`: forces a rollback and verifies partial state is not committed.

Transaction runs add transaction-specific columns to `summary.csv` and `operations.csv`, and a transaction summary table to `report.md`. Use these fields to compare transaction start, commit, rollback, conflict, and retry rates across concurrency ramps, and to confirm whether transactions used isolated repository state or the serialized compatibility gate.

## Vector Tests

Run vector search and index workloads:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads vector --vector-dimensions 768 --vector-top-k 20
```

Sweep vector dimensions:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads vector --vector-dimensions 128
dotnet run --project src/Test.PerformanceAndScalability -- --workloads vector --vector-dimensions 384
dotnet run --project src/Test.PerformanceAndScalability -- --workloads vector --vector-dimensions 1536
```

Interpret vector results together with `vector-telemetry.csv`, storage metrics, and index file size.

## Native Query Profiling

Run native query workloads:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads query
```

Include query profile timing:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads query --include-query-profile true
```

Profile output appears in `query-profiles.csv`. Use it to separate parse, plan, execute, repository, vector, transaction, and total query time.

## Mixed Workloads

Run a balanced mixed workload:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --operation-mix balanced --duration 00:05:00
```

Run specific mixes:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --operation-mix read-heavy
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --operation-mix write-heavy
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --operation-mix vector-heavy
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --operation-mix transaction-heavy
dotnet run --project src/Test.PerformanceAndScalability -- --workloads mixed --operation-mix hotspot
```

Mixed workloads are useful for finding read/write/vector/transaction interference that single-operation tests miss.

## Soak Tests

Run a one-hour soak profile and keep the database for inspection:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --profile soak --workloads mixed --duration 01:00:00 --keep-database true
```

Watch for:

- Memory growth in `process-metrics.csv`.
- WAL or database growth in `storage-metrics.csv`.
- p95 and p99 latency drift in `summary.csv`.
- Error or timeout drift in `report.md`.

## Baselines And Regressions

Create a baseline:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --profile small --workloads reads,search,traversal --run-id baseline
```

Compare a later run to the baseline:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --profile small --workloads reads,search,traversal --baseline ./artifacts/perf-scale/baseline/summary.json
```

Fail the process when regressions are detected:

```powershell
dotnet run --project src/Test.PerformanceAndScalability -- --baseline ./artifacts/perf-scale/baseline/summary.json --fail-on-regression true
```

The comparison flags throughput drops greater than 10 percent, p99 increases greater than 20 percent, and increased failures for matching scenario names.

## Artifacts

Each run writes:

```text
config.redacted.json
environment.json
dataset.json
summary.json
summary.csv
operations.csv
repository-telemetry.csv
vector-telemetry.csv
query-profiles.csv
process-metrics.csv
storage-metrics.csv
report.md
anomalies.json
```

Use `report.md` for a human-readable overview. Use CSV files for spreadsheet analysis and dashboards. Use `summary.json` for regression comparison.

## Interpreting Results

Throughput:

- `ops_per_sec` is completed high-level operations per second.
- `items_per_sec` is item throughput for batch and transaction scenarios.

Latency:

- `p50_ms` shows the median operation.
- `p95_ms` and `p99_ms` show tail behavior under pressure.
- A rising p99 with flat throughput usually means the workload passed the scalability knee.

Repository telemetry:

- High statement counts per operation point to chatty storage access.
- High row counts with low result counts point to poor selectivity or unnecessary scans.
- High repository duration with low CPU can indicate storage waits, locks, or connection pool pressure.

Transaction metrics:

- `tx_started_per_sec`, `tx_commits_per_sec`, and `tx_rollbacks_per_sec` show transaction lifecycle throughput.
- `tx_conflicts_per_sec`, `tx_retry_count`, and `tx_retryable` show provider-level contention or retryable failures.
- `tx_isolated_repository` should increase with transaction count on providers that support isolated transaction state.
- `tx_serialized_by_gate` should remain zero for providers expected to support parallel transaction scaling.
- `tx_commit_p95_ms` and `tx_rollback_p95_ms` isolate commit and rollback tail latency from total operation latency.

Query profiles:

- High parse or plan time means query engine overhead.
- High repository time means storage execution dominates.
- High vector time means vector search or index behavior dominates.

Process metrics:

- Rising managed heap and frequent Gen2 collections point to allocation pressure.
- Low CPU with high latency often points to locks, IO, or pool contention.

Storage metrics:

- SQLite WAL growth during mixed writes can expose checkpoint pressure.
- Index byte growth should be reviewed with vector index rebuild and search results.

## Troubleshooting

Unsupported provider:

- Use `sqlite` or `postgresql` for `--db-type`.

SQLite file already contains prior data:

- Use a new `--sqlite-file`, omit `--keep-database`, or delete the old file before rerunning.

PostgreSQL authentication failure:

- Verify credentials, database name, schema permissions, and SSL requirements in the connection string.

Long runs consume too much disk:

- Reduce `--profile`, `--nodes-per-graph`, `--edges-per-graph`, `--vectors-per-graph`, or `--duration`.

Vector index rebuild is slow:

- Lower vector count or dimensions for local runs, then scale up on dedicated hardware.

## Security

- Do not paste real passwords into shared logs.
- Prefer environment-specific secret handling around the CLI command.
- The harness redacts passwords and connection strings from artifacts, but shell history is outside the harness.

## Suggested Optimization Workflow

1. Run a baseline for a focused workload.
2. Inspect `report.md`, `summary.csv`, and `repository-telemetry.csv`.
3. Identify the slowest operation family and the likely bottleneck layer.
4. Make one targeted code change.
5. Rerun the same command with the same seed.
6. Compare against the baseline with `--baseline`.
7. Keep the before/after commands and artifacts with the performance change.
