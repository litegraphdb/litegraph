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
from ..models.tag import TagModel


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
    def read_graph_tags(cls, graph_guid: str, **kwargs):
        """Read all tags for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/tags"
        return client.request("GET", url)

    @classmethod
    def read_node_tags(cls, graph_guid: str, node_guid: str, **kwargs):
        """Read all tags for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/tags"
        return client.request("GET", url)

    @classmethod
    def read_edge_tags(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Read all tags for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/tags"
        return client.request("GET", url)

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
