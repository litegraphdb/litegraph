namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Fluent builder for graph-scoped transaction requests.
    /// </summary>
    public class GraphTransactionBuilder
    {
        /// <summary>
        /// Number of operations currently added to the request.
        /// </summary>
        public int Count
        {
            get
            {
                return _Operations.Count;
            }
        }

        private readonly List<TransactionOperation> _Operations = new List<TransactionOperation>();
        private int _MaxOperations = 1000;
        private int _TimeoutSeconds = 60;
        private TransactionIsolationLevelEnum _IsolationLevel = TransactionIsolationLevelEnum.Default;

        /// <summary>
        /// Set the maximum operation count for the request.
        /// </summary>
        /// <param name="maxOperations">Maximum operation count.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder WithMaxOperations(int maxOperations)
        {
            TransactionRequest request = new TransactionRequest
            {
                MaxOperations = maxOperations
            };

            _MaxOperations = request.MaxOperations;
            return this;
        }

        /// <summary>
        /// Set the timeout for the request.
        /// </summary>
        /// <param name="timeoutSeconds">Timeout in seconds.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder WithTimeoutSeconds(int timeoutSeconds)
        {
            TransactionRequest request = new TransactionRequest
            {
                TimeoutSeconds = timeoutSeconds
            };

            _TimeoutSeconds = request.TimeoutSeconds;
            return this;
        }

        /// <summary>
        /// Set the provider transaction isolation level for the request.
        /// </summary>
        /// <param name="isolationLevel">Isolation level.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder WithIsolationLevel(TransactionIsolationLevelEnum isolationLevel)
        {
            _IsolationLevel = isolationLevel;
            return this;
        }

        /// <summary>
        /// Add a raw transaction operation.
        /// </summary>
        /// <param name="operation">Transaction operation.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Add(TransactionOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _Operations.Add(operation);
            return this;
        }

        /// <summary>
        /// Add a create operation.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        /// <param name="payload">Object payload.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Create(TransactionObjectTypeEnum objectType, object payload)
        {
            return AddPayloadOperation(TransactionOperationTypeEnum.Create, objectType, payload, null);
        }

        /// <summary>
        /// Add an update operation.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        /// <param name="payload">Object payload.</param>
        /// <param name="guid">Object GUID. If set, this overrides any payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Update(TransactionObjectTypeEnum objectType, object payload, Guid? guid = null)
        {
            return AddPayloadOperation(TransactionOperationTypeEnum.Update, objectType, payload, guid);
        }

        /// <summary>
        /// Add a delete operation.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        /// <param name="guid">Object GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Delete(TransactionObjectTypeEnum objectType, Guid guid)
        {
            return Add(new TransactionOperation
            {
                OperationType = TransactionOperationTypeEnum.Delete,
                ObjectType = objectType,
                GUID = guid
            });
        }

        /// <summary>
        /// Add an attach operation for a label, tag, or vector subordinate.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        /// <param name="payload">Object payload.</param>
        /// <param name="guid">Object GUID. If set, this overrides any payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Attach(TransactionObjectTypeEnum objectType, object payload, Guid? guid = null)
        {
            return AddPayloadOperation(TransactionOperationTypeEnum.Attach, objectType, payload, guid);
        }

        /// <summary>
        /// Add a detach operation for a label, tag, or vector subordinate.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        /// <param name="guid">Object GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Detach(TransactionObjectTypeEnum objectType, Guid guid)
        {
            return Add(new TransactionOperation
            {
                OperationType = TransactionOperationTypeEnum.Detach,
                ObjectType = objectType,
                GUID = guid
            });
        }

        /// <summary>
        /// Add an upsert operation.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        /// <param name="payload">Object payload.</param>
        /// <param name="guid">Object GUID. If set, this overrides any payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder Upsert(TransactionObjectTypeEnum objectType, object payload, Guid? guid = null)
        {
            return AddPayloadOperation(TransactionOperationTypeEnum.Upsert, objectType, payload, guid);
        }

        /// <summary>
        /// Add a node create operation.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder CreateNode(Node node)
        {
            return Create(TransactionObjectTypeEnum.Node, node);
        }

        /// <summary>
        /// Add a node update operation.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="guid">Node GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpdateNode(Node node, Guid? guid = null)
        {
            return Update(TransactionObjectTypeEnum.Node, node, guid);
        }

        /// <summary>
        /// Add a node delete operation.
        /// </summary>
        /// <param name="guid">Node GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DeleteNode(Guid guid)
        {
            return Delete(TransactionObjectTypeEnum.Node, guid);
        }

        /// <summary>
        /// Add a node upsert operation.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="guid">Node GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpsertNode(Node node, Guid? guid = null)
        {
            return Upsert(TransactionObjectTypeEnum.Node, node, guid);
        }

        /// <summary>
        /// Add an edge create operation.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder CreateEdge(Edge edge)
        {
            return Create(TransactionObjectTypeEnum.Edge, edge);
        }

        /// <summary>
        /// Add an edge update operation.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <param name="guid">Edge GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpdateEdge(Edge edge, Guid? guid = null)
        {
            return Update(TransactionObjectTypeEnum.Edge, edge, guid);
        }

        /// <summary>
        /// Add an edge delete operation.
        /// </summary>
        /// <param name="guid">Edge GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DeleteEdge(Guid guid)
        {
            return Delete(TransactionObjectTypeEnum.Edge, guid);
        }

        /// <summary>
        /// Add an edge upsert operation.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <param name="guid">Edge GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpsertEdge(Edge edge, Guid? guid = null)
        {
            return Upsert(TransactionObjectTypeEnum.Edge, edge, guid);
        }

        /// <summary>
        /// Add a label create operation.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder CreateLabel(LabelMetadata label)
        {
            return Create(TransactionObjectTypeEnum.Label, label);
        }

        /// <summary>
        /// Add a label update operation.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <param name="guid">Label GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpdateLabel(LabelMetadata label, Guid? guid = null)
        {
            return Update(TransactionObjectTypeEnum.Label, label, guid);
        }

        /// <summary>
        /// Add a label delete operation.
        /// </summary>
        /// <param name="guid">Label GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DeleteLabel(Guid guid)
        {
            return Delete(TransactionObjectTypeEnum.Label, guid);
        }

        /// <summary>
        /// Add a label attach operation.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <param name="guid">Label GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder AttachLabel(LabelMetadata label, Guid? guid = null)
        {
            return Attach(TransactionObjectTypeEnum.Label, label, guid);
        }

        /// <summary>
        /// Add a label detach operation.
        /// </summary>
        /// <param name="guid">Label GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DetachLabel(Guid guid)
        {
            return Detach(TransactionObjectTypeEnum.Label, guid);
        }

        /// <summary>
        /// Add a label upsert operation.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <param name="guid">Label GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpsertLabel(LabelMetadata label, Guid? guid = null)
        {
            return Upsert(TransactionObjectTypeEnum.Label, label, guid);
        }

        /// <summary>
        /// Add a tag create operation.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder CreateTag(TagMetadata tag)
        {
            return Create(TransactionObjectTypeEnum.Tag, tag);
        }

        /// <summary>
        /// Add a tag update operation.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <param name="guid">Tag GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpdateTag(TagMetadata tag, Guid? guid = null)
        {
            return Update(TransactionObjectTypeEnum.Tag, tag, guid);
        }

        /// <summary>
        /// Add a tag delete operation.
        /// </summary>
        /// <param name="guid">Tag GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DeleteTag(Guid guid)
        {
            return Delete(TransactionObjectTypeEnum.Tag, guid);
        }

        /// <summary>
        /// Add a tag attach operation.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <param name="guid">Tag GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder AttachTag(TagMetadata tag, Guid? guid = null)
        {
            return Attach(TransactionObjectTypeEnum.Tag, tag, guid);
        }

        /// <summary>
        /// Add a tag detach operation.
        /// </summary>
        /// <param name="guid">Tag GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DetachTag(Guid guid)
        {
            return Detach(TransactionObjectTypeEnum.Tag, guid);
        }

        /// <summary>
        /// Add a tag upsert operation.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <param name="guid">Tag GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpsertTag(TagMetadata tag, Guid? guid = null)
        {
            return Upsert(TransactionObjectTypeEnum.Tag, tag, guid);
        }

        /// <summary>
        /// Add a vector create operation.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder CreateVector(VectorMetadata vector)
        {
            return Create(TransactionObjectTypeEnum.Vector, vector);
        }

        /// <summary>
        /// Add a vector update operation.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <param name="guid">Vector GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpdateVector(VectorMetadata vector, Guid? guid = null)
        {
            return Update(TransactionObjectTypeEnum.Vector, vector, guid);
        }

        /// <summary>
        /// Add a vector delete operation.
        /// </summary>
        /// <param name="guid">Vector GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DeleteVector(Guid guid)
        {
            return Delete(TransactionObjectTypeEnum.Vector, guid);
        }

        /// <summary>
        /// Add a vector attach operation.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <param name="guid">Vector GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder AttachVector(VectorMetadata vector, Guid? guid = null)
        {
            return Attach(TransactionObjectTypeEnum.Vector, vector, guid);
        }

        /// <summary>
        /// Add a vector detach operation.
        /// </summary>
        /// <param name="guid">Vector GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder DetachVector(Guid guid)
        {
            return Detach(TransactionObjectTypeEnum.Vector, guid);
        }

        /// <summary>
        /// Add a vector upsert operation.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <param name="guid">Vector GUID. If set, this overrides the payload GUID.</param>
        /// <returns>The transaction request builder.</returns>
        public GraphTransactionBuilder UpsertVector(VectorMetadata vector, Guid? guid = null)
        {
            return Upsert(TransactionObjectTypeEnum.Vector, vector, guid);
        }

        /// <summary>
        /// Build the transaction request.
        /// </summary>
        /// <returns>Transaction request.</returns>
        public TransactionRequest Build()
        {
            return new TransactionRequest
            {
                MaxOperations = _MaxOperations,
                TimeoutSeconds = _TimeoutSeconds,
                IsolationLevel = _IsolationLevel,
                Operations = new List<TransactionOperation>(_Operations)
            };
        }

        private GraphTransactionBuilder AddPayloadOperation(
            TransactionOperationTypeEnum operationType,
            TransactionObjectTypeEnum objectType,
            object payload,
            Guid? guid)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            return Add(new TransactionOperation
            {
                OperationType = operationType,
                ObjectType = objectType,
                GUID = guid,
                Payload = payload
            });
        }
    }
}
