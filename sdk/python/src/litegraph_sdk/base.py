import json
from typing import Optional, TypeVar

import httpx

from .enums.severity_enum import Severity_Enum
from .exceptions import SdkException, get_exception_for_error_code
from .models.api_error import ApiErrorResponseModel
from .sdk_logging import log_error, log_info, log_warning

T = TypeVar("T", bound="BaseClient")


class BaseClient:
    """
    LiteGraph SDK base client class.
    """

    def __init__(
        self,
        base_url: str,
        tenant_guid: str,
        graph_guid: Optional[str] = None,
        timeout: int = 10,
        retries: int = 3,
        access_key: str = None,
    ):
        self.base_url = base_url
        self.tenant_guid = tenant_guid
        self.graph_guid = graph_guid
        self.timeout = timeout
        self.retries = retries
        self.access_key = access_key
        self.client = httpx.Client(base_url=self.base_url, timeout=self.timeout)

        log_info(
            Severity_Enum.Info.value,
            f"BaseClient initialized with base_url: {self.base_url}, "
            f"tenant_guid: {self.tenant_guid}, "
            f"graph_guid: {self.graph_guid}, "
            f"timeout: {self.timeout}, "
            f"retries: {self.retries}",
        )

    def _get_headers(self):
        """
        Generate the default headers for API requests.
        """
        headers = {"Content-Type": "application/json"}
        if self.access_key:
            headers["Authorization"] = f"Bearer {self.access_key}"
        return headers

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
        if error.response.headers.get("Content-Type") == "application/json":
            error_response = ApiErrorResponseModel(**error.response.json())
            log_error(
                Severity_Enum.Error.value,
                f"Error response: {error_response.error.value} - {error_response.description}",
            )
            raise get_exception_for_error_code(error_response.error)
        log_error(
            Severity_Enum.Error.value,
            f"Server responded with non-JSON content: {error.response.content}",
        )
        raise SdkException("Server responded with non-JSON content")

    def request(self, method: str, url: str, **kwargs):
        """
        Make an HTTP request to the API with automatic retries and error handling.

        Args:
            method (str): The HTTP method to use (GET, POST, PUT, DELETE, etc.).
            url (str): The URL to send the request to.
            **kwargs: Additional arguments to pass to the underlying httpx request.
                - headers (dict, optional): Additional headers for the request.
                - data (dict, optional): The data to be sent in the request body.

        Returns:
            dict: The JSON response from the API if the response has content, None otherwise.

        Raises:
            SdkException: If the request fails after all retries.
            Various exceptions from get_exception_for_error_code based on the API error response.
        """
        headers = self._get_headers()
        if "headers" in kwargs:
            headers.update(kwargs["headers"])
        kwargs["headers"] = headers

        log_info(
            Severity_Enum.Info.value,
            f"Making {method} request to {url} with headers: {headers}",
        )

        for attempt in range(self.retries):
            try:
                response = self.client.request(method, url, **kwargs)
                return self._handle_response(response)

            except httpx.HTTPStatusError as e:
                try:
                    self._handle_error_response(e)
                except ValueError:
                    log_error(
                        Severity_Enum.Error.value,
                        f"Unexpected error while parsing error Response: {e}",
                    )
                    raise SdkException(f"Unexpected error: {e}")

            except httpx.RequestError as e:
                if attempt == self.retries - 1:
                    log_error(
                        Severity_Enum.Error.value,
                        "Max retries reached. Failing request.",
                    )
                    raise SdkException(
                        f"Request failed after {self.retries} attempts: {e}"
                    )
                log_warning(
                    Severity_Enum.Warn.value,
                    f"Request attempt {attempt + 1} failed: {e}",
                )

    def close(self):
        """
        Close the HTTP client.
        """
        log_info(Severity_Enum.Info.value, "Closing HTTP Client")
        self.client.close()
