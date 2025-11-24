namespace LiteGraph.McpServer.Classes
{
    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        #region Public-Members

        /// <summary>
        /// Backups directory.
        /// </summary>
        public string BackupsDirectory { get; set; } = "./backups/";

        /// <summary>
        /// Temp directory.
        /// </summary>
        public string TempDirectory { get; set; } = "./temp/";

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageSettings"/> class.
        /// </summary>
        public StorageSettings()
        {
        }

        #endregion
    }
}

