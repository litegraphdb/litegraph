import uuid
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field

from .search_node_edge import SearchRequest


class RouteRequestModel(BaseModel):
    """
    Route request model.
    """

    graph: str = Field(default_factory=lambda: uuid.uuid4(), alias="Graph")
    from_node: str = Field(default_factory=lambda: uuid.uuid4(), alias="From")
    to_node: str = Field(default_factory=lambda: uuid.uuid4(), alias="To")
    edge_filter: Optional[SearchRequest] = Field(default=None, alias="EdgeFilter")
    node_filter: Optional[SearchRequest] = Field(default=None, alias="NodeFilter")
    model_config = ConfigDict(
        populate_by_name=True
    )  # class Config: populate_by_name = True
