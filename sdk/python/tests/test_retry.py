"""Retry policy and node header tests for BaseClient, using an httpx mock transport."""

import httpx
import pytest

from litegraph_sdk.base import BaseClient
from litegraph_sdk.exceptions import ResourceNotFoundError, SdkException, ServiceUnavailableError

BASE_URL = "http://127.0.0.1:8701"


def make_client(handler, **kwargs):
    """Create a BaseClient whose HTTP client answers through handler, with no retry delay."""
    kwargs.setdefault("retry_base_delay_ms", 0)
    client = BaseClient(base_url=BASE_URL, tenant_guid="default", access_key="default", **kwargs)
    client.client = httpx.Client(base_url=BASE_URL, transport=httpx.MockTransport(handler))
    return client


class Recorder:
    """Answers with the given responses in order, recording each request."""

    def __init__(self, *responses):
        self.responses = list(responses)
        self.requests = []

    def __call__(self, request):
        self.requests.append(request)
        response = self.responses.pop(0) if len(self.responses) > 1 else self.responses[0]
        if isinstance(response, Exception):
            raise response
        return response


def ok(body=None, node="litegraph-1"):
    return httpx.Response(200, json=body if body is not None else {"ok": True}, headers={"x-litegraph-node": node})


def status(code, node="litegraph-1"):
    return httpx.Response(code, headers={"x-litegraph-node": node})


def connect_error():
    return httpx.ConnectError("connection refused")


class TestRetrySettings:
    def test_defaults(self):
        client = make_client(Recorder(ok()))
        assert client.max_retries == 2
        assert client.retries == 3
        assert client.retry_post is False
        assert client.last_node_id is None
        assert BaseClient(base_url=BASE_URL, tenant_guid="default").retry_base_delay_ms == 200

    def test_legacy_retries_argument(self):
        client = BaseClient(base_url=BASE_URL, tenant_guid="default", retries=5)
        assert client.max_retries == 4
        assert client.retries == 5

    @pytest.mark.parametrize("value", [-1, 11])
    def test_max_retries_range(self, value):
        client = make_client(Recorder(ok()))
        with pytest.raises(ValueError):
            client.max_retries = value

    def test_retry_base_delay_range(self):
        client = make_client(Recorder(ok()))
        with pytest.raises(ValueError):
            client.retry_base_delay_ms = 6000


class TestRetries:
    def test_get_retried_after_503_then_succeeds(self):
        recorder = Recorder(status(503), status(503), ok({"value": 1}))
        client = make_client(recorder)
        assert client.request("GET", "v1.0/cluster/nodes") == {"value": 1}
        assert len(recorder.requests) == 3

    @pytest.mark.parametrize("code", [502, 503, 504])
    def test_each_gateway_status_is_retried(self, code):
        recorder = Recorder(status(code), ok())
        client = make_client(recorder)
        client.request("GET", "v1.0/cluster/nodes")
        assert len(recorder.requests) == 2

    def test_gives_up_after_max_retries(self):
        recorder = Recorder(status(503))
        client = make_client(recorder, max_retries=1)
        with pytest.raises(SdkException):
            client.request("GET", "v1.0/cluster/nodes")
        assert len(recorder.requests) == 2

    def test_connection_failure_retried_for_get(self):
        recorder = Recorder(connect_error(), ok({"value": 2}))
        client = make_client(recorder)
        assert client.request("GET", "v1.0/cluster/nodes") == {"value": 2}
        assert len(recorder.requests) == 2

    def test_connection_failure_exhausts_retries(self):
        recorder = Recorder(connect_error())
        client = make_client(recorder)
        with pytest.raises(SdkException) as exc_info:
            client.request("GET", "v1.0/cluster/nodes")
        assert "Request failed after 3 attempts" in str(exc_info.value)
        assert len(recorder.requests) == 3

    @pytest.mark.parametrize("method", ["HEAD", "PUT", "DELETE"])
    def test_idempotent_methods_retried(self, method):
        recorder = Recorder(status(503), ok())
        client = make_client(recorder)
        client.request(method, "v1.0/things/1")
        assert len(recorder.requests) == 2

    def test_post_not_retried_by_default(self):
        recorder = Recorder(status(503), ok())
        client = make_client(recorder)
        with pytest.raises(SdkException):
            client.request("POST", "v1.0/cluster/restart", json={"confirm": True})
        assert len(recorder.requests) == 1

    def test_post_connection_failure_not_retried_by_default(self):
        recorder = Recorder(connect_error(), ok())
        client = make_client(recorder)
        with pytest.raises(SdkException):
            client.request("POST", "v1.0/cluster/restart", json={"confirm": True})
        assert len(recorder.requests) == 1

    def test_post_retried_when_opted_in(self):
        recorder = Recorder(status(504), ok({"Restarting": True}))
        client = make_client(recorder, retry_post=True)
        assert client.request("POST", "v1.0/cluster/restart", json={"confirm": True}) == {"Restarting": True}
        assert len(recorder.requests) == 2

    def test_500_not_retried(self):
        recorder = Recorder(httpx.Response(500, json={"Error": "InternalError"}, headers={"Content-Type": "application/json"}))
        client = make_client(recorder)
        with pytest.raises(SdkException):
            client.request("GET", "v1.0/cluster/nodes")
        assert len(recorder.requests) == 1

    def test_zero_retries(self):
        recorder = Recorder(status(503))
        client = make_client(recorder, max_retries=0)
        with pytest.raises(SdkException):
            client.request("GET", "v1.0/cluster/nodes")
        assert len(recorder.requests) == 1

    def test_accepted_status_is_returned_not_retried(self):
        recorder = Recorder(httpx.Response(503, json={"Status": "Unavailable"}))
        client = make_client(recorder)
        assert client.request("GET", "v1.0/health/ready", accepted_status_codes=[503]) == {"Status": "Unavailable"}
        assert len(recorder.requests) == 1

    def test_read_timeout_not_retried(self):
        recorder = Recorder(httpx.ReadTimeout("read timed out"), ok())
        client = make_client(recorder)
        with pytest.raises(SdkException):
            client.request("GET", "v1.0/cluster/nodes")
        assert len(recorder.requests) == 1


class TestNodeHeader:
    def test_last_node_id_recorded(self):
        client = make_client(Recorder(ok(node="litegraph-3")))
        client.request("GET", "v1.0/cluster/nodes")
        assert client.last_node_id == "litegraph-3"

    def test_error_carries_node_id_and_status(self):
        response = httpx.Response(
            404,
            json={"Error": "NotFound", "Description": "Node not found"},
            headers={"Content-Type": "application/json", "x-litegraph-node": "litegraph-2"},
        )
        client = make_client(Recorder(response))
        with pytest.raises(ResourceNotFoundError) as exc_info:
            client.request("GET", "v1.0/cluster/nodes/missing")
        assert exc_info.value.node_id == "litegraph-2"
        assert exc_info.value.status_code == 404
        assert client.last_node_id == "litegraph-2"

    def test_unavailable_maps_to_service_unavailable(self):
        response = httpx.Response(
            503,
            json={"Error": "Unavailable", "Description": "Redis is unreachable"},
            headers={"Content-Type": "application/json", "x-litegraph-node": "litegraph-1"},
        )
        client = make_client(Recorder(response), max_retries=0)
        with pytest.raises(ServiceUnavailableError) as exc_info:
            client.request("GET", "v1.0/cluster/nodes")
        assert exc_info.value.status_code == 503
