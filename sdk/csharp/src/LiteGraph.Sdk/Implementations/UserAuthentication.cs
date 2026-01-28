namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// User authentication methods.
    /// </summary>
    public class UserAuthentication : IUserAuthentication
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// User authentication methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public UserAuthentication(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<List<TenantMetadata>> GetTenantsForEmail(CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(_Sdk.Email)) 
                throw new InvalidOperationException("Email is not set. Use the constructor with email/password/tenantGuid to set the email.");

            string url = _Sdk.Endpoint + "v1.0/token/tenants";

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "x-email", _Sdk.Email }
            };

            return await _Sdk.Get<List<TenantMetadata>>(url, headers, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationToken> GenerateToken(CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/token";

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "x-email", _Sdk.Email },
                { "x-password", _Sdk.Password },
                { "x-tenant-guid", _Sdk.TenantGuid.ToString() }
            };

            return await _Sdk.Get<AuthenticationToken>(url, headers, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationToken> GetTokenDetails(string authToken, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(authToken)) throw new ArgumentNullException(nameof(authToken));
            string url = _Sdk.Endpoint + "v1.0/token/details";

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "x-token", authToken }
            };

            return await _Sdk.Get<AuthenticationToken>(url, headers, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}

