from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    UpdatableAPIResource,
)
from ..models.user_master import UserMasterModel


class User(
    ExistsAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    CreateableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
):
    """Users resource."""

    REQUIRE_GRAPH_GUID = False
    RESOURCE_NAME = "users"
    MODEL = UserMasterModel
