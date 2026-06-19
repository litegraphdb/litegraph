namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Graph-scoped transaction methods.
    /// </summary>
    public class TransactionMethods : ITransactionMethods
    {
        #region Private-Members

        private readonly GraphRepositoryBase _Repo;
        private readonly SemaphoreSlim _TransactionGate;
        private static readonly ConditionalWeakTable<GraphRepositoryBase, SemaphoreSlim> TransactionGates = new ConditionalWeakTable<GraphRepositoryBase, SemaphoreSlim>();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public TransactionMethods(GraphRepositoryBase repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _TransactionGate = TransactionGates.GetValue(_Repo, _ => new SemaphoreSlim(1, 1));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public TransactionRequestBuilder CreateRequestBuilder()
        {
            return new TransactionRequestBuilder();
        }

        /// <inheritdoc />
        public async Task<TransactionResult> Execute(Guid tenantGuid, Guid graphGuid, TransactionRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Operations == null || request.Operations.Count < 1) throw new ArgumentException("At least one transaction operation is required.", nameof(request));
            if (request.Operations.Count > request.MaxOperations) throw new ArgumentException("Transaction operation count exceeds MaxOperations.");

            Stopwatch sw = Stopwatch.StartNew();
            using (CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(request.TimeoutSeconds)))
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token))
            {
                CancellationToken linkedToken = linkedCts.Token;
                TransactionExecutionOptions executionOptions = new TransactionExecutionOptions
                {
                    TransactionId = Guid.NewGuid(),
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    IsolationLevel = request.IsolationLevel,
                    OperationCount = request.Operations.Count,
                    Source = "LiteGraphClient.Transaction.Execute"
                };
                TransactionResult result = new TransactionResult
                {
                    TransactionId = executionOptions.TransactionId,
                    OperationCount = request.Operations.Count,
                    StartedUtc = DateTime.UtcNow,
                    IsolationLevel = request.IsolationLevel.ToString(),
                    State = TransactionStateEnum.Created.ToString(),
                    RetryCount = 0
                };

                IRepositorySession session = null;
                GraphTransactionContext transactionContext = null;
                bool startedTransaction = false;
                int currentOperationIndex = -1;
                using Activity activity = LiteGraphTelemetry.ActivitySource.StartActivity(LiteGraphTelemetry.TransactionActivityName, ActivityKind.Internal);
                try
                {
                    ValidateOperations(request, ref currentOperationIndex);
                    activity?.AddEvent(new ActivityEvent("litegraph.transaction.validated"));

                    session = _Repo.CreateRepositorySession(executionOptions);
                    if (session == null) throw new InvalidOperationException("Transaction repository session factory returned null.");
                    transactionContext = new GraphTransactionContext(executionOptions, session, _TransactionGate);

                    SetTransactionActivityStartTags(activity, transactionContext, request.Operations.Count);
                    await transactionContext.BeginAsync(linkedToken).ConfigureAwait(false);
                    startedTransaction = true;
                    result.Provider = transactionContext.Provider;
                    result.IsolatedRepository = transactionContext.IsolatedRepository;
                    result.SerializedByGate = transactionContext.SerializedByGate;
                    result.QueueWaitDurationMs = transactionContext.QueueWaitDurationMs;
                    result.State = transactionContext.State.ToString();
                    activity?.AddEvent(new ActivityEvent("litegraph.transaction.started"));

                    for (int i = 0; i < request.Operations.Count; i++)
                    {
                        currentOperationIndex = i;
                        linkedToken.ThrowIfCancellationRequested();
                        TransactionOperation op = request.Operations[i] ?? throw new ArgumentException("Transaction operation " + i + " is null.");
                        TransactionOperationResult opResult = await ExecuteOperation(transactionContext.Repository, tenantGuid, graphGuid, i, op, linkedToken).ConfigureAwait(false);
                        result.Operations.Add(opResult);
                    }

                    if (startedTransaction)
                    {
                        result.State = TransactionStateEnum.Committing.ToString();
                        await transactionContext.CommitAsync(linkedToken).ConfigureAwait(false);
                        result.State = transactionContext.State.ToString();
                        result.CommitDurationMs = transactionContext.CommitDurationMs;
                        activity?.AddEvent(new ActivityEvent("litegraph.transaction.committed"));
                    }

                    result.Success = true;
                    SetTransactionActivityResultTags(activity, result);
                    LiteGraphTelemetry.SetActivityOk(activity);
                    return result;
                }
                catch (Exception e)
                {
                    if (startedTransaction && transactionContext != null && transactionContext.Repository.GraphTransactionActive)
                    {
                        result.State = TransactionStateEnum.RollingBack.ToString();
                        try
                        {
                            await transactionContext.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                            result.State = transactionContext.State.ToString();
                            activity?.AddEvent(new ActivityEvent("litegraph.transaction.rolled_back"));
                        }
                        catch
                        {
                            result.State = TransactionStateEnum.Faulted.ToString();
                        }
                        finally
                        {
                            result.RollbackDurationMs = transactionContext.RollbackDurationMs;
                        }
                    }

                    result.Success = false;
                    result.RolledBack = startedTransaction;
                    if (transactionContext != null)
                    {
                        result.Provider = transactionContext.Provider;
                        result.IsolatedRepository = transactionContext.IsolatedRepository;
                        result.SerializedByGate = transactionContext.SerializedByGate;
                        result.QueueWaitDurationMs = transactionContext.QueueWaitDurationMs;
                        result.CommitDurationMs = transactionContext.CommitDurationMs;
                        result.RollbackDurationMs = transactionContext.RollbackDurationMs;
                    }
                    if (!String.Equals(result.State, TransactionStateEnum.RolledBack.ToString(), StringComparison.Ordinal)
                        && !String.Equals(result.State, TransactionStateEnum.Faulted.ToString(), StringComparison.Ordinal))
                    {
                        result.State = TransactionStateEnum.Faulted.ToString();
                    }
                    result.ValidationFailure = !startedTransaction && IsValidationException(e);
                    result.Error = e.Message;
                    ApplyProviderErrorDiagnostics(result, e);
                    result.FailedOperationIndex = currentOperationIndex >= 0 ? currentOperationIndex : result.Operations.Count;
                    if (request.Operations != null
                        && result.FailedOperationIndex >= 0
                        && result.FailedOperationIndex < request.Operations.Count
                        && request.Operations[result.FailedOperationIndex.Value] != null)
                    {
                        TransactionOperation failed = request.Operations[result.FailedOperationIndex.Value];
                        result.Operations.Add(new TransactionOperationResult
                        {
                            Index = result.FailedOperationIndex.Value,
                            OperationType = failed.OperationType,
                            ObjectType = failed.ObjectType,
                            GUID = failed.GUID,
                            Success = false,
                            Error = e.Message
                        });
                    }

                    SetTransactionActivityResultTags(activity, result);
                    LiteGraphTelemetry.SetActivityException(activity, e);
                    return result;
                }
                finally
                {
                    if (transactionContext != null)
                    {
                        await transactionContext.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (session != null)
                    {
                        await session.DisposeAsync().ConfigureAwait(false);
                    }

                    sw.Stop();
                    result.CompletedUtc = DateTime.UtcNow;
                    result.DurationMs = sw.Elapsed.TotalMilliseconds;
                }
            }
        }

        #endregion

        #region Private-Methods

        private async Task<TransactionOperationResult> ExecuteOperation(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            switch (op.OperationType)
            {
                case TransactionOperationTypeEnum.Create:
                    return await Create(repo, tenantGuid, graphGuid, index, op, token).ConfigureAwait(false);
                case TransactionOperationTypeEnum.Update:
                    return await Update(repo, tenantGuid, graphGuid, index, op, token).ConfigureAwait(false);
                case TransactionOperationTypeEnum.Delete:
                    return await Delete(repo, tenantGuid, graphGuid, index, op, token).ConfigureAwait(false);
                case TransactionOperationTypeEnum.Attach:
                    return await Attach(repo, tenantGuid, graphGuid, index, op, token).ConfigureAwait(false);
                case TransactionOperationTypeEnum.Detach:
                    return await Detach(repo, tenantGuid, graphGuid, index, op, token).ConfigureAwait(false);
                case TransactionOperationTypeEnum.Upsert:
                    return await Upsert(repo, tenantGuid, graphGuid, index, op, token).ConfigureAwait(false);
                default:
                    throw new NotSupportedException("Unsupported transaction operation type '" + op.OperationType + "'.");
            }
        }

        private static void SetTransactionActivityStartTags(Activity activity, GraphTransactionContext context, int operationCount)
        {
            if (activity == null || context == null) return;

            activity.SetTag("litegraph.transaction.id", context.Options.TransactionId);
            activity.SetTag("litegraph.transaction.source", context.Options.Source);
            activity.SetTag("litegraph.transaction.provider", context.Provider);
            activity.SetTag("litegraph.transaction.isolation_level", context.Options.IsolationLevel.ToString());
            activity.SetTag("litegraph.transaction.operation_count", operationCount);
            activity.SetTag("litegraph.transaction.isolated_repository", context.IsolatedRepository);
            activity.SetTag("litegraph.transaction.serialized_by_gate", context.SerializedByGate);
        }

        private static void SetTransactionActivityResultTags(Activity activity, TransactionResult result)
        {
            if (activity == null || result == null) return;

            activity.SetTag("litegraph.transaction.state", result.State);
            activity.SetTag("litegraph.transaction.success", result.Success);
            activity.SetTag("litegraph.transaction.rolled_back", result.RolledBack);
            activity.SetTag("litegraph.transaction.validation_failure", result.ValidationFailure);
            activity.SetTag("litegraph.transaction.queue_wait_duration_ms", result.QueueWaitDurationMs);
            activity.SetTag("litegraph.transaction.commit_duration_ms", result.CommitDurationMs);
            activity.SetTag("litegraph.transaction.rollback_duration_ms", result.RollbackDurationMs);
            activity.SetTag("litegraph.transaction.retry_count", result.RetryCount);
            activity.SetTag("litegraph.transaction.retryable", result.Retryable);
            activity.SetTag("litegraph.transaction.concurrency_conflict", result.ConcurrencyConflict);
            if (!String.IsNullOrEmpty(result.ProviderErrorCode)) activity.SetTag("litegraph.transaction.provider_error_code", result.ProviderErrorCode);
            if (result.FailedOperationIndex != null) activity.SetTag("litegraph.transaction.failed_operation_index", result.FailedOperationIndex.Value);
        }

        private static void ValidateOperations(TransactionRequest request, ref int currentOperationIndex)
        {
            for (int i = 0; i < request.Operations.Count; i++)
            {
                currentOperationIndex = i;

                TransactionOperation op = request.Operations[i];
                if (op == null) throw new ArgumentException("Transaction operation " + i + " is null.");

                switch (op.OperationType)
                {
                    case TransactionOperationTypeEnum.Create:
                    case TransactionOperationTypeEnum.Update:
                    case TransactionOperationTypeEnum.Delete:
                    case TransactionOperationTypeEnum.Attach:
                    case TransactionOperationTypeEnum.Detach:
                    case TransactionOperationTypeEnum.Upsert:
                        break;
                    default:
                        throw new NotSupportedException("Unsupported transaction operation type '" + op.OperationType + "'.");
                }

                switch (op.ObjectType)
                {
                    case TransactionObjectTypeEnum.Node:
                    case TransactionObjectTypeEnum.Edge:
                    case TransactionObjectTypeEnum.Label:
                    case TransactionObjectTypeEnum.Tag:
                    case TransactionObjectTypeEnum.Vector:
                        break;
                    default:
                        throw new NotSupportedException("Unsupported transaction object type '" + op.ObjectType + "'.");
                }

                if ((op.OperationType == TransactionOperationTypeEnum.Create
                    || op.OperationType == TransactionOperationTypeEnum.Update
                    || op.OperationType == TransactionOperationTypeEnum.Attach
                    || op.OperationType == TransactionOperationTypeEnum.Upsert)
                    && op.Payload == null)
                {
                    throw new ArgumentException("Transaction operation " + i + " requires a payload.");
                }

                if ((op.OperationType == TransactionOperationTypeEnum.Delete || op.OperationType == TransactionOperationTypeEnum.Detach)
                    && op.GUID == null
                    && !TryExtractGuidFromPayload(op.Payload, out _))
                {
                    throw new ArgumentException("Transaction operation " + i + " " + op.OperationType.ToString().ToLowerInvariant() + " operations require GUID or payload with GUID.");
                }

                if ((op.OperationType == TransactionOperationTypeEnum.Attach || op.OperationType == TransactionOperationTypeEnum.Detach)
                    && !IsAttachableObject(op.ObjectType))
                {
                    throw new ArgumentException("Transaction operation " + i + " " + op.OperationType.ToString().ToLowerInvariant() + " operations only support labels, tags, and vectors.");
                }
            }

            currentOperationIndex = -1;
        }

        private async Task<TransactionOperationResult> Create(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            object created;
            Guid? guid;

            switch (op.ObjectType)
            {
                case TransactionObjectTypeEnum.Node:
                    Node node = PrepareNode(tenantGuid, graphGuid, ConvertPayload<Node>(op.Payload));
                    created = await repo.Node.Create(node, token).ConfigureAwait(false);
                    guid = ((Node)created).GUID;
                    break;
                case TransactionObjectTypeEnum.Edge:
                    Edge edge = PrepareEdge(tenantGuid, graphGuid, ConvertPayload<Edge>(op.Payload));
                    created = await repo.Edge.Create(edge, token).ConfigureAwait(false);
                    guid = ((Edge)created).GUID;
                    break;
                case TransactionObjectTypeEnum.Label:
                    LabelMetadata label = PrepareLabel(tenantGuid, graphGuid, ConvertPayload<LabelMetadata>(op.Payload));
                    created = await repo.Label.Create(label, token).ConfigureAwait(false);
                    guid = ((LabelMetadata)created).GUID;
                    break;
                case TransactionObjectTypeEnum.Tag:
                    TagMetadata tag = PrepareTag(tenantGuid, graphGuid, ConvertPayload<TagMetadata>(op.Payload));
                    created = await repo.Tag.Create(tag, token).ConfigureAwait(false);
                    guid = ((TagMetadata)created).GUID;
                    break;
                case TransactionObjectTypeEnum.Vector:
                    VectorMetadata vector = PrepareVector(tenantGuid, graphGuid, ConvertPayload<VectorMetadata>(op.Payload));
                    created = await repo.Vector.Create(vector, token).ConfigureAwait(false);
                    guid = ((VectorMetadata)created).GUID;
                    break;
                default:
                    throw new NotSupportedException("Unsupported transaction object type '" + op.ObjectType + "'.");
            }

            return Success(index, op, guid, created);
        }

        private async Task<TransactionOperationResult> Update(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            object updated;
            Guid? guid;

            switch (op.ObjectType)
            {
                case TransactionObjectTypeEnum.Node:
                    Node node = PrepareNode(tenantGuid, graphGuid, ConvertPayload<Node>(op.Payload));
                    if (op.GUID != null) node.GUID = op.GUID.Value;
                    updated = await repo.Node.Update(node, token).ConfigureAwait(false);
                    guid = ((Node)updated).GUID;
                    break;
                case TransactionObjectTypeEnum.Edge:
                    Edge edge = PrepareEdge(tenantGuid, graphGuid, ConvertPayload<Edge>(op.Payload));
                    if (op.GUID != null) edge.GUID = op.GUID.Value;
                    updated = await repo.Edge.Update(edge, token).ConfigureAwait(false);
                    guid = ((Edge)updated).GUID;
                    break;
                case TransactionObjectTypeEnum.Label:
                    LabelMetadata label = PrepareLabel(tenantGuid, graphGuid, ConvertPayload<LabelMetadata>(op.Payload));
                    if (op.GUID != null) label.GUID = op.GUID.Value;
                    updated = await repo.Label.Update(label, token).ConfigureAwait(false);
                    guid = ((LabelMetadata)updated).GUID;
                    break;
                case TransactionObjectTypeEnum.Tag:
                    TagMetadata tag = PrepareTag(tenantGuid, graphGuid, ConvertPayload<TagMetadata>(op.Payload));
                    if (op.GUID != null) tag.GUID = op.GUID.Value;
                    updated = await repo.Tag.Update(tag, token).ConfigureAwait(false);
                    guid = ((TagMetadata)updated).GUID;
                    break;
                case TransactionObjectTypeEnum.Vector:
                    VectorMetadata vector = PrepareVector(tenantGuid, graphGuid, ConvertPayload<VectorMetadata>(op.Payload));
                    if (op.GUID != null) vector.GUID = op.GUID.Value;
                    updated = await repo.Vector.Update(vector, token).ConfigureAwait(false);
                    guid = ((VectorMetadata)updated).GUID;
                    break;
                default:
                    throw new NotSupportedException("Unsupported transaction object type '" + op.ObjectType + "'.");
            }

            return Success(index, op, guid, updated);
        }

        private async Task<TransactionOperationResult> Delete(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            Guid guid = ResolveGuid(op);

            switch (op.ObjectType)
            {
                case TransactionObjectTypeEnum.Node:
                    await repo.Node.DeleteByGuid(tenantGuid, graphGuid, guid, token).ConfigureAwait(false);
                    break;
                case TransactionObjectTypeEnum.Edge:
                    await repo.Edge.DeleteByGuid(tenantGuid, graphGuid, guid, token).ConfigureAwait(false);
                    break;
                case TransactionObjectTypeEnum.Label:
                    await repo.Label.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
                    break;
                case TransactionObjectTypeEnum.Tag:
                    await repo.Tag.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
                    break;
                case TransactionObjectTypeEnum.Vector:
                    await repo.Vector.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException("Unsupported transaction object type '" + op.ObjectType + "'.");
            }

            return Success(index, op, guid, null);
        }

        private async Task<TransactionOperationResult> Attach(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            TransactionOperation create = new TransactionOperation
            {
                OperationType = TransactionOperationTypeEnum.Attach,
                ObjectType = op.ObjectType,
                GUID = op.GUID,
                Payload = op.Payload
            };

            object created;
            Guid? guid;

            switch (op.ObjectType)
            {
                case TransactionObjectTypeEnum.Label:
                    LabelMetadata label = PrepareLabel(tenantGuid, graphGuid, ConvertPayload<LabelMetadata>(op.Payload));
                    if (op.GUID != null) label.GUID = op.GUID.Value;
                    ValidateAttachmentTarget(label.NodeGUID, label.EdgeGUID, "Label attach");
                    created = await repo.Label.Create(label, token).ConfigureAwait(false);
                    guid = ((LabelMetadata)created).GUID;
                    break;
                case TransactionObjectTypeEnum.Tag:
                    TagMetadata tag = PrepareTag(tenantGuid, graphGuid, ConvertPayload<TagMetadata>(op.Payload));
                    if (op.GUID != null) tag.GUID = op.GUID.Value;
                    ValidateAttachmentTarget(tag.NodeGUID, tag.EdgeGUID, "Tag attach");
                    created = await repo.Tag.Create(tag, token).ConfigureAwait(false);
                    guid = ((TagMetadata)created).GUID;
                    break;
                case TransactionObjectTypeEnum.Vector:
                    VectorMetadata vector = PrepareVector(tenantGuid, graphGuid, ConvertPayload<VectorMetadata>(op.Payload));
                    if (op.GUID != null) vector.GUID = op.GUID.Value;
                    ValidateAttachmentTarget(vector.NodeGUID, vector.EdgeGUID, "Vector attach");
                    created = await repo.Vector.Create(vector, token).ConfigureAwait(false);
                    guid = ((VectorMetadata)created).GUID;
                    break;
                default:
                    throw new NotSupportedException("Attach operations only support labels, tags, and vectors.");
            }

            return Success(index, create, guid, created);
        }

        private async Task<TransactionOperationResult> Detach(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            return await Delete(repo, tenantGuid, graphGuid, index, new TransactionOperation
            {
                OperationType = TransactionOperationTypeEnum.Detach,
                ObjectType = op.ObjectType,
                GUID = op.GUID,
                Payload = op.Payload
            }, token).ConfigureAwait(false);
        }

        private async Task<TransactionOperationResult> Upsert(GraphRepositoryBase repo, Guid tenantGuid, Guid graphGuid, int index, TransactionOperation op, CancellationToken token)
        {
            object saved;
            Guid? guid;
            bool exists;

            switch (op.ObjectType)
            {
                case TransactionObjectTypeEnum.Node:
                    Node node = PrepareNode(tenantGuid, graphGuid, ConvertPayload<Node>(op.Payload));
                    if (op.GUID != null) node.GUID = op.GUID.Value;
                    Node existingNode = await repo.Node.ReadByGuid(tenantGuid, node.GUID, token).ConfigureAwait(false);
                    if (existingNode != null && existingNode.GraphGUID != graphGuid) throw new InvalidOperationException("Node upsert target belongs to another graph.");
                    exists = existingNode != null;
                    saved = exists ? await repo.Node.Update(node, token).ConfigureAwait(false) : await repo.Node.Create(node, token).ConfigureAwait(false);
                    guid = ((Node)saved).GUID;
                    break;
                case TransactionObjectTypeEnum.Edge:
                    Edge edge = PrepareEdge(tenantGuid, graphGuid, ConvertPayload<Edge>(op.Payload));
                    if (op.GUID != null) edge.GUID = op.GUID.Value;
                    Edge existingEdge = await repo.Edge.ReadByGuid(tenantGuid, edge.GUID, token).ConfigureAwait(false);
                    if (existingEdge != null && existingEdge.GraphGUID != graphGuid) throw new InvalidOperationException("Edge upsert target belongs to another graph.");
                    exists = existingEdge != null;
                    saved = exists ? await repo.Edge.Update(edge, token).ConfigureAwait(false) : await repo.Edge.Create(edge, token).ConfigureAwait(false);
                    guid = ((Edge)saved).GUID;
                    break;
                case TransactionObjectTypeEnum.Label:
                    LabelMetadata label = PrepareLabel(tenantGuid, graphGuid, ConvertPayload<LabelMetadata>(op.Payload));
                    if (op.GUID != null) label.GUID = op.GUID.Value;
                    LabelMetadata existingLabel = await repo.Label.ReadByGuid(tenantGuid, label.GUID, token).ConfigureAwait(false);
                    if (existingLabel != null && existingLabel.GraphGUID != graphGuid) throw new InvalidOperationException("Label upsert target belongs to another graph.");
                    exists = existingLabel != null;
                    saved = exists ? await repo.Label.Update(label, token).ConfigureAwait(false) : await repo.Label.Create(label, token).ConfigureAwait(false);
                    guid = ((LabelMetadata)saved).GUID;
                    break;
                case TransactionObjectTypeEnum.Tag:
                    TagMetadata tag = PrepareTag(tenantGuid, graphGuid, ConvertPayload<TagMetadata>(op.Payload));
                    if (op.GUID != null) tag.GUID = op.GUID.Value;
                    TagMetadata existingTag = await repo.Tag.ReadByGuid(tenantGuid, tag.GUID, token).ConfigureAwait(false);
                    if (existingTag != null && existingTag.GraphGUID != graphGuid) throw new InvalidOperationException("Tag upsert target belongs to another graph.");
                    exists = existingTag != null;
                    saved = exists ? await repo.Tag.Update(tag, token).ConfigureAwait(false) : await repo.Tag.Create(tag, token).ConfigureAwait(false);
                    guid = ((TagMetadata)saved).GUID;
                    break;
                case TransactionObjectTypeEnum.Vector:
                    VectorMetadata vector = PrepareVector(tenantGuid, graphGuid, ConvertPayload<VectorMetadata>(op.Payload));
                    if (op.GUID != null) vector.GUID = op.GUID.Value;
                    VectorMetadata existingVector = await repo.Vector.ReadByGuid(tenantGuid, vector.GUID, token).ConfigureAwait(false);
                    if (existingVector != null && existingVector.GraphGUID != graphGuid) throw new InvalidOperationException("Vector upsert target belongs to another graph.");
                    exists = existingVector != null;
                    saved = exists ? await repo.Vector.Update(vector, token).ConfigureAwait(false) : await repo.Vector.Create(vector, token).ConfigureAwait(false);
                    guid = ((VectorMetadata)saved).GUID;
                    break;
                default:
                    throw new NotSupportedException("Unsupported transaction object type '" + op.ObjectType + "'.");
            }

            return Success(index, op, guid, saved);
        }

        private static TransactionOperationResult Success(int index, TransactionOperation op, Guid? guid, object result)
        {
            return new TransactionOperationResult
            {
                Index = index,
                OperationType = op.OperationType,
                ObjectType = op.ObjectType,
                GUID = guid,
                Success = true,
                Result = result
            };
        }

        private static T ConvertPayload<T>(object payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload is T typed) return typed;

            if (payload is JsonElement element)
                return JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions);

            string json = JsonSerializer.Serialize(payload, JsonOptions);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        private static string ResolveProviderName(GraphRepositoryBase repo)
        {
            if (repo == null) return null;

            string typeName = repo.GetType().Name;
            const string suffix = "GraphRepository";
            if (typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length)
                return typeName.Substring(0, typeName.Length - suffix.Length);

            return typeName;
        }

        private static bool IsValidationException(Exception e)
        {
            return e is ArgumentNullException
                || e is ArgumentOutOfRangeException
                || e is ArgumentException
                || e is FormatException
                || e is NotSupportedException;
        }

        private static void ApplyProviderErrorDiagnostics(TransactionResult result, Exception exception)
        {
            if (result == null || exception == null) return;

            Exception current = exception;
            while (current != null)
            {
                string typeName = current.GetType().FullName;
                string providerCode = ReadStringProperty(current, "SqlState") ?? ReadIntProperty(current, "SqliteErrorCode");
                if (!String.IsNullOrEmpty(providerCode))
                {
                    result.ProviderErrorCode = providerCode;
                    if (IsRetryableProviderError(typeName, providerCode))
                    {
                        result.Retryable = true;
                        result.ConcurrencyConflict = true;
                    }

                    return;
                }

                current = current.InnerException;
            }
        }

        private static string ReadStringProperty(Exception exception, string propertyName)
        {
            object value = exception.GetType().GetProperty(propertyName)?.GetValue(exception);
            return value as string;
        }

        private static string ReadIntProperty(Exception exception, string propertyName)
        {
            object value = exception.GetType().GetProperty(propertyName)?.GetValue(exception);
            if (value == null) return null;
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool IsRetryableProviderError(string exceptionTypeName, string providerCode)
        {
            if (String.IsNullOrEmpty(providerCode)) return false;

            if (String.Equals(providerCode, "40001", StringComparison.Ordinal)
                || String.Equals(providerCode, "40P01", StringComparison.Ordinal)
                || String.Equals(providerCode, "55P03", StringComparison.Ordinal))
            {
                return true;
            }

            if (String.Equals(providerCode, "5", StringComparison.Ordinal)
                || String.Equals(providerCode, "6", StringComparison.Ordinal))
            {
                return exceptionTypeName != null
                    && exceptionTypeName.IndexOf("Sqlite", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }

        private static Guid ResolveGuid(TransactionOperation op)
        {
            if (op.GUID != null) return op.GUID.Value;
            if (op.Payload == null) throw new ArgumentException("Delete operations require GUID or payload with GUID.");
            if (TryExtractGuidFromPayload(op.Payload, out Guid guid)) return guid;

            throw new ArgumentException("Delete operations require GUID or payload with GUID.");
        }

        private static bool TryExtractGuidFromPayload(object payload, out Guid guid)
        {
            guid = Guid.Empty;
            if (payload == null) return false;

            if (payload is JsonElement element)
            {
                return TryExtractGuidFromJson(element, out guid);
            }

            string json = JsonSerializer.Serialize(payload, JsonOptions);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                return TryExtractGuidFromJson(doc.RootElement, out guid);
            }
        }

        private static bool TryExtractGuidFromJson(JsonElement element, out Guid guid)
        {
            guid = Guid.Empty;
            if (element.ValueKind != JsonValueKind.Object) return false;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, "GUID", StringComparison.OrdinalIgnoreCase)
                    && TryParseGuid(property.Value, out guid))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseGuid(JsonElement element, out Guid guid)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return Guid.TryParse(element.GetString(), out guid);
            }

            return Guid.TryParse(element.ToString(), out guid);
        }

        private static bool IsAttachableObject(TransactionObjectTypeEnum objectType)
        {
            return objectType == TransactionObjectTypeEnum.Label
                || objectType == TransactionObjectTypeEnum.Tag
                || objectType == TransactionObjectTypeEnum.Vector;
        }

        private static void ValidateAttachmentTarget(Guid? nodeGuid, Guid? edgeGuid, string description)
        {
            if (nodeGuid == null && edgeGuid == null)
                throw new ArgumentException(description + " requires a node or edge target.");
            if (nodeGuid != null && edgeGuid != null)
                throw new ArgumentException(description + " can target either a node or an edge, not both.");
        }

        private static Node PrepareNode(Guid tenantGuid, Guid graphGuid, Node node)
        {
            node.TenantGUID = tenantGuid;
            node.GraphGUID = graphGuid;
            return node;
        }

        private static Edge PrepareEdge(Guid tenantGuid, Guid graphGuid, Edge edge)
        {
            edge.TenantGUID = tenantGuid;
            edge.GraphGUID = graphGuid;
            return edge;
        }

        private static LabelMetadata PrepareLabel(Guid tenantGuid, Guid graphGuid, LabelMetadata label)
        {
            label.TenantGUID = tenantGuid;
            label.GraphGUID = graphGuid;
            return label;
        }

        private static TagMetadata PrepareTag(Guid tenantGuid, Guid graphGuid, TagMetadata tag)
        {
            tag.TenantGUID = tenantGuid;
            tag.GraphGUID = graphGuid;
            return tag;
        }

        private static VectorMetadata PrepareVector(Guid tenantGuid, Guid graphGuid, VectorMetadata vector)
        {
            vector.TenantGUID = tenantGuid;
            vector.GraphGUID = graphGuid;
            return vector;
        }

        #endregion
    }
}
