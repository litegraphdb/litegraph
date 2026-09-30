from .base import BaseClient

# Global client instance
_client = None


def configure(
    endpoint: str,
    tenant_guid: str | None,
    graph_guid: str | None = None,
    access_key: str | None = None,
    max_retries: int = 2,
    retry_base_delay_ms: int = 200,
    retry_post: bool = False,
):
    """Configure the SDK with access credentials, endpoint, and graph GUID.

    Connection failures and 502, 503, and 504 responses are retried up to max_retries times with exponential
    backoff starting at retry_base_delay_ms (capped at 5000 ms, with jitter). GET, HEAD, PUT, and DELETE are
    retried; POST only when retry_post is True.
    """
    global _client
    if tenant_guid is None:
        raise ValueError("Tenant GUID is required")
    _client = BaseClient(
        base_url=endpoint,
        tenant_guid=tenant_guid,
        graph_guid=graph_guid,
        access_key=access_key,
        max_retries=max_retries,
        retry_base_delay_ms=retry_base_delay_ms,
        retry_post=retry_post,
    )


# Utility function to get the shared client
def get_client():
    """Get the shared client instance."""
    if _client is None:
        raise ValueError("SDK is not configured. Call 'configure' first.")
    return _client
