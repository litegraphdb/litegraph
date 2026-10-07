namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Text.Json;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration helpers that wrap LiteGraph MCP handlers so any exception they raise is translated by
    /// <see cref="LiteGraphMcpErrors"/> into a specific MCP protocol error rather than a generic internal error.
    /// Methods registered on the TCP and WebSocket servers return their value as a tool call result
    /// (<c>{"content":[{"type":"text","text":...}]}</c>), the same shape <c>tools/call</c> returns over HTTP,
    /// because every JSON-RPC result must be a JSON object.
    /// Registration is not thread-safe; register tools before starting the servers.
    /// </summary>
    public static class LiteGraphMcpRegistrationExtensions
    {
        #region Public-Members

        #endregion

        #region Private-Members

        // Default options with Voltaic's metadata (and reflection only where reflection-based serialization is enabled),
        // so method results are written as before and also under Native AOT.
        private static readonly JsonSerializerOptions _ResultJsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = VoltaicJson.TypeInfoResolver
        };

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Register a LiteGraph tool on the HTTP server with exception translation.
        /// </summary>
        /// <param name="server">HTTP server; must not be null.</param>
        /// <param name="name">Tool name.</param>
        /// <param name="description">Tool description.</param>
        /// <param name="inputSchema">JSON schema for the tool arguments.</param>
        /// <param name="handler">Tool handler; must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when server or handler is null.</exception>
        public static void RegisterLiteGraphTool(this McpHttpServer server, string name, string description, object inputSchema, Func<RpcParameters?, object> handler)
        {
            ArgumentNullException.ThrowIfNull(server);
            server.RegisterTool(name, description, inputSchema, Wrap(handler));
        }

        /// <summary>
        /// Register a LiteGraph method on the TCP server with exception translation.
        /// </summary>
        /// <param name="server">TCP server; must not be null.</param>
        /// <param name="name">Method name.</param>
        /// <param name="handler">Method handler; must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when server or handler is null.</exception>
        public static void RegisterLiteGraphMethod(this McpTcpServer server, string name, Func<RpcParameters?, object> handler)
        {
            ArgumentNullException.ThrowIfNull(server);
            server.RegisterMethod(name, WrapMethod(handler));
        }

        /// <summary>
        /// Register a LiteGraph method on the WebSocket server with exception translation.
        /// </summary>
        /// <param name="server">WebSocket server; must not be null.</param>
        /// <param name="name">Method name.</param>
        /// <param name="handler">Method handler; must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when server or handler is null.</exception>
        public static void RegisterLiteGraphMethod(this McpWebsocketsServer server, string name, Func<RpcParameters?, object> handler)
        {
            ArgumentNullException.ThrowIfNull(server);
            server.RegisterMethod(name, WrapMethod(handler));
        }

        #endregion

        #region Private-Methods

        private static Func<RpcParameters?, object> Wrap(Func<RpcParameters?, object> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            return args =>
            {
                try
                {
                    return handler(args);
                }
                catch (Exception e) when (e is not McpProtocolException)
                {
                    throw LiteGraphMcpErrors.Translate(e);
                }
            };
        }

        private static Func<RpcParameters?, object> WrapMethod(Func<RpcParameters?, object> handler)
        {
            Func<RpcParameters?, object> wrapped = Wrap(handler);

            return args =>
            {
                object result = wrapped(args);
                if (result is McpToolCallResult toolResult) return toolResult;
                if (result is string text) return McpToolCallResult.FromText(text);
                return McpToolCallResult.FromText(JsonSerializer.Serialize(result, _ResultJsonOptions.GetTypeInfo(typeof(object))));
            };
        }

        #endregion
    }
}
