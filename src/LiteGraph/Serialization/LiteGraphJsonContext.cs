namespace LiteGraph.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;
    using ExpressionTree;
    using LiteGraph.Algorithms;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Storage;

    /// <summary>
    /// Source-generated JSON metadata for every type LiteGraph serializes or deserializes.
    /// <see cref="Serializer"/> uses it first, so LiteGraph works under Native AOT and trimming, where reflection-based
    /// System.Text.Json serialization is disabled.
    /// Applications can add it to their own options, for example
    /// <c>options.TypeInfoResolverChain.Add(LiteGraphJsonContext.Default)</c>.
    /// Enums are written as strings.
    /// Thread safety: the generated metadata is immutable and safe to use from multiple threads.
    /// </summary>
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]

    // Model types.
    [JsonSerializable(typeof(AuthenticationToken))]
    [JsonSerializable(typeof(AuthorizationAuditEntry))]
    [JsonSerializable(typeof(AuthorizationAuditSearchRequest))]
    [JsonSerializable(typeof(AuthorizationAuditSearchResult))]
    [JsonSerializable(typeof(AuthorizationEffectiveGrant))]
    [JsonSerializable(typeof(AuthorizationEffectivePermissionsResult))]
    [JsonSerializable(typeof(AuthorizationRole))]
    [JsonSerializable(typeof(AuthorizationRoleSearchRequest))]
    [JsonSerializable(typeof(AuthorizationRoleSearchResult))]
    [JsonSerializable(typeof(BackupFile))]
    [JsonSerializable(typeof(CachingSettings))]
    [JsonSerializable(typeof(ChatEndpoint))]
    [JsonSerializable(typeof(ChatFeedback))]
    [JsonSerializable(typeof(ChatSettings))]
    [JsonSerializable(typeof(ChatThread))]
    [JsonSerializable(typeof(ChatTurn))]
    [JsonSerializable(typeof(Credential))]
    [JsonSerializable(typeof(CredentialScopeAssignment))]
    [JsonSerializable(typeof(CredentialScopeAssignmentSearchRequest))]
    [JsonSerializable(typeof(CredentialScopeAssignmentSearchResult))]
    [JsonSerializable(typeof(DatabaseSettings))]
    [JsonSerializable(typeof(Edge))]
    [JsonSerializable(typeof(EdgeBetween))]
    [JsonSerializable(typeof(EnumerationRequest))]
    [JsonSerializable(typeof(ExistenceRequest))]
    [JsonSerializable(typeof(ExistenceResult))]
    [JsonSerializable(typeof(Graph))]
    [JsonSerializable(typeof(GraphImportRequest))]
    [JsonSerializable(typeof(GraphImportResult))]
    [JsonSerializable(typeof(GraphQueryExecutionProfile))]
    [JsonSerializable(typeof(GraphQueryPlanSummary))]
    [JsonSerializable(typeof(GraphQueryRequest))]
    [JsonSerializable(typeof(GraphQueryResponse))]
    [JsonSerializable(typeof(GraphQueryResult))]
    [JsonSerializable(typeof(GraphStatistics))]
    [JsonSerializable(typeof(JsonlExportMetadata))]
    [JsonSerializable(typeof(JsonlRecord))]
    [JsonSerializable(typeof(LabelMetadata))]
    [JsonSerializable(typeof(LoggingSettings))]
    [JsonSerializable(typeof(Node))]
    [JsonSerializable(typeof(RequestHistoryDetail))]
    [JsonSerializable(typeof(RequestHistoryEntry))]
    [JsonSerializable(typeof(RequestHistorySearchRequest))]
    [JsonSerializable(typeof(RequestHistorySearchResult))]
    [JsonSerializable(typeof(RequestHistorySummary))]
    [JsonSerializable(typeof(RequestHistorySummaryBucket))]
    [JsonSerializable(typeof(RoleDefinition))]
    [JsonSerializable(typeof(RouteDetail))]
    [JsonSerializable(typeof(SearchRequest))]
    [JsonSerializable(typeof(SearchResult))]
    [JsonSerializable(typeof(StorageSettings))]
    [JsonSerializable(typeof(SubgraphExtractionRequest))]
    [JsonSerializable(typeof(SyslogServer))]
    [JsonSerializable(typeof(TagMetadata))]
    [JsonSerializable(typeof(TenantMetadata))]
    [JsonSerializable(typeof(TenantStatistics))]
    [JsonSerializable(typeof(TransactionExecutionOptions))]
    [JsonSerializable(typeof(TransactionOperation))]
    [JsonSerializable(typeof(TransactionOperationResult))]
    [JsonSerializable(typeof(TransactionRequest))]
    [JsonSerializable(typeof(TransactionResult))]
    [JsonSerializable(typeof(UserMaster))]
    [JsonSerializable(typeof(UserRoleAssignment))]
    [JsonSerializable(typeof(UserRoleAssignmentSearchRequest))]
    [JsonSerializable(typeof(UserRoleAssignmentSearchResult))]
    [JsonSerializable(typeof(VectorIndexConfiguration))]
    [JsonSerializable(typeof(VectorMetadata))]
    [JsonSerializable(typeof(VectorScoreResult))]
    [JsonSerializable(typeof(VectorSearchRequest))]
    [JsonSerializable(typeof(VectorSearchResult))]
    [JsonSerializable(typeof(GraphAlgorithmConfiguration))]
    [JsonSerializable(typeof(GraphAlgorithmImportRequest))]
    [JsonSerializable(typeof(GraphAlgorithmNodeResult))]
    [JsonSerializable(typeof(GraphAlgorithmRequest))]
    [JsonSerializable(typeof(GraphAlgorithmResult))]
    [JsonSerializable(typeof(HnswIndexState))]
    [JsonSerializable(typeof(HnswNodeState))]
    [JsonSerializable(typeof(VectorDistanceResult))]
    [JsonSerializable(typeof(VectorIndexEntry))]
    [JsonSerializable(typeof(VectorIndexStatistics))]
    [JsonSerializable(typeof(StorageEntityCounts))]
    [JsonSerializable(typeof(StorageMigrationResult))]
    [JsonSerializable(typeof(StorageVerificationResult))]
    [JsonSerializable(typeof(Expr))]

    // Enumeration results returned by the client.
    [JsonSerializable(typeof(EnumerationResult<BackupFile>))]
    [JsonSerializable(typeof(EnumerationResult<ChatEndpoint>))]
    [JsonSerializable(typeof(EnumerationResult<ChatFeedback>))]
    [JsonSerializable(typeof(EnumerationResult<ChatThread>))]
    [JsonSerializable(typeof(EnumerationResult<ChatTurn>))]
    [JsonSerializable(typeof(EnumerationResult<Credential>))]
    [JsonSerializable(typeof(EnumerationResult<Edge>))]
    [JsonSerializable(typeof(EnumerationResult<Graph>))]
    [JsonSerializable(typeof(EnumerationResult<LabelMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<Node>))]
    [JsonSerializable(typeof(EnumerationResult<TagMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<TenantMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<UserMaster>))]
    [JsonSerializable(typeof(EnumerationResult<VectorMetadata>))]
    [JsonSerializable(typeof(EnumerationResult<RouteDetail>))]

    // Collections stored in columns or exchanged with callers.
    [JsonSerializable(typeof(List<AuthorizationPermissionEnum>))]
    [JsonSerializable(typeof(List<AuthorizationResourceTypeEnum>))]
    [JsonSerializable(typeof(List<Guid>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(List<float>))]
    [JsonSerializable(typeof(List<object>))]
    [JsonSerializable(typeof(List<Dictionary<string, object>>))]
    [JsonSerializable(typeof(Dictionary<Guid, GraphStatistics>))]
    [JsonSerializable(typeof(Dictionary<Guid, TenantStatistics>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    [JsonSerializable(typeof(Dictionary<string, long>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<Node>))]
    [JsonSerializable(typeof(List<Edge>))]
    [JsonSerializable(typeof(List<Graph>))]

    // Untyped data (Graph.Data, Node.Data, Edge.Data, transaction payloads, query values).
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
    public partial class LiteGraphJsonContext : JsonSerializerContext
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
