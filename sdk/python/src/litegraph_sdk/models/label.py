import uuid
from datetime import datetime, timezone
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class LabelModel(BaseModel):
    """
    Label metadata.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    node_guid: Optional[str] = Field(default=None, alias="NodeGUID")
    edge_guid: Optional[str] = Field(default=None, alias="EdgeGUID")
    label: str = Field(default="", alias="Label")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)
