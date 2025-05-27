namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;

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
        IEnumerable<BackupFile> ListBackups();

        /// <summary>
        /// Read the contents of a backup file.
        /// </summary>
        /// <param name="backupFilename">Backup filename.</param>
        /// <returns>File contents.</returns>
        BackupFile ReadBackup(string backupFilename);

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
