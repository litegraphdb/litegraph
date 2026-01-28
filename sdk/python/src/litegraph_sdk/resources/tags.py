from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
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
    UpdatableAPIResource,
    DeletableAPIResource,
):
    """Tags resource."""

    REQUIRE_GRAPH_GUID = False
    RESOURCE_NAME = "tags"
    MODEL = TagModel
