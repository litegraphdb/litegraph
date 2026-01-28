import uuid
from datetime import datetime, timezone

import pytest
from litegraph_sdk.models.edge import EdgeModel
from litegraph_sdk.models.route_response import (
    RouteDetailModel,
    RouteResultModel,
    Timestamp,
)


@pytest.fixture
def sample_edge():
    return EdgeModel(
        guid=str(uuid.uuid4()),
        graph_guid=str(uuid.uuid4()),
        from_node_guid=str(uuid.uuid4()),
        to_node_guid=str(uuid.uuid4()),
        cost=10,
        name="Test Edge",
    )


@pytest.fixture
def sample_route_detail(sample_edge):
    return RouteDetailModel(TotalCost=10.5, Edges=[sample_edge])


@pytest.fixture
def sample_timestamp():
    start_time = datetime(2024, 1, 1, 12, 0, tzinfo=timezone.utc)
    end_time = datetime(2024, 1, 1, 12, 1, tzinfo=timezone.utc)
    return Timestamp(
        Start=start_time,
        End=end_time,
        TotalMs=60000.0,
        Messages={"status": "completed"},
    )


def test_create_valid_route_response(sample_timestamp, sample_route_detail):
    """Test creating a valid RouteResultModel"""
    response = RouteResultModel(
        Timestamp=sample_timestamp, Routes=[sample_route_detail]
    )

    assert isinstance(response.Timestamp, Timestamp)
    assert isinstance(response.Routes, list)
    assert len(response.Routes) == 1
    assert isinstance(response.Routes[0], RouteDetailModel)


def test_timestamp_default_values():
    """Test that Timestamp model creates default values correctly"""
    timestamp = Timestamp(TotalMs=1000.0)

    assert isinstance(timestamp.Start, datetime)
    assert isinstance(timestamp.End, datetime)
    assert timestamp.TotalMs == 1000.0
    assert isinstance(timestamp.Messages, dict)
    assert len(timestamp.Messages) == 0


def test_route_response_with_multiple_routes(sample_timestamp, sample_route_detail):
    """Test RouteResultModel with multiple routes"""
    response = RouteResultModel(
        Timestamp=sample_timestamp, Routes=[sample_route_detail, sample_route_detail]
    )

    assert len(response.Routes) == 2
    assert all(isinstance(route, RouteDetailModel) for route in response.Routes)


def test_route_response_with_empty_routes(sample_timestamp):
    """Test RouteResultModel with empty routes list"""
    response = RouteResultModel(Timestamp=sample_timestamp, Routes=[])

    assert len(response.Routes) == 0


def test_timestamp_with_messages(sample_timestamp):
    """Test Timestamp model with various message types"""
    messages = {
        "info": "Processing complete",
        "warning": "High latency detected",
        "stats": {"processed": 100, "failed": 0},
    }

    timestamp = Timestamp(
        Start=sample_timestamp.Start,
        End=sample_timestamp.End,
        TotalMs=1000.0,
        Messages=messages,
    )

    assert timestamp.Messages == messages
    assert len(timestamp.Messages) == 3
    assert isinstance(timestamp.Messages["stats"], dict)


def test_route_detail_validation():
    """Test RouteDetailModel validation"""
    with pytest.raises(ValueError):
        RouteDetailModel(
            TotalCost="invalid",  # Should be float
            Edges=[],
        )


def test_timestamp_time_validation():
    """Test Timestamp time validation"""
    future_time = datetime(2025, 1, 1, tzinfo=timezone.utc)
    past_time = datetime(2023, 1, 1, tzinfo=timezone.utc)

    timestamp = Timestamp(Start=past_time, End=future_time, TotalMs=1000.0)

    assert timestamp.Start < timestamp.End
    assert isinstance(timestamp.Start, datetime)
    assert timestamp.Start.tzinfo == timezone.utc


def test_route_response_model_dict_conversion(sample_timestamp, sample_route_detail):
    """Test converting RouteResultModel to dict"""
    response = RouteResultModel(
        Timestamp=sample_timestamp, Routes=[sample_route_detail]
    )

    response_dict = response.model_dump()
    assert isinstance(response_dict, dict)
    assert "Timestamp" in response_dict
    assert "Routes" in response_dict
    assert isinstance(response_dict["Routes"], list)


def test_route_response_with_large_dataset(sample_timestamp, sample_edge):
    """Test RouteResultModel with a large number of routes"""
    large_routes = [
        RouteDetailModel(TotalCost=float(i), Edges=[sample_edge for _ in range(5)])
        for i in range(100)
    ]

    response = RouteResultModel(Timestamp=sample_timestamp, Routes=large_routes)

    assert len(response.Routes) == 100
    assert all(len(route.Edges) == 5 for route in response.Routes)
    assert response.Routes[-1].TotalCost == 99.0
