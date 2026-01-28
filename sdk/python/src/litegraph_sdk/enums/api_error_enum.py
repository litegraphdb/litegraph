from enum import Enum


class ApiError_Enum(str, Enum):
    """
    API error codes
    """
    authentication_failed = "AuthenticationFailed"
    authorization_failed = "AuthorizationFailed"
    bad_request = "BadRequest"
    conflict = "Conflict"
    deserialization_error = "DeserializationError"
    inactive = "Inactive"
    internal_error = "InternalError"
    invalid_range = "InvalidRange"
    in_use = "InUse"
    not_empty = "NotEmpty"
    not_found = "NotFound"
    too_large = "TooLarge"


ERROR_DESCRIPTIONS = {
    ApiError_Enum.authentication_failed: "Your authentication material was not accepted.",
    ApiError_Enum.authorization_failed: "Your authentication material was accepted, but you are not authorized to perform this request.",
    ApiError_Enum.bad_request: "We were unable to discern your request. Please check your URL, query, and request body.",
    ApiError_Enum.conflict: "Operation failed as it would create a conflict with an existing resource.",
    ApiError_Enum.deserialization_error: "Your request body was invalid and could not be deserialized.",
    ApiError_Enum.inactive: "Your account, credentials, or the requested resource are marked as inactive.",
    ApiError_Enum.internal_error: "An internal error has been encountered.",
    ApiError_Enum.invalid_range: "An invalid range has been supplied and cannot be fulfilled.",
    ApiError_Enum.in_use: "The requested resource is in use.",
    ApiError_Enum.not_empty: "The requested resource is not empty.",
    ApiError_Enum.not_found: "The requested resource was not found.",
    ApiError_Enum.too_large: "The size of your request exceeds the maximum allowed by this server.",
}
