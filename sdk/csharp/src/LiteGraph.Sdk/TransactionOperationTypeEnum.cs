namespace LiteGraph.Sdk
{
    /// <summary>
    /// Graph transaction operation type.
    /// </summary>
    public enum TransactionOperationTypeEnum
    {
        /// <summary>
        /// Create an object.
        /// </summary>
        Create,

        /// <summary>
        /// Update an object.
        /// </summary>
        Update,

        /// <summary>
        /// Delete an object.
        /// </summary>
        Delete,

        /// <summary>
        /// Attach a subordinate object to a node or edge.
        /// </summary>
        Attach,

        /// <summary>
        /// Detach a subordinate object from a node or edge.
        /// </summary>
        Detach,

        /// <summary>
        /// Create an object when missing or update it when present.
        /// </summary>
        Upsert
    }
}
