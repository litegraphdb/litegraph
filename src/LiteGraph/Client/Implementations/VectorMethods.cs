namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization.Json;
    using System.Text;
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
        public VectorMetadata Create(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            _Client.ValidateGraphExists(vector.TenantGUID, vector.GraphGUID);
            if (vector.NodeGUID != null) _Client.ValidateNodeExists(vector.TenantGUID, vector.NodeGUID.Value);
            if (vector.EdgeGUID != null) _Client.ValidateEdgeExists(vector.TenantGUID, vector.EdgeGUID.Value);
            VectorMetadata created = _Repo.Vector.Create(vector);
            _Client.Logging.Log(SeverityEnum.Info, "created vector " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public List<VectorMetadata> CreateMany(Guid tenantGuid, List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count < 1) return null;

            _Client.ValidateTenantExists(tenantGuid);

            foreach (VectorMetadata vector in vectors)
            {
                if (string.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
                if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
                if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

                vector.TenantGUID = tenantGuid;
                
                _Client.ValidateGraphExists(vector.TenantGUID, vector.GraphGUID);
                if (vector.NodeGUID != null) _Client.ValidateNodeExists(vector.TenantGUID, vector.NodeGUID.Value);
                if (vector.EdgeGUID != null) _Client.ValidateEdgeExists(vector.TenantGUID, vector.EdgeGUID.Value);
            }

            return _Repo.Vector.CreateMany(tenantGuid, vectors);
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.ValidateTenantExists(tenantGuid);
            
            foreach (VectorMetadata vector in _Repo.Vector.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadAllInGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);

            foreach (VectorMetadata vector in _Repo.Vector.ReadAllInGraph(tenantGuid, graphGuid, order, skip))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving vectors");

            IEnumerable<VectorMetadata> vectors;

            if (graphGuid != null && nodeGuid != null && edgeGuid == null)
            {
                vectors = _Repo.Vector.ReadManyNode(tenantGuid, graphGuid.Value, nodeGuid.Value, order, skip);
            }
            else if (graphGuid != null && nodeGuid == null && edgeGuid != null)
            {
                vectors = _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid.Value, nodeGuid.Value, order, skip);
            }
            else if (graphGuid != null)
            {
                vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graphGuid.Value, order, skip);
            }
            else
            {
                vectors = _Repo.Vector.ReadMany(tenantGuid, null, null, null, order, skip);
            }

            if (vectors != null)
            {
                foreach (VectorMetadata vector in vectors)
                {
                    yield return vector;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            foreach (VectorMetadata vector in _Repo.Vector.ReadManyGraph(tenantGuid, graphGuid, order, skip))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            foreach (VectorMetadata vector in _Repo.Vector.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip))
            {
                yield return vector;
            }
        }

        /// <inheritdoc />
        public VectorMetadata ReadByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving vector with GUID " + guid);

            return _Repo.Vector.ReadByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving vectors");
            foreach (VectorMetadata obj in _Repo.Vector.ReadByGuids(tenantGuid, guids))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<VectorMetadata> Enumerate(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Repo.Vector.Enumerate(query);
        }

        /// <inheritdoc />
        public VectorMetadata Update(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            _Client.ValidateTenantExists(vector.TenantGUID);
            _Client.ValidateGraphExists(vector.TenantGUID, vector.GraphGUID);
            if (vector.NodeGUID != null) _Client.ValidateNodeExists(vector.TenantGUID, vector.NodeGUID.Value);
            if (vector.EdgeGUID != null) _Client.ValidateEdgeExists(vector.TenantGUID, vector.EdgeGUID.Value);
            vector = _Repo.Vector.Update(vector);
            _Client.Logging.Log(SeverityEnum.Debug, "updated vector GUID " + vector.GUID);
            return vector;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            VectorMetadata vector = ReadByGuid(tenantGuid, guid);
            if (vector == null) return;
            _Repo.Vector.DeleteByGuid(tenantGuid, guid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vector GUID " + vector.GUID);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Vector.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Vector.DeleteMany(tenantGuid, guids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Vector.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Vector.DeleteAllInGraph(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors in graph " + graphGuid);
        }

        /// <inheritdoc />
        public void DeleteGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Vector.DeleteGraphVectors(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors for graph " + graphGuid);
        }

        /// <inheritdoc />
        public void DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);
            _Repo.Vector.DeleteNodeVectors(tenantGuid, graphGuid, nodeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors for node " + nodeGuid);
        }

        /// <inheritdoc />
        public void DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateEdgeExists(tenantGuid, edgeGuid);
            _Repo.Vector.DeleteEdgeVectors(tenantGuid, graphGuid, edgeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted vectors for edge " + edgeGuid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            return _Repo.Vector.ExistsByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> Search(VectorSearchRequest searchReq)
        {
            if (searchReq == null) throw new ArgumentNullException(nameof(searchReq));
            return Search(
                searchReq.Domain,
                searchReq.SearchType,
                searchReq.Embeddings,
                searchReq.TenantGUID,
                searchReq.GraphGUID,
                searchReq.Labels,
                searchReq.Tags,
                searchReq.Expr);
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> Search(
            VectorSearchDomainEnum domain,
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid? graphGuid = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must include at least one value.");

            if (domain == VectorSearchDomainEnum.Graph)
            {
                return _Repo.Vector.SearchGraph(searchType, vectors, tenantGuid, labels, tags, filter);
            }
            else if (domain == VectorSearchDomainEnum.Node)
            {
                if (graphGuid == null) throw new ArgumentException("Graph GUID must be supplied when performing a node vector search.");
                return _Repo.Vector.SearchNode(searchType, vectors, tenantGuid, graphGuid.Value, labels, tags, filter);
            }
            else if (domain == VectorSearchDomainEnum.Edge)
            {
                if (graphGuid == null) throw new ArgumentException("Graph GUID must be supplied when performing an edge vector search.");
                return _Repo.Vector.SearchEdge(searchType, vectors, tenantGuid, graphGuid.Value, labels, tags, filter);
            }
            else
            {
                throw new ArgumentException("Unknown vector search domain '" + domain.ToString() + "'.");
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchGraph(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (VectorSearchResult result in _Repo.Vector.SearchGraph(searchType, vectors, tenantGuid, labels, tags, filter))
            {
                yield return result;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchNode(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (VectorSearchResult result in _Repo.Vector.SearchNode(searchType, vectors, tenantGuid, graphGuid, labels, tags, filter))
            {
                yield return result;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchEdge(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (VectorSearchResult result in _Repo.Vector.SearchEdge(searchType, vectors, tenantGuid, graphGuid, labels, tags, filter))
            {
                yield return result;
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
