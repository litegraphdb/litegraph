from typing import Type, Union

from pydantic import BaseModel

from ..configuration import get_client
from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    ExportGexfMixin,
    RetrievableAPIResource,
    SearchableAPIResource,
    UpdatableAPIResource,
)
from ..enums.severity_enum import Severity_Enum
from ..exceptions import SdkException, TENANT_REQUIRED_ERROR
from ..models.existence_request import ExistenceRequestModel
from ..models.existence_result import ExistenceResultModel
from ..models.graphs import GraphModel
from ..models.import_export import SubgraphExtractionRequestModel
from ..models.search_graphs import SearchRequestGraph, SearchResultGraph
from ..sdk_logging import log_error
from ..utils.url_helper import _get_url

NDJSON_CONTENT_TYPE = {"Content-Type": "application/x-ndjson"}
_GUID_STRATEGIES = {"preserve", "regenerate", "skip", "overwrite"}
_ON_ERROR_MODES = {"abort", "skip"}


class Graph(
    ExistsAPIResource,
    CreateableAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    ExportGexfMixin,
    SearchableAPIResource,
):
    """
    Graph resource class.
    """

    RESOURCE_NAME: str = "graphs"
    REQUIRE_GRAPH_GUID: bool = False
    MODEL = GraphModel
    SEARCH_MODELS = SearchRequestGraph, SearchResultGraph
    EXISTENCE_REQUEST_MODEL: Type[BaseModel] = ExistenceRequestModel
    EXISTENCE_RESPONSE_MODEL: Type[BaseModel] = ExistenceResultModel

    @classmethod
    def delete(cls, resource_id: str, force: bool = False) -> None:
        """
        Delete a resource by its ID.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        if cls.REQUIRE_GRAPH_GUID and graph_id is None:
            raise ValueError("Graph GUID is required for this resource.")

        url = (
            _get_url(cls, graph_id, resource_id, force=None)
            if force
            else _get_url(cls, graph_id, resource_id)
        )
        client.request("DELETE", url)

    @classmethod
    def batch_existence(
        cls, graph_guid: str, request: ExistenceRequestModel
    ) -> ExistenceResultModel:
        """
        Execute a batch existence request.
        """
        if request is None:
            raise ValueError("Request cannot be None")

        if not isinstance(request, cls.EXISTENCE_REQUEST_MODEL):
            raise TypeError(
                f"Request must be an instance of {cls.EXISTENCE_REQUEST_MODEL.__name__}"
            )

        if not request.contains_existence_request():
            raise ValueError("Request must contain at least one existence check")

        client = get_client()

        # Construct URL
        url = _get_url(cls, graph_guid, "existence")

        # Prepare request data
        data = request.model_dump(mode="json", by_alias=True)

        # Make the request
        headers = {"Content-Type": "application/json"}
        response = client.request(method="POST", url=url, json=data, headers=headers)

        # Parse and validate response

        return cls.EXISTENCE_RESPONSE_MODEL.model_validate(response)

    @classmethod
    def export_gexf(cls, graph_id: str, include_data: bool = False) -> str:
        params = {}
        if include_data:
            params["incldata"] = None
        return super().export_gexf(graph_id, **params)

    @classmethod
    def get_statistics(cls, graph_guid: str = None, **kwargs):
        """Get graph statistics."""
        client = get_client()
        if graph_guid:
            url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/stats"
        else:
            url = f"v1.0/tenants/{client.tenant_guid}/graphs/stats"
        return client.request("GET", url)

    @classmethod
    def enable_vector_index(cls, graph_guid: str, config: dict, **kwargs):
        """Enable vector indexing on a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/enable"
        return client.request("PUT", url, json=config)

    @classmethod
    def disable_vector_index(cls, graph_guid: str, **kwargs):
        """Disable vector indexing on a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex"
        return client.request("DELETE", url)

    @classmethod
    def rebuild_vector_index(cls, graph_guid: str, **kwargs):
        """Rebuild the vector index for a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/rebuild"
        return client.request("POST", url)

    @classmethod
    def get_vector_index_config(cls, graph_guid: str, **kwargs):
        """Get the vector index configuration for a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/config"
        return client.request("GET", url)

    @classmethod
    def get_vector_index_stats(cls, graph_guid: str, **kwargs):
        """Get vector index statistics for a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/stats"
        return client.request("GET", url)

    @classmethod
    def get_subgraph(cls, graph_guid: str, node_guid: str, **kwargs):
        """Get subgraph starting from a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/subgraph"
        return client.request("GET", url)

    @classmethod
    def _decode_jsonl(cls, response) -> str:
        """Decode a raw JSONL (application/x-ndjson) response body into text."""
        if isinstance(response, str):
            return response
        try:
            return response.decode("utf-8")
        except Exception as e:
            log_error(
                Severity_Enum.Error.value, f"Error decoding JSONL response: {response}"
            )
            raise SdkException("Error decoding JSONL response") from e

    @classmethod
    def export_jsonl(
        cls,
        graph_guid: str,
        include_data: bool = False,
        include_subordinates: bool = False,
    ) -> str:
        """
        Export a graph to JSONL (application/x-ndjson) format.

        Args:
            graph_guid (str): The GUID of the graph to export.
            include_data (bool): Include the ``Data`` property of each object.
            include_subordinates (bool): Include subordinate labels, tags, and vectors.

        Returns:
            str: The exported graph as JSONL text.
        """
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)

        params = {}
        if include_data:
            params["incldata"] = None
        if include_subordinates:
            params["inclsub"] = None

        url = _get_url(cls, client.tenant_guid, graph_guid, "export", "jsonl", **params)
        response = client.request("GET", url)
        return cls._decode_jsonl(response)

    @classmethod
    def export_subgraph_jsonl(
        cls,
        graph_guid: str,
        request: Union[dict, SubgraphExtractionRequestModel],
    ) -> str:
        """
        Export a subgraph to JSONL (application/x-ndjson) format.

        Args:
            graph_guid (str): The GUID of the graph to export from.
            request (dict | SubgraphExtractionRequestModel): The subgraph
                extraction request describing the traversal.

        Returns:
            str: The exported subgraph as JSONL text.
        """
        if request is None:
            raise ValueError("Request cannot be None")

        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)

        if isinstance(request, SubgraphExtractionRequestModel):
            data = request.model_dump(mode="json", by_alias=True)
        elif isinstance(request, dict):
            data = SubgraphExtractionRequestModel(**request).model_dump(
                mode="json", by_alias=True
            )
        else:
            raise TypeError(
                "Request must be a dict or SubgraphExtractionRequestModel instance"
            )

        url = _get_url(cls, client.tenant_guid, graph_guid, "export", "jsonl")
        response = client.request("POST", url, json=data)
        return cls._decode_jsonl(response)

    @classmethod
    def _import_query_params(
        cls,
        guid_strategy: str = None,
        on_error: str = None,
        batch_size: int = None,
    ) -> dict:
        """Validate and build the query parameters for a JSONL import."""
        params = {}
        if guid_strategy is not None:
            if guid_strategy not in _GUID_STRATEGIES:
                raise ValueError(
                    f"guid_strategy must be one of {sorted(_GUID_STRATEGIES)}"
                )
            params["guidstrategy"] = guid_strategy
        if on_error is not None:
            if on_error not in _ON_ERROR_MODES:
                raise ValueError(f"on_error must be one of {sorted(_ON_ERROR_MODES)}")
            params["onerror"] = on_error
        if batch_size is not None:
            if not isinstance(batch_size, int) or batch_size <= 0:
                raise ValueError("batch_size must be a positive integer")
            params["batchsize"] = batch_size
        return params

    @classmethod
    def import_jsonl(
        cls,
        graph_guid: str,
        jsonl: str,
        guid_strategy: str = None,
        on_error: str = None,
        batch_size: int = None,
    ) -> dict:
        """
        Import JSONL (application/x-ndjson) content, merging into an existing graph.

        Args:
            graph_guid (str): The GUID of the graph to merge into.
            jsonl (str): The raw JSONL content to import.
            guid_strategy (str, optional): One of ``preserve``, ``regenerate``,
                ``skip``, or ``overwrite``.
            on_error (str, optional): One of ``abort`` or ``skip``.
            batch_size (int, optional): Positive batch size for the import.

        Returns:
            dict: The GraphImportResult returned by the server.
        """
        if jsonl is None:
            raise ValueError("JSONL content cannot be None")

        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)

        params = cls._import_query_params(guid_strategy, on_error, batch_size)
        url = _get_url(cls, client.tenant_guid, graph_guid, "import", "jsonl", **params)
        return client.request(
            "POST", url, content=jsonl, headers=NDJSON_CONTENT_TYPE
        )

    @classmethod
    def import_jsonl_as_new(
        cls,
        jsonl: str,
        guid_strategy: str = None,
        on_error: str = None,
        batch_size: int = None,
    ) -> dict:
        """
        Import JSONL (application/x-ndjson) content as a new graph.

        Args:
            jsonl (str): The raw JSONL content to import.
            guid_strategy (str, optional): One of ``preserve``, ``regenerate``,
                ``skip``, or ``overwrite``.
            on_error (str, optional): One of ``abort`` or ``skip``.
            batch_size (int, optional): Positive batch size for the import.

        Returns:
            dict: The GraphImportResult returned by the server.
        """
        if jsonl is None:
            raise ValueError("JSONL content cannot be None")

        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)

        params = cls._import_query_params(guid_strategy, on_error, batch_size)
        url = _get_url(cls, client.tenant_guid, "import", "jsonl", **params)
        return client.request(
            "POST", url, content=jsonl, headers=NDJSON_CONTENT_TYPE
        )
