import pytest
from litegraph_sdk.enums.operator_enum import Opertator_Enum
from litegraph_sdk.models.expression import ExprModel
from pydantic import ValidationError


@pytest.fixture
def valid_expr_data():
    """Fixture providing valid expression data."""
    return {"Left": "field1", "Operator": Opertator_Enum.Equal, "Right": "value1"}


@pytest.fixture
def all_operators():
    """Fixture providing all valid operators."""
    return [Opertator_Enum.Equal, Opertator_Enum.GreaterThan, Opertator_Enum.LessThan]


def test_valid_expression_creation(valid_expr_data):
    """Test creating an expression with valid data."""
    expr = ExprModel(**valid_expr_data)
    assert expr.Left == valid_expr_data["Left"]
    assert expr.Operator == valid_expr_data["Operator"]
    assert expr.Right == valid_expr_data["Right"]


@pytest.mark.parametrize(
    "operator",
    [Opertator_Enum.Equal, Opertator_Enum.GreaterThan, Opertator_Enum.LessThan],
)
def test_all_operators(operator):
    """Test expression creation with all possible operators."""
    expr = ExprModel(Left="field", Operator=operator, Right="value")
    assert expr.Operator == operator


def test_invalid_operator():
    """Test that invalid operators are rejected."""
    with pytest.raises(ValidationError):
        ExprModel(Left="field", Operator="InvalidOperator", Right="value")


def test_missing_fields():
    """Test that missing required fields raise validation error."""
    invalid_cases = [
        {"Operator": Opertator_Enum.Equal, "Right": "value"},  # Missing Left
        {"Left": "field", "Right": "value"},  # Missing Operator
        {"Left": "field", "Operator": Opertator_Enum.Equal},  # Missing Right
        {},  # Missing all
    ]

    for invalid_data in invalid_cases:
        with pytest.raises(ValidationError):
            ExprModel(**invalid_data)


def test_string_fields_validation():
    """Test validation of string fields."""
    invalid_types = [
        {"Left": 123, "Operator": Opertator_Enum.Equal, "Right": "value"},
        {"Left": "field", "Operator": Opertator_Enum.Equal, "Right": 123},
        {"Left": True, "Operator": Opertator_Enum.Equal, "Right": "value"},
        {"Left": "field", "Operator": Opertator_Enum.Equal, "Right": True},
    ]

    for invalid_data in invalid_types:
        with pytest.raises(ValidationError):
            ExprModel(**invalid_data)


def test_empty_and_whitespace_strings():
    """Test that empty and whitespace strings are allowed."""
    # Test empty strings
    expr1 = ExprModel(Left="", Operator=Opertator_Enum.Equal, Right="value")
    assert expr1.Left == ""

    expr2 = ExprModel(Left="field", Operator=Opertator_Enum.Equal, Right="")
    assert expr2.Right == ""

    # Test whitespace strings
    expr3 = ExprModel(Left="   ", Operator=Opertator_Enum.Equal, Right="value")
    assert expr3.Left == "   "

    expr4 = ExprModel(Left="field", Operator=Opertator_Enum.Equal, Right="   ")
    assert expr4.Right == "   "


@pytest.mark.parametrize(
    "left,operator,right",
    [
        ("field1", Opertator_Enum.Equal, "value1"),
        ("long_field_name", Opertator_Enum.GreaterThan, "long_value_name"),
        ("f1", Opertator_Enum.LessThan, "v1"),
        ("field_with_numbers_123", Opertator_Enum.Equal, "value_with_numbers_456"),
        (
            "field_with_special_chars_!@#",
            Opertator_Enum.Equal,
            "value_with_special_chars_!@#",
        ),
        ("", Opertator_Enum.Equal, "value"),  # Empty string
        ("field", Opertator_Enum.Equal, ""),  # Empty string
        ("   ", Opertator_Enum.Equal, "value"),  # Whitespace
        ("field", Opertator_Enum.Equal, "   "),  # Whitespace
    ],
)
def test_valid_field_values(left, operator, right):
    """Test various valid combinations of field values."""
    expr = ExprModel(Left=left, Operator=operator, Right=right)
    assert expr.Left == left
    assert expr.Operator == operator
    assert expr.Right == right


def test_model_serialization():
    """Test model serialization and deserialization."""
    original_data = {
        "Left": "field1",
        "Operator": Opertator_Enum.Equal,
        "Right": "value1",
    }

    # Create model
    expr = ExprModel(**original_data)

    # Serialize to dict
    serialized = expr.model_dump()

    # Create new model from serialized data
    new_expr = ExprModel(**serialized)

    # Verify all fields match
    assert new_expr.Left == expr.Left
    assert new_expr.Operator == expr.Operator
    assert new_expr.Right == expr.Right


@pytest.mark.parametrize(
    "operator,left,right",
    [
        (Opertator_Enum.Equal, "age", "25"),
        (Opertator_Enum.GreaterThan, "price", "100"),
        (Opertator_Enum.LessThan, "quantity", "10"),
        (Opertator_Enum.Equal, "", "25"),  # Empty string test
        (Opertator_Enum.GreaterThan, "price", ""),  # Empty string test
    ],
)
def test_typical_use_cases(operator, left, right):
    """Test typical use cases for expressions."""
    expr = ExprModel(Left=left, Operator=operator, Right=right)
    assert expr.Left == left
    assert expr.Operator == operator
    assert expr.Right == right


def test_case_sensitivity():
    """Test case sensitivity handling."""
    expr1 = ExprModel(Left="Field", Operator=Opertator_Enum.Equal, Right="Value")

    expr2 = ExprModel(Left="field", Operator=Opertator_Enum.Equal, Right="value")

    assert expr1.Left != expr2.Left
    assert expr1.Right != expr2.Right


def test_none_values():
    """Test that None values are rejected."""
    invalid_cases = [
        {"Left": None, "Operator": Opertator_Enum.Equal, "Right": "value"},
        {"Left": "field", "Operator": None, "Right": "value"},
        {"Left": "field", "Operator": Opertator_Enum.Equal, "Right": None},
    ]

    for invalid_data in invalid_cases:
        with pytest.raises(ValidationError):
            ExprModel(**invalid_data)
