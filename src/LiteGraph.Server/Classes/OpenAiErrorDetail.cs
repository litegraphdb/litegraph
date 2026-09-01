namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible error detail.
    /// </summary>
    public class OpenAiErrorDetail
    {
        #region Public-Members

        /// <summary>
        /// Human-readable error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = null;

        /// <summary>
        /// Error type, for example invalid_request_error or server_error.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiErrorDetail()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
