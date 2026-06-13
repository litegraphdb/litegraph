export class GraphQueryExecutionProfile {
    constructor(profile?: {});
    ParseTimeMs: any;
    PlanTimeMs: any;
    ExecuteTimeMs: any;
    AuthorizationTimeMs: any;
    RepositoryTimeMs: any;
    RepositoryOperationCount: any;
    VectorSearchTimeMs: any;
    VectorSearchCount: any;
    TransactionTimeMs: any;
    SerializationTimeMs: any;
    TotalTimeMs: any;
}
export class GraphQueryPlanSummary {
    constructor(plan?: {});
    Kind: any;
    Mutates: any;
    UsesVectorSearch: any;
    VectorDomain: any;
    HasOrder: any;
    HasLimit: any;
    EstimatedCost: any;
    SeedKind: any;
    SeedVariable: any;
    SeedField: any;
}
export default class GraphQueryResult {
    constructor(result?: {});
    Profile: any;
    Mutated: any;
    ExecutionTimeMs: any;
    ExecutionProfile: GraphQueryExecutionProfile;
    Warnings: any;
    Plan: GraphQueryPlanSummary;
    Rows: any;
    Nodes: any;
    Edges: any;
    Labels: any;
    Tags: any;
    Vectors: any;
    VectorSearchResults: any;
    RowCount: any;
}
