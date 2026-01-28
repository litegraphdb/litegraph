from datetime import datetime, timezone
from unittest.mock import Mock, patch

import httpx
import pytest
from litegraph_sdk.configuration import configure
from litegraph_sdk.exceptions import BadRequestError, ResourceNotFoundError
from litegraph_sdk.models.edge import EdgeModel
from litegraph_sdk.resources.routes_between import RouteEdges


# Fixtures
@pytest.fixture(autouse=True)
def mock_http_client():
    """Mock the HTTP client to prevent actual network calls"""
    with patch("httpx.Client") as mock:
        client_instance = Mock()
        mock.return_value = client_instance
        yield client_instance


@pytest.fixture(autouse=True)
def setup_sdk():
    """Configure the SDK with a mock client for each test"""
    configure(
        endpoint="https://test.com",
        tenant_guid="test-tenant-guid",
        graph_guid="test-graph-guid",
    )
    yield
    # Clean up after test
    global _client
    _client = None


@pytest.fixture
def sample_edges_response():
    """Sample response for edges between nodes"""
    return [
        {
            "GUID": "edge-1",
            "GraphGUID": "test-graph-guid",
            "Name": "Edge 1",
            "From": "node-1",
            "To": "node-2",
            "Cost": 5,
            "CreatedUtc": datetime.now(timezone.utc).isoformat(),
            "Data": {"key": "value1"},
        },
        {
            "GUID": "edge-2",
            "GraphGUID": "test-graph-guid",
            "Name": "Edge 2",
            "From": "node-2",
            "To": "node-3",
            "Cost": 3,
            "CreatedUtc": datetime.now(timezone.utc).isoformat(),
            "Data": {"key": "value2"},
        },
    ]


def test_between_successful(mock_http_client, sample_edges_response):
    """Test successful retrieval of edges between nodes"""
    # Arrange
    mock_response = Mock()
    mock_response.status_code = 200
    mock_response.json.return_value = sample_edges_response
    mock_http_client.request.return_value = mock_response

    # Act
    result = RouteEdges.between(
        graph_guid="test-graph-guid", from_node_guid="node-1", to_node_guid="node-3"
    )

    # Assert
    assert isinstance(result, list)
    assert len(result) == 2
    assert all(isinstance(edge, EdgeModel) for edge in result)
    assert result[0].guid == "edge-1"
    assert result[1].guid == "edge-2"

    # Verify the request was made correctly
    mock_http_client.request.assert_called_once()
    called_args = mock_http_client.request.call_args
    assert called_args[0][0] == "GET"  # HTTP method
    assert "between" in called_args[0][1]  # URL contains 'between'
    assert "from=node-1" in called_args[0][1]  # URL contains from parameter
    assert "to=node-3" in called_args[0][1]  # URL contains to parameter


def test_between_empty_response(mock_http_client):
    """Test when no edges are found between nodes"""
    # Arrange
    mock_response = Mock()
    mock_response.status_code = 200
    mock_response.json.return_value = []
    mock_http_client.request.return_value = mock_response

    # Act
    result = RouteEdges.between(
        graph_guid="test-graph-guid", from_node_guid="node-1", to_node_guid="node-2"
    )

    # Assert
    assert isinstance(result, list)
    assert len(result) == 0


def test_between_invalid_graph(mock_http_client):
    """Test with invalid graph ID"""
    # Arrange
    mock_response = Mock()
    mock_response.status_code = 404
    mock_response.json.return_value = {
        "Error": "NotFound",
        "Description": "Graph not found",
    }
    mock_response.raise_for_status.side_effect = httpx.HTTPStatusError(
        "Not Found", request=Mock(), response=mock_response
    )
    mock_http_client.request.side_effect = ResourceNotFoundError("Graph not found")

    # Act & Assert
    with pytest.raises(ResourceNotFoundError):
        RouteEdges.between(
            graph_guid="invalid-graph", from_node_guid="node-1", to_node_guid="node-2"
        )


def test_between_invalid_node_guids(mock_http_client):
    """Test with invalid node GUIDs"""
    # Arrange
    mock_response = Mock()
    mock_response.status_code = 400
    mock_response.json.return_value = {
        "Error": "BadRequest",
        "Description": "Invalid node GUIDs",
    }
    mock_response.raise_for_status.side_effect = httpx.HTTPStatusError(
        "Bad Request", request=Mock(), response=mock_response
    )
    mock_http_client.request.side_effect = BadRequestError("Invalid node GUIDs")

    # Act & Assert
    with pytest.raises(BadRequestError):
        RouteEdges.between(
            graph_guid="test-graph-guid",
            from_node_guid="invalid-node",
            to_node_guid="invalid-node",
        )


def test_between_url_construction(mock_http_client):
    """Test proper URL construction"""
    # Arrange
    mock_response = Mock()
    mock_response.status_code = 200
    mock_response.json.return_value = []
    mock_http_client.request.return_value = mock_response

    # Act
    RouteEdges.between(
        graph_guid="test-graph-guid", from_node_guid="node-1", to_node_guid="node-2"
    )

    # Assert
    mock_http_client.request.assert_called_once()
    called_args = mock_http_client.request.call_args
    assert called_args[0][0] == "GET"
    assert "between" in called_args[0][1]
    assert "test-graph-guid" in called_args[0][1]
    assert "from=node-1" in called_args[0][1]  # Check params in URL
    assert "to=node-2" in called_args[0][1]  # Check params in URL


def test_between_missing_required_params(mock_http_client):
    """Test with missing required parameters"""
    # Act & Assert
    with pytest.raises(TypeError):
        RouteEdges.between(graph_guid="test-graph-guid")


def test_between_response_validation(mock_http_client):
    """Test with invalid response data structure"""
    # Arrange
    mock_response = Mock()
    mock_response.status_code = 200
    # Missing required fields for EdgeModel
    mock_response.json.return_value = [
        {
            "GUID": "edge-1",
            # Missing GraphGUID
            "From": "node-1",
            # Missing To
            "Cost": "invalid-cost",  # Invalid type for Cost (should be int)
            # Missing CreatedUtc
            "Data": {"key": "value1"},
        }
    ]
    mock_http_client.request.return_value = mock_response

    # Act & Assert
    with pytest.raises(ValueError):
        RouteEdges.between(
            graph_guid="test-graph-guid", from_node_guid="node-1", to_node_guid="node-2"
        )
