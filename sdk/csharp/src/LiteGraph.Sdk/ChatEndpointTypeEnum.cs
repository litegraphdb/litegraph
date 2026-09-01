namespace LiteGraph.Sdk
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat endpoint type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChatEndpointTypeEnum
    {
        /// <summary>
        /// Embedding endpoint, used to generate vector embeddings.
        /// </summary>
        Embedding,
        /// <summary>
        /// Completion (inference) endpoint, used to generate chat completions.
        /// </summary>
        Completion
    }
}
