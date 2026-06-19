from unittest.mock import Mock

import pytest

from litegraph_sdk.base import BaseClient
from litegraph_sdk.models.node import NodeModel
from litegraph_sdk.models.transaction import TransactionRequestModel
from litegraph_sdk.resources.transactions import Transaction


@pytest.fixture
def mock_client(monkeypatch):
    client = Mock(spec=BaseClient)
    client.tenant_guid = "tenant-1"
    client.graph_guid = "graph-1"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_transaction_builder_serializes_typed_operations(mock_client):
    request = (
        Transaction.builder()
        .with_max_operations(10)
        .with_timeout_seconds(30)
        .with_isolation_level("Serializable")
        .create_node(
            NodeModel(
                GUID="node-1",
                TenantGUID="tenant-1",
                GraphGUID="graph-1",
                Name="Ada",
            )
        )
        .attach_tag(
            {"GUID": "tag-1", "NodeGUID": "node-1", "Key": "role", "Value": "engineer"}
        )
        .delete_edge("edge-1")
        .build()
    )

    payload = request.model_dump(mode="json", by_alias=True, exclude_none=True)

    assert payload["MaxOperations"] == 10
    assert payload["TimeoutSeconds"] == 30
    assert payload["IsolationLevel"] == "Serializable"
    assert payload["Operations"][0]["OperationType"] == "Create"
    assert payload["Operations"][0]["ObjectType"] == "Node"
    assert payload["Operations"][0]["Payload"]["Name"] == "Ada"
    assert payload["Operations"][1]["OperationType"] == "Attach"
    assert payload["Operations"][1]["ObjectType"] == "Tag"
    assert payload["Operations"][2]["OperationType"] == "Delete"
    assert payload["Operations"][2]["GUID"] == "edge-1"


def test_transaction_execute_posts_to_graph_scoped_endpoint(mock_client):
    mock_client.request.return_value = {
        "Success": True,
        "TransactionId": "11111111-1111-1111-1111-111111111111",
        "State": "Committed",
        "RolledBack": False,
        "ValidationFailure": False,
        "OperationCount": 1,
        "StartedUtc": "2026-06-17T19:00:00Z",
        "CompletedUtc": "2026-06-17T19:00:00.010Z",
        "Operations": [
            {
                "Index": 0,
                "OperationType": "Create",
                "ObjectType": "Node",
                "GUID": "node-1",
                "Success": True,
                "Result": {"GUID": "node-1"},
            }
        ],
        "DurationMs": 2.5,
        "QueueWaitDurationMs": 0,
        "CommitDurationMs": 0.5,
        "RollbackDurationMs": 0,
        "Provider": "Sqlite",
        "IsolationLevel": "Default",
        "IsolatedRepository": True,
        "SerializedByGate": False,
        "RetryCount": 0,
        "Retryable": False,
        "ConcurrencyConflict": False,
    }

    result = Transaction.execute(
        Transaction.builder().create_node({"GUID": "node-1", "Name": "Ada"})
    )

    mock_client.request.assert_called_once()
    args, kwargs = mock_client.request.call_args
    assert args[0] == "POST"
    assert args[1] == "v1.0/tenants/tenant-1/graphs/graph-1/transaction"
    assert kwargs["json"]["Operations"][0]["Payload"]["Name"] == "Ada"
    assert kwargs["json"]["IsolationLevel"] == "Default"
    assert kwargs["accepted_status_codes"] == [400, 409]
    assert result.success is True
    assert result.validation_failure is False
    assert result.transaction_id == "11111111-1111-1111-1111-111111111111"
    assert result.state == "Committed"
    assert result.operation_count == 1
    assert result.provider == "Sqlite"
    assert result.isolation_level == "Default"
    assert result.isolated_repository is True
    assert result.serialized_by_gate is False
    assert result.queue_wait_duration_ms == 0
    assert result.commit_duration_ms == 0.5
    assert result.operations[0].guid == "node-1"


def test_transaction_context_executes_on_clean_exit(mock_client):
    mock_client.request.return_value = {
        "Success": False,
        "TransactionId": "22222222-2222-2222-2222-222222222222",
        "State": "RolledBack",
        "RolledBack": True,
        "ValidationFailure": False,
        "FailedOperationIndex": 0,
        "Error": "duplicate node",
        "OperationCount": 1,
        "QueueWaitDurationMs": 0,
        "RollbackDurationMs": 0.75,
        "Provider": "Postgresql",
        "Retryable": True,
        "ConcurrencyConflict": True,
        "ProviderErrorCode": "40001",
        "Operations": [
            {
                "Index": 0,
                "OperationType": "Create",
                "ObjectType": "Node",
                "GUID": "node-1",
                "Success": False,
                "Error": "duplicate node",
            }
        ],
    }

    with Transaction.context() as tx:
        tx.create_node({"GUID": "node-1", "Name": "Ada"})

    assert tx.result is not None
    assert tx.result.success is False
    assert tx.result.state == "RolledBack"
    assert tx.result.rolled_back is True
    assert tx.result.validation_failure is False
    assert tx.result.failed_operation_index == 0
    assert tx.result.transaction_id == "22222222-2222-2222-2222-222222222222"
    assert tx.result.retryable is True
    assert tx.result.concurrency_conflict is True
    assert tx.result.provider_error_code == "40001"
    assert tx.result.queue_wait_duration_ms == 0
    assert tx.result.rollback_duration_ms == 0.75


def test_transaction_requires_graph_guid(mock_client):
    mock_client.graph_guid = None
    request = TransactionRequestModel(Operations=[])

    with pytest.raises(ValueError, match="Graph GUID is required"):
        Transaction.execute(request)
