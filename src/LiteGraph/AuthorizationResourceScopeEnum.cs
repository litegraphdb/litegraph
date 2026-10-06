namespace LiteGraph
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Authorization resource scope.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AuthorizationResourceScopeEnum>))]
    public enum AuthorizationResourceScopeEnum
    {
        /// <summary>
        /// Tenant-level scope.
        /// </summary>
        [EnumMember(Value = "Tenant")]
        Tenant,
        /// <summary>
        /// Graph-level scope.
        /// </summary>
        [EnumMember(Value = "Graph")]
        Graph
    }
}
