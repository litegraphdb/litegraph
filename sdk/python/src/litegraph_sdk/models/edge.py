import uuid
from datetime import datetime, timezone
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class EdgeModel(BaseModel):
    """
    Edge in a graph.
    """

    guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="GUID", strict=True
    )
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID", strict=True
    )
    graph_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="GraphGUID", strict=True
    )
    name: Optional[str] = Field(default=None, alias="Name")
    from_node_guid: str = Field(default=str(uuid.UUID(int=0)), alias="From")
    from_node: Optional[dict] = Field(default=None, alias="FromNode")
    to_node_guid: str = Field(default=str(uuid.UUID(int=0)), alias="To")
    to_node: Optional[dict] = Field(default=None, alias="ToNode")
    cost: int = Field(default=0, alias="Cost", ge=0)
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )
    labels: Optional[list] = Field(default_factory=list, alias="Labels")
    tags: Optional[dict] = Field(default_factory=dict, alias="Tags")
    data: Optional[dict] = Field(default=None, alias="Data")
    model_config = ConfigDict(populate_by_name=True)
