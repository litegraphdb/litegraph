namespace LiteGraph.Server.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of a server settings update, describing which sections applied live and which require a restart.
    /// </summary>
    public class SettingsUpdateResult
    {
        #region Public-Members

        /// <summary>
        /// True if the settings were validated and written to disk successfully.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Names of the settings sections that were applied to the running server without a restart.
        /// </summary>
        public List<string> AppliedLive { get; set; } = new List<string>();

        /// <summary>
        /// Names of the settings sections whose changes require a server restart to take effect.
        /// </summary>
        public List<string> RestartRequired { get; set; } = new List<string>();

        /// <summary>
        /// Human-readable message describing the outcome.
        /// </summary>
        public string Message { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SettingsUpdateResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
