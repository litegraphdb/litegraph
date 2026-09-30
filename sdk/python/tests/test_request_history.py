"""Request history resource."""

from datetime import datetime, timezone
from unittest.mock import Mock

import pytest
from litegraph_sdk import RequestHistory


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and register it as the active SDK client."""
    client = Mock()
    client.base_url = "http://127.0.0.1:8701"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def _envelope(objects, remaining=0):
    return {
        "Success": True,
        "MaxResults": 100,
        "EndOfResults": remaining == 0,
        "TotalRecords": len(objects) + remaining,
        "RecordsRemaining": remaining,
        "Objects": objects,
    }


class TestRequestHistoryList:
    def test_list_sends_paging_and_node_filter(self, mock_client):
        mock_client.request.return_value = _envelope([{"GUID": "a", "NodeId": "litegraph-2"}])
        result = RequestHistory.list(max_keys=50, skip=0, node_id="litegraph-2", status_code=200, success=True)
        mock_client.request.assert_called_once_with(
            "GET", "v1.0/requesthistory?max-keys=50&skip=0&nodeId=litegraph-2&statusCode=200&success=true"
        )
        objects = result.objects if hasattr(result, "objects") else result.Objects
        assert objects[0]["NodeId"] == "litegraph-2"

    def test_list_sends_paths_and_timestamps_unencoded(self, mock_client):
        mock_client.request.return_value = _envelope([])
        RequestHistory.list(path="/v1.0/tenants", from_utc=datetime(2026, 9, 30, 10, 0, tzinfo=timezone.utc))
        mock_client.request.assert_called_once_with(
            "GET", "v1.0/requesthistory?path=/v1.0/tenants&fromUtc=2026-09-30T10:00:00.000000Z"
        )

    def test_list_escapes_characters_that_break_the_query(self, mock_client):
        mock_client.request.return_value = _envelope([])
        RequestHistory.list(path="a b&c=d")
        mock_client.request.assert_called_once_with("GET", "v1.0/requesthistory?path=a%20b%26c%3Dd")

    def test_list_treats_naive_datetimes_as_utc(self, mock_client):
        mock_client.request.return_value = _envelope([])
        RequestHistory.list(to_utc=datetime(2026, 9, 30, 12, 30))
        mock_client.request.assert_called_once_with("GET", "v1.0/requesthistory?toUtc=2026-09-30T12:30:00.000000Z")

    def test_list_rejects_unknown_filters(self, mock_client):
        with pytest.raises(ValueError):
            RequestHistory.list(nodeid="litegraph-1")
        mock_client.request.assert_not_called()


class TestRequestHistoryReads:
    def test_read(self, mock_client):
        mock_client.request.return_value = {"GUID": "g1", "NodeId": "litegraph-1"}
        assert RequestHistory.read("g1")["NodeId"] == "litegraph-1"
        mock_client.request.assert_called_once_with("GET", "v1.0/requesthistory/g1")

    def test_read_detail(self, mock_client):
        mock_client.request.return_value = {"GUID": "g1", "RequestHeaders": {"Accept": "*/*"}}
        assert RequestHistory.read_detail("g1")["RequestHeaders"]["Accept"] == "*/*"
        mock_client.request.assert_called_once_with("GET", "v1.0/requesthistory/g1/detail")

    def test_read_requires_guid(self, mock_client):
        with pytest.raises(ValueError):
            RequestHistory.read("")
        with pytest.raises(ValueError):
            RequestHistory.read_detail(None)

    def test_summary(self, mock_client):
        mock_client.request.return_value = {"Interval": "hour", "TotalRequests": 5}
        RequestHistory.summary(interval="hour", start_utc="2026-09-30T00:00:00Z", tenant_guid="t1")
        mock_client.request.assert_called_once_with(
            "GET", "v1.0/requesthistory/summary?interval=hour&startUtc=2026-09-30T00:00:00Z&tenantGuid=t1"
        )

    def test_summary_without_options(self, mock_client):
        mock_client.request.return_value = {}
        RequestHistory.summary()
        mock_client.request.assert_called_once_with("GET", "v1.0/requesthistory/summary")


class TestRequestHistoryDeletes:
    def test_delete(self, mock_client):
        mock_client.request.return_value = None
        RequestHistory.delete("g1")
        mock_client.request.assert_called_once_with("DELETE", "v1.0/requesthistory/g1")

    def test_delete_many_uses_filters_without_paging(self, mock_client):
        mock_client.request.return_value = {"Deleted": 3}
        result = RequestHistory.delete_many(node_id="litegraph-3", method="GET")
        assert result["Deleted"] == 3
        mock_client.request.assert_called_once_with("DELETE", "v1.0/requesthistory/bulk?method=GET&nodeId=litegraph-3")

    def test_delete_many_rejects_unknown_filters(self, mock_client):
        with pytest.raises(ValueError):
            RequestHistory.delete_many(max_keys=5)
