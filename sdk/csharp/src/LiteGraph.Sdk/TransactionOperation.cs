namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// A single graph-scoped transaction operation.
    /// </summary>
    public class TransactionOperation
    {
        /// <summary>
        /// Operation type.
        /// </summary>
        public TransactionOperationTypeEnum OperationType { get; set; } = TransactionOperationTypeEnum.Create;

        /// <summary>
        /// Target object type.
        /// </summary>
        public TransactionObjectTypeEnum ObjectType { get; set; } = TransactionObjectTypeEnum.Node;

        /// <summary>
        /// Target object GUID for update, delete, detach, or upsert operations, or when payload omits a GUID.
        /// </summary>
        public Guid? GUID { get; set; } = null;

        /// <summary>
        /// Typed payload for create, update, attach, and upsert operations.
        /// </summary>
        public object Payload { get; set; } = null;
    }
}
