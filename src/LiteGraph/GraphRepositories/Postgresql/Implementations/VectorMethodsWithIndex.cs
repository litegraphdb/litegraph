namespace LiteGraph.GraphRepositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
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
        public static async Task UpdateIndexForCreateAsync(PostgresqlGraphRepository repo, VectorMetadata vector)
        {
            if (vector == null || vector.Vectors == null || vector.Vectors.Count == 0) return;

            Graph graph = await repo.Graph.ReadByGuid(vector.TenantGUID, vector.GraphGUID).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            if (!vector.NodeGUID.HasValue) return;

            await ExecuteIndexMutationAsync(
                repo,
                graph,
                "Vector create index update failed",
                async index =>
                {
                    VectorIndexEntry entry = await BuildNodeIndexEntryAsync(repo, graph, vector).ConfigureAwait(false);
                    await index.AddAsync(entry).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Update index when multiple vectors are created.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="vectors">Created vectors.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForCreateManyAsync(PostgresqlGraphRepository repo, List<VectorMetadata> vectors)
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

                List<VectorIndexEntry> batch = await BuildNodeIndexEntriesAsync(repo, graph, graphVectors).ConfigureAwait(false);

                if (batch.Count < 1) continue;

                await ExecuteIndexMutationAsync(
                    repo,
                    graph,
                    "Vector batch create index update failed",
                    async index => await index.AddBatchAsync(batch).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update index when a vector is updated.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="vector">Updated vector.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForUpdateAsync(PostgresqlGraphRepository repo, VectorMetadata vector)
        {
            if (vector == null || vector.Vectors == null || vector.Vectors.Count == 0) return;

            Graph graph = await repo.Graph.ReadByGuid(vector.TenantGUID, vector.GraphGUID).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            if (!vector.NodeGUID.HasValue) return;

            await ExecuteIndexMutationAsync(
                repo,
                graph,
                "Vector update index update failed",
                async index =>
                {
                    VectorIndexEntry entry = await BuildNodeIndexEntryAsync(repo, graph, vector).ConfigureAwait(false);
                    await index.UpdateAsync(entry).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Update index when a node with vectors is deleted.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="nodeGuid">Node GUID (used as key in the index).</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForDeleteAsync(PostgresqlGraphRepository repo, Guid tenantGuid, Guid nodeGuid, Guid graphGuid)
        {
            Graph graph = await repo.Graph.ReadByGuid(tenantGuid, graphGuid).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            await ExecuteIndexMutationAsync(
                repo,
                graph,
                "Vector delete index update failed",
                async index => await index.RemoveAsync(nodeGuid).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Update index when multiple nodes with vectors are deleted.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="nodeGuids">Node GUIDs (used as keys in the index).</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Task.</returns>
        public static async Task UpdateIndexForDeleteManyAsync(PostgresqlGraphRepository repo, Guid tenantGuid, List<Guid> nodeGuids, Guid graphGuid)
        {
            if (nodeGuids == null || nodeGuids.Count == 0) return;

            Graph graph = await repo.Graph.ReadByGuid(tenantGuid, graphGuid).ConfigureAwait(false);
            if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return;

            await ExecuteIndexMutationAsync(
                repo,
                graph,
                "Vector batch delete index update failed",
                async index => await index.RemoveBatchAsync(nodeGuids).ConfigureAwait(false)).ConfigureAwait(false);
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
            PostgresqlGraphRepository repo,
            VectorSearchTypeEnum searchType,
            List<float> queryVector,
            Graph graph,
            int topK,
            int? ef = null)
        {
            using Activity activity = LiteGraphTelemetry.ActivitySource.StartActivity(LiteGraphTelemetry.VectorIndexSearchActivityName, ActivityKind.Internal);
            SetVectorIndexSearchActivityTags(activity, searchType, queryVector, graph, topK, ef);

            try
            {
                if (graph == null || !graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                {
                    activity?.SetTag("litegraph.vector.index.used", false);
                    activity?.SetTag("litegraph.vector.index.skip_reason", "not_configured");
                    LiteGraphTelemetry.SetActivityOk(activity);
                    return null;
                }

                if (graph.VectorIndexDirty)
                {
                    activity?.SetTag("litegraph.vector.index.used", false);
                    activity?.SetTag("litegraph.vector.index.skip_reason", "dirty");
                    LiteGraphTelemetry.SetActivityOk(activity);
                    return null;
                }

                List<VectorScoreResult> scoredResults = await repo.VectorIndexManager.ExecuteWithIndexAsync(
                    graph,
                    async index =>
                    {
                        // Perform indexed search
                        List<VectorDistanceResult> results = await index.SearchAsync(queryVector, topK, ef ?? graph.VectorIndexEf).ConfigureAwait(false);

                        // Convert distance to appropriate score based on search type
                        List<VectorScoreResult> scored = new List<VectorScoreResult>();
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

                            scored.Add(new VectorScoreResult(result.Id, score));
                        }

                        return scored;
                    }).ConfigureAwait(false);

                if (scoredResults == null)
                {
                    activity?.SetTag("litegraph.vector.index.used", false);
                    activity?.SetTag("litegraph.vector.index.skip_reason", "unavailable");
                    LiteGraphTelemetry.SetActivityOk(activity);
                    return null;
                }

                activity?.SetTag("litegraph.vector.index.used", true);
                activity?.SetTag("litegraph.vector.index.results", scoredResults.Count);
                LiteGraphTelemetry.SetActivityOk(activity);
                return scoredResults;
            }
            catch (Exception e)
            {
                LiteGraphTelemetry.SetActivityException(activity, e);
                throw;
            }
        }

        private static void SetVectorIndexSearchActivityTags(
            Activity activity,
            VectorSearchTypeEnum searchType,
            List<float> queryVector,
            Graph graph,
            int topK,
            int? ef)
        {
            if (activity == null) return;

            activity.SetTag("db.system", "litegraph");
            activity.SetTag("litegraph.vector.search_type", searchType.ToString());
            activity.SetTag("litegraph.vector.dimensions", queryVector?.Count ?? 0);
            activity.SetTag("litegraph.vector.top_k", topK);
            if (ef != null) activity.SetTag("litegraph.vector.index.ef", ef.Value);
            if (graph == null) return;

            activity.SetTag("litegraph.tenant_guid", graph.TenantGUID.ToString("D"));
            activity.SetTag("litegraph.graph_guid", graph.GUID.ToString("D"));
            activity.SetTag("litegraph.vector.index.type", graph.VectorIndexType?.ToString() ?? "None");
            activity.SetTag("litegraph.vector.index.dirty", graph.VectorIndexDirty);
        }

        private static async Task ExecuteIndexMutationAsync(
            PostgresqlGraphRepository repo,
            Graph graph,
            string dirtyReason,
            Func<IVectorIndex, Task> mutation)
        {
            try
            {
                await repo.VectorIndexManager.ExecuteWithIndexAsync(graph, mutation).ConfigureAwait(false);
                repo.NoteVectorIndexMutation(graph.TenantGUID, graph.GUID, dirtyReason);
            }
            catch (Exception e)
            {
                await MarkIndexDirtyAfterFailureAsync(repo, graph, dirtyReason, e).ConfigureAwait(false);
            }
        }

        private static async Task MarkIndexDirtyAfterFailureAsync(
            PostgresqlGraphRepository repo,
            Graph graph,
            string dirtyReason,
            Exception exception)
        {
            string reason = dirtyReason + ": " + exception.GetType().Name + ": " + exception.Message;
            repo.Logging.Log(SeverityEnum.Warn, reason);

            if (repo.GraphTransactionActive)
            {
                repo.NoteVectorIndexFailure(graph.TenantGUID, graph.GUID, reason);
                return;
            }

            await repo.Graph.MarkVectorIndexDirtyAsync(graph.TenantGUID, graph.GUID, reason).ConfigureAwait(false);
        }

        /// <summary>
        /// Build vector index entries for node vectors with node metadata resolved from the repository.
        /// </summary>
        /// <param name="repo">Repository.</param>
        /// <param name="graph">Graph.</param>
        /// <param name="vectors">Vectors.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector index entries.</returns>
        public static async Task<List<VectorIndexEntry>> BuildNodeIndexEntriesAsync(
            PostgresqlGraphRepository repo,
            Graph graph,
            IEnumerable<VectorMetadata> vectors,
            CancellationToken token = default)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            List<VectorMetadata> nodeVectors = vectors?
                .Where(vector => vector != null && vector.Vectors != null && vector.Vectors.Count > 0 && vector.NodeGUID.HasValue)
                .ToList() ?? new List<VectorMetadata>();

            if (nodeVectors.Count < 1) return new List<VectorIndexEntry>();

            Dictionary<Guid, Node> nodesByGuid = new Dictionary<Guid, Node>();
            List<Guid> nodeGuids = nodeVectors.Select(vector => vector.NodeGUID.Value).Distinct().ToList();

            await foreach (Node node in repo.Node.ReadByGuids(graph.TenantGUID, nodeGuids, token).ConfigureAwait(false))
            {
                nodesByGuid[node.GUID] = node;
            }

            List<VectorIndexEntry> entries = new List<VectorIndexEntry>();
            foreach (VectorMetadata vector in nodeVectors)
            {
                nodesByGuid.TryGetValue(vector.NodeGUID.Value, out Node node);
                entries.Add(VectorIndexEntry.FromVectorMetadata(vector, graph, node));
            }

            return entries;
        }

        private static async Task<VectorIndexEntry> BuildNodeIndexEntryAsync(
            PostgresqlGraphRepository repo,
            Graph graph,
            VectorMetadata vector,
            CancellationToken token = default)
        {
            Node node = null;
            if (vector?.NodeGUID.HasValue == true)
                node = await repo.Node.ReadByGuid(vector.TenantGUID, vector.NodeGUID.Value, token).ConfigureAwait(false);

            return VectorIndexEntry.FromVectorMetadata(vector, graph, node);
        }
    }
}

