namespace LiteGraph.Client.Implementations
{
    using LiteGraph.GraphRepositories;
    using LiteGraph.Indexing.Vector;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Vector index methods implementation for the client.
    /// Provides client-side validation and error handling for vector index operations.
    /// </summary>
    public class VectorIndexMethods : Interfaces.IVectorIndexMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly LiteGraphClient _Client;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate VectorIndexMethods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public VectorIndexMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public VectorIndexConfiguration GetConfiguration(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            return _Repo.VectorIndex.GetConfiguration(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public VectorIndexStatistics GetStatistics(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            return _Repo.VectorIndex.GetStatistics(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public async Task EnableVectorIndexAsync(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await _Repo.VectorIndex.EnableVectorIndexAsync(tenantGuid, graphGuid, configuration);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndexAsync(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await _Repo.VectorIndex.RebuildVectorIndexAsync(tenantGuid, graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteVectorIndexAsync(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            await _Repo.VectorIndex.DeleteVectorIndexAsync(tenantGuid, graphGuid, deleteIndexFile);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
