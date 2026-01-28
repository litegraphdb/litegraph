from typing import Optional

from pydantic import BaseModel, ConfigDict, Field

from ..enums.api_error_enum import ApiError_Enum


class ApiErrorResponseModel(BaseModel):
    """
    An API error response model.
    """

    error: ApiError_Enum = Field(alias="Error")
    context: Optional[str] = Field(default=None, alias="Context")
    description: Optional[str] = Field(default=None, alias="Description")
    model_config = ConfigDict(populate_by_name=True)
