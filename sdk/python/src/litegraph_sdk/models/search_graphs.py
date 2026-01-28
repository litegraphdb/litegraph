from typing import List, Optional

from pydantic import BaseModel, ConfigDict, Field

from ..enums.enumeration_order_enum import EnumerationOrder_Enum
from ..models.expression import ExprModel
from ..models.graphs import GraphModel


class SearchRequestGraph(BaseModel):
    """
    Search request for graphs.
    """

    ordering: EnumerationOrder_Enum = Field(
        EnumerationOrder_Enum.CreatedDescending, alias="Ordering"
    )  # Default ordering
    expr: Optional[ExprModel] = Field(None, alias="Expr")  # Optional Expression
    labels: Optional[List] = Field(None, alias="Labels")  # Optional Labels
    tags: Optional[dict] = Field(None, alias="Tags")  # Optional Tags


class SearchResultGraph(BaseModel):
    """
    Search result for graphs.
    """

    graphs: Optional[List[GraphModel]] = Field(None, alias="Graphs")
    model_config = ConfigDict(
        from_attributes=True,
        json_encoders={Optional[List]: lambda v: v or None},
        exclude_none=True,
    )
