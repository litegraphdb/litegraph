namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

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
        public void Backup(string outputFilename)
        {
            if (String.IsNullOrEmpty(outputFilename)) throw new ArgumentNullException(nameof(outputFilename));
            string file = _BackupDirectory + outputFilename;
            _Repo.Admin.Backup(file);
        }

        /// <inheritdoc />
        public void DeleteBackup(string backupFilename)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            string file = _BackupDirectory + backupFilename;
            if (File.Exists(file))
            {
                File.Delete(file);
            }
            else
            {
                throw new KeyNotFoundException("Unable to find backup with filename " + backupFilename);
            }
        }

        /// <inheritdoc />
        public IEnumerable<BackupFile> ListBackups()
        {
            if (!Directory.Exists(_BackupDirectory)) Directory.CreateDirectory(_BackupDirectory);

            foreach (string file in Directory.GetFiles(_BackupDirectory))
            {
                FileInfo fileInfo = new FileInfo(file);
                byte[] data = File.ReadAllBytes(file);

                yield return new BackupFile
                {
                    Filename = fileInfo.Name,
                    Length = fileInfo.Length,
                    MD5Hash = Convert.ToHexString(HashHelper.MD5Hash(data)),
                    SHA1Hash = Convert.ToHexString(HashHelper.SHA1Hash(data)),
                    SHA256Hash = Convert.ToHexString(HashHelper.SHA256Hash(data)),
                    CreatedUtc = fileInfo.CreationTimeUtc,
                    LastUpdateUtc = fileInfo.LastWriteTimeUtc,
                    LastAccessUtc = fileInfo.LastAccessTimeUtc
                };
            }
        }

        /// <inheritdoc />
        public BackupFile ReadBackup(string backupFilename)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
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
            byte[] data = File.ReadAllBytes(file);

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
                Data = File.ReadAllBytes(file)
            };
        }

        /// <inheritdoc />
        public bool BackupExists(string backupFilename)
        {
            if (String.IsNullOrEmpty(backupFilename)) throw new ArgumentNullException(nameof(backupFilename));
            if (!Directory.Exists(_BackupDirectory)) return false;
            string file = _BackupDirectory + backupFilename;
            if (!File.Exists(file)) return false;
            return true;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
