import sys

import pytest
from litegraph_sdk.base import BaseClient
from litegraph_sdk.configuration import configure, get_client


@pytest.fixture(autouse=True)
def reset_client():
    """Fixture to reset the global client before and after each test."""
    # Store the original client
    original_client = sys.modules["litegraph_sdk.configuration"]._client

    # Reset client to None
    sys.modules["litegraph_sdk.configuration"]._client = None

    yield

    # Restore the original state after the test
    sys.modules["litegraph_sdk.configuration"]._client = original_client


def test_configure_with_endpoint_only():
    """Test configuration with only endpoint provided."""
    endpoint = "http://test-api.com"
    configure(endpoint=endpoint, tenant_guid="test-tenant-guid")

    client = get_client()
    assert isinstance(client, BaseClient)
    assert client.base_url == endpoint
    assert client.graph_guid is None
    assert client.tenant_guid == "test-tenant-guid"

def test_configure_with_endpoint_and_graph_guid():
    """Test configuration with both endpoint and graph_guid."""
    endpoint = "http://test-api.com"
    graph_guid = "test-graph-id"
    configure(endpoint=endpoint, graph_guid=graph_guid, tenant_guid="test-tenant-guid")

    client = get_client()
    assert isinstance(client, BaseClient)
    assert client.base_url == endpoint
    assert client.graph_guid == graph_guid
    assert client.tenant_guid == "test-tenant-guid"

def test_reconfigure_client():
    """Test that reconfiguring client creates new instance."""
    configure(endpoint="http://test-api-1.com", tenant_guid="test-tenant-guid")
    first_client = get_client()

    configure(endpoint="http://test-api-2.com", tenant_guid="test-tenant-guid")
    second_client = get_client()

    assert first_client is not second_client
    assert first_client.base_url != second_client.base_url


def test_client_singleton():
    """Test that get_client returns the same instance."""
    configure(endpoint="http://test-api.com", tenant_guid="test-tenant-guid")

    client1 = get_client()
    client2 = get_client()

    assert client1 is client2


@pytest.mark.parametrize(
    "endpoint,graph_guid",
    [
        ("http://test-api.com", None),
        ("http://test-api.com/", "test-guid"),
        ("https://test-api.com", ""),
        ("http://localhost:8080", "test-guid"),
    ],
)
def test_valid_configuration_combinations(endpoint, graph_guid):
    """Test various valid combinations of configuration parameters."""
    configure(endpoint=endpoint, graph_guid=graph_guid, tenant_guid="test-tenant-guid")
    client = get_client()

    assert client.base_url == endpoint
    assert client.graph_guid == graph_guid


def test_configure_maintains_default_client_settings():
    """Test that configured client maintains default BaseClient settings."""
    configure(endpoint="http://test-api.com", tenant_guid="test-tenant-guid")
    client = get_client()

    assert client.timeout == 10
    assert client.retries == 3


def test_multiple_rapid_configurations():
    """Test multiple rapid configurations."""
    endpoints = [
        "http://test-api-1.com",
        "http://test-api-2.com",
        "http://test-api-3.com",
    ]

    for endpoint in endpoints:
        configure(endpoint=endpoint, tenant_guid="test-tenant-guid")
        client = get_client()
        assert client.base_url == endpoint


def test_empty_graph_guid_handling():
    """Test handling of empty graph_guid values."""
    configure(endpoint="http://test-api.com", graph_guid="", tenant_guid="test-tenant-guid")
    client = get_client()
    assert client.graph_guid == ""

    configure(endpoint="http://test-api.com", graph_guid=None, tenant_guid="test-tenant-guid")
    client = get_client()
    assert client.graph_guid is None


def test_client_attribute_access():
    """Test client attribute access."""
    configure(endpoint="http://test-api.com", graph_guid="test-guid", tenant_guid="test-tenant-guid")
    client = get_client()

    assert client.base_url == "http://test-api.com"
    assert client.graph_guid == "test-guid"
    assert client.timeout == 10
    assert client.retries == 3


def test_get_client_without_configuration():
    """Test that get_client raises ValueError when client is not configured."""
    # Import the module directly to access _client
    import litegraph_sdk.configuration as config

    # Explicitly set _client to None
    config._client = None

    # Assert that get_client raises ValueError
    with pytest.raises(ValueError) as exc_info:
        get_client()
    assert str(exc_info.value) == "SDK is not configured. Call 'configure' first."


def test_configure_with_special_characters():
    """Test configuration with special characters in endpoint and graph_guid."""
    endpoint = "http://test-api.com/path-with-special/chars"
    graph_guid = "guid-with-special-chars-123"

    configure(endpoint=endpoint, graph_guid=graph_guid, tenant_guid="test-tenant-guid")
    client = get_client()

    assert client.base_url == endpoint
    assert client.graph_guid == graph_guid


def test_configure_thread_safety():
    """Test configuration in multi-threaded environment."""
    import queue
    import threading

    results = queue.Queue()

    def configure_and_verify(endpoint):
        configure(endpoint=endpoint, tenant_guid="test-tenant-guid")
        client = get_client()
        results.put((endpoint, client.base_url))

    threads = []
    endpoints = [f"http://test-api-{i}.com" for i in range(5)]

    for endpoint in endpoints:
        thread = threading.Thread(target=configure_and_verify, args=(endpoint,))
        threads.append(thread)
        thread.start()

    for thread in threads:
        thread.join()

    while not results.empty():
        endpoint, client_url = results.get()
        assert endpoint == client_url


def test_client_cleanup():
    """Test client cleanup on reconfiguration."""
    configure(endpoint="http://test-api-1.com", tenant_guid="test-tenant-guid")
    first_client = get_client()

    # Force client to close
    first_client.close()

    # Reconfigure and get new client
    configure(endpoint="http://test-api-2.com", tenant_guid="test-tenant-guid")
    second_client = get_client()

    assert first_client is not second_client
    assert first_client.base_url != second_client.base_url


def test_empty_string_endpoint():
    """Test configuration with empty string endpoint."""
    configure(endpoint="", tenant_guid="test-tenant-guid")
    client = get_client()
    assert client.base_url == ""


def test_configure_without_tenant_guid():
    """Test configuration fails when tenant_guid is None."""
    with pytest.raises(ValueError) as exc_info:
        configure(endpoint="http://test-api.com", tenant_guid=None)
    assert str(exc_info.value) == "Tenant GUID is required"
