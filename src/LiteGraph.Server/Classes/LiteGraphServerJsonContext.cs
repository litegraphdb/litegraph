namespace LiteGraph.Server.Classes
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using LiteGraph;
    using WatsonWebserver.Core;

    /// <summary>
    /// Source-generated JSON metadata for every type LiteGraph.Server serializes or deserializes that the LiteGraph library
    /// does not already cover: settings (including Watson's <see cref="WebserverSettings"/> in <see cref="Settings.Rest"/>),
    /// API request and response types, chat stream events, and the generic results the server builds from library types.
    /// Registered through <see cref="ServerJson.Register"/>. Enums are written as strings. Watson's SSL settings are written by
    /// <see cref="SslSettingsJsonConverter"/> (through <see cref="ServerConverterTypeInfoResolver"/>), so the certificate
    /// object is never serialized.
    /// Thread safety: the generated metadata is immutable and safe to use from multiple threads.
    /// </summary>
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]

    // Server types.
    [JsonSerializable(typeof(ApiErrorResponse))]
    [JsonSerializable(typeof(AuthenticationToken))]
    [JsonSerializable(typeof(AuthorizationAuditSettings))]
    [JsonSerializable(typeof(BackupRequest))]
    [JsonSerializable(typeof(ChatCompletionRequest))]
    [JsonSerializable(typeof(ChatCompletionResult))]
    [JsonSerializable(typeof(ChatEndpointHealth))]
    [JsonSerializable(typeof(ChatEndpointHealthSample))]
    [JsonSerializable(typeof(ChatEndpointPreloadResult))]
    [JsonSerializable(typeof(ChatEndpointTestResult))]
    [JsonSerializable(typeof(ChatModelSummary))]
    [JsonSerializable(typeof(ChatRetrievalChunk))]
    [JsonSerializable(typeof(ChatServerSettings))]
    [JsonSerializable(typeof(ChatStreamContentEvent))]
    [JsonSerializable(typeof(ChatStreamErrorEvent))]
    [JsonSerializable(typeof(ChatStreamRetrievalEvent))]
    [JsonSerializable(typeof(ChatStreamStartedEvent))]
    [JsonSerializable(typeof(ChatStreamToolCallEvent))]
    [JsonSerializable(typeof(ChatStreamToolResultEvent))]
    [JsonSerializable(typeof(ChatStreamUsageEvent))]
    [JsonSerializable(typeof(ChatToolErrorContent))]
    [JsonSerializable(typeof(ChatToolTranscriptEntry))]
    [JsonSerializable(typeof(ClusterJobList))]
    [JsonSerializable(typeof(ClusterJobRun))]
    [JsonSerializable(typeof(ClusterLock))]
    [JsonSerializable(typeof(ClusterLockList))]
    [JsonSerializable(typeof(ClusterNode))]
    [JsonSerializable(typeof(ClusterRestartResult))]
    [JsonSerializable(typeof(ClusterSettings))]
    [JsonSerializable(typeof(ClusterStatus))]
    [JsonSerializable(typeof(ClutchSettings))]
    [JsonSerializable(typeof(DebugSettings), TypeInfoPropertyName = "ServerDebugSettings")]
    [JsonSerializable(typeof(EncryptionSettings))]
    [JsonSerializable(typeof(GenerateEmbeddingsRequest))]
    [JsonSerializable(typeof(GenerateEmbeddingsResult))]
    [JsonSerializable(typeof(HealthChecks))]
    [JsonSerializable(typeof(HealthResponse))]
    [JsonSerializable(typeof(LiteGraphSettings))]
    [JsonSerializable(typeof(ObservabilitySettings))]
    [JsonSerializable(typeof(OllamaChatMessage))]
    [JsonSerializable(typeof(OllamaChatOptions))]
    [JsonSerializable(typeof(OllamaChatRequest))]
    [JsonSerializable(typeof(OllamaChatResponse))]
    [JsonSerializable(typeof(OpenAiChatChoice))]
    [JsonSerializable(typeof(OpenAiChatChunkChoice))]
    [JsonSerializable(typeof(OpenAiChatCompletionChunk))]
    [JsonSerializable(typeof(OpenAiChatCompletionRequest))]
    [JsonSerializable(typeof(OpenAiChatCompletionResponse))]
    [JsonSerializable(typeof(OpenAiChatDelta))]
    [JsonSerializable(typeof(OpenAiChatMessage))]
    [JsonSerializable(typeof(OpenAiChatResponseMessage))]
    [JsonSerializable(typeof(OpenAiChatUsage))]
    [JsonSerializable(typeof(OpenAiErrorDetail))]
    [JsonSerializable(typeof(OpenAiErrorResponse))]
    [JsonSerializable(typeof(OpenAiModelEntry))]
    [JsonSerializable(typeof(OpenAiModelList))]
    [JsonSerializable(typeof(OpenAiStreamOptions))]
    [JsonSerializable(typeof(RedisSettings))]
    [JsonSerializable(typeof(RequestHistorySettings))]
    [JsonSerializable(typeof(RequestHistoryTransactionDiagnostics))]
    [JsonSerializable(typeof(RouteRequest))]
    [JsonSerializable(typeof(RouteResponse))]
    [JsonSerializable(typeof(Settings))]
    [JsonSerializable(typeof(SettingsUpdateResult))]
    [JsonSerializable(typeof(StorageSettings))]
    [JsonSerializable(typeof(TenantOnboardRequest))]
    [JsonSerializable(typeof(TenantOnboardResponse))]
    [JsonSerializable(typeof(TransactionSettings))]

    // Watson settings, embedded in Settings.Rest.
    [JsonSerializable(typeof(WebserverSettings))]
    [JsonSerializable(typeof(WatsonWebserver.Core.Settings.DebugSettings), TypeInfoPropertyName = "WatsonDebugSettings")]

    // Results the server builds from library types.
    [JsonSerializable(typeof(EnumerationResult<AuthorizationRole>))]
    [JsonSerializable(typeof(EnumerationResult<CredentialScopeAssignment>))]
    [JsonSerializable(typeof(EnumerationResult<RequestHistoryEntry>))]
    [JsonSerializable(typeof(EnumerationResult<ChatEndpointHealth>))]
    [JsonSerializable(typeof(EnumerationResult<ChatModelSummary>))]
    [JsonSerializable(typeof(EnumerationResult<UserRoleAssignment>))]
    [JsonSerializable(typeof(EnumerationResult<VectorSearchResult>))]
    [JsonSerializable(typeof(List<RouteDetail>))]
    [JsonSerializable(typeof(List<ChatEndpointHealth>))]
    [JsonSerializable(typeof(List<ChatEndpointHealthSample>))]
    [JsonSerializable(typeof(List<ChatModelSummary>))]
    [JsonSerializable(typeof(List<ClusterNode>))]
    [JsonSerializable(typeof(List<ChatToolTranscriptEntry>))]
    [JsonSerializable(typeof(List<ChatRetrievalChunk>))]
    internal partial class LiteGraphServerJsonContext : JsonSerializerContext
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
