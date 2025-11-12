namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;

    /// <summary>
    /// Admin methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class AdminMethods : IAdminMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Admin methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public AdminMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task Backup(string outputFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(outputFilename)) throw new ArgumentNullException(nameof(outputFilename));
            token.ThrowIfCancellationRequested();
            string query = "VACUUM INTO '" + Sanitizer.Sanitize(outputFilename) + "';";
            await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
