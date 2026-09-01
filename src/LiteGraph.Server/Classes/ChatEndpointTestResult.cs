namespace LiteGraph.Server.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Chat endpoint connectivity test result.
    /// </summary>
    public class ChatEndpointTestResult
    {
        #region Public-Members

        /// <summary>
        /// Whether the upstream endpoint responded to the connectivity probe.
        /// </summary>
        public bool Reachable { get; set; } = false;

        /// <summary>
        /// Models advertised by the upstream endpoint.  Null when the provider has no model listing endpoint.
        /// </summary>
        public List<string> Models { get; set; } = null;

        /// <summary>
        /// Whether the endpoint's configured model appears in the model list.  Null when the provider has no model listing endpoint.
        /// </summary>
        public bool? ModelExists { get; set; } = null;

        /// <summary>
        /// Error message.  Null on success.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Probe runtime in milliseconds.
        /// </summary>
        public double RuntimeMs { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatEndpointTestResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
