from enum import Enum


class Opertator_Enum(str, Enum):
    """
    Operator Enum
    """
    GreaterThan = "GreaterThan"
    LessThan = "LessThan"
    Equal = "Equal"
