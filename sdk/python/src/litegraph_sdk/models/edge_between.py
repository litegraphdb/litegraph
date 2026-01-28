import uuid

from pydantic import BaseModel, ConfigDict, Field


class EdgeBetweenModel(BaseModel):
    """
    Represents an edge between two nodes in a graph.
    """
    from_node_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="From", strict=True
    )
    to_node_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="To", strict=True
    )
    model_config = ConfigDict(
        populate_by_name=True
    )
