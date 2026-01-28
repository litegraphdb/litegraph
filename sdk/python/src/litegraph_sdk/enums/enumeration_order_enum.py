from enum import Enum


class EnumerationOrder_Enum(str, Enum):
    """
    Enumeration order
    """
    CreatedAscending = "CreatedAscending"
    CreatedDescending = "CreatedDescending"
    NameAscending = "NameAscending"
    NameDescending = "NameDescending"
    GuidAscending = "GuidAscending"
    GuidDescending = "GuidDescending"
    CostAscending = "CostAscending"
    CostDescending = "CostDescending"
