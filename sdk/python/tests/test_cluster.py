"""Cluster node and health methods on the Admin resource."""

from unittest.mock import Mock

import pytest
from litegraph_sdk.exceptions import ConflictError, SdkException
from litegraph_sdk.resources.admin import Admin


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and register it as the active SDK client."""
    client = Mock()
    client.base_url = "http://127.0.0.1:8701"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


class TestClusterNodes:
    def test_read_cluster_node(self, mock_client):
        mock_client.request.return_value = {"NodeId": "litegraph-2", "State": "Healthy"}
        node = Admin.read_cluster_node("litegraph-2")
        assert node["State"] == "Healthy"
        mock_client.request.assert_called_once_with("GET", "v1.0/cluster/nodes/litegraph-2")

    def test_read_cluster_node_escapes_identifier(self, mock_client):
        mock_client.request.return_value = {}
        Admin.read_cluster_node("a/b c")
        mock_client.request.assert_called_once_with("GET", "v1.0/cluster/nodes/a%2Fb%20c")

    def test_read_cluster_node_requires_identifier(self, mock_client):
        with pytest.raises(ValueError):
            Admin.read_cluster_node("")

    def test_restart_cluster_node(self, mock_client):
        mock_client.request.return_value = {"Restarting": True, "Rolling": False}
        result = Admin.restart_cluster_node("litegraph-3")
        assert result["Restarting"] is True
        mock_client.request.assert_called_once_with(
            "POST", "v1.0/cluster/nodes/litegraph-3/restart", json={"confirm": True}
        )

    def test_restart_cluster_node_raises_server_refusal(self, mock_client):
        error = ConflictError("Node is Offline")
        error.status_code = 409
        mock_client.request.side_effect = error
        with pytest.raises(ConflictError):
            Admin.restart_cluster_node("litegraph-9")

    def test_restart_cluster_node_swallows_dropped_connection(self, mock_client):
        mock_client.request.side_effect = SdkException("Request failed after 1 attempts: connection reset")
        assert Admin.restart_cluster_node("host-1") is None

    def test_delete_cluster_node(self, mock_client):
        mock_client.request.return_value = None
        Admin.delete_cluster_node("old-node")
        mock_client.request.assert_called_once_with("DELETE", "v1.0/cluster/nodes/old-node")


class TestHealth:
    def test_health_live(self, mock_client):
        mock_client.request.return_value = {"Status": "Healthy"}
        assert Admin.health_live()["Status"] == "Healthy"
        mock_client.request.assert_called_once_with("GET", "v1.0/health/live", accepted_status_codes=[503])

    def test_health_ready_returns_unavailable_body(self, mock_client):
        mock_client.request.return_value = {"Status": "Unavailable", "Checks": {"Database": False}}
        ready = Admin.health_ready()
        assert ready["Status"] == "Unavailable"
        mock_client.request.assert_called_once_with("GET", "v1.0/health/ready", accepted_status_codes=[503])
