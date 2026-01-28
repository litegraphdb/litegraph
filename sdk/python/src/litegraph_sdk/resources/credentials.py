from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    UpdatableAPIResource,
)
from ..models.credential import CredentialModel


class Credential(
    ExistsAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    CreateableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
):
    """Credentials resource."""

    REQUIRE_GRAPH_GUID = False
    RESOURCE_NAME = "credentials"
    MODEL = CredentialModel
