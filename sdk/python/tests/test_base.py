import json
from unittest.mock import Mock, patch

import httpx
import pytest
from litegraph_sdk.base import BaseClient
from litegraph_sdk.enums.api_error_enum import ApiError_Enum
from litegraph_sdk.exceptions import SdkException, ServerError


@pytest.fixture
def base_url():
    return "http://test-api.com"


@pytest.fixture
def graph_guid():
    return "test-graph-id"


@pytest.fixture
def base_client(base_url, graph_guid):
    """Create a base client for testing."""
    with patch("httpx.Client"):
        return BaseClient(
            base_url=base_url,
            tenant_guid="test-tenant-guid",
            graph_guid=graph_guid,
        )


@pytest.fixture
def success_response():
    """Create a success response dictionary."""
    return {"data": "test"}


@pytest.fixture
def mock_httpx_client(monkeypatch):
    """Create a mock httpx client that properly handles requests."""

    def mock_request(*args, **kwargs):
        mock_response = Mock(spec=httpx.Response)
        mock_response.status_code = 200
        mock_response.content = b'{"data": "test"}'
        mock_response.json.return_value = {"data": "test"}
        mock_response.raise_for_status.return_value = None
        return mock_response

    mock_client = Mock(spec=httpx.Client)
    mock_client.request = mock_request
    mock_client.close.return_value = None

    def mock_client_constructor(*args, **kwargs):
        return mock_client

    monkeypatch.setattr(httpx, "Client", mock_client_constructor)
    return mock_client


@pytest.fixture
def mock_error_response():
    """Create a mock error response."""

    def create_error(error_type: str, description: str, status_code: int):
        mock_response = Mock(spec=httpx.Response)
        mock_response.status_code = status_code
        mock_response.json.return_value = {
            "Error": error_type,
            "Description": description,
        }
        return mock_response

    return create_error


def test_client_initialization(base_url):
    """Test client initialization with different parameters."""
    with patch("httpx.Client"):
        # Test default values
        client = BaseClient(base_url=base_url, tenant_guid="test-tenant-guid")
        assert client.base_url == base_url
        assert client.graph_guid is None
        assert client.timeout == 10
        assert client.retries == 3
        assert client.tenant_guid == "test-tenant-guid"
        # Test custom values
        custom_client = BaseClient(
            base_url=base_url, graph_guid="custom-guid", timeout=20, retries=5, tenant_guid="test-tenant-guid"
        )
        assert custom_client.base_url == base_url
        assert custom_client.graph_guid == "custom-guid"
        assert custom_client.timeout == 20
        assert custom_client.retries == 5


def test_successful_request(base_client, monkeypatch):
    """Test successful request with JSON response."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b'{"data": "test"}'
    mock_response.json.return_value = {"data": "test"}
    mock_response.raise_for_status.return_value = None

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response == {"data": "test"}


def test_empty_response(base_client, monkeypatch):
    """Test successful request with empty response."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b""
    mock_response.raise_for_status.return_value = None

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response is None


def test_request_with_headers_and_params(base_client, monkeypatch):
    """Test request with both headers and query parameters."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b'{"data": "test"}'
    mock_response.json.return_value = {"data": "test"}
    mock_response.raise_for_status.return_value = None

    mock_request = Mock(return_value=mock_response)
    with patch.object(base_client.client, "request", mock_request):
        headers = {"Content-Type": "application/json", "Authorization": "Bearer token"}
        params = {"filter": "value"}
        base_client.request("GET", "/test", headers=headers, params=params)

        mock_request.assert_called_once_with(
            "GET", "/test", headers=headers, params=params
        )


def test_conflict_error(base_client, monkeypatch):
    """Test handling of conflict error."""
    error_response = {
        "Error": ApiError_Enum.conflict,
        "Description": "Operation failed as it would create a conflict with an existing resource.",
    }
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 409
    mock_response.json.return_value = error_response
    mock_response.raise_for_status.side_effect = httpx.HTTPStatusError(
        "409 Conflict", request=Mock(spec=httpx.Request), response=mock_response
    )
    mock_response.headers = {"Content-Type": "application/json"}

    with patch.object(base_client.client, "request") as mock_request:
        mock_request.side_effect = httpx.HTTPStatusError(
            "409 Conflict", request=Mock(spec=httpx.Request), response=mock_response
        )
        with pytest.raises(SdkException) as exc_info:
            base_client.request("POST", "/test")
        assert error_response["Description"] in str(exc_info.value)


def test_server_error(base_client, monkeypatch):
    """Test handling of server error."""
    error_response = {
        "Error": "InternalError",
        "Description": "Internal server error occurred",
    }
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 500
    mock_response.headers = {"Content-Type": "application/json"}
    mock_response.json.return_value = error_response
    mock_response.raise_for_status.side_effect = httpx.HTTPStatusError(
        "500 Internal Server Error",
        request=Mock(spec=httpx.Request),
        response=mock_response,
    )

    with patch.object(
        base_client.client,
        "request",
        side_effect=httpx.HTTPStatusError(
            "500 Internal Server Error",
            request=Mock(spec=httpx.Request),
            response=mock_response,
        ),
    ):
        with pytest.raises(ServerError):
            base_client.request("GET", "/test")


def test_request_with_malformed_error_response(base_client, monkeypatch):
    """Test handling of malformed error response."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 400
    mock_response.headers = {"Content-Type": "application/json"}
    mock_response.json.return_value = {"InvalidFormat": "Missing Error field"}
    mock_response.raise_for_status.side_effect = httpx.HTTPStatusError(
        "400 Bad Request", request=Mock(spec=httpx.Request), response=mock_response
    )

    with patch.object(base_client.client, "request") as mock_request:
        mock_request.side_effect = httpx.HTTPStatusError(
            "400 Bad Request", request=Mock(spec=httpx.Request), response=mock_response
        )
        with pytest.raises(SdkException) as exc_info:
            base_client.request("GET", "/test")
        assert "Unexpected error" in str(exc_info.value)


def test_request_with_retry_success(base_client, monkeypatch):
    """Test request that succeeds after retries."""
    success_response = Mock(spec=httpx.Response)
    success_response.status_code = 200
    success_response.content = b'{"data": "success"}'
    success_response.json.return_value = {"data": "success"}
    success_response.raise_for_status.return_value = None

    mock_request = Mock(
        side_effect=[httpx.RequestError("First attempt failed"), success_response]
    )

    with patch.object(base_client.client, "request", mock_request):
        response = base_client.request("GET", "/test")
        assert response == {"data": "success"}
        assert mock_request.call_count == 2


def test_client_close(base_client):
    """Test client close method."""
    mock_close = Mock()
    base_client.client.close = mock_close
    base_client.close()
    mock_close.assert_called_once()


def test_request_with_network_error(base_client, monkeypatch):
    """Test handling of network error."""
    with patch.object(
        base_client.client, "request", side_effect=httpx.NetworkError("Network error")
    ):
        with pytest.raises(SdkException) as exc_info:
            base_client.request("GET", "/test")
        assert "Request failed after 3 attempts" in str(exc_info.value)


def test_request_with_timeout_error(base_client, monkeypatch):
    """Test handling of timeout error."""
    with patch.object(
        base_client.client,
        "request",
        side_effect=httpx.TimeoutException("Request timed out"),
    ):
        with pytest.raises(SdkException) as exc_info:
            base_client.request("GET", "/test")
        assert "Request failed after 3 attempts" in str(exc_info.value)


def test_request_with_different_http_methods(base_client, monkeypatch):
    """Test requests with different HTTP methods."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b'{"data": "test"}'
    mock_response.json.return_value = {"data": "test"}
    mock_response.raise_for_status.return_value = None
    mock_response.headers = {"Content-Type": "application/json"}

    mock_request = Mock(return_value=mock_response)
    with patch.object(base_client.client, "request", mock_request):
        methods = ["GET", "POST", "PUT", "DELETE", "PATCH"]
        for method in methods:
            base_client.request(method, "/test")
            mock_request.assert_called_with(method, "/test", headers={"Content-Type": "application/json"})


def test_request_with_complex_json(base_client, monkeypatch):
    """Test request with complex JSON payload."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b'{"data": "test"}'
    mock_response.json.return_value = {"data": "test"}
    mock_response.raise_for_status.return_value = None
    mock_response.headers = {"Content-Type": "application/json"}
    mock_request = Mock(return_value=mock_response)
    with patch.object(base_client.client, "request", mock_request):
        complex_payload = {
            "nested": {"array": [1, 2, 3], "object": {"key": "value"}},
            "list": ["item1", "item2"],
            "null": None,
        }

        base_client.request("POST", "/test", json=complex_payload)
        mock_request.assert_called_once_with(
            "POST", "/test", headers={"Content-Type": "application/json"}, json=complex_payload
        )


def test_empty_json_response(base_client, monkeypatch):
    """Test handling of empty JSON response."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b"{}"
    mock_response.json.return_value = {}
    mock_response.raise_for_status.return_value = None

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response == {}


def test_none_json_response(base_client, monkeypatch):
    """Test handling of None JSON response."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b"null"
    mock_response.json.return_value = None
    mock_response.raise_for_status.return_value = None

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response is None


def test_request_with_unicode_json(base_client, monkeypatch):
    """Test handling of Unicode JSON response."""
    test_data = {"message": "测试消息"}  # Unicode content
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = json.dumps(test_data).encode("utf-8")
    mock_response.json.return_value = test_data
    mock_response.raise_for_status.return_value = None

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response == test_data


def test_request_with_large_json(base_client, monkeypatch):
    """Test handling of large JSON response."""
    large_data = {"items": [{"id": i, "data": "x" * 1000} for i in range(100)]}
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = json.dumps(large_data).encode("utf-8")
    mock_response.json.return_value = large_data
    mock_response.raise_for_status.return_value = None

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response == large_data


def test_handle_non_json_response(base_client, monkeypatch):
    """Test handling of non-JSON response content."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b"Plain text response"
    mock_response.raise_for_status.return_value = None
    mock_response.json.side_effect = json.JSONDecodeError("Invalid JSON", "", 0)

    with patch.object(base_client.client, "request", return_value=mock_response):
        response = base_client.request("GET", "/test")
        assert response == b"Plain text response"


def test_handle_non_json_error_response(base_client, monkeypatch):
    """Test handling of non-JSON error response."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 500
    mock_response.content = b"Internal Server Error"
    mock_response.headers = {"Content-Type": "text/plain"}
    mock_response.raise_for_status.side_effect = httpx.HTTPStatusError(
        "500 Internal Server Error",
        request=Mock(spec=httpx.Request),
        response=mock_response,
    )

    with patch.object(base_client.client, "request") as mock_request:
        mock_request.side_effect = httpx.HTTPStatusError(
            "500 Internal Server Error",
            request=Mock(spec=httpx.Request),
            response=mock_response,
        )
        with pytest.raises(SdkException) as exc_info:
            base_client.request("GET", "/test")
        assert "Server responded with non-JSON content" in str(exc_info.value)


def test_request_with_access_key(base_client, monkeypatch):
    """Test request with access key in headers."""
    mock_response = Mock(spec=httpx.Response)
    mock_response.status_code = 200
    mock_response.content = b'{"data": "test"}'
    mock_response.json.return_value = {"data": "test"}
    mock_response.raise_for_status.return_value = None

    # Set access key
    base_client.access_key = "test-access-key"

    mock_request = Mock(return_value=mock_response)
    with patch.object(base_client.client, "request", mock_request):
        base_client.request("GET", "/test")

        # Verify headers contain Authorization
        mock_request.assert_called_once()
        call_kwargs = mock_request.call_args[1]
        assert call_kwargs["headers"]["Authorization"] == "Bearer test-access-key"


def test_request_max_retries_exhausted(base_client, monkeypatch):
    """Test request fails after max retries with RequestError."""
    with patch.object(
        base_client.client,
        "request",
        side_effect=httpx.RequestError("Connection failed")
    ):
        with pytest.raises(SdkException) as exc_info:
            base_client.request("GET", "/test")
        assert f"Request failed after {base_client.retries} attempts" in str(exc_info.value)
