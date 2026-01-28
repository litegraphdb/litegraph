from ..configuration import get_client
from ..models.route_request import RouteRequestModel
from ..models.route_response import RouteResultModel
from ..utils.url_helper import _get_url


class Routes:
    """
    Route resource class.
    """

    RESOURCE_NAME: str = "routes"
    MODEL = RouteRequestModel
    RESPONSE_MODEL = RouteResultModel
    REQUIRE_GRAPH_GUID = True
    REQUIRE_TENANT = True

    @classmethod
    def routes(cls, graph_guid: str, **kwargs):
        """
        Routes
        """
        headers = {"Content-Type": "application/octet-stream"}
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        url = _get_url(cls, graph_id) if graph_id else _get_url(cls, graph_guid)
        instance = client.request("POST", url, data=kwargs, headers=headers)
        return cls.RESPONSE_MODEL.model_validate(instance) if cls.MODEL else instance
