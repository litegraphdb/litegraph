from ..configuration import get_client
from ..models.edge import EdgeModel
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result
from ..models.route_request import RouteRequestModel
from ..utils.url_helper import _get_url, _pagination_params


class RouteEdges:
    """
    Route Between the node of a graph resource class.
    """

    RESOURCE_NAME: str = "edges"
    MODEL = RouteRequestModel
    RESPONSE_MODEL = EdgeModel
    REQUIRE_GRAPH_GUID = True
    REQUIRE_TENANT = True

    @classmethod
    def between(
        cls,
        graph_guid: str,
        from_node_guid: str,
        to_node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """
        Get the edges between two nodes in a graph as an EnumerationResult
        envelope whose ``objects`` are EdgeModel items.
        """
        # Define query parameters
        query_params = {"from": from_node_guid, "to": to_node_guid}
        query_params.update(
            _pagination_params(max_keys, skip, order, continuation_token)
        )
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        url = (
            _get_url(cls, graph_guid, "between", **query_params)
            if graph_id
            else _get_url(cls, graph_guid)
        )

        instance = client.request("GET", url)
        return parse_enumeration_result(
            instance, cls.RESPONSE_MODEL if cls.MODEL else None
        )
