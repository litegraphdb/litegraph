namespace LoadGenerator
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Command-line argument parser for the load generator.
    /// </summary>
    public static class ArgumentParser
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Determine whether the supplied arguments contain a help flag.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>True when help was requested.</returns>
        public static bool IsHelpRequested(string[] args)
        {
            if (args == null) return false;

            foreach (string arg in args)
            {
                if (arg == null) continue;
                if (arg.Equals("/?") || arg.Equals("-?") || arg.Equals("--help") || arg.Equals("-h")) return true;
            }

            return false;
        }

        /// <summary>
        /// Parse command-line arguments into settings.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Parsed settings.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is unknown, malformed, or missing a value.</exception>
        public static LoadGeneratorSettings Parse(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            LoadGeneratorSettings settings = new LoadGeneratorSettings();

            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "--sqlite":
                        settings.SqliteFilename = NextValue(args, ref i, arg);
                        break;
                    case "--postgres":
                        settings.PostgresConnectionString = NextValue(args, ref i, arg);
                        break;
                    case "--tenant":
                        settings.TenantGuid = ParseGuid(NextValue(args, ref i, arg), arg);
                        break;
                    case "--graphs":
                        settings.GraphCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--nodes":
                        settings.NodesPerGraph = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--density":
                        settings.Density = ParseDouble(NextValue(args, ref i, arg), arg);
                        break;
                    case "--vectors":
                        settings.VectorFraction = ParseDouble(NextValue(args, ref i, arg), arg);
                        break;
                    case "--days":
                        settings.Days = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--requests":
                        settings.RequestCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--chat-threads":
                        settings.ChatThreadCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--chat-turns":
                        settings.ChatTurnsAverage = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--seed":
                        settings.Seed = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--wipe":
                        settings.Wipe = true;
                        break;
                    case "--wipe-only":
                        settings.WipeOnly = true;
                        break;
                    default:
                        throw new ArgumentException("Unknown argument '" + arg + "'.  Use --help for usage.");
                }

                i++;
            }

            bool haveSqlite = !String.IsNullOrEmpty(settings.SqliteFilename);
            bool havePostgres = !String.IsNullOrEmpty(settings.PostgresConnectionString);

            if (haveSqlite == havePostgres)
                throw new ArgumentException("Exactly one of --sqlite <file> or --postgres \"<connection string>\" is required.");

            return settings;
        }

        /// <summary>
        /// Print the usage menu to the console.
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("LiteGraph LoadGenerator");
            Console.WriteLine("Seeds a LiteGraph database with realistic synthetic graphs, request history, and chat activity");
            Console.WriteLine("so that the dashboard and Grafana render a fully hydrated, believable system.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  loadgenerator (--sqlite <file> | --postgres \"<connection string>\") [options]");
            Console.WriteLine();
            Console.WriteLine("Database target (exactly one required):");
            Console.WriteLine("  --sqlite <file>          Path to a SQLite database file (created if absent)");
            Console.WriteLine("  --postgres <connstr>     PostgreSQL connection string, e.g.");
            Console.WriteLine("                           \"Host=localhost;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph\"");
            Console.WriteLine();
            Console.WriteLine("Options (defaults in parentheses):");
            Console.WriteLine("  --tenant <guid>          Tenant GUID to seed (00000000-0000-0000-0000-000000000000); created if absent");
            Console.WriteLine("  --graphs <n>             Number of graphs to create (3)");
            Console.WriteLine("  --nodes <n>              Number of nodes per graph (50)");
            Console.WriteLine("  --density <0..1>         Probability an ordered node pair gets an edge (0.05); values are clamped,");
            Console.WriteLine("                           and a spanning path is always created so no node is orphaned");
            Console.WriteLine("  --vectors <0..1>         Fraction of nodes that receive a 384-dim vector, model all-minilm (0.5)");
            Console.WriteLine("  --days <n>               How far back to spread synthetic activity, in days (7)");
            Console.WriteLine("  --requests <n>           Synthetic API request-history entries across the window (2000)");
            Console.WriteLine("  --chat-threads <n>       Number of chat threads to create (6)");
            Console.WriteLine("  --chat-turns <n>         Average number of turns per thread (4)");
            Console.WriteLine("  --seed <n>               Random number generator seed (random)");
            Console.WriteLine("  --wipe                   Delete previously generated synthetic data first, then seed");
            Console.WriteLine("  --wipe-only              Delete previously generated synthetic data and exit");
            Console.WriteLine("  /? -? -h --help          Show this help");
            Console.WriteLine();
            Console.WriteLine("Timestamps are backdated across the window using a diurnal Poisson-style process with random");
            Console.WriteLine("bursts, so activity looks organic rather than uniform.  All synthetic entities are marked with");
            Console.WriteLine("the label 'synthetic', the tag generator=loadgen, users under the loadgen.synthetic email domain,");
            Console.WriteLine("and request-history correlation ID 'loadgen-synthetic' so --wipe can find them.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  loadgenerator --sqlite litegraph.db");
            Console.WriteLine("  loadgenerator --sqlite litegraph.db --graphs 5 --nodes 200 --density 0.02 --days 14 --requests 10000");
            Console.WriteLine("  loadgenerator --postgres \"Host=localhost;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph\"");
            Console.WriteLine("  loadgenerator --sqlite litegraph.db --wipe --seed 42");
            Console.WriteLine("  loadgenerator --sqlite litegraph.db --wipe-only");
            Console.WriteLine();
        }

        #endregion

        #region Private-Methods

        private static string NextValue(string[] args, ref int i, string arg)
        {
            if (i + 1 >= args.Length) throw new ArgumentException("Argument '" + arg + "' requires a value.");
            i++;
            return args[i];
        }

        private static int ParseInt(string value, string arg)
        {
            if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new ArgumentException("Argument '" + arg + "' requires an integer value, received '" + value + "'.");
            return result;
        }

        private static double ParseDouble(string value, string arg)
        {
            if (!Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                throw new ArgumentException("Argument '" + arg + "' requires a numeric value, received '" + value + "'.");
            return result;
        }

        private static Guid ParseGuid(string value, string arg)
        {
            if (!Guid.TryParse(value, out Guid result))
                throw new ArgumentException("Argument '" + arg + "' requires a GUID value, received '" + value + "'.");
            return result;
        }

        #endregion
    }
}
