namespace LiteGraph.Sdk
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;
    using RestWrapper;
    using System.Collections.Generic;

    /// <summary>
    /// View SDK base class.
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

        #endregion

        #region Private-Members

        private string _Header = "[LiteGraphSdk] ";
        private string _Endpoint = null;
        private string _BearerToken = null;
        private int _TimeoutMs = 300000;

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
                using (RestRequest req = new RestRequest(url, HttpMethod.Head))
                {
                    req.TimeoutMilliseconds = TimeoutMs;

                    using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                    {
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
            
            using (RestRequest req = new RestRequest(url, HttpMethod.Put))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(json, token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<T>(resp.DataAsString);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
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

            using (RestRequest req = new RestRequest(url, HttpMethod.Head))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
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

            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<T>(resp.DataAsString);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
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

            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.Authorization.BearerToken = BearerToken;

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        req.Headers.Add(header.Key, header.Value);
                    }
                }

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<T>(resp.DataAsString);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
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

            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return resp.DataAsBytes;
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Read objects.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="url">URL.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List.</returns>
        public async Task<List<T>> GetMany<T>(string url, CancellationToken token = default) where T : class
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<List<T>>(resp.DataAsString);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
            }
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

            using (RestRequest req = new RestRequest(url, HttpMethod.Put))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(json, token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<T>(resp.DataAsString);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
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

            using (RestRequest req = new RestRequest(url, HttpMethod.Delete))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                    }

                    return;
                }
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

            using (RestRequest req = new RestRequest(url, HttpMethod.Delete))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(json, token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return;
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return;
                    }
                }
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

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = "application/json";
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(json, token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<T2>(resp.DataAsString);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
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

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                req.ContentType = contentType;
                req.Authorization.BearerToken = BearerToken;

                using (RestResponse resp = await req.SendAsync(bytes, token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return resp.DataAsBytes;
                        }
                        else
                        {
                            Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
