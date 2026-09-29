namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;

    public partial class PostgresqlGraphRepository
    {
        /// <summary>
        /// Schema migration version that converts vectors.embeddings from BYTEA to pgvector.
        /// </summary>
        internal const int MigrationPgvectorEmbeddings = 1;

        /// <summary>
        /// Schema migration version that enforces unique built-in role names and unique chat turn sequences.
        /// </summary>
        internal const int MigrationIntegrityConstraints = 2;

        private async Task EnsureVectorExtensionAsync(CancellationToken token)
        {
            DataTable existing = await ExecuteNativeQueryAsync(
                "SELECT extversion FROM pg_extension WHERE extname = 'vector';", false, token).ConfigureAwait(false);

            if (existing == null || existing.Rows.Count < 1)
            {
                try
                {
                    await ExecuteNativeQueryAsync("CREATE EXTENSION IF NOT EXISTS vector;", false, token).ConfigureAwait(false);
                    Logging.Log(SeverityEnum.Info, "created the pgvector extension in database " + Settings.DatabaseName);
                }
                catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InsufficientPrivilege)
                {
                    throw new InvalidOperationException(
                        "LiteGraph requires the pgvector extension, and the database role '" + Settings.Username + "' is not permitted to create it. "
                        + "Ask a database administrator to run 'CREATE EXTENSION vector;' in database '" + Settings.DatabaseName + "', then restart LiteGraph.", e);
                }
                catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UndefinedFile || e.SqlState == PostgresErrorCodes.FeatureNotSupported)
                {
                    throw new InvalidOperationException(
                        "LiteGraph requires the pgvector extension, which is not installed on this PostgreSQL server. "
                        + "Use a PostgreSQL image that includes it (for example pgvector/pgvector:pg17) or install pgvector, then restart LiteGraph.", e);
                }
            }

            // Connections opened before the extension existed cached a type map without 'vector'.
            await _DataSource.ReloadTypesAsync(token).ConfigureAwait(false);
        }

        private async Task EnsureSchemaMigrationsTableAsync(CancellationToken token)
        {
            await ExecuteNativeQueryAsync(
                "CREATE TABLE IF NOT EXISTS " + QualifiedTable("schemamigrations") + " ("
                + "version INT PRIMARY KEY, "
                + "description TEXT NOT NULL, "
                + "appliedutc VARCHAR(64) NOT NULL"
                + ");",
                false,
                token).ConfigureAwait(false);
        }

        private async Task<bool> IsMigrationAppliedAsync(int version, CancellationToken token)
        {
            DataTable result = await ExecuteNativeQueryAsync(
                "SELECT version FROM " + QualifiedTable("schemamigrations") + " WHERE version = " + version + ";",
                false,
                token).ConfigureAwait(false);
            return result != null && result.Rows.Count > 0;
        }

        private string RecordMigrationSql(int version, string description)
        {
            return "INSERT INTO " + QualifiedTable("schemamigrations") + " (version, description, appliedutc) VALUES ("
                + version + ", "
                + "'" + Sanitizer.Sanitize(description) + "', "
                + "'" + DateTime.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture) + "') "
                + "ON CONFLICT (version) DO NOTHING;";
        }

        private async Task MigrateEmbeddingsToPgvectorAsync(CancellationToken token)
        {
            if (await IsMigrationAppliedAsync(MigrationPgvectorEmbeddings, token).ConfigureAwait(false)) return;

            string vectors = QualifiedTable("vectors");
            string columnType = await GetColumnTypeAsync("vectors", "embeddings", token).ConfigureAwait(false);

            if (String.Equals(columnType, "vector", StringComparison.OrdinalIgnoreCase))
            {
                // New installation: the table was created with a pgvector column.
                await ExecuteNativeQueryAsync(RecordMigrationSql(MigrationPgvectorEmbeddings, "vectors.embeddings stored as pgvector"), false, token).ConfigureAwait(false);
                return;
            }

            if (!String.Equals(columnType, "bytea", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected type '" + columnType + "' for column vectors.embeddings in schema " + Schema + "; expected bytea or vector.");

            Logging.Log(SeverityEnum.Info, "migrating vectors.embeddings from BYTEA to pgvector in schema " + Schema + "; this runs once and may take a while for large vector sets");

            await ExecuteNativeQueryAsync("ALTER TABLE " + vectors + " ADD COLUMN IF NOT EXISTS embeddings_pgvector vector;", false, token).ConfigureAwait(false);

            string lastGuid = "";
            long converted = 0;
            long skipped = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                DataTable batch = await ExecuteNativeQueryAsync(
                    "SELECT guid, embeddings FROM " + vectors + " "
                    + "WHERE guid > '" + Sanitizer.Sanitize(lastGuid) + "' "
                    + "AND embeddings_pgvector IS NULL AND embeddings IS NOT NULL "
                    + "ORDER BY guid ASC LIMIT " + EmbeddingMigrationBatchSize + ";",
                    false,
                    token).ConfigureAwait(false);

                if (batch == null || batch.Rows.Count < 1) break;

                List<string> values = new List<string>();
                foreach (DataRow row in batch.Rows)
                {
                    string guid = row["guid"].ToString();
                    lastGuid = guid;

                    byte[] blob = row["embeddings"] as byte[];
                    if (blob == null || blob.Length < 4 || blob.Length % 4 != 0)
                    {
                        skipped++;
                        Logging.Log(SeverityEnum.Warn, "skipping vector " + guid + " during pgvector migration: stored embedding is empty or not a float32 array");
                        continue;
                    }

                    List<float> floats = Converters.BlobToVector(blob);
                    if (floats.Count < 1)
                    {
                        skipped++;
                        Logging.Log(SeverityEnum.Warn, "skipping vector " + guid + " during pgvector migration: stored embedding is empty or not a float32 array");
                        continue;
                    }

                    string text;
                    try
                    {
                        text = Converters.VectorToText(floats);
                    }
                    catch (ArgumentException e)
                    {
                        skipped++;
                        Logging.Log(SeverityEnum.Warn, "skipping vector " + guid + " during pgvector migration: " + e.Message);
                        continue;
                    }

                    values.Add("('" + Sanitizer.Sanitize(guid) + "', '" + text + "', " + floats.Count.ToString(CultureInfo.InvariantCulture) + ")");
                }

                if (values.Count > 0)
                {
                    StringBuilder update = new StringBuilder();
                    update.Append("UPDATE " + vectors + " AS v SET embeddings_pgvector = d.e::vector, dimensionality = d.dims ");
                    update.Append("FROM (VALUES ");
                    update.Append(String.Join(", ", values));
                    update.Append(") AS d(guid, e, dims) WHERE v.guid = d.guid;");
                    await ExecuteNativeQueryAsync(update.ToString(), true, token).ConfigureAwait(false);
                    converted += values.Count;
                }

                Logging.Log(SeverityEnum.Info, "pgvector migration progress: " + converted + " converted, " + skipped + " skipped");
                if (batch.Rows.Count < EmbeddingMigrationBatchSize) break;
            }

            await ExecuteNativeQueryAsync(
                "ALTER TABLE " + vectors + " DROP COLUMN embeddings; "
                + "ALTER TABLE " + vectors + " RENAME COLUMN embeddings_pgvector TO embeddings; "
                + RecordMigrationSql(MigrationPgvectorEmbeddings, "vectors.embeddings converted from BYTEA to pgvector"),
                true,
                token).ConfigureAwait(false);

            Logging.Log(SeverityEnum.Info, "pgvector migration complete: " + converted + " converted, " + skipped + " skipped");
        }

        private async Task EnsureIntegrityConstraintsAsync(CancellationToken token)
        {
            if (await IsMigrationAppliedAsync(MigrationIntegrityConstraints, token).ConfigureAwait(false)) return;

            string roles = QualifiedTable("authorizationroles");
            string userRoles = QualifiedTable("userroleassignments");
            string credentialScopes = QualifiedTable("credentialscopeassignments");
            string turns = QualifiedTable("chatturns");

            // Concurrent first starts could seed a built-in role more than once.  Keep the oldest copy of each built-in
            // (tenant, name) pair (user-defined roles are never touched), re-point assignments at it, and delete the rest before adding the unique index.
            string dedupeRoles =
                "WITH ranked AS ("
                + "SELECT guid, FIRST_VALUE(guid) OVER (PARTITION BY COALESCE(tenantguid, ''), name ORDER BY createdutc ASC NULLS LAST, guid ASC) AS keeper "
                + "FROM " + roles + " WHERE builtin = 1), "
                + "dupes AS (SELECT guid, keeper FROM ranked WHERE guid <> keeper) "
                + "UPDATE " + userRoles + " AS u SET roleguid = dupes.keeper FROM dupes WHERE u.roleguid = dupes.guid; "
                + "WITH ranked AS ("
                + "SELECT guid, FIRST_VALUE(guid) OVER (PARTITION BY COALESCE(tenantguid, ''), name ORDER BY createdutc ASC NULLS LAST, guid ASC) AS keeper "
                + "FROM " + roles + " WHERE builtin = 1), "
                + "dupes AS (SELECT guid, keeper FROM ranked WHERE guid <> keeper) "
                + "UPDATE " + credentialScopes + " AS c SET roleguid = dupes.keeper FROM dupes WHERE c.roleguid = dupes.guid; "
                + "WITH ranked AS ("
                + "SELECT guid, FIRST_VALUE(guid) OVER (PARTITION BY COALESCE(tenantguid, ''), name ORDER BY createdutc ASC NULLS LAST, guid ASC) AS keeper "
                + "FROM " + roles + " WHERE builtin = 1) "
                + "DELETE FROM " + roles + " WHERE guid IN (SELECT guid FROM ranked WHERE guid <> keeper); "
                + "CREATE UNIQUE INDEX IF NOT EXISTS " + QuoteIdentifier("uq_authorizationroles_tenant_name") + " ON " + roles + " ((COALESCE(tenantguid, '')), name) WHERE builtin = 1;";

            // Two nodes appending to one thread could both take MAX(sequence) + 1.  Renumber any thread with
            // duplicate sequences in (sequence, created, guid) order before adding the unique index.
            string dedupeTurns =
                "WITH dupthreads AS (SELECT threadguid FROM " + turns + " GROUP BY threadguid, sequence HAVING COUNT(*) > 1), "
                + "renumbered AS (SELECT guid, ROW_NUMBER() OVER (PARTITION BY threadguid ORDER BY sequence ASC, createdutc ASC, guid ASC) AS rn "
                + "FROM " + turns + " WHERE threadguid IN (SELECT threadguid FROM dupthreads)) "
                + "UPDATE " + turns + " AS t SET sequence = renumbered.rn FROM renumbered WHERE t.guid = renumbered.guid; "
                + "CREATE UNIQUE INDEX IF NOT EXISTS " + QuoteIdentifier("uq_chatturns_thread_sequence") + " ON " + turns + " (threadguid, sequence);";

            await ExecuteNativeQueryAsync(
                dedupeRoles + " " + dedupeTurns + " "
                + RecordMigrationSql(MigrationIntegrityConstraints, "unique built-in role names and unique chat turn sequences"),
                true,
                token).ConfigureAwait(false);
        }

        private async Task<string> GetColumnTypeAsync(string table, string column, CancellationToken token)
        {
            DataTable result = await ExecuteNativeQueryAsync(
                "SELECT udt_name FROM information_schema.columns "
                + "WHERE table_schema = '" + Sanitizer.Sanitize(Schema) + "' "
                + "AND table_name = '" + Sanitizer.Sanitize(table) + "' "
                + "AND column_name = '" + Sanitizer.Sanitize(column) + "';",
                false,
                token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return result.Rows[0]["udt_name"].ToString();
        }

        internal string QualifiedTable(string table)
        {
            return QuoteIdentifier(Schema) + "." + QuoteIdentifier(table);
        }
    }
}
