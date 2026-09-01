namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Ollama-compatible generation options.  Only options LiteGraph honors are modeled; others are ignored.
    /// </summary>
    public class OllamaChatOptions
    {
        #region Public-Members

        /// <summary>
        /// Sampling temperature.  Null uses the endpoint default.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; } = null;

        /// <summary>
        /// Maximum tokens to generate.  Null uses the endpoint default.
        /// </summary>
        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OllamaChatOptions()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
