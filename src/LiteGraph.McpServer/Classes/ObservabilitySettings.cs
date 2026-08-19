namespace LiteGraph.McpServer.Classes
{
    using System;

    /// <summary>
    /// Observability settings for the MCP server. Exposes a Prometheus scrape endpoint on its own port
    /// because the underlying MCP transport does not support arbitrary content routes.
    /// </summary>
    public class ObservabilitySettings
    {
        #region Public-Members

        /// <summary>
        /// Enable observability instrumentation.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Enable the Prometheus scrape endpoint.
        /// </summary>
        public bool EnablePrometheus { get; set; } = true;

        /// <summary>
        /// Enable OpenTelemetry-compatible Meter instruments.
        /// </summary>
        public bool EnableOpenTelemetry { get; set; } = true;

        /// <summary>
        /// Hostname the Prometheus metrics listener binds to. Use "*" or "+" to bind all interfaces.
        /// </summary>
        public string Hostname
        {
            get
            {
                return _Hostname;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Hostname));
                _Hostname = value;
            }
        }

        /// <summary>
        /// Port the Prometheus metrics listener binds to. Minimum 0, maximum 65535.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Prometheus metrics endpoint path.
        /// </summary>
        public string MetricsPath
        {
            get
            {
                return _MetricsPath;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(MetricsPath));
                _MetricsPath = value.StartsWith("/") ? value : "/" + value;
            }
        }

        /// <summary>
        /// Service name used by OpenTelemetry-compatible instruments.
        /// </summary>
        public string ServiceName
        {
            get
            {
                return _ServiceName;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ServiceName));
                _ServiceName = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Hostname = "*";
        private int _Port = 8705;
        private string _MetricsPath = "/metrics";
        private string _ServiceName = "LiteGraph.McpServer";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservabilitySettings"/> class.
        /// </summary>
        public ObservabilitySettings()
        {
        }

        #endregion
    }
}
