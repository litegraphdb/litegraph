import uuid
from datetime import datetime, timezone

import pytest
from litegraph_sdk.models.node import NodeModel
from pydantic import ValidationError


@pytest.fixture
def valid_node_data():
    """Fixture providing valid node data."""
    return {
        "GUID": str(uuid.uuid4()),
        "GraphGUID": str(uuid.uuid4()),
        "Name": "Test Node",
        "Data": {"key": "value"},
        "CreatedUtc": datetime.now(timezone.utc).isoformat(),
    }


@pytest.fixture
def minimal_node_data():
    """Fixture providing minimal valid node data."""
    return {"GUID": str(uuid.uuid4()), "GraphGUID": str(uuid.uuid4())}


def test_valid_node_creation(valid_node_data):
    """Test creating a node with valid data."""
    node = NodeModel(**valid_node_data)
    assert node.guid == valid_node_data["GUID"]
    assert node.graph_guid == valid_node_data["GraphGUID"]
    assert node.name == valid_node_data["Name"]
    assert node.data == valid_node_data["Data"]
    assert isinstance(node.created_utc, datetime)


def test_minimal_node_creation(minimal_node_data):
    """Test creating a node with minimal required data."""
    node = NodeModel(**minimal_node_data)
    assert node.guid == minimal_node_data["GUID"]
    assert node.graph_guid == minimal_node_data["GraphGUID"]
    assert node.name is None
    assert node.data is None
    assert isinstance(node.created_utc, datetime)


def test_auto_generated_fields():
    """Test auto-generated fields when creating node without providing values."""
    node = NodeModel()

    # Test GUID auto-generation
    assert isinstance(node.guid, str)
    uuid.UUID(node.guid)  # Validates UUID format

    # Test GraphGUID auto-generation
    assert isinstance(node.graph_guid, str)
    uuid.UUID(node.graph_guid)  # Validates UUID format

    # Test CreatedUtc auto-generation
    assert isinstance(node.created_utc, datetime)
    assert node.created_utc.tzinfo == timezone.utc


def test_strict_type_validation():
    """Test strict type validation for GUID and GraphGUID fields."""
    with pytest.raises(ValidationError):
        NodeModel(GUID=123)  # Non-string GUID should fail

    with pytest.raises(ValidationError):
        NodeModel(GraphGUID=123)  # Non-string GraphGUID should fail


def test_data_field_validation():
    """Test validation of the Data field."""
    valid_data_cases = [
        {"key": "value"},
        {"numbers": [1, 2, 3]},
        {"nested": {"key": "value"}},
        None,
    ]

    for data in valid_data_cases:
        node = NodeModel(GUID=str(uuid.uuid4()), GraphGUID=str(uuid.uuid4()), Data=data)
        assert node.data == data


def test_created_utc_validation():
    """Test CreatedUtc field validation."""
    # Test valid datetime formats
    valid_dates = [datetime.now(timezone.utc), datetime.now(timezone.utc).isoformat()]

    for date in valid_dates:
        node = NodeModel(
            GUID=str(uuid.uuid4()), GraphGUID=str(uuid.uuid4()), CreatedUtc=date
        )
        assert isinstance(node.created_utc, datetime)
        assert node.created_utc.tzinfo == timezone.utc

    # Test invalid datetime formats
    invalid_dates = ["not-a-date", "2024-99-99", {"date": "2024-01-01"}]

    for date in invalid_dates:
        with pytest.raises(ValidationError):
            NodeModel(
                GUID=str(uuid.uuid4()), GraphGUID=str(uuid.uuid4()), CreatedUtc=date
            )


def test_model_export():
    """Test model serialization."""
    test_data = {
        "GUID": str(uuid.uuid4()),
        "GraphGUID": str(uuid.uuid4()),
        "Name": "Test Node",
        "Data": {"key": "value"},
    }

    node = NodeModel(**test_data)
    exported = node.model_dump(by_alias=True)

    assert exported["GUID"] == test_data["GUID"]
    assert exported["GraphGUID"] == test_data["GraphGUID"]
    assert exported["Name"] == test_data["Name"]
    assert exported["Data"] == test_data["Data"]
    assert "CreatedUtc" in exported


def test_name_validation():
    """Test name field validation."""
    valid_names = [
        "Test Node",
        "",  # Empty string is valid
        None,  # None is valid
        "Node 123",
        "Special Characters !@#",
    ]

    for name in valid_names:
        node = NodeModel(GUID=str(uuid.uuid4()), GraphGUID=str(uuid.uuid4()), Name=name)
        assert node.name == name


@pytest.mark.parametrize(
    "field,value,valid",
    [
        pytest.param("guid", str(uuid.uuid4()), True, id="valid_guid"),
        pytest.param("guid", 123, False, id="invalid_guid_type"),
        pytest.param("graph_guid", str(uuid.uuid4()), True, id="valid_graph_guid"),
        pytest.param("graph_guid", 123, False, id="invalid_graph_guid_type"),
        pytest.param("name", "Test Node", True, id="valid_name"),
        pytest.param("name", None, True, id="null_name"),
        pytest.param("name", "", True, id="empty_name"),
        pytest.param("data", {"key": "value"}, True, id="valid_data"),
        pytest.param("data", None, True, id="null_data"),
        pytest.param("data", "invalid", False, id="invalid_data_type"),
    ],
)
def test_field_validation(field, value, valid):
    """Test field validation with various inputs."""
    test_data = {
        "guid": str(uuid.uuid4()),
        "graph_guid": str(uuid.uuid4())
    }
    test_data[field] = value

    if valid:
        node = NodeModel(**test_data)
        assert getattr(node, field) == value
    else:
        with pytest.raises(ValidationError):
            NodeModel(**test_data)


def test_model_config():
    """Test model configuration for field aliases and population by name."""
    test_guid = str(uuid.uuid4())
    test_graph_guid = str(uuid.uuid4())

    # Test using Python attribute names
    node = NodeModel(
        **{"guid": test_guid, "graph_guid": test_graph_guid, "name": "Test Node"}
    )

    assert node.guid == test_guid
    assert node.graph_guid == test_graph_guid
    assert node.name == "Test Node"

    # Test serialization with aliases
    exported = node.model_dump(by_alias=True)
    assert "GUID" in exported
    assert "GraphGUID" in exported
    assert "Name" in exported


def test_created_utc_timezone():
    """Test that CreatedUtc is always in UTC timezone."""
    node = NodeModel(GUID=str(uuid.uuid4()), GraphGUID=str(uuid.uuid4()))
    assert node.created_utc.tzinfo == timezone.utc

    # Test with provided datetime
    custom_date = datetime.now(timezone.utc)
    node = NodeModel(
        GUID=str(uuid.uuid4()), GraphGUID=str(uuid.uuid4()), CreatedUtc=custom_date
    )
    assert node.created_utc.tzinfo == timezone.utc
