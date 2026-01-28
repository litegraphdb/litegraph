from pydantic import BaseModel, Field, ConfigDict
from datetime import datetime, timezone
from uuid import uuid4


class TenantMetadataModel(BaseModel):
    """
    Tenant metadata.
    """
    guid: str = Field(default_factory=lambda: str(uuid4()), alias="GUID")
    name: str | None = Field(default=None, alias="Name")
    active: bool = Field(default=True, alias="Active")
    created_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc")
    last_update_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc")

    model_config = ConfigDict(
        populate_by_name=True,
        from_attributes=True
    )
