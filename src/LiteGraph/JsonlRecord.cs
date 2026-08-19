namespace LiteGraph
{
    using LiteGraph.Serialization;

    /// <summary>
    /// A single JSONL record line: a type discriminator paired with a graph, node, or edge payload.
    /// </summary>
    public class JsonlRecord
    {
        #region Public-Members

        /// <summary>
        /// Record type discriminator.
        /// </summary>
        public JsonlRecordTypeEnum Type { get; set; } = JsonlRecordTypeEnum.Node;

        /// <summary>
        /// Record payload.  On write this is a Graph, Node, or Edge instance; on read it is the deserialized JSON payload.
        /// Use AsGraph, AsNode, or AsEdge to obtain a strongly-typed instance.
        /// </summary>
        public object Object { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public JsonlRecord()
        {

        }

        /// <summary>
        /// Create a record wrapping a graph.
        /// </summary>
        /// <param name="graph">Graph.</param>
        /// <returns>Record.</returns>
        public static JsonlRecord ForGraph(Graph graph)
        {
            return new JsonlRecord { Type = JsonlRecordTypeEnum.Graph, Object = graph };
        }

        /// <summary>
        /// Create a record wrapping a node.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <returns>Record.</returns>
        public static JsonlRecord ForNode(Node node)
        {
            return new JsonlRecord { Type = JsonlRecordTypeEnum.Node, Object = node };
        }

        /// <summary>
        /// Create a record wrapping an edge.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <returns>Record.</returns>
        public static JsonlRecord ForEdge(Edge edge)
        {
            return new JsonlRecord { Type = JsonlRecordTypeEnum.Edge, Object = edge };
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Materialize the payload as a graph.
        /// </summary>
        /// <param name="serializer">Serializer used to convert the payload.</param>
        /// <returns>Graph, or null if the payload is null.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the serializer is null.</exception>
        public Graph AsGraph(Serializer serializer)
        {
            return Convert<Graph>(serializer);
        }

        /// <summary>
        /// Materialize the payload as a node.
        /// </summary>
        /// <param name="serializer">Serializer used to convert the payload.</param>
        /// <returns>Node, or null if the payload is null.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the serializer is null.</exception>
        public Node AsNode(Serializer serializer)
        {
            return Convert<Node>(serializer);
        }

        /// <summary>
        /// Materialize the payload as an edge.
        /// </summary>
        /// <param name="serializer">Serializer used to convert the payload.</param>
        /// <returns>Edge, or null if the payload is null.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the serializer is null.</exception>
        public Edge AsEdge(Serializer serializer)
        {
            return Convert<Edge>(serializer);
        }

        #endregion

        #region Private-Methods

        private T Convert<T>(Serializer serializer)
        {
            if (serializer == null) throw new System.ArgumentNullException(nameof(serializer));
            if (Object == null) return default(T);
            if (Object is T typed) return typed;
            return serializer.DeserializeJson<T>(serializer.SerializeJson(Object));
        }

        #endregion
    }
}
