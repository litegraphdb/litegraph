namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;
    using ExpressionTree;

    /// <summary>
    /// Source-generated JSON metadata for every type the LiteGraph SDK sends or receives.
    /// <see cref="Serializer"/> uses it first, so the SDK works under Native AOT and trimming, where reflection-based
    /// System.Text.Json serialization is disabled.
    /// Applications can add it to their own options, for example
    /// <c>options.TypeInfoResolverChain.Add(LiteGraphSdkJsonContext.Default)</c>.
    /// Enums are written as strings.
    /// Thread safety: the generated metadata is immutable and safe to use from multiple threads.
    /// </summary>
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]

    // Model types.
    [JsonSerializable(typeof(AuthenticationToken))]
    [JsonSerializable(typeof(BackupFile))]
    [JsonSerializable(typeof(BackupRequest))]
    [JsonSerializable(typeof(ChatCompletionRequest))]
    [JsonSerializable(typeof(ChatCompletionResult))]
    [JsonSerializable(typeof(ChatEndpoint))]
    [JsonSerializable(typeof(ChatEndpointHealth))]
    [JsonSerializable(typeof(ChatEndpointHealthSample))]
    [JsonSerializable(typeof(ChatEndpointPreloadResult))]
    [JsonSerializable(typeof(ChatEndpointTestResult))]
    [JsonSerializable(typeof(ChatFeedback))]
    [JsonSerializable(typeof(ChatModelSummary))]
    [JsonSerializable(typeof(ChatSettings))]
    [JsonSerializable(typeof(ChatStreamEvent))]
    [JsonSerializable(typeof(ChatThread))]
    [JsonSerializable(typeof(ChatTurn))]
    [JsonSerializable(typeof(ClusterJobList))]
    [JsonSerializable(typeof(ClusterJobRun))]
    [JsonSerializable(typeof(ClusterLock))]
    [JsonSerializable(typeof(ClusterLockList))]
    [JsonSerializable(typeof(ClusterNode))]
    [JsonSerializable(typeof(ClusterNodeChecks))]
    [JsonSerializable(typeof(ClusterRestartResult))]
    [JsonSerializable(typeof(ClusterStatus))]
    [JsonSerializable(typeof(Credential))]
    [JsonSerializable(typeof(Edge))]
    [JsonSerializable(typeof(EdgeBetween))]
    [JsonSerializable(typeof(EnumerationRequest))]
    [JsonSerializable(typeof(ExistenceRequest))]
    [JsonSerializable(typeof(ExistenceResult))]
    [JsonSerializable(typeof(GenerateEmbeddingsRequest))]
    [JsonSerializable(typeof(GenerateEmbeddingsResult))]
    [JsonSerializable(typeof(Graph))]
    [JsonSerializable(typeof(GraphAlgorithmImportRequest))]
    [JsonSerializable(typeof(GraphAlgorithmImportResult))]
    [JsonSerializable(typeof(GraphAlgorithmNodeResult))]
    [JsonSerializable(typeof(GraphAlgorithmRequest))]
    [JsonSerializable(typeof(GraphAlgorithmResult))]
    [JsonSerializable(typeof(GraphImportRequest))]
    [JsonSerializable(typeof(GraphImportResult))]
    [JsonSerializable(typeof(GraphStatistics))]
    [JsonSerializable(typeof(HealthResponse))]
    [JsonSerializable(typeof(LabelMetadata))]
    [JsonSerializable(typeof(Node))]
    [JsonSerializable(typeof(RequestHistoryDeleteResult))]
    [JsonSerializable(typeof(RequestHistoryDetail))]
    [JsonSerializable(typeof(RequestHistoryEntry))]
    [JsonSerializable(typeof(RequestHistorySearchRequest))]
    [JsonSerializable(typeof(RequestHistorySummary))]
    [JsonSerializable(typeof(RequestHistorySummaryBucket))]
    [JsonSerializable(typeof(RouteDetail))]
    [JsonSerializable(typeof(RouteRequest))]
    [JsonSerializable(typeof(RouteResponse))]
    [JsonSerializable(typeof(RouteResult))]
    [JsonSerializable(typeof(SearchRequest))]
    [JsonSerializable(typeof(SearchResult))]
    [JsonSerializable(typeof(SettingsUpdateResult))]
    [JsonSerializable(typeof(SubgraphExtractionRequest))]
    [JsonSerializable(typeof(TagMetadata))]
    [JsonSerializable(typeof(TenantMetadata))]
    [JsonSerializable(typeof(TenantStatistics))]
    [JsonSerializable(typeof(TransactionOperation))]
    [JsonSerializable(typeof(TransactionOperationResult))]
    [JsonSerializable(typeof(TransactionRequest))]
    [JsonSerializable(typeof(TransactionResult))]
    [JsonSerializable(typeof(UserMaster))]
    [JsonSerializable(typeof(VectorIndexConfiguration))]
    [JsonSerializable(typeof(VectorIndexStatistics))]
    [JsonSerializable(typeof(VectorMetadata))]
    [JsonSerializable(typeof(VectorSearchRequest))]
    [JsonSerializable(typeof(VectorSearchResult))]

    // Enumeration results and lists returned by the server.
    [JsonSerializable(typeof(EnumerationResult<BackupFile>))]
    [JsonSerializable(typeof(EnumerationResult<ChatEndpoint>))]
    [JsonSerializable(typeof(EnumerationResult<ChatEndpointHealth>))]
    [JsonSerializable(typeof(EnumerationResult<ChatFeedback>))]
    [JsonSerializable(typeof(EnumerationResult<ChatModelSummary>))]
    [JsonSerializable(typeof(EnumerationResult<ChatThread>))]
    [JsonSerializable(typeof(EnumerationResult<ChatTurn>))]
    [JsonSerializable(typeof(EnumerationResult<Credential>))]
    [JsonSerializable(typeof(EnumerationResult<Edge>))]
    [JsonSerializable(typeof(EnumerationResult<Graph>))]
    [JsonSerializable(typeof(EnumerationResult<LabelMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<Node>))]
    [JsonSerializable(typeof(EnumerationResult<RequestHistoryEntry>))]
    [JsonSerializable(typeof(EnumerationResult<TagMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<TenantMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<UserMaster>))]
    [JsonSerializable(typeof(EnumerationResult<VectorMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<VectorSearchResult>))]
    [JsonSerializable(typeof(List<ChatEndpointHealthSample>))]
    [JsonSerializable(typeof(List<ClusterJobRun>))]
    [JsonSerializable(typeof(List<ClusterLock>))]
    [JsonSerializable(typeof(List<ClusterNode>))]
    [JsonSerializable(typeof(List<Edge>))]
    [JsonSerializable(typeof(List<EdgeBetween>))]
    [JsonSerializable(typeof(List<Graph>))]
    [JsonSerializable(typeof(List<GraphAlgorithmNodeResult>))]
    [JsonSerializable(typeof(List<LabelMetadata>))]
    [JsonSerializable(typeof(List<Node>))]
    [JsonSerializable(typeof(List<RequestHistorySummaryBucket>))]
    [JsonSerializable(typeof(List<RouteDetail>))]
    [JsonSerializable(typeof(List<TagMetadata>))]
    [JsonSerializable(typeof(List<TransactionOperation>))]
    [JsonSerializable(typeof(List<TransactionOperationResult>))]
    [JsonSerializable(typeof(List<VectorMetadata>))]

    // Collections and untyped data.
    [JsonSerializable(typeof(List<Guid>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(List<float>))]
    [JsonSerializable(typeof(List<object>))]
    [JsonSerializable(typeof(Dictionary<Guid, GraphStatistics>))]
    [JsonSerializable(typeof(Dictionary<Guid, TenantStatistics>))]
    [JsonSerializable(typeof(Dictionary<Guid, Guid>))]
    [JsonSerializable(typeof(Dictionary<string, double>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(object))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonDocument))]
    [JsonSerializable(typeof(JsonNode))]
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(JsonArray))]
    [JsonSerializable(typeof(JsonValue))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(char))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(byte))]
    [JsonSerializable(typeof(sbyte))]
    [JsonSerializable(typeof(short))]
    [JsonSerializable(typeof(ushort))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(decimal))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(float[]))]
    [JsonSerializable(typeof(double[]))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(object[]))]
    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(long[]))]
    [JsonSerializable(typeof(bool[]))]
    [JsonSerializable(typeof(decimal[]))]
    [JsonSerializable(typeof(Guid[]))]
    [JsonSerializable(typeof(DateTime[]))]
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(List<long>))]
    [JsonSerializable(typeof(List<double>))]
    [JsonSerializable(typeof(List<bool>))]
    [JsonSerializable(typeof(List<decimal>))]
    [JsonSerializable(typeof(List<DateTime>))]
    [JsonSerializable(typeof(Expr))]
    public partial class LiteGraphSdkJsonContext : JsonSerializerContext
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
