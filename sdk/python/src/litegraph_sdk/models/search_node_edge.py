from typing import Dict, List, Optional

from pydantic import BaseModel, ConfigDict, Field

from ..enums.enumeration_order_enum import EnumerationOrder_Enum
from ..models.edge import EdgeModel
from ..models.expression import ExprModel
from ..models.node import NodeModel


class SearchRequest(BaseModel):
    """
    Search request for nodes and edges.
    """

    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    ordering: EnumerationOrder_Enum = Field(
        EnumerationOrder_Enum.CreatedDescending, alias="Ordering"
    )
    expr: Optional[ExprModel] = Field(None, alias="Expr")
    labels: Optional[List] = Field(None, alias="Labels")
    tags: Optional[Dict[str, str]] = Field(default_factory=dict, alias="Tags")
    model_config = ConfigDict(populate_by_name=True)


class SearchResult(BaseModel):
    """
    Search result for nodes.
    """

    nodes: Optional[List[NodeModel]] = Field(None, alias="Nodes")
    model_config = ConfigDict(populate_by_name=True)


class SearchResultEdge(BaseModel):
    """
    Search result for edges.
    """

    edges: Optional[List[EdgeModel]] = Field(None, alias="Edges")
    model_config = ConfigDict(populate_by_name=True)
