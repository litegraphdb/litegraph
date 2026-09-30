namespace LiteGraph.Sdk
{
    using System.Collections.Generic;

    /// <summary>
    /// A request history entry with its captured headers and bodies, subject to the server's redaction and truncation.
    /// </summary>
    public class RequestHistoryDetail : RequestHistoryEntry
    {
        #region Public-Members

        /// <summary>
        /// Captured request headers.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Captured response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Captured request body, or null.
        /// </summary>
        public string RequestBody { get; set; } = null;

        /// <summary>
        /// Captured response body, or null.
        /// </summary>
        public string ResponseBody { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryDetail()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
