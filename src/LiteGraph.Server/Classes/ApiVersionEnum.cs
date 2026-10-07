namespace LiteGraph.Server.Classes
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// API versions.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ApiVersionEnum>))]
    public enum ApiVersionEnum
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        [EnumMember(Value = "Unknown")]
        Unknown,
        /// <summary>
        /// V1.0.
        /// </summary>
        [EnumMember(Value = "v1.0")]
        V1_0
    }
}