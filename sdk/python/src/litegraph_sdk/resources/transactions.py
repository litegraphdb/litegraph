from typing import Any, Dict, List, Optional, Union

from pydantic import BaseModel

from ..configuration import get_client
from ..exceptions import GRAPH_REQUIRED_ERROR, TENANT_REQUIRED_ERROR
from ..mixins import JSON_CONTENT_TYPE
from ..models.transaction import (
    TransactionOperationModel,
    TransactionRequestModel,
    TransactionResultModel,
)


def _payload_to_json(payload: Any) -> Any:
    if isinstance(payload, BaseModel):
        return payload.model_dump(
            mode="json", by_alias=True, exclude_none=True, exclude_unset=True
        )
    return payload


class TransactionBuilder:
    """
    Builds and optionally executes a graph-scoped transaction request.
    """

    def __init__(
        self,
        graph_guid: Optional[str] = None,
        max_operations: int = 1000,
        timeout_seconds: int = 60,
        isolation_level: str = "Default",
        execute_on_exit: bool = False,
    ):
        self.graph_guid = graph_guid
        self.max_operations = max_operations
        self.timeout_seconds = timeout_seconds
        self.isolation_level = isolation_level
        self.operations: List[TransactionOperationModel] = []
        self.execute_on_exit = execute_on_exit
        self.result: Optional[TransactionResultModel] = None

    def __enter__(self) -> "TransactionBuilder":
        return self

    def __exit__(self, exc_type, exc, traceback) -> bool:
        if exc_type is None and self.execute_on_exit:
            self.result = self.execute()
        return False

    def with_max_operations(self, max_operations: int) -> "TransactionBuilder":
        TransactionRequestModel(MaxOperations=max_operations)
        self.max_operations = max_operations
        return self

    def with_timeout_seconds(self, timeout_seconds: int) -> "TransactionBuilder":
        TransactionRequestModel(TimeoutSeconds=timeout_seconds)
        self.timeout_seconds = timeout_seconds
        return self

    def with_isolation_level(self, isolation_level: str) -> "TransactionBuilder":
        allowed = {"Default", "ReadCommitted", "RepeatableRead", "Serializable"}
        if isolation_level not in allowed:
            raise ValueError(
                "IsolationLevel must be Default, ReadCommitted, RepeatableRead, or Serializable."
            )
        self.isolation_level = isolation_level
        return self

    def add(self, operation: Union[TransactionOperationModel, Dict[str, Any]]):
        if operation is None:
            raise ValueError("Transaction operation is required")
        if isinstance(operation, TransactionOperationModel):
            self.operations.append(operation)
        else:
            self.operations.append(TransactionOperationModel.model_validate(operation))
        return self

    def create(self, object_type: str, payload: Any, guid: Optional[str] = None):
        return self._payload_operation("Create", object_type, payload, guid)

    def update(self, object_type: str, payload: Any, guid: Optional[str] = None):
        return self._payload_operation("Update", object_type, payload, guid)

    def delete(self, object_type: str, guid: str):
        return self.add(
            TransactionOperationModel(
                OperationType="Delete", ObjectType=object_type, GUID=guid
            )
        )

    def attach(self, object_type: str, payload: Any, guid: Optional[str] = None):
        return self._payload_operation("Attach", object_type, payload, guid)

    def detach(self, object_type: str, guid: str):
        return self.add(
            TransactionOperationModel(
                OperationType="Detach", ObjectType=object_type, GUID=guid
            )
        )

    def upsert(self, object_type: str, payload: Any, guid: Optional[str] = None):
        return self._payload_operation("Upsert", object_type, payload, guid)

    def create_node(self, payload: Any):
        return self.create("Node", payload)

    def update_node(self, payload: Any, guid: Optional[str] = None):
        return self.update("Node", payload, guid)

    def delete_node(self, guid: str):
        return self.delete("Node", guid)

    def upsert_node(self, payload: Any, guid: Optional[str] = None):
        return self.upsert("Node", payload, guid)

    def create_edge(self, payload: Any):
        return self.create("Edge", payload)

    def update_edge(self, payload: Any, guid: Optional[str] = None):
        return self.update("Edge", payload, guid)

    def delete_edge(self, guid: str):
        return self.delete("Edge", guid)

    def upsert_edge(self, payload: Any, guid: Optional[str] = None):
        return self.upsert("Edge", payload, guid)

    def create_label(self, payload: Any):
        return self.create("Label", payload)

    def update_label(self, payload: Any, guid: Optional[str] = None):
        return self.update("Label", payload, guid)

    def delete_label(self, guid: str):
        return self.delete("Label", guid)

    def attach_label(self, payload: Any, guid: Optional[str] = None):
        return self.attach("Label", payload, guid)

    def detach_label(self, guid: str):
        return self.detach("Label", guid)

    def upsert_label(self, payload: Any, guid: Optional[str] = None):
        return self.upsert("Label", payload, guid)

    def create_tag(self, payload: Any):
        return self.create("Tag", payload)

    def update_tag(self, payload: Any, guid: Optional[str] = None):
        return self.update("Tag", payload, guid)

    def delete_tag(self, guid: str):
        return self.delete("Tag", guid)

    def attach_tag(self, payload: Any, guid: Optional[str] = None):
        return self.attach("Tag", payload, guid)

    def detach_tag(self, guid: str):
        return self.detach("Tag", guid)

    def upsert_tag(self, payload: Any, guid: Optional[str] = None):
        return self.upsert("Tag", payload, guid)

    def create_vector(self, payload: Any):
        return self.create("Vector", payload)

    def update_vector(self, payload: Any, guid: Optional[str] = None):
        return self.update("Vector", payload, guid)

    def delete_vector(self, guid: str):
        return self.delete("Vector", guid)

    def attach_vector(self, payload: Any, guid: Optional[str] = None):
        return self.attach("Vector", payload, guid)

    def detach_vector(self, guid: str):
        return self.detach("Vector", guid)

    def upsert_vector(self, payload: Any, guid: Optional[str] = None):
        return self.upsert("Vector", payload, guid)

    def build(self) -> TransactionRequestModel:
        return TransactionRequestModel(
            Operations=self.operations,
            MaxOperations=self.max_operations,
            TimeoutSeconds=self.timeout_seconds,
            IsolationLevel=self.isolation_level,
        )

    def execute(self, graph_guid: Optional[str] = None) -> TransactionResultModel:
        return Transaction.execute(self.build(), graph_guid or self.graph_guid)

    def _payload_operation(
        self,
        operation_type: str,
        object_type: str,
        payload: Any,
        guid: Optional[str],
    ):
        return self.add(
            TransactionOperationModel(
                OperationType=operation_type,
                ObjectType=object_type,
                GUID=guid,
                Payload=_payload_to_json(payload),
            )
        )


class Transaction:
    """
    Graph-scoped transaction helpers.
    """

    @classmethod
    def builder(
        cls,
        graph_guid: Optional[str] = None,
        max_operations: int = 1000,
        timeout_seconds: int = 60,
        isolation_level: str = "Default",
    ) -> TransactionBuilder:
        return TransactionBuilder(
            graph_guid, max_operations, timeout_seconds, isolation_level
        )

    @classmethod
    def context(
        cls,
        graph_guid: Optional[str] = None,
        max_operations: int = 1000,
        timeout_seconds: int = 60,
        isolation_level: str = "Default",
    ) -> TransactionBuilder:
        return TransactionBuilder(
            graph_guid,
            max_operations,
            timeout_seconds,
            isolation_level,
            execute_on_exit=True,
        )

    @classmethod
    def execute(
        cls,
        request: Union[TransactionRequestModel, TransactionBuilder, Dict[str, Any]],
        graph_guid: Optional[str] = None,
    ) -> TransactionResultModel:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        gid = graph_guid or client.graph_guid
        if not gid:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        if isinstance(request, TransactionBuilder):
            request = request.build()
        elif isinstance(request, dict):
            request = TransactionRequestModel.model_validate(request)
        elif not isinstance(request, TransactionRequestModel):
            raise TypeError("request must be a TransactionRequestModel, builder, or dict")

        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/transaction"
        response = client.request(
            "POST",
            url,
            json=data,
            headers=JSON_CONTENT_TYPE,
            accepted_status_codes=[400, 409],
        )
        return TransactionResultModel.model_validate(response)
