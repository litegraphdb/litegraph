import pytest
from litegraph_sdk.enums.api_error_enum import ERROR_DESCRIPTIONS, ApiError_Enum
from litegraph_sdk.models.api_error import ApiErrorResponseModel
from pydantic import ValidationError


@pytest.fixture
def valid_error_data():
    """Fixture providing valid error response data."""
    return {
        "Error": ApiError_Enum.not_found,
        "Context": "Resource ID: 123",
        "Description": "The requested resource was not found.",
    }


@pytest.fixture
def minimal_error_data():
    """Fixture providing minimal valid error response data."""
    return {"Error": ApiError_Enum.bad_request}


def test_valid_error_creation(valid_error_data):
    """Test creating an error response with all valid fields."""
    error = ApiErrorResponseModel(**valid_error_data)
    assert error.error == valid_error_data["Error"]
    assert error.context == valid_error_data["Context"]
    assert error.description == valid_error_data["Description"]


def test_minimal_error_creation(minimal_error_data):
    """Test creating an error response with only required fields."""
    error = ApiErrorResponseModel(**minimal_error_data)
    assert error.error == minimal_error_data["Error"]
    assert error.context is None
    assert error.description is None


def test_error_aliases():
    """Test that field aliases work correctly."""
    data = {
        "error": ApiError_Enum.bad_request,
        "context": "Test context",
        "description": "Test description",
    }
    error = ApiErrorResponseModel(**data)

    # Test model dump with aliases
    dumped = error.model_dump(by_alias=True)
    assert "Error" in dumped
    assert "Context" in dumped
    assert "Description" in dumped

    # Test values
    assert dumped["Error"] == data["error"]
    assert dumped["Context"] == data["context"]
    assert dumped["Description"] == data["description"]


def test_invalid_error_enum():
    """Test that invalid error enum values are rejected."""
    with pytest.raises(ValidationError):
        ApiErrorResponseModel(Error="INVALID_ERROR")


@pytest.mark.parametrize("error_enum", list(ApiError_Enum))
def test_all_error_enums(error_enum):
    """Test model creation with all possible error enum values."""
    error = ApiErrorResponseModel(Error=error_enum)
    assert error.error == error_enum
    assert error.error in ApiError_Enum


def test_optional_fields():
    """Test handling of optional fields."""
    # Test different combinations of optional fields
    test_cases = [
        {"Error": ApiError_Enum.not_found, "Context": "Test context"},
        {"Error": ApiError_Enum.not_found, "Description": "Test description"},
        {"Error": ApiError_Enum.not_found},
        {"Error": ApiError_Enum.not_found, "Context": None, "Description": None},
    ]

    for test_data in test_cases:
        error = ApiErrorResponseModel(**test_data)
        assert error.error == test_data["Error"]
        assert error.context == test_data.get("Context")
        assert error.description == test_data.get("Description")


def test_error_descriptions_mapping():
    """Test that error enums map to correct descriptions in ERROR_DESCRIPTIONS."""
    for error_enum in ApiError_Enum:
        assert error_enum in ERROR_DESCRIPTIONS
        assert isinstance(ERROR_DESCRIPTIONS[error_enum], str)
        assert len(ERROR_DESCRIPTIONS[error_enum]) > 0


def test_model_config():
    """Test model configuration options."""
    # Test that populate_by_name works with both alias and field names
    alias_data = {"Error": ApiError_Enum.bad_request, "Context": "Test context"}
    field_data = {"error": ApiError_Enum.bad_request, "context": "Test context"}

    error1 = ApiErrorResponseModel(**alias_data)
    error2 = ApiErrorResponseModel(**field_data)

    assert error1.error == error2.error
    assert error1.context == error2.context


def test_string_type_validation():
    """Test validation of string fields."""
    invalid_types = [
        {"Error": ApiError_Enum.not_found, "Context": 123},
        {"Error": ApiError_Enum.not_found, "Description": 123},
        {"Error": ApiError_Enum.not_found, "Context": True},
        {"Error": ApiError_Enum.not_found, "Description": []},
    ]

    for invalid_data in invalid_types:
        with pytest.raises(ValidationError):
            ApiErrorResponseModel(**invalid_data)


def test_model_serialization():
    """Test model serialization and deserialization."""
    original_data = {
        "Error": ApiError_Enum.not_found,
        "Context": "Test context",
        "Description": "Test description",
    }

    # Create model
    error = ApiErrorResponseModel(**original_data)

    # Serialize to dict
    serialized = error.model_dump(by_alias=True)

    # Create new model from serialized data
    new_error = ApiErrorResponseModel(**serialized)

    # Verify all fields match
    assert new_error.error == error.error
    assert new_error.context == error.context
    assert new_error.description == error.description


def test_empty_string_handling():
    """Test handling of empty strings in optional fields."""
    test_data = {"Error": ApiError_Enum.not_found, "Context": "", "Description": ""}

    error = ApiErrorResponseModel(**test_data)
    assert error.context == ""
    assert error.description == ""


@pytest.mark.parametrize(
    "error_enum,expected_description",
    [
        (ApiError_Enum.not_found, "The requested resource was not found."),
        (
            ApiError_Enum.bad_request,
            "We were unable to discern your request. Please check your URL, query, and request body.",
        ),
        (
            ApiError_Enum.authorization_failed,
            "Your authentication material was accepted, but you are not authorized to perform this request.",
        ),
    ],
)
def test_error_descriptions_content(error_enum, expected_description):
    """Test specific error descriptions content."""
    assert ERROR_DESCRIPTIONS[error_enum] == expected_description
