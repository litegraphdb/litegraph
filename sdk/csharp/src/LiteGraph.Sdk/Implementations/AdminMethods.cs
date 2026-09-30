namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Admin methods.
    /// </summary>
    public class AdminMethods : IAdminMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Admin methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public AdminMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task Backup(string outputFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(outputFilename)) throw new ArgumentNullException(nameof(outputFilename));
            string url = _Sdk.Endpoint + "v1.0/backups";

            BackupRequest req = new BackupRequest
            {
                Filename = outputFilename
            };

            await _Sdk.Post<BackupRequest, object>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<BackupFile>> ListBackups(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            string url = _Sdk.Endpoint + "v1.0/backups?max-keys=" + maxKeys + "&skip=" + skip + "&order=" + order.ToString();
            if (continuationToken != null) url += "&token=" + continuationToken.Value;
            EnumerationResult<BackupFile> ret = await _Sdk.GetEnumeration<BackupFile>(url, token).ConfigureAwait(false);
            return ret;
        }

        /// <inheritdoc />
        public async Task<BackupFile> ReadBackup(string backupFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            string url = _Sdk.Endpoint + "v1.0/backups/" + backupFilename;
            BackupFile ret = await _Sdk.Get<BackupFile>(url, token).ConfigureAwait(false);
            return ret;
        }

        /// <inheritdoc />
        public async Task<bool> BackupExists(string backupFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            string url = _Sdk.Endpoint + "v1.0/backups/" + backupFilename;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteBackup(string backupFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            string url = _Sdk.Endpoint + "v1.0/backups/" + backupFilename;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task FlushDatabase(CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/flush";
            await _Sdk.PostRaw(url, null, "application/octet-stream", token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> ReadSettings(CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/settings";
            byte[] bytes = await _Sdk.Get(url, token).ConfigureAwait(false);
            if (bytes != null && bytes.Length > 0) return Encoding.UTF8.GetString(bytes);
            return null;
        }

        /// <inheritdoc />
        public async Task<SettingsUpdateResult> UpdateSettings(string settingsJson, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(settingsJson)) throw new ArgumentNullException(nameof(settingsJson));
            string url = _Sdk.Endpoint + "v1.0/settings";
            byte[] body = Encoding.UTF8.GetBytes(settingsJson);
            byte[] bytes = await _Sdk.PutStreamingBytes(url, body, "application/json", token).ConfigureAwait(false);
            if (bytes != null && bytes.Length > 0) return Serializer.DeserializeJson<SettingsUpdateResult>(Encoding.UTF8.GetString(bytes));
            return null;
        }

        /// <inheritdoc />
        public Task<ClusterRestartResult> RestartServer(CancellationToken token = default)
        {
            return RequestRestart(_Sdk.Endpoint + "v1.0/settings/restart", token);
        }

        /// <inheritdoc />
        public async Task<ClusterStatus> ReadClusterNodes(CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/cluster/nodes";
            return await _Sdk.Get<ClusterStatus>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<ClusterRestartResult> RestartCluster(CancellationToken token = default)
        {
            return RequestRestart(_Sdk.Endpoint + "v1.0/cluster/restart", token);
        }

        /// <inheritdoc />
        public async Task<ClusterNode> ReadClusterNode(string nodeId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(nodeId)) throw new ArgumentNullException(nameof(nodeId));
            string url = _Sdk.Endpoint + "v1.0/cluster/nodes/" + Uri.EscapeDataString(nodeId);
            return await _Sdk.Get<ClusterNode>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<ClusterRestartResult> RestartClusterNode(string nodeId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(nodeId)) throw new ArgumentNullException(nameof(nodeId));
            return RequestRestart(_Sdk.Endpoint + "v1.0/cluster/nodes/" + Uri.EscapeDataString(nodeId) + "/restart", token);
        }

        /// <inheritdoc />
        public async Task DeleteClusterNode(string nodeId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(nodeId)) throw new ArgumentNullException(nameof(nodeId));
            string url = _Sdk.Endpoint + "v1.0/cluster/nodes/" + Uri.EscapeDataString(nodeId);
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task<ClusterRestartResult> RequestRestart(string url, CancellationToken token)
        {
            try
            {
                byte[] bytes = await _Sdk.PostStreamingBytes(url, Encoding.UTF8.GetBytes("{\"confirm\":true}"), "application/json", token).ConfigureAwait(false);
                if (bytes != null && bytes.Length > 0) return Serializer.DeserializeJson<ClusterRestartResult>(Encoding.UTF8.GetString(bytes));
                return null;
            }
            catch (Exception e) when (!(e is OperationCanceledException && token.IsCancellationRequested))
            {
                // A single server may drop the connection as it exits; this is expected.
                return null;
            }
        }

        #endregion
    }
}
