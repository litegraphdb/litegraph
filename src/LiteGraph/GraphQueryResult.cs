namespace LiteGraph
{
    using System.Collections.Generic;
    using LiteGraph.Query;

    /// <summary>
    /// Native LiteGraph graph query result.
    /// </summary>
    public class GraphQueryResult
    {
        #region Public-Members

        /// <summary>
        /// Query language profile.
        /// </summary>
        public string Profile { get; set; } = "LiteGraph Cypher/GQL-inspired";

        /// <summary>
        /// True if the query mutated graph child objects.
        /// </summary>
        public bool Mutated { get; set; } = false;

        /// <summary>
        /// Query execution time in milliseconds.
        /// </summary>
        public double ExecutionTimeMs { get; set; } = 0;

        /// <summary>
        /// Optional execution profile. Populated only when requested.
        /// </summary>
        public GraphQueryExecutionProfile ExecutionProfile { get; set; } = null;

        /// <summary>
        /// Planner or execution warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Query plan summary.
        /// </summary>
        public GraphQueryPlanSummary Plan { get; set; } = null;

        /// <summary>
        /// Algorithm type name when the query is a graph algorithm CALL (for example "PageRank"); null otherwise.
        /// </summary>
        public string AlgorithmType { get; set; } = null;

        /// <summary>
        /// Result rows keyed by return variable.
        /// </summary>
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// Nodes returned by the query.
        /// </summary>
        public List<Node> Nodes { get; set; } = new List<Node>();

        /// <summary>
        /// Edges returned by the query.
        /// </summary>
        public List<Edge> Edges { get; set; } = new List<Edge>();

        /// <summary>
        /// Labels returned by the query.
        /// </summary>
        public List<LabelMetadata> Labels { get; set; } = new List<LabelMetadata>();

        /// <summary>
        /// Tags returned by the query.
        /// </summary>
        public List<TagMetadata> Tags { get; set; } = new List<TagMetadata>();

        /// <summary>
        /// Vectors returned by the query.
        /// </summary>
        public List<VectorMetadata> Vectors { get; set; } = new List<VectorMetadata>();

        /// <summary>
        /// Vector search results returned by the query.
        /// </summary>
        public List<VectorSearchResult> VectorSearchResults { get; set; } = new List<VectorSearchResult>();

        /// <summary>
        /// Number of result rows.
        /// </summary>
        public int RowCount
        {
            get
            {
                return Rows != null ? Rows.Count : 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// Optional native query execution timing profile.
    /// </summary>
    public class GraphQueryExecutionProfile
    {
        /// <summary>
        /// Query parse duration in milliseconds.
        /// </summary>
        public double ParseTimeMs { get; set; } = 0;

        /// <summary>
        /// Query planning duration in milliseconds.
        /// </summary>
        public double PlanTimeMs { get; set; } = 0;

        /// <summary>
        /// Query executor duration in milliseconds.
        /// </summary>
        public double ExecuteTimeMs { get; set; } = 0;

        /// <summary>
        /// REST query authorization duration in milliseconds. This is populated by REST execution only.
        /// </summary>
        public double AuthorizationTimeMs { get; set; } = 0;

        /// <summary>
        /// Repository operation duration observed during query execution in milliseconds.
        /// </summary>
        public double RepositoryTimeMs { get; set; } = 0;

        /// <summary>
        /// Number of repository operations observed during query execution.
        /// </summary>
        public int RepositoryOperationCount { get; set; } = 0;

        /// <summary>
        /// Vector search duration observed during query execution in milliseconds.
        /// </summary>
        public double VectorSearchTimeMs { get; set; } = 0;

        /// <summary>
        /// Number of vector searches observed during query execution.
        /// </summary>
        public int VectorSearchCount { get; set; } = 0;

        /// <summary>
        /// Graph mutation transaction duration observed during query execution in milliseconds.
        /// </summary>
        public double TransactionTimeMs { get; set; } = 0;

        /// <summary>
        /// REST response serialization duration in milliseconds. This is populated by REST execution only.
        /// </summary>
        public double SerializationTimeMs { get; set; } = 0;

        /// <summary>
        /// Total observed query duration in milliseconds.
        /// </summary>
        public double TotalTimeMs { get; set; } = 0;
    }

    /// <summary>
    /// Serializable query plan summary.
    /// </summary>
    public class GraphQueryPlanSummary
    {
        /// <summary>
        /// Query kind.
        /// </summary>
        public GraphQueryKindEnum Kind { get; set; }

        /// <summary>
        /// True if the query mutates graph child objects.
        /// </summary>
        public bool Mutates { get; set; }

        /// <summary>
        /// True if the query uses vector search.
        /// </summary>
        public bool UsesVectorSearch { get; set; }

        /// <summary>
        /// Vector search domain, if the query uses vector search.
        /// </summary>
        public VectorSearchDomainEnum? VectorDomain { get; set; }

        /// <summary>
        /// True if the query has ORDER BY.
        /// </summary>
        public bool HasOrder { get; set; }

        /// <summary>
        /// True if the query has LIMIT.
        /// </summary>
        public bool HasLimit { get; set; }

        /// <summary>
        /// Estimated relative cost.
        /// </summary>
        public int EstimatedCost { get; set; }

        /// <summary>
        /// Repository seed kind.
        /// </summary>
        public GraphQueryPlanSeedKindEnum SeedKind { get; set; }

        /// <summary>
        /// Seed variable, if any.
        /// </summary>
        public string SeedVariable { get; set; }

        /// <summary>
        /// Seed field, if any.
        /// </summary>
        public string SeedField { get; set; }

        /// <summary>
        /// Build a summary from a plan.
        /// </summary>
        /// <param name="plan">Query plan.</param>
        /// <returns>Plan summary.</returns>
        public static GraphQueryPlanSummary FromPlan(GraphQueryPlan plan)
        {
            if (plan == null) return null;

            return new GraphQueryPlanSummary
            {
                Kind = plan.Kind,
                Mutates = plan.Mutates,
                UsesVectorSearch = plan.UsesVectorSearch,
                VectorDomain = plan.Ast?.VectorDomain,
                HasOrder = plan.HasOrder,
                HasLimit = plan.HasLimit,
                EstimatedCost = plan.EstimatedCost,
                SeedKind = plan.SeedKind,
                SeedVariable = plan.SeedVariable,
                SeedField = plan.SeedField
            };
        }
    }
}
