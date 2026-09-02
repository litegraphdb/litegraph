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
from ..models.label import LabelModel
from ..utils.url_helper import _append_query, _pagination_params


class Label(
    ExistsAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
):
    """Labels resource."""

    REQUIRE_GRAPH_GUID = False
    RESOURCE_NAME = "labels"
    MODEL = LabelModel

    @classmethod
    def read_graph_labels(
        cls,
        graph_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read labels for a specific graph as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/labels"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), LabelModel)

    @classmethod
    def read_node_labels(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read labels for a specific node as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/labels"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), LabelModel)

    @classmethod
    def read_edge_labels(
        cls,
        graph_guid: str,
        edge_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read labels for a specific edge as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/labels"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), LabelModel)

    @classmethod
    def delete_graph_labels(cls, graph_guid: str, **kwargs):
        """Delete all labels for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/labels"
        return client.request("DELETE", url)

    @classmethod
    def delete_node_labels(cls, graph_guid: str, node_guid: str, **kwargs):
        """Delete all labels for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/labels"
        return client.request("DELETE", url)

    @classmethod
    def delete_edge_labels(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Delete all labels for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/labels"
        return client.request("DELETE", url)
