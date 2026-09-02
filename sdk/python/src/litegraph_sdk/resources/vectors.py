from ..configuration import get_client
from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    DeleteMultipleAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    UpdatableAPIResource,
)
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result
from ..models.vector_metadata import VectorMetadataModel
from ..models.vector_search_request import VectorSearchRequestModel
from ..models.vector_search_result import VectorSearchResultModel
from ..utils.url_helper import _append_query, _pagination_params


class Vector(
    ExistsAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    DeleteMultipleAPIResource,
):
    """
    Vector resource class.
    """

    RESOURCE_NAME: str = "vectors"
    MODEL = VectorMetadataModel
    REQUIRE_GRAPH_GUID: bool = False

    @classmethod
    def search(
        cls, search_request: VectorSearchRequestModel, **kwargs
    ) -> EnumerationResultModel:
        """Search vectors using similarity search.

        Returns an EnumerationResult envelope whose ``objects`` are
        VectorSearchResultModel items.
        """
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/vectors"
        data = search_request.model_dump(by_alias=True, exclude_none=True)
        response = client.request("POST", url, json=data)
        return parse_enumeration_result(response, VectorSearchResultModel)

    @classmethod
    def read_graph_vectors(
        cls,
        graph_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read vectors for a specific graph as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectors"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), VectorMetadataModel)

    @classmethod
    def read_node_vectors(
        cls,
        graph_guid: str,
        node_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read vectors for a specific node as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/vectors"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), VectorMetadataModel)

    @classmethod
    def read_edge_vectors(
        cls,
        graph_guid: str,
        edge_guid: str,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
        **kwargs,
    ) -> EnumerationResultModel:
        """Read vectors for a specific edge as an EnumerationResult envelope."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/vectors"
        url = _append_query(
            url, _pagination_params(max_keys, skip, order, continuation_token)
        )
        return parse_enumeration_result(client.request("GET", url), VectorMetadataModel)

    @classmethod
    def delete_graph_vectors(cls, graph_guid: str, **kwargs):
        """Delete all vectors for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectors"
        return client.request("DELETE", url)

    @classmethod
    def delete_node_vectors(cls, graph_guid: str, node_guid: str, **kwargs):
        """Delete all vectors for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/vectors"
        return client.request("DELETE", url)

    @classmethod
    def delete_edge_vectors(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Delete all vectors for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/vectors"
        return client.request("DELETE", url)
