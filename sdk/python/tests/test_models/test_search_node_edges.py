import uuid
from datetime import datetime, timezone

import pytest
from litegraph_sdk.enums.enumeration_order_enum import EnumerationOrder_Enum
from litegraph_sdk.enums.operator_enum import Opertator_Enum
from litegraph_sdk.models.edge import EdgeModel
from litegraph_sdk.models.expression import ExprModel
from litegraph_sdk.models.node import NodeModel
from litegraph_sdk.models.search_node_edge import (
    SearchRequest,
    SearchResult,
    SearchResultEdge,
)
from pydantic import ValidationError


@pytest.fixture
def sample_node():
    """Create a sample NodeModel for testing"""
    return NodeModel(
        GUID=str(uuid.uuid4()),
        GraphGUID=str(uuid.uuid4()),
        Name="Test Node",
        CreatedUtc=datetime.now(timezone.utc),
        Data={"key": "value"},
    )


@pytest.fixture
def sample_edge():
    """Create a sample EdgeModel for testing"""
    return EdgeModel(
        GUID=str(uuid.uuid4()),
        GraphGUID=str(uuid.uuid4()),
        From=str(uuid.uuid4()),
        To=str(uuid.uuid4()),
        Name="Test Edge",
        Cost=10,
        CreatedUtc=datetime.now(timezone.utc),
        Data={"key": "value"},
    )


@pytest.fixture
def sample_expression():
    """Create a sample ExprModel for testing"""
    return ExprModel(Left="name", Operator=Opertator_Enum.Equal, Right="Test Node")


class TestSearchRequest:
    def test_create_search_request_minimal(self):
        """Test creating a SearchRequest with minimal required fields"""
        graph_guid = str(uuid.uuid4())
        request = SearchRequest(graph_guid=graph_guid)
        assert request.graph_guid == graph_guid
        assert request.ordering == EnumerationOrder_Enum.CreatedDescending
        assert request.expr is None

    def test_create_search_request_complete(self, sample_expression):
        """Test creating a SearchRequest with all fields"""
        graph_guid = str(uuid.uuid4())
        request = SearchRequest(
            graph_guid=graph_guid,
            ordering=EnumerationOrder_Enum.NameAscending,
            expr=sample_expression,
        )
        assert request.graph_guid == graph_guid
        assert request.ordering == EnumerationOrder_Enum.NameAscending
        assert request.expr == sample_expression

    @pytest.mark.parametrize("ordering", list(EnumerationOrder_Enum))
    def test_valid_ordering_values(self, ordering):
        """Test SearchRequest with all valid ordering values"""
        request = SearchRequest(graph_guid=str(uuid.uuid4()), ordering=ordering)
        assert request.ordering == ordering

    def test_invalid_ordering(self):
        """Test SearchRequest with invalid ordering value"""
        with pytest.raises((ValueError, ValidationError)):
            SearchRequest(graph_guid=str(uuid.uuid4()), ordering="invalid_ordering")


class TestSearchResult:
    def test_empty_search_result(self):
        """Test creating an empty SearchResult"""
        result = SearchResult()
        assert result.nodes is None

    def test_search_result_with_nodes(self, sample_node):
        """Test SearchResult with a list of nodes"""
        result = SearchResult(Nodes=[sample_node])
        assert len(result.nodes) == 1
        assert isinstance(result.nodes[0], NodeModel)
        assert result.nodes[0].name == "Test Node"

    def test_search_result_with_multiple_nodes(self, sample_node):
        """Test SearchResult with multiple nodes"""
        nodes = [
            sample_node,
            NodeModel(
                GUID=str(uuid.uuid4()),
                GraphGUID=str(uuid.uuid4()),
                Name="Another Node",
                CreatedUtc=datetime.now(timezone.utc),
            ),
        ]
        result = SearchResult(Nodes=nodes)
        assert len(result.nodes) == 2
        assert all(isinstance(node, NodeModel) for node in result.nodes)

    def test_search_result_with_empty_list(self):
        """Test SearchResult with an empty list"""
        result = SearchResult(Nodes=[])
        assert isinstance(result.nodes, list)
        assert len(result.nodes) == 0


class TestSearchResultEdge:
    def test_empty_search_result_edge(self):
        """Test creating an empty SearchResult_Edge"""
        result = SearchResultEdge()
        assert result.edges is None

    def test_search_result_edge_with_edges(self, sample_edge):
        """Test SearchResult_Edge with a list of edges"""
        result = SearchResultEdge(Edges=[sample_edge])
        assert len(result.edges) == 1
        assert isinstance(result.edges[0], EdgeModel)
        assert result.edges[0].name == "Test Edge"

    def test_search_result_edge_with_multiple_edges(self, sample_edge):
        """Test SearchResult_Edge with multiple edges"""
        edges = [
            sample_edge,
            EdgeModel(
                GUID=str(uuid.uuid4()),
                GraphGUID=str(uuid.uuid4()),
                From=str(uuid.uuid4()),
                To=str(uuid.uuid4()),
                Name="Another Edge",
                Cost=20,
                CreatedUtc=datetime.now(timezone.utc),
            ),
        ]
        result = SearchResultEdge(Edges=edges)
        assert len(result.edges) == 2
        assert all(isinstance(edge, EdgeModel) for edge in result.edges)

    def test_search_result_edge_with_empty_list(self):
        """Test SearchResult_Edge with an empty list"""
        result = SearchResultEdge(edges=[])
        assert isinstance(result.edges, list)
        assert len(result.edges) == 0

    def test_edge_cost_validation(self):
        """Test edge cost validation"""
        with pytest.raises((ValueError, ValidationError)):
            EdgeModel(
                GUID=str(uuid.uuid4()),
                GraphGUID=str(uuid.uuid4()),
                From=str(uuid.uuid4()),
                To=str(uuid.uuid4()),
                Name="Test Edge",
                Cost="invalid",  # Invalid cost type
                CreatedUtc=datetime.now(timezone.utc),
            )

    @pytest.mark.parametrize("num_edges", [0, 1, 5])
    def test_edge_list_sizes(self, num_edges):
        """Test different sizes of edge lists"""
        edges = [
            EdgeModel(
                GUID=str(uuid.uuid4()),
                GraphGUID=str(uuid.uuid4()),
                From=str(uuid.uuid4()),
                To=str(uuid.uuid4()),
                Name=f"Edge {i}",
                Cost=i,
                CreatedUtc=datetime.now(timezone.utc),
            )
            for i in range(num_edges)
        ]
        result = SearchResultEdge(edges=edges)
        assert len(result.edges) == num_edges


def test_integration_search_request_with_results(
    sample_node, sample_edge, sample_expression
):
    """Test integration between SearchRequest and search results"""
    # Create a search request
    request = SearchRequest(
        graph_guid=str(uuid.uuid4()),
        ordering=EnumerationOrder_Enum.NameAscending,
        expr=sample_expression,
        labels=["label1", "label2"],
        tags={"tag1": "value1", "tag2": "value2"},
    )

    # Create search results
    node_result = SearchResult(Nodes=[sample_node])
    edge_result = SearchResultEdge(Edges=[sample_edge])

    # Verify the integration
    assert isinstance(request.expr, ExprModel)
    assert isinstance(node_result.nodes[0], NodeModel)
    assert isinstance(edge_result.edges[0], EdgeModel)
    assert isinstance(node_result.nodes[0].graph_guid, str)
    assert isinstance(edge_result.edges[0].cost, int)
    assert edge_result.edges[0].cost >= 0


def test_large_dataset():
    """Test handling of large datasets"""
    # Create large lists of nodes and edges
    nodes = [
        NodeModel(
            GUID=str(uuid.uuid4()),
            GraphGUID=str(uuid.uuid4()),
            Name=f"Node {i}",
            CreatedUtc=datetime.now(timezone.utc),
        )
        for i in range(100)
    ]

    edges = [
        EdgeModel(
            GUID=str(uuid.uuid4()),
            GraphGUID=str(uuid.uuid4()),
            From=str(uuid.uuid4()),
            To=str(uuid.uuid4()),
            Name=f"Edge {i}",
            Cost=i,
            CreatedUtc=datetime.now(timezone.utc),
        )
        for i in range(100)
    ]

    node_result = SearchResult(Nodes=nodes)
    edge_result = SearchResultEdge(Edges=edges)

    assert len(node_result.nodes) == 100
    assert len(edge_result.edges) == 100
