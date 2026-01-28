from pydantic import BaseModel

from ..enums.operator_enum import Opertator_Enum


class ExprModel(BaseModel):
    """
    Represents an expression with a left operand, an operator, and a right operand.
    """

    Left: str
    Operator: Opertator_Enum
    Right: str
