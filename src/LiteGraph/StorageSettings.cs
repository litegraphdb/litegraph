namespace LiteGraph
{
    using LiteGraph.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        #region Public-Members

        /// <summary>
        /// Backups directory.
        /// </summary>
        public string BackupsDirectory
        {
            get
            {
                return _BackupsDirectory;
            }
            set
            {
                try
                {
                    if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(BackupsDirectory));

                    _BackupsDirectory = FileHelpers.NormalizeDirectory(value);

                    if (!Directory.Exists(_BackupsDirectory))
                    {
                        Directory.CreateDirectory(_BackupsDirectory);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        #endregion

        #region Private-Members

        private string _BackupsDirectory = "./backups/";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public StorageSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
