namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Implementations;
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
        public VectorIndexConfiguration GetConfiguration(Guid tenantGuid, Guid graphGuid)
        {
            Graph graph = _Repo.Graph.ReadByGuid(tenantGuid, graphGuid);
            if (graph == null) return null;

            return new VectorIndexConfiguration(graph);
        }

        /// <inheritdoc />
        public VectorIndexStatistics GetStatistics(Guid tenantGuid, Guid graphGuid)
        {
            return _Repo.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public async Task EnableVectorIndexAsync(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (!configuration.IsValid(out string errorMessage))
                throw new ArgumentException($"Invalid vector index configuration: {errorMessage}");

            await _Repo.Graph.EnableVectorIndexingAsync(tenantGuid, graphGuid, configuration);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndexAsync(Guid tenantGuid, Guid graphGuid)
        {
            await _Repo.Graph.RebuildVectorIndexAsync(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteVectorIndexAsync(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false)
        {
            await _Repo.Graph.DisableVectorIndexingAsync(tenantGuid, graphGuid, deleteIndexFile);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}