import uuid
from datetime import datetime, timezone

import pytest
from litegraph_sdk.models.graphs import GraphModel
from litegraph_sdk.models.search_graphs import SearchResultGraph
from pydantic import ValidationError


@pytest.fixture
def sample_graph():
    """Create a sample GraphModel for testing"""
    return GraphModel(
        GUID=str(uuid.uuid4()),
        Name="Test Graph",
        CreatedUtc=datetime.now(timezone.utc),
        Data={"key": "value"},
    )


class TestSearchResultGraph:
    def test_empty_search_result(self):
        """Test creating an empty SearchResult_Graph"""
        result = SearchResultGraph()
        assert result.graphs is None
        assert result.model_dump(exclude_none=True) == {}

    def test_search_result_with_graphs(self, sample_graph):
        """Test SearchResult_Graph with a list of graphs"""
        result = SearchResultGraph(Graphs=[sample_graph])
        assert len(result.graphs) == 1
        assert isinstance(result.graphs[0], GraphModel)
        assert result.graphs[0].name == "Test Graph"

    def test_graph_model_validation(self):
        """Test validation of invalid graph data"""
        with pytest.raises((ValueError, ValidationError)):
            # Create an invalid graph directly through GraphModel
            invalid_graph = GraphModel(
                GUID="invalid-guid",  # Invalid GUID format
                Name=123,  # Invalid type for name (should be string)
                CreatedUtc="not-a-date",  # Invalid date format
            )
            SearchResultGraph(Graphs=[invalid_graph])

    def test_graph_model_validation_with_invalid_types(self):
        """Test validation with invalid data types"""
        with pytest.raises((ValueError, ValidationError)):
            # Try to create a SearchResult_Graph with invalid data types
            SearchResultGraph(
                Graphs=[
                    {
                        "GUID": 12345,  # Should be string
                        "Name": ["invalid", "name"],  # Should be string
                        "CreatedUtc": "invalid-date",  # Should be datetime
                    }
                ]
            )

    def test_graph_model_validation_with_invalid_list_type(self):
        """Test validation with invalid list type"""
        with pytest.raises((ValueError, ValidationError)):
            # Try to pass a string instead of a list of graphs
            SearchResultGraph(Graphs="not a list")

    def test_search_result_with_none_values(self):
        """Test handling of None values in required fields"""
        with pytest.raises((ValueError, ValidationError)):
            invalid_graph = GraphModel(
                GUID=None,  # Required field cannot be None
                Name="Test Graph",
                CreatedUtc=datetime.now(timezone.utc),
            )
            SearchResultGraph(Graphs=[invalid_graph])

    def test_valid_graph_creation(self):
        """Test creating a valid graph"""
        valid_graph = GraphModel(
            GUID=str(uuid.uuid4()),
            Name="Test Graph",
            CreatedUtc=datetime.now(timezone.utc),
        )
        result = SearchResultGraph(Graphs=[valid_graph])
        assert len(result.graphs) == 1
        assert isinstance(result.graphs[0], GraphModel)

    def test_search_result_serialization(self, sample_graph):
        """Test serialization of SearchResult_Graph"""
        result = SearchResultGraph(Graphs=[sample_graph])
        serialized = result.model_dump()
        assert isinstance(serialized, dict)
        assert isinstance(serialized["graphs"], list)
        assert serialized["graphs"][0]["name"] == "Test Graph"

    def test_search_result_with_empty_list(self):
        """Test SearchResult_Graph with an empty list"""
        result = SearchResultGraph(Graphs=[])
        assert isinstance(result.graphs, list)
        assert len(result.graphs) == 0

    def test_large_result_set(self):
        """Test SearchResult_Graph with a large number of graphs"""
        large_graph_list = [
            GraphModel(
                GUID=str(uuid.uuid4()),
                Name=f"Graph {i}",
                CreatedUtc=datetime.now(timezone.utc),
            )
            for i in range(100)
        ]
        result = SearchResultGraph(Graphs=large_graph_list)
        assert len(result.graphs) == 100
        assert result.graphs[0].name == "Graph 0"
        assert result.graphs[-1].name == "Graph 99"
