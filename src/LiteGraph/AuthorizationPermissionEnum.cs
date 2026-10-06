namespace LiteGraph
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Authorization permission.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AuthorizationPermissionEnum>))]
    public enum AuthorizationPermissionEnum
    {
        /// <summary>
        /// Read resources.
        /// </summary>
        [EnumMember(Value = "Read")]
        Read,
        /// <summary>
        /// Create or update resources.
        /// </summary>
        [EnumMember(Value = "Write")]
        Write,
        /// <summary>
        /// Delete resources.
        /// </summary>
        [EnumMember(Value = "Delete")]
        Delete,
        /// <summary>
        /// Administer resources.
        /// </summary>
        [EnumMember(Value = "Admin")]
        Admin
    }
}
