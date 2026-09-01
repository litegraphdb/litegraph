namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible model list.
    /// </summary>
    public class OpenAiModelList
    {
        #region Public-Members

        /// <summary>
        /// Object type.  Always list.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        /// <summary>
        /// Model entries.
        /// </summary>
        [JsonPropertyName("data")]
        public List<OpenAiModelEntry> Data { get; set; } = new List<OpenAiModelEntry>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiModelList()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
