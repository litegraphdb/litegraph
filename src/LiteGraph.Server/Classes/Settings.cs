namespace LiteGraph.Server.Classes
{
    using System;
    using WatsonWebserver.Core;

    /// <summary>
    /// Settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// Timestamp from creation, in UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Default REST request timeout, in seconds.
        /// </summary>
        public int RequestTimeoutSeconds
        {
            get
            {
                return _RequestTimeoutSeconds;
            }
            set
            {
                if (value < 1 || value > 3600) throw new ArgumentOutOfRangeException(nameof(RequestTimeoutSeconds));
                _RequestTimeoutSeconds = value;
            }
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Logging));
                _Logging = value;
            }
        }

        /// <summary>
        /// Caching settings.
        /// </summary>
        public CachingSettings Caching
        {
            get
            {
                return _Caching;
            }
            set
            {
                if (value == null) value = new CachingSettings();
                _Caching = value;
            }
        }

        /// <summary>
        /// REST settings.
        /// </summary>
        public WebserverSettings Rest
        {
            get
            {
                return _Rest;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Rest));
                _Rest = value;
            }
        }

        /// <summary>
        /// LiteGraph settings.
        /// </summary>
        public LiteGraphSettings LiteGraph
        {
            get
            {
                return _LiteGraph;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(LiteGraph));
                _LiteGraph = value;
            }
        }

        /// <summary>
        /// Encryption settings.
        /// </summary>
        public EncryptionSettings Encryption
        {
            get
            {
                return _Encryption;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(EncryptionSettings));
                _Encryption = value;
            }
        }

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage
        {
            get
            {
                return _Storage;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Storage));
                _Storage = value;
            }
        }

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings Debug
        {
            get
            {
                return _Debug;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Debug));
                _Debug = value;
            }
        }

        /// <summary>
        /// Request history settings.
        /// </summary>
        public RequestHistorySettings RequestHistory
        {
            get
            {
                return _RequestHistory;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(RequestHistory));
                _RequestHistory = value;
            }
        }

        /// <summary>
        /// Chat settings.
        /// </summary>
        public ChatServerSettings Chat
        {
            get
            {
                return _Chat;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Chat));
                _Chat = value;
            }
        }

        /// <summary>
        /// Observability settings.
        /// </summary>
        public ObservabilitySettings Observability
        {
            get
            {
                return _Observability;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Observability));
                _Observability = value;
            }
        }

        #endregion

        #region Private-Members

        private LoggingSettings _Logging = new LoggingSettings();
        private CachingSettings _Caching = new CachingSettings();
        private WebserverSettings _Rest = new WebserverSettings();
        private LiteGraphSettings _LiteGraph = new LiteGraphSettings();
        private EncryptionSettings _Encryption = new EncryptionSettings();
        private StorageSettings _Storage = new StorageSettings();
        private DebugSettings _Debug = new DebugSettings();
        private RequestHistorySettings _RequestHistory = new RequestHistorySettings();
        private ObservabilitySettings _Observability = new ObservabilitySettings();
        private ChatServerSettings _Chat = new ChatServerSettings();
        private int _RequestTimeoutSeconds = 60;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Settings()
        {
            Rest.Hostname = "localhost";
            Rest.Port = 8701;
            Rest.Ssl.Enable = false;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
