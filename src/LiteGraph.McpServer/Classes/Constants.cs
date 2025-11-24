namespace LiteGraph.McpServer.Classes
{
    using System;

    /// <summary>
    /// Constants.
    /// </summary>
    internal static class Constants
    {
        #region General

        /// <summary>
        /// Software version.
        /// </summary>
        public static string Version = "v1.0.0";

        /// <summary>
        /// Logo.
        /// </summary>
        public static string Logo =
            @"  _ _ _                          _     " + Environment.NewLine +
            @" | (_) |_ ___ __ _ _ _ __ _ _ __| |_   " + Environment.NewLine +
            @" | | |  _/ -_) _` | '_/ _` | '_ \ ' \  " + Environment.NewLine +
            @" |_|_|\__\___\__, |_| \__,_| .__/_||_| " + Environment.NewLine +
            @"             |___/         |_|         " + Environment.NewLine;

        /// <summary>
        /// Product name.
        /// </summary>
        public static string ProductName = " LiteGraph MCP Server";

        /// <summary>
        /// Copyright.
        /// </summary>
        public static string Copyright = " (c)2025 Joel Christner";

        /// <summary>
        /// Settings file path.
        /// </summary>
        public static string SettingsFile = "./litegraph.json";

        #endregion

        #region Environment-Variables

        /// <summary>
        /// Environment variable for LiteGraph endpoint.
        /// </summary>
        public static string LiteGraphEndpointEnvironmentVariable = "LITEGRAPH_ENDPOINT";

        /// <summary>
        /// Environment variable for LiteGraph API key.
        /// </summary>
        public static string LiteGraphApiKeyEnvironmentVariable = "LITEGRAPH_API_KEY";

        /// <summary>
        /// Environment variable for MCP HTTP hostname.
        /// </summary>
        public static string McpHttpHostnameEnvironmentVariable = "MCP_HTTP_HOSTNAME";

        /// <summary>
        /// Environment variable for MCP HTTP port.
        /// </summary>
        public static string McpHttpPortEnvironmentVariable = "MCP_HTTP_PORT";

        /// <summary>
        /// Environment variable for MCP TCP address.
        /// </summary>
        public static string McpTcpAddressEnvironmentVariable = "MCP_TCP_ADDRESS";

        /// <summary>
        /// Environment variable for MCP TCP port.
        /// </summary>
        public static string McpTcpPortEnvironmentVariable = "MCP_TCP_PORT";

        /// <summary>
        /// Environment variable for MCP WebSocket hostname.
        /// </summary>
        public static string McpWebSocketHostnameEnvironmentVariable = "MCP_WS_HOSTNAME";

        /// <summary>
        /// Environment variable for MCP WebSocket port.
        /// </summary>
        public static string McpWebSocketPortEnvironmentVariable = "MCP_WS_PORT";

        /// <summary>
        /// Environment variable for console logging.
        /// </summary>
        public static string ConsoleLoggingEnvironmentVariable = "MCP_CONSOLE_LOGGING";

        #endregion
    }
}

