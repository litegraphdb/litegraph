namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Chat endpoint.  Defines an embedding or completion (inference) endpoint used by the chat feature.
    /// </summary>
    public class ChatEndpoint
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Endpoint type.  Default is Completion.
        /// </summary>
        public ChatEndpointTypeEnum EndpointType { get; set; } = ChatEndpointTypeEnum.Completion;

        /// <summary>
        /// Provider type.  Default is OpenAI, which also covers any OpenAI-compatible server.
        /// Anthropic is valid only for completion endpoints; VoyageAI is valid only for embedding endpoints.
        /// </summary>
        public ChatProviderTypeEnum Provider { get; set; } = ChatProviderTypeEnum.OpenAI;

        /// <summary>
        /// Base URL of the upstream endpoint, for example http://127.0.0.1:11434 or https://api.openai.com.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// API key.  Null when the upstream requires no authentication.
        /// Redacted to its last four characters in API responses; sending a redacted value back on update preserves the stored key.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Model name, for example gpt-4o-mini, gemma3:4b, or voyage-3.5.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Maximum output tokens per completion.  Default is 4096.  Minimum is 1, maximum is 10000000.
        /// </summary>
        public int MaxOutputTokens
        {
            get
            {
                return _MaxOutputTokens;
            }
            set
            {
                if (value < 1 || value > 10000000) throw new ArgumentOutOfRangeException(nameof(MaxOutputTokens));
                _MaxOutputTokens = value;
            }
        }

        /// <summary>
        /// Sampling temperature.  Default is 0.7.  Minimum is 0, maximum is 2.
        /// </summary>
        public double Temperature
        {
            get
            {
                return _Temperature;
            }
            set
            {
                if (value < 0 || value > 2) throw new ArgumentOutOfRangeException(nameof(Temperature));
                _Temperature = value;
            }
        }

        /// <summary>
        /// Request timeout in milliseconds.  Default is 120000.  Minimum is 1000.
        /// </summary>
        public int TimeoutMs
        {
            get
            {
                return _TimeoutMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(TimeoutMs));
                _TimeoutMs = value;
            }
        }

        /// <summary>
        /// Maximum concurrent requests to the upstream endpoint.  Default is 2.  Minimum is 1.
        /// </summary>
        public int MaxConcurrentRequests
        {
            get
            {
                return _MaxConcurrentRequests;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxConcurrentRequests));
                _MaxConcurrentRequests = value;
            }
        }

        /// <summary>
        /// Active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Enable background health checks.  Default is true.
        /// </summary>
        public bool HealthCheckEnabled { get; set; } = true;

        /// <summary>
        /// Health check URL.  When null, the endpoint base URL is probed.
        /// </summary>
        public string HealthCheckUrl { get; set; } = null;

        /// <summary>
        /// Health check HTTP method.  Valid values are GET and HEAD.  Default is GET.
        /// </summary>
        public string HealthCheckMethod
        {
            get
            {
                return _HealthCheckMethod;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(HealthCheckMethod));
                if (!String.Equals(value, "GET", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(value, "HEAD", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Health check method must be GET or HEAD.");
                _HealthCheckMethod = value.ToUpperInvariant();
            }
        }

        /// <summary>
        /// Interval between health checks in milliseconds.  Default is 30000.  Minimum is 1000.
        /// </summary>
        public int HealthCheckIntervalMs
        {
            get
            {
                return _HealthCheckIntervalMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(HealthCheckIntervalMs));
                _HealthCheckIntervalMs = value;
            }
        }

        /// <summary>
        /// Health check timeout in milliseconds.  Default is 10000.  Minimum is 1000.
        /// </summary>
        public int HealthCheckTimeoutMs
        {
            get
            {
                return _HealthCheckTimeoutMs;
            }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(HealthCheckTimeoutMs));
                _HealthCheckTimeoutMs = value;
            }
        }

        /// <summary>
        /// HTTP status code expected from a healthy endpoint.  Default is 200.  Minimum is 100, maximum is 599.
        /// </summary>
        public int HealthCheckExpectedStatusCode
        {
            get
            {
                return _HealthCheckExpectedStatusCode;
            }
            set
            {
                if (value < 100 || value > 599) throw new ArgumentOutOfRangeException(nameof(HealthCheckExpectedStatusCode));
                _HealthCheckExpectedStatusCode = value;
            }
        }

        /// <summary>
        /// Consecutive successful checks required to transition to healthy.  Default is 2.  Minimum is 1.
        /// </summary>
        public int HealthyThreshold
        {
            get
            {
                return _HealthyThreshold;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(HealthyThreshold));
                _HealthyThreshold = value;
            }
        }

        /// <summary>
        /// Consecutive failed checks required to transition to unhealthy.  Default is 2.  Minimum is 1.
        /// </summary>
        public int UnhealthyThreshold
        {
            get
            {
                return _UnhealthyThreshold;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(UnhealthyThreshold));
                _UnhealthyThreshold = value;
            }
        }

        /// <summary>
        /// Send the endpoint's API key with health check probes.  Default is false.
        /// </summary>
        public bool HealthCheckUseAuth { get; set; } = false;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private int _MaxOutputTokens = 4096;
        private double _Temperature = 0.7;
        private int _TimeoutMs = 120000;
        private int _MaxConcurrentRequests = 2;
        private string _HealthCheckMethod = "GET";
        private int _HealthCheckIntervalMs = 30000;
        private int _HealthCheckTimeoutMs = 10000;
        private int _HealthCheckExpectedStatusCode = 200;
        private int _HealthyThreshold = 2;
        private int _UnhealthyThreshold = 2;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatEndpoint()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
