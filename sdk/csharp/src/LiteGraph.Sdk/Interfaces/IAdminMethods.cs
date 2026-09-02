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
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing backup files.</returns>
        Task<EnumerationResult<BackupFile>> ListBackups(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default);

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

        /// <summary>
        /// Read the server settings as a JSON string.  Requires system administrator privileges.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Server settings as a JSON string.</returns>
        Task<string> ReadSettings(CancellationToken token = default);

        /// <summary>
        /// Update the server settings.  Requires system administrator privileges.
        /// </summary>
        /// <param name="settingsJson">Full settings object as a JSON string.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Settings update result describing which sections applied live and which require a restart.</returns>
        Task<SettingsUpdateResult> UpdateSettings(string settingsJson, CancellationToken token = default);

        /// <summary>
        /// Request a server restart.  The server exits its process so the container restart policy applies the new settings.
        /// Requires system administrator privileges.  The call returns best-effort; the connection may drop as the server exits.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        Task RestartServer(CancellationToken token = default);
    }
}