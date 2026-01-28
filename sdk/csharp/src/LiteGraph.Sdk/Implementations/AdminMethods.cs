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
        public async Task<List<BackupFile>> ListBackups(CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/backups";
            List<BackupFile> ret = await _Sdk.Get<List<BackupFile>>(url, token).ConfigureAwait(false);
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

        #endregion

        #region Private-Methods

        #endregion
    }
}
