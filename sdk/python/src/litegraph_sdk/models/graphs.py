from datetime import datetime, timezone
from typing import Dict, List, Optional
from uuid import uuid4

from pydantic import BaseModel, ConfigDict, Field


class GraphModel(BaseModel):
    """
    Represents a graph.
    """

    guid: str = Field(default_factory=lambda: str(uuid4()), alias="GUID", strict=True)
    tenant_guid: Optional[str] = Field(default=None, alias="TenantGUID", strict=True)
    name: Optional[str] = Field(default=None, alias="Name")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )
    labels: Optional[List] = Field(default_factory=list, alias="Labels")
    tags: Optional[Dict[str, str]] = Field(default_factory=dict, alias="Tags")
    vectors: Optional[List] = Field(default_factory=list, alias="Vectors")
    data: Optional[Dict] = Field(default=None, alias="Data")
    model_config = ConfigDict(populate_by_name=True)
