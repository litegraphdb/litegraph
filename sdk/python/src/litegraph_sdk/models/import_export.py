from typing import Dict, List, Optional

from pydantic import BaseModel, ConfigDict, Field


class SubgraphExtractionRequestModel(BaseModel):
    """Request model for subgraph extraction / JSONL subgraph export."""

    tenant_guid: Optional[str] = Field(None, alias="TenantGUID")
    graph_guid: Optional[str] = Field(None, alias="GraphGUID")
    start_node_guids: Optional[List[str]] = Field(
        default_factory=list, alias="StartNodeGUIDs"
    )
    max_depth: int = Field(2, alias="MaxDepth")
    direction: str = Field("Both", alias="Direction")
    max_nodes: int = Field(0, alias="MaxNodes")
    max_edges: int = Field(0, alias="MaxEdges")
    edge_labels: Optional[List[str]] = Field(default_factory=list, alias="EdgeLabels")
    edge_tags: Optional[Dict[str, str]] = Field(default_factory=dict, alias="EdgeTags")
    edge_filter: Optional[dict] = Field(None, alias="EdgeFilter")
    max_edge_cost: Optional[int] = Field(None, alias="MaxEdgeCost")
    node_labels: Optional[List[str]] = Field(default_factory=list, alias="NodeLabels")
    node_tags: Optional[Dict[str, str]] = Field(default_factory=dict, alias="NodeTags")
    node_filter: Optional[dict] = Field(None, alias="NodeFilter")
    include_data: bool = Field(False, alias="IncludeData")
    include_subordinates: bool = Field(False, alias="IncludeSubordinates")

    model_config = ConfigDict(populate_by_name=True)


class GraphImportResultModel(BaseModel):
    """Result model returned by JSONL import operations."""

    success: bool = Field(False, alias="Success")
    tenant_guid: Optional[str] = Field(None, alias="TenantGUID")
    graph_guid: Optional[str] = Field(None, alias="GraphGUID")
    graphs_created: int = Field(0, alias="GraphsCreated")
    nodes_created: int = Field(0, alias="NodesCreated")
    nodes_updated: int = Field(0, alias="NodesUpdated")
    nodes_skipped: int = Field(0, alias="NodesSkipped")
    edges_created: int = Field(0, alias="EdgesCreated")
    edges_updated: int = Field(0, alias="EdgesUpdated")
    edges_skipped: int = Field(0, alias="EdgesSkipped")
    lines_read: int = Field(0, alias="LinesRead")
    lines_ignored: int = Field(0, alias="LinesIgnored")
    warnings: Optional[List[str]] = Field(default_factory=list, alias="Warnings")
    guid_map: Optional[Dict[str, str]] = Field(default_factory=dict, alias="GuidMap")

    model_config = ConfigDict(populate_by_name=True)
