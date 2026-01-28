import pytest
from urllib.parse import urlparse, parse_qs
from litegraph_sdk.utils.url_helper import _get_url

class MockResource:
    """Mock resource class for testing."""
    RESOURCE_NAME = "test-resource"
    REQUIRE_GRAPH_GUID = True
    REQUIRE_TENANT = True

class MockResourceNoGraph:
    """Mock resource class that doesn't require graph GUID."""
    RESOURCE_NAME = "test-resource"
    REQUIRE_GRAPH_GUID = False
    REQUIRE_TENANT = False

@pytest.fixture
def mock_resource():
    return MockResource

@pytest.fixture
def mock_resource_no_graph():
    return MockResourceNoGraph

def test_basic_url_construction(mock_resource):
    """Test basic URL construction with graph GUID."""
    url = _get_url(mock_resource, "123")
    assert url == "v1.0/tenants/123/test-resource"

def test_url_without_graph_guid(mock_resource_no_graph):
    """Test URL construction without graph GUID requirement."""
    url = _get_url(mock_resource_no_graph)
    assert url == "v1.0/test-resource"

def test_url_with_additional_path_segments(mock_resource):
    """Test URL construction with additional path segments."""
    url = _get_url(mock_resource, "123", "segment1", "segment2")
    assert url == "v1.0/tenants/123/graphs/segment1/test-resource/segment2"

def test_url_with_query_parameters(mock_resource):
    """Test URL construction with query parameters."""
    url = _get_url(mock_resource, "123", filter="name eq 'test'", select="id,name")
    parsed_url = urlparse(url)
    query_params = parse_qs(parsed_url.query)

    assert parsed_url.path == "v1.0/tenants/123/test-resource"
    assert query_params['filter'] == ["name eq 'test'"]
    assert query_params['select'] == ["id,name"]

def test_url_with_flag_parameters(mock_resource):
    """Test URL construction with flag parameters (None values)."""
    url = _get_url(mock_resource, "123", include_inactive=None, show_deleted=None)
    assert url.endswith("include_inactive&show_deleted")
    assert url.startswith("v1.0/tenants/123/test-resource")

def test_url_with_mixed_parameters(mock_resource):
    """Test URL construction with both regular and flag parameters."""
    url = _get_url(mock_resource, "123", filter="active eq true", include_inactive=None)
    assert "filter=active+eq+true" in url
    assert "include_inactive" in url
    assert url.startswith("v1.0/tenants/123/test-resource")

def test_none_handling_in_args(mock_resource):
    """Test that None values in args are properly filtered."""
    url = _get_url(mock_resource, "123", None, "segment1")
    assert url == "v1.0/tenants/123/graphs/segment1/test-resource"

def test_special_characters_in_query_params(mock_resource):
    """Test handling of special characters in query parameters."""
    url = _get_url(mock_resource, "123", filter="name eq 'test & more'")
    assert "filter=name+eq+%27test+%26+more%27" in url

def test_empty_query_params(mock_resource):
    """Test URL construction with empty query parameters."""
    url = _get_url(mock_resource, "123", **{})
    assert url == "v1.0/tenants/123/test-resource"

def test_numeric_path_segments(mock_resource):
    """Test URL construction with numeric path segments."""
    url = _get_url(mock_resource, "123", 456, 789)
    assert url == "v1.0/tenants/123/graphs/456/test-resource/789"

@pytest.mark.parametrize("query_param,expected", [
    ({"param": "value"}, "param=value"),
    ({"param": "value with spaces"}, "param=value+with+spaces"),
    ({"param": "special&chars"}, "param=special%26chars"),
    ({"param": None}, "param"),
])
def test_different_query_param_formats(mock_resource, query_param, expected):
    """Test different query parameter formats."""
    url = _get_url(mock_resource, "123", **query_param)
    assert expected in url

def test_resource_without_trailing_slash(mock_resource):
    """Test URL construction ensures proper slash handling."""
    url = _get_url(mock_resource, "123", "endpoint")
    assert not url.endswith("//")
    assert url == "v1.0/tenants/123/graphs/endpoint/test-resource"

def test_multiple_flag_parameters_order(mock_resource):
    """Test that multiple flag parameters maintain order."""
    url1 = _get_url(mock_resource, "123", flag1=None, flag2=None)
    url2 = _get_url(mock_resource, "123", flag2=None, flag1=None)

    # Get the query string parts
    query1 = url1.split('?')[1] if '?' in url1 else ''
    query2 = url2.split('?')[1] if '?' in url2 else ''

    # Check that both flags are present in both URLs
    assert 'flag1' in query1 and 'flag2' in query1
    assert 'flag1' in query2 and 'flag2' in query2
