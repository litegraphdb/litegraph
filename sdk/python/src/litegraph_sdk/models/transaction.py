from typing import Any, List, Optional

from pydantic import BaseModel, ConfigDict, Field


class TransactionOperationModel(BaseModel):
    """
    A single graph-scoped transaction operation.
    """

    operation_type: str = Field(default="Create", alias="OperationType")
    object_type: str = Field(default="Node", alias="ObjectType")
    guid: Optional[str] = Field(default=None, alias="GUID")
    payload: Optional[Any] = Field(default=None, alias="Payload")
    model_config = ConfigDict(populate_by_name=True)


class TransactionOperationResultModel(BaseModel):
    """
    Result for a single graph transaction operation.
    """

    index: int = Field(default=0, alias="Index")
    operation_type: str = Field(default="Create", alias="OperationType")
    object_type: str = Field(default="Node", alias="ObjectType")
    guid: Optional[str] = Field(default=None, alias="GUID")
    success: bool = Field(default=True, alias="Success")
    result: Optional[Any] = Field(default=None, alias="Result")
    error: Optional[str] = Field(default=None, alias="Error")
    model_config = ConfigDict(populate_by_name=True)


class TransactionRequestModel(BaseModel):
    """
    Graph-scoped transaction request.
    """

    operations: List[TransactionOperationModel] = Field(
        default_factory=list, alias="Operations"
    )
    max_operations: int = Field(default=1000, alias="MaxOperations", ge=1, le=10000)
    timeout_seconds: int = Field(default=60, alias="TimeoutSeconds", ge=1, le=3600)
    isolation_level: str = Field(default="Default", alias="IsolationLevel")
    model_config = ConfigDict(populate_by_name=True)


class TransactionResultModel(BaseModel):
    """
    Graph-scoped transaction result.
    """

    success: bool = Field(default=True, alias="Success")
    transaction_id: Optional[str] = Field(default=None, alias="TransactionId")
    state: str = Field(default="Created", alias="State")
    rolled_back: bool = Field(default=False, alias="RolledBack")
    validation_failure: bool = Field(default=False, alias="ValidationFailure")
    failed_operation_index: Optional[int] = Field(
        default=None, alias="FailedOperationIndex"
    )
    error: Optional[str] = Field(default=None, alias="Error")
    operations: List[TransactionOperationResultModel] = Field(
        default_factory=list, alias="Operations"
    )
    operation_count: int = Field(default=0, alias="OperationCount")
    started_utc: Optional[str] = Field(default=None, alias="StartedUtc")
    completed_utc: Optional[str] = Field(default=None, alias="CompletedUtc")
    duration_ms: float = Field(default=0, alias="DurationMs")
    commit_duration_ms: float = Field(default=0, alias="CommitDurationMs")
    rollback_duration_ms: float = Field(default=0, alias="RollbackDurationMs")
    provider: Optional[str] = Field(default=None, alias="Provider")
    isolation_level: str = Field(default="Default", alias="IsolationLevel")
    isolated_repository: bool = Field(default=False, alias="IsolatedRepository")
    serialized_by_gate: bool = Field(default=False, alias="SerializedByGate")
    retry_count: int = Field(default=0, alias="RetryCount")
    retryable: bool = Field(default=False, alias="Retryable")
    concurrency_conflict: bool = Field(default=False, alias="ConcurrencyConflict")
    provider_error_code: Optional[str] = Field(default=None, alias="ProviderErrorCode")
    model_config = ConfigDict(populate_by_name=True)
