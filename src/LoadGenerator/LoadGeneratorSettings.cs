namespace LoadGenerator
{
    using System;

    /// <summary>
    /// Settings controlling synthetic load generation.
    /// </summary>
    public class LoadGeneratorSettings
    {
        #region Public-Members

        /// <summary>
        /// Path to the SQLite database file.  Null when targeting PostgreSQL.
        /// </summary>
        public string? SqliteFilename { get; set; } = null;

        /// <summary>
        /// PostgreSQL connection string.  Null when targeting SQLite.
        /// </summary>
        public string? PostgresConnectionString { get; set; } = null;

        /// <summary>
        /// Tenant GUID to seed.  Default is the default tenant, 00000000-0000-0000-0000-000000000000.  Created if absent.
        /// </summary>
        public Guid TenantGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Number of graphs to create.  Default is 3.  Minimum is 1.
        /// </summary>
        public int GraphCount
        {
            get
            {
                return _GraphCount;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(GraphCount), "Graph count must be at least 1.");
                _GraphCount = value;
            }
        }

        /// <summary>
        /// Number of nodes per graph.  Default is 50.  Minimum is 1.
        /// </summary>
        public int NodesPerGraph
        {
            get
            {
                return _NodesPerGraph;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(NodesPerGraph), "Nodes per graph must be at least 1.");
                _NodesPerGraph = value;
            }
        }

        /// <summary>
        /// Connection density, the probability that an ordered node pair receives an edge.
        /// Default is 0.05.  Values outside 0 to 1 are clamped.  A spanning path is always created so no node is orphaned.
        /// </summary>
        public double Density
        {
            get
            {
                return _Density;
            }
            set
            {
                if (value < 0.0) value = 0.0;
                if (value > 1.0) value = 1.0;
                _Density = value;
            }
        }

        /// <summary>
        /// Fraction of nodes that receive a 384-dimension vector.  Default is 0.5.  Values outside 0 to 1 are clamped.
        /// </summary>
        public double VectorFraction
        {
            get
            {
                return _VectorFraction;
            }
            set
            {
                if (value < 0.0) value = 0.0;
                if (value > 1.0) value = 1.0;
                _VectorFraction = value;
            }
        }

        /// <summary>
        /// Number of days into the past over which synthetic activity is spread.  Default is 7.  Minimum is 1.
        /// </summary>
        public int Days
        {
            get
            {
                return _Days;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(Days), "Days must be at least 1.");
                _Days = value;
            }
        }

        /// <summary>
        /// Number of synthetic API request-history entries to create across the window.  Default is 2000.  Minimum is 0.
        /// </summary>
        public int RequestCount
        {
            get
            {
                return _RequestCount;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(RequestCount), "Request count must be zero or greater.");
                _RequestCount = value;
            }
        }

        /// <summary>
        /// Number of chat threads to create.  Default is 6.  Minimum is 0.
        /// </summary>
        public int ChatThreadCount
        {
            get
            {
                return _ChatThreadCount;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(ChatThreadCount), "Chat thread count must be zero or greater.");
                _ChatThreadCount = value;
            }
        }

        /// <summary>
        /// Average number of turns per chat thread.  Default is 4.  Minimum is 1.
        /// </summary>
        public int ChatTurnsAverage
        {
            get
            {
                return _ChatTurnsAverage;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(ChatTurnsAverage), "Average chat turns must be at least 1.");
                _ChatTurnsAverage = value;
            }
        }

        /// <summary>
        /// Random number generator seed.  Null selects a random seed.
        /// </summary>
        public int? Seed { get; set; } = null;

        /// <summary>
        /// True to delete previously generated synthetic data before seeding.
        /// </summary>
        public bool Wipe { get; set; } = false;

        /// <summary>
        /// True to delete previously generated synthetic data and exit without seeding.
        /// </summary>
        public bool WipeOnly { get; set; } = false;

        #endregion

        #region Private-Members

        private int _GraphCount = 3;
        private int _NodesPerGraph = 50;
        private double _Density = 0.05;
        private double _VectorFraction = 0.5;
        private int _Days = 7;
        private int _RequestCount = 2000;
        private int _ChatThreadCount = 6;
        private int _ChatTurnsAverage = 4;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LoadGeneratorSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
