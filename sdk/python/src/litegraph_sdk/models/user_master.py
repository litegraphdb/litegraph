from datetime import datetime, timezone
from pydantic import BaseModel, Field, ConfigDict


class UserMasterModel(BaseModel):
    """User master model."""

    guid: str = Field(default="", alias="GUID")
    tenant_guid: str = Field(default="", alias="TenantGUID")
    first_name: str = Field(default="", alias="FirstName")
    last_name: str = Field(default="", alias="LastName")
    email: str = Field(default="", alias="Email")
    password: str = Field(default="", alias="Password")
    active: bool = Field(default=True, alias="Active")
    created_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc")
    last_update_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc")

    model_config = ConfigDict(populate_by_name=True)
