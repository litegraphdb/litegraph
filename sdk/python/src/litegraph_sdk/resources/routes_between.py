from ..configuration import get_client
from ..models.edge import EdgeModel
from ..models.route_request import RouteRequestModel
from ..utils.url_helper import _get_url


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
    def between(cls, graph_guid: str, from_node_guid: str, to_node_guid: str, **kwargs):
        """
        Get the routes between two nodes in a graph.
        """
        # Define query parameters
        query_params = {"from": from_node_guid, "to": to_node_guid}
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        url = (
            _get_url(cls, graph_guid, "between", **query_params)
            if graph_id
            else _get_url(cls, graph_guid)
        )

        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )
