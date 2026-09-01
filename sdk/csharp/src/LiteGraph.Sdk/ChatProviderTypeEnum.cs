namespace LiteGraph.Sdk
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat provider type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChatProviderTypeEnum
    {
        /// <summary>
        /// OpenAI, or any OpenAI-compatible server (for example vLLM or LM Studio).
        /// </summary>
        OpenAI,
        /// <summary>
        /// Ollama.
        /// </summary>
        Ollama,
        /// <summary>
        /// Google Gemini.
        /// </summary>
        Gemini,
        /// <summary>
        /// Anthropic.  Completion endpoints only; Anthropic has no embeddings API.
        /// </summary>
        Anthropic,
        /// <summary>
        /// VoyageAI.  Embedding endpoints only; VoyageAI has no completions API.
        /// </summary>
        VoyageAI
    }
}
