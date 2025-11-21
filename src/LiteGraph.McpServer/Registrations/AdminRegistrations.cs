namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.Sdk;
    using Voltaic;

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
            server.RegisterTool(
                "admin/backup",
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
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("outputFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Output filename is required");

                    string? outputFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(outputFilename))
                        throw new ArgumentException("Output filename cannot be empty");

                    sdk.Admin.Backup(outputFilename).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "admin/backups",
                "Lists all backup files",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (args) =>
                {
                    List<BackupFile> backups = sdk.Admin.ListBackups().GetAwaiter().GetResult();
                    return Serializer.SerializeJson(backups, true);
                });

            server.RegisterTool(
                "admin/backupread",
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
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Backup filename is required");

                    string? backupFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(backupFilename))
                        throw new ArgumentException("Backup filename cannot be empty");

                    BackupFile backup = sdk.Admin.ReadBackup(backupFilename).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(backup, true);
                });

            server.RegisterTool(
                "admin/backupexists",
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
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Backup filename is required");

                    string? backupFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(backupFilename))
                        throw new ArgumentException("Backup filename cannot be empty");

                    bool exists = sdk.Admin.BackupExists(backupFilename).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "admin/backupdelete",
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
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                        throw new ArgumentException("Backup filename is required");

                    string? backupFilename = filenameProp.GetString();
                    if (string.IsNullOrEmpty(backupFilename))
                        throw new ArgumentException("Backup filename cannot be empty");

                    sdk.Admin.DeleteBackup(backupFilename).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "admin/flush",
                "Flushes an in-memory database to disk",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (args) =>
                {
                    sdk.Admin.FlushDatabase().GetAwaiter().GetResult();
                    return string.Empty;
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
            server.RegisterMethod("admin/backup", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("outputFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Output filename is required");

                string? outputFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(outputFilename))
                    throw new ArgumentException("Output filename cannot be empty");

                sdk.Admin.Backup(outputFilename).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("admin/backups", (args) =>
            {
                List<BackupFile> backups = sdk.Admin.ListBackups().GetAwaiter().GetResult();
                return Serializer.SerializeJson(backups, true);
            });

            server.RegisterMethod("admin/backupread", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                BackupFile backup = sdk.Admin.ReadBackup(backupFilename).GetAwaiter().GetResult();
                return Serializer.SerializeJson(backup, true);
            });

            server.RegisterMethod("admin/backupexists", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                bool exists = sdk.Admin.BackupExists(backupFilename).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("admin/backupdelete", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                sdk.Admin.DeleteBackup(backupFilename).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("admin/flush", (args) =>
            {
                sdk.Admin.FlushDatabase().GetAwaiter().GetResult();
                return string.Empty;
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
            server.RegisterMethod("admin/backup", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("outputFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Output filename is required");

                string? outputFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(outputFilename))
                    throw new ArgumentException("Output filename cannot be empty");

                sdk.Admin.Backup(outputFilename).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("admin/backups", (args) =>
            {
                List<BackupFile> backups = sdk.Admin.ListBackups().GetAwaiter().GetResult();
                return Serializer.SerializeJson(backups, true);
            });

            server.RegisterMethod("admin/backupread", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                BackupFile backup = sdk.Admin.ReadBackup(backupFilename).GetAwaiter().GetResult();
                return Serializer.SerializeJson(backup, true);
            });

            server.RegisterMethod("admin/backupexists", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                bool exists = sdk.Admin.BackupExists(backupFilename).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("admin/backupdelete", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("backupFilename", out JsonElement filenameProp))
                    throw new ArgumentException("Backup filename is required");

                string? backupFilename = filenameProp.GetString();
                if (string.IsNullOrEmpty(backupFilename))
                    throw new ArgumentException("Backup filename cannot be empty");

                sdk.Admin.DeleteBackup(backupFilename).GetAwaiter().GetResult();
                return string.Empty;
            });

            server.RegisterMethod("admin/flush", (args) =>
            {
                sdk.Admin.FlushDatabase().GetAwaiter().GetResult();
                return string.Empty;
            });
        }

        #endregion
    }
}

