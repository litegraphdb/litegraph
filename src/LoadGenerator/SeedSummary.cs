namespace LoadGenerator
{
    using System;
    using System.Text;

    /// <summary>
    /// Counts of entities created during a seeding run.
    /// </summary>
    public class SeedSummary
    {
        #region Public-Members

        /// <summary>
        /// Inclusive start of the activity window, in UTC.
        /// </summary>
        public DateTime WindowStartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Exclusive end of the activity window, in UTC.
        /// </summary>
        public DateTime WindowEndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of tenants created.
        /// </summary>
        public int TenantsCreated { get; set; } = 0;

        /// <summary>
        /// Number of synthetic users created.
        /// </summary>
        public int UsersCreated { get; set; } = 0;

        /// <summary>
        /// Number of graphs created.
        /// </summary>
        public int GraphsCreated { get; set; } = 0;

        /// <summary>
        /// Number of nodes created.
        /// </summary>
        public int NodesCreated { get; set; } = 0;

        /// <summary>
        /// Number of edges created.
        /// </summary>
        public int EdgesCreated { get; set; } = 0;

        /// <summary>
        /// Number of node vectors created.
        /// </summary>
        public int VectorsCreated { get; set; } = 0;

        /// <summary>
        /// Number of request-history entries created.
        /// </summary>
        public int RequestHistoryCreated { get; set; } = 0;

        /// <summary>
        /// Number of chat threads created.
        /// </summary>
        public int ChatThreadsCreated { get; set; } = 0;

        /// <summary>
        /// Number of chat turns created.
        /// </summary>
        public int ChatTurnsCreated { get; set; } = 0;

        /// <summary>
        /// Number of chat feedback entries created.
        /// </summary>
        public int ChatFeedbackCreated { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SeedSummary()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Render the summary as an aligned console table.
        /// </summary>
        /// <returns>Multi-line table string.</returns>
        public string Render()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("+------------------------+------------+");
            sb.AppendLine("| Entity                 | Created    |");
            sb.AppendLine("+------------------------+------------+");
            AppendRow(sb, "Tenants", TenantsCreated);
            AppendRow(sb, "Users", UsersCreated);
            AppendRow(sb, "Graphs", GraphsCreated);
            AppendRow(sb, "Nodes", NodesCreated);
            AppendRow(sb, "Edges", EdgesCreated);
            AppendRow(sb, "Vectors", VectorsCreated);
            AppendRow(sb, "Request history", RequestHistoryCreated);
            AppendRow(sb, "Chat threads", ChatThreadsCreated);
            AppendRow(sb, "Chat turns", ChatTurnsCreated);
            AppendRow(sb, "Chat feedback", ChatFeedbackCreated);
            sb.AppendLine("+------------------------+------------+");
            sb.AppendLine("Activity window (UTC): " + WindowStartUtc.ToString("yyyy-MM-dd HH:mm") + " to " + WindowEndUtc.ToString("yyyy-MM-dd HH:mm"));
            return sb.ToString();
        }

        #endregion

        #region Private-Methods

        private void AppendRow(StringBuilder sb, string name, int count)
        {
            sb.AppendLine("| " + name.PadRight(22) + " | " + count.ToString().PadLeft(10) + " |");
        }

        #endregion
    }
}
