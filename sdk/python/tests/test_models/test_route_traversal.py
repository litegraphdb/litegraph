from datetime import datetime, timezone

import pytest
from litegraph_sdk.models.edge import EdgeModel
from litegraph_sdk.models.node import NodeModel
from litegraph_sdk.resources.route_traversal import RouteNodes
from pydantic import ValidationError


# Create test class with required graph_guid
class TestRoutesWithGraphGuid(RouteNodes):
    REQUIRE_GRAPH_GUID = True
    REQUIRE_TENANT = True

@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client for testing."""

    class MockClient:
        def __init__(self):
            self.graph_guid = None  # Initialize with None
            self.base_url = "http://test-api.com"

        def request(self, method, url):
            if self.graph_guid is None and TestRoutesWithGraphGuid.REQUIRE_GRAPH_GUID:
                raise ValueError("Graph Id is required for this operation")
            return []

    client = MockClient()
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_get_edges_from(mock_client, monkeypatch):
    """Test getting edges from a node with missing graph_guid."""
    # Ensure we're using TestRoutesWithGraphGuid
    monkeypatch.setattr(TestRoutesWithGraphGuid, "REQUIRE_GRAPH_GUID", True)

    # Ensure graph_guid is None
    mock_client.graph_guid = None

    # This should raise ValueError
    with pytest.raises(ValueError, match="Graph Id is required for this operation"):
        TestRoutesWithGraphGuid.get_edges_from("test-graph-guid", "test-node")


def test_model_validation_edges(mock_client):
    """Test model validation for edge responses."""

    def mock_request(method, url):
        # Return data that will fail EdgeModel validation
        return [
            {
                "GUID": None,  # Invalid: GUID cannot be None
                "GraphGUID": "",  # Invalid: GraphGUID cannot be empty
                "Name": None,
                "From": None,  # Invalid: From cannot be None
                "To": None,  # Invalid: To cannot be None
                "Cost": "invalid",  # Invalid: Cost must be integer
                "CreatedUtc": "invalid-date",  # Invalid: CreatedUtc must be valid datetime
                "Data": None,
            }
        ]

    mock_client.request = mock_request
    mock_client.graph_guid = "test-graph-guid"

    with pytest.raises(ValidationError):
        RouteNodes.get_edges_from("test-graph-guid", "test-node")


def test_model_validation_nodes(mock_client):
    """Test model validation for node responses."""

    def mock_request(method, url):
        # Return data that will fail NodeModel validation
        return [
            {
                "GUID": None,  # Invalid: GUID cannot be None
                "GraphGUID": "",  # Invalid: GraphGUID cannot be empty
                "Name": None,
                "CreatedUtc": "invalid-date",  # Invalid: CreatedUtc must be valid datetime
                "Data": "invalid-data",  # Invalid: Data must be dict or None
            }
        ]

    mock_client.request = mock_request
    mock_client.graph_guid = "test-graph-guid"

    with pytest.raises(ValidationError):
        RouteNodes.parents("test-graph-guid", "test-node")


def test_edges(mock_client, sample_edge_data):
    """Test getting all edges for a node."""

    def mock_request(method, url):
        assert method == "GET"
        return [sample_edge_data]

    mock_client.request = mock_request
    mock_client.graph_guid = "test-graph-guid"

    # Test successful case
    result = RouteNodes.edges("test-graph-guid", "test-node")
    assert isinstance(result, list)
    assert len(result) == 1
    assert isinstance(result[0], EdgeModel)
    assert result[0].guid == sample_edge_data["GUID"]


def test_parents(mock_client, sample_node_data):
    """Test getting parent nodes."""

    def mock_request(method, url):
        assert method == "GET"
        return [sample_node_data]

    mock_client.request = mock_request
    mock_client.graph_guid = "test-graph-guid"

    # Test successful case
    result = RouteNodes.parents("test-graph-guid", "test-node")
    assert isinstance(result, list)
    assert len(result) == 1
    assert isinstance(result[0], NodeModel)
    assert result[0].guid == sample_node_data["GUID"]


def test_empty_responses(mock_client):
    """Test handling of empty responses."""

    def mock_request(method, url):
        return []

    mock_client.request = mock_request
    mock_client.graph_guid = "test-graph-guid"

    # Test various methods with empty responses
    assert RouteNodes.get_edges_from("test-graph-guid", "test-node") == []
    assert RouteNodes.get_edges_to("test-graph-guid", "test-node") == []
    assert RouteNodes.edges("test-graph-guid", "test-node") == []
    assert RouteNodes.parents("test-graph-guid", "test-node") == []
    assert RouteNodes.children("test-graph-guid", "test-node") == []
    assert RouteNodes.neighbors("test-graph-guid", "test-node") == []
    assert RouteNodes.between("test-graph-guid", "test-node") == []


def test_error_handling(mock_client):
    """Test error handling in routes."""

    def mock_request(method, url):
        raise Exception("API Error")

    mock_client.request = mock_request
    mock_client.graph_guid = "test-graph-guid"

    with pytest.raises(Exception) as exc:
        RouteNodes.get_edges_from("test-graph-guid", "test-node")
    assert str(exc.value) == "API Error"


@pytest.fixture
def sample_edge_data():
    """Sample edge data for testing."""
    return {
        "GUID": "edge-123",
        "GraphGUID": "test-graph-guid",
        "Name": "Test Edge",
        "From": "node-1",
        "To": "node-2",
        "Cost": 10,
        "CreatedUtc": datetime.now(timezone.utc).isoformat(),
        "Data": {"key": "value"},
    }


@pytest.fixture
def sample_node_data():
    """Sample node data for testing."""
    return {
        "GUID": "node-123",
        "GraphGUID": "test-graph-guid",
        "Name": "Test Node",
        "CreatedUtc": datetime.now(timezone.utc).isoformat(),
        "Data": {"key": "value"},
    }
