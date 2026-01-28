import json
import uuid
from typing import Any, Dict, List, Optional, Type

from pydantic import BaseModel

from .configuration import get_client
from .enums.severity_enum import Severity_Enum
from .exceptions import GRAPH_REQUIRED_ERROR, TENANT_REQUIRED_ERROR, SdkException
from .sdk_logging import log_error
from .utils.url_helper import _get_url

JSON_CONTENT_TYPE = {"Content-Type": "application/json"}


class ExistsAPIResource:
    """
    Mixin class for checking if a resource exists.
    If the resource exists, the method returns `True`, otherwise it returns `False`.
    """

    RESOURCE_NAME: str = ""
    REQUIRE_TENANT: bool = True

    @classmethod
    def exists(cls, resource_guid: str) -> bool:
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)

        graph_id = client.graph_guid
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, resource_guid)
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, resource_guid)
        )

        try:
            client.request("HEAD", url)
            return True
        except Exception:
            return False


class CreateableAPIResource:
    """
    A mixin class for creating resources.
    This class implements a generic creation pattern for resources using Pydantic models
    for data validation and serialization.
    """

    MODEL: Optional[Type[BaseModel]] = None
    RESOURCE_NAME: str = ""
    REQUIRE_TENANT: bool = True
    CREATE_METHOD: str = "PUT"

    @classmethod
    def create(cls, **kwargs) -> "BaseModel":
        """
        Creates a new resource.

        Args:
            **kwargs: Keyword arguments for the request, including the resource data.
                - headers (dict, optional): Additional headers for the request.
                - _data (dict, optional): The data to be sent in the request body.

        Returns:
            BaseModel: The created resource, validated against the MODEL if defined.

        Raises:
            ValueError: If tenant GUID or graph GUID is required but not provided.
        """
        client = get_client()
        headers = kwargs.pop("headers", {})

        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)

        graph_id = client.graph_guid
        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        # Extract data from kwargs
        _data = kwargs.pop("_data", kwargs.copy())

        # Build URL based on requirements
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id)
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant)
        )
        if cls.MODEL is not None:
            data = cls.MODEL(**_data).model_dump(
                mode="json", by_alias=True, exclude_unset=True
            )

        # Make request and validate response
        instance = client.request(cls.CREATE_METHOD, url, json=data, headers=headers)
        return cls.MODEL.model_validate(instance) if cls.MODEL else instance


class CreateableMultipleAPIResource:
    """
    A mixin class for creating multiple resources at once.
    This class implements a generic creation pattern for multiple resources using Pydantic models
    for data validation and serialization.
    """

    MODEL: Optional[Type[BaseModel]] = None
    RESOURCE_NAME: str = ""
    REQUIRE_TENANT: bool = True

    @classmethod
    def create_multiple(cls, resource_guid: List[dict]) -> List[BaseModel]:
        """
        Creates multiple nodes or edges in a single request.
        """
        if resource_guid is None:
            raise TypeError("Nodes parameter cannot be None")

        if not resource_guid:
            return []

        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid

        # Validate and serialize each node if MODEL is provided
        if cls.MODEL is not None:
            validated_nodes = [
                cls.MODEL(**node).model_dump(mode="json", by_alias=True)
                for node in resource_guid
            ]
        else:
            validated_nodes = resource_guid

        # Construct URL for multiple creation
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, "multiple")
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, "multiple")
        )

        # Make the request
        instances = client.request("PUT", url, json=validated_nodes)

        # Validate response data if MODEL is provided
        if cls.MODEL is not None:
            return [cls.MODEL.model_validate(instance) for instance in instances]
        return instances


class RetrievableAPIResource:
    """
    A mixin class for retrieving resources.
    This class implements a generic retrieval pattern for resources using Pydantic models
    (for data validation and deserialization).
    """

    RESOURCE_NAME: str = ""
    REQUIRE_GRAPH_GUID: bool = True
    MODEL: Optional[Type[BaseModel]] = None
    REQUIRE_TENANT: bool = True

    @classmethod
    def retrieve(cls, resource_guid: str) -> "BaseModel":
        """
        Retrieve a specific instance of the resource by its ID.
        """
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid
        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, resource_guid)
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, resource_guid)
        )
        instance = client.request("GET", url)

        return cls.MODEL.model_validate(instance) if cls.MODEL else instance


class UpdatableAPIResource:
    """
    A mixin class for updating resources.
    This class implements a generic update pattern for resources using Pydantic models(for data validation and serialization).
    """

    RESOURCE_NAME: str = ""
    MODEL: Optional[Type[BaseModel]] = None
    REQUIRE_GRAPH_GUID: bool = True
    REQUIRE_TENANT: bool = True

    @classmethod
    def update(cls, resource_guid: str, **kwargs) -> "BaseModel":
        """
        Update a specific instance of the resource by its ID.
        """
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid
        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, resource_guid)
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, resource_guid)
        )

        if cls.MODEL is not None:
            data = cls.MODEL(**kwargs).model_dump(
                mode="json", by_alias=True, exclude_unset=True, exclude_defaults=True
            )
        print(data)
        instance = client.request("PUT", url, json=data)

        return cls.MODEL.model_validate(instance) if cls.MODEL else instance


class DeletableAPIResource:
    """
    A mixin class for deleting resources.
    This class implements a generic delete pattern for resources for a specific resource.
    """

    RESOURCE_NAME: str = ""
    REQUIRE_GRAPH_GUID: bool = True
    REQUIRE_TENANT: bool = True

    @classmethod
    def delete(cls, resource_guid: str, **kwargs) -> None:
        """
        Delete a resource by its ID.
        """
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid
        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, resource_guid, **kwargs)
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, resource_guid, **kwargs)
        )

        client.request("DELETE", url)


class DeleteMultipleAPIResource:
    """
    A mixin class for deleting multiple resources.
    This class implements a generic delete pattern for resources for a specific resource.
    """

    RESOURCE_NAME: str = ""
    REQUIRE_GRAPH_GUID: bool = True
    REQUIRE_TENANT: bool = True

    @classmethod
    def delete_multiple(cls, resource_guid: List[str]) -> None:
        """
        Delete multiple resources by their IDs.
        """
        if not isinstance(resource_guid, list):
            raise TypeError("Input must be a list of IDs")

        if not resource_guid:
            return

        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, "multiple")
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, "multiple")
        )

        client.request(
            "DELETE",
            url,
            json=resource_guid,
            headers=JSON_CONTENT_TYPE,
        )


class DeleteAllAPIResource:
    """
    A mixin class for deleting all resources of a given type.
    This class implements a generic delete pattern for resources for a specific resource.
    """

    RESOURCE_NAME: str = ""
    REQUIRE_GRAPH_GUID: bool = True
    REQUIRE_TENANT: bool = True

    @classmethod
    def delete_all(cls) -> None:
        """
        Delete all resources of a given type.
        """
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None
        graph_id = str(uuid.UUID(str(graph_id))).lower()

        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, "all")
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, "all")
        )

        client.request("DELETE", url, headers=JSON_CONTENT_TYPE)


class AllRetrievableAPIResource:
    """
    A mixin class for retrieving all resources of a given type.
    This class implements a generic retrieval pattern for resources using Pydantic models(for data validation and deserialization).
    """

    RESOURCE_NAME: str = ""
    MODEL: Optional[Optional[Type[BaseModel]]] = None
    REQUIRE_GRAPH_GUID: bool = True
    REQUIRE_TENANT: bool = True

    @classmethod
    def retrieve_all(cls) -> list["BaseModel"]:
        """
        Retrieve all instances of the resource.
        """
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        graph_id = client.graph_guid
        if cls.REQUIRE_GRAPH_GUID and not graph_id:
            raise ValueError(GRAPH_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id)
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant)
        )
        instances = client.request("GET", url)

        return (
            [cls.MODEL.model_validate(instance) for instance in instances]
            if cls.MODEL
            else instances
        )


class SearchableAPIResource:
    """
    Provides a search method to search for resources based on criteria.
    This class implements a flexible search pattern against resources
    using request/response models for validation and serialization.
    """

    RESOURCE_NAME: str = ""
    SEARCH_MODELS: Optional[
        tuple[Optional[type[BaseModel]], Optional[type[BaseModel]]]
    ] = None
    REQUIRE_TENANT: bool = True

    @classmethod
    def search(cls, graph_id: str = None, **data) -> BaseModel:
        """
        Search for resources based on the provided criteria.
        """
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = (
            _get_url(cls, tenant, graph_id, "search")
            if graph_id and cls.REQUIRE_GRAPH_GUID
            else _get_url(cls, tenant, "search")
        )
        result_model = cls.SEARCH_MODELS[1]

        instance = client.request(
            "POST", url, data=json.dumps(data).encode(), headers=JSON_CONTENT_TYPE
        )
        return result_model(**instance)


class ExportGexfMixin:
    """
    Export a graph to GEXF format.
    """

    RESOURCE_NAME: str = ""
    MODEL: Optional[Type[BaseModel]] = None
    REQUIRE_TENANT: bool = True

    @classmethod
    def export_gexf(cls, graph_id: str, **params: Dict[str, Any]) -> str:
        client = get_client()
        if cls.REQUIRE_TENANT and client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        tenant = client.tenant_guid if cls.REQUIRE_TENANT else None
        url = _get_url(cls, tenant, graph_id, "export", "gexf", **params)
        response = client.request("GET", url)
        try:
            return response.decode("utf-8")
        except Exception as e:
            log_error(Severity_Enum.Error.value, f"Error exporting GEXF: {response}")
            raise SdkException("Error exporting GEXF") from e
