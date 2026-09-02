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
        """Read the server settings. Requires system administrator privileges."""
        client = get_client()
        return client.request("GET", "v1.0/settings")

    @classmethod
    def update_settings(cls, settings: dict):
        """Update the server settings. Requires system administrator privileges.

        Returns the update result: {Success, AppliedLive, RestartRequired, Message}.
        """
        client = get_client()
        return client.request("PUT", "v1.0/settings", json=settings)

    @classmethod
    def restart_server(cls):
        """Request a server restart so the container restart policy applies the new settings.

        Requires system administrator privileges. Best-effort; the connection may drop as the server exits.
        """
        client = get_client()
        try:
            return client.request("POST", "v1.0/settings/restart", json={"confirm": True})
        except Exception:
            # The server may drop the connection as it exits; this is expected.
            return None
