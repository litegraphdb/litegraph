from typing import Dict, List, Optional

from pydantic import BaseModel, ConfigDict, Field


class GraphAlgorithmNodeResultModel(BaseModel):
    """
    Per-node result produced by a graph algorithm.
    """

    node_guid: Optional[str] = Field(default=None, alias="NodeGUID")
    name: Optional[str] = Field(default=None, alias="Name")
    score: float = Field(default=0.0, alias="Score")
    edges_in: Optional[int] = Field(default=None, alias="EdgesIn")
    edges_out: Optional[int] = Field(default=None, alias="EdgesOut")
    community: Optional[int] = Field(default=None, alias="Community")
    model_config = ConfigDict(populate_by_name=True)


class GraphAlgorithmRequestModel(BaseModel):
    """
    Request to run a graph algorithm over a single graph.
    """

    algorithm_type: str = Field(default="DegreeCentrality", alias="AlgorithmType")
    damping_factor: float = Field(default=0.85, alias="DampingFactor")
    max_iterations: int = Field(default=100, alias="MaxIterations")
    tolerance: float = Field(default=0.000001, alias="Tolerance")
    treat_as_undirected: bool = Field(default=False, alias="TreatAsUndirected")
    max_results: Optional[int] = Field(default=None, alias="MaxResults")
    write_back: bool = Field(default=False, alias="WriteBack")
    write_back_property: Optional[str] = Field(default=None, alias="WriteBackProperty")
    model_config = ConfigDict(populate_by_name=True)


class GraphAlgorithmResultModel(BaseModel):
    """
    Result of a graph algorithm run.
    """

    success: bool = Field(default=True, alias="Success")
    tenant_guid: Optional[str] = Field(default=None, alias="TenantGUID")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    algorithm_type: str = Field(default="DegreeCentrality", alias="AlgorithmType")
    node_count: int = Field(default=0, alias="NodeCount")
    edge_count: int = Field(default=0, alias="EdgeCount")
    iterations: int = Field(default=0, alias="Iterations")
    converged: bool = Field(default=True, alias="Converged")
    community_count: Optional[int] = Field(default=None, alias="CommunityCount")
    compute_ms: float = Field(default=0.0, alias="ComputeMs")
    load_ms: float = Field(default=0.0, alias="LoadMs")
    written_back: bool = Field(default=False, alias="WrittenBack")
    write_back_property: Optional[str] = Field(default=None, alias="WriteBackProperty")
    nodes: List[GraphAlgorithmNodeResultModel] = Field(default_factory=list, alias="Nodes")
    model_config = ConfigDict(populate_by_name=True)


class GraphAlgorithmImportRequestModel(BaseModel):
    """
    Request to import externally computed per-node values back onto graph nodes.
    Values maps node GUID strings to a dictionary of property name to numeric value.
    """

    values: Dict[str, Dict[str, float]] = Field(default_factory=dict, alias="Values")
    model_config = ConfigDict(populate_by_name=True)
