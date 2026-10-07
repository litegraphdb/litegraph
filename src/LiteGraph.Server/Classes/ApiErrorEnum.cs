namespace LiteGraph.Server.Classes
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// API error codes.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ApiErrorEnum>))]
    public enum ApiErrorEnum
    {
        /// <summary>
        /// Authentication failed.
        /// </summary>
        [EnumMember(Value = "AuthenticationFailed")]
        AuthenticationFailed,
        /// <summary>
        /// Authorization failed.
        /// </summary>
        [EnumMember(Value = "AuthorizationFailed")]
        AuthorizationFailed,
        /// <summary>
        /// Bad request.
        /// </summary>
        [EnumMember(Value = "BadRequest")]
        BadRequest,
        /// <summary>
        /// Conflict.
        /// </summary>
        [EnumMember(Value = "Conflict")]
        Conflict,
        /// <summary>
        /// DeserializationError.
        /// </summary>
        [EnumMember(Value = "DeserializationError")]
        DeserializationError,
        /// <summary>
        /// Inactive.
        /// </summary>
        [EnumMember(Value = "Inactive")]
        Inactive,
        /// <summary>
        /// Internal error.
        /// </summary>
        [EnumMember(Value = "InternalError")]
        InternalError,
        /// <summary>
        /// Invalid range.
        /// </summary>
        [EnumMember(Value = "InvalidRange")]
        InvalidRange,
        /// <summary>
        /// In use.
        /// </summary>
        [EnumMember(Value = "InUse")]
        InUse,
        /// <summary>
        /// Not empty.
        /// </summary>
        [EnumMember(Value = "NotEmpty")]
        NotEmpty,
        /// <summary>
        /// Not found.
        /// </summary>
        [EnumMember(Value = "NotFound")]
        NotFound,
        /// <summary>
        /// Request timeout.
        /// </summary>
        [EnumMember(Value = "RequestTimeout")]
        RequestTimeout,
        /// <summary>
        /// Request too large.
        /// </summary>
        [EnumMember(Value = "TooLarge")]
        TooLarge,
        /// <summary>
        /// A service the request depends on is unavailable.
        /// </summary>
        [EnumMember(Value = "Unavailable")]
        Unavailable
    }
}
