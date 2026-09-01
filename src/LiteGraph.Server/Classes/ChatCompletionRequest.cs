namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Chat completion request.
    /// </summary>
    public class ChatCompletionRequest
    {
        #region Public-Members

        /// <summary>
        /// Thread GUID.  Null creates a new thread.
        /// </summary>
        public Guid? ThreadGUID { get; set; } = null;

        /// <summary>
        /// Graph GUID to bind a newly created thread to.  Ignored when ThreadGUID is supplied.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// User message.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// Stream the response as server-sent events.  Default is false.
        /// </summary>
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Completion endpoint GUID override.  Null uses the tenant default.
        /// </summary>
        public Guid? CompletionEndpointGUID { get; set; } = null;

        /// <summary>
        /// Embedding endpoint GUID override.  Null uses the tenant default.
        /// </summary>
        public Guid? EmbeddingEndpointGUID { get; set; } = null;

        /// <summary>
        /// Sampling temperature override.  Null uses the endpoint default.  Minimum is 0, maximum is 2.
        /// </summary>
        public double? Temperature
        {
            get
            {
                return _Temperature;
            }
            set
            {
                if (value != null && (value.Value < 0 || value.Value > 2)) throw new ArgumentOutOfRangeException(nameof(Temperature));
                _Temperature = value;
            }
        }

        /// <summary>
        /// Maximum output tokens override.  Null uses the endpoint default.  Minimum is 1.
        /// </summary>
        public int? MaxOutputTokens
        {
            get
            {
                return _MaxOutputTokens;
            }
            set
            {
                if (value != null && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(MaxOutputTokens));
                _MaxOutputTokens = value;
            }
        }

        /// <summary>
        /// Tool advertisement override.  Null uses the tenant chat settings.
        /// </summary>
        public bool? EnableTools { get; set; } = null;

        /// <summary>
        /// Retrieval override.  Null uses the tenant chat settings.
        /// </summary>
        public bool? EnableRag { get; set; } = null;

        /// <summary>
        /// Retrieval top-K override.  Null uses the tenant chat settings.  Minimum is 1, maximum is 100.
        /// </summary>
        public int? RagTopK
        {
            get
            {
                return _RagTopK;
            }
            set
            {
                if (value != null && (value.Value < 1 || value.Value > 100)) throw new ArgumentOutOfRangeException(nameof(RagTopK));
                _RagTopK = value;
            }
        }

        /// <summary>
        /// System prompt override.  Null uses the tenant chat settings.
        /// </summary>
        public string SystemPrompt { get; set; } = null;

        #endregion

        #region Private-Members

        private double? _Temperature = null;
        private int? _MaxOutputTokens = null;
        private int? _RagTopK = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatCompletionRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
