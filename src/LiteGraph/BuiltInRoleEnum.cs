namespace LiteGraph
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Built-in role.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BuiltInRoleEnum
    {
        /// <summary>
        /// Tenant administrator.
        /// </summary>
        [EnumMember(Value = "TenantAdmin")]
        TenantAdmin,
        /// <summary>
        /// Graph administrator.
        /// </summary>
        [EnumMember(Value = "GraphAdmin")]
        GraphAdmin,
        /// <summary>
        /// Graph editor.
        /// </summary>
        [EnumMember(Value = "Editor")]
        Editor,
        /// <summary>
        /// Graph viewer.
        /// </summary>
        [EnumMember(Value = "Viewer")]
        Viewer,
        /// <summary>
        /// Chat administrator.
        /// </summary>
        [EnumMember(Value = "ChatAdmin")]
        ChatAdmin,
        /// <summary>
        /// Custom role.
        /// </summary>
        [EnumMember(Value = "Custom")]
        Custom
    }
}
