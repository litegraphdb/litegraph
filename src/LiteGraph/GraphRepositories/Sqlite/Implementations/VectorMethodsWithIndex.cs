namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Extension methods for VectorMethods to integrate with vector indexing.
    /// </summary>
    public static class VectorMethodsIndexExtensions
    {
        /// <summary>
        /// Update index when a vector is created.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="vector">Created vector.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForCreateAsync(SqliteGraphRepository repo, VectorMetadata vector)
        {
            if (vector == null || vector.Vectors == null || vector.Vectors.Count == 0) return;

            Graph graph = await repo.Graph.ReadByGuid(vector.TenantGUID, vector.GraphGUID).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            IVectorIndex index = await repo.VectorIndexManager.GetOrCreateIndexAsync(graph);
            if (index != null && vector.NodeGUID.HasValue)
            {
                // Store node GUID in the index so we can retrieve nodes from search results
                await index.AddAsync(vector.NodeGUID.Value, vector.Vectors);
            }
        }

        /// <summary>
        /// Update index when multiple vectors are created.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="vectors">Created vectors.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForCreateManyAsync(SqliteGraphRepository repo, List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count == 0) return;

            // Group vectors by graph
            IEnumerable<IGrouping<Guid, VectorMetadata>> vectorsByGraph = vectors
                .Where(v => v.Vectors != null && v.Vectors.Count > 0)
                .GroupBy(v => v.GraphGUID);

            foreach (IGrouping<Guid, VectorMetadata> graphGroup in vectorsByGraph)
            {
                Guid graphGuid = graphGroup.Key;
                List<VectorMetadata> graphVectors = graphGroup.ToList();
                
                if (graphVectors.Count == 0) continue;

                Graph graph = await repo.Graph.ReadByGuid(graphVectors[0].TenantGUID, graphGuid).ConfigureAwait(false);
                if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                    continue;

                IVectorIndex index = await repo.VectorIndexManager.GetOrCreateIndexAsync(graph);
                if (index != null)
                {
                    // Store node GUIDs in the index so we can retrieve nodes from search results
                    Dictionary<Guid, List<float>> batch = graphVectors.Where(v => v.NodeGUID.HasValue).ToDictionary(v => v.NodeGUID.Value, v => v.Vectors);
                    if (batch.Count > 0)
                        await index.AddBatchAsync(batch);
                }
            }
        }

        /// <summary>
        /// Update index when a vector is updated.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="vector">Updated vector.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForUpdateAsync(SqliteGraphRepository repo, VectorMetadata vector)
        {
            if (vector == null || vector.Vectors == null || vector.Vectors.Count == 0) return;

            Graph graph = await repo.Graph.ReadByGuid(vector.TenantGUID, vector.GraphGUID).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            IVectorIndex index = await repo.VectorIndexManager.GetOrCreateIndexAsync(graph);
            if (index != null && vector.NodeGUID.HasValue)
            {
                await index.UpdateAsync(vector.NodeGUID.Value, vector.Vectors);
            }
        }

        /// <summary>
        /// Update index when a node with vectors is deleted.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="nodeGuid">Node GUID (used as key in the index).</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForDeleteAsync(SqliteGraphRepository repo, Guid tenantGuid, Guid nodeGuid, Guid graphGuid)
        {
            Graph graph = await repo.Graph.ReadByGuid(tenantGuid, graphGuid).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            IVectorIndex index = repo.VectorIndexManager.GetIndex(graphGuid);
            if (index != null)
            {
                // Use nodeGuid as the key since that's what we store in the index
                await index.RemoveAsync(nodeGuid);
            }
        }

        /// <summary>
        /// Update index when multiple nodes with vectors are deleted.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="nodeGuids">Node GUIDs (used as keys in the index).</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForDeleteManyAsync(SqliteGraphRepository repo, Guid tenantGuid, List<Guid> nodeGuids, Guid graphGuid)
        {
            if (nodeGuids == null || nodeGuids.Count == 0) return;

            Graph graph = await repo.Graph.ReadByGuid(tenantGuid, graphGuid).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            IVectorIndex index = repo.VectorIndexManager.GetIndex(graphGuid);
            if (index != null)
            {
                // Use nodeGuids as keys since that's what we store in the index
                await index.RemoveBatchAsync(nodeGuids);
            }
        }

        /// <summary>
        /// Search using the vector index if available.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="searchType">Search type.</param>
        /// <param name="queryVector">Query vector.</param>
        /// <param name="graph">Graph to search in.</param>
        /// <param name="topK">Number of results.</param>
        /// <param name="ef">Search ef parameter.</param>
        /// <returns>Indexed search results or null if no index.</returns>
        public static async Task<List<VectorScoreResult>> SearchWithIndexAsync(
            SqliteGraphRepository repo,
            VectorSearchTypeEnum searchType,
            List<float> queryVector,
            Graph graph,
            int topK,
            int? ef = null)
        {
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return null;

            IVectorIndex index = await repo.VectorIndexManager.GetOrCreateIndexAsync(graph);
            if (index == null)
                return null;

            // Perform indexed search
            List<VectorDistanceResult> results = await index.SearchAsync(queryVector, topK, ef ?? graph.VectorIndexEf);

            // Convert distance to appropriate score based on search type
            List<VectorScoreResult> scoredResults = new List<VectorScoreResult>();
            foreach (VectorDistanceResult result in results)
            {
                float score = result.Distance;

                // Convert based on search type
                switch (searchType)
                {
                    case VectorSearchTypeEnum.CosineSimilarity:
                        // HnswLite returns cosine distance, convert to similarity
                        score = 1.0f - result.Distance;
                        break;
                    case VectorSearchTypeEnum.CosineDistance:
                        // Already in distance form
                        break;
                    case VectorSearchTypeEnum.EuclidianSimilarity:
                        // Convert distance to similarity
                        score = 1.0f / (1.0f + result.Distance);
                        break;
                    case VectorSearchTypeEnum.EuclidianDistance:
                        // Already in distance form
                        break;
                    case VectorSearchTypeEnum.DotProduct:
                        // For dot product, higher is better, so negate if it's a distance
                        score = -result.Distance;
                        break;
                }

                scoredResults.Add(new VectorScoreResult(result.Id, score));
            }

            return scoredResults;
        }
    }
}