from ..configuration import get_client
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result
from ..utils.url_helper import _append_query, _pagination_params


class Admin:
    """Administrative operations for LiteGraph server."""

    @classmethod
    def list_backups(
        cls,
        max_keys: int = None,
        skip: int = None,
        order: str = None,
        continuation_token: str = None,
    ) -> EnumerationResultModel:
        """List available backups as an EnumerationResult envelope."""
        client = get_client()
        url = _append_query(
            "v1.0/backups",
            _pagination_params(max_keys, skip, order, continuation_token),
        )
        return parse_enumeration_result(client.request("GET", url))

    @classmethod
    def create_backup(cls):
        """Create a new database backup."""
        client = get_client()
        return client.request("POST", "v1.0/backups")

    @classmethod
    def read_backup(cls, backup_filename: str):
        """Read a specific backup file."""
        client = get_client()
        return client.request("GET", f"v1.0/backups/{backup_filename}")

    @classmethod
    def backup_exists(cls, backup_filename: str) -> bool:
        """Check if a backup file exists."""
        client = get_client()
        try:
            client.request("HEAD", f"v1.0/backups/{backup_filename}")
            return True
        except Exception:
            return False

    @classmethod
    def delete_backup(cls, backup_filename: str):
        """Delete a backup file."""
        client = get_client()
        return client.request("DELETE", f"v1.0/backups/{backup_filename}")

    @classmethod
    def flush(cls):
        """Flush in-memory database to disk."""
        client = get_client()
        return client.request("POST", "v1.0/flush")

    @classmethod
    def read_settings(cls):
        """Read the server settings file. Requires system administrator privileges.

        Every node sharing the settings file returns the same settings.
        """
        client = get_client()
        return client.request("GET", "v1.0/settings")

    @classmethod
    def update_settings(cls, settings: dict):
        """Update the server settings. Requires system administrator privileges.

        Returns the update result: {Success, AppliedLive, RestartRequired, Message, EnvironmentOverrides,
        SettingsVersion}. Settings supplied by environment variables keep their file values.
        """
        client = get_client()
        return client.request("PUT", "v1.0/settings", json=settings)

    @classmethod
    def restart_server(cls):
        """Request a restart so saved settings take effect.

        In cluster mode every node restarts, one at a time, each after the previous one reports healthy; on a
        single node the server exits so the container restart policy restarts it. Requires system administrator
        privileges. Returns the restart result {Restarting, Rolling, RestartVersion, Message, RequestedUtc}, or
        None if the connection dropped as a single server exited.
        """
        client = get_client()
        try:
            return client.request("POST", "v1.0/settings/restart", json={"confirm": True})
        except Exception:
            # A single server may drop the connection as it exits; this is expected.
            return None

    @classmethod
    def read_cluster_nodes(cls):
        """List the cluster nodes with their state and health, plus the settings and restart counters.

        On a single node the answering server is the only node. Requires system administrator privileges.
        Returns {ClusterEnabled, ClusterName, AnsweredBy, RegistryAvailable, SettingsVersion, SettingsUpdatedUtc,
        RestartVersion, RestartRequestedUtc, Nodes, Utc}.
        """
        client = get_client()
        return client.request("GET", "v1.0/cluster/nodes")

    @classmethod
    def restart_cluster(cls):
        """Request a rolling restart of every cluster node (a restart of the answering server on a single node).

        Requires system administrator privileges. Returns the restart result, or None if the connection dropped as
        a single server exited.
        """
        client = get_client()
        try:
            return client.request("POST", "v1.0/cluster/restart", json={"confirm": True})
        except Exception:
            # A single server may drop the connection as it exits; this is expected.
            return None
