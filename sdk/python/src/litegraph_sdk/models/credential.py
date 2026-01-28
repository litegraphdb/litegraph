import uuid
from datetime import datetime, timezone
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class CredentialModel(BaseModel):
    """
    Credentials.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="TenantGUID"
    )
    user_guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="UserGUID")
    name: Optional[str] = Field(default=None, alias="Name")
    bearer_token: str = Field(
        default_factory=lambda: str(uuid.uuid4()), alias="BearerToken"
    )
    active: bool = Field(default=True, alias="Active")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)
