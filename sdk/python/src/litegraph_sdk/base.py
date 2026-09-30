import json
import random
import time
from typing import Optional, TypeVar

import httpx

from .enums.severity_enum import Severity_Enum
from .exceptions import SdkException, get_exception_for_error_code
from .models.api_error import ApiErrorResponseModel
from .sdk_logging import log_error, log_info, log_warning

_SENSITIVE_HEADERS = {"authorization", "x-token", "x-api-key", "cookie"}


def _redact_headers(headers):
    """Return a copy of the headers with credentials replaced, for logging."""
    return {k: ("***" if str(k).lower() in _SENSITIVE_HEADERS else v) for k, v in (headers or {}).items()}


T = TypeVar("T", bound="BaseClient")

NODE_HEADER = "x-litegraph-node"
"""Response header naming the cluster node that answered a request."""

RETRYABLE_STATUS_CODES = frozenset({502, 503, 504})
"""HTTP status codes that are retried (the load balancer or a node could not serve the request)."""

IDEMPOTENT_METHODS = frozenset({"GET", "HEAD", "PUT", "DELETE"})
"""Methods retried by default; POST is retried only when retry_post is True."""

MAX_RETRY_DELAY_MS = 5000
"""Upper bound on the delay before any retry, in milliseconds."""

NON_RETRYABLE_TRANSPORT_ERRORS = (httpx.ReadTimeout, httpx.WriteTimeout)
"""Transport errors after which the server may already have acted on the request, so they are never retried."""


class BaseClient:
    """
    LiteGraph SDK base client class.

    Requests that fail with a connection error or a 502, 503, or 504 response are retried with exponential backoff
    and jitter: GET, HEAD, PUT, and DELETE always, POST only when retry_post is True. The node that answered the
    most recent request is available as last_node_id.
    """

    def __init__(
        self,
        base_url: str,
        tenant_guid: str,
        graph_guid: Optional[str] = None,
        timeout: int = 10,
        retries: Optional[int] = None,
        access_key: str = None,
        max_retries: int = 2,
        retry_base_delay_ms: int = 200,
        retry_post: bool = False,
    ):
        """
        Args:
            base_url: API endpoint, for example "http://127.0.0.1:8701".
            tenant_guid: Tenant GUID.
            graph_guid: Optional graph GUID.
            timeout: Request timeout in seconds.
            retries: Deprecated. Total attempts (first attempt plus retries); when given, overrides max_retries
                with retries - 1.
            access_key: Bearer token.
            max_retries: Retries after the first attempt, 0 to 10. Default 2.
            retry_base_delay_ms: Delay before the first retry, doubled per retry and capped at 5000 ms, less up to
                half as jitter. 0 to 5000. Default 200.
            retry_post: Also retry POST requests. POST is not idempotent, so a retried POST can apply twice if the
                first attempt reached the server. Default False.
        """
        self.base_url = base_url
        self.tenant_guid = tenant_guid
        self.graph_guid = graph_guid
        self.timeout = timeout
        self.access_key = access_key
        self.max_retries = max(0, retries - 1) if retries is not None else max_retries
        self.retry_base_delay_ms = retry_base_delay_ms
        self.retry_post = retry_post
        self.last_node_id: Optional[str] = None
        self.client = httpx.Client(base_url=self.base_url, timeout=self.timeout)

        log_info(
            Severity_Enum.Info.value,
            f"BaseClient initialized with base_url: {self.base_url}, "
            f"tenant_guid: {self.tenant_guid}, "
            f"graph_guid: {self.graph_guid}, "
            f"timeout: {self.timeout}, "
            f"max_retries: {self.max_retries}",
        )

    @property
    def max_retries(self) -> int:
        """Retries after the first attempt, 0 to 10."""
        return self._max_retries

    @max_retries.setter
    def max_retries(self, value: int) -> None:
        if not isinstance(value, int) or value < 0 or value > 10:
            raise ValueError("max_retries must be an integer between 0 and 10")
        self._max_retries = value

    @property
    def retry_base_delay_ms(self) -> int:
        """Delay before the first retry in milliseconds, 0 to 5000."""
        return self._retry_base_delay_ms

    @retry_base_delay_ms.setter
    def retry_base_delay_ms(self, value: int) -> None:
        if not isinstance(value, (int, float)) or value < 0 or value > MAX_RETRY_DELAY_MS:
            raise ValueError("retry_base_delay_ms must be between 0 and 5000")
        self._retry_base_delay_ms = value

    @property
    def retries(self) -> int:
        """Total attempts per request (first attempt plus max_retries). Kept for compatibility."""
        return self._max_retries + 1

    def _get_headers(self):
        """
        Generate the default headers for API requests.
        """
        headers = {"Content-Type": "application/json"}
        if self.access_key:
            headers["Authorization"] = f"Bearer {self.access_key}"
        return headers

    def _record_node_id(self, response) -> Optional[str]:
        """Record the x-litegraph-node header of a response in last_node_id and return it."""
        try:
            node_id = response.headers.get(NODE_HEADER) if response is not None else None
        except Exception:
            node_id = None
        if isinstance(node_id, str) and node_id:
            self.last_node_id = node_id
            return node_id
        return None

    def _can_retry(self, method: str, attempt: int) -> bool:
        """True if a request with this method may be retried after the given number of completed retries."""
        if attempt >= self._max_retries:
            return False
        method = method.upper()
        if method == "POST":
            return bool(self.retry_post)
        return method in IDEMPOTENT_METHODS

    def _sleep_before_retry(self, retry: int) -> None:
        """Wait before retry number retry (1-based): base delay doubled per retry, capped, less up to half as jitter."""
        delay = min(MAX_RETRY_DELAY_MS, self._retry_base_delay_ms * (2 ** (retry - 1)))
        if delay <= 0:
            return
        jittered = delay - random.uniform(0, delay / 2)
        time.sleep(jittered / 1000.0)

    def _handle_response(self, response):
        """Handle successful API response."""
        response.raise_for_status()
        log_info(
            Severity_Enum.Info.value, f"Request successful: {response.status_code}"
        )
        try:
            return response.json() if response.content else None
        except json.JSONDecodeError:
            return response.content

    def _handle_error_response(self, error):
        """Handle HTTP error response."""
        node_id = self._record_node_id(error.response)
        status_code = getattr(error.response, "status_code", None)
        if error.response.headers.get("Content-Type") == "application/json":
            error_response = ApiErrorResponseModel(**error.response.json())
            log_error(
                Severity_Enum.Error.value,
                f"Error response: {error_response.error.value} - {error_response.description}",
            )
            exception = get_exception_for_error_code(error_response.error)
            exception.node_id = node_id
            exception.status_code = status_code if isinstance(status_code, int) else None
            raise exception
        log_error(
            Severity_Enum.Error.value,
            f"Server responded with non-JSON content: {error.response.content}",
        )
        raise SdkException(
            "Server responded with non-JSON content",
            node_id=node_id,
            status_code=status_code if isinstance(status_code, int) else None,
        )

    def request(self, method: str, url: str, **kwargs):
        """
        Make an HTTP request to the API with automatic retries and error handling.

        Connection failures and 502, 503, and 504 responses are retried with exponential backoff and jitter for
        GET, HEAD, PUT, and DELETE, and for POST only when retry_post is True. The answering node is recorded in
        last_node_id.

        Args:
            method (str): The HTTP method to use (GET, POST, PUT, DELETE, etc.).
            url (str): The URL to send the request to.
            **kwargs: Additional arguments to pass to the underlying httpx request.
                - headers (dict, optional): Additional headers for the request.
                - data (dict, optional): The data to be sent in the request body.
                - accepted_status_codes (iterable, optional): Non-success status codes whose body is returned
                  instead of raised; these are never retried.

        Returns:
            dict: The JSON response from the API if the response has content, None otherwise.

        Raises:
            SdkException: If the request fails after all retries. HTTP errors carry node_id and status_code.
            Various exceptions from get_exception_for_error_code based on the API error response.
        """
        accepted_status_codes = set(kwargs.pop("accepted_status_codes", []))
        headers = self._get_headers()
        if "headers" in kwargs:
            headers.update(kwargs["headers"])
        kwargs["headers"] = headers

        log_info(
            Severity_Enum.Info.value,
            f"Making {method} request to {url} with headers: {_redact_headers(headers)}",
        )

        attempt = 0
        while True:
            try:
                response = self.client.request(method, url, **kwargs)
            except httpx.HTTPStatusError as e:
                self._raise_http_error(e)
            except NON_RETRYABLE_TRANSPORT_ERRORS as e:
                raise SdkException(f"Request failed after {attempt + 1} attempts: {e}")
            except httpx.RequestError as e:
                if not self._can_retry(method, attempt):
                    log_error(
                        Severity_Enum.Error.value,
                        "Max retries reached. Failing request.",
                    )
                    raise SdkException(
                        f"Request failed after {attempt + 1} attempts: {e}"
                    )
                attempt += 1
                log_warning(
                    Severity_Enum.Warn.value,
                    f"Request attempt {attempt} failed: {e}",
                )
                self._sleep_before_retry(attempt)
                continue

            self._record_node_id(response)
            status_code = getattr(response, "status_code", None)

            if status_code in accepted_status_codes:
                log_info(
                    Severity_Enum.Info.value,
                    f"Accepted non-success response: {status_code}",
                )
                try:
                    return response.json() if response.content else None
                except json.JSONDecodeError:
                    return response.content

            if status_code in RETRYABLE_STATUS_CODES and self._can_retry(method, attempt):
                attempt += 1
                log_warning(
                    Severity_Enum.Warn.value,
                    f"Status {status_code} on {method} {url}, retry {attempt} of {self._max_retries}",
                )
                self._sleep_before_retry(attempt)
                continue

            try:
                return self._handle_response(response)
            except httpx.HTTPStatusError as e:
                self._raise_http_error(e)

    def _raise_http_error(self, error):
        """Raise the SDK exception for an HTTP error response."""
        try:
            self._handle_error_response(error)
        except ValueError:
            log_error(
                Severity_Enum.Error.value,
                f"Unexpected error while parsing error Response: {error}",
            )
            raise SdkException(f"Unexpected error: {error}")

    def close(self):
        """
        Close the HTTP client.
        """
        log_info(Severity_Enum.Info.value, "Closing HTTP Client")
        self.client.close()
