namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible streaming options.
    /// </summary>
    public class OpenAiStreamOptions
    {
        #region Public-Members

        /// <summary>
        /// Emit a terminal chunk carrying token usage before the [DONE] sentinel.  Default is false.
        /// </summary>
        [JsonPropertyName("include_usage")]
        public bool IncludeUsage { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiStreamOptions()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
