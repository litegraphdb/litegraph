from .enums.api_error_enum import ERROR_DESCRIPTIONS, ApiError_Enum


class SdkException(Exception):
    """Base exception for the SDK."""

    pass


class AuthorizationError(SdkException):
    """Raised when authorization fails."""

    pass


class AuthenticationError(SdkException):
    """Raised when there is an authentication error."""

    pass


class ResourceNotFoundError(SdkException):
    """Raised when a requested resource is not found."""

    pass


class BadRequestError(SdkException):
    """Raised when the request is invalid."""

    pass


class TimeoutError(SdkException):
    """Raised when a request times out."""

    pass


class ServerError(SdkException):
    """Raised when there is a server-side error."""

    pass


class ConflictError(SdkException):
    """Raised when a conflict occurs, such as resource already existing."""

    pass


class InactiveError(SdkException):
    """Raised when the resource or account is inactive."""

    pass


class InvalidRangeError(SdkException):
    """Raised when an invalid range is supplied."""

    pass


class InUseError(SdkException):
    """Raised when the resource is in use."""

    pass


class NotEmptyError(SdkException):
    """Raised when the resource is not empty."""

    pass


class DeserializationError(SdkException):
    """Raised when there is an error in deserialization."""

    pass


def get_exception_for_error_code(error_code: ApiError_Enum) -> SdkException:
    """
    Maps API error codes to specific exception types.
    """
    # Handle invalid error codes
    if not isinstance(error_code, ApiError_Enum):
        return SdkException(f"Unknown error: Invalid error code type - {error_code}")

    error_mapping = {
        ApiError_Enum.authentication_failed: AuthenticationError,
        ApiError_Enum.authorization_failed: AuthorizationError,
        ApiError_Enum.bad_request: BadRequestError,
        ApiError_Enum.not_found: ResourceNotFoundError,
        ApiError_Enum.internal_error: ServerError,
        ApiError_Enum.too_large: BadRequestError,
        ApiError_Enum.conflict: ConflictError,
        ApiError_Enum.inactive: InactiveError,
        ApiError_Enum.invalid_range: InvalidRangeError,
        ApiError_Enum.in_use: InUseError,
        ApiError_Enum.not_empty: NotEmptyError,
        ApiError_Enum.deserialization_error: DeserializationError,
    }

    # Get the exception class from the mapping, default to SdkException if not found
    exception_class = error_mapping.get(error_code, SdkException)

    # Get the error description from the ERROR_DESCRIPTIONS mapping
    error_description = ERROR_DESCRIPTIONS[error_code]

    return exception_class(error_description)


TENANT_REQUIRED_ERROR = "Tenant GUID is required for this resource."
GRAPH_REQUIRED_ERROR = "Graph GUID is required for this resource."
