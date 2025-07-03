namespace LiteGraph.Client.Interfaces
{
    using System.Collections.Generic;
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
        void Backup(string outputFilename);

        /// <summary>
        /// List backups request.
        /// </summary>
        /// <returns>Enumerable of backup files.</returns>
        IEnumerable<BackupFile> BackupReadAll();

        /// <summary>
        /// Read the contents of a backup file.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <returns>File contents.</returns>
        BackupFile BackupRead(string backupFilename);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<BackupFile> BackupEnumerate(EnumerationRequest query = null);

        /// <summary>
        /// Check if a backup file exists.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <returns>True if exists.</returns>
        bool BackupExists(string backupFilename);

        /// <summary>
        /// Delete a backup file.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        void DeleteBackup(string backupFilename);
    }
}
