namespace LiteGraph.GraphRepositories
{
    using System.Data;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Loads query results into a <see cref="DataTable"/>.
    /// <see cref="DataTable.Load(IDataReader)"/> is annotated as unsafe for trimming because a table may contain expression
    /// columns, whose evaluation can reach members of arbitrary types through reflection. LiteGraph result tables are
    /// created empty and filled only from data readers, so they never have expression columns. Native AOT runs of the
    /// SQLite and PostgreSQL repositories (Test.Aot) verify this.
    /// Keeping <see cref="DataTable.Load(IDataReader)"/> (rather than copying rows by hand) preserves its exact semantics:
    /// key and unique constraints from the reader's schema, merging of rows with the same key, and advancing the reader to
    /// the next result set.
    /// Thread safety: stateless; the table and reader are not thread-safe and must not be shared during the call.
    /// </summary>
    internal static class DataTableLoader
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private const string _Justification =
            "LiteGraph result tables never define DataColumn expressions, the only feature behind the annotation; "
            + "verified under Native AOT by Test.Aot.";

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Load the reader's current result set into the table, then advance the reader as
        /// <see cref="DataTable.Load(IDataReader)"/> does.
        /// </summary>
        /// <param name="table">Table without expression columns.</param>
        /// <param name="reader">Data reader.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = _Justification)]
        internal static void Load(DataTable table, IDataReader reader)
        {
            table.Load(reader);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
