namespace LiteGraph.Gexf
{
    using System;
    using System.IO;
    using System.Text;
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
        public void ExportToFile(
            LiteGraphClient client, 
            Guid tenantGuid, 
            Guid graphGuid, 
            string filename, 
            bool includeData = false)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            GexfDocument doc = GraphToGexfDocument(client, tenantGuid, graphGuid, includeData);

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                string xml = SerializeXml<GexfDocument>(doc, true);
                byte[] bytes = Encoding.UTF8.GetBytes(xml);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Render a graph as a GEXF string.
        /// </summary>
        /// <param name="client">LiteGraphClient.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include node and edge data.</param>
        /// <returns>GEXF document.</returns>
        public string RenderAsGexf(
            LiteGraphClient client, 
            Guid tenantGuid, 
            Guid graphGuid, 
            bool includeData = false)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            GexfDocument doc = GraphToGexfDocument(client, tenantGuid, graphGuid, includeData);
            string xml = SerializeXml<GexfDocument>(doc, true);
            return xml;
        }

        #endregion

        #region Private-Methods

        private string SerializeXml<T>(object obj, bool pretty = true)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

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
                }
                else
                {
                    settings.Encoding = Encoding.UTF8;
                    settings.Indent = false;
                    settings.NewLineHandling = NewLineHandling.None;
                    settings.NewLineOnAttributes = false;
                    settings.ConformanceLevel = ConformanceLevel.Document;
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

                return xml;
            }
        }

        private GexfDocument GraphToGexfDocument(
            LiteGraphClient client, 
            Guid tenantGuid, 
            Guid graphGuid, 
            bool includeData = false)
        {
            LiteGraph.Graph graph = client.Graph.ReadByGuid(tenantGuid, graphGuid);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' was found.");

            GexfDocument doc = new GexfDocument();
            doc.Graph.DefaultEdgeType = "directed";
            doc.Graph.Attributes.AttributeList.Add(new GexfAttribute("0", "props"));

            foreach (LiteGraph.Node node in client.Node.ReadMany(tenantGuid, graphGuid))
            {
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

                if (node.Data != null && includeData)
                {
                    gNode.ValueList.Values.Add(new GexfAttributeValue("Data", _Serializer.SerializeJson(node.Data, false)));
                }

                doc.Graph.NodeList.Nodes.Add(gNode);
            }

            foreach (LiteGraph.Edge edge in client.Edge.ReadMany(tenantGuid, graphGuid))
            {
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

                if (edge.Data != null && includeData)
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