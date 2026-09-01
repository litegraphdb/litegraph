namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible error envelope.
    /// </summary>
    public class OpenAiErrorResponse
    {
        #region Public-Members

        /// <summary>
        /// Error detail.
        /// </summary>
        [JsonPropertyName("error")]
        public OpenAiErrorDetail Error { get; set; } = new OpenAiErrorDetail();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiErrorResponse()
        {

        }

        /// <summary>
        /// Instantiate with a message and error type.
        /// </summary>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="type">Error type, for example invalid_request_error.</param>
        public OpenAiErrorResponse(string message, string type)
        {
            Error = new OpenAiErrorDetail
            {
                Message = message,
                Type = type
            };
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
