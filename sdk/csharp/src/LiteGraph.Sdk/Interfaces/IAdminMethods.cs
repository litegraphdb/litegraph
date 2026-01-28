namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;

    /// <summary>
    /// Interface for admin methods.
    /// </summary>
    public interface IAdminMethods
    {
        /// <summary>
        /// Database backup request.
        /// </summary>
        /// <param name="outputFilename">Output filename.</param>
        /// <param name="token">Cancellation token.</param>
        Task Backup(string outputFilename, CancellationToken token = default);

        /// <summary>
        /// List backups request.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumerable of backup files.</returns>
        Task<List<BackupFile>> ListBackups(CancellationToken token = default);

        /// <summary>
        /// Read the contents of a backup file.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>File contents.</returns>
        Task<BackupFile> ReadBackup(string backupFilename, CancellationToken token = default);

        /// <summary>
        /// Check if a backup file exists.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> BackupExists(string backupFilename, CancellationToken token = default);

        /// <summary>
        /// Delete a backup file.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteBackup(string backupFilename, CancellationToken token = default);

        /// <summary>
        /// Flush an in-memory database to disk.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        Task FlushDatabase(CancellationToken token = default);
    }
}