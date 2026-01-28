import pytest
from litegraph_sdk.enums.api_error_enum import ERROR_DESCRIPTIONS, ApiError_Enum
from litegraph_sdk.exceptions import (
    AuthenticationError,
    AuthorizationError,
    BadRequestError,
    ConflictError,
    DeserializationError,
    InactiveError,
    InUseError,
    InvalidRangeError,
    NotEmptyError,
    ResourceNotFoundError,
    SdkException,
    ServerError,
    TimeoutError,
    get_exception_for_error_code,
)


def test_exception_inheritance():
    """Test that all exceptions inherit from SdkException."""
    exceptions = [
        AuthenticationError,
        AuthorizationError,
        ResourceNotFoundError,
        BadRequestError,
        TimeoutError,
        ServerError,
        ConflictError,
        InactiveError,
        InvalidRangeError,
        InUseError,
        NotEmptyError,
        DeserializationError,
    ]

    for exception_class in exceptions:
        assert issubclass(exception_class, SdkException)


def test_exception_messages():
    """Test that exceptions can be created with custom messages."""
    test_message = "Test error message"
    exceptions = [
        SdkException,
        AuthenticationError,
        AuthorizationError,
        ResourceNotFoundError,
        BadRequestError,
        TimeoutError,
        ServerError,
        ConflictError,
        InactiveError,
        InvalidRangeError,
        InUseError,
        NotEmptyError,
        DeserializationError,
    ]

    for exception_class in exceptions:
        exc = exception_class(test_message)
        assert str(exc) == test_message


@pytest.mark.parametrize(
    "error_code,expected_exception",
    [
        (ApiError_Enum.authentication_failed, AuthenticationError),
        (ApiError_Enum.authorization_failed, AuthorizationError),
        (ApiError_Enum.bad_request, BadRequestError),
        (ApiError_Enum.not_found, ResourceNotFoundError),
        (ApiError_Enum.internal_error, ServerError),
        (ApiError_Enum.too_large, BadRequestError),
        (ApiError_Enum.conflict, ConflictError),
        (ApiError_Enum.inactive, InactiveError),
        (ApiError_Enum.invalid_range, InvalidRangeError),
        (ApiError_Enum.in_use, InUseError),
        (ApiError_Enum.not_empty, NotEmptyError),
        (ApiError_Enum.deserialization_error, DeserializationError),
    ],
)
def test_get_exception_for_error_code(error_code, expected_exception):
    """Test mapping of API error codes to specific exceptions."""
    exception = get_exception_for_error_code(error_code)
    assert isinstance(exception, expected_exception)
    assert str(exception) == ERROR_DESCRIPTIONS[error_code]


def test_invalid_error_code():
    """Test handling of invalid error code."""
    invalid_error = "INVALID_ERROR"
    exception = get_exception_for_error_code(invalid_error)
    assert isinstance(exception, SdkException)
    assert "Unknown error: Invalid error code type" in str(exception)


def test_error_descriptions_match():
    """Test that all API error codes have corresponding descriptions."""
    for error_code in ApiError_Enum:
        assert error_code in ERROR_DESCRIPTIONS
        assert isinstance(ERROR_DESCRIPTIONS[error_code], str)
        assert len(ERROR_DESCRIPTIONS[error_code]) > 0


def test_exception_instantiation():
    """Test that all exceptions can be instantiated with and without messages."""
    exceptions = [
        SdkException,
        AuthenticationError,
        AuthorizationError,
        ResourceNotFoundError,
        BadRequestError,
        TimeoutError,
        ServerError,
        ConflictError,
        InactiveError,
        InvalidRangeError,
        InUseError,
        NotEmptyError,
        DeserializationError,
    ]

    for exception_class in exceptions:
        # Test without message
        exc1 = exception_class()
        assert isinstance(exc1, exception_class)

        # Test with message
        message = f"Test {exception_class.__name__}"
        exc2 = exception_class(message)
        assert isinstance(exc2, exception_class)
        assert str(exc2) == message


def test_exception_chaining():
    """Test exception chaining functionality."""
    original_error = ValueError("Original error")

    for exception_class in [
        SdkException,
        AuthenticationError,
        AuthorizationError,
        ResourceNotFoundError,
    ]:
        try:
            raise exception_class("Chained exception") from original_error
        except exception_class as e:
            assert isinstance(e.__cause__, ValueError)
            assert str(e.__cause__) == "Original error"


@pytest.mark.parametrize(
    "error_code,expected_message",
    [
        (
            ApiError_Enum.authentication_failed,
            "Your authentication material was not accepted.",
        ),
        (
            ApiError_Enum.authorization_failed,
            "Your authentication material was accepted, but you are not authorized to perform this request.",
        ),
        (ApiError_Enum.not_found, "The requested resource was not found."),
        (
            ApiError_Enum.bad_request,
            "We were unable to discern your request. Please check your URL, query, and request body.",
        ),
    ],
)
def test_error_messages(error_code, expected_message):
    """Test that error messages match expected descriptions."""
    exception = get_exception_for_error_code(error_code)
    assert str(exception) == expected_message


def test_custom_sdk_exception():
    """Test custom SdkException behavior."""
    # Test with string message
    exc1 = SdkException("Custom error")
    assert str(exc1) == "Custom error"

    # Test with formatted message
    exc2 = SdkException(f"Error code: {123}")
    assert str(exc2) == "Error code: 123"

    # Test with object message
    data = {"error": "test"}
    exc3 = SdkException(data)
    assert str(exc3) == str(data)


def test_exception_hierarchy():
    """Test the exception hierarchy and isinstance relationships."""
    auth_error = AuthenticationError("Auth failed")
    assert isinstance(auth_error, AuthenticationError)
    assert isinstance(auth_error, SdkException)
    assert isinstance(auth_error, Exception)

    server_error = ServerError("Server error")
    assert isinstance(server_error, ServerError)
    assert isinstance(server_error, SdkException)
    assert isinstance(server_error, Exception)
