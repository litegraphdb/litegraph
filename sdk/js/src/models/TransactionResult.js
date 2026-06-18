export class TransactionOperationResult {
  constructor(result = {}) {
    const {
      Index = 0,
      OperationType = 'Create',
      ObjectType = 'Node',
      GUID = null,
      Success = true,
      Result = null,
      Error = null,
    } = result;

    this.Index = Index;
    this.OperationType = OperationType;
    this.ObjectType = ObjectType;
    this.GUID = GUID;
    this.Success = Success;
    this.Result = Result;
    this.Error = Error;
  }
}

export default class TransactionResult {
  constructor(result = {}) {
    const {
      Success = true,
      TransactionId = null,
      RolledBack = false,
      ValidationFailure = false,
      FailedOperationIndex = null,
      Error = null,
      Operations = [],
      OperationCount = 0,
      StartedUtc = null,
      CompletedUtc = null,
      DurationMs = 0,
      CommitDurationMs = 0,
      RollbackDurationMs = 0,
      Provider = null,
      IsolationLevel = 'Default',
      IsolatedRepository = false,
      SerializedByGate = false,
      RetryCount = 0,
      Retryable = false,
      ConcurrencyConflict = false,
      ProviderErrorCode = null,
    } = result;

    this.Success = Success;
    this.TransactionId = TransactionId;
    this.RolledBack = RolledBack;
    this.ValidationFailure = ValidationFailure;
    this.FailedOperationIndex = FailedOperationIndex;
    this.Error = Error;
    this.Operations = Operations.map((operation) => new TransactionOperationResult(operation));
    this.OperationCount = OperationCount;
    this.StartedUtc = StartedUtc;
    this.CompletedUtc = CompletedUtc;
    this.DurationMs = DurationMs;
    this.CommitDurationMs = CommitDurationMs;
    this.RollbackDurationMs = RollbackDurationMs;
    this.Provider = Provider;
    this.IsolationLevel = IsolationLevel;
    this.IsolatedRepository = IsolatedRepository;
    this.SerializedByGate = SerializedByGate;
    this.RetryCount = RetryCount;
    this.Retryable = Retryable;
    this.ConcurrencyConflict = ConcurrencyConflict;
    this.ProviderErrorCode = ProviderErrorCode;
  }
}
