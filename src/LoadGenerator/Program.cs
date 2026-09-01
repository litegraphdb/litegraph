namespace LoadGenerator
{
    using System;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Postgresql;
    using LiteGraph.GraphRepositories.Sqlite;

    /// <summary>
    /// LoadGenerator entry point.  Seeds a LiteGraph database with realistic synthetic activity.
    /// </summary>
    public static class Program
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Zero on success, non-zero on failure.</returns>
        public static async Task<int> Main(string[] args)
        {
            if (ArgumentParser.IsHelpRequested(args))
            {
                ArgumentParser.PrintUsage();
                return 0;
            }

            LoadGeneratorSettings settings;

            try
            {
                settings = ArgumentParser.Parse(args);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                return 1;
            }

            int seed = settings.Seed.HasValue ? settings.Seed.Value : Environment.TickCount;
            Random random = new Random(seed);

            Console.WriteLine("LiteGraph LoadGenerator");
            Console.WriteLine("Target   : " + (!String.IsNullOrEmpty(settings.SqliteFilename) ? ("sqlite " + settings.SqliteFilename) : "postgresql"));
            Console.WriteLine("Tenant   : " + settings.TenantGuid);
            Console.WriteLine("Seed     : " + seed);
            Console.WriteLine();

            try
            {
                GraphRepositoryBase repo = CreateRepository(settings);

                LoggingSettings logging = new LoggingSettings
                {
                    Enable = false,
                    ConsoleLogging = false
                };

                using (LiteGraphClient client = new LiteGraphClient(repo, logging, null, null))
                {
                    client.InitializeRepository();

                    Seeder seeder = new Seeder(client, repo, settings, random);

                    if (settings.Wipe || settings.WipeOnly)
                    {
                        await seeder.WipeAsync().ConfigureAwait(false);
                        if (settings.WipeOnly) return 0;
                        Console.WriteLine();
                    }

                    SeedSummary summary = await seeder.SeedAsync().ConfigureAwait(false);

                    Console.WriteLine();
                    Console.WriteLine(summary.Render());
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed: " + e.Message);
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }

        #endregion

        #region Private-Methods

        private static GraphRepositoryBase CreateRepository(LoadGeneratorSettings settings)
        {
            if (!String.IsNullOrEmpty(settings.SqliteFilename))
            {
                return new SqliteGraphRepository(settings.SqliteFilename, false);
            }

            return new PostgresqlGraphRepository(new DatabaseSettings
            {
                Type = DatabaseTypeEnum.Postgresql,
                ConnectionString = settings.PostgresConnectionString
            });
        }

        #endregion
    }
}
