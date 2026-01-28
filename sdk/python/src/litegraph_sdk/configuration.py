from .base import BaseClient

# Global client instance
_client = None


def configure(
    endpoint: str,
    tenant_guid: str | None,
    graph_guid: str | None = None,
    access_key: str | None = None,
):
    """Configure the SDK with access credentials, endpoint, and graph GUID."""
    global _client
    if tenant_guid is None:
        raise ValueError("Tenant GUID is required")
    _client = BaseClient(
        base_url=endpoint,
        tenant_guid=tenant_guid,
        graph_guid=graph_guid,
        access_key=access_key,
    )


# Utility function to get the shared client
def get_client():
    """Get the shared client instance."""
    if _client is None:
        raise ValueError("SDK is not configured. Call 'configure' first.")
    return _client
