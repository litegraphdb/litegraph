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
                    Console.WriteLine($"BackupsDirectory setter called with: '{value}'");
                    if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(BackupsDirectory));

                    Console.WriteLine($"About to call NormalizeDirectory with: '{value}'");
                    _BackupsDirectory = FileHelpers.NormalizeDirectory(value);
                    Console.WriteLine($"NormalizeDirectory returned: '{_BackupsDirectory}'");

                    if (!Directory.Exists(_BackupsDirectory))
                    {
                        Console.WriteLine($"Creating directory: '{_BackupsDirectory}'");
                        Directory.CreateDirectory(_BackupsDirectory);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in BackupsDirectory setter: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"Value was: '{value}'");
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
