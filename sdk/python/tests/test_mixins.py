import uuid
from typing import Optional
from unittest.mock import Mock, patch

import pytest
from litegraph_sdk.base import BaseClient
from litegraph_sdk.mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    DeleteAllAPIResource,
    DeleteMultipleAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    SearchableAPIResource,
    UpdatableAPIResource,
    ExportGexfMixin,
)
from pydantic import BaseModel
from litegraph_sdk.exceptions import SdkException


# Test Models
class MockModel(BaseModel):
    id: str
    name: Optional[str] = None
    data: Optional[dict] = None


class MockRequestModel(BaseModel):
    query: str


class MockResponseModel(BaseModel):
    results: list[MockModel]


# Test Resource Classes
class TestBaseResource:
    RESOURCE_NAME = "test-resources"
    MODEL = MockModel
    SEARCH_MODELS = (MockRequestModel, MockResponseModel)
    REQUIRE_GRAPH_GUID = True


class ResourceModel(
    TestBaseResource,
    ExistsAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,  # Added new mixin
    RetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    DeleteAllAPIResource,  # Added new mixin
    DeleteMultipleAPIResource,  # Added new mixin
    AllRetrievableAPIResource,
    SearchableAPIResource,
):
    pass


class TestResourceNoGraphGuid(ResourceModel):
    REQUIRE_GRAPH_GUID = False


class TestExportGexf(TestBaseResource, ExportGexfMixin):
    pass


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and configure it."""
    client = Mock(spec=BaseClient)
    client.tenant_guid = "test-tenant-guid"
    client.graph_guid = "test-graph-guid"
    client.base_url = "http://test-api.com"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_exists_resource(mock_client):
    """Test ExistsAPIResource mixin."""
    # Test resource exists
    mock_client.request.return_value = None
    assert ResourceModel.exists("test-id") is True

    # Test resource doesn't exist
    mock_client.request.side_effect = Exception("Not found")
    assert ResourceModel.exists("test-id") is False

    # Test with no graph_guid
    mock_client.graph_guid = None
    assert TestResourceNoGraphGuid.exists("test-id") is False


def test_create_resource(mock_client):
    """Test CreateableAPIResource mixin."""
    test_data = {"id": "test-id", "name": "Test Resource", "data": {"key": "value"}}
    mock_client.request.return_value = test_data
    mock_client.request.side_effect = None  # Reset any previous side effects

    # Test successful creation
    result = ResourceModel.create(**test_data)
    assert isinstance(result, MockModel)
    assert result.id == test_data["id"]
    assert result.name == test_data["name"]
    assert result.data == test_data["data"]

    # Test creation without graph_guid
    mock_client.graph_guid = None
    result = TestResourceNoGraphGuid.create(**test_data)
    assert isinstance(result, MockModel)

    # Test minimal data
    minimal_data = {"id": "test-id"}
    mock_client.graph_guid = "test-graph-guid"
    mock_client.request.return_value = minimal_data
    result = ResourceModel.create(**minimal_data)
    assert isinstance(result, MockModel)
    assert result.name is None
    assert result.data is None


def test_retrieve_resource(mock_client):
    """Test RetrievableAPIResource mixin."""
    test_data = {"id": "test-id", "name": "Test Resource", "data": {"key": "value"}}
    mock_client.request.return_value = test_data
    mock_client.request.side_effect = None

    # Test successful retrieval
    result = ResourceModel.retrieve("test-id")
    assert isinstance(result, MockModel)
    assert result.id == test_data["id"]
    assert result.name == test_data["name"]
    assert result.data == test_data["data"]

    # Test retrieval without graph_guid when required
    mock_client.graph_guid = None
    with pytest.raises(ValueError, match="Graph GUID is required for this resource"):
        ResourceModel.retrieve("test-id")

    # Test retrieval without graph_guid when not required
    mock_client.request.return_value = test_data
    result = TestResourceNoGraphGuid.retrieve("test-id")
    assert isinstance(result, MockModel)


def test_update_resource(mock_client):
    """Test UpdatableAPIResource mixin."""
    test_data = {
        "id": "test-id",
        "name": "Updated Resource",
        "data": {"key": "new-value"},
    }
    mock_client.request.return_value = test_data
    mock_client.request.side_effect = None

    # Test successful update
    result = ResourceModel.update("test-id", **test_data)
    assert isinstance(result, MockModel)
    assert result.id == test_data["id"]
    assert result.name == test_data["name"]
    assert result.data == test_data["data"]

    # Test update without graph_guid when required
    mock_client.graph_guid = None
    with pytest.raises(ValueError, match="Graph GUID is required for this resource"):
        ResourceModel.update("test-id", **test_data)

    # Test update without graph_guid when not required
    mock_client.request.return_value = test_data
    result = TestResourceNoGraphGuid.update("test-id", **test_data)
    assert isinstance(result, MockModel)


def test_delete_resource(mock_client):
    """Test DeletableAPIResource mixin."""
    mock_client.request.side_effect = None

    # Test successful deletion
    ResourceModel.delete("test-id")
    mock_client.request.assert_called_once()
    assert mock_client.request.call_args[0][0] == "DELETE"

    # Test deletion without graph_guid when required
    mock_client.request.reset_mock()
    mock_client.graph_guid = None
    with pytest.raises(ValueError, match="Graph GUID is required for this resource"):
        ResourceModel.delete("test-id")

    # Test deletion without graph_guid when not required
    mock_client.request.reset_mock()
    TestResourceNoGraphGuid.delete("test-id")
    mock_client.request.assert_called_once()


def test_retrieve_all_resources(mock_client):
    """Test AllRetrievableAPIResource mixin."""
    test_data = [
        {"id": "test-id-1", "name": "Resource 1", "data": {"key": "value1"}},
        {"id": "test-id-2", "name": "Resource 2", "data": {"key": "value2"}},
    ]
    mock_client.request.return_value = test_data
    mock_client.request.side_effect = None

    # Test successful retrieval of all resources
    results = ResourceModel.retrieve_all()
    assert isinstance(results, list)
    assert all(isinstance(item, MockModel) for item in results)
    assert len(results) == 2

    # Test retrieve all without graph_guid when required
    mock_client.graph_guid = None
    with pytest.raises(ValueError, match="Graph GUID is required for this resource."):
        ResourceModel.retrieve_all()

    # Test retrieve all without graph_guid when not required
    mock_client.request.return_value = test_data
    results = TestResourceNoGraphGuid.retrieve_all()
    assert isinstance(results, list)
    assert len(results) == 2


def test_search_resources(mock_client):
    """Test SearchableAPIResource mixin."""
    search_response = {
        "results": [
            {"id": "test-id-1", "name": "Resource 1", "data": {"key": "value1"}},
            {"id": "test-id-2", "name": "Resource 2", "data": {"key": "value2"}},
        ]
    }
    mock_client.request.return_value = search_response
    mock_client.request.side_effect = None

    # Test basic search
    search_params = {"query": "test"}
    result = ResourceModel.search(graph_id="test-graph", **search_params)
    assert isinstance(result, MockResponseModel)
    assert len(result.results) == 2

    # Test search with empty results
    mock_client.request.return_value = {"results": []}
    result = ResourceModel.search(graph_id="test-graph", query="nonexistent")
    assert isinstance(result, MockResponseModel)
    assert len(result.results) == 0


def test_model_validation(mock_client):
    """Test model validation in mixins."""
    mock_client.request.side_effect = None

    # Test invalid create data
    invalid_data = {"invalid_field": "value"}
    mock_client.request.return_value = invalid_data
    with pytest.raises(ValueError):
        ResourceModel.create(**invalid_data)

    # Test invalid search response
    mock_client.request.return_value = {"invalid_field": "value"}
    with pytest.raises(ValueError):
        ResourceModel.search(graph_id="test-graph", query="test")


def test_error_handling(mock_client):
    """Test error handling in mixins."""
    api_error = Exception("API Error")
    mock_client.request.side_effect = api_error

    with pytest.raises(Exception):
        ResourceModel.create(id="test-id")

    with pytest.raises(Exception):
        ResourceModel.retrieve("test-id")

    with pytest.raises(Exception):
        ResourceModel.update("test-id", id="test-id")

    with pytest.raises(Exception):
        ResourceModel.delete("test-id")

    with pytest.raises(Exception):
        ResourceModel.retrieve_all()

    with pytest.raises(Exception):
        ResourceModel.search(graph_id="test-graph", query="test")


def test_create_multiple_resources(mock_client):
    """Test CreateableMultipleAPIResource mixin."""
    test_data = [
        {"id": "test-id-1", "name": "Test Resource 1", "data": {"key": "value1"}},
        {"id": "test-id-2", "name": "Test Resource 2", "data": {"key": "value2"}},
    ]
    mock_client.request.return_value = test_data
    mock_client.request.side_effect = None

    # Test successful multiple creation
    result = ResourceModel.create_multiple(test_data)
    assert isinstance(result, list)
    assert len(result) == 2
    assert all(isinstance(item, MockModel) for item in result)
    assert result[0].id == test_data[0]["id"]
    assert result[1].id == test_data[1]["id"]

    # Test with empty list
    assert ResourceModel.create_multiple([]) == []

    # Test with None
    with pytest.raises(TypeError, match="Nodes parameter cannot be None"):
        ResourceModel.create_multiple(None)

    # Verify request parameters
    mock_client.request.assert_called()
    called_args = mock_client.request.call_args
    assert called_args[0][0] == "PUT"
    assert "multiple" in called_args[0][1]


def test_delete_all_resources(mock_client):
    """Test DeleteAllAPIResource mixin."""
    mock_client.request.side_effect = None

    # Test successful deletion of all resources with a valid UUID
    valid_uuid = "550e8400-e29b-41d4-a716-446655440000"
    mock_client.graph_guid = valid_uuid  # Valid UUID string
    ResourceModel.delete_all()
    mock_client.request.assert_called_once()
    called_args = mock_client.request.call_args
    assert called_args[0][0] == "DELETE"
    assert "all" in called_args[0][1]
    assert called_args[1]["headers"] == {"Content-Type": "application/json"}

    # Test without graph_guid when required (expect ValueError)
    mock_client.request.reset_mock()
    mock_client.graph_guid = None
    with pytest.raises(ValueError) as exc_info:
        ResourceModel.delete_all()

    # Match the expected error message
    assert "badly formed hexadecimal UUID string" in str(exc_info.value)

    # Test with invalid UUID format (expect ValueError due to invalid UUID string)
    mock_client.request.reset_mock()
    invalid_uuid = "invalid-uuid"
    mock_client.graph_guid = invalid_uuid
    with pytest.raises(ValueError) as exc_info:
        ResourceModel.delete_all()

    # Match the expected error message
    assert "badly formed hexadecimal UUID string" in str(exc_info.value)

    mock_client.request.reset_mock()
    mock_client.graph_guid = None
    with patch("uuid.UUID", side_effect=lambda x: x if x is not None else valid_uuid):
        TestResourceNoGraphGuid.delete_all()
    mock_client.request.assert_called_once()
    called_args = mock_client.request.call_args
    assert called_args[0][0] == "DELETE"
    assert "all" in called_args[0][1]
    assert called_args[1]["headers"] == {"Content-Type": "application/json"}


def test_delete_multiple_resources(mock_client):
    """Test DeleteMultipleAPIResource mixin."""
    mock_client.request.side_effect = None

    # Test successful multiple deletion
    resource_ids = ["test-id-1", "test-id-2"]
    ResourceModel.delete_multiple(resource_ids)
    mock_client.request.assert_called_once()
    called_args = mock_client.request.call_args
    assert called_args[0][0] == "DELETE"
    assert "multiple" in called_args[0][1]
    assert called_args[1]["headers"] == {"Content-Type": "application/json"}
    assert called_args[1]["json"] == resource_ids

    # Test with empty list
    mock_client.request.reset_mock()
    ResourceModel.delete_multiple([])
    mock_client.request.assert_not_called()

    # Test with None
    with pytest.raises(TypeError, match="Input must be a list of IDs"):
        ResourceModel.delete_multiple(None)

    # Test with invalid input type
    with pytest.raises(TypeError, match="Input must be a list of IDs"):
        ResourceModel.delete_multiple("not-a-list")

    # Test without graph_guid when required
    mock_client.request.reset_mock()
    mock_client.graph_guid = None
    with pytest.raises(ValueError, match="Graph GUID is required for this resource."):
        ResourceModel.delete_multiple(resource_ids)


def test_model_validation_for_create_multiple(mock_client):
    """Test model validation in create_multiple."""
    mock_client.request.side_effect = None

    # Test with valid data
    valid_data = [{"id": "test-id", "name": "Test Resource"}]
    mock_client.request.return_value = valid_data
    result = ResourceModel.create_multiple(valid_data)
    assert isinstance(result, list)
    assert all(isinstance(item, MockModel) for item in result)

    # Test with invalid data
    invalid_data = [{"invalid_field": "value"}]
    mock_client.request.return_value = invalid_data
    with pytest.raises(ValueError):
        ResourceModel.create_multiple(invalid_data)

    # Test with mixed valid/invalid data
    mixed_data = [
        {"id": "test-id-1"},  # valid
        {"invalid_field": "value"},  # invalid
    ]
    mock_client.request.return_value = mixed_data
    with pytest.raises(ValueError):
        ResourceModel.create_multiple(mixed_data)


def test_export_gexf_success(mock_client):
    """Test successful GEXF export."""
    # Mock successful GEXF export response
    mock_gexf_content = b'<?xml version="1.0" encoding="UTF-8"?><gexf>...</gexf>'
    mock_client.request.return_value = mock_gexf_content
    mock_client.request.side_effect = None

    # Test basic export
    result = TestExportGexf.export_gexf("test-graph-id")
    assert isinstance(result, str)
    assert "<?xml" in result
    assert "<gexf>" in result

    # Verify the request was made correctly
    mock_client.request.assert_called_once()
    called_args = mock_client.request.call_args
    assert called_args[0][0] == "GET"
    assert "export" in called_args[0][1]
    assert "gexf" in called_args[0][1]


def test_export_gexf_with_params(mock_client):
    """Test GEXF export with additional parameters."""
    mock_gexf_content = b'<?xml version="1.0" encoding="UTF-8"?><gexf>...</gexf>'
    mock_client.request.return_value = mock_gexf_content

    # Test export with additional parameters
    params = {"format": "pretty", "version": "1.2"}
    result = TestExportGexf.export_gexf("test-graph-id", **params)
    assert isinstance(result, str)

    # Verify parameters were included in the URL
    called_args = mock_client.request.call_args
    assert "format=pretty" in called_args[0][1]
    assert "version=1.2" in called_args[0][1]


def test_export_gexf_error_handling(mock_client):
    """Test error handling during GEXF export."""
    # Test with invalid UTF-8 response
    mock_client.request.return_value = b'\x80invalid'

    with pytest.raises(SdkException, match="Error exporting GEXF"):
        TestExportGexf.export_gexf("test-graph-id")

    # Test with missing tenant GUID
    mock_client.tenant_guid = None
    with pytest.raises(ValueError, match="Tenant GUID is required for this resource"):
        TestExportGexf.export_gexf("test-graph-id")
