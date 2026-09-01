namespace LiteGraph.Server.Services.Chat
{
    using System;
    using System.Text.Json;
    using LiteGraph.Server.Classes;

    /// <summary>
    /// One tool advertised to the model by the chat orchestrator.
    /// Names, descriptions, and schemas mirror the MCP server's tool catalog so the two surfaces stay aligned.
    /// </summary>
    public class ChatToolDefinition
    {
        #region Public-Members

        /// <summary>
        /// Tool name, identical to the MCP tool name (for example graph/search).
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Human-readable description shown to the model.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// JSON schema for the tool's arguments, as an anonymous object graph.
        /// </summary>
        public object Schema { get; set; } = null;

        /// <summary>
        /// Request type used for authorization of the dispatched call.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; } = RequestTypeEnum.Unknown;

        /// <summary>
        /// Whether the tool mutates data.  Mutation tools are advertised only when the tenant opts in.
        /// </summary>
        public bool Mutation { get; set; } = false;

        /// <summary>
        /// Whether the dispatcher must embed the tool's text argument before executing (vector search).
        /// </summary>
        public bool RequiresEmbedding { get; set; } = false;

        /// <summary>
        /// Populates a synthetic request context from the model-supplied arguments.
        /// The dispatcher forces tenant scope after binding; binders never set TenantGUID.
        /// </summary>
        public Action<JsonElement?, RequestContext> Bind { get; set; } = null;

        /// <summary>
        /// The agnostic handler executed for this tool.
        /// </summary>
        public Func<RequestContext, System.Threading.CancellationToken, System.Threading.Tasks.Task<ResponseContext>> Handler { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatToolDefinition()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
