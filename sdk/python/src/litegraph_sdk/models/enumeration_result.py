from typing import Any, Generic, List, Optional, Type, TypeVar

from pydantic import BaseModel, ConfigDict, Field

T = TypeVar("T")


class EnumerationResultModel(BaseModel, Generic[T]):
    """
    Paginated enumeration envelope returned by every list-shaped LiteGraph route.

    Mirrors the server's EnumerationResult object:
    Success, Timestamp, MaxResults, ContinuationToken, EndOfResults,
    TotalRecords, RecordsRemaining, and Objects.
    """

    success: bool = Field(default=True, alias="Success")
    timestamp: Optional[dict] = Field(default=None, alias="Timestamp")
    max_results: int = Field(default=1000, alias="MaxResults")
    continuation_token: Optional[str] = Field(default=None, alias="ContinuationToken")
    end_of_results: bool = Field(default=True, alias="EndOfResults")
    total_records: int = Field(default=0, alias="TotalRecords")
    records_remaining: int = Field(default=0, alias="RecordsRemaining")
    objects: List[T] = Field(default_factory=list, alias="Objects")

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


def parse_enumeration_result(
    response: Any, item_model: Optional[Type[BaseModel]] = None
) -> EnumerationResultModel:
    """
    Deserialize an EnumerationResult envelope response.

    Args:
        response: The raw JSON response (dict) returned by the server.
        item_model: Optional pydantic model class used to validate each entry
            in the envelope's Objects list. When None, objects are returned
            as-is (dicts).

    Returns:
        EnumerationResultModel: The validated enumeration envelope.
    """
    if response is None:
        response = {}
    if item_model is not None:
        return EnumerationResultModel[item_model].model_validate(response)
    return EnumerationResultModel.model_validate(response)
