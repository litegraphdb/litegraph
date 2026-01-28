import uuid
from datetime import datetime, timezone

import pytest
from litegraph_sdk.models.edge import EdgeModel
from litegraph_sdk.models.route_detail import RouteDetailModel
from pydantic import ValidationError


@pytest.fixture
def valid_edge_data():
    """Fixture providing valid edge data."""
    return {
        "GUID": str(uuid.uuid4()),
        "GraphGUID": str(uuid.uuid4()),
        "Name": "Test Edge",
        "From": str(uuid.uuid4()),
        "To": str(uuid.uuid4()),
        "Cost": 10,
        "CreatedUtc": datetime.now(timezone.utc),
        "Data": {"key": "value"},
    }


@pytest.fixture
def valid_route_detail_data(valid_edge_data):
    """Fixture providing valid route detail data."""
    return {"TotalCost": 100.5, "Edges": [valid_edge_data, valid_edge_data]}


def test_valid_route_detail_creation(valid_route_detail_data):
    """Test creating a route detail with valid data."""
    route_detail = RouteDetailModel(**valid_route_detail_data)
    assert isinstance(route_detail.TotalCost, float)
    assert len(route_detail.Edges) == 2
    assert all(isinstance(edge, EdgeModel) for edge in route_detail.Edges)


def test_empty_edges_list():
    """Test route detail with empty edges list."""
    route_detail = RouteDetailModel(TotalCost=0.0, Edges=[])
    assert route_detail.TotalCost == 0.0
    assert len(route_detail.Edges) == 0


def test_total_cost_validation():
    """Test TotalCost field validation."""
    valid_costs = [0.0, 1.5, -1.5, 100.0, float("inf"), float("-inf")]

    for cost in valid_costs:
        route_detail = RouteDetailModel(TotalCost=cost, Edges=[])
        assert route_detail.TotalCost == cost


def test_total_cost_type_conversion():
    """Test TotalCost type conversion."""
    test_cases = [
        (100, 100.0),  # Integer to float
        ("100", 100.0),  # String to float
        (True, 1.0),  # Boolean to float
        (False, 0.0),  # Boolean to float
    ]

    for input_cost, expected_cost in test_cases:
        route_detail = RouteDetailModel(TotalCost=input_cost, Edges=[])
        assert isinstance(route_detail.TotalCost, float)
        assert route_detail.TotalCost == expected_cost


def test_edges_type_validation():
    """Test edges list validation with valid edge data."""
    valid_edge = {
        "GUID": str(uuid.uuid4()),
        "GraphGUID": str(uuid.uuid4()),
        "From": str(uuid.uuid4()),
        "To": str(uuid.uuid4()),
        "Cost": 0,
    }

    route_detail = RouteDetailModel(TotalCost=0.0, Edges=[valid_edge])
    assert len(route_detail.Edges) == 1
    assert isinstance(route_detail.Edges[0], EdgeModel)


def test_multiple_edges(valid_edge_data):
    """Test route detail with multiple edges."""
    num_edges = 5
    edges = [valid_edge_data for _ in range(num_edges)]
    route_detail = RouteDetailModel(TotalCost=50.0, Edges=edges)

    assert len(route_detail.Edges) == num_edges
    assert all(isinstance(edge, EdgeModel) for edge in route_detail.Edges)


def test_model_serialization(valid_route_detail_data):
    """Test model serialization and deserialization."""
    route_detail = RouteDetailModel(**valid_route_detail_data)
    serialized = route_detail.model_dump()
    new_route_detail = RouteDetailModel(**serialized)

    assert new_route_detail.TotalCost == route_detail.TotalCost
    assert len(new_route_detail.Edges) == len(route_detail.Edges)
    assert all(isinstance(edge, EdgeModel) for edge in new_route_detail.Edges)


def test_float_precision():
    """Test handling of float precision in TotalCost."""
    test_cases = [
        (1.23456789, 1.23456789),  # Many decimal places
        (1.0, 1.0),  # Whole number as float
        (0.0001, 0.0001),  # Very small number
        (1e6, 1e6),  # Scientific notation
    ]

    for input_cost, expected_cost in test_cases:
        route_detail = RouteDetailModel(TotalCost=input_cost, Edges=[])
        assert route_detail.TotalCost == expected_cost


def test_edge_order_preservation(valid_edge_data):
    """Test that edge order is preserved."""
    edges = [{**valid_edge_data, "GUID": str(uuid.uuid4())} for _ in range(3)]

    route_detail = RouteDetailModel(TotalCost=0.0, Edges=edges)

    for i, edge in enumerate(route_detail.Edges):
        assert edge.guid == edges[i]["GUID"]


def test_missing_required_fields():
    """Test that missing required fields raise validation error."""
    invalid_cases = [
        {"Edges": []},  # Missing TotalCost
        {"TotalCost": 0.0},  # Missing Edges
        {},  # Missing all required fields
    ]

    for invalid_data in invalid_cases:
        with pytest.raises(ValidationError):
            RouteDetailModel(**invalid_data)


@pytest.mark.parametrize(
    "total_cost,num_edges",
    [
        (0.0, 0),  # Empty route
        (10.5, 1),  # Single edge
        (100.0, 5),  # Multiple edges
        (-50.0, 2),  # Negative cost
        (float("inf"), 3),  # Infinite cost
    ],
)
def test_various_combinations(total_cost, num_edges, valid_edge_data):
    """Test various combinations of total cost and number of edges."""
    edges = [valid_edge_data for _ in range(num_edges)]
    route_detail = RouteDetailModel(TotalCost=total_cost, Edges=edges)

    assert route_detail.TotalCost == total_cost
    assert len(route_detail.Edges) == num_edges


def test_edge_field_access(valid_route_detail_data):
    """Test accessing edge fields after model creation."""
    route_detail = RouteDetailModel(**valid_route_detail_data)

    for edge in route_detail.Edges:
        assert hasattr(edge, "guid")
        assert hasattr(edge, "cost")
        assert hasattr(edge, "from_node_guid")
        assert hasattr(edge, "to_node_guid")
