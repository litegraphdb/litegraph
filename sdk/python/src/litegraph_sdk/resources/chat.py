import json
from typing import Generator, List, Optional, Union

from ..configuration import get_client
from ..enums.chat_endpoint_type_enum import ChatEndpointType_Enum
from ..enums.chat_feedback_rating_enum import ChatFeedbackRating_Enum
from ..enums.severity_enum import Severity_Enum
from ..exceptions import TENANT_REQUIRED_ERROR, SdkException, get_exception_for_error_code
from ..models.api_error import ApiErrorResponseModel
from ..models.chat import (
    ChatCompletionRequestModel,
    ChatCompletionResultModel,
    ChatEndpointHealthModel,
    ChatEndpointModel,
    ChatEndpointTestResultModel,
    ChatFeedbackModel,
    ChatModelSummaryModel,
    ChatSettingsModel,
    ChatThreadModel,
    ChatTurnModel,
)
from ..sdk_logging import log_warning

SSE_DATA_PREFIX = "data:"
SSE_DONE_SENTINEL = "[DONE]"


class Chat:
    """
    Chat resource class. Wraps the LiteGraph chat surface: endpoint CRUD and
    health, the model catalog, completions (non-streaming and streaming SSE),
    threads, turns, feedback, and tenant chat settings.
    """

    @classmethod
    def _base_url(cls) -> str:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        return f"v1.0/tenants/{client.tenant_guid}/chat"

    # region Endpoints

    @classmethod
    def create_endpoint(cls, **kwargs) -> ChatEndpointModel:
        """Create a chat endpoint. The ApiKey is redacted in the response."""
        client = get_client()
        data = ChatEndpointModel(**kwargs).model_dump(
            mode="json", by_alias=True, exclude_unset=True
        )
        instance = client.request("PUT", f"{cls._base_url()}/endpoints", json=data)
        return ChatEndpointModel.model_validate(instance)

    @classmethod
    def read_endpoints(
        cls, endpoint_type: Optional[Union[ChatEndpointType_Enum, str]] = None
    ) -> List[ChatEndpointModel]:
        """Read all chat endpoints, optionally filtered by endpoint type."""
        client = get_client()
        url = f"{cls._base_url()}/endpoints"
        if endpoint_type is not None:
            type_value = (
                endpoint_type.value
                if isinstance(endpoint_type, ChatEndpointType_Enum)
                else str(endpoint_type)
            )
            url = f"{url}?endpointType={type_value}"
        instances = client.request("GET", url)
        return [ChatEndpointModel.model_validate(instance) for instance in instances]

    @classmethod
    def read_endpoint(cls, endpoint_guid: str) -> ChatEndpointModel:
        """Read a chat endpoint by GUID."""
        client = get_client()
        instance = client.request(
            "GET", f"{cls._base_url()}/endpoints/{endpoint_guid}"
        )
        return ChatEndpointModel.model_validate(instance)

    @classmethod
    def endpoint_exists(cls, endpoint_guid: str) -> bool:
        """Check whether a chat endpoint exists."""
        client = get_client()
        try:
            client.request("HEAD", f"{cls._base_url()}/endpoints/{endpoint_guid}")
            return True
        except Exception:
            return False

    @classmethod
    def update_endpoint(cls, endpoint_guid: str, **kwargs) -> ChatEndpointModel:
        """Update a chat endpoint (full object PUT). Sending back a redacted
        ApiKey preserves the stored key."""
        client = get_client()
        data = ChatEndpointModel(**kwargs).model_dump(
            mode="json", by_alias=True, exclude_unset=True
        )
        instance = client.request(
            "PUT", f"{cls._base_url()}/endpoints/{endpoint_guid}", json=data
        )
        return ChatEndpointModel.model_validate(instance)

    @classmethod
    def delete_endpoint(cls, endpoint_guid: str) -> None:
        """Delete a chat endpoint."""
        client = get_client()
        client.request("DELETE", f"{cls._base_url()}/endpoints/{endpoint_guid}")

    @classmethod
    def test_endpoint(cls, endpoint_guid: str) -> ChatEndpointTestResultModel:
        """Run a connectivity test against a chat endpoint."""
        client = get_client()
        instance = client.request(
            "POST", f"{cls._base_url()}/endpoints/{endpoint_guid}/test"
        )
        return ChatEndpointTestResultModel.model_validate(instance)

    @classmethod
    def read_endpoint_health(cls, endpoint_guid: str) -> ChatEndpointHealthModel:
        """Read health status for a single chat endpoint."""
        client = get_client()
        instance = client.request(
            "GET", f"{cls._base_url()}/endpoints/{endpoint_guid}/health"
        )
        return ChatEndpointHealthModel.model_validate(instance)

    @classmethod
    def read_all_endpoint_health(cls) -> List[ChatEndpointHealthModel]:
        """Read health status for every chat endpoint in the tenant."""
        client = get_client()
        instances = client.request("GET", f"{cls._base_url()}/endpoints/health")
        return [
            ChatEndpointHealthModel.model_validate(instance) for instance in instances
        ]

    # endregion

    # region Models

    @classmethod
    def read_models(cls) -> List[ChatModelSummaryModel]:
        """Read the model catalog: active chat endpoints projected as
        non-privileged model summaries (no administrator role required).
        Endpoint URLs, keys, and health configuration are never included."""
        client = get_client()
        instances = client.request("GET", f"{cls._base_url()}/models")
        return [
            ChatModelSummaryModel.model_validate(instance) for instance in instances
        ]

    # endregion

    # region Threads

    @classmethod
    def create_thread(
        cls, graph_guid: Optional[str] = None, title: Optional[str] = None
    ) -> ChatThreadModel:
        """Create a chat thread owned by the caller, optionally bound to a graph."""
        client = get_client()
        body = {}
        if graph_guid is not None:
            body["GraphGUID"] = graph_guid
        if title is not None:
            body["Title"] = title
        instance = client.request("PUT", f"{cls._base_url()}/threads", json=body)
        return ChatThreadModel.model_validate(instance)

    @classmethod
    def read_threads(cls, all_users: bool = False) -> List[ChatThreadModel]:
        """Read the caller's chat threads, or every user's threads when
        all_users is True (administrators only)."""
        client = get_client()
        url = f"{cls._base_url()}/threads"
        if all_users:
            url = f"{url}?all"
        instances = client.request("GET", url)
        return [ChatThreadModel.model_validate(instance) for instance in instances]

    @classmethod
    def read_thread(cls, thread_guid: str) -> ChatThreadModel:
        """Read a chat thread by GUID (owner or administrator)."""
        client = get_client()
        instance = client.request("GET", f"{cls._base_url()}/threads/{thread_guid}")
        return ChatThreadModel.model_validate(instance)

    @classmethod
    def update_thread(cls, thread_guid: str, title: str) -> ChatThreadModel:
        """Update (rename) a chat thread. Only the Title is honored and it
        must be non-empty (owner or administrator)."""
        client = get_client()
        body = {"Title": title}
        instance = client.request(
            "PUT", f"{cls._base_url()}/threads/{thread_guid}", json=body
        )
        return ChatThreadModel.model_validate(instance)

    @classmethod
    def delete_thread(cls, thread_guid: str) -> None:
        """Delete a chat thread along with its turns and feedback."""
        client = get_client()
        client.request("DELETE", f"{cls._base_url()}/threads/{thread_guid}")

    @classmethod
    def read_thread_turns(cls, thread_guid: str) -> List[ChatTurnModel]:
        """Read the turns for a thread, ascending by sequence."""
        client = get_client()
        instances = client.request(
            "GET", f"{cls._base_url()}/threads/{thread_guid}/turns"
        )
        return [ChatTurnModel.model_validate(instance) for instance in instances]

    # endregion

    # region Completions

    @classmethod
    def completion(cls, message: str, **kwargs) -> ChatCompletionResultModel:
        """Run a non-streaming chat completion.

        Accepts any ChatCompletionRequestModel field as a keyword argument
        (thread_guid, graph_guid, completion_endpoint_guid,
        embedding_endpoint_guid, temperature, max_output_tokens, enable_tools,
        enable_rag, rag_top_k, system_prompt). Stream is always forced False.
        """
        client = get_client()
        kwargs.pop("stream", None)
        kwargs.pop("Stream", None)
        request = ChatCompletionRequestModel(message=message, stream=False, **kwargs)
        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        instance = client.request("POST", f"{cls._base_url()}/completions", json=data)
        return ChatCompletionResultModel.model_validate(instance)

    @classmethod
    def completion_streaming(
        cls, message: str, **kwargs
    ) -> Generator[dict, None, None]:
        """Run a streaming chat completion and yield parsed SSE event dicts.

        Each yielded dict carries an "event" discriminator: started, delta,
        thinking, retrieval, tool_call, tool_result, usage, or error. The
        generator ends when the server sends the [DONE] sentinel. Malformed
        SSE frames are logged and skipped.
        """
        client = get_client()
        kwargs.pop("stream", None)
        kwargs.pop("Stream", None)
        request = ChatCompletionRequestModel(message=message, stream=True, **kwargs)
        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        url = f"{cls._base_url()}/completions"
        headers = client._get_headers()

        with client.client.stream("POST", url, json=data, headers=headers) as response:
            if response.status_code >= 400:
                cls._raise_stream_error(response)
            for line in response.iter_lines():
                if line is None:
                    continue
                if isinstance(line, bytes):
                    line = line.decode("utf-8", errors="replace")
                line = line.strip()
                if not line.startswith(SSE_DATA_PREFIX):
                    continue
                payload = line[len(SSE_DATA_PREFIX):].strip()
                if payload == SSE_DONE_SENTINEL:
                    return
                try:
                    yield json.loads(payload)
                except json.JSONDecodeError:
                    log_warning(
                        Severity_Enum.Warn.value,
                        f"Skipping malformed SSE frame: {payload}",
                    )

    @classmethod
    def _raise_stream_error(cls, response) -> None:
        body = response.read()
        content_type = response.headers.get("Content-Type", "")
        if "application/json" in content_type:
            try:
                error_response = ApiErrorResponseModel(**json.loads(body))
                raise get_exception_for_error_code(error_response.error)
            except (json.JSONDecodeError, ValueError):
                pass
        raise SdkException(
            f"Streaming request failed with status {response.status_code}"
        )

    # endregion

    # region Feedback

    @classmethod
    def submit_feedback(
        cls,
        turn_guid: str,
        rating: Union[ChatFeedbackRating_Enum, str],
        feedback_text: Optional[str] = None,
    ) -> ChatFeedbackModel:
        """Submit feedback (ThumbsUp or ThumbsDown) for a chat turn."""
        client = get_client()
        rating_value = (
            rating.value if isinstance(rating, ChatFeedbackRating_Enum) else str(rating)
        )
        body = {"Rating": rating_value}
        if feedback_text is not None:
            body["FeedbackText"] = feedback_text
        instance = client.request(
            "POST", f"{cls._base_url()}/turns/{turn_guid}/feedback", json=body
        )
        return ChatFeedbackModel.model_validate(instance)

    @classmethod
    def read_feedback(
        cls, feedback_guid: Optional[str] = None
    ) -> Union[ChatFeedbackModel, List[ChatFeedbackModel]]:
        """Read a single feedback record by GUID, or all feedback records when
        no GUID is supplied (administrators only)."""
        client = get_client()
        if feedback_guid is not None:
            instance = client.request(
                "GET", f"{cls._base_url()}/feedback/{feedback_guid}"
            )
            return ChatFeedbackModel.model_validate(instance)
        instances = client.request("GET", f"{cls._base_url()}/feedback")
        return [ChatFeedbackModel.model_validate(instance) for instance in instances]

    @classmethod
    def delete_feedback(cls, feedback_guid: str) -> None:
        """Delete a feedback record (administrators only)."""
        client = get_client()
        client.request("DELETE", f"{cls._base_url()}/feedback/{feedback_guid}")

    # endregion

    # region Settings

    @classmethod
    def read_chat_settings(cls) -> ChatSettingsModel:
        """Read the tenant chat settings. Defaults are returned when no record
        exists."""
        client = get_client()
        instance = client.request("GET", f"{cls._base_url()}/settings")
        return ChatSettingsModel.model_validate(instance)

    @classmethod
    def update_chat_settings(cls, **kwargs) -> ChatSettingsModel:
        """Upsert the tenant chat settings (administrators only)."""
        client = get_client()
        data = ChatSettingsModel(**kwargs).model_dump(
            mode="json", by_alias=True, exclude_unset=True
        )
        instance = client.request("PUT", f"{cls._base_url()}/settings", json=data)
        return ChatSettingsModel.model_validate(instance)

    # endregion
