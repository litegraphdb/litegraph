namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Vector index methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class VectorIndexMethods : IVectorIndexMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Vector index methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public VectorIndexMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<VectorIndexConfiguration> GetConfiguration(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph graph = await _Repo.Graph.ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            if (graph == null) return null;

            return new VectorIndexConfiguration(graph);
        }

        /// <inheritdoc />
        public async Task<VectorIndexStatistics> GetStatistics(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task EnableVectorIndex(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration configuration, CancellationToken token = default)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (!configuration.IsValid(out string errorMessage))
                throw new ArgumentException($"Invalid vector index configuration: {errorMessage}");
            token.ThrowIfCancellationRequested();

            await _Repo.Graph.EnableVectorIndexingAsync(tenantGuid, graphGuid, configuration, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndex(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.Graph.RebuildVectorIndexAsync(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteVectorIndex(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.Graph.DisableVectorIndexingAsync(tenantGuid, graphGuid, deleteIndexFile, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}