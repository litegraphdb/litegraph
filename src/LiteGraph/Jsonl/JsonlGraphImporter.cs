namespace LiteGraph.Jsonl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;

    /// <summary>
    /// Imports LiteGraph JSONL into a new or existing graph.
    /// Nodes are streamed and written in batches while edges are buffered until all nodes are known, so that
    /// endpoint validity and GUID remapping can be enforced.  On failure, records created during the import are
    /// removed on a best-effort basis (compensating rollback); records updated under the Overwrite strategy are
    /// not restored.
    /// </summary>
    public class JsonlGraphImporter
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private JsonlGraphReader _Reader = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="reader">Reader.  When null, a default reader is used.</param>
        public JsonlGraphImporter(JsonlGraphReader reader = null)
        {
            if (reader != null) _Reader = reader;
            else _Reader = new JsonlGraphReader();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Import JSONL from a stream into a new or existing graph.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Target tenant GUID.  All imported records are stamped with this tenant.</param>
        /// <param name="jsonl">Source JSONL stream.</param>
        /// <param name="request">Import request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Import result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when client, stream, or request is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the request is inconsistent with its mode, or the target graph does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown on a GUID collision under the Preserve strategy.</exception>
        /// <exception cref="JsonlFormatException">Thrown on a malformed line when the error policy is Abort.</exception>
        public async Task<GraphImportResult> Import(
            LiteGraphClient client,
            Guid tenantGuid,
            Stream jsonl,
            GraphImportRequest request,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (jsonl == null) throw new ArgumentNullException(nameof(jsonl));
            if (request == null) throw new ArgumentNullException(nameof(request));

            Serializer serializer = client.Serializer;

            GraphImportResult result = new GraphImportResult();
            result.TenantGUID = tenantGuid;

            bool regenerate = request.GuidStrategy == GraphImportGuidStrategyEnum.Regenerate;

            Guid targetGraphGuid = Guid.Empty;
            bool graphResolved = false;
            bool graphCreated = false;
            Graph fileGraph = null;

            // Validate the target for merge up front.
            if (request.Mode == GraphImportModeEnum.MergeIntoExisting)
            {
                if (request.TargetGraphGUID == null)
                    throw new ArgumentException("A target graph GUID is required when merging into an existing graph.");
                Graph existingTarget = await client.Graph.ReadByGuid(tenantGuid, request.TargetGraphGUID.Value, token: token).ConfigureAwait(false);
                if (existingTarget == null)
                    throw new ArgumentException("Target graph with GUID '" + request.TargetGraphGUID.Value + "' was not found.");
                targetGraphGuid = request.TargetGraphGUID.Value;
                graphResolved = true;
            }

            Dictionary<Guid, Guid> guidMap = result.GuidMap;
            HashSet<Guid> importedNodeGuids = new HashSet<Guid>();
            List<Node> nodeBatch = new List<Node>();
            List<Edge> edgeBuffer = new List<Edge>();
            List<Guid> createdNodeGuids = new List<Guid>();
            List<Guid> createdEdgeGuids = new List<Guid>();

            try
            {
                using (StreamReader lineReader = new StreamReader(jsonl, Encoding.UTF8, true, 65536, true))
                {
                    long lineNumber = 0;
                    string line;
                    while ((line = await lineReader.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        token.ThrowIfCancellationRequested();
                        lineNumber++;

                        JsonlRecord record;
                        try
                        {
                            record = _Reader.ParseLine(line, lineNumber);
                        }
                        catch (JsonlFormatException jfe)
                        {
                            if (request.OnError == GraphImportErrorPolicyEnum.Skip)
                            {
                                result.LinesRead++;
                                result.Warnings.Add("Skipped malformed line " + jfe.LineNumber + ": " + jfe.Message);
                                continue;
                            }
                            throw;
                        }

                        if (record == null)
                        {
                            result.LinesIgnored++;
                            continue;
                        }

                        result.LinesRead++;

                        if (record.Type == JsonlRecordTypeEnum.Graph)
                        {
                            if (fileGraph == null) fileGraph = record.AsGraph(serializer);
                            else result.Warnings.Add("Ignored additional graph record on line " + lineNumber + "; a file describes a single source graph.");
                            continue;
                        }

                        if (record.Type == JsonlRecordTypeEnum.Node)
                        {
                            if (!graphResolved)
                            {
                                targetGraphGuid = await ResolveNewGraph(client, tenantGuid, request, fileGraph, regenerate, guidMap, token).ConfigureAwait(false);
                                graphResolved = true;
                                graphCreated = true;
                                result.GraphsCreated++;
                            }

                            Node node = record.AsNode(serializer);
                            RemapNode(node, tenantGuid, targetGraphGuid, regenerate, guidMap, request.IncludeVectors);
                            importedNodeGuids.Add(node.GUID);
                            nodeBatch.Add(node);

                            if (nodeBatch.Count >= request.BatchSize)
                                await FlushNodeBatch(client, tenantGuid, targetGraphGuid, request, nodeBatch, createdNodeGuids, result, token).ConfigureAwait(false);

                            continue;
                        }

                        if (record.Type == JsonlRecordTypeEnum.Edge)
                        {
                            edgeBuffer.Add(record.AsEdge(serializer));
                            continue;
                        }
                    }
                }

                // For a create-new import with no node records at all, still create the (empty) graph.
                if (!graphResolved && request.Mode == GraphImportModeEnum.CreateNew)
                {
                    targetGraphGuid = await ResolveNewGraph(client, tenantGuid, request, fileGraph, regenerate, guidMap, token).ConfigureAwait(false);
                    graphResolved = true;
                    graphCreated = true;
                    result.GraphsCreated++;
                }

                await FlushNodeBatch(client, tenantGuid, targetGraphGuid, request, nodeBatch, createdNodeGuids, result, token).ConfigureAwait(false);

                await ProcessEdges(client, tenantGuid, targetGraphGuid, request, edgeBuffer, importedNodeGuids, guidMap, regenerate, createdEdgeGuids, result, token).ConfigureAwait(false);

                result.GraphGUID = targetGraphGuid;
                result.Success = true;
                return result;
            }
            catch (Exception)
            {
                await Rollback(client, tenantGuid, targetGraphGuid, createdEdgeGuids, createdNodeGuids, graphCreated, token).ConfigureAwait(false);
                throw;
            }
        }

        #endregion

        #region Private-Methods

        private async Task<Guid> ResolveNewGraph(
            LiteGraphClient client,
            Guid tenantGuid,
            GraphImportRequest request,
            Graph fileGraph,
            bool regenerate,
            Dictionary<Guid, Guid> guidMap,
            CancellationToken token)
        {
            Graph graph = request.NewGraph ?? fileGraph ?? new Graph();
            Guid originalGuid = graph.GUID;

            graph.TenantGUID = tenantGuid;
            if (regenerate)
            {
                graph.GUID = Guid.NewGuid();
                if (originalGuid != Guid.Empty && originalGuid != graph.GUID) guidMap[originalGuid] = graph.GUID;
            }

            // Reset vector-index runtime state so a fresh graph starts clean.
            graph.VectorIndexFile = null;
            graph.VectorIndexDirty = false;
            graph.VectorIndexDirtyUtc = null;
            graph.VectorIndexDirtyReason = null;

            Graph created = await client.Graph.Create(graph, token).ConfigureAwait(false);
            return created.GUID;
        }

        private void RemapNode(Node node, Guid tenantGuid, Guid graphGuid, bool regenerate, Dictionary<Guid, Guid> guidMap, bool includeVectors)
        {
            Guid originalGuid = node.GUID;
            if (regenerate)
            {
                node.GUID = Guid.NewGuid();
                guidMap[originalGuid] = node.GUID;
            }

            node.TenantGUID = tenantGuid;
            node.GraphGUID = graphGuid;

            if (!includeVectors)
            {
                node.Vectors = null;
            }
            else if (node.Vectors != null)
            {
                foreach (VectorMetadata vector in node.Vectors)
                {
                    if (regenerate) vector.GUID = Guid.NewGuid();
                    vector.TenantGUID = tenantGuid;
                    vector.GraphGUID = graphGuid;
                    vector.NodeGUID = node.GUID;
                    vector.EdgeGUID = null;
                }
            }
        }

        private void RemapEdge(Edge edge, Guid tenantGuid, Guid graphGuid, bool regenerate, Dictionary<Guid, Guid> guidMap, bool includeVectors)
        {
            if (regenerate)
            {
                Guid originalGuid = edge.GUID;
                edge.GUID = Guid.NewGuid();
                guidMap[originalGuid] = edge.GUID;
            }

            edge.TenantGUID = tenantGuid;
            edge.GraphGUID = graphGuid;

            Guid mappedFrom;
            if (guidMap.TryGetValue(edge.From, out mappedFrom)) edge.From = mappedFrom;
            Guid mappedTo;
            if (guidMap.TryGetValue(edge.To, out mappedTo)) edge.To = mappedTo;

            edge.FromNode = null;
            edge.ToNode = null;

            if (!includeVectors)
            {
                edge.Vectors = null;
            }
            else if (edge.Vectors != null)
            {
                foreach (VectorMetadata vector in edge.Vectors)
                {
                    if (regenerate) vector.GUID = Guid.NewGuid();
                    vector.TenantGUID = tenantGuid;
                    vector.GraphGUID = graphGuid;
                    vector.EdgeGUID = edge.GUID;
                    vector.NodeGUID = null;
                }
            }
        }

        private async Task FlushNodeBatch(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphImportRequest request,
            List<Node> batch,
            List<Guid> createdNodeGuids,
            GraphImportResult result,
            CancellationToken token)
        {
            if (batch.Count == 0) return;

            List<Node> toCreate = batch;
            List<Node> toUpdate = new List<Node>();

            if (request.GuidStrategy != GraphImportGuidStrategyEnum.Regenerate
                && request.Mode == GraphImportModeEnum.MergeIntoExisting)
            {
                ExistenceRequest existReq = new ExistenceRequest { Nodes = batch.Select(n => n.GUID).ToList() };
                ExistenceResult existRes = await client.Batch.Existence(tenantGuid, graphGuid, existReq, token).ConfigureAwait(false);
                HashSet<Guid> existing = new HashSet<Guid>(existRes.ExistingNodes ?? new List<Guid>());

                if (existing.Count > 0)
                {
                    if (request.GuidStrategy == GraphImportGuidStrategyEnum.Preserve)
                        throw new InvalidOperationException("Node GUID collision under the Preserve strategy: " + String.Join(", ", existing.Take(5)) + (existing.Count > 5 ? ", ..." : ""));

                    if (request.GuidStrategy == GraphImportGuidStrategyEnum.Skip)
                    {
                        toCreate = batch.Where(n => !existing.Contains(n.GUID)).ToList();
                        result.NodesSkipped += existing.Count;
                    }
                    else // Overwrite
                    {
                        toCreate = batch.Where(n => !existing.Contains(n.GUID)).ToList();
                        toUpdate = batch.Where(n => existing.Contains(n.GUID)).ToList();
                    }
                }
            }

            if (toCreate.Count > 0)
            {
                await client.Node.CreateMany(tenantGuid, graphGuid, toCreate, request.ReturnMode, token).ConfigureAwait(false);
                foreach (Node n in toCreate) createdNodeGuids.Add(n.GUID);
                result.NodesCreated += toCreate.Count;
            }

            foreach (Node n in toUpdate)
            {
                await client.Node.Update(n, token).ConfigureAwait(false);
                result.NodesUpdated++;
            }

            batch.Clear();
        }

        private async Task ProcessEdges(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphImportRequest request,
            List<Edge> edges,
            HashSet<Guid> importedNodeGuids,
            Dictionary<Guid, Guid> guidMap,
            bool regenerate,
            List<Guid> createdEdgeGuids,
            GraphImportResult result,
            CancellationToken token)
        {
            if (edges.Count == 0) return;

            foreach (Edge edge in edges) RemapEdge(edge, tenantGuid, graphGuid, regenerate, guidMap, request.IncludeVectors);

            // Determine which endpoints are external (not part of the imported node set) so we can verify them once.
            HashSet<Guid> externalEndpoints = new HashSet<Guid>();
            foreach (Edge edge in edges)
            {
                if (!importedNodeGuids.Contains(edge.From)) externalEndpoints.Add(edge.From);
                if (!importedNodeGuids.Contains(edge.To)) externalEndpoints.Add(edge.To);
            }

            HashSet<Guid> validExternal = new HashSet<Guid>();
            if (externalEndpoints.Count > 0 && request.Mode == GraphImportModeEnum.MergeIntoExisting)
            {
                ExistenceRequest existReq = new ExistenceRequest { Nodes = externalEndpoints.ToList() };
                ExistenceResult existRes = await client.Batch.Existence(tenantGuid, graphGuid, existReq, token).ConfigureAwait(false);
                if (existRes.ExistingNodes != null) foreach (Guid g in existRes.ExistingNodes) validExternal.Add(g);
            }

            List<Edge> valid = new List<Edge>();
            foreach (Edge edge in edges)
            {
                bool fromOk = importedNodeGuids.Contains(edge.From) || validExternal.Contains(edge.From);
                bool toOk = importedNodeGuids.Contains(edge.To) || validExternal.Contains(edge.To);
                if (fromOk && toOk) valid.Add(edge);
                else result.Warnings.Add("Dropped edge '" + edge.GUID + "' with unresolved endpoint(s) From=" + edge.From + " To=" + edge.To + ".");
            }

            for (int i = 0; i < valid.Count; i += request.BatchSize)
            {
                token.ThrowIfCancellationRequested();
                List<Edge> batch = valid.Skip(i).Take(request.BatchSize).ToList();
                await FlushEdgeBatch(client, tenantGuid, graphGuid, request, batch, createdEdgeGuids, result, token).ConfigureAwait(false);
            }
        }

        private async Task FlushEdgeBatch(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphImportRequest request,
            List<Edge> batch,
            List<Guid> createdEdgeGuids,
            GraphImportResult result,
            CancellationToken token)
        {
            if (batch.Count == 0) return;

            List<Edge> toCreate = batch;
            List<Edge> toUpdate = new List<Edge>();

            if (request.GuidStrategy != GraphImportGuidStrategyEnum.Regenerate
                && request.Mode == GraphImportModeEnum.MergeIntoExisting)
            {
                ExistenceRequest existReq = new ExistenceRequest { Edges = batch.Select(e => e.GUID).ToList() };
                ExistenceResult existRes = await client.Batch.Existence(tenantGuid, graphGuid, existReq, token).ConfigureAwait(false);
                HashSet<Guid> existing = new HashSet<Guid>(existRes.ExistingEdges ?? new List<Guid>());

                if (existing.Count > 0)
                {
                    if (request.GuidStrategy == GraphImportGuidStrategyEnum.Preserve)
                        throw new InvalidOperationException("Edge GUID collision under the Preserve strategy: " + String.Join(", ", existing.Take(5)) + (existing.Count > 5 ? ", ..." : ""));

                    if (request.GuidStrategy == GraphImportGuidStrategyEnum.Skip)
                    {
                        toCreate = batch.Where(e => !existing.Contains(e.GUID)).ToList();
                        result.EdgesSkipped += existing.Count;
                    }
                    else // Overwrite
                    {
                        toCreate = batch.Where(e => !existing.Contains(e.GUID)).ToList();
                        toUpdate = batch.Where(e => existing.Contains(e.GUID)).ToList();
                    }
                }
            }

            if (toCreate.Count > 0)
            {
                await client.Edge.CreateMany(tenantGuid, graphGuid, toCreate, request.ReturnMode, token).ConfigureAwait(false);
                foreach (Edge e in toCreate) createdEdgeGuids.Add(e.GUID);
                result.EdgesCreated += toCreate.Count;
            }

            foreach (Edge e in toUpdate)
            {
                await client.Edge.Update(e, token).ConfigureAwait(false);
                result.EdgesUpdated++;
            }
        }

        private async Task Rollback(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            List<Guid> createdEdgeGuids,
            List<Guid> createdNodeGuids,
            bool graphCreated,
            CancellationToken token)
        {
            if (graphCreated && graphGuid != Guid.Empty)
            {
                try { await client.Graph.DeleteByGuid(tenantGuid, graphGuid, true, token).ConfigureAwait(false); }
                catch { }
                return;
            }

            if (createdEdgeGuids.Count > 0)
            {
                try { await client.Edge.DeleteMany(tenantGuid, graphGuid, createdEdgeGuids, token).ConfigureAwait(false); }
                catch { }
            }

            if (createdNodeGuids.Count > 0)
            {
                try { await client.Node.DeleteMany(tenantGuid, graphGuid, createdNodeGuids, token).ConfigureAwait(false); }
                catch { }
            }
        }

        #endregion
    }
}
