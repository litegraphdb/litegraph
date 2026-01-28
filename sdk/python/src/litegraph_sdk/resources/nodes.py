from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    SearchableAPIResource,
    UpdatableAPIResource,
    DeleteMultipleAPIResource,
    DeleteAllAPIResource
)
from ..models.node import NodeModel
from ..models.search_node_edge import SearchRequest, SearchResult


class Node(
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
    Node resource class.
    """

    RESOURCE_NAME: str = "nodes"
    MODEL = NodeModel
    SEARCH_MODELS = SearchRequest, SearchResult
