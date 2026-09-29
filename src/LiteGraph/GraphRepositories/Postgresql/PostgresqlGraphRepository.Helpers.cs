namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Postgresql.Queries;
    using Npgsql;

    public partial class PostgresqlGraphRepository
    {
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _TransactionSemaphore.Wait();
                try
                {
                    lock (_QueryLock)
                    {
                        if (_Transaction != null)
                        {
                            try { _Transaction.Rollback(); } catch { }
                            ClearGraphTransaction();
                        }

                        if (_OwnsDataSource) _DataSource?.Dispose();
                    }
                }
                finally
                {
                    _TransactionSemaphore.Release();
                }
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsyncCore()
        {
            if (Disposed) return;

            await _TransactionSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                NpgsqlTransaction transaction = null;
                lock (_QueryLock)
                {
                    transaction = _Transaction;
                }

                if (transaction != null)
                {
                    try { await transaction.RollbackAsync().ConfigureAwait(false); } catch { }
                }

                await ClearGraphTransactionAsync().ConfigureAwait(false);
            }
            finally
            {
                _TransactionSemaphore.Release();
            }

            if (_OwnsDataSource) await _DataSource.DisposeAsync().ConfigureAwait(false);

            base.Dispose(true);
        }

        private void ClearGraphTransaction()
        {
            NpgsqlTransaction transaction = _Transaction;
            NpgsqlConnection conn = _TransactionConnection;

            _Transaction = null;
            _TransactionConnection = null;
            _GraphTransactionTenantGUID = null;
            _GraphTransactionGraphGUID = null;

            try { transaction?.Dispose(); } catch { }
            try { conn?.Close(); } catch { }
            try { conn?.Dispose(); } catch { }
        }

        private async Task ClearGraphTransactionAsync()
        {
            NpgsqlTransaction transaction;
            NpgsqlConnection conn;

            lock (_QueryLock)
            {
                transaction = _Transaction;
                conn = _TransactionConnection;

                _Transaction = null;
                _TransactionConnection = null;
                _GraphTransactionTenantGUID = null;
                _GraphTransactionGraphGUID = null;
            }

            if (transaction != null)
            {
                try { await transaction.DisposeAsync().ConfigureAwait(false); } catch { }
            }

            if (conn != null)
            {
                try { await conn.CloseAsync().ConfigureAwait(false); } catch { }
                try { await conn.DisposeAsync().ConfigureAwait(false); } catch { }
            }
        }

        private Task EnsureRequestHistoryTransactionDiagnosticsColumnAsync(CancellationToken token)
        {
            return ExecuteQueryAsync("ALTER TABLE " + QuoteIdentifier(Schema) + "." + QuoteIdentifier("requesthistory") + " ADD COLUMN IF NOT EXISTS transactiondiagnosticsjson TEXT;", true, token);
        }

        private async Task EnsureUserAdminFlagColumnsAsync(CancellationToken token)
        {
            await ExecuteQueryAsync("ALTER TABLE " + QuoteIdentifier(Schema) + "." + QuoteIdentifier("users") + " ADD COLUMN IF NOT EXISTS issystemadmin INT NOT NULL DEFAULT 0;", true, token).ConfigureAwait(false);
            await ExecuteQueryAsync("ALTER TABLE " + QuoteIdentifier(Schema) + "." + QuoteIdentifier("users") + " ADD COLUMN IF NOT EXISTS istenantadmin INT NOT NULL DEFAULT 0;", true, token).ConfigureAwait(false);
        }

        private Task EnsureChatEndpointContextWindowColumnAsync(CancellationToken token)
        {
            return ExecuteQueryAsync("ALTER TABLE " + QuoteIdentifier(Schema) + "." + QuoteIdentifier("chatendpoints") + " ADD COLUMN IF NOT EXISTS contextwindowtokens INT NOT NULL DEFAULT 0;", true, token);
        }

        private async Task EnsureBuiltInAuthorizationRolesAsync(CancellationToken token)
        {
            bool changed = false;

            foreach (RoleDefinition definition in AuthorizationPolicyDefinitions.BuiltInRoles)
            {
                token.ThrowIfCancellationRequested();
                AuthorizationRole role = AuthorizationRole.FromDefinition(definition);
                DataTable existing = await ExecuteQueryAsync(AuthorizationRoleQueries.SelectRoleByName(null, role.Name), false, token).ConfigureAwait(false);

                if (existing != null && existing.Rows.Count > 0)
                {
                    DataRow row = existing.Rows[0];
                    string guid = Converters.GetDataRowStringValue(row, "guid");
                    if (!String.IsNullOrEmpty(guid) && Guid.TryParse(guid, out Guid parsedGuid))
                        role.GUID = parsedGuid;

                    string created = Converters.GetDataRowStringValue(row, "createdutc");
                    if (!String.IsNullOrEmpty(created) && DateTime.TryParse(created, out DateTime parsedCreated))
                        role.CreatedUtc = DateTime.SpecifyKind(parsedCreated, DateTimeKind.Utc);

                    await ExecuteQueryAsync(AuthorizationRoleQueries.UpdateRole(role), true, token).ConfigureAwait(false);
                    changed = true;
                }
                else
                {
                    await ExecuteQueryAsync(AuthorizationRoleQueries.InsertRole(role), true, token).ConfigureAwait(false);
                    changed = true;
                }
            }

            if (changed) AuthorizationPolicyChangeTracker.SignalChanged();
        }

        private static NpgsqlConnectionStringBuilder BuildConnectionString(DatabaseSettings settings)
        {
            NpgsqlConnectionStringBuilder builder = !String.IsNullOrWhiteSpace(settings.ConnectionString)
                ? new NpgsqlConnectionStringBuilder(settings.ConnectionString)
                : new NpgsqlConnectionStringBuilder
                {
                    Host = settings.Hostname,
                    Database = settings.DatabaseName
                };

            if (settings.Port.HasValue) builder.Port = settings.Port.Value;
            if (!String.IsNullOrWhiteSpace(settings.Username)) builder.Username = settings.Username;
            if (!String.IsNullOrWhiteSpace(settings.Password)) builder.Password = settings.Password;

            HashSet<string> providedKeys = !String.IsNullOrWhiteSpace(settings.ConnectionString)
                ? ExtractConnectionStringKeys(settings.ConnectionString)
                : new HashSet<string>(StringComparer.Ordinal);

            if (!providedKeys.Contains("maximumpoolsize") && !providedKeys.Contains("maxpoolsize"))
                builder.MaxPoolSize = settings.MaxConnections;
            if (!providedKeys.Contains("commandtimeout"))
                builder.CommandTimeout = settings.CommandTimeoutSeconds;

            builder.Pooling = true;
            return builder;
        }

        private static HashSet<string> ExtractConnectionStringKeys(string connectionString)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string pair in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int separatorIndex = pair.IndexOf('=');
                if (separatorIndex <= 0) continue;
                string key = pair.Substring(0, separatorIndex).Trim().Replace(" ", String.Empty).ToLowerInvariant();
                if (key.Length > 0) keys.Add(key);
            }
            return keys;
        }

        private static string NormalizeSchema(string schema)
        {
            if (String.IsNullOrWhiteSpace(schema)) return "litegraph";
            foreach (char c in schema)
            {
                if (!(Char.IsLetterOrDigit(c) || c == '_'))
                    throw new ArgumentException("PostgreSQL schema names may contain only letters, digits, and underscores.", nameof(schema));
            }
            return schema;
        }

        internal static string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
