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
        /// Read the server settings file as a JSON string.  Every node sharing the file returns the same settings.
        /// Requires system administrator privileges.
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
        /// Request a restart so saved settings take effect.  In cluster mode every node restarts, one at a time, each after
        /// the previous one reports healthy; on a single node the server exits so the container restart policy restarts it.
        /// Requires system administrator privileges.  Returns null if the connection dropped as a single server exited.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Restart result, or null.</returns>
        Task<ClusterRestartResult> RestartServer(CancellationToken token = default);

        /// <summary>
        /// List the cluster nodes with their state and health, plus the settings and restart counters.  On a single node the
        /// answering server is the only node.  Requires system administrator privileges.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Cluster status.</returns>
        Task<ClusterStatus> ReadClusterNodes(CancellationToken token = default);

        /// <summary>
        /// Request a rolling restart of every cluster node (a restart of the answering server on a single node).
        /// Requires system administrator privileges.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Restart result, or null if the connection dropped as a single server exited.</returns>
        Task<ClusterRestartResult> RestartCluster(CancellationToken token = default);

        /// <summary>
        /// Read one cluster node from the node registry.  Requires system administrator privileges.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Node, or null if it is not in the registry.</returns>
        /// <exception cref="ArgumentNullException">Thrown when nodeId is null or empty.</exception>
        Task<ClusterNode> ReadClusterNode(string nodeId, CancellationToken token = default);

        /// <summary>
        /// List the distributed locks the cluster currently holds in Clutch.  On a single node the list is empty and
        /// ClusterEnabled is false.  Requires system administrator privileges.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Lock list.</returns>
        Task<ClusterLockList> ReadClusterLocks(CancellationToken token = default);

        /// <summary>
        /// List the most recent run of each cluster singleton job.  On a single node the list is empty and ClusterEnabled
        /// is false.  Requires system administrator privileges.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Job list.</returns>
        Task<ClusterJobList> ReadClusterJobs(CancellationToken token = default);

        /// <summary>
        /// Request a restart of one cluster node (on a single node, of the server itself).  The node waits for any other node
        /// that is restarting, then restarts.  Requires system administrator privileges.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Restart result, or null if the connection dropped as a single server exited.  When the server refuses the
        /// request (for example an unknown, offline, or stopped node), Restarting is false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when nodeId is null or empty.</exception>
        Task<ClusterRestartResult> RestartClusterNode(string nodeId, CancellationToken token = default);

        /// <summary>
        /// Remove an Offline or Stopped node from the node registry.  A running node cannot be removed, because it registers
        /// again on its next heartbeat.  Requires system administrator privileges.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown when nodeId is null or empty.</exception>
        Task DeleteClusterNode(string nodeId, CancellationToken token = default);
    }
}