namespace LiteGraph
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Authorization resource type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthorizationResourceTypeEnum
    {
        /// <summary>
        /// Tenant administration and tenant records.
        /// </summary>
        [EnumMember(Value = "Admin")]
        Admin,
        /// <summary>
        /// Graph resources.
        /// </summary>
        [EnumMember(Value = "Graph")]
        Graph,
        /// <summary>
        /// Node resources.
        /// </summary>
        [EnumMember(Value = "Node")]
        Node,
        /// <summary>
        /// Edge resources.
        /// </summary>
        [EnumMember(Value = "Edge")]
        Edge,
        /// <summary>
        /// Label resources.
        /// </summary>
        [EnumMember(Value = "Label")]
        Label,
        /// <summary>
        /// Tag resources.
        /// </summary>
        [EnumMember(Value = "Tag")]
        Tag,
        /// <summary>
        /// Vector resources.
        /// </summary>
        [EnumMember(Value = "Vector")]
        Vector,
        /// <summary>
        /// Native query execution.
        /// </summary>
        [EnumMember(Value = "Query")]
        Query,
        /// <summary>
        /// Graph transaction execution.
        /// </summary>
        [EnumMember(Value = "Transaction")]
        Transaction,
        /// <summary>
        /// Chat resources, including endpoints, threads, turns, feedback, and settings.
        /// </summary>
        [EnumMember(Value = "Chat")]
        Chat
    }
}
