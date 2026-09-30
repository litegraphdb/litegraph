/**
 * SDK Base class for making API calls with logging and timeout functionality.
 * @module SdkBase
 */
export default class SdkBase {
    /**
     * Creates an instance of SdkBase.
     * @param {string} endpoint - The API endpoint base URL.
     * @param {string} [tenantGuid] - The tenant GUID.
     * @param {string} [accessKey] - The access key.
     * @throws {Error} Throws an error if the endpoint is null or empty.
     */
    constructor(endpoint: string, tenantGuid?: string, accessKey?: string);
    /**
     * Setter for the tenant GUID.
     * @param {string} value - The tenant GUID.
     * @throws {Error} Throws an error if the tenant GUID is null or empty.
     */
    set tenantGuid(value: string);
    /**
     * Getter for the tenant GUID.
     * @return {string} The tenant GUID.
     */
    get tenantGuid(): string;
    /**
     * Setter for the access key.
     * @param {string} value - The access key.
     * @throws {Error} Throws an error if the access key is null or empty.
     */
    set accessKey(value: string);
    /**
     * Getter for the access key.
     * @return {string} The access key.
     */
    get accessKey(): string;
    _header: string;
    _endpoint: string;
    _timeoutMs: number;
    _maxRetries: number;
    _retryBaseDelayMs: number;
    _retryPost: boolean;
    _lastNodeId: string;
    logger: (severity: any, message: string) => void;
    /**
     * Setter for the maximum number of retries.
     * @param {number} value - Retries, 0 (no retries) to 10.
     * @throws {Error} Throws an error if the value is outside 0 to 10.
     */
    set maxRetries(value: number);
    /**
     * Maximum number of retries after the first attempt for requests that fail with a connection error or a 502, 503,
     * or 504 response. GET, HEAD, PUT, and DELETE are retried; POST only when retryPost is true. Default 2, range 0 to 10.
     * @return {number} The maximum number of retries.
     */
    get maxRetries(): number;
    /**
     * Setter for the base retry delay.
     * @param {number} value - Delay in milliseconds, 0 to 5000.
     * @throws {Error} Throws an error if the value is outside 0 to 5000.
     */
    set retryBaseDelayMs(value: number);
    /**
     * Base delay before the first retry, in milliseconds. Each further retry doubles it, capped at 5000 ms, less a
     * random jitter of up to half the delay. Default 200, range 0 to 5000.
     * @return {number} The base retry delay in milliseconds.
     */
    get retryBaseDelayMs(): number;
    /**
     * Setter for POST retries.
     * @param {boolean} value - True to retry POST requests.
     */
    set retryPost(value: boolean);
    /**
     * Whether POST requests are retried too. POST is not idempotent, so a retried POST can apply twice if the first
     * attempt reached the server. Default false. Streaming responses are never retried once any body has been read.
     * @return {boolean} True if POST requests are retried.
     */
    get retryPost(): boolean;
    /**
     * Node that answered the most recent request, from the x-litegraph-node response header, or null until a response
     * carrying the header is received. Behind a load balancer this identifies which cluster node served the request.
     * @return {string|null} The node identifier.
     */
    get lastNodeId(): string | null;
    _tenantGuid: string;
    defaultHeaders: any;
    _accessKey: string;
    /**
     * Setter for the access token.
     * @param {string} value - The access token.
     * @throws {Error} Throws an error if the access token is null or empty.
     */
    set accessToken(value: string);
    /**
     * Getter for the access token.
     * @return {string} The access token.
     */
    get accessToken(): string;
    _accessToken: string;
    /**
     * Setter for the request header prefix.
     * @param {string} value - The header prefix.
     */
    set header(value: string);
    /**
     * Getter for the request header prefix.
     * @return {string} The header prefix.
     */
    get header(): string;
    /**
     * Setter for the API endpoint.
     * @param {string} value - The endpoint URL.
     * @throws {Error} Throws an error if the endpoint is null or empty.
     */
    set endpoint(value: string);
    /**
     * Getter for the API endpoint.
     * @return {string} The endpoint URL.
     */
    get endpoint(): string;
    /**
     * Setter for the timeout in milliseconds.
     * @param {number} value - Timeout value in milliseconds.
     * @throws {Error} Throws an error if the timeout is less than 1.
     */
    set timeoutMs(value: number);
    /**
     * Getter for the timeout in milliseconds.
     * @return {number} The timeout in milliseconds.
     */
    get timeoutMs(): number;
    /**
     * Logs a message with a severity level.
     * @param {string} sev - The severity level (e.g., SeverityEnum.Debug, 'warn').
     * @param {string} msg - The message to log.
     */
    log(sev: string, msg: string): void;
    /**
     * Validates API connectivity using a HEAD request.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<boolean>} Resolves to true if the connection is successful.
     * @throws {Error} Rejects with the error in case of failure.
     */
    validateConnectivity(cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Sends a PUT request to create an object at a given URL.
     * @param {string} url - The URL where the object is created.
     * @param {Object} obj - The object to be created.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object>} Resolves with the created object.
     * @throws {Error} Rejects if the URL or object is invalid or if the request fails.
     */
    putCreate(url: string, obj: any, model: Class, cancellationToken?: AbortController): Promise<any>;
    /**
     * Checks if an object exists at a given URL using a HEAD request.
     * @param {string} url - The URL to check.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<boolean>} Resolves to true if the object exists.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    head(url: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Retrieves an object from a given URL using a GET request.
     * @param {string} url - The URL of the object.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @param {Object} [headers] - Additional headers.
     * @return {Promise<Object>} Resolves with the retrieved object.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    get(url: string, model: Class, cancellationToken?: AbortController, headers?: any): Promise<any>;
    /**
     * Retrieves raw data from a given URL using a GET request.
     * @param {string} url - The URL of the object.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object>} Resolves with the retrieved data.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    getDataInBytes(url: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Retrieves a paginated enumeration envelope from a given URL using a GET request.
     * The response body is an EnumerationResult envelope whose Objects entries are instantiated with the supplied model.
     * @param {string} url - The URL of the objects.
     * @param {Class} model - Model used to instantiate each entry of the envelope's Objects array.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @param {Object} [headers] - Additional headers.
     * @return {Promise<import('../models/EnumerationResult').default>} Resolves with the enumeration result envelope.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    getMany(url: string, model: Class, cancellationToken?: AbortController, headers?: any): Promise<import("../models/EnumerationResult").default>;
    /**
     * Sends a PUT request to update an object at a given URL.
     * @param {string} url - The URL where the object is created.
     * @param {Object} obj - The object to be created.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object>} Resolves with the created object.
     * @throws {Error} Rejects if the URL or object is invalid or if the request fails.
     */
    putUpdate(url: string, obj: any, model: Class, cancellationToken?: AbortController): Promise<any>;
    /**
     * Sends a DELETE request to remove an object at a given URL.
     * @param {string} url - The URL of the object to delete.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<void>} Resolves if the object is successfully deleted.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    delete(url: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Sends a DELETE request and resolves the parsed JSON response body.
     * @param {string} url - The URL to delete.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object|null>} Resolves with the response body, or null when the response has no body.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    deleteForJson(url: string, cancellationToken?: AbortController): Promise<any | null>;
    /**
     * Submits data using a POST request to a given URL.
     * @param {string} url - The URL to post data to.
     * @param {Object|string} data - The data to send in the POST request.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @param {number[]} [acceptedStatusCodes] - Additional HTTP status codes to deserialize as successful responses.
     * @return {Promise<Object>} Resolves with the response data.
     * @throws {Error} Rejects if the URL or data is invalid or if the request fails.
     */
    post(url: string, data: any | string, model: Class, cancellationToken?: AbortController, acceptedStatusCodes?: number[]): Promise<any>;
    /**
     * Sends a GET request and resolves the raw response body as text (no JSON deserialization).
     * Useful for exports whose body may be JSON, CSV, or XML.
     * @param {string} url - The URL to retrieve.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<string>} Resolves with the raw response text.
     */
    getText(url: string, cancellationToken?: AbortController): Promise<string>;
    /**
     * Submits a POST request whose response is a paginated enumeration envelope.
     * The response body is an EnumerationResult envelope whose Objects entries are instantiated with the supplied model.
     * @param {string} url - The URL to post data to.
     * @param {Object|string} data - The data to send in the POST request.
     * @param {Class} model - Model used to instantiate each entry of the envelope's Objects array.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<import('../models/EnumerationResult').default>} Resolves with the enumeration result envelope.
     * @throws {Error} Rejects if the URL or data is invalid or if the request fails.
     */
    postEnumeration(url: string, data: any | string, model: Class, cancellationToken?: AbortController): Promise<import("../models/EnumerationResult").default>;
    /**
     * Submits a POST request and resolves with the raw response text.
     * @param {string} url - The URL to post data to.
     * @param {Object|string} data - The data to send in the POST request body.
     * @param {string} contentType - The Content-Type header value for the request body.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<string>} Resolves with the raw response text.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    postForText(url: string, data: any | string, contentType: string, cancellationToken?: AbortController): Promise<string>;
    /**
     * Submits a POST request with a raw string body and resolves with the parsed JSON response object.
     * @param {string} url - The URL to post data to.
     * @param {string} data - The raw string body to send in the POST request.
     * @param {string} contentType - The Content-Type header value for the request body.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object>} Resolves with the parsed JSON response object.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    postRawForJson(url: string, data: string, contentType: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Submits a POST request and yields parsed server-sent event (SSE) frames as they arrive.
     * Each frame is a `data: <json>` block terminated by a blank line; the stream ends at `data: [DONE]`.
     * Frames that cannot be parsed as JSON are skipped with a warning log.
     * @param {string} url - The URL to post data to.
     * @param {Object|string} data - The data to send in the POST request body.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {AsyncGenerator<Object>} Yields parsed event objects from the SSE stream.
     * @throws {Error} Throws if the URL is invalid or if the request fails with a non-success status.
     */
    postSse(url: string, data: any | string, cancellationToken?: AbortController): AsyncGenerator<any>;
    /**
     * Sends a DELETE request to remove an object at a given URL.
     * @param {string} url - The URL of the object to delete.
     * @param {Object} obj - The object to be created.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<void>} Resolves if the object is successfully deleted.
     * @throws {Error} Rejects if the URL is invalid, the object is not serializable, or if the request fails.
     */
    deleteMany(url: string, obj: any, cancellationToken?: AbortController): Promise<void>;
    /**
     * Submits a POST request.
     * @param {string} url - The URL to which the request is sent.
     * @param {Object} obj - The object to send in the POST request body.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token to cancel the request.
     * @returns {Promise<Object|null>} The response data parsed as an object of type Object, or null if unsuccessful.
     * @throws {Error} If the URL is invalid or the object cannot be serialized to JSON.
     */
    postBatch(url: string, obj: any, model: Class, cancellationToken?: AbortController): Promise<any | null>;
    /**
     * Sends a GET request and resolves with the parsed JSON body whatever the status code, so a response such as a
     * 503 readiness report is returned rather than thrown. Such responses are not retried; connection failures are.
     * @param {string} url - The URL to retrieve.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object|null>} Resolves with the parsed body, or null if the body is empty.
     */
    getAnyStatus(url: string, cancellationToken?: AbortController): Promise<any | null>;
    /**
     * Sends a superagent request built by buildRequest, retrying connection failures and 502/503/504 responses for
     * retryable methods with exponential backoff and jitter, and records the answering node in lastNodeId.
     * Errors are rejected with a nodeId property naming the node that answered, when known.
     * @param {Function} buildRequest - Returns a new superagent request each time it is called.
     * @param {string} url - The request URL, for logging.
     * @param {AbortController} [cancellationToken] - Optional cancellation token; its abort method is replaced.
     * @return {Promise<Object>} Resolves with the superagent response.
     */
    _send(buildRequest: Function, url: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Returns true if the method may be retried under the current policy.
     * @param {string} method - HTTP method, upper case.
     * @return {boolean} True if retryable.
     */
    _canRetryMethod(method: string): boolean;
    /**
     * Returns true for a connection failure (no response, not a timeout) or a 502, 503, or 504 response.
     * @param {Object} err - The superagent error.
     * @return {boolean} True if retryable.
     */
    _isRetryableError(err: any): boolean;
    /**
     * Waits before a retry: the base delay doubled per retry, capped at 5000 ms, less up to half as jitter.
     * @param {number} retry - Retry number, starting at 1.
     * @return {Promise<void>} Resolves after the delay.
     */
    _delayBeforeRetry(retry: number): Promise<void>;
    /**
     * Reads the x-litegraph-node header from a superagent response.
     * @param {Object} [res] - The response.
     * @return {string|null} The node identifier, or null.
     */
    _headerNodeId(res?: any): string | null;
    /**
     * Records the answering node.
     * @param {string|null} nodeId - The node identifier.
     */
    _recordNodeId(nodeId: string | null): void;
}
