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
from ..models.label import LabelModel


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
    def read_graph_labels(cls, graph_guid: str, **kwargs):
        """Read all labels for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/labels"
        return client.request("GET", url)

    @classmethod
    def read_node_labels(cls, graph_guid: str, node_guid: str, **kwargs):
        """Read all labels for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/labels"
        return client.request("GET", url)

    @classmethod
    def read_edge_labels(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Read all labels for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/labels"
        return client.request("GET", url)

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
