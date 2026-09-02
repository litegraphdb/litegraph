from unittest.mock import Mock

import pytest
from litegraph_sdk.resources.admin import Admin


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and register it as the active SDK client."""
    client = Mock()
    client.base_url = "http://test-api.com"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


class TestAdminBackups:
    def test_list_backups(self, mock_client):
        """list_backups GETs the backups enumeration envelope."""
        mock_client.request.return_value = {
            "Success": True,
            "MaxResults": 1000,
            "ContinuationToken": None,
            "EndOfResults": True,
            "TotalRecords": 1,
            "RecordsRemaining": 0,
            "Objects": [{"Filename": "backup-1.db", "Length": 1024}],
        }
        result = Admin.list_backups()
        assert result.total_records == 1
        assert result.objects[0]["Filename"] == "backup-1.db"
        mock_client.request.assert_called_once_with("GET", "v1.0/backups")

    def test_list_backups_pagination(self, mock_client):
        """list_backups maps pagination args to max-keys/skip query params."""
        mock_client.request.return_value = {"Objects": []}
        Admin.list_backups(max_keys=5, skip=2)
        called_url = mock_client.request.call_args[0][1]
        assert "max-keys=5" in called_url
        assert "skip=2" in called_url


class TestAdminSettings:
    def test_read_settings(self, mock_client):
        """read_settings issues a GET against the settings endpoint."""
        mock_client.request.return_value = {"RequestTimeoutSeconds": 60}
        result = Admin.read_settings()
        assert result["RequestTimeoutSeconds"] == 60
        mock_client.request.assert_called_once_with("GET", "v1.0/settings")

    def test_update_settings(self, mock_client):
        """update_settings PUTs the supplied settings and returns the update result."""
        mock_client.request.return_value = {
            "Success": True,
            "AppliedLive": ["RequestTimeoutSeconds"],
            "RestartRequired": [],
        }
        result = Admin.update_settings({"RequestTimeoutSeconds": 30})
        assert result["Success"] is True
        assert "RequestTimeoutSeconds" in result["AppliedLive"]
        mock_client.request.assert_called_once_with(
            "PUT", "v1.0/settings", json={"RequestTimeoutSeconds": 30}
        )

    def test_restart_server(self, mock_client):
        """restart_server POSTs to the restart endpoint with a confirmation flag."""
        mock_client.request.return_value = {"restarting": True}
        Admin.restart_server()
        mock_client.request.assert_called_once_with(
            "POST", "v1.0/settings/restart", json={"confirm": True}
        )

    def test_restart_server_swallows_dropped_connection(self, mock_client):
        """A dropped connection during restart is expected and not raised."""
        mock_client.request.side_effect = ConnectionError("connection reset")
        assert Admin.restart_server() is None
