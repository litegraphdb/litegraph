namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Streams a graph out as a portable projection (node-link JSON, edge list, or GraphML) for computation in external engines such as rustworkx or NetworkX.
    /// Streaming keeps memory bounded so graphs larger than the in-memory algorithm ceiling can still be exported.
    /// </summary>
    public static class GraphProjectionExporter
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Export a graph to a stream in the requested format.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="format">Export format.</param>
        /// <param name="attributeLevel">Attribute detail level.</param>
        /// <param name="stream">Destination stream (left open).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when client or stream is null.</exception>
        public static async Task ExportAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportFormatEnum format,
            GraphExportAttributeLevelEnum attributeLevel,
            Stream stream,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            switch (format)
            {
                case GraphExportFormatEnum.NodeLinkJson:
                    await WriteNodeLinkJsonAsync(client, tenantGuid, graphGuid, attributeLevel, stream, token).ConfigureAwait(false);
                    return;
                case GraphExportFormatEnum.EdgeList:
                    await WriteEdgeListAsync(client, tenantGuid, graphGuid, stream, token).ConfigureAwait(false);
                    return;
                case GraphExportFormatEnum.Graphml:
                    await WriteGraphmlAsync(client, tenantGuid, graphGuid, attributeLevel, stream, token).ConfigureAwait(false);
                    return;
                default:
                    throw new NotSupportedException("Export format '" + format + "' is not supported.");
            }
        }

        #endregion

        #region Private-Methods

        private static bool IncludeData(GraphExportAttributeLevelEnum level)
        {
            return level == GraphExportAttributeLevelEnum.Full;
        }

        private static bool IncludeSubordinates(GraphExportAttributeLevelEnum level)
        {
            return level == GraphExportAttributeLevelEnum.Full || level == GraphExportAttributeLevelEnum.Meta;
        }

        private static async Task WriteNodeLinkJsonAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportAttributeLevelEnum level,
            Stream stream,
            CancellationToken token)
        {
            bool includeData = IncludeData(level);
            bool includeSub = IncludeSubordinates(level);

            Utf8JsonWriter writer = new Utf8JsonWriter(stream);
            await using (writer.ConfigureAwait(false))
            {
                writer.WriteStartObject();
                writer.WriteBoolean("directed", true);
                writer.WriteBoolean("multigraph", true);
                writer.WriteStartObject("graph");
                writer.WriteEndObject();

                writer.WriteStartArray("nodes");
                await foreach (Node node in client.Node.ReadAllInGraph(
                    tenantGuid, graphGuid, EnumerationOrderEnum.CreatedAscending, 0, includeData, includeSub, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (node == null) continue;

                    writer.WriteStartObject();
                    writer.WriteString("id", node.GUID.ToString());
                    if (level != GraphExportAttributeLevelEnum.None)
                    {
                        if (node.Name != null) writer.WriteString("name", node.Name);
                        if (includeSub) WriteLabelsAndTags(writer, node.Labels, node.Tags);
                        if (includeData && node.Data != null) WriteRawData(writer, "data", node.Data);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteStartArray("links");
                await foreach (Edge edge in client.Edge.ReadAllInGraph(
                    tenantGuid, graphGuid, EnumerationOrderEnum.CreatedAscending, 0, includeData, includeSub, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (edge == null) continue;

                    writer.WriteStartObject();
                    writer.WriteString("source", edge.From.ToString());
                    writer.WriteString("target", edge.To.ToString());
                    writer.WriteNumber("weight", 1.0d);
                    writer.WriteNumber("cost", edge.Cost);
                    writer.WriteString("id", edge.GUID.ToString());
                    if (level != GraphExportAttributeLevelEnum.None)
                    {
                        if (edge.Name != null) writer.WriteString("name", edge.Name);
                        if (includeSub) WriteLabelsAndTags(writer, edge.Labels, edge.Tags);
                        if (includeData && edge.Data != null) WriteRawData(writer, "data", edge.Data);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
                await writer.FlushAsync(token).ConfigureAwait(false);
            }
        }

        private static void WriteLabelsAndTags(Utf8JsonWriter writer, List<string> labels, NameValueCollection tags)
        {
            if (labels != null && labels.Count > 0)
            {
                writer.WriteStartArray("labels");
                foreach (string label in labels) writer.WriteStringValue(label);
                writer.WriteEndArray();
            }

            if (tags != null && tags.Count > 0)
            {
                writer.WriteStartObject("tags");
                foreach (string key in tags.AllKeys)
                {
                    if (key == null) continue;
                    writer.WriteString(key, tags[key]);
                }
                writer.WriteEndObject();
            }
        }

        private static void WriteRawData(Utf8JsonWriter writer, string propertyName, object data)
        {
            try
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data);
                writer.WritePropertyName(propertyName);
                writer.WriteRawValue(bytes);
            }
            catch (JsonException)
            {
                // Skip non-serializable data rather than fail the whole export.
            }
        }

        private static async Task WriteEdgeListAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            Stream stream,
            CancellationToken token)
        {
            StreamWriter sw = new StreamWriter(stream, new UTF8Encoding(false), 65536, true);
            await using (sw.ConfigureAwait(false))
            {
                await sw.WriteLineAsync("source,target,weight").ConfigureAwait(false);
                await foreach (Edge edge in client.Edge.ReadAllInGraph(
                    tenantGuid, graphGuid, EnumerationOrderEnum.CreatedAscending, 0, false, false, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (edge == null) continue;
                    await sw.WriteLineAsync(edge.From.ToString() + "," + edge.To.ToString() + ",1").ConfigureAwait(false);
                }
                await sw.FlushAsync().ConfigureAwait(false);
            }
        }

        private static async Task WriteGraphmlAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportAttributeLevelEnum level,
            Stream stream,
            CancellationToken token)
        {
            bool includeMeta = level != GraphExportAttributeLevelEnum.None;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Async = true;
            settings.Indent = false;
            settings.CloseOutput = false;
            settings.Encoding = new UTF8Encoding(false);

            XmlWriter writer = XmlWriter.Create(stream, settings);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteStartDocumentAsync().ConfigureAwait(false);
                writer.WriteStartElement("graphml", "http://graphml.graphdrawing.org/xmlns");

                if (includeMeta)
                {
                    writer.WriteStartElement("key");
                    writer.WriteAttributeString("id", "name");
                    writer.WriteAttributeString("for", "node");
                    writer.WriteAttributeString("attr.name", "name");
                    writer.WriteAttributeString("attr.type", "string");
                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }

                writer.WriteStartElement("graph");
                writer.WriteAttributeString("id", graphGuid.ToString());
                writer.WriteAttributeString("edgedefault", "directed");

                await foreach (Node node in client.Node.ReadAllInGraph(
                    tenantGuid, graphGuid, EnumerationOrderEnum.CreatedAscending, 0, false, false, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (node == null) continue;

                    writer.WriteStartElement("node");
                    writer.WriteAttributeString("id", node.GUID.ToString());
                    if (includeMeta && node.Name != null)
                    {
                        writer.WriteStartElement("data");
                        writer.WriteAttributeString("key", "name");
                        writer.WriteString(node.Name);
                        await writer.WriteEndElementAsync().ConfigureAwait(false);
                    }
                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }

                await foreach (Edge edge in client.Edge.ReadAllInGraph(
                    tenantGuid, graphGuid, EnumerationOrderEnum.CreatedAscending, 0, false, false, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (edge == null) continue;

                    writer.WriteStartElement("edge");
                    writer.WriteAttributeString("id", edge.GUID.ToString());
                    writer.WriteAttributeString("source", edge.From.ToString());
                    writer.WriteAttributeString("target", edge.To.ToString());
                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndDocumentAsync().ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        #endregion
    }
}
