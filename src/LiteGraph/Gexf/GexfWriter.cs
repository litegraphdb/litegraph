namespace LiteGraph.Gexf
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using LiteGraph;
    using LiteGraph.Serialization;

    /// <summary>
    /// GEXF file writer.
    /// </summary>
    public class GexfWriter
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
        /// <param name="serializer">Serializer.</param>
        public GexfWriter(Serializer serializer = null)
        {
            if (serializer != null) _Serializer = serializer;
            else _Serializer = new Serializer();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Write a GEXF file.
        /// </summary>
        /// <param name="client">LiteGraphClient.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="filename">Output filename.</param>
        /// <param name="includeData">True to include node and edge data.</param>
        /// <param name="includeSubordinates">True to include subordinates (labels, tags, vectors).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ExportToFile(
            LiteGraphClient client, 
            Guid tenantGuid, 
            Guid graphGuid, 
            string filename, 
            bool includeData,
            bool includeSubordinates,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            token.ThrowIfCancellationRequested();

            GexfDocument doc = await GraphToGexfDocument(client, tenantGuid, graphGuid, includeData, includeSubordinates, token).ConfigureAwait(false);

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                string xml = await SerializeXml<GexfDocument>(doc, true, token).ConfigureAwait(false);
                byte[] bytes = Encoding.UTF8.GetBytes(xml);
                await fs.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Render a graph as a GEXF string.
        /// </summary>
        /// <param name="client">LiteGraphClient.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include node and edge data.</param>
        /// <param name="includeSubordinates">True to include subordinates (labels, tags, vectors).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GEXF document.</returns>
        public async Task<string> RenderAsGexf(
            LiteGraphClient client, 
            Guid tenantGuid, 
            Guid graphGuid, 
            bool includeData,
            bool includeSubordinates,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            token.ThrowIfCancellationRequested();
            GexfDocument doc = await GraphToGexfDocument(client, tenantGuid, graphGuid, includeData, includeSubordinates, token).ConfigureAwait(false);
            string xml = await SerializeXml<GexfDocument>(doc, true, token).ConfigureAwait(false);
            return xml;
        }

        #endregion

        #region Private-Methods

        private Task<string> SerializeXml<T>(object obj, bool pretty = true, CancellationToken token = default)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            token.ThrowIfCancellationRequested();

            XmlSerializer xmls = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                XmlWriterSettings settings = new XmlWriterSettings();

                if (pretty)
                {
                    settings.Encoding = Encoding.UTF8;
                    settings.Indent = true;
                    settings.NewLineChars = "\n";
                    settings.NewLineHandling = NewLineHandling.None;
                    settings.NewLineOnAttributes = false;
                    settings.ConformanceLevel = ConformanceLevel.Document;
                    settings.Async = true;
                }
                else
                {
                    settings.Encoding = Encoding.UTF8;
                    settings.Indent = false;
                    settings.NewLineHandling = NewLineHandling.None;
                    settings.NewLineOnAttributes = false;
                    settings.ConformanceLevel = ConformanceLevel.Document;
                    settings.Async = true;
                }

                using (XmlWriter writer = XmlWriter.Create(ms, settings))
                {
                    xmls.Serialize(writer, obj, ns);
                }

                string xml = Encoding.UTF8.GetString(ms.ToArray());

                string byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                while (xml.StartsWith(byteOrderMarkUtf8, StringComparison.Ordinal))
                {
                    xml = xml.Remove(0, byteOrderMarkUtf8.Length);
                }

                return Task.FromResult(xml);
            }
        }

        private async Task<GexfDocument> GraphToGexfDocument(
            LiteGraphClient client, 
            Guid tenantGuid, 
            Guid graphGuid, 
            bool includeData,
            bool includeSubordinates,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Graph graph = await client.Graph.ReadByGuid(tenantGuid, graphGuid, token: token).ConfigureAwait(false);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' was found.");

            GexfDocument doc = new GexfDocument();
            doc.Graph.DefaultEdgeType = "directed";
            doc.Graph.Attributes.AttributeList.Add(new GexfAttribute("0", "props"));

            await foreach (Node node in client.Node.ReadMany(
                tenantGuid, 
                graphGuid, 
                null, 
                null, 
                null, 
                null, 
                EnumerationOrderEnum.CreatedDescending, 
                0, 
                includeData, 
                includeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                GexfNode gNode = new GexfNode(node.GUID, node.Name);

                if (!String.IsNullOrEmpty(node.Name))
                    gNode.ValueList.Values.Add(new GexfAttributeValue("Name", node.Name));

                if (node.Labels != null)
                {
                    foreach (string label in node.Labels)
                    {
                        gNode.ValueList.Values.Add(new GexfAttributeValue(label, null));
                    }
                }

                if (node.Tags != null && node.Tags.Count > 0)
                {
                    foreach (string key in node.Tags)
                    {
                        gNode.ValueList.Values.Add(new GexfAttributeValue(key, node.Tags.Get(key)));
                    }
                }

                if (node.Data != null)
                {
                    gNode.ValueList.Values.Add(new GexfAttributeValue("Data", _Serializer.SerializeJson(node.Data, false)));
                }

                doc.Graph.NodeList.Nodes.Add(gNode);
            }

            await foreach (Edge edge in client.Edge.ReadMany(
                tenantGuid, 
                graphGuid,
                null,
                null,
                null,
                null,
                EnumerationOrderEnum.CreatedDescending,
                0,
                includeData,
                includeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                GexfEdge gEdge = new GexfEdge(edge.GUID, edge.From, edge.To);

                if (!String.IsNullOrEmpty(edge.Name))
                    gEdge.ValueList.Values.Add(new GexfAttributeValue("Name", edge.Name));

                gEdge.ValueList.Values.Add(new GexfAttributeValue("Cost", edge.Cost.ToString()));

                if (edge.Labels != null)
                {
                    foreach (string label in edge.Labels)
                    {
                        gEdge.ValueList.Values.Add(new GexfAttributeValue(label, null));
                    }
                }

                if (edge.Tags != null && edge.Tags.Count > 0)
                {
                    foreach (string key in edge.Tags)
                    {
                        gEdge.ValueList.Values.Add(new GexfAttributeValue(key, edge.Tags.Get(key)));
                    }
                }

                if (edge.Data != null)
                {
                    gEdge.ValueList.Values.Add(new GexfAttributeValue("Data", _Serializer.SerializeJson(edge.Data, false)));
                }

                doc.Graph.EdgeList.Edges.Add(gEdge);
            }

            return doc;
        }

        #endregion
    }
}