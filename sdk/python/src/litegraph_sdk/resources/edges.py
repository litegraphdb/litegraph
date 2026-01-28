from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    DeleteAllAPIResource,
    DeleteMultipleAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    SearchableAPIResource,
    UpdatableAPIResource,
)
from ..models.edge import EdgeModel
from ..models.search_node_edge import SearchRequest, SearchResultEdge


class Edge(
    ExistsAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    SearchableAPIResource,
    DeleteMultipleAPIResource,
    DeleteAllAPIResource,
):
    """
    Edge resource class.
    """

    RESOURCE_NAME: str = "edges"
    MODEL = EdgeModel
    SEARCH_MODELS = SearchRequest, SearchResultEdge
