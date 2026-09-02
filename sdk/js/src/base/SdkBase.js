import superagent from 'superagent';
import { SeverityEnum } from '../enums/SeverityEnum';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
import Logger from '../utils/Logger';
import Serializer from '../utils/Serializer';
import ApiErrorResponse from '../models/ApiErrorResponse';

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
  constructor(endpoint, tenantGuid, accessKey) {
    if (!endpoint) {
      GenericExceptionHandlers.ArgumentNullException('Endpoint');
    }
    if (tenantGuid) {
      this.tenantGuid = tenantGuid;
    }
    if (accessKey) {
      this.accessKey = accessKey;
    }

    this._header = '[LiteGraphSdk] ';
    this._endpoint = endpoint.endsWith('/') ? endpoint : endpoint + '/';
    this._timeoutMs = 300000;
    this.logger = Logger.log; // Callback for logging
  }

  /**
   * Getter for the tenant GUID.
   * @return {string} The tenant GUID.
   */
  get tenantGuid() {
    if (!this._tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('TenantGuid');
    }
    return this._tenantGuid;
  }

  /**
   * Setter for the tenant GUID.
   * @param {string} value - The tenant GUID.
   * @throws {Error} Throws an error if the tenant GUID is null or empty.
   */
  set tenantGuid(value) {
    if (!value) {
      GenericExceptionHandlers.ArgumentNullException('TenantGuid');
    }
    this._tenantGuid = value;
  }

  /**
   * Getter for the access key.
   * @return {string} The access key.
   */
  get accessKey() {
    return this._accessKey;
  }

  /**
   * Setter for the access key.
   * @param {string} value - The access key.
   * @throws {Error} Throws an error if the access key is null or empty.
   */
  set accessKey(value) {
    if (!value) {
      GenericExceptionHandlers.ArgumentNullException('AccessKey');
    }
    this.defaultHeaders = {
      ...this.defaultHeaders,
      Authorization: `Bearer ${value}`,
    };
    this._accessKey = value;
  }
  /**
   * Getter for the access token.
   * @return {string} The access token.
   */
  get accessToken() {
    return this._accessToken;
  }

  /**
   * Setter for the access token.
   * @param {string} value - The access token.
   * @throws {Error} Throws an error if the access token is null or empty.
   */
  set accessToken(value) {
    if (!value) {
      GenericExceptionHandlers.ArgumentNullException('AccessToken');
    }

    this.defaultHeaders = {
      ...this.defaultHeaders,
      'x-token': value,
    };
    this._accessToken = value;
  }

  /**
   * Getter for the request header prefix.
   * @return {string} The header prefix.
   */
  get header() {
    return this._header;
  }

  /**
   * Setter for the request header prefix.
   * @param {string} value - The header prefix.
   */
  set header(value) {
    if (!value || typeof value !== 'string') {
      this._header = value;
    } else {
      this._header = value.endsWith(' ') ? value : value + ' ';
    }
  }

  /**
   * Getter for the API endpoint.
   * @return {string} The endpoint URL.
   */
  get endpoint() {
    return this._endpoint;
  }

  /**
   * Setter for the API endpoint.
   * @param {string} value - The endpoint URL.
   * @throws {Error} Throws an error if the endpoint is null or empty.
   */
  set endpoint(value) {
    if (!value) {
      GenericExceptionHandlers.ArgumentNullException('Endpoint');
    }
    this._endpoint = value.endsWith('/') ? value : value + '/';
  }

  /**
   * Getter for the timeout in milliseconds.
   * @return {number} The timeout in milliseconds.
   */
  get timeoutMs() {
    return this._timeoutMs;
  }

  /**
   * Setter for the timeout in milliseconds.
   * @param {number} value - Timeout value in milliseconds.
   * @throws {Error} Throws an error if the timeout is less than 1.
   */
  set timeoutMs(value) {
    if (value < 1) {
      GenericExceptionHandlers.GenericException('TimeoutMs must be greater than 0.');
    }
    this._timeoutMs = value;
  }

  /**
   * Logs a message with a severity level.
   * @param {string} sev - The severity level (e.g., SeverityEnum.Debug, 'warn').
   * @param {string} msg - The message to log.
   */
  log(sev, msg) {
    if (!msg) return;
    if (this.logger) this.logger(sev, this._header + msg);
  }

  /**
   * Validates API connectivity using a HEAD request.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the connection is successful.
   * @throws {Error} Rejects with the error in case of failure.
   */
  validateConnectivity(cancellationToken) {
    return new Promise((resolve, reject) => {
      const request = superagent.head(this._endpoint).timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${this._endpoint}`);
          resolve(res.ok);
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${this._endpoint}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Sends a PUT request to create an object at a given URL.
   * @param {string} url - The URL where the object is created.
   * @param {Object} obj - The object to be created.
   * @param {Class} model - Modal to deserialize on
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Object>} Resolves with the created object.
   * @throws {Error} Rejects if the URL or object is invalid or if the request fails.
   */
  putCreate(url, obj, model, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));
      if (!obj) return reject(new Error('Object cannot be null.'));

      const request = superagent
        .put(url)
        .set(this.defaultHeaders)
        .set('Content-Type', 'application/json')
        .send(obj)
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeJson(res.text, model));
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Checks if an object exists at a given URL using a HEAD request.
   * @param {string} url - The URL to check.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the object exists.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  head(url, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent.head(url).set(this.defaultHeaders).timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(res.ok);
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Retrieves an object from a given URL using a GET request.
   * @param {string} url - The URL of the object.
   * @param {Class} model - Modal to deserialize on
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @param {Object} [headers] - Additional headers.
   * @return {Promise<Object>} Resolves with the retrieved object.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  get(url, model, cancellationToken, headers) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .get(url)
        .set({ ...this.defaultHeaders, ...(headers || {}) })
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeJson(res.text, model));
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Retrieves raw data from a given URL using a GET request.
   * @param {string} url - The URL of the object.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Object>} Resolves with the retrieved data.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  getDataInBytes(url, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent.get(url).set(this.defaultHeaders).timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(res.text ? res.text : Serializer.deserializeJson(res.body));
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

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
  getMany(url, model, cancellationToken, headers) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .get(url)
        .set({ ...this.defaultHeaders, ...(headers || {}) })
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeEnumeration(res.text, model));
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Sends a PUT request to update an object at a given URL.
   * @param {string} url - The URL where the object is created.
   * @param {Object} obj - The object to be created.
   * @param {Class} model - Modal to deserialize on
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Object>} Resolves with the created object.
   * @throws {Error} Rejects if the URL or object is invalid or if the request fails.
   */
  putUpdate(url, obj, model, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));
      if (!obj) return reject(new Error('Object cannot be null.'));

      const request = superagent
        .put(url)
        .set(this.defaultHeaders)
        .set('Content-Type', 'application/json')
        .send(obj)
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeJson(res.text, model));
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Sends a DELETE request to remove an object at a given URL.
   * @param {string} url - The URL of the object to delete.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<void>} Resolves if the object is successfully deleted.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  delete(url, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent.delete(url).set(this.defaultHeaders).timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve();
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

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
  post(url, data, model, cancellationToken, acceptedStatusCodes = []) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .post(url)
        .set(this.defaultHeaders)
        // .set('Content-Type', contentType)
        .send(data)
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeJson(res.text, model));
        })
        .catch((err) => {
          const statusCode = err?.response?.status;
          if (acceptedStatusCodes.includes(statusCode)) {
            this.log(SeverityEnum.Debug, `Accepted non-success reported from ${url}: ${statusCode}`);
            const responseText = err?.response?.text || JSON.stringify(err?.response?.body || {});
            resolve(Serializer.deserializeJson(responseText, model));
            return;
          }

          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

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
  postEnumeration(url, data, model, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent.post(url).set(this.defaultHeaders).send(data).timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeEnumeration(res.text, model));
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Submits a POST request and resolves with the raw response text.
   * @param {string} url - The URL to post data to.
   * @param {Object|string} data - The data to send in the POST request body.
   * @param {string} contentType - The Content-Type header value for the request body.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<string>} Resolves with the raw response text.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  postForText(url, data, contentType, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .post(url)
        .set(this.defaultHeaders)
        .set('Content-Type', contentType)
        .buffer(true)
        .send(data)
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          if (res.text) {
            resolve(res.text);
          } else if (res.body && Buffer.isBuffer(res.body)) {
            resolve(res.body.toString('utf-8'));
          } else {
            resolve(null);
          }
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Submits a POST request with a raw string body and resolves with the parsed JSON response object.
   * @param {string} url - The URL to post data to.
   * @param {string} data - The raw string body to send in the POST request.
   * @param {string} contentType - The Content-Type header value for the request body.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Object>} Resolves with the parsed JSON response object.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  postRawForJson(url, data, contentType, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .post(url)
        .set(this.defaultHeaders)
        .set('Content-Type', contentType)
        .send(data)
        .timeout({ response: this._timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          if (res.text && res.text.length > 0) {
            resolve(JSON.parse(res.text));
          } else {
            resolve(res.body || null);
          }
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

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
  // eslint-disable-next-line node/no-unsupported-features/es-syntax -- streaming requires Node 18+ (fetch); guarded at runtime below
  async *postSse(url, data, cancellationToken) {
    if (!url) throw new Error('URL cannot be null or empty.');
    if (typeof fetch !== 'function') {
      throw new Error('Streaming requires a fetch-capable environment (Node 18+ or a modern browser).');
    }

    const controller = new AbortController();
    if (cancellationToken) {
      cancellationToken.abort = () => {
        controller.abort();
        this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
      };
    }

    const response = await fetch(url, {
      method: 'POST',
      headers: { ...this.defaultHeaders, 'Content-Type': 'application/json', Accept: 'text/event-stream' },
      body: typeof data === 'string' ? data : JSON.stringify(data),
      signal: controller.signal,
    });

    if (!response.ok) {
      this.log(SeverityEnum.Warn, `Non-success reported from ${url}: ${response.status}`);
      let errorResponse = null;
      try {
        errorResponse = await response.json();
      } catch (err) {
        errorResponse = null;
      }
      if (errorResponse && errorResponse.Error) {
        throw new ApiErrorResponse(errorResponse.Error, errorResponse.Context, errorResponse.Message);
      }
      throw new Error(`Request to ${url} failed with status ${response.status}.`);
    }

    this.log(SeverityEnum.Debug, `Success reported from ${url}: ${response.status}`);

    const reader = response.body.getReader();
    // eslint-disable-next-line node/no-unsupported-features/node-builtins -- available in all fetch-capable environments
    const decoder = new TextDecoder('utf-8');
    let buffer = '';

    const parseFrame = (frame) => {
      const dataLines = frame
        .split('\n')
        .filter((line) => line.startsWith('data:'))
        .map((line) => line.slice(5).trim());
      if (dataLines.length === 0) return { done: false, event: undefined };
      const payload = dataLines.join('\n');
      if (payload === '[DONE]') return { done: true, event: undefined };
      try {
        return { done: false, event: JSON.parse(payload) };
      } catch (err) {
        this.log(SeverityEnum.Warn, `Skipping malformed SSE frame from ${url}: ${payload}`);
        return { done: false, event: undefined };
      }
    };

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        buffer = buffer.replace(/\r\n/g, '\n');

        let separatorIndex;
        while ((separatorIndex = buffer.indexOf('\n\n')) !== -1) {
          const frame = buffer.slice(0, separatorIndex);
          buffer = buffer.slice(separatorIndex + 2);
          const parsed = parseFrame(frame);
          if (parsed.done) return;
          if (parsed.event !== undefined) yield parsed.event;
        }
      }

      // Flush any trailing frame without a terminating blank line.
      const remainder = buffer.trim();
      if (remainder.length > 0) {
        const parsed = parseFrame(remainder);
        if (!parsed.done && parsed.event !== undefined) yield parsed.event;
      }
    } finally {
      try {
        await reader.cancel();
      } catch (err) {
        // Reader may already be closed; ignore.
      }
    }
  }

  /**
   * Sends a DELETE request to remove an object at a given URL.
   * @param {string} url - The URL of the object to delete.
   * @param {Object} obj - The object to be created.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<void>} Resolves if the object is successfully deleted.
   * @throws {Error} Rejects if the URL is invalid, the object is not serializable, or if the request fails.
   */
  deleteMany(url, obj, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      let json;
      try {
        // json = JSON.stringify(obj);
        json = Serializer.serializeJson(obj, true);
      } catch (err) {
        return reject(new Error('Supplied object is not serializable to JSON.'));
      }

      const request = superagent
        .delete(url)
        .send(json)
        .set(this.defaultHeaders)
        .set('Content-Type', 'application/json')
        .timeout({ response: this._timeoutMs });

      // Attach the abort method if cancellationToken is provided
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }

      request
        .then((res) => {
          if (res.status >= 200 && res.status <= 299) {
            this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
            resolve();
          } else {
            this.log(SeverityEnum.Warn, `Non-success reported from ${url}: ${res.status}`);
            resolve();
          }
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Submits a POST request.
   * @param {string} url - The URL to which the request is sent.
   * @param {Object} obj - The object to send in the POST request body.
   * @param {Class} model - Modal to deserialize on
   * @param {AbortController} [cancellationToken] - Optional cancellation token to cancel the request.
   * @returns {Promise<Object|null>} The response data parsed as an object of type Object, or null if unsuccessful.
   * @throws {Error} If the URL is invalid or the object cannot be serialized to JSON.
   */
  async postBatch(url, obj, model, cancellationToken) {
    return new Promise((resolve, reject) => {
      if (!url) throw new Error('URL cannot be null or empty.');

      const json = Serializer.serializeJson(obj, true);
      if (json === null) throw new Error('Supplied object is not serializable to JSON.');

      const request = superagent
        .post(url)
        .timeout({ response: this._timeoutMs })
        .set(this.defaultHeaders)
        .set('Content-Type', 'application/json')
        .send(json);

      // Handle cancellation if token is provided
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }

      // const res = await request;
      request
        .then((res) => {
          if (res.status >= 200 && res.status <= 299) {
            this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);

            // If response has content, parse as JSON and return
            if (res.text && res.text.length > 0) {
              resolve(Serializer.deserializeJson(res.text, model));
            } else {
              return null;
            }
          } else {
            this.log(SeverityEnum.Warn, `Non-success reported from ${url}: ${res.status}`);
            return null;
          }
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.Error) {
            const apiErrorResponse = new ApiErrorResponse(
              errorResponse?.Error,
              errorResponse?.Context,
              errorResponse?.Message
            );
            reject(apiErrorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }
}
