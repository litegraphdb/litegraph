from ..configuration import get_client
from ..models.edge import EdgeModel
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result
from ..models.node import NodeModel
from ..models.route_request import RouteRequestModel
from ..utils.url_helper import _get_url, _pagination_params


class RouteNodes:
    """
    Route Traversal resource class.

    Every traversal read returns a paginated EnumerationResult envelope whose
    ``objects`` are EdgeModel or NodeModel items.
    """

    RESOURCE_NAME: str = "nodes"
    MODEL = RouteRequestModel
    RESPONSE_MODEL = EdgeModel
    RESPONSE_NODE_MODEL = NodeModel
    REQUIRE_GRAPH_GUID = True
    REQUIRE_TENANT = True

    @classmethod
    def _traverse(
        cls,
        graph_guid: str,
        node_guid: str,
        segment: str,
        item_model,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        params = _pagination_params(max_keys, skip, order, continuation_token)

        url = (
            _get_url(cls, graph_id, node_guid, segment, **params)
            if graph_id
            else _get_url(cls, graph_guid, **params)
        )

        instance = client.request("GET", url)
        return parse_enumeration_result(instance, item_model if cls.MODEL else None)

    @classmethod
    def get_edges_from(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the edges from a node of a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/edges/from", cls.RESPONSE_MODEL,
            max_keys, skip, order, continuation_token,
        )

    @classmethod
    def get_edges_to(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the edges to a node of a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/edges/to", cls.RESPONSE_MODEL,
            max_keys, skip, order, continuation_token,
        )

    @classmethod
    def edges(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the edges of a node in a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/edges", cls.RESPONSE_MODEL,
            max_keys, skip, order, continuation_token,
        )

    @classmethod
    def parents(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the parents of a node in a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/parents", cls.RESPONSE_NODE_MODEL,
            max_keys, skip, order, continuation_token,
        )

    @classmethod
    def children(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the children of a node in a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/children", cls.RESPONSE_NODE_MODEL,
            max_keys, skip, order, continuation_token,
        )

    @classmethod
    def neighbors(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the neighbors of a node in a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/neighbors", cls.RESPONSE_NODE_MODEL,
            max_keys, skip, order, continuation_token,
        )

    @classmethod
    def between(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """
        Get the nodes between two nodes in a graph as an EnumerationResult envelope.
        """
        return cls._traverse(
            graph_guid, node_guid, "/between", cls.RESPONSE_NODE_MODEL,
            max_keys, skip, order, continuation_token,
        )
