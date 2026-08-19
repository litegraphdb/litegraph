namespace LiteGraph.Jsonl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;

    /// <summary>
    /// Writes graphs, nodes, and edges to the LiteGraph JSONL interchange format.
    /// The writer streams line by line and never materializes the whole document, so it is safe for
    /// graphs too large to hold in memory.
    /// </summary>
    public class JsonlGraphWriter
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Serializer _Serializer = new Serializer();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="serializer">Serializer.  When null, a default serializer is used.</param>
        public JsonlGraphWriter(Serializer serializer = null)
        {
            if (serializer != null) _Serializer = serializer;
            else _Serializer = new Serializer();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Write a stream of JSONL records to a stream.
        /// </summary>
        /// <param name="records">Records to write.</param>
        /// <param name="metadata">Export metadata rendered as leading comment lines.  May be null.</param>
        /// <param name="stream">Destination stream.  The stream is left open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when records or stream is null.</exception>
        public async Task WriteRecords(
            IAsyncEnumerable<JsonlRecord> records,
            JsonlExportMetadata metadata,
            Stream stream,
            CancellationToken token = default)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 65536, true))
            {
                await WriteHeader(writer, metadata, token).ConfigureAwait(false);

                await foreach (JsonlRecord record in records.WithCancellation(token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (record == null) continue;
                    string json = _Serializer.SerializeJson(record, false);
                    await writer.WriteAsync(json).ConfigureAwait(false);
                    await writer.WriteAsync('\n').ConfigureAwait(false);
                }

                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Write a materialized search result to a stream.
        /// </summary>
        /// <param name="result">Search result.</param>
        /// <param name="metadata">Export metadata.  May be null.</param>
        /// <param name="stream">Destination stream.  The stream is left open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when result or stream is null.</exception>
        public async Task WriteSearchResult(
            SearchResult result,
            JsonlExportMetadata metadata,
            Stream stream,
            CancellationToken token = default)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            await WriteRecords(SearchResultToRecords(result, token), metadata, stream, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Stream an entire graph to a stream as JSONL: the graph record, then all node records, then all edge records.
        /// This is the provider-agnostic whole-graph backup path and runs in constant memory.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="metadata">Export metadata.  May be null; when null a default is generated.</param>
        /// <param name="includeData">True to include the data property of objects.</param>
        /// <param name="includeSubordinates">True to include labels, tags, and vectors.</param>
        /// <param name="stream">Destination stream.  The stream is left open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when client or stream is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the graph does not exist.</exception>
        public async Task WriteGraph(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            JsonlExportMetadata metadata,
            bool includeData,
            bool includeSubordinates,
            Stream stream,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            Graph graph = await client.Graph.ReadByGuid(tenantGuid, graphGuid, token: token).ConfigureAwait(false);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' was found.");

            if (metadata == null)
            {
                metadata = new JsonlExportMetadata
                {
                    Kind = "graph-backup",
                    SourceTenantGUID = tenantGuid,
                    SourceGraphGUID = graphGuid,
                    SourceGraphName = graph.Name
                };
            }

            await WriteRecords(GraphToRecords(client, tenantGuid, graphGuid, graph, includeData, includeSubordinates, token), metadata, stream, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Write a materialized search result to a file.
        /// </summary>
        /// <param name="result">Search result.</param>
        /// <param name="metadata">Export metadata.  May be null.</param>
        /// <param name="filename">Destination filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when result or filename is null.</exception>
        public async Task WriteSearchResultToFile(
            SearchResult result,
            JsonlExportMetadata metadata,
            string filename,
            CancellationToken token = default)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                await WriteSearchResult(result, metadata, fs, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stream an entire graph to a file as JSONL.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="metadata">Export metadata.  May be null.</param>
        /// <param name="includeData">True to include the data property of objects.</param>
        /// <param name="includeSubordinates">True to include labels, tags, and vectors.</param>
        /// <param name="filename">Destination filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when client or filename is null.</exception>
        public async Task WriteGraphToFile(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            JsonlExportMetadata metadata,
            bool includeData,
            bool includeSubordinates,
            string filename,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                await WriteGraph(client, tenantGuid, graphGuid, metadata, includeData, includeSubordinates, fs, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Render a materialized search result to a JSONL string.  Not for large graphs; use the streaming methods instead.
        /// </summary>
        /// <param name="result">Search result.</param>
        /// <param name="metadata">Export metadata.  May be null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>JSONL string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when result is null.</exception>
        public async Task<string> Render(
            SearchResult result,
            JsonlExportMetadata metadata,
            CancellationToken token = default)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            using (MemoryStream ms = new MemoryStream())
            {
                await WriteSearchResult(result, metadata, ms, token).ConfigureAwait(false);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Render an entire graph to a JSONL string.  Not for large graphs; use the streaming methods instead.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="metadata">Export metadata.  May be null.</param>
        /// <param name="includeData">True to include the data property of objects.</param>
        /// <param name="includeSubordinates">True to include labels, tags, and vectors.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>JSONL string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
        public async Task<string> RenderGraph(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            JsonlExportMetadata metadata,
            bool includeData,
            bool includeSubordinates,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            using (MemoryStream ms = new MemoryStream())
            {
                await WriteGraph(client, tenantGuid, graphGuid, metadata, includeData, includeSubordinates, ms, token).ConfigureAwait(false);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        #endregion

        #region Private-Methods

        private async Task WriteHeader(StreamWriter writer, JsonlExportMetadata metadata, CancellationToken token)
        {
            if (metadata == null) return;
            token.ThrowIfCancellationRequested();

            await writer.WriteAsync("# litegraph-jsonl " + (metadata.FormatVersion ?? "v1") + "\n").ConfigureAwait(false);
            if (!String.IsNullOrEmpty(metadata.Kind)) await writer.WriteAsync("# kind: " + metadata.Kind + "\n").ConfigureAwait(false);
            await writer.WriteAsync("# exported-utc: " + metadata.ExportedUtc.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") + "\n").ConfigureAwait(false);
            await writer.WriteAsync("# source-tenant: " + metadata.SourceTenantGUID + "\n").ConfigureAwait(false);

            string graphLine = "# source-graph: " + metadata.SourceGraphGUID;
            if (!String.IsNullOrEmpty(metadata.SourceGraphName)) graphLine += " (name: \"" + metadata.SourceGraphName.Replace("\n", " ").Replace("\r", " ") + "\")";
            await writer.WriteAsync(graphLine + "\n").ConfigureAwait(false);

            if (!String.IsNullOrEmpty(metadata.SelectionSummary))
                await writer.WriteAsync("# selection: " + metadata.SelectionSummary.Replace("\n", " ").Replace("\r", " ") + "\n").ConfigureAwait(false);

            if (!String.IsNullOrEmpty(metadata.Generator))
                await writer.WriteAsync("# generator: " + metadata.Generator + "\n").ConfigureAwait(false);
        }

        private async IAsyncEnumerable<JsonlRecord> SearchResultToRecords(
            SearchResult result,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            if (result.Graphs != null)
            {
                foreach (Graph graph in result.Graphs)
                {
                    token.ThrowIfCancellationRequested();
                    yield return JsonlRecord.ForGraph(graph);
                }
            }

            if (result.Nodes != null)
            {
                foreach (Node node in result.Nodes)
                {
                    token.ThrowIfCancellationRequested();
                    yield return JsonlRecord.ForNode(node);
                }
            }

            if (result.Edges != null)
            {
                foreach (Edge edge in result.Edges)
                {
                    token.ThrowIfCancellationRequested();
                    yield return JsonlRecord.ForEdge(edge);
                }
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async IAsyncEnumerable<JsonlRecord> GraphToRecords(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            Graph graph,
            bool includeData,
            bool includeSubordinates,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            yield return JsonlRecord.ForGraph(graph);

            await foreach (Node node in client.Node.ReadMany(
                tenantGuid, graphGuid, null, null, null, null,
                EnumerationOrderEnum.CreatedDescending, 0, includeData, includeSubordinates, token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return JsonlRecord.ForNode(node);
            }

            await foreach (Edge edge in client.Edge.ReadMany(
                tenantGuid, graphGuid, null, null, null, null,
                EnumerationOrderEnum.CreatedDescending, 0, includeData, includeSubordinates, token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return JsonlRecord.ForEdge(edge);
            }
        }

        #endregion
    }
}
