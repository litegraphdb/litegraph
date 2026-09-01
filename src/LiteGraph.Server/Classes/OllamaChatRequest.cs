namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama-compatible chat request body (/api/chat shape).  Unknown fields are ignored during deserialization.
    /// </summary>
    public class OllamaChatRequest
    {
        #region Public-Members

        /// <summary>
        /// Model selector.  Matches a tenant chat endpoint by Name, Model, or GUID (case-insensitive).
        /// Null or empty selects the tenant default completion endpoint.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// Conversation messages, oldest first.  Roles system, user, and assistant are supported.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = null;

        /// <summary>
        /// Stream the response as newline-delimited JSON.  Null defaults to true, matching Ollama.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; } = null;

        /// <summary>
        /// Generation options.  Null uses endpoint defaults.
        /// </summary>
        [JsonPropertyName("options")]
        public OllamaChatOptions Options { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OllamaChatRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
