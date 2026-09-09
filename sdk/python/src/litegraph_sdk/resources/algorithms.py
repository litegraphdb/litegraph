import json
from typing import Any, Dict, Optional, Union

from ..configuration import get_client
from ..exceptions import GRAPH_REQUIRED_ERROR, TENANT_REQUIRED_ERROR
from ..mixins import JSON_CONTENT_TYPE
from ..models.algorithms import (
    GenerateEmbeddingsRequestModel,
    GenerateEmbeddingsResultModel,
    GraphAlgorithmImportRequestModel,
    GraphAlgorithmRequestModel,
    GraphAlgorithmResultModel,
)


class Algorithm:
    """
    Graph algorithm helpers: run algorithms, export projections for external
    computation (for example rustworkx or NetworkX), and import results back.
    """

    @classmethod
    def run(
        cls,
        request: Union[GraphAlgorithmRequestModel, Dict[str, Any], str],
        graph_guid: Optional[str] = None,
    ) -> GraphAlgorithmResultModel:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        gid = graph_guid or client.graph_guid
        if not gid:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        if isinstance(request, str):
            request = GraphAlgorithmRequestModel(AlgorithmType=request)
        elif isinstance(request, dict):
            request = GraphAlgorithmRequestModel.model_validate(request)
        elif not isinstance(request, GraphAlgorithmRequestModel):
            raise TypeError("request must be a GraphAlgorithmRequestModel, dict, or algorithm-type string")

        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/algorithms"
        response = client.request("POST", url, json=data, headers=JSON_CONTENT_TYPE)
        return GraphAlgorithmResultModel.model_validate(response)

    @classmethod
    def export_projection(
        cls,
        graph_guid: Optional[str] = None,
        export_format: str = "NodeLinkJson",
        attributes: str = "Meta",
    ) -> str:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        gid = graph_guid or client.graph_guid
        if not gid:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        url = (
            f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/export/projection"
            f"?format={export_format}&attributes={attributes}"
        )
        response = client.request("GET", url)
        if isinstance(response, (bytes, bytearray)):
            return response.decode("utf-8")
        if isinstance(response, str):
            return response
        return json.dumps(response)

    @classmethod
    def import_results(
        cls,
        request: Union[GraphAlgorithmImportRequestModel, Dict[str, Any]],
        graph_guid: Optional[str] = None,
    ) -> Dict[str, Any]:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        gid = graph_guid or client.graph_guid
        if not gid:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        if isinstance(request, dict):
            request = GraphAlgorithmImportRequestModel.model_validate(request)
        elif not isinstance(request, GraphAlgorithmImportRequestModel):
            raise TypeError("request must be a GraphAlgorithmImportRequestModel or dict")

        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/algorithms/import"
        return client.request("POST", url, json=data, headers=JSON_CONTENT_TYPE)

    @classmethod
    def generate_embeddings(
        cls,
        request: Optional[Union[GenerateEmbeddingsRequestModel, Dict[str, Any]]] = None,
        graph_guid: Optional[str] = None,
    ) -> GenerateEmbeddingsResultModel:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        gid = graph_guid or client.graph_guid
        if not gid:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        if request is None:
            request = GenerateEmbeddingsRequestModel()
        elif isinstance(request, dict):
            request = GenerateEmbeddingsRequestModel.model_validate(request)
        elif not isinstance(request, GenerateEmbeddingsRequestModel):
            raise TypeError("request must be a GenerateEmbeddingsRequestModel or dict")

        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/algorithms/embeddings"
        response = client.request("POST", url, json=data, headers=JSON_CONTENT_TYPE)
        return GenerateEmbeddingsResultModel.model_validate(response)
