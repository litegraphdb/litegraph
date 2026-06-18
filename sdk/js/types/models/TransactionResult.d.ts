export class TransactionOperationResult {
    constructor(result?: {});
    Index: any;
    OperationType: any;
    ObjectType: any;
    GUID: any;
    Success: any;
    Result: any;
    Error: any;
}
export default class TransactionResult {
    constructor(result?: {});
    Success: any;
    TransactionId: any;
    RolledBack: any;
    ValidationFailure: any;
    FailedOperationIndex: any;
    Error: any;
    Operations: any;
    OperationCount: any;
    StartedUtc: any;
    CompletedUtc: any;
    DurationMs: any;
    CommitDurationMs: any;
    RollbackDurationMs: any;
    Provider: any;
    IsolationLevel: any;
    IsolatedRepository: any;
    SerializedByGate: any;
    RetryCount: any;
    Retryable: any;
    ConcurrencyConflict: any;
    ProviderErrorCode: any;
}
