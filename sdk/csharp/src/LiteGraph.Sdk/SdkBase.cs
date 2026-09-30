namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;

    /// <summary>
    /// View SDK base class.
    /// Requests that fail with a connection error or a 502, 503, or 504 response are retried with exponential backoff and
    /// jitter (see MaxRetries, RetryBaseDelayMs, and RetryPost), and the node that answered each request is recorded in
    /// LastNodeId.
    /// Thread safety: an instance may be used from multiple threads; LastNodeId then reflects whichever request completed last.
    /// </summary>
    public class SdkBase : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Method to invoke to send log messages.
        /// </summary>
        public Action<SeverityEnum, string> Logger { get; set; } = null;

        /// <summary>
        /// Header to prepend to log messages.
        /// </summary>
        public string Header
        {
            get
            {
                return _Header;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _Header = value;
                }
                else
                {
                    if (!value.EndsWith(" ")) value += " ";
                    _Header = value;
                }
            }
        }

        /// <summary>
        /// Endpoint URL.
        /// </summary>
        public string Endpoint
        {
            get
            {
                return _Endpoint;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Endpoint));
                Uri uri = new Uri(value);
                if (!value.EndsWith("/")) value += "/";
                _Endpoint = value;
            }
        }

        /// <summary>
        /// Bearer token.
        /// </summary>
        public string BearerToken
        {
            get
            {
                return _BearerToken;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(BearerToken));
                _BearerToken = value;
            }
        }

        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        public int TimeoutMs
        {
            get
            {
                return _TimeoutMs;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(TimeoutMs));
                _TimeoutMs = value;
            }
        }

        /// <summary>
        /// Maximum number of retries after the first attempt, for requests that fail with a connection error or a 502, 503,
        /// or 504 response.  GET, HEAD, PUT, and DELETE requests are retried; POST requests only when RetryPost is true.
        /// Default is 2.  Minimum is 0 (no retries), maximum is 10.
        /// </summary>
        public int MaxRetries
        {
            get
            {
                return _MaxRetries;
            }
            set
            {
                if (value < 0 || value > 10) throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries must be between 0 and 10.");
                _MaxRetries = value;
            }
        }

        /// <summary>
        /// Base delay before the first retry, in milliseconds.  Each further retry doubles it, capped at 5000 ms, and a random
        /// jitter of up to half the delay is subtracted so clients do not retry in lockstep.
        /// Default is 200.  Minimum is 0, maximum is 5000.
        /// </summary>
        public int RetryBaseDelayMs
        {
            get
            {
                return _RetryBaseDelayMs;
            }
            set
            {
                if (value < 0 || value > 5000) throw new ArgumentOutOfRangeException(nameof(RetryBaseDelayMs), "RetryBaseDelayMs must be between 0 and 5000.");
                _RetryBaseDelayMs = value;
            }
        }

        /// <summary>
        /// Retry POST requests too.  POST requests are not idempotent, so a retried POST can apply twice if the first attempt
        /// reached the server; enable this only when the operations you POST are safe to repeat.  Default is false.
        /// Streaming responses are never retried once any of the body has been read.
        /// </summary>
        public bool RetryPost { get; set; } = false;

        /// <summary>
        /// Node that answered the most recent request, from the x-litegraph-node response header.  Null until a response
        /// carrying the header is received.  Behind a load balancer this identifies which cluster node served the request.
        /// </summary>
        public string LastNodeId
        {
            get
            {
                return Volatile.Read(ref _LastNodeId);
            }
        }

        #endregion

        #region Private-Members

        private static readonly string _NodeHeader = "x-litegraph-node";
        private static readonly int _MaxRetryDelayMs = 5000;
        private static readonly HttpClient _StreamingHttpClient = new HttpClient() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

        private string _Header = "[LiteGraphSdk] ";
        private string _Endpoint = null;
        private string _BearerToken = null;
        private int _TimeoutMs = 300000;
        private int _MaxRetries = 2;
        private int _RetryBaseDelayMs = 200;
        private string _LastNodeId = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="bearerToken">Bearer token.</param>
        public SdkBase(string endpoint, string bearerToken)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrEmpty(bearerToken)) throw new ArgumentNullException(nameof(bearerToken));

            Endpoint = endpoint;
            BearerToken = bearerToken;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Logger = null;

            _Header = null;
            _Endpoint = null;
        }

        /// <summary>
        /// Emit a log message.
        /// </summary>
        /// <param name="sev">Severity.</param>
        /// <param name="msg">Message.</param>
        public void Log(SeverityEnum sev, string msg)
        {
            if (String.IsNullOrEmpty(msg)) return;
            Logger?.Invoke(sev, _Header + msg);
        }

        /// <summary>
        /// Validate connectivity.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Boolean indicating success.</returns>
        public async Task<bool> ValidateConnectivity(CancellationToken token = default)
        {
            string url = _Endpoint;

            try
            {
                using (SdkExchange exchange = await SendAsync(url, HttpMethod.Head, null, null, null, false, token).ConfigureAwait(false))
                {
                    RestResponse resp = exchange.Response;
                    if (resp != null && resp.StatusCode == 200)
                    {
                        Log(SeverityEnum.Debug, "success reported from " + url);
                        return true;
                    }
                    else if (resp != null)
                    {
                        Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode);
                        return false;
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log(SeverityEnum.Warn, "exception while validating connectivity to " + url + Environment.NewLine + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Create an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="obj">Object.</param>
        /// <param name="token"></param>
        /// <returns>Instance.</returns>
        public async Task<T> PutCreate<T>(string url, T obj, CancellationToken token = default) where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            string json = null;
            if (!Serializer.TrySerializeJson(obj, true, out json))
                throw new ArgumentException("Supplied object is not serializable to JSON.");

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Put, "application/json", json, null, true, token).ConfigureAwait(false))
            {
                return DeserializeOnSuccess<T>(url, exchange.Response);
            }
        }

        /// <summary>
        /// Check if an object exists.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        public async Task<bool> Head(string url, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Head, null, null, null, true, token).ConfigureAwait(false))
            {
                RestResponse resp = exchange.Response;
                if (resp != null)
                {
                    if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                        return true;
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                        return false;
                    }
                }
                else
                {
                    Log(SeverityEnum.Warn, "no response from " + url);
                    return false;
                }
            }
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance.</returns>
        public async Task<T> Get<T>(string url, CancellationToken token = default) where T : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Get, null, null, null, true, token).ConfigureAwait(false))
            {
                return DeserializeOnSuccess<T>(url, exchange.Response);
            }
        }

        /// <summary>
        /// Read an object with custom headers.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="headers">Custom headers dictionary.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance.</returns>
        public async Task<T> Get<T>(string url, Dictionary<string, string> headers, CancellationToken token = default) where T : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Get, null, null, headers, true, token).ConfigureAwait(false))
            {
                return DeserializeOnSuccess<T>(url, exchange.Response);
            }
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance.</returns>
        public async Task<byte[]> Get(string url, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Get, null, null, null, true, token).ConfigureAwait(false))
            {
                return await BytesOnSuccess(url, exchange.Response, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read an enumeration envelope from a GET URL.
        /// All list-shaped GET routes on the LiteGraph server return an EnumerationResult envelope.
        /// </summary>
        /// <typeparam name="T">Type of the enumerated objects.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing the matching objects, or null if the request failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the URL is null or empty.</exception>
        public async Task<EnumerationResult<T>> GetEnumeration<T>(string url, CancellationToken token = default) where T : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            return await Get<EnumerationResult<T>>(url, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="obj">Object.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance.</returns>
        public async Task<T> PutUpdate<T>(string url, T obj, CancellationToken token = default) where T : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            string json = null;
            if (!Serializer.TrySerializeJson(obj, true, out json))
                throw new ArgumentException("Supplied object is not serializable to JSON.");

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Put, "application/json", json, null, true, token).ConfigureAwait(false))
            {
                return DeserializeOnSuccess<T>(url, exchange.Response);
            }
        }

        /// <summary>
        /// Delete an object.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Delete(string url, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Delete, null, null, null, true, token).ConfigureAwait(false))
            {
                LogOutcome(url, exchange.Response);
            }
        }

        /// <summary>
        /// Delete an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="obj">Object.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Delete<T>(string url, T obj, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            string json = null;
            if (!Serializer.TrySerializeJson(obj, true, out json))
                throw new ArgumentException("Supplied object is not serializable to JSON.");

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Delete, "application/json", json, null, true, token).ConfigureAwait(false))
            {
                LogOutcome(url, exchange.Response);
            }
        }

        /// <summary>
        /// Submit a DELETE request and deserialize the response body.
        /// </summary>
        /// <typeparam name="T">Response type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance of T, or null if the request failed or returned no body.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the URL is null or empty.</exception>
        public async Task<T> DeleteWithResult<T>(string url, CancellationToken token = default) where T : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Delete, null, null, null, true, token).ConfigureAwait(false))
            {
                return DeserializeOnSuccess<T>(url, exchange.Response);
            }
        }

        /// <summary>
        /// Submit a POST request.
        /// </summary>
        /// <typeparam name="T1">Input object type.</typeparam>
        /// <typeparam name="T2">Return object type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="obj">Object.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Instance of T2.</returns>
        public async Task<T2> Post<T1, T2>(string url, T1 obj, CancellationToken token = default)
            where T1 : class
            where T2 : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            string json = null;
            if (!Serializer.TrySerializeJson(obj, true, out json))
                throw new ArgumentException("Supplied object is not serializable to JSON.");

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Post, "application/json", json, null, true, token).ConfigureAwait(false))
            {
                return DeserializeOnSuccess<T2>(url, exchange.Response);
            }
        }

        /// <summary>
        /// Submit a POST request.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="bytes">Bytes.</param>
        /// <param name="contentType">Content-type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Bytes.</returns>
        public async Task<byte[]> PostRaw(
            string url,
            byte[] bytes,
            string contentType = "application/octet-stream",
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (bytes == null) bytes = Array.Empty<byte>();

            using (SdkExchange exchange = await SendAsync(url, HttpMethod.Post, contentType, bytes, null, true, token).ConfigureAwait(false))
            {
                return await BytesOnSuccess(url, exchange.Response, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Issue a GET and return the raw response body bytes, correctly reassembling chunked (streamed) responses.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Response body bytes on success; null otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the URL is null or empty.</exception>
        public async Task<byte[]> GetStreamingBytes(string url, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (HttpResponseMessage resp = await SendHttpAsync(HttpMethod.Get, url, () => BuildHttpRequest(HttpMethod.Get, url, null, null), token).ConfigureAwait(false))
            {
                if (!resp.IsSuccessStatusCode)
                {
                    Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + (int)resp.StatusCode);
                    return null;
                }
                return await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Issue a POST with a raw body and return the raw response body bytes, correctly reassembling chunked responses.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="bytes">Request body bytes.</param>
        /// <param name="contentType">Request content type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Response body bytes on success; null otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the URL is null or empty.</exception>
        public async Task<byte[]> PostStreamingBytes(string url, byte[] bytes, string contentType = "application/octet-stream", CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (bytes == null) bytes = Array.Empty<byte>();

            using (HttpResponseMessage resp = await SendHttpAsync(HttpMethod.Post, url, () => BuildHttpRequest(HttpMethod.Post, url, bytes, contentType), token).ConfigureAwait(false))
            {
                byte[] body = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + (int)resp.StatusCode);
                return body;
            }
        }

        /// <summary>
        /// Issue a PUT with a raw body and return the raw response body bytes.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="bytes">Request body bytes.</param>
        /// <param name="contentType">Request content type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Response body bytes on success; null otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the URL is null or empty.</exception>
        public async Task<byte[]> PutStreamingBytes(string url, byte[] bytes, string contentType = "application/octet-stream", CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (bytes == null) bytes = Array.Empty<byte>();

            using (HttpResponseMessage resp = await SendHttpAsync(HttpMethod.Put, url, () => BuildHttpRequest(HttpMethod.Put, url, bytes, contentType), token).ConfigureAwait(false))
            {
                byte[] body = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + (int)resp.StatusCode);
                return body;
            }
        }

        /// <summary>
        /// Issue a POST with a raw body and consume the response as a server-sent event stream.
        /// Yields the data payload of each SSE frame with the leading "data:" prefix removed.
        /// Enumeration ends when the server emits a "[DONE]" sentinel frame or closes the stream.
        /// On a non-success status code a warning is logged and no events are yielded.
        /// The request is retried only before the stream starts, and only when RetryPost is true.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="bytes">Request body bytes.</param>
        /// <param name="contentType">Request content type.  Default is application/json.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of SSE data payloads.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the URL is null or empty.</exception>
        public async IAsyncEnumerable<string> PostServerSentEvents(
            string url,
            byte[] bytes,
            string contentType = "application/json",
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (bytes == null) bytes = Array.Empty<byte>();

            Func<HttpRequestMessage> build = () =>
            {
                HttpRequestMessage message = BuildHttpRequest(HttpMethod.Post, url, bytes, contentType);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                return message;
            };

            using (HttpResponseMessage resp = await SendHttpAsync(HttpMethod.Post, url, build, token).ConfigureAwait(false))
            {
                if (!resp.IsSuccessStatusCode)
                {
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + (int)resp.StatusCode + (!String.IsNullOrEmpty(body) ? ", " + body : String.Empty));
                    yield break;
                }

                using (Stream stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (StreamReader reader = new StreamReader(stream))
                {
                    StringBuilder dataBuffer = new StringBuilder();

                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        string line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break;

                        if (line.Length == 0)
                        {
                            if (dataBuffer.Length > 0)
                            {
                                string payload = dataBuffer.ToString();
                                dataBuffer.Clear();
                                if (payload.Equals("[DONE]", StringComparison.Ordinal)) yield break;
                                yield return payload;
                            }

                            continue;
                        }

                        if (line.StartsWith("data:", StringComparison.Ordinal))
                        {
                            string data = line.Substring(5);
                            if (data.StartsWith(" ", StringComparison.Ordinal)) data = data.Substring(1);
                            if (dataBuffer.Length > 0) dataBuffer.Append('\n');
                            dataBuffer.Append(data);
                        }
                    }

                    if (dataBuffer.Length > 0)
                    {
                        string payload = dataBuffer.ToString();
                        if (!payload.Equals("[DONE]", StringComparison.Ordinal)) yield return payload;
                    }
                }
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Send a request with the retry policy applied, recording the answering node.  The caller disposes the exchange.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="contentType">Content type, or null.</param>
        /// <param name="body">Body: a string, a byte array, or null.</param>
        /// <param name="headers">Additional headers, or null.</param>
        /// <param name="authenticate">True to send the bearer token.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Exchange holding the final response, which is null if the server did not answer.</returns>
        internal Task<SdkExchange> SendAsync(
            string url,
            HttpMethod method,
            string contentType,
            object body,
            Dictionary<string, string> headers,
            bool authenticate,
            CancellationToken token)
        {
            return SendAsync(url, method, contentType, body, headers, authenticate, true, token);
        }

        /// <summary>
        /// Send a request, applying the retry policy only when retry is true, and record the answering node.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="contentType">Content type, or null.</param>
        /// <param name="body">Body: a string, a byte array, or null.</param>
        /// <param name="headers">Additional headers, or null.</param>
        /// <param name="authenticate">True to send the bearer token.</param>
        /// <param name="retry">False to send exactly once, for example when a 503 response is itself the answer.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Exchange holding the final response, which is null if the server did not answer.</returns>
        internal async Task<SdkExchange> SendAsync(
            string url,
            HttpMethod method,
            string contentType,
            object body,
            Dictionary<string, string> headers,
            bool authenticate,
            bool retry,
            CancellationToken token)
        {
            int attempt = retry ? 0 : Int32.MaxValue;

            while (true)
            {
                RestRequest req = new RestRequest(url, method);
                req.TimeoutMilliseconds = TimeoutMs;
                if (!String.IsNullOrEmpty(contentType)) req.ContentType = contentType;
                if (authenticate) req.Authorization.BearerToken = BearerToken;
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers) req.Headers.Add(header.Key, header.Value);
                }

                RestResponse resp;
                try
                {
                    if (body is string text) resp = await req.SendAsync(text, token).ConfigureAwait(false);
                    else if (body is byte[] bytes) resp = await req.SendAsync(bytes, token).ConfigureAwait(false);
                    else resp = await req.SendAsync(token).ConfigureAwait(false);
                }
                catch (Exception e) when (IsConnectionFailure(e, token) && CanRetry(method, attempt))
                {
                    req.Dispose();
                    attempt++;
                    Log(SeverityEnum.Debug, "connection failure on " + method + " " + url + " (" + e.Message + "), retry " + attempt + " of " + MaxRetries);
                    await DelayBeforeRetryAsync(attempt, token).ConfigureAwait(false);
                    continue;
                }
                catch
                {
                    req.Dispose();
                    throw;
                }

                if (resp != null) RecordNode(resp.Headers?.Get(_NodeHeader));

                bool retryable = resp == null || IsRetryableStatus(resp.StatusCode);
                if (retryable && CanRetry(method, attempt))
                {
                    resp?.Dispose();
                    req.Dispose();
                    attempt++;
                    Log(SeverityEnum.Debug, (resp == null ? "no response" : "status " + resp.StatusCode) + " on " + method + " " + url + ", retry " + attempt + " of " + MaxRetries);
                    await DelayBeforeRetryAsync(attempt, token).ConfigureAwait(false);
                    continue;
                }

                return new SdkExchange(req, resp);
            }
        }

        private async Task<HttpResponseMessage> SendHttpAsync(HttpMethod method, string url, Func<HttpRequestMessage> build, CancellationToken token)
        {
            int attempt = 0;

            while (true)
            {
                HttpRequestMessage req = build();
                HttpResponseMessage resp;
                try
                {
                    resp = await _StreamingHttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                }
                catch (Exception e) when (IsConnectionFailure(e, token) && CanRetry(method, attempt))
                {
                    req.Dispose();
                    attempt++;
                    Log(SeverityEnum.Debug, "connection failure on " + method + " " + url + " (" + e.Message + "), retry " + attempt + " of " + MaxRetries);
                    await DelayBeforeRetryAsync(attempt, token).ConfigureAwait(false);
                    continue;
                }

                if (resp.Headers.TryGetValues(_NodeHeader, out IEnumerable<string> values)) RecordNode(values.FirstOrDefault());

                // Only the status line and headers have been read, so no body bytes have been consumed yet.
                if (IsRetryableStatus((int)resp.StatusCode) && CanRetry(method, attempt))
                {
                    resp.Dispose();
                    req.Dispose();
                    attempt++;
                    Log(SeverityEnum.Debug, "status " + (int)resp.StatusCode + " on " + method + " " + url + ", retry " + attempt + " of " + MaxRetries);
                    await DelayBeforeRetryAsync(attempt, token).ConfigureAwait(false);
                    continue;
                }

                return resp;
            }
        }

        private HttpRequestMessage BuildHttpRequest(HttpMethod method, string url, byte[] bytes, string contentType)
        {
            HttpRequestMessage req = new HttpRequestMessage(method, url);
            if (!String.IsNullOrEmpty(BearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
            if (bytes != null)
            {
                req.Content = new ByteArrayContent(bytes);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
            return req;
        }

        private bool CanRetry(HttpMethod method, int attempt)
        {
            if (attempt >= MaxRetries) return false;
            if (method == HttpMethod.Post) return RetryPost;
            return method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Put || method == HttpMethod.Delete;
        }

        private static bool IsRetryableStatus(int statusCode)
        {
            return statusCode == 502 || statusCode == 503 || statusCode == 504;
        }

        private static bool IsConnectionFailure(Exception e, CancellationToken token)
        {
            if (token.IsCancellationRequested) return false;
            for (Exception current = e; current != null; current = current.InnerException)
            {
                if (current is HttpRequestException || current is SocketException || current is IOException) return true;
            }
            return false;
        }

        private async Task DelayBeforeRetryAsync(int attempt, CancellationToken token)
        {
            int delay = (int)Math.Min(_MaxRetryDelayMs, (long)RetryBaseDelayMs << Math.Min(attempt - 1, 16));
            if (delay <= 0) return;
            int jittered = delay - Random.Shared.Next(0, delay / 2 + 1);
            await Task.Delay(jittered, token).ConfigureAwait(false);
        }

        private void RecordNode(string nodeId)
        {
            if (!String.IsNullOrEmpty(nodeId)) Volatile.Write(ref _LastNodeId, nodeId);
        }

        private T DeserializeOnSuccess<T>(string url, RestResponse resp) where T : class
        {
            if (resp == null)
            {
                Log(SeverityEnum.Warn, "no response from " + url);
                return null;
            }

            if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
            {
                Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                if (!String.IsNullOrEmpty(resp.DataAsString)) return Serializer.DeserializeJson<T>(resp.DataAsString);
                return null;
            }

            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
            return null;
        }

        private async Task<byte[]> BytesOnSuccess(string url, RestResponse resp, CancellationToken token)
        {
            if (resp == null)
            {
                Log(SeverityEnum.Warn, "no response from " + url);
                return null;
            }

            if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
            {
                Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                return await ReadAllBytes(resp, token).ConfigureAwait(false);
            }

            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
            return null;
        }

        private void LogOutcome(string url, RestResponse resp)
        {
            if (resp == null)
                Log(SeverityEnum.Warn, "no response from " + url);
            else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
            else
                Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
        }

        private static async Task<byte[]> ReadAllBytes(RestResponse resp, CancellationToken token)
        {
            if (resp == null) return null;

            if (resp.ChunkedTransferEncoding)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();
                        ChunkData chunk = await resp.ReadChunkAsync(token).ConfigureAwait(false);
                        if (chunk == null) break;
                        if (chunk.Data != null && chunk.Data.Length > 0) ms.Write(chunk.Data, 0, chunk.Data.Length);
                        if (chunk.IsFinal) break;
                    }
                    return ms.ToArray();
                }
            }

            return resp.DataAsBytes;
        }

        #endregion
    }
}
