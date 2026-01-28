from ..configuration import get_client
from ..models.edge import EdgeModel
from ..models.node import NodeModel
from ..models.route_request import RouteRequestModel
from ..utils.url_helper import _get_url


class RouteNodes:
    """
    Route Traversal resource class.
    """

    RESOURCE_NAME: str = "nodes"
    MODEL = RouteRequestModel
    RESPONSE_MODEL = EdgeModel
    RESPONSE_NODE_MODEL = NodeModel
    REQUIRE_GRAPH_GUID = True
    REQUIRE_TENANT = True

    @classmethod
    def get_edges_from(cls, graph_guid: str, node_guid: str):
        """
        Get the edges from a node of a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        url = (
            _get_url(cls, graph_id, node_guid, "/edges/from")
            if graph_id
            else _get_url(cls, graph_guid)
        )

        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )

    @classmethod
    def get_edges_to(cls, graph_guid: str, node_guid: str):
        """
        Get the edges to a node of a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        url = (
            _get_url(cls, graph_id, node_guid, "/edges/to")
            if graph_id
            else _get_url(cls, graph_guid)
        )
        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )

    @classmethod
    def edges(cls, graph_guid: str, node_guid: str):
        """
        Get the edges of a node in a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        url = (
            _get_url(cls, graph_id, node_guid, "/edges")
            if graph_id
            else _get_url(cls, graph_guid)
        )

        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )

    @classmethod
    def parents(cls, graph_guid: str, node_guid: str):
        """
        Get the parents of a node in a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        url = (
            _get_url(cls, graph_id, node_guid, "/parents")
            if graph_id
            else _get_url(cls, graph_guid)
        )
        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_NODE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )

    @classmethod
    def children(cls, graph_guid: str, node_guid: str):
        """
        Get the children of a node in a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        url = (
            _get_url(cls, graph_id, node_guid, "/children")
            if graph_id
            else _get_url(cls, graph_guid)
        )

        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_NODE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )

    @classmethod
    def neighbors(cls, graph_guid: str, node_guid: str):
        """
        Get the neighbors of a node in a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        url = (
            _get_url(cls, graph_id, node_guid, "/neighbors")
            if graph_id
            else _get_url(cls, graph_guid)
        )
        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_NODE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )

    @classmethod
    def between(cls, graph_guid: str, node_guid: str):
        """
        Get the nodes between two nodes in a graph.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        url = (
            _get_url(cls, graph_id, node_guid, "/between")
            if graph_id
            else _get_url(cls, graph_guid)
        )
        instance = client.request("GET", url)
        return (
            [cls.RESPONSE_NODE_MODEL.model_validate(item) for item in instance]
            if cls.MODEL
            else instance
        )
