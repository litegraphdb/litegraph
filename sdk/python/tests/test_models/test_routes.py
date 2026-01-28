from datetime import datetime, timezone
from unittest.mock import Mock, patch

import pytest
from litegraph_sdk.models.route_response import RouteResultModel, Timestamp


class MockBaseClient:
    def __init__(self, base_url, graph_guid, tenant_guid):
        self.base_url = base_url
        self.graph_guid = graph_guid
        self.tenant_guid = tenant_guid
        self._request = Mock()

    def request(self, method, url, **kwargs):
        return self._request(method, url, **kwargs)


@pytest.fixture
def mock_base_client():
    client = MockBaseClient(
        base_url="https://test.api.com",
        graph_guid="test-graph-guid",
        tenant_guid="test-tenant-guid",
    )
    return client


@pytest.fixture(autouse=True)
def mock_configuration(mock_base_client):
    with patch("litegraph_sdk.configuration._client", mock_base_client), patch(
        "litegraph_sdk.configuration.get_client", return_value=mock_base_client
    ):
        yield mock_base_client


@pytest.fixture
def routes_class():
    from litegraph_sdk.resources.routes import Routes

    return Routes


@pytest.fixture
def mock_response_data():
    return {
        "Timestamp": {
            "Start": datetime.now(timezone.utc).isoformat(),
            "End": datetime.now(timezone.utc).isoformat(),
            "TotalMs": 150.5,
            "Messages": {},
        },
        "Routes": [
            {
                "TotalCost": 10.5,
                "Edges": [{"lat": 1.0, "lng": 1.0}, {"lat": 2.0, "lng": 2.0}],
            }
        ],
    }


@pytest.fixture
def route_request_data():
    return {"origin": "New York", "destination": "Boston"}


class TestRoutesClass:
    @patch("litegraph_sdk.utils.url_helper._get_url")
    def test_routes_basic_functionality(
        self,
        mock_get_url,
        routes_class,
        mock_response_data,
        route_request_data,
        mock_configuration,
    ):
        """Test basic route calculation functionality"""
        mock_get_url.return_value = "/v1.0/graphs/test-graph-guid/routes"
        mock_configuration._request.return_value = mock_response_data

        result = routes_class.routes("test-graph-guid", **route_request_data)

        assert isinstance(result, RouteResultModel)
        assert isinstance(result.Timestamp, Timestamp)
        assert isinstance(result.Routes, list)
        assert len(result.Routes) == 1
        assert result.Routes[0].TotalCost == 10.5
        assert isinstance(result.Routes[0].Edges, list)
        assert len(result.Routes[0].Edges) == 2

    @patch("litegraph_sdk.utils.url_helper._get_url")
    def test_routes_with_no_graph_guid(
        self, mock_get_url, routes_class, mock_response_data, mock_configuration
    ):
        """Test route calculation when no graph GUID is required"""
        routes_class.REQUIRE_GRAPH_GUID = False
        mock_configuration.graph_guid = None
        mock_get_url.return_value = "/v1.0/routes"
        mock_configuration._request.return_value = mock_response_data

        result = routes_class.routes("test-graph-guid", origin="A", destination="B")

        if routes_class.REQUIRE_GRAPH_GUID:
            mock_get_url.assert_called_once_with(routes_class, "test-graph-guid")
        else:
            assert mock_get_url.call_count == 0

        assert isinstance(result, RouteResultModel)
        assert isinstance(result.Timestamp, Timestamp)
        assert len(result.Routes) > 0
        assert result.Routes[0].TotalCost == 10.5
        assert isinstance(result.Routes[0].Edges, list)
