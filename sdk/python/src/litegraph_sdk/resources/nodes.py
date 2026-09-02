from ..configuration import get_client
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
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result
from ..models.node import NodeModel
from ..models.search_node_edge import SearchRequest, SearchResult
from ..utils.url_helper import _append_query, _pagination_params


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

    @classmethod
    def read_most_connected(
        cls,
        graph_guid: str = None,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read the most connected nodes in a graph as an EnumerationResult envelope."""
        client = get_client()
        gid = graph_guid or client.graph_guid
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/nodes/mostconnected"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), NodeModel)

    @classmethod
    def read_least_connected(
        cls,
        graph_guid: str = None,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read the least connected nodes in a graph as an EnumerationResult envelope."""
        client = get_client()
        gid = graph_guid or client.graph_guid
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/nodes/leastconnected"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), NodeModel)
