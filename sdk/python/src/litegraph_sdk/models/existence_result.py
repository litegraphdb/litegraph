from typing import List, Optional

from pydantic import BaseModel, ConfigDict, Field

from .edge_between import EdgeBetweenModel


class ExistenceResultModel(BaseModel):
    """
    Existence check for multiple identifiers response.
    """

    existing_nodes: Optional[List[str]] = Field(default=None, alias="ExistingNodes")
    existing_edges: Optional[List[str]] = Field(default=None, alias="ExistingEdges")
    existing_edges_between: Optional[List[EdgeBetweenModel]] = Field(
        default=None, alias="ExistingEdgesBetween"
    )
    missing_nodes: Optional[List[str]] = Field(default=None, alias="MissingNodes")
    missing_edges: Optional[List[str]] = Field(default=None, alias="MissingEdges")
    missing_edges_between: Optional[List[EdgeBetweenModel]] = Field(
        default=None, alias="MissingEdgesBetween"
    )
    model_config = ConfigDict(populate_by_name=True)
