namespace LiteGraph.Server.Classes
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Wire protocol for graph-scoped compatible chat routes.
    /// </summary>
    public enum ChatCompatProtocolEnum
    {
        /// <summary>
        /// OpenAI chat completions request and response bodies.
        /// </summary>
        [EnumMember(Value = "OpenAI")]
        OpenAI,
        /// <summary>
        /// Ollama /api/chat request and response bodies.
        /// </summary>
        [EnumMember(Value = "Ollama")]
        Ollama
    }
}
