namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Removes the directories that server settings samples create in the current directory. Some settings setters create
    /// the directory they are given (StorageSettings.BackupsDirectory, for example), so serializing and deserializing sample
    /// settings would otherwise leave empty folders behind.
    /// </summary>
    internal sealed class AotServerDirectoryGuard : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static readonly string[] _Candidates = new string[] { "s-backupsdirectory", "s-tempdirectory", "s-logdirectory", "backups", "temp", "logs" };
        private readonly List<string> _Missing = new List<string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Record which candidate directories do not exist yet.
        /// </summary>
        public AotServerDirectoryGuard()
        {
            foreach (string candidate in _Candidates)
            {
                if (!Directory.Exists(candidate)) _Missing.Add(candidate);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Delete the candidate directories created since construction, if they are empty.
        /// </summary>
        public void Dispose()
        {
            foreach (string candidate in _Missing)
            {
                try
                {
                    if (Directory.Exists(candidate) && Directory.GetFileSystemEntries(candidate).Length == 0) Directory.Delete(candidate);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
