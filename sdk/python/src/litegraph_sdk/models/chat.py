import uuid
from datetime import datetime, timezone
from typing import List, Optional

from pydantic import BaseModel, ConfigDict, Field

from ..enums.chat_endpoint_type_enum import ChatEndpointType_Enum
from ..enums.chat_feedback_rating_enum import ChatFeedbackRating_Enum
from ..enums.chat_provider_type_enum import ChatProviderType_Enum


class ChatEndpointModel(BaseModel):
    """
    Chat endpoint. Represents a configured LLM endpoint (completion or embedding).
    The server redacts ApiKey in every response; sending a redacted value back on
    update preserves the stored key.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    name: Optional[str] = Field(default=None, alias="Name")
    endpoint_type: ChatEndpointType_Enum = Field(
        default=ChatEndpointType_Enum.Completion, alias="EndpointType"
    )
    provider: ChatProviderType_Enum = Field(
        default=ChatProviderType_Enum.OpenAI, alias="Provider"
    )
    endpoint: Optional[str] = Field(default=None, alias="Endpoint")
    api_key: Optional[str] = Field(default=None, alias="ApiKey")
    model: Optional[str] = Field(default=None, alias="Model")
    context_window_tokens: int = Field(default=0, alias="ContextWindowTokens")
    active: bool = Field(default=True, alias="Active")
    health_check_enabled: bool = Field(default=True, alias="HealthCheckEnabled")
    health_check_url: Optional[str] = Field(default=None, alias="HealthCheckUrl")
    health_check_use_auth: bool = Field(default=False, alias="HealthCheckUseAuth")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatModelSummaryModel(BaseModel):
    """
    Non-privileged projection of a chat endpoint, exposing only what a chat
    user needs to pick a model: identity, display name, model, provider, type,
    and whether it is the tenant default. Endpoint URLs, keys, and health
    configuration are never included.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    name: Optional[str] = Field(default=None, alias="Name")
    model: Optional[str] = Field(default=None, alias="Model")
    provider: ChatProviderType_Enum = Field(
        default=ChatProviderType_Enum.OpenAI, alias="Provider"
    )
    endpoint_type: ChatEndpointType_Enum = Field(
        default=ChatEndpointType_Enum.Completion, alias="EndpointType"
    )
    is_default: bool = Field(default=False, alias="IsDefault")

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatThreadModel(BaseModel):
    """
    Chat thread. A conversation owned by a user, optionally bound to a graph.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    user_guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="UserGUID")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    title: Optional[str] = Field(default=None, alias="Title")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatTurnModel(BaseModel):
    """
    Chat turn. One user message and assistant response pair, with metrics.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    thread_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="ThreadGUID"
    )
    user_message: Optional[str] = Field(default=None, alias="UserMessage")
    assistant_response: Optional[str] = Field(default=None, alias="AssistantResponse")
    reasoning: Optional[str] = Field(default=None, alias="Reasoning")
    tool_transcript_json: Optional[str] = Field(
        default=None, alias="ToolTranscriptJson"
    )
    telemetry_json: Optional[str] = Field(default=None, alias="TelemetryJson")
    trace_id: Optional[str] = Field(default=None, alias="TraceId")
    completion_endpoint_guid: Optional[str] = Field(
        default=None, alias="CompletionEndpointGUID"
    )
    embedding_endpoint_guid: Optional[str] = Field(
        default=None, alias="EmbeddingEndpointGUID"
    )
    provider: ChatProviderType_Enum = Field(
        default=ChatProviderType_Enum.OpenAI, alias="Provider"
    )
    model: Optional[str] = Field(default=None, alias="Model")
    embedding_duration_ms: Optional[float] = Field(
        default=None, alias="EmbeddingDurationMs"
    )
    retrieval_duration_ms: Optional[float] = Field(
        default=None, alias="RetrievalDurationMs"
    )
    retrieved_chunk_count: int = Field(default=0, alias="RetrievedChunkCount")
    tool_loop_iterations: int = Field(default=0, alias="ToolLoopIterations")
    tool_call_count: int = Field(default=0, alias="ToolCallCount")
    limiter_wait_ms: Optional[float] = Field(default=None, alias="LimiterWaitMs")
    inference_connection_ms: Optional[float] = Field(
        default=None, alias="InferenceConnectionMs"
    )
    time_to_first_token_ms: Optional[float] = Field(
        default=None, alias="TimeToFirstTokenMs"
    )
    time_to_last_token_ms: Optional[float] = Field(
        default=None, alias="TimeToLastTokenMs"
    )
    total_duration_ms: float = Field(default=0, alias="TotalDurationMs")
    prompt_tokens: Optional[int] = Field(default=None, alias="PromptTokens")
    completion_tokens: Optional[int] = Field(default=None, alias="CompletionTokens")
    tokens_per_second_overall: Optional[float] = Field(
        default=None, alias="TokensPerSecondOverall"
    )
    tokens_per_second_generation: Optional[float] = Field(
        default=None, alias="TokensPerSecondGeneration"
    )
    retry_count: int = Field(default=0, alias="RetryCount")
    success: bool = Field(default=True, alias="Success")
    http_status: Optional[int] = Field(default=None, alias="HttpStatus")
    error: Optional[str] = Field(default=None, alias="Error")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatFeedbackModel(BaseModel):
    """
    Chat feedback. A thumbs-up or thumbs-down rating attached to a turn.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    thread_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="ThreadGUID"
    )
    turn_guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="TurnGUID")
    user_guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="UserGUID")
    rating: ChatFeedbackRating_Enum = Field(
        default=ChatFeedbackRating_Enum.ThumbsUp, alias="Rating"
    )
    feedback_text: Optional[str] = Field(default=None, alias="FeedbackText")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatSettingsModel(BaseModel):
    """
    Tenant chat settings. Defaults are returned by the server when no record exists.
    """

    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    default_completion_endpoint_guid: Optional[str] = Field(
        default=None, alias="DefaultCompletionEndpointGUID"
    )
    default_embedding_endpoint_guid: Optional[str] = Field(
        default=None, alias="DefaultEmbeddingEndpointGUID"
    )
    system_prompt: Optional[str] = Field(default=None, alias="SystemPrompt")
    enable_chat: bool = Field(default=True, alias="EnableChat")
    enable_tools: bool = Field(default=True, alias="EnableTools")
    enable_mutation_tools: bool = Field(default=True, alias="EnableMutationTools")
    max_tool_iterations: int = Field(default=5, alias="MaxToolIterations")
    enable_rag: bool = Field(default=True, alias="EnableRag")
    rag_top_k: int = Field(default=8, alias="RagTopK")
    rag_score_threshold: float = Field(default=0.0, alias="RagScoreThreshold")
    history_retention_days: int = Field(default=30, alias="HistoryRetentionDays")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatEndpointHealthSampleModel(BaseModel):
    """
    A single health-check sample for a chat endpoint.
    """

    timestamp_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="TimestampUtc"
    )
    success: bool = Field(default=False, alias="Success")
    duration_ms: float = Field(default=0, alias="DurationMs")

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatEndpointHealthModel(BaseModel):
    """
    Health status for a chat endpoint, including recent check history.
    """

    endpoint_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="EndpointGUID"
    )
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    name: Optional[str] = Field(default=None, alias="Name")
    endpoint_type: ChatEndpointType_Enum = Field(
        default=ChatEndpointType_Enum.Completion, alias="EndpointType"
    )
    monitored: bool = Field(default=False, alias="Monitored")
    healthy: Optional[bool] = Field(default=None, alias="Healthy")
    last_checked_utc: Optional[datetime] = Field(default=None, alias="LastCheckedUtc")
    last_error: Optional[str] = Field(default=None, alias="LastError")
    consecutive_successes: int = Field(default=0, alias="ConsecutiveSuccesses")
    consecutive_failures: int = Field(default=0, alias="ConsecutiveFailures")
    uptime_percentage: Optional[float] = Field(default=None, alias="UptimePercentage")
    check_history: List[ChatEndpointHealthSampleModel] = Field(
        default_factory=list, alias="CheckHistory"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatEndpointTestResultModel(BaseModel):
    """
    Result of a connectivity test against a chat endpoint. Models is omitted for
    providers that do not enumerate models (for example VoyageAI).
    """

    reachable: bool = Field(default=False, alias="Reachable")
    models: Optional[List[str]] = Field(default=None, alias="Models")
    model_exists: Optional[bool] = Field(default=None, alias="ModelExists")
    error: Optional[str] = Field(default=None, alias="Error")
    runtime_ms: float = Field(default=0, alias="RuntimeMs")

    model_config = ConfigDict(
        populate_by_name=True, from_attributes=True, protected_namespaces=()
    )


class ChatCompletionRequestModel(BaseModel):
    """
    Chat completion request. When thread_guid is None a new thread is created
    (bound to graph_guid if given). Endpoint GUIDs default to the tenant chat
    settings defaults.
    """

    thread_guid: Optional[str] = Field(default=None, alias="ThreadGUID")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    message: Optional[str] = Field(default=None, alias="Message")
    stream: bool = Field(default=False, alias="Stream")
    completion_endpoint_guid: Optional[str] = Field(
        default=None, alias="CompletionEndpointGUID"
    )
    embedding_endpoint_guid: Optional[str] = Field(
        default=None, alias="EmbeddingEndpointGUID"
    )
    temperature: Optional[float] = Field(default=None, alias="Temperature")
    context_window_tokens: Optional[int] = Field(default=None, alias="ContextWindowTokens")
    max_output_tokens: Optional[int] = Field(default=None, alias="MaxOutputTokens")
    enable_tools: Optional[bool] = Field(default=None, alias="EnableTools")
    enable_rag: Optional[bool] = Field(default=None, alias="EnableRag")
    rag_top_k: Optional[int] = Field(default=None, alias="RagTopK")
    system_prompt: Optional[str] = Field(default=None, alias="SystemPrompt")

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class ChatCompletionResultModel(BaseModel):
    """
    Chat completion result returned for non-streaming completions and inside the
    streaming usage frame.
    """

    thread_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="ThreadGUID"
    )
    turn_guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="TurnGUID")
    message: Optional[str] = Field(default=None, alias="Message")
    reasoning: Optional[str] = Field(default=None, alias="Reasoning")
    provider: ChatProviderType_Enum = Field(
        default=ChatProviderType_Enum.OpenAI, alias="Provider"
    )
    model: Optional[str] = Field(default=None, alias="Model")
    prompt_tokens: Optional[int] = Field(default=None, alias="PromptTokens")
    completion_tokens: Optional[int] = Field(default=None, alias="CompletionTokens")
    time_to_first_token_ms: Optional[float] = Field(
        default=None, alias="TimeToFirstTokenMs"
    )
    time_to_last_token_ms: Optional[float] = Field(
        default=None, alias="TimeToLastTokenMs"
    )
    total_duration_ms: float = Field(default=0, alias="TotalDurationMs")
    tokens_per_second_overall: Optional[float] = Field(
        default=None, alias="TokensPerSecondOverall"
    )
    tool_call_count: int = Field(default=0, alias="ToolCallCount")
    tool_loop_iterations: int = Field(default=0, alias="ToolLoopIterations")
    retrieved_chunk_count: int = Field(default=0, alias="RetrievedChunkCount")
    retry_count: int = Field(default=0, alias="RetryCount")

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)
