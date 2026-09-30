namespace LiteGraph.Sdk
{
    using System;
    using RestWrapper;

    /// <summary>
    /// A completed REST exchange: the request and its response, disposed together so the response body stays readable
    /// for as long as the caller needs it.
    /// </summary>
    internal sealed class SdkExchange : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Response, or null when the server did not answer.
        /// </summary>
        internal RestResponse Response { get; }

        #endregion

        #region Private-Members

        private readonly RestRequest _Request;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="response">Response, or null.</param>
        internal SdkExchange(RestRequest request, RestResponse response)
        {
            _Request = request ?? throw new ArgumentNullException(nameof(request));
            Response = response;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose the response and the request.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            Response?.Dispose();
            _Request.Dispose();
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
