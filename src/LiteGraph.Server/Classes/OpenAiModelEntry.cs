namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible model entry.
    /// </summary>
    public class OpenAiModelEntry
    {
        #region Public-Members

        /// <summary>
        /// Model identifier.  For LiteGraph this is the chat endpoint name.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Object type.  Always model.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        /// <summary>
        /// Creation time as a Unix epoch in seconds.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; } = 0;

        /// <summary>
        /// Owner label.  Default is litegraph.
        /// </summary>
        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = "litegraph";

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiModelEntry()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
