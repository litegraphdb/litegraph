from unittest.mock import Mock, patch

import httpx
import pytest
from litegraph_sdk.base import BaseClient
from litegraph_sdk.enums.chat_endpoint_type_enum import ChatEndpointType_Enum
from litegraph_sdk.enums.chat_feedback_rating_enum import ChatFeedbackRating_Enum
from litegraph_sdk.enums.chat_provider_type_enum import ChatProviderType_Enum
from litegraph_sdk.exceptions import AuthenticationError, BadRequestError, SdkException
from litegraph_sdk.models.chat import (
    ChatCompletionResultModel,
    ChatEndpointHealthModel,
    ChatEndpointModel,
    ChatEndpointPreloadResultModel,
    ChatEndpointTestResultModel,
    ChatFeedbackModel,
    ChatModelSummaryModel,
    ChatSettingsModel,
    ChatThreadModel,
    ChatTurnModel,
)
from litegraph_sdk.resources.chat import Chat

TENANT_GUID = "00000000-0000-0000-0000-000000000001"
ENDPOINT_GUID = "11111111-1111-1111-1111-111111111111"
THREAD_GUID = "22222222-2222-2222-2222-222222222222"
TURN_GUID = "33333333-3333-3333-3333-333333333333"
FEEDBACK_GUID = "44444444-4444-4444-4444-444444444444"

CHAT_BASE = f"v1.0/tenants/{TENANT_GUID}/chat"


def _envelope(objects):
    """Wrap objects in an EnumerationResult envelope."""
    return {
        "Success": True,
        "MaxResults": 1000,
        "ContinuationToken": None,
        "EndOfResults": True,
        "TotalRecords": len(objects),
        "RecordsRemaining": 0,
        "Objects": objects,
    }


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and register it as the active SDK client."""
    client = Mock()
    client.base_url = "http://test-api.com"
    client.tenant_guid = TENANT_GUID
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def _endpoint_response(**overrides):
    response = {
        "GUID": ENDPOINT_GUID,
        "TenantGUID": TENANT_GUID,
        "Name": "openai-completion",
        "EndpointType": "Completion",
        "Provider": "OpenAI",
        "Endpoint": "https://api.openai.com/",
        "ApiKey": "********abcd",
        "Model": "gpt-4o-mini",
        "ContextWindowTokens": 128000,
        "MaxOutputTokens": 4096,
        "Temperature": 0.7,
        "TimeoutMs": 120000,
        "MaxConcurrentRequests": 2,
        "Active": True,
        "HealthCheckEnabled": True,
        "HealthCheckUrl": None,
        "HealthCheckMethod": "GET",
        "HealthCheckIntervalMs": 30000,
        "HealthCheckTimeoutMs": 10000,
        "HealthCheckExpectedStatusCode": 200,
        "HealthyThreshold": 2,
        "UnhealthyThreshold": 2,
        "HealthCheckUseAuth": False,
        "CreatedUtc": "2026-08-31T00:00:00.000000Z",
        "LastUpdateUtc": "2026-08-31T00:00:00.000000Z",
    }
    response.update(overrides)
    return response


class _FakeStreamResponse:
    """Minimal stand-in for an httpx streaming response."""

    def __init__(self, lines, status_code=200, headers=None, body=b""):
        self.status_code = status_code
        self.headers = headers or {}
        self._lines = lines
        self._body = body

    def iter_lines(self):
        return iter(self._lines)

    def read(self):
        return self._body


class _FakeStreamContext:
    def __init__(self, response):
        self._response = response

    def __enter__(self):
        return self._response

    def __exit__(self, exc_type, exc_value, traceback):
        return False


class TestChatEndpoints:
    def test_create_endpoint(self, mock_client):
        """create_endpoint PUTs a PascalCase body and returns the model."""
        mock_client.request.return_value = _endpoint_response()
        result = Chat.create_endpoint(
            name="openai-completion",
            endpoint_type=ChatEndpointType_Enum.Completion,
            provider=ChatProviderType_Enum.OpenAI,
            endpoint="https://api.openai.com/",
            api_key="sk-secret",
            model="gpt-4o-mini",
            context_window_tokens=128000,
            max_output_tokens=8192,
            temperature=0.5,
            timeout_ms=60000,
            max_concurrent_requests=4,
            health_check_method="HEAD",
            health_check_interval_ms=15000,
            health_check_timeout_ms=5000,
            health_check_expected_status_code=204,
            healthy_threshold=3,
            unhealthy_threshold=5,
        )
        assert isinstance(result, ChatEndpointModel)
        assert result.guid == ENDPOINT_GUID
        assert result.api_key == "********abcd"
        assert result.provider == ChatProviderType_Enum.OpenAI
        assert result.context_window_tokens == 128000
        assert result.max_output_tokens == 4096
        assert result.temperature == 0.7
        assert result.timeout_ms == 120000
        assert result.max_concurrent_requests == 2
        assert result.health_check_method == "GET"
        assert result.health_check_interval_ms == 30000
        assert result.health_check_timeout_ms == 10000
        assert result.health_check_expected_status_code == 200
        assert result.healthy_threshold == 2
        assert result.unhealthy_threshold == 2
        method, url = mock_client.request.call_args[0]
        assert method == "PUT"
        assert url == f"{CHAT_BASE}/endpoints"
        body = mock_client.request.call_args[1]["json"]
        assert body["Name"] == "openai-completion"
        assert body["EndpointType"] == "Completion"
        assert body["Provider"] == "OpenAI"
        assert body["Endpoint"] == "https://api.openai.com/"
        assert body["ApiKey"] == "sk-secret"
        assert body["Model"] == "gpt-4o-mini"
        assert body["ContextWindowTokens"] == 128000
        assert body["MaxOutputTokens"] == 8192
        assert body["Temperature"] == 0.5
        assert body["TimeoutMs"] == 60000
        assert body["MaxConcurrentRequests"] == 4
        assert body["HealthCheckMethod"] == "HEAD"
        assert body["HealthCheckIntervalMs"] == 15000
        assert body["HealthCheckTimeoutMs"] == 5000
        assert body["HealthCheckExpectedStatusCode"] == 204
        assert body["HealthyThreshold"] == 3
        assert body["UnhealthyThreshold"] == 5

    def test_endpoint_model_defaults_match_server(self):
        """ChatEndpointModel defaults mirror the server-side C# defaults."""
        model = ChatEndpointModel()
        assert model.context_window_tokens == 0
        assert model.max_output_tokens == 4096
        assert model.temperature == 0.7
        assert model.timeout_ms == 120000
        assert model.max_concurrent_requests == 2
        assert model.active is True
        assert model.health_check_enabled is True
        assert model.health_check_url is None
        assert model.health_check_method == "GET"
        assert model.health_check_interval_ms == 30000
        assert model.health_check_timeout_ms == 10000
        assert model.health_check_expected_status_code == 200
        assert model.healthy_threshold == 2
        assert model.unhealthy_threshold == 2
        assert model.health_check_use_auth is False

    def test_read_endpoints(self, mock_client):
        """read_endpoints returns an enumeration envelope of endpoint models."""
        mock_client.request.return_value = _envelope([_endpoint_response()])
        result = Chat.read_endpoints()
        assert result.total_records == 1
        assert len(result.objects) == 1
        assert isinstance(result.objects[0], ChatEndpointModel)
        mock_client.request.assert_called_once_with("GET", f"{CHAT_BASE}/endpoints")

    def test_read_endpoints_filtered_by_type(self, mock_client):
        """read_endpoints appends the endpointType query parameter."""
        mock_client.request.return_value = _envelope([])
        Chat.read_endpoints(endpoint_type=ChatEndpointType_Enum.Embedding)
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/endpoints?endpointType=Embedding"
        )

    def test_read_endpoints_pagination(self, mock_client):
        """read_endpoints maps pagination args to max-keys/skip/order/token."""
        mock_client.request.return_value = _envelope([])
        Chat.read_endpoints(max_keys=5, skip=10, order="CreatedDescending")
        called_url = mock_client.request.call_args[0][1]
        assert "max-keys=5" in called_url
        assert "skip=10" in called_url
        assert "order=CreatedDescending" in called_url

    def test_read_endpoint(self, mock_client):
        """read_endpoint GETs a single endpoint by GUID."""
        mock_client.request.return_value = _endpoint_response()
        result = Chat.read_endpoint(ENDPOINT_GUID)
        assert result.name == "openai-completion"
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}"
        )

    def test_endpoint_exists_true(self, mock_client):
        """endpoint_exists returns True when the HEAD request succeeds."""
        mock_client.request.return_value = None
        assert Chat.endpoint_exists(ENDPOINT_GUID) is True
        mock_client.request.assert_called_once_with(
            "HEAD", f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}"
        )

    def test_endpoint_exists_false(self, mock_client):
        """endpoint_exists returns False when the HEAD request fails."""
        mock_client.request.side_effect = SdkException("not found")
        assert Chat.endpoint_exists(ENDPOINT_GUID) is False

    def test_update_endpoint_round_trip(self, mock_client):
        """update_endpoint PUTs the full object; a redacted ApiKey is passed
        through untouched so the server preserves the stored key."""
        mock_client.request.return_value = _endpoint_response(Name="renamed")
        result = Chat.update_endpoint(
            ENDPOINT_GUID,
            guid=ENDPOINT_GUID,
            tenant_guid=TENANT_GUID,
            name="renamed",
            endpoint_type="Completion",
            provider="OpenAI",
            endpoint="https://api.openai.com/",
            api_key="********abcd",
            model="gpt-4o-mini",
            max_output_tokens=2048,
            temperature=1.1,
            timeout_ms=90000,
            max_concurrent_requests=8,
            health_check_method="HEAD",
            health_check_interval_ms=45000,
            health_check_timeout_ms=20000,
            health_check_expected_status_code=200,
            healthy_threshold=1,
            unhealthy_threshold=4,
        )
        assert result.name == "renamed"
        method, url = mock_client.request.call_args[0]
        assert method == "PUT"
        assert url == f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}"
        body = mock_client.request.call_args[1]["json"]
        assert body["ApiKey"] == "********abcd"
        assert body["MaxOutputTokens"] == 2048
        assert body["Temperature"] == 1.1
        assert body["TimeoutMs"] == 90000
        assert body["MaxConcurrentRequests"] == 8
        assert body["HealthCheckMethod"] == "HEAD"
        assert body["HealthCheckIntervalMs"] == 45000
        assert body["HealthCheckTimeoutMs"] == 20000
        assert body["HealthCheckExpectedStatusCode"] == 200
        assert body["HealthyThreshold"] == 1
        assert body["UnhealthyThreshold"] == 4

    def test_delete_endpoint(self, mock_client):
        """delete_endpoint issues a DELETE against the endpoint URL."""
        mock_client.request.return_value = None
        Chat.delete_endpoint(ENDPOINT_GUID)
        mock_client.request.assert_called_once_with(
            "DELETE", f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}"
        )

    def test_test_endpoint(self, mock_client):
        """test_endpoint POSTs to /test and parses the result."""
        mock_client.request.return_value = {
            "Reachable": True,
            "Models": ["gpt-4o-mini", "gpt-4o"],
            "ModelExists": True,
            "Error": None,
            "RuntimeMs": 123.4,
        }
        result = Chat.test_endpoint(ENDPOINT_GUID)
        assert isinstance(result, ChatEndpointTestResultModel)
        assert result.reachable is True
        assert result.model_exists is True
        assert "gpt-4o" in result.models
        mock_client.request.assert_called_once_with(
            "POST", f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}/test"
        )

    def test_preload_endpoint(self, mock_client):
        """preload_endpoint POSTs to /preload and parses the result."""
        mock_client.request.return_value = {
            "EndpointGUID": ENDPOINT_GUID,
            "Model": "gemma3:4b",
            "Provider": "Ollama",
            "Supported": True,
            "Started": True,
            "AlreadyInProgress": False,
        }
        result = Chat.preload_endpoint(ENDPOINT_GUID)
        assert isinstance(result, ChatEndpointPreloadResultModel)
        assert result.endpoint_guid == ENDPOINT_GUID
        assert result.model == "gemma3:4b"
        assert result.supported is True
        assert result.started is True
        assert result.already_in_progress is False
        mock_client.request.assert_called_once_with(
            "POST", f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}/preload"
        )

    def test_read_endpoint_health(self, mock_client):
        """read_endpoint_health GETs health for a single endpoint."""
        mock_client.request.return_value = {
            "EndpointGUID": ENDPOINT_GUID,
            "TenantGUID": TENANT_GUID,
            "Name": "openai-completion",
            "EndpointType": "Completion",
            "Monitored": True,
            "Healthy": True,
            "LastCheckedUtc": "2026-08-31T00:00:00.000000Z",
            "LastError": None,
            "ConsecutiveSuccesses": 5,
            "ConsecutiveFailures": 0,
            "UptimePercentage": 100.0,
            "CheckHistory": [
                {
                    "TimestampUtc": "2026-08-31T00:00:00.000000Z",
                    "Success": True,
                    "DurationMs": 42.0,
                }
            ],
        }
        result = Chat.read_endpoint_health(ENDPOINT_GUID)
        assert isinstance(result, ChatEndpointHealthModel)
        assert result.healthy is True
        assert result.check_history[0].duration_ms == 42.0
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/endpoints/{ENDPOINT_GUID}/health"
        )

    def test_read_all_endpoint_health(self, mock_client):
        """read_all_endpoint_health GETs the health envelope."""
        mock_client.request.return_value = _envelope(
            [
                {
                    "EndpointGUID": ENDPOINT_GUID,
                    "TenantGUID": TENANT_GUID,
                    "Name": "openai-completion",
                    "EndpointType": "Completion",
                    "Monitored": False,
                    "Healthy": None,
                }
            ]
        )
        result = Chat.read_all_endpoint_health()
        assert result.total_records == 1
        assert len(result.objects) == 1
        assert result.objects[0].healthy is None
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/endpoints/health"
        )


class TestChatModels:
    def test_read_models(self, mock_client):
        """read_models GETs the non-admin model catalog and parses summaries."""
        mock_client.request.return_value = _envelope(
            [
                {
                    "GUID": ENDPOINT_GUID,
                    "Name": "openai-completion",
                    "Model": "gpt-4o-mini",
                    "Provider": "OpenAI",
                    "EndpointType": "Completion",
                    "IsDefault": True,
                },
                {
                    "GUID": "66666666-6666-6666-6666-666666666666",
                    "Name": "voyage-embedding",
                    "Model": "voyage-3.5",
                    "Provider": "VoyageAI",
                    "EndpointType": "Embedding",
                    "IsDefault": False,
                },
            ]
        )
        result = Chat.read_models()
        assert result.total_records == 2
        assert len(result.objects) == 2
        assert isinstance(result.objects[0], ChatModelSummaryModel)
        assert result.objects[0].guid == ENDPOINT_GUID
        assert result.objects[0].model == "gpt-4o-mini"
        assert result.objects[0].provider == ChatProviderType_Enum.OpenAI
        assert result.objects[0].endpoint_type == ChatEndpointType_Enum.Completion
        assert result.objects[0].is_default is True
        assert result.objects[1].endpoint_type == ChatEndpointType_Enum.Embedding
        assert result.objects[1].is_default is False
        mock_client.request.assert_called_once_with("GET", f"{CHAT_BASE}/models")


class TestChatThreads:
    def test_create_thread(self, mock_client):
        """create_thread PUTs the optional GraphGUID and Title."""
        mock_client.request.return_value = {
            "GUID": THREAD_GUID,
            "TenantGUID": TENANT_GUID,
            "UserGUID": "55555555-5555-5555-5555-555555555555",
            "GraphGUID": None,
            "Title": "My thread",
            "CreatedUtc": "2026-08-31T00:00:00.000000Z",
            "LastUpdateUtc": "2026-08-31T00:00:00.000000Z",
        }
        result = Chat.create_thread(title="My thread")
        assert isinstance(result, ChatThreadModel)
        assert result.title == "My thread"
        mock_client.request.assert_called_once_with(
            "PUT", f"{CHAT_BASE}/threads", json={"Title": "My thread"}
        )

    def test_read_threads(self, mock_client):
        """read_threads GETs the caller's threads as an envelope."""
        mock_client.request.return_value = _envelope([])
        result = Chat.read_threads()
        assert result.objects == []
        mock_client.request.assert_called_once_with("GET", f"{CHAT_BASE}/threads")

    def test_read_threads_all_users(self, mock_client):
        """read_threads with all_users appends the all flag."""
        mock_client.request.return_value = _envelope([])
        Chat.read_threads(all_users=True)
        mock_client.request.assert_called_once_with("GET", f"{CHAT_BASE}/threads?all")

    def test_read_thread(self, mock_client):
        """read_thread GETs a single thread."""
        mock_client.request.return_value = {
            "GUID": THREAD_GUID,
            "TenantGUID": TENANT_GUID,
            "UserGUID": "55555555-5555-5555-5555-555555555555",
        }
        result = Chat.read_thread(THREAD_GUID)
        assert result.guid == THREAD_GUID
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/threads/{THREAD_GUID}"
        )

    def test_update_thread(self, mock_client):
        """update_thread PUTs the new Title."""
        mock_client.request.return_value = {
            "GUID": THREAD_GUID,
            "TenantGUID": TENANT_GUID,
            "UserGUID": "55555555-5555-5555-5555-555555555555",
            "Title": "Renamed thread",
        }
        result = Chat.update_thread(THREAD_GUID, "Renamed thread")
        assert isinstance(result, ChatThreadModel)
        assert result.title == "Renamed thread"
        mock_client.request.assert_called_once_with(
            "PUT",
            f"{CHAT_BASE}/threads/{THREAD_GUID}",
            json={"Title": "Renamed thread"},
        )

    def test_delete_thread(self, mock_client):
        """delete_thread issues a DELETE."""
        mock_client.request.return_value = None
        Chat.delete_thread(THREAD_GUID)
        mock_client.request.assert_called_once_with(
            "DELETE", f"{CHAT_BASE}/threads/{THREAD_GUID}"
        )

    def test_read_thread_turns(self, mock_client):
        """read_thread_turns GETs the turns envelope and parses metrics."""
        mock_client.request.return_value = _envelope(
            [
                {
                    "GUID": TURN_GUID,
                    "TenantGUID": TENANT_GUID,
                    "ThreadGUID": THREAD_GUID,
                    "UserMessage": "hello",
                    "AssistantResponse": "hi there",
                    "Provider": "Ollama",
                    "Model": "llama3",
                    "TotalDurationMs": 1234.5,
                    "PromptTokens": 12,
                    "CompletionTokens": 34,
                    "Success": True,
                    "CreatedUtc": "2026-08-31T00:00:00.000000Z",
                }
            ]
        )
        result = Chat.read_thread_turns(THREAD_GUID)
        assert result.total_records == 1
        assert len(result.objects) == 1
        assert isinstance(result.objects[0], ChatTurnModel)
        assert result.objects[0].provider == ChatProviderType_Enum.Ollama
        assert result.objects[0].completion_tokens == 34
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/threads/{THREAD_GUID}/turns"
        )


class TestChatCompletion:
    def test_completion_non_streaming(self, mock_client):
        """completion POSTs Stream=false and returns a completion result."""
        mock_client.request.return_value = {
            "ThreadGUID": THREAD_GUID,
            "TurnGUID": TURN_GUID,
            "Message": "The answer is 42.",
            "Provider": "OpenAI",
            "Model": "gpt-4o-mini",
            "PromptTokens": 10,
            "CompletionTokens": 7,
            "TotalDurationMs": 812.5,
            "ToolCallCount": 0,
            "ToolLoopIterations": 0,
            "RetrievedChunkCount": 0,
            "RetryCount": 0,
        }
        result = Chat.completion(
            "What is the answer?", thread_guid=THREAD_GUID, temperature=0.2
        )
        assert isinstance(result, ChatCompletionResultModel)
        assert result.message == "The answer is 42."
        assert result.turn_guid == TURN_GUID
        method, url = mock_client.request.call_args[0]
        assert method == "POST"
        assert url == f"{CHAT_BASE}/completions"
        body = mock_client.request.call_args[1]["json"]
        assert body["Message"] == "What is the answer?"
        assert body["Stream"] is False
        assert body["ThreadGUID"] == THREAD_GUID
        assert body["Temperature"] == 0.2

    def test_completion_streaming_parses_sse_frames(self, mock_client):
        """completion_streaming yields each event dict and stops at [DONE]."""
        lines = [
            'data: {"event":"started","threadGuid":"' + THREAD_GUID + '","turnGuid":"' + TURN_GUID + '"}',
            "",
            'data: {"event":"delta","content":"The answer"}',
            'data: {"event":"delta","content":" is 42."}',
            'data: {"event":"tool_call","name":"node/search","arguments":"{}","iteration":1}',
            'data: {"event":"tool_result","name":"node/search","success":true,"error":null,"runtimeMs":12.3}',
            'data: {"event":"usage","usage":{"ThreadGUID":"' + THREAD_GUID + '","TurnGUID":"' + TURN_GUID + '","Message":"The answer is 42.","Provider":"OpenAI","TotalDurationMs":812.5}}',
            "data: [DONE]",
            'data: {"event":"delta","content":"never seen"}',
        ]
        response = _FakeStreamResponse(lines)
        mock_client.client.stream.return_value = _FakeStreamContext(response)
        mock_client._get_headers.return_value = {"Content-Type": "application/json"}

        events = list(Chat.completion_streaming("What is the answer?"))

        assert [e["event"] for e in events] == [
            "started",
            "delta",
            "delta",
            "tool_call",
            "tool_result",
            "usage",
        ]
        assert events[0]["threadGuid"] == THREAD_GUID
        assert events[1]["content"] == "The answer"
        assert events[3]["name"] == "node/search"
        assert events[4]["success"] is True
        assert events[5]["usage"]["Message"] == "The answer is 42."

        method, url = mock_client.client.stream.call_args[0]
        assert method == "POST"
        assert url == f"{CHAT_BASE}/completions"
        body = mock_client.client.stream.call_args[1]["json"]
        assert body["Stream"] is True
        assert body["Message"] == "What is the answer?"

    def test_completion_streaming_skips_malformed_frame(self, mock_client):
        """A malformed SSE frame is skipped without breaking the stream."""
        lines = [
            'data: {"event":"started"}',
            "data: {not-valid-json",
            'data: {"event":"delta","content":"ok"}',
            "data: [DONE]",
        ]
        response = _FakeStreamResponse(lines)
        mock_client.client.stream.return_value = _FakeStreamContext(response)
        mock_client._get_headers.return_value = {}

        events = list(Chat.completion_streaming("hello"))
        assert [e["event"] for e in events] == ["started", "delta"]

    def test_completion_streaming_http_error(self, mock_client):
        """An HTTP error on the streaming request surfaces as an SDK exception."""
        response = _FakeStreamResponse(
            [],
            status_code=400,
            headers={"Content-Type": "application/json"},
            body=b'{"Error":"BadRequest","Description":"No completion endpoint resolves."}',
        )
        mock_client.client.stream.return_value = _FakeStreamContext(response)
        mock_client._get_headers.return_value = {}

        with pytest.raises(BadRequestError):
            list(Chat.completion_streaming("hello"))


class TestChatFeedback:
    def test_submit_feedback(self, mock_client):
        """submit_feedback POSTs the rating and text against the turn."""
        mock_client.request.return_value = {
            "GUID": FEEDBACK_GUID,
            "TenantGUID": TENANT_GUID,
            "ThreadGUID": THREAD_GUID,
            "TurnGUID": TURN_GUID,
            "UserGUID": "55555555-5555-5555-5555-555555555555",
            "Rating": "ThumbsUp",
            "FeedbackText": "Great answer",
            "CreatedUtc": "2026-08-31T00:00:00.000000Z",
        }
        result = Chat.submit_feedback(
            TURN_GUID, ChatFeedbackRating_Enum.ThumbsUp, feedback_text="Great answer"
        )
        assert isinstance(result, ChatFeedbackModel)
        assert result.rating == ChatFeedbackRating_Enum.ThumbsUp
        mock_client.request.assert_called_once_with(
            "POST",
            f"{CHAT_BASE}/turns/{TURN_GUID}/feedback",
            json={"Rating": "ThumbsUp", "FeedbackText": "Great answer"},
        )

    def test_read_feedback_list(self, mock_client):
        """read_feedback without a GUID lists all feedback records as an envelope."""
        mock_client.request.return_value = _envelope(
            [
                {
                    "GUID": FEEDBACK_GUID,
                    "TenantGUID": TENANT_GUID,
                    "ThreadGUID": THREAD_GUID,
                    "TurnGUID": TURN_GUID,
                    "UserGUID": "55555555-5555-5555-5555-555555555555",
                    "Rating": "ThumbsDown",
                }
            ]
        )
        result = Chat.read_feedback()
        assert result.total_records == 1
        assert isinstance(result.objects[0], ChatFeedbackModel)
        assert result.objects[0].rating == ChatFeedbackRating_Enum.ThumbsDown
        mock_client.request.assert_called_once_with("GET", f"{CHAT_BASE}/feedback")

    def test_read_feedback_single(self, mock_client):
        """read_feedback with a GUID reads a single record."""
        mock_client.request.return_value = {
            "GUID": FEEDBACK_GUID,
            "TenantGUID": TENANT_GUID,
            "ThreadGUID": THREAD_GUID,
            "TurnGUID": TURN_GUID,
            "UserGUID": "55555555-5555-5555-5555-555555555555",
            "Rating": "ThumbsUp",
        }
        result = Chat.read_feedback(FEEDBACK_GUID)
        assert isinstance(result, ChatFeedbackModel)
        assert result.guid == FEEDBACK_GUID
        mock_client.request.assert_called_once_with(
            "GET", f"{CHAT_BASE}/feedback/{FEEDBACK_GUID}"
        )

    def test_delete_feedback(self, mock_client):
        """delete_feedback issues a DELETE."""
        mock_client.request.return_value = None
        Chat.delete_feedback(FEEDBACK_GUID)
        mock_client.request.assert_called_once_with(
            "DELETE", f"{CHAT_BASE}/feedback/{FEEDBACK_GUID}"
        )


class TestChatSettings:
    def test_read_chat_settings(self, mock_client):
        """read_chat_settings GETs the tenant chat settings."""
        mock_client.request.return_value = {
            "TenantGUID": TENANT_GUID,
            "EnableChat": True,
            "EnableTools": True,
            "EnableMutationTools": False,
            "MaxToolIterations": 5,
            "EnableRag": True,
            "RagTopK": 8,
            "RagScoreThreshold": 0.0,
            "MaxContextTokens": 8192,
            "HistoryRetentionDays": 90,
        }
        result = Chat.read_chat_settings()
        assert isinstance(result, ChatSettingsModel)
        assert result.enable_chat is True
        assert result.rag_top_k == 8
        mock_client.request.assert_called_once_with("GET", f"{CHAT_BASE}/settings")

    def test_update_chat_settings(self, mock_client):
        """update_chat_settings PUTs the supplied fields with PascalCase keys."""
        mock_client.request.return_value = {
            "TenantGUID": TENANT_GUID,
            "EnableMutationTools": True,
            "RagTopK": 4,
        }
        result = Chat.update_chat_settings(
            tenant_guid=TENANT_GUID, enable_mutation_tools=True, rag_top_k=4
        )
        assert result.enable_mutation_tools is True
        assert result.rag_top_k == 4
        method, url = mock_client.request.call_args[0]
        assert method == "PUT"
        assert url == f"{CHAT_BASE}/settings"
        body = mock_client.request.call_args[1]["json"]
        assert body == {
            "TenantGUID": TENANT_GUID,
            "EnableMutationTools": True,
            "RagTopK": 4,
        }


class TestChatErrors:
    @pytest.fixture
    def real_client(self, monkeypatch):
        """A real BaseClient (mocked transport) registered as the SDK client."""
        with patch("httpx.Client"):
            client = BaseClient(
                base_url="http://test-api.com", tenant_guid=TENANT_GUID
            )
        monkeypatch.setattr("litegraph_sdk.configuration._client", client)
        return client

    def _http_status_error(self, status_code, json_body):
        mock_response = Mock(spec=httpx.Response)
        mock_response.status_code = status_code
        mock_response.headers = {"Content-Type": "application/json"}
        mock_response.json.return_value = json_body
        return httpx.HTTPStatusError(
            f"{status_code}", request=Mock(spec=httpx.Request), response=mock_response
        )

    def test_401_surfaces_authentication_error(self, real_client):
        """A 401 with an AuthenticationFailed body raises AuthenticationError."""
        error = self._http_status_error(
            401,
            {
                "Error": "AuthenticationFailed",
                "Description": "Your authentication material was not accepted.",
            },
        )
        with patch.object(real_client.client, "request", side_effect=error):
            with pytest.raises(AuthenticationError):
                Chat.read_endpoints()

    def test_voyageai_completion_validation_error(self, real_client):
        """Creating a VoyageAI completion endpoint surfaces the 400 as
        BadRequestError."""
        error = self._http_status_error(
            400,
            {
                "Error": "BadRequest",
                "Description": "VoyageAI endpoints support embeddings only.",
            },
        )
        with patch.object(real_client.client, "request", side_effect=error):
            with pytest.raises(BadRequestError):
                Chat.create_endpoint(
                    name="voyage-completion",
                    endpoint_type=ChatEndpointType_Enum.Completion,
                    provider=ChatProviderType_Enum.VoyageAI,
                    endpoint="https://api.voyageai.com/",
                    api_key="pa-secret",
                    model="voyage-3",
                )

    def test_tenant_required(self, monkeypatch):
        """Chat methods require a tenant GUID on the configured client."""
        client = Mock()
        client.tenant_guid = None
        monkeypatch.setattr("litegraph_sdk.configuration._client", client)
        with pytest.raises(ValueError):
            Chat.read_endpoints()
