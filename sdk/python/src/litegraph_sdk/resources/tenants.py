from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    UpdatableAPIResource,
)
from ..models.tenant_metadata import TenantMetadataModel


class Tenant(
    ExistsAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    CreateableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
):
    RESOURCE_NAME: str = "tenants"
    MODEL = TenantMetadataModel
    REQUIRE_TENANT = False
    REQUIRE_GRAPH_GUID = False

    @classmethod
    def delete(cls, resource_guid: str, force: bool = False) -> None:
        """
        Delete a resource by its GUID.
        """
        kwargs = {"force": None} if force else {}
        return super().delete(resource_guid, **kwargs)
