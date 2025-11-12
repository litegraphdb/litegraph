namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Helpers;

    /// <summary>
    /// Admin methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class AdminMethods : IAdminMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;
        private string _BackupDirectory = "./backups/";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Admin methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        /// <param name="backupDirectory">Backup directory.</param>
        public AdminMethods(
            LiteGraphClient client, 
            GraphRepositoryBase repo,
            string backupDirectory)
        {
            if (String.IsNullOrEmpty(backupDirectory)) throw new ArgumentNullException(nameof(backupDirectory));

            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _BackupDirectory = FileHelpers.NormalizeDirectory(backupDirectory);

            if (!Directory.Exists(_BackupDirectory)) Directory.CreateDirectory(_BackupDirectory);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task Backup(string outputFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(outputFilename)) throw new ArgumentNullException(nameof(outputFilename));
            token.ThrowIfCancellationRequested();
            string file = _BackupDirectory + outputFilename;
            await _Repo.Admin.Backup(file, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteBackup(string backupFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            token.ThrowIfCancellationRequested();
            string file = _BackupDirectory + backupFilename;
            if (File.Exists(file))
            {
                await Task.Run(() => File.Delete(file), token).ConfigureAwait(false);
            }
            else
            {
                throw new KeyNotFoundException("Unable to find backup with filename " + backupFilename);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<BackupFile>> BackupReadAll(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!Directory.Exists(_BackupDirectory)) Directory.CreateDirectory(_BackupDirectory);

            List<BackupFile> backupFiles = new List<BackupFile>();
            string[] files = await Task.Run(() => Directory.GetFiles(_BackupDirectory), token).ConfigureAwait(false);

            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();
                FileInfo fileInfo = new FileInfo(file);
                byte[] data = await File.ReadAllBytesAsync(file, token).ConfigureAwait(false);

                backupFiles.Add(new BackupFile
                {
                    Filename = fileInfo.Name,
                    Length = fileInfo.Length,
                    MD5Hash = Convert.ToHexString(HashHelper.MD5Hash(data)),
                    SHA1Hash = Convert.ToHexString(HashHelper.SHA1Hash(data)),
                    SHA256Hash = Convert.ToHexString(HashHelper.SHA256Hash(data)),
                    CreatedUtc = fileInfo.CreationTimeUtc,
                    LastUpdateUtc = fileInfo.LastWriteTimeUtc,
                    LastAccessUtc = fileInfo.LastAccessTimeUtc
                });
            }

            return backupFiles;
        }

        /// <inheritdoc />
        public async Task<BackupFile> BackupRead(string backupFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            token.ThrowIfCancellationRequested();
            if (!Directory.Exists(_BackupDirectory))
            {
                Directory.CreateDirectory(_BackupDirectory);
                throw new KeyNotFoundException("Unable to find backup with filename " + backupFilename);
            }

            string file = _BackupDirectory + backupFilename;
            if (!File.Exists(file))
            {
                throw new KeyNotFoundException("Unable to find backup with filename " + backupFilename);
            }

            FileInfo fi = new FileInfo(file);
            byte[] data = await File.ReadAllBytesAsync(file, token).ConfigureAwait(false);

            return new BackupFile
            {
                Filename = fi.Name,
                Length = fi.Length,
                MD5Hash = Convert.ToHexString(HashHelper.MD5Hash(data)),
                SHA1Hash = Convert.ToHexString(HashHelper.SHA1Hash(data)),
                SHA256Hash = Convert.ToHexString(HashHelper.SHA256Hash(data)),
                CreatedUtc = fi.CreationTimeUtc,
                LastUpdateUtc = fi.LastWriteTimeUtc,
                LastAccessUtc = fi.LastAccessTimeUtc,
                Data = data
            };
        }

        /// <inheritdoc />
        public Task<EnumerationResult<BackupFile>> BackupEnumerate(EnumerationRequest query = null, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<bool> BackupExists(string backupFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            token.ThrowIfCancellationRequested();
            if (!Directory.Exists(_BackupDirectory)) return Task.FromResult(false);
            string file = _BackupDirectory + backupFilename;
            if (!File.Exists(file)) return Task.FromResult(false);
            return Task.FromResult(true);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
