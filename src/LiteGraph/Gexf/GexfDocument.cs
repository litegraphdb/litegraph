namespace LiteGraph.Gexf
{
    using System;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Graph Exchange XML Format document.
    /// </summary>
    [XmlRoot(ElementName = "gexf", Namespace = "http://www.gexf.net/1.3")]
    public class GexfDocument
    {
        /// <summary>
        /// Document metadata.
        /// </summary>
        [XmlElement(ElementName = "meta", Namespace = "http://www.gexf.net/1.3")]
        public GexfMetadata Meta { get; set; } = new GexfMetadata();

        /// <summary>
        /// Graph.
        /// </summary>
        [XmlElement(ElementName = "graph", Namespace = "http://www.gexf.net/1.3")]
        public GexfGraph Graph { get; set; } = new GexfGraph();

        /// <summary>
        /// XML namespace.
        /// </summary>
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; } = "http://www.gexf.net/1.3";

        /// <summary>
        /// Schema location.
        /// </summary>
        [XmlAttribute(AttributeName = "schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string SchemaLocation { get; set; } = "http://www.gexf.net/1.3 http://www.gexf.net/1.3/gexf.xsd";

        /// <summary>
        /// Version.
        /// </summary>
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; } = "1.3";

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public GexfDocument()
        {

        }
    }
}
