from ..configuration import get_client
from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    UpdatableAPIResource,
)
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result
from ..models.tag import TagModel
from ..utils.url_helper import _append_query, _pagination_params


class Tag(
    ExistsAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
):
    """Tags resource."""

    REQUIRE_GRAPH_GUID = False
    RESOURCE_NAME = "tags"
    MODEL = TagModel

    @classmethod
    def read_graph_tags(
        cls,
        graph_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read tags for a specific graph as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/tags"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), TagModel)

    @classmethod
    def read_node_tags(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read tags for a specific node as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/tags"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), TagModel)

    @classmethod
    def read_edge_tags(
        cls,
        graph_guid: str,
        edge_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read tags for a specific edge as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/tags"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), TagModel)

    @classmethod
    def delete_graph_tags(cls, graph_guid: str, **kwargs):
        """Delete all tags for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/tags"
        return client.request("DELETE", url)

    @classmethod
    def delete_node_tags(cls, graph_guid: str, node_guid: str, **kwargs):
        """Delete all tags for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/tags"
        return client.request("DELETE", url)

    @classmethod
    def delete_edge_tags(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Delete all tags for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/tags"
        return client.request("DELETE", url)
