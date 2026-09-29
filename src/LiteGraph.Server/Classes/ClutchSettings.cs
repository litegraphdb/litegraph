namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Connection settings for the Clutch distributed lock service, used when the server runs as part of a cluster.
    /// </summary>
    public class ClutchSettings
    {
        #region Public-Members

        /// <summary>
        /// Base URL of the Clutch server or of the load balancer in front of its nodes.
        /// Default is http://127.0.0.1:8090.  Overridden by LITEGRAPH_CLUTCH_ENDPOINT.
        /// </summary>
        public string Endpoint
        {
            get
            {
                return _Endpoint;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Endpoint));
                if (!Uri.TryCreate(value, UriKind.Absolute, out Uri parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
                    throw new ArgumentException("Clutch endpoint must be an absolute http or https URL.", nameof(Endpoint));
                _Endpoint = value.TrimEnd('/');
            }
        }

        /// <summary>
        /// Clutch application access key.  Secret.  Overridden by LITEGRAPH_CLUTCH_ACCESS_KEY.
        /// Null or empty means not configured; cluster mode refuses to start without it.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// Lock lease duration in milliseconds.  Held locks are renewed at a third of this interval.
        /// A node that stops renewing (for example because it crashed) loses its locks when the lease expires.
        /// Default is 30000.  Minimum is 5000, maximum is 300000.
        /// </summary>
        public int LeaseMs
        {
            get
            {
                return _LeaseMs;
            }
            set
            {
                if (value < 5000 || value > 300000) throw new ArgumentOutOfRangeException(nameof(LeaseMs), "LeaseMs must be between 5000 and 300000.");
                _LeaseMs = value;
            }
        }

        /// <summary>
        /// Timeout for each HTTP request to Clutch, in milliseconds, excluding time spent waiting for a lock.
        /// Default is 10000.  Minimum is 1000, maximum is 120000.
        /// </summary>
        public int RequestTimeoutMs
        {
            get
            {
                return _RequestTimeoutMs;
            }
            set
            {
                if (value < 1000 || value > 120000) throw new ArgumentOutOfRangeException(nameof(RequestTimeoutMs), "RequestTimeoutMs must be between 1000 and 120000.");
                _RequestTimeoutMs = value;
            }
        }

        /// <summary>
        /// How long to keep retrying the first connection to Clutch at startup before giving up, in milliseconds.
        /// Default is 120000.  Minimum is 0 (fail immediately), maximum is 3600000.
        /// </summary>
        public int StartupConnectTimeoutMs
        {
            get
            {
                return _StartupConnectTimeoutMs;
            }
            set
            {
                if (value < 0 || value > 3600000) throw new ArgumentOutOfRangeException(nameof(StartupConnectTimeoutMs), "StartupConnectTimeoutMs must be between 0 and 3600000.");
                _StartupConnectTimeoutMs = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://127.0.0.1:8090";
        private int _LeaseMs = 30000;
        private int _RequestTimeoutMs = 10000;
        private int _StartupConnectTimeoutMs = 120000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClutchSettings()
        {
        }

        #endregion
    }
}
