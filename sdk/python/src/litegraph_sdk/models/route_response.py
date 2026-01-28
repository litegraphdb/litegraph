from datetime import datetime, timezone
from typing import List

from pydantic import BaseModel, ConfigDict, Field

from .route_detail import RouteDetailModel


class Timestamp(BaseModel):
    """
    Timestamp model.
    """

    Start: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    End: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    TotalMs: float
    Messages: dict = Field(default_factory=dict)
    model_config = ConfigDict(
        populate_by_name=True
    )  # class Config: populate_by_name = True


class RouteResultModel(BaseModel):
    """
    Route Result model.
    """

    Timestamp: Timestamp
    Routes: List[RouteDetailModel]
