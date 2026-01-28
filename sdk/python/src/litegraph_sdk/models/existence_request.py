from typing import List, Optional

from pydantic import BaseModel, ConfigDict, Field

from .edge_between import EdgeBetweenModel


class ExistenceRequestModel(BaseModel):
    """
    Existence check for multiple identifiers request.
    """

    nodes: Optional[List[str]] = Field(default=None, alias="Nodes")
    edges: Optional[List[str]] = Field(default=None, alias="Edges")
    edges_between: Optional[List[EdgeBetweenModel]] = Field(
        default=None, alias="EdgesBetween"
    )
    model_config = ConfigDict(populate_by_name=True)

    def contains_existence_request(self) -> bool:
        """
        Verify that the object contains at least one existence request.
        """
        return bool(self.nodes or self.edges or self.edges_between)