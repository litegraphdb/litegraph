from typing import List

from pydantic import BaseModel

from .edge import EdgeModel


class RouteDetailModel(BaseModel):
    """
    Total cost and ordered list of edges between two nodes.
    """

    TotalCost: float
    Edges: List[EdgeModel]
