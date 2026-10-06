namespace LiteGraph.Gexf
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
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

        private const string _GexfNamespace = "http://www.gexf.net/1.3";
        private const string _XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string _XsdNamespace = "http://www.w3.org/2001/XMLSchema";

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
                string xml = await SerializeGexf(doc, true, token).ConfigureAwait(false);
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
            string xml = await SerializeGexf(doc, true, token).ConfigureAwait(false);
            return xml;
        }

        #endregion

        #region Private-Methods

        private Task<string> SerializeGexf(GexfDocument doc, bool pretty = true, CancellationToken token = default)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            token.ThrowIfCancellationRequested();

            using (MemoryStream ms = new MemoryStream())
            {
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
                    WriteDocument(writer, doc);
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

        // Writes the same XML that XmlSerializer produced from the [Xml*] attributes on the Gexf classes (null attributes
        // and elements omitted, xsi and xsd namespaces declared on the root), without runtime code generation.
        private static void WriteDocument(XmlWriter writer, GexfDocument doc)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("gexf", _GexfNamespace);
            writer.WriteAttributeString("xmlns", "xsi", null, _XsiNamespace);
            writer.WriteAttributeString("xmlns", "xsd", null, _XsdNamespace);
            WriteAttribute(writer, "schemaLocation", _XsiNamespace, doc.SchemaLocation);
            WriteAttribute(writer, "version", null, doc.Version);

            if (doc.Meta != null)
            {
                writer.WriteStartElement("meta", _GexfNamespace);
                writer.WriteAttributeString("lastmodifieddate", XmlConvert.ToString(doc.Meta.LastModifiedDate, XmlDateTimeSerializationMode.RoundtripKind));
                WriteElement(writer, "creator", doc.Meta.Creator);
                WriteElement(writer, "description", doc.Meta.Description);
                writer.WriteEndElement();
            }

            if (doc.Graph != null)
            {
                writer.WriteStartElement("graph", _GexfNamespace);
                WriteAttribute(writer, "defaultedgetype", null, doc.Graph.DefaultEdgeType);

                if (doc.Graph.Attributes != null)
                {
                    writer.WriteStartElement("attributes", _GexfNamespace);
                    WriteAttribute(writer, "class", null, doc.Graph.Attributes.Class);
                    if (doc.Graph.Attributes.AttributeList != null)
                    {
                        foreach (GexfAttribute attribute in doc.Graph.Attributes.AttributeList)
                        {
                            if (attribute == null) continue;
                            writer.WriteStartElement("attribute", _GexfNamespace);
                            WriteAttribute(writer, "id", null, attribute.Id);
                            WriteAttribute(writer, "title", null, attribute.Title);
                            WriteAttribute(writer, "type", null, attribute.Type);
                            WriteElement(writer, "default", attribute.Default);
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                }

                if (doc.Graph.NodeList != null)
                {
                    writer.WriteStartElement("nodes", _GexfNamespace);
                    if (doc.Graph.NodeList.Nodes != null)
                    {
                        foreach (GexfNode node in doc.Graph.NodeList.Nodes)
                        {
                            if (node == null) continue;
                            writer.WriteStartElement("node", _GexfNamespace);
                            WriteAttribute(writer, "id", null, node.Id);
                            WriteAttribute(writer, "label", null, node.Label);
                            WriteAttributeValues(writer, node.ValueList);
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                }

                if (doc.Graph.EdgeList != null)
                {
                    writer.WriteStartElement("edges", _GexfNamespace);
                    if (doc.Graph.EdgeList.Edges != null)
                    {
                        foreach (GexfEdge edge in doc.Graph.EdgeList.Edges)
                        {
                            if (edge == null) continue;
                            writer.WriteStartElement("edge", _GexfNamespace);
                            WriteAttribute(writer, "id", null, edge.Id);
                            WriteAttribute(writer, "source", null, edge.Source);
                            WriteAttribute(writer, "target", null, edge.Target);
                            WriteAttributeValues(writer, edge.ValueList);
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static void WriteAttributeValues(XmlWriter writer, GexfAttributeValues values)
        {
            if (values == null) return;

            writer.WriteStartElement("attvalues", _GexfNamespace);
            if (values.Values != null)
            {
                foreach (GexfAttributeValue value in values.Values)
                {
                    if (value == null) continue;
                    writer.WriteStartElement("attvalue", _GexfNamespace);
                    WriteAttribute(writer, "for", null, value.For);
                    WriteAttribute(writer, "value", null, value.Value);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        private static void WriteAttribute(XmlWriter writer, string name, string ns, string value)
        {
            if (value == null) return;
            writer.WriteAttributeString(name, ns, value);
        }

        private static void WriteElement(XmlWriter writer, string name, string value)
        {
            if (value == null) return;
            writer.WriteElementString(name, _GexfNamespace, value);
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