from datetime import datetime, timezone
from urllib.parse import quote

from ..configuration import get_client
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result

# Python keyword arguments mapped to the server's query parameter names.
_FILTERS = {
    "tenant_guid": "tenantGuid",
    "request_id": "requestId",
    "correlation_id": "correlationId",
    "trace_id": "traceId",
    "method": "method",
    "path": "path",
    "source_ip": "sourceIp",
    "node_id": "nodeId",
    "transaction_id": "transactionId",
    "status_code": "statusCode",
    "success": "success",
    "has_transaction_diagnostics": "hasTransactionDiagnostics",
    "from_utc": "fromUtc",
    "to_utc": "toUtc",
}


def _encode_value(value) -> str:
    """Encode a query value.

    The server matches request history query values as sent, without percent-decoding, so only characters that
    would break the query string are escaped; path separators and the colons in timestamps are sent as-is.
    """
    if isinstance(value, bool):
        text = "true" if value else "false"
    elif isinstance(value, datetime):
        utc = value.astimezone(timezone.utc) if value.tzinfo else value.replace(tzinfo=timezone.utc)
        text = utc.strftime("%Y-%m-%dT%H:%M:%S.%fZ")
    else:
        text = str(value)
    out = []
    for ch in text:
        code = ord(ch)
        if code < 0x21 or code > 0x7E or ch in "&#+%=?":
            out.append(quote(ch, safe=""))
        else:
            out.append(ch)
    return "".join(out)


def _query(filters: dict, max_keys=None, skip=None) -> str:
    parts = []
    if max_keys is not None:
        parts.append(f"max-keys={int(max_keys)}")
    if skip is not None:
        parts.append(f"skip={int(skip)}")
    for key, name in _FILTERS.items():
        value = filters.get(key)
        if value is None or value == "":
            continue
        parts.append(f"{name}={_encode_value(value)}")
    return ("?" + "&".join(parts)) if parts else ""


def _unknown_filters(filters: dict):
    unknown = sorted(set(filters) - set(_FILTERS))
    if unknown:
        raise ValueError(f"Unknown request history filter(s): {', '.join(unknown)}")


class RequestHistory:
    """Request history recorded by the LiteGraph server.

    System administrators see every tenant and may filter by tenant_guid; tenant administrators are scoped to their
    own tenant by the server. Filters: tenant_guid, request_id, correlation_id, trace_id, method, path (substring),
    source_ip, node_id (the node that handled the request, v10.0), transaction_id, status_code, success,
    has_transaction_diagnostics, from_utc, to_utc (datetime or ISO 8601 string).
    """

    @classmethod
    def list(cls, max_keys: int = None, skip: int = None, **filters) -> EnumerationResultModel:
        """Search request history, returning one page (newest first) as an EnumerationResult envelope.

        Args:
            max_keys (int, optional): Page size, 1-1000 (server default 100).
            skip (int, optional): Records to skip; the server aligns this down to a multiple of max_keys.
            **filters: Any of the filters listed on the class.

        Returns:
            EnumerationResultModel: Objects is a list of entry dicts (GUID, Method, Path, SourceIp, NodeId, StatusCode, ...).
        """
        _unknown_filters(filters)
        client = get_client()
        return parse_enumeration_result(client.request("GET", "v1.0/requesthistory" + _query(filters, max_keys, skip)))

    @classmethod
    def read(cls, request_guid: str):
        """Read one request history entry. Raises ResourceNotFoundError when it does not exist."""
        if not request_guid:
            raise ValueError("request_guid is required")
        client = get_client()
        return client.request("GET", f"v1.0/requesthistory/{request_guid}")

    @classmethod
    def read_detail(cls, request_guid: str):
        """Read one entry with its captured headers and bodies (RequestHeaders, ResponseHeaders, RequestBody,
        ResponseBody). Raises ResourceNotFoundError when it does not exist."""
        if not request_guid:
            raise ValueError("request_guid is required")
        client = get_client()
        return client.request("GET", f"v1.0/requesthistory/{request_guid}/detail")

    @classmethod
    def summary(cls, interval: str = None, start_utc=None, end_utc=None, tenant_guid: str = None):
        """Read request counts over a time range, bucketed by interval.

        Args:
            interval (str, optional): minute, 15minute, hour, 6hour, or day (server default hour).
            start_utc (datetime or str, optional): Range start (server default 24 hours before the end).
            end_utc (datetime or str, optional): Range end (server default now).
            tenant_guid (str, optional): Tenant filter for system administrators.

        Returns:
            dict: {StartUtc, EndUtc, Interval, TotalSuccess, TotalFailure, TotalRequests, Data}.
        """
        parts = []
        for name, value in (("interval", interval), ("startUtc", start_utc), ("endUtc", end_utc), ("tenantGuid", tenant_guid)):
            if value is not None and value != "":
                parts.append(f"{name}={_encode_value(value)}")
        client = get_client()
        return client.request("GET", "v1.0/requesthistory/summary" + (("?" + "&".join(parts)) if parts else ""))

    @classmethod
    def delete(cls, request_guid: str):
        """Delete one request history entry."""
        if not request_guid:
            raise ValueError("request_guid is required")
        client = get_client()
        return client.request("DELETE", f"v1.0/requesthistory/{request_guid}")

    @classmethod
    def delete_many(cls, **filters):
        """Delete every entry matching the filters and return {"Deleted": count}.

        An empty filter deletes every entry the caller can see.
        """
        _unknown_filters(filters)
        client = get_client()
        return client.request("DELETE", "v1.0/requesthistory/bulk" + _query(filters))
