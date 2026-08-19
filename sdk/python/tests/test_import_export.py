from unittest.mock import Mock

import pytest
from litegraph_sdk.base import BaseClient
from litegraph_sdk.models.import_export import (
    GraphImportResultModel,
    SubgraphExtractionRequestModel,
)
from litegraph_sdk.resources.graphs import Graph

TENANT_GUID = "11111111-1111-1111-1111-111111111111"
GRAPH_GUID = "22222222-2222-2222-2222-222222222222"

SAMPLE_JSONL = (
    "# litegraph-jsonl\n"
    '{"Type":"Graph","Object":{"GUID":"' + GRAPH_GUID + '"}}\n'
    '{"Type":"Node","Object":{"GUID":"33333333-3333-3333-3333-333333333333"}}\n'
)


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and configure it as the active SDK client."""
    client = Mock(spec=BaseClient)
    client.tenant_guid = TENANT_GUID
    client.graph_guid = GRAPH_GUID
    client.base_url = "http://127.0.0.1:8000"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_export_jsonl_returns_text(mock_client):
    """export_jsonl returns decoded JSONL text and hits the export endpoint."""
    mock_client.request.return_value = SAMPLE_JSONL.encode("utf-8")
    mock_client.request.side_effect = None

    result = Graph.export_jsonl(GRAPH_GUID)

    assert isinstance(result, str)
    assert "# litegraph-jsonl" in result

    called_args = mock_client.request.call_args
    assert called_args[0][0] == "GET"
    assert f"tenants/{TENANT_GUID}/graphs/{GRAPH_GUID}/export/jsonl" in called_args[0][1]


def test_export_jsonl_with_flags(mock_client):
    """export_jsonl includes incldata and inclsub flags when requested."""
    mock_client.request.return_value = SAMPLE_JSONL.encode("utf-8")
    mock_client.request.side_effect = None

    Graph.export_jsonl(GRAPH_GUID, include_data=True, include_subordinates=True)

    url = mock_client.request.call_args[0][1]
    assert "incldata" in url
    assert "inclsub" in url


def test_export_subgraph_jsonl_posts_request(mock_client):
    """export_subgraph_jsonl posts the request body and returns JSONL text."""
    mock_client.request.return_value = SAMPLE_JSONL.encode("utf-8")
    mock_client.request.side_effect = None

    request = SubgraphExtractionRequestModel(
        tenant_guid=TENANT_GUID,
        graph_guid=GRAPH_GUID,
        start_node_guids=["33333333-3333-3333-3333-333333333333"],
        max_depth=3,
        direction="Outbound",
    )
    result = Graph.export_subgraph_jsonl(GRAPH_GUID, request)

    assert isinstance(result, str)
    assert "# litegraph-jsonl" in result

    called_args = mock_client.request.call_args
    assert called_args[0][0] == "POST"
    assert f"tenants/{TENANT_GUID}/graphs/{GRAPH_GUID}/export/jsonl" in called_args[0][1]
    sent_body = called_args[1]["json"]
    assert sent_body["StartNodeGUIDs"] == ["33333333-3333-3333-3333-333333333333"]
    assert sent_body["MaxDepth"] == 3
    assert sent_body["Direction"] == "Outbound"


def test_export_subgraph_jsonl_accepts_dict(mock_client):
    """export_subgraph_jsonl accepts a plain dict request."""
    mock_client.request.return_value = SAMPLE_JSONL.encode("utf-8")
    mock_client.request.side_effect = None

    result = Graph.export_subgraph_jsonl(
        GRAPH_GUID,
        {"StartNodeGUIDs": ["33333333-3333-3333-3333-333333333333"], "MaxDepth": 1},
    )
    assert isinstance(result, str)
    sent_body = mock_client.request.call_args[1]["json"]
    assert sent_body["MaxDepth"] == 1


def test_import_jsonl_returns_result(mock_client):
    """import_jsonl posts raw JSONL and returns a GraphImportResult dict."""
    import_result = {
        "Success": True,
        "TenantGUID": TENANT_GUID,
        "GraphGUID": GRAPH_GUID,
        "GraphsCreated": 0,
        "NodesCreated": 1,
        "NodesUpdated": 0,
        "NodesSkipped": 0,
        "EdgesCreated": 0,
        "EdgesUpdated": 0,
        "EdgesSkipped": 0,
        "LinesRead": 3,
        "LinesIgnored": 1,
        "Warnings": [],
        "GuidMap": {},
    }
    mock_client.request.return_value = import_result
    mock_client.request.side_effect = None

    result = Graph.import_jsonl(
        GRAPH_GUID,
        SAMPLE_JSONL,
        guid_strategy="preserve",
        on_error="abort",
        batch_size=100,
    )

    assert isinstance(result, dict)
    assert result["Success"] is True
    assert result["NodesCreated"] == 1

    # The model can validate the raw dict.
    model = GraphImportResultModel.model_validate(result)
    assert model.success is True
    assert model.lines_read == 3

    called_args = mock_client.request.call_args
    assert called_args[0][0] == "POST"
    url = called_args[0][1]
    assert f"tenants/{TENANT_GUID}/graphs/{GRAPH_GUID}/import/jsonl" in url
    assert "guidstrategy=preserve" in url
    assert "onerror=abort" in url
    assert "batchsize=100" in url
    assert called_args[1]["content"] == SAMPLE_JSONL
    assert called_args[1]["headers"] == {"Content-Type": "application/x-ndjson"}


def test_import_jsonl_as_new_returns_result(mock_client):
    """import_jsonl_as_new posts raw JSONL to the graph-less import endpoint."""
    import_result = {
        "Success": True,
        "TenantGUID": TENANT_GUID,
        "GraphGUID": "44444444-4444-4444-4444-444444444444",
        "GraphsCreated": 1,
        "NodesCreated": 1,
        "LinesRead": 3,
        "LinesIgnored": 1,
    }
    mock_client.request.return_value = import_result
    mock_client.request.side_effect = None

    result = Graph.import_jsonl_as_new(SAMPLE_JSONL, guid_strategy="regenerate")

    assert isinstance(result, dict)
    assert result["Success"] is True
    assert result["GraphsCreated"] == 1

    called_args = mock_client.request.call_args
    assert called_args[0][0] == "POST"
    url = called_args[0][1]
    assert f"tenants/{TENANT_GUID}/graphs/import/jsonl" in url
    assert f"graphs/{GRAPH_GUID}" not in url
    assert "guidstrategy=regenerate" in url
    assert called_args[1]["headers"] == {"Content-Type": "application/x-ndjson"}


def test_import_jsonl_invalid_guid_strategy(mock_client):
    """Invalid guid_strategy raises ValueError before any request is made."""
    mock_client.request.side_effect = None
    with pytest.raises(ValueError, match="guid_strategy must be one of"):
        Graph.import_jsonl(GRAPH_GUID, SAMPLE_JSONL, guid_strategy="bogus")
    mock_client.request.assert_not_called()


def test_import_jsonl_invalid_batch_size(mock_client):
    """Non-positive batch_size raises ValueError."""
    mock_client.request.side_effect = None
    with pytest.raises(ValueError, match="batch_size must be a positive integer"):
        Graph.import_jsonl(GRAPH_GUID, SAMPLE_JSONL, batch_size=0)


def test_import_jsonl_invalid_on_error(mock_client):
    """Invalid on_error raises ValueError."""
    mock_client.request.side_effect = None
    with pytest.raises(ValueError, match="on_error must be one of"):
        Graph.import_jsonl(GRAPH_GUID, SAMPLE_JSONL, on_error="explode")
