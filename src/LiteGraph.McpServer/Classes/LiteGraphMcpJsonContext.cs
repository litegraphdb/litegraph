namespace LiteGraph.McpServer.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Source-generated JSON metadata for the types the MCP server itself serializes: its settings file.
    /// Everything else the MCP server sends or receives is a LiteGraph SDK type (covered by the SDK's metadata), a tool
    /// schema (a <see cref="System.Text.Json.JsonElement"/> or dictionary), or a value Voltaic already has metadata for.
    /// Registered with the SDK serializer at startup, so the MCP server runs under Native AOT, where reflection-based
    /// System.Text.Json serialization is disabled.
    /// Thread safety: the generated metadata is immutable and safe to use from multiple threads.
    /// </summary>
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]
    [JsonSerializable(typeof(LiteGraphMcpServerSettings))]
    internal partial class LiteGraphMcpJsonContext : JsonSerializerContext
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
