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
    logger: (severity: any, message: string) => void;
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
     * Retrieves a list of objects from a given URL using a GET request.
     * @param {string} url - The URL of the objects.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @param {Object} [headers] - Additional headers.
     * @return {Promise<Array>} Resolves with the list of retrieved objects.
     * @throws {Error} Rejects if the URL is invalid or if the request fails.
     */
    getMany(url: string, model: Class, cancellationToken?: AbortController, headers?: any): Promise<any[]>;
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
     * Submits data using a POST request to a given URL.
     * @param {string} url - The URL to post data to.
     * @param {Object|string} data - The data to send in the POST request.
     * @param {Class} model - Modal to deserialize on
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @return {Promise<Object>} Resolves with the response data.
     * @throws {Error} Rejects if the URL or data is invalid or if the request fails.
     */
    post(url: string, data: any | string, model: Class, cancellationToken?: AbortController): Promise<any>;
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
}
