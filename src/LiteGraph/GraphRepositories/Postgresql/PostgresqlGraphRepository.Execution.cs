namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;

    public partial class PostgresqlGraphRepository
    {
        internal DataTable ExecuteQuery(string query, bool isTransaction = false)
        {
            return ExecuteRepositoryOperation(
                ClassifySqlOperation(query, isTransaction),
                isTransaction,
                1,
                () => ExecuteQueryCore(query, isTransaction));
        }

        internal async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            return await ExecuteRepositoryOperationAsync(
                ClassifySqlOperation(query, isTransaction),
                isTransaction,
                1,
                async () => await ExecuteQueryCoreAsync(query, isTransaction, token, true).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Execute PostgreSQL-native SQL without SQLite-dialect translation.  Callers must schema-qualify every table.
        /// </summary>
        internal async Task<DataTable> ExecuteNativeQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            return await ExecuteRepositoryOperationAsync(
                ClassifySqlOperation(query, isTransaction),
                isTransaction,
                1,
                async () => await ExecuteQueryCoreAsync(query, isTransaction, token, false).ConfigureAwait(false)).ConfigureAwait(false);
        }

        internal DataTable ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false)
        {
            List<string> queryList = queries?.Where(q => !String.IsNullOrWhiteSpace(q)).ToList();
            return ExecuteRepositoryOperation(
                "batch",
                isTransaction,
                queryList?.Count ?? 0,
                () => ExecuteQueriesCore(queryList, isTransaction));
        }

        private DataTable ExecuteQueryCore(string query, bool isTransaction)
        {
            ThrowIfDisposed();
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            if (_Transaction != null)
            {
                _TransactionSemaphore.Wait();
                try
                {
                    NpgsqlTransaction active = GetActiveTransactionOrThrow();
                    return ExecuteOnConnection(active.Connection, active, query);
                }
                finally
                {
                    _TransactionSemaphore.Release();
                }
            }

            using (NpgsqlConnection conn = _DataSource.OpenConnection())
            {
                if (isTransaction)
                {
                    using (NpgsqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            DataTable result = ExecuteOnConnection(conn, transaction, query);
                            transaction.Commit();
                            return result;
                        }
                        catch
                        {
                            try { transaction.Rollback(); } catch { }
                            throw;
                        }
                    }
                }

                return ExecuteOnConnection(conn, null, query);
            }
        }

        private async Task<DataTable> ExecuteQueryCoreAsync(string query, bool isTransaction, CancellationToken token, bool translate)
        {
            ThrowIfDisposed();
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");
            token.ThrowIfCancellationRequested();

            if (_Transaction != null)
            {
                await _TransactionSemaphore.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    NpgsqlTransaction active = GetActiveTransactionOrThrow();
                    return await ExecuteOnConnectionAsync(active.Connection, active, query, token, translate).ConfigureAwait(false);
                }
                finally
                {
                    _TransactionSemaphore.Release();
                }
            }

            await using (NpgsqlConnection conn = await _DataSource.OpenConnectionAsync(token).ConfigureAwait(false))
            {
                if (isTransaction)
                {
                    await using (NpgsqlTransaction transaction = await conn.BeginTransactionAsync(token).ConfigureAwait(false))
                    {
                        try
                        {
                            DataTable result = await ExecuteOnConnectionAsync(conn, transaction, query, token, translate).ConfigureAwait(false);
                            await transaction.CommitAsync(token).ConfigureAwait(false);
                            return result;
                        }
                        catch
                        {
                            try { await transaction.RollbackAsync(token).ConfigureAwait(false); } catch { }
                            throw;
                        }
                    }
                }

                return await ExecuteOnConnectionAsync(conn, null, query, token, translate).ConfigureAwait(false);
            }
        }

        private DataTable ExecuteQueriesCore(IEnumerable<string> queries, bool isTransaction)
        {
            ThrowIfDisposed();
            if (queries == null || !queries.Any()) throw new ArgumentNullException(nameof(queries));

            if (_Transaction != null)
            {
                _TransactionSemaphore.Wait();
                try
                {
                    NpgsqlTransaction active = GetActiveTransactionOrThrow();
                    DataTable result = new DataTable();
                    foreach (string query in queries.Where(q => !String.IsNullOrWhiteSpace(q)))
                    {
                        DataTable current = ExecuteOnConnection(active.Connection, active, query);
                        if (current.Rows.Count > 0) result = current;
                    }
                    return result;
                }
                finally
                {
                    _TransactionSemaphore.Release();
                }
            }

            using (NpgsqlConnection conn = _DataSource.OpenConnection())
            {
                NpgsqlTransaction transaction = null;
                try
                {
                    if (isTransaction) transaction = conn.BeginTransaction();

                    DataTable result = new DataTable();
                    foreach (string query in queries.Where(q => !String.IsNullOrWhiteSpace(q)))
                    {
                        DataTable current = ExecuteOnConnection(conn, transaction, query);
                        if (current.Rows.Count > 0) result = current;
                    }

                    transaction?.Commit();
                    return result;
                }
                catch
                {
                    try { transaction?.Rollback(); } catch { }
                    throw;
                }
                finally
                {
                    transaction?.Dispose();
                }
            }
        }

        private NpgsqlTransaction GetActiveTransactionOrThrow()
        {
            lock (_QueryLock)
            {
                if (_Transaction == null) throw new InvalidOperationException("The graph transaction ended before the query could run.");
                return _Transaction;
            }
        }

        private DataTable ExecuteOnConnection(NpgsqlConnection conn, NpgsqlTransaction transaction, string query)
        {
            string translated = PostgresqlSqlTranslator.Translate(query, Schema);
            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + translated);

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(translated, conn))
                {
                    cmd.CommandTimeout = Settings.CommandTimeoutSeconds;
                    if (transaction != null) cmd.Transaction = transaction;
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        DataTable result = LoadAllResultSets(rdr);
                        if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                e.Data["Query"] = translated;
                throw;
            }
        }

        private async Task<DataTable> ExecuteOnConnectionAsync(NpgsqlConnection conn, NpgsqlTransaction transaction, string query, CancellationToken token, bool translate)
        {
            string translated = translate ? PostgresqlSqlTranslator.Translate(query, Schema) : query;
            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + translated);

            try
            {
                await using (NpgsqlCommand cmd = new NpgsqlCommand(translated, conn))
                {
                    cmd.CommandTimeout = Settings.CommandTimeoutSeconds;
                    if (transaction != null) cmd.Transaction = transaction;
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        DataTable result = LoadAllResultSets(rdr);
                        if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                e.Data["Query"] = translated;
                throw;
            }
        }

        private static DataTable LoadAllResultSets(IDataReader rdr)
        {
            DataTable result = new DataTable();

            do
            {
                DataTable current = new DataTable();
                DataTableLoader.Load(current, rdr);
                if (current.Rows.Count > 0 || current.Columns.Count > 0)
                    result = current;
            }
            while (!rdr.IsClosed && rdr.NextResult());

            return result;
        }
    }
}
