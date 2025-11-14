namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Vector methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class VectorMethods : IVectorMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Vector methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public VectorMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<VectorMetadata> Create(VectorMetadata vector, CancellationToken token = default)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateGraphExists(vector.TenantGUID, vector.GraphGUID, token).ConfigureAwait(false);
            if (vector.NodeGUID != null) await _Client.ValidateNodeExists(vector.TenantGUID, vector.NodeGUID.Value, token).ConfigureAwait(false);
            if (vector.EdgeGUID != null) await _Client.ValidateEdgeExists(vector.TenantGUID, vector.EdgeGUID.Value, token).ConfigureAwait(false);
            VectorMetadata created = await _Repo.Vector.Create(vector, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created vector " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> CreateMany(Guid tenantGuid, List<VectorMetadata> vectors, CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) return null;
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);

            foreach (VectorMetadata vector in vectors)
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
                if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
                if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

                vector.TenantGUID = tenantGuid;
                
                await _Client.ValidateGraphExists(vector.TenantGUID, vector.GraphGUID, token).ConfigureAwait(false);
                if (vector.NodeGUID != null) await _Client.ValidateNodeExists(vector.TenantGUID, vector.NodeGUID.Value, token).ConfigureAwait(false);
                if (vector.EdgeGUID != null) await _Client.ValidateEdgeExists(vector.TenantGUID, vector.EdgeGUID.Value, token).ConfigureAwait(false);
            }

            return await _Repo.Vector.CreateMany(tenantGuid, vectors, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            
            await foreach (VectorMetadata vector in _Repo.Vector.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadAllInGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await foreach (VectorMetadata vector in _Repo.Vector.ReadAllInGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving vectors");

            IAsyncEnumerable<VectorMetadata> vectors;

            if (graphGuid != null && nodeGuid != null && edgeGuid == null)
            {
                vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid.Value, nodeGuid.Value, order, skip, token);
            }
            else if (graphGuid != null && nodeGuid == null && edgeGuid != null)
            {
                vectors = _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid.Value, edgeGuid.Value, order, skip, token);
            }
            else if (graphGuid != null)
            {
                vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graphGuid.Value, order, skip, token);
            }
            else
            {
                vectors = _Repo.Vector.ReadMany(tenantGuid, null, null, null, order, skip, token);
            }

            await foreach (VectorMetadata vector in vectors.WithCancellation(token).ConfigureAwait(false))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public async Task<VectorMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving vector with GUID " + guid);
            token.ThrowIfCancellationRequested();

            return await _Repo.Vector.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving vectors");
            await foreach (VectorMetadata obj in _Repo.Vector.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<VectorMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            return await _Repo.Vector.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorMetadata> Update(VectorMetadata vector, CancellationToken token = default)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(vector.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(vector.TenantGUID, vector.GraphGUID, token).ConfigureAwait(false);
            if (vector.NodeGUID != null) await _Client.ValidateNodeExists(vector.TenantGUID, vector.NodeGUID.Value, token).ConfigureAwait(false);
            if (vector.EdgeGUID != null) await _Client.ValidateEdgeExists(vector.TenantGUID, vector.EdgeGUID.Value, token).ConfigureAwait(false);
            vector = await _Repo.Vector.Update(vector, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "updated vector GUID " + vector.GUID);
            return vector;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            VectorMetadata vector = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            if (vector == null) return;
            await _Repo.Vector.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vector GUID " + vector.GUID);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteMany(tenantGuid, guids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteAllInGraph(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in graph " + graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteGraphVectors(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteGraphVectors(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors for graph " + graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteNodeVectors(tenantGuid, graphGuid, nodeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors for node " + nodeGuid);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateEdgeExists(tenantGuid, edgeGuid, token).ConfigureAwait(false);
            await _Repo.Vector.DeleteEdgeVectors(tenantGuid, graphGuid, edgeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors for edge " + edgeGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.Vector.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorSearchResult> Search(VectorSearchRequest searchReq, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (searchReq == null) throw new ArgumentNullException(nameof(searchReq));
            token.ThrowIfCancellationRequested();
            await foreach (VectorSearchResult result in Search(
                searchReq.Domain,
                searchReq.SearchType,
                searchReq.Embeddings,
                searchReq.TenantGUID,
                searchReq.GraphGUID,
                searchReq.Labels,
                searchReq.Tags,
                searchReq.Expr,
                searchReq.TopK,
                searchReq.MinimumScore,
                searchReq.MaximumDistance,
                searchReq.MinimumInnerProduct,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return result;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorSearchResult> Search(
            VectorSearchDomainEnum domain,
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid? graphGuid = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            int? topK = 100,
            float? minScore = 0.0f,
            float? maxDistance = float.MaxValue,
            float? minInnerProduct = 0.0f,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must include at least one value.");
            token.ThrowIfCancellationRequested();

            if (domain == VectorSearchDomainEnum.Graph)
            {
                await foreach (VectorSearchResult result in _Repo.Vector.SearchGraph(
                    searchType, 
                    vectors, 
                    tenantGuid, 
                    labels, 
                    tags, 
                    filter,
                    topK,
                    minScore,
                    maxDistance,
                    minInnerProduct,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    List<LabelMetadata> graphLabels = new List<LabelMetadata>();
                    await foreach (LabelMetadata label in _Repo.Label.ReadMany(tenantGuid, result.Graph.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        graphLabels.Add(label);
                    }
                    result.Graph.Labels = LabelMetadata.ToListString(graphLabels);
                    
                    List<TagMetadata> graphTags = new List<TagMetadata>();
                    await foreach (TagMetadata tag in _Repo.Tag.ReadMany(tenantGuid, result.Graph.GUID, null, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        graphTags.Add(tag);
                    }
                    result.Graph.Tags = TagMetadata.ToNameValueCollection(graphTags);
                    yield return result;
                }
            }
            else if (domain == VectorSearchDomainEnum.Node)
            {
                if (graphGuid == null) throw new ArgumentException("Graph GUID must be supplied when performing a node vector search.");
                
                await foreach (VectorSearchResult result in _Repo.Vector.SearchNode(
                    searchType, 
                    vectors, 
                    tenantGuid, 
                    graphGuid.Value, 
                    labels, 
                    tags, 
                    filter,
                    topK,
                    minScore,
                    maxDistance,
                    minInnerProduct,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    List<LabelMetadata> nodeLabels = new List<LabelMetadata>();
                    await foreach (LabelMetadata label in _Repo.Label.ReadMany(tenantGuid, result.Node.GraphGUID, result.Node.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        nodeLabels.Add(label);
                    }
                    result.Node.Labels = LabelMetadata.ToListString(nodeLabels);
                    
                    List<TagMetadata> nodeTags = new List<TagMetadata>();
                    await foreach (TagMetadata tag in _Repo.Tag.ReadMany(tenantGuid, result.Node.GraphGUID, result.Node.GUID, null, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        nodeTags.Add(tag);
                    }
                    result.Node.Tags = TagMetadata.ToNameValueCollection(nodeTags);
                    yield return result;
                }
            }
            else if (domain == VectorSearchDomainEnum.Edge)
            {
                if (graphGuid == null) throw new ArgumentException("Graph GUID must be supplied when performing an edge vector search.");
                
                await foreach (VectorSearchResult result in _Repo.Vector.SearchEdge(
                    searchType, 
                    vectors, 
                    tenantGuid, 
                    graphGuid.Value, 
                    labels,
                    tags, 
                    filter,
                    topK,
                    minScore,
                    maxDistance,
                    minInnerProduct,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    List<LabelMetadata> edgeLabels = new List<LabelMetadata>();
                    await foreach (LabelMetadata label in _Repo.Label.ReadMany(tenantGuid, result.Edge.GraphGUID, null, result.Edge.GUID, null, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        edgeLabels.Add(label);
                    }
                    result.Edge.Labels = LabelMetadata.ToListString(edgeLabels);
                    
                    List<TagMetadata> edgeTags = new List<TagMetadata>();
                    await foreach (TagMetadata tag in _Repo.Tag.ReadMany(tenantGuid, result.Edge.GraphGUID, null, result.Edge.GUID, null, null, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        edgeTags.Add(tag);
                    }
                    result.Edge.Tags = TagMetadata.ToNameValueCollection(edgeTags);
                    yield return result;
                }
            }
            else
            {
                throw new ArgumentException("Unknown vector search domain '" + domain.ToString() + "'.");
            }
        }

        #endregion

        #region Private-Methods

        private void CompareVectors(
            VectorSearchTypeEnum searchType,
            List<float> vectors1,
            List<float> vectors2,
            out float? score,
            out float? distance,
            out float? innerProduct)
        {
            score = null;
            distance = null;
            innerProduct = null;

            if (searchType == VectorSearchTypeEnum.CosineDistance)
                distance = VectorHelper.CalculateCosineDistance(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.CosineSimilarity)
                score = VectorHelper.CalculateCosineSimilarity(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.DotProduct)
                innerProduct = VectorHelper.CalculateInnerProduct(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.EuclidianDistance)
                distance = VectorHelper.CalculateEuclidianDistance(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.EuclidianSimilarity)
                score = VectorHelper.CalculateEuclidianSimilarity(vectors1, vectors2);
            else
            {
                throw new ArgumentException("Unknown vector search type " + searchType.ToString() + ".");
            }
        }

        #endregion
    }
}
