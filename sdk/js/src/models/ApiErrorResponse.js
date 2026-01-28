import { ApiErrorEnum } from '../enums/ApiErrorEnum';

/**
 * Class representing an API error response.
 */
export default class ApiErrorResponse {
  /**
   * Create an API error response.
   *
   * @param {ApiErrorEnum} error - The error code.
   * @param {Object} [context=null] - Additional contextual information.
   * @param {string} [description=null] - Description of the error.
   */
  constructor(error = ApiErrorEnum.AuthenticationFailed, context = null, description = null) {
    this.error = error;
    this.context = context;
    this.description = description;
  }

  /**
   * Get the human-readable message corresponding to the error.
   *
   * @returns {string} - The message for the current error.
   */
  get message() {
    switch (this.error) {
      case ApiErrorEnum.AuthenticationFailed:
        return 'Your authentication material was not accepted.';
      case ApiErrorEnum.AuthorizationFailed:
        return 'Your authentication material was accepted, but you are not authorized to perform this request.';
      case ApiErrorEnum.BadRequest:
        return 'We were unable to discern your request. Please check your URL, query, and request body.';
      case ApiErrorEnum.Conflict:
        return 'Operation failed as it would create a conflict with an existing resource.';
      case ApiErrorEnum.DeserializationError:
        return 'Your request body was invalid and could not be deserialized.';
      case ApiErrorEnum.Inactive:
        return 'Your account, credentials, or the requested resource are marked as inactive.';
      case ApiErrorEnum.InternalError:
        return 'An internal error has been encountered.';
      case ApiErrorEnum.InvalidRange:
        return 'An invalid range has been supplied and cannot be fulfilled.';
      case ApiErrorEnum.InUse:
        return 'The requested resource is in use.';
      case ApiErrorEnum.NotEmpty:
        return 'The requested resource is not empty.';
      case ApiErrorEnum.NotFound:
        return 'The requested resource was not found.';
      case ApiErrorEnum.TooLarge:
        return 'The size of your request exceeds the maximum allowed by this server.';
      // Add more cases based on ApiErrorEnum...
      default:
        return `An unknown error code '${this.error}' was encountered.`;
    }
  }

  /**
   * Get the HTTP status code corresponding to the error.
   *
   * @returns {number} - The HTTP status code for the current error.
   */
  get statusCode() {
    switch (this.error) {
      case ApiErrorEnum.AuthenticationFailed:
        return 401;
      case ApiErrorEnum.AuthorizationFailed:
        return 403;
      case ApiErrorEnum.BadRequest:
        return 400;
      case ApiErrorEnum.Conflict:
        return 409;
      case ApiErrorEnum.DeserializationError:
        return 400;
      case ApiErrorEnum.Inactive:
        return 401;
      case ApiErrorEnum.InternalError:
        return 500;
      case ApiErrorEnum.InvalidRange:
        return 400;
      case ApiErrorEnum.InUse:
        return 409;
      case ApiErrorEnum.NotEmpty:
        return 400;
      case ApiErrorEnum.NotFound:
        return 404;
      case ApiErrorEnum.TooLarge:
        return 413;
      // Add more cases based on ApiErrorEnum...
      default:
        return 500;
    }
  }
}
