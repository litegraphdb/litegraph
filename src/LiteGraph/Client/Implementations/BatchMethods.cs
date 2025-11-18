namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Batch methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class BatchMethods : IBatchMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Batch methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public BatchMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ExistenceResult> Existence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.ContainsExistenceRequest()) throw new ArgumentException("Supplied existence request contains no valid existence filters.");
            token.ThrowIfCancellationRequested();

            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            return await _Repo.Batch.Existence(tenantGuid, graphGuid, req, token).ConfigureAwait(false);  
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
