namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable logging.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// List of syslog servers.
        /// </summary>
        public List<SyslogServer> Servers
        {
            get
            {
                return _Servers;
            }
            set
            {
                if (value == null) value = new List<SyslogServer>();
                _Servers = value;
            }
        }

        /// <summary>
        /// Log directory.
        /// </summary>
        public string LogDirectory { get; set; } = Constants.LogDirectory;

        /// <summary>
        /// Log filename.
        /// </summary>
        public string LogFilename { get; set; } = Constants.LogFilename;

        /// <summary>
        /// Enable or disable console logging.
        /// </summary>
        public bool ConsoleLogging { get; set; } = true;

        /// <summary>
        /// Enable colors in logging.
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Emit supported operational logs as single-line JSON records.
        /// </summary>
        public bool JsonLogOutput { get; set; } = false;

        /// <summary>
        /// Minimum severity.
        /// </summary>
        public int MinimumSeverity
        {
            get
            {
                return _MinimumSeverity;
            }
            set
            {
                if (value < 0 || value > 7) throw new ArgumentOutOfRangeException(nameof(MinimumSeverity));
                _MinimumSeverity = value;
            }
        }

        /// <summary>
        /// Header to prepend to emitted messages.
        /// </summary>
        public string Header
        {
            get
            {
                return _Header;
            }
            set
            {
                if (!String.IsNullOrEmpty(value)) if (!value.EndsWith(" ")) value += " ";
                _Header = value;
            }
        }

        /// <summary>
        /// Method to invoke to send log messages.
        /// This is a runtime callback and is never serialized.
        /// </summary>
        [JsonIgnore]
        public Action<SeverityEnum, string> Logger { get; set; } = null;

        /// <summary>
        /// Enable or disable logging of queries.
        /// </summary>
        public bool LogQueries { get; set; } = false;

        /// <summary>
        /// Enable or disable logging of results.
        /// </summary>
        public bool LogResults { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Header = "[LiteGraph] ";
        private int _MinimumSeverity = 0;
        private List<SyslogServer> _Servers = new List<SyslogServer>()
        {
            new SyslogServer
            {
                Hostname = "127.0.0.1",
                Port = 514
            }
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings()
        {

        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Emit a log message.
        /// </summary>
        /// <param name="sev">Severity.</param>
        /// <param name="msg">Message.</param>
        public void Log(SeverityEnum sev, string msg)
        {
            if (Enable
                && (int)sev >= MinimumSeverity
                && !String.IsNullOrEmpty(msg))
            {
                Logger?.Invoke(sev, Header + msg);
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
