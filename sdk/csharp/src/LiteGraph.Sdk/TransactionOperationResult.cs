namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Result for a single graph transaction operation.
    /// </summary>
    public class TransactionOperationResult
    {
        /// <summary>
        /// Operation index in the original request.
        /// </summary>
        public int Index { get; set; } = 0;

        /// <summary>
        /// Operation type.
        /// </summary>
        public TransactionOperationTypeEnum OperationType { get; set; } = TransactionOperationTypeEnum.Create;

        /// <summary>
        /// Object type.
        /// </summary>
        public TransactionObjectTypeEnum ObjectType { get; set; } = TransactionObjectTypeEnum.Node;

        /// <summary>
        /// Object GUID, where available.
        /// </summary>
        public Guid? GUID { get; set; } = null;

        /// <summary>
        /// True if the operation succeeded.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Result object, where available.
        /// </summary>
        public object Result { get; set; } = null;

        /// <summary>
        /// Error message, if the operation failed.
        /// </summary>
        public string Error { get; set; } = null;
    }
}
