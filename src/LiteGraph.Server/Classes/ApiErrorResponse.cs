namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// API error response.
    /// </summary>
    public class ApiErrorResponse
    {
        #region Public-Members

        /// <summary>
        /// Error.
        /// </summary>
        public ApiErrorEnum Error { get; set; } = ApiErrorEnum.AuthenticationFailed;

        /// <summary>
        /// Human-readable message.
        /// </summary>
        public string Message
        {
            get
            {
                switch (Error)
                {
                    case ApiErrorEnum.AuthenticationFailed:
                        return "Your authentication material was not accepted.";
                    case ApiErrorEnum.AuthorizationFailed:
                        return "Your authentication material was accepted, but you are not authorized to perform this request.";
                    case ApiErrorEnum.BadRequest:
                        return "We were unable to discern your request.  Please check your URL, query, and request body.";
                    case ApiErrorEnum.Conflict:
                        return "Operation failed as it would create a conflict with an existing resource.";
                    case ApiErrorEnum.DeserializationError:
                        return "Your request body was invalid and could not be deserialized.";
                    case ApiErrorEnum.Inactive:
                        return "Your account, credentials, or the requested resource are marked as inactive.";
                    case ApiErrorEnum.InternalError:
                        return "An internal error has been encountered.";
                    case ApiErrorEnum.InvalidRange:
                        return "An invalid range has been supplied and cannot be fulfilled.";
                    case ApiErrorEnum.InUse:
                        return "The requested resource is in use.";
                    case ApiErrorEnum.NotEmpty:
                        return "The requested resource is not empty.";
                    case ApiErrorEnum.NotFound:
                        return "The requested resource was not found.";
                    case ApiErrorEnum.RequestTimeout:
                        return "The request timed out before it could be completed.";
                    case ApiErrorEnum.TooLarge:
                        return "The size of your request exceeds the maximum allowed by this server.";
                    case ApiErrorEnum.Unavailable:
                        return "A service required by this request is currently unavailable.";

                    default:
                        return "An unknown error code '" + Error.ToString() + "' was encountered.";
                }
            }
        }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode
        {
            get
            {
                switch (Error)
                {
                    case ApiErrorEnum.AuthenticationFailed:
                        return 401;
                    case ApiErrorEnum.AuthorizationFailed:
                        return 401;
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
                    case ApiErrorEnum.RequestTimeout:
                        return 408;
                    case ApiErrorEnum.TooLarge:
                        return 413;
                    case ApiErrorEnum.Unavailable:
                        return 503;

                    default:
                        return 500;
                }
            }
        }

        /// <summary>
        /// Additional contextual information.
        /// </summary>
        public object Context { get; set; } = null;

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ApiErrorResponse()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="error">Error code.</param>
        /// <param name="context">Context.</param>
        /// <param name="description">Description.</param>
        ///
        public ApiErrorResponse(ApiErrorEnum error, object context = null, string description = null)
        {
            Error = error;
            Context = context;
            Description = description;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
