namespace LiteGraph.Sdk
{
    /// <summary>
    /// Graph child object types that can participate in graph-scoped transactions.
    /// </summary>
    public enum TransactionObjectTypeEnum
    {
        /// <summary>
        /// Node.
        /// </summary>
        Node,

        /// <summary>
        /// Edge.
        /// </summary>
        Edge,

        /// <summary>
        /// Label.
        /// </summary>
        Label,

        /// <summary>
        /// Tag.
        /// </summary>
        Tag,

        /// <summary>
        /// Vector.
        /// </summary>
        Vector
    }
}
