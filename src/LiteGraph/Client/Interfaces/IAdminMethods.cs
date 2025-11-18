namespace LiteGraph.Client.Interfaces
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for admin methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IAdminMethods
    {
        /// <summary>
        /// Backup request.
        /// </summary>
        /// <param name="outputFilename">Output filename.</param>
        /// <param name="token">Cancellation token.</param>
        Task Backup(string outputFilename, CancellationToken token = default);

        /// <summary>
        /// List backups request.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of backup files.</returns>
        IAsyncEnumerable<BackupFile> BackupReadAll(CancellationToken token = default);

        /// <summary>
        /// Read the contents of a backup file.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>File contents.</returns>
        Task<BackupFile> BackupRead(string backupFilename, CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<BackupFile>> BackupEnumerate(EnumerationRequest query = null, CancellationToken token = default);

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
    }
}
