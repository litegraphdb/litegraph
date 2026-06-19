namespace LiteGraph.GraphRepositories
{
    using System;
    using LiteGraph.GraphRepositories.Postgresql;
    using LiteGraph.GraphRepositories.Sqlite;

    /// <summary>
    /// Creates graph repositories from provider-neutral database settings.
    /// </summary>
    public static class GraphRepositoryFactory
    {
        /// <summary>
        /// Create a graph repository.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <returns>Graph repository.</returns>
        public static GraphRepositoryBase Create(DatabaseSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            switch (settings.Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return new SqliteGraphRepository(settings.Filename, settings.InMemory);

                case DatabaseTypeEnum.Postgresql:
                    return new PostgresqlGraphRepository(settings);

                default:
                    throw new NotSupportedException("Unsupported graph repository database type '" + settings.Type + "'.");
            }
        }
    }
}
