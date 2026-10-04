namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for Admin operations.
    /// </summary>
    public static class AdminRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers admin tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphTool(
                "admin_backup",
                "Creates a database backup",
                new
                {
                    type = "object",
                    properties = new
                    {
                        outputFilename = new { type = "string", description = "Output filename for the backup" }
                    },
                    required = new[] { "outputFilename" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("outputFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Output filename is required");

                    string? outputFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(outputFilename))
                        throw new ArgumentException("Output filename cannot be empty");

                    sdk.Admin.Backup(outputFilename).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterLiteGraphTool(
                "admin_backups",
                "Lists all backup files. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new string[] { }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    int skip = args.HasValue ? LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0) : 0;
                    return ListBackups(sdk, LiteGraphMcpServerHelpers.GetMaxResults(args), skip);
                });

            server.RegisterLiteGraphTool(
                "admin_backupread",
                "Reads the contents of a backup file",
                new
                {
                    type = "object",
                    properties = new
                    {
                        backupFilename = new { type = "string", description = "Backup filename" }
                    },
                    required = new[] { "backupFilename" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Backup filename is required");

                    string? backupFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(backupFilename))
                        throw new ArgumentException("Backup filename cannot be empty");

                    BackupFile backup = sdk.Admin.ReadBackup(backupFilename).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(backup, true);
                });

            server.RegisterLiteGraphTool(
                "admin_backupexists",
                "Checks if a backup file exists",
                new
                {
                    type = "object",
                    properties = new
                    {
                        backupFilename = new { type = "string", description = "Backup filename" }
                    },
                    required = new[] { "backupFilename" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Backup filename is required");

                    string? backupFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(backupFilename))
                        throw new ArgumentException("Backup filename cannot be empty");

                    bool exists = sdk.Admin.BackupExists(backupFilename).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterLiteGraphTool(
                "admin_backupdelete",
                "Deletes a backup file",
                new
                {
                    type = "object",
                    properties = new
                    {
                        backupFilename = new { type = "string", description = "Backup filename" }
                    },
                    required = new[] { "backupFilename" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Backup filename is required");

                    string? backupFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(backupFilename))
                        throw new ArgumentException("Backup filename cannot be empty");

                    sdk.Admin.DeleteBackup(backupFilename).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterLiteGraphTool(
                "admin_flush",
                "Flushes an in-memory database to disk",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    return FlushDatabase(sdk);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers admin methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("admin_backup", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("outputFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Output filename is required");

                string? outputFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(outputFilename))
                    throw new ArgumentException("Output filename cannot be empty");

                sdk.Admin.Backup(outputFilename).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterLiteGraphMethod("admin_backups", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                int skip = args.HasValue ? LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0) : 0;
                return ListBackups(sdk, LiteGraphMcpServerHelpers.GetMaxResults(args), skip);
            });

            server.RegisterLiteGraphMethod("admin_backupread", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                BackupFile backup = sdk.Admin.ReadBackup(backupFilename).GetAwaiter().GetResult();
                return Serializer.SerializeJson(backup, true);
            });

            server.RegisterLiteGraphMethod("admin_backupexists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                bool exists = sdk.Admin.BackupExists(backupFilename).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterLiteGraphMethod("admin_backupdelete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                sdk.Admin.DeleteBackup(backupFilename).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterLiteGraphMethod("admin_flush", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return FlushDatabase(sdk);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers admin methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("admin_backup", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("outputFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Output filename is required");

                string? outputFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(outputFilename))
                    throw new ArgumentException("Output filename cannot be empty");

                sdk.Admin.Backup(outputFilename).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterLiteGraphMethod("admin_backups", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                int skip = args.HasValue ? LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0) : 0;
                return ListBackups(sdk, LiteGraphMcpServerHelpers.GetMaxResults(args), skip);
            });

            server.RegisterLiteGraphMethod("admin_backupread", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                BackupFile backup = sdk.Admin.ReadBackup(backupFilename).GetAwaiter().GetResult();
                return Serializer.SerializeJson(backup, true);
            });

            server.RegisterLiteGraphMethod("admin_backupexists", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                bool exists = sdk.Admin.BackupExists(backupFilename).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterLiteGraphMethod("admin_backupdelete", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                sdk.Admin.DeleteBackup(backupFilename).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterLiteGraphMethod("admin_flush", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return FlushDatabase(sdk);
            });
        }

        #endregion

        #region Private-Methods

        private static string ListBackups(LiteGraphSdk sdk, int maxResults, int skip)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/backups?max-keys=" + maxResults + "&skip=" + skip);
        }

        private static string FlushDatabase(LiteGraphSdk sdk)
        {
            return LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Post, "/v1.0/flush");
        }

        #endregion
    }
}
