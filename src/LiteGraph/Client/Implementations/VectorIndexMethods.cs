namespace LiteGraph.Client.Implementations
{
    using LiteGraph.GraphRepositories;
    using LiteGraph.Indexing.Vector;
    using System;
    using System.Threading;
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
        public async Task<VectorIndexConfiguration> GetConfiguration(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            return await _Repo.VectorIndex.GetConfiguration(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorIndexStatistics> GetStatistics(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            return await _Repo.VectorIndex.GetStatistics(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task EnableVectorIndex(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration configuration, CancellationToken token = default)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await _Repo.VectorIndex.EnableVectorIndex(tenantGuid, graphGuid, configuration, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndex(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await _Repo.VectorIndex.RebuildVectorIndex(tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteVectorIndex(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await _Repo.VectorIndex.DeleteVectorIndex(tenantGuid, graphGuid, deleteIndexFile, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
