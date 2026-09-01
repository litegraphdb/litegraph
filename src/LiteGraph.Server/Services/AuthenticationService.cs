namespace LiteGraph.Server.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SyslogLogging;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;

    internal class AuthenticationService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[AuthService] ";
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = new Serializer();
        private GraphRepositoryBase _Repo = null;
        private AuthorizationService _Authorization = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Authentication service.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging settings.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="repo">Graph repository driver.</param>
        public AuthenticationService(
            Settings settings,
            LoggingModule logging,
            Serializer serializer,
            GraphRepositoryBase repo)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _Authorization = new AuthorizationService(_Logging, _Repo);
        }

        /// <summary>
        /// Authorization service used by this authentication service.
        /// Shared with in-process dispatchers (for example chat tool calls) so RBAC caches stay unified.
        /// </summary>
        public AuthorizationService Authorization
        {
            get
            {
                return _Authorization;
            }
        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Authenticate and authorize.
        /// </summary>
        /// <param name="req">Request context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Request context.</returns>
        internal async Task AuthenticateAndAuthorize(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            try
            {
                if (_Settings.Debug.Authentication)
                {
                    _Logging.Info(
                        _Header + "authenticating and authorizing using the following materials:" + Environment.NewLine +
                        "| Tenant GUID    : " + req.Authentication.TenantGUID + Environment.NewLine +
                        "| Email          : " + req.Authentication.Email + Environment.NewLine +
                        "| Password       : " + OperationalLogRedactor.RedactValue(req.Authentication.Password) + Environment.NewLine +
                        "| Bearer token   : " + OperationalLogRedactor.RedactValue(req.Authentication.BearerToken) + Environment.NewLine +
                        "| Security token : " + OperationalLogRedactor.RedactValue(req.Authentication.SecurityToken));
                }

                await Authenticate(req, token).ConfigureAwait(false);
                await _Authorization.Authorize(req, token).ConfigureAwait(false);

                if (_Settings.Debug.Authentication)
                {
                    _Logging.Info(
                        _Header + "authentication and authorization result:" + Environment.NewLine +
                        "| Authentication : " + req.Authentication.Result + Environment.NewLine +
                        "| Authorization  : " + req.Authorization.Result);
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception:" + Environment.NewLine + e.ToString());
                req.Authentication.Result = AuthenticationResultEnum.Invalid;
                req.Authorization.Result = AuthorizationResultEnum.Denied;
                return;
            }
        }

        /// <summary>
        /// Generate a token for a user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <returns>Authentication token.</returns>
        internal AuthenticationToken GenerateToken(Guid tenantGuid, Guid userGuid)
        {
            AuthenticationToken token = new AuthenticationToken
            {
                TenantGUID = tenantGuid,
                UserGUID = userGuid
            };

            token.Token = GenerateSecurityTokenString(token);
            token.Random = null;
            return token;
        }

        /// <summary>
        /// Read a token's details.
        /// </summary>
        /// <param name="token">Authentication token string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication token.</returns>
        internal async Task<AuthenticationToken> ReadToken(string token, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
            AuthenticationToken authToken = ParseSecurityTokenString(token);

            if (authToken.TenantGUID != null && authToken.UserGUID != null)
            {
                authToken.Tenant = await _Repo.Tenant.ReadByGuid(authToken.TenantGUID.Value, cancellationToken).ConfigureAwait(false);
                authToken.User = await _Repo.User.ReadByGuid(authToken.TenantGUID.Value, authToken.UserGUID.Value, cancellationToken).ConfigureAwait(false);

                if (authToken.User != null) authToken.User = UserMaster.Redact(_Serializer, authToken.User);
            }

            authToken.Random = null;
            return authToken;
        }

        /// <summary>
        /// Authorize a request against an already-classified scope/resource pair.
        /// </summary>
        /// <param name="req">Request context.</param>
        /// <param name="requiredScope">Required scope.</param>
        /// <param name="resourceType">Resource type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authorization decision.</returns>
        internal async Task<AuthorizationDecision> AuthorizeRequestScope(
            RequestContext req,
            string requiredScope,
            AuthorizationResourceTypeEnum resourceType,
            CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            return await _Authorization.EvaluateRequestAccess(req, requiredScope, resourceType, token).ConfigureAwait(false);
        }


        #endregion

        #region Private-Methods

        private async Task Authenticate(RequestContext req, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrEmpty(req.Authentication.Email)
                && !String.IsNullOrEmpty(req.Authentication.Password)
                && req.Authentication.TenantGUID != null)
            {
                #region Credential-Authentication

                req.Authentication.User = await _Repo.User.ReadByEmail(req.Authentication.TenantGUID.Value, req.Authentication.Email, cancellationToken).ConfigureAwait(false);
                if (req.Authentication.User == null)
                {
                    _Logging.Warn(_Header + "user with email " + req.Authentication.Email + " not found");
                    req.Authentication.Result = AuthenticationResultEnum.NotFound;
                    return;
                }
                else
                {
                    req.Authentication.UserGUID = req.Authentication.User.GUID;
                    req.Authentication.TenantGUID = req.Authentication.User.TenantGUID;
                }

                if (!req.Authentication.User.Active)
                {
                    _Logging.Warn(_Header + "user " + req.Authentication.UserGUID + " is inactive");
                    req.Authentication.Result = AuthenticationResultEnum.Inactive;
                    return;
                }

                if (!req.Authentication.Password.Equals(req.Authentication.User.Password))
                {
                    _Logging.Warn(_Header + "invalid password supplied for user " + req.Authentication.UserGUID);
                    req.Authentication.Result = AuthenticationResultEnum.Invalid;
                    return;
                }

                req.Authentication.Tenant = await _Repo.Tenant.ReadByGuid(req.Authentication.TenantGUID.Value, cancellationToken).ConfigureAwait(false);
                if (req.Authentication.Tenant == null)
                {
                    _Logging.Warn(_Header + "tenant " + req.Authentication.TenantGUID + " referenced by user " + req.Authentication.UserGUID + " not found");
                    req.Authentication.Result = AuthenticationResultEnum.NotFound;
                    return;
                }
                else
                {
                    req.Authentication.TenantGUID = req.Authentication.Tenant.GUID;
                }

                if (!req.Authentication.Tenant.Active)
                {
                    _Logging.Warn(_Header + "tenant " + req.Authentication.TenantGUID + " referenced by user " + req.Authentication.UserGUID + " is inactive");
                    req.Authentication.Result = AuthenticationResultEnum.Inactive;
                    return;
                }

                req.Authentication.Result = AuthenticationResultEnum.Success;
                return;

                #endregion
            }
            else if (!String.IsNullOrEmpty(req.Authentication.BearerToken))
            {
                #region Bearer-Token-Authentication

                if (req.Authentication.BearerToken.Equals(_Settings.LiteGraph.AdminBearerToken))
                {
                    #region LiteGraph-Admin

                    req.Authentication.IsAdmin = true;
                    req.Authentication.Result = AuthenticationResultEnum.Success;
                    return;

                    #endregion
                }
                else
                {
                    #region User

                    req.Authentication.Credential = await _Repo.Credential.ReadByBearerToken(req.Authentication.BearerToken, cancellationToken).ConfigureAwait(false);
                    if (req.Authentication.Credential == null)
                    {
                        _Logging.Warn(_Header + "unable to find bearer token " + OperationalLogRedactor.RedactValue(req.Authentication.BearerToken));
                        req.Authentication.Result = AuthenticationResultEnum.NotFound;
                        return;
                    }
                    else
                    {
                        req.Authentication.CredentialGUID = req.Authentication.Credential.GUID;
                    }

                    if (!req.Authentication.Credential.Active)
                    {
                        _Logging.Warn(_Header + "credential " + req.Authentication.Credential.GUID + " is inactive");
                        req.Authentication.Result = AuthenticationResultEnum.Inactive;
                        return;
                    }

                    req.Authentication.Tenant = await _Repo.Tenant.ReadByGuid(req.Authentication.Credential.TenantGUID, cancellationToken).ConfigureAwait(false);
                    if (req.Authentication.Tenant == null)
                    {
                        _Logging.Warn(_Header + "tenant " + req.Authentication.Credential.TenantGUID + " referenced in credential " + req.Authentication.Credential.GUID + " not found");
                        req.Authentication.Result = AuthenticationResultEnum.NotFound;
                        return;
                    }
                    else
                    {
                        req.Authentication.TenantGUID = req.Authentication.Tenant.GUID;
                    }

                    if (!req.Authentication.Tenant.Active)
                    {
                        _Logging.Warn(_Header + "tenant " + req.Authentication.Credential.TenantGUID + " referenced in credential " + req.Authentication.Credential.GUID + " is inactive");
                        req.Authentication.Result = AuthenticationResultEnum.Inactive;
                        return;
                    }

                    req.Authentication.User = await _Repo.User.ReadByGuid(req.Authentication.Credential.TenantGUID, req.Authentication.Credential.UserGUID, cancellationToken).ConfigureAwait(false);
                    if (req.Authentication.User == null)
                    {
                        _Logging.Warn(_Header + "user " + req.Authentication.Credential.UserGUID + " referenced in credential " + req.Authentication.Credential.GUID + " not found");
                        req.Authentication.Result = AuthenticationResultEnum.NotFound;
                        return;
                    }
                    else
                    {
                        req.Authentication.UserGUID = req.Authentication.User.GUID;
                    }

                    if (!req.Authentication.User.Active)
                    {
                        _Logging.Warn(_Header + "user " + req.Authentication.Credential.UserGUID + " referenced in credential " + req.Authentication.Credential.GUID + " is inactive");
                        req.Authentication.Result = AuthenticationResultEnum.Inactive;
                        return;
                    }

                    req.Authentication.Result = AuthenticationResultEnum.Success;
                    return;

                    #endregion
                }

                #endregion
            }
            else if (!String.IsNullOrEmpty(req.Authentication.SecurityToken))
            {
                #region Security-Token-Authentication

                AuthenticationToken token = null;

                try
                {
                    token = ParseSecurityTokenString(req.Authentication.SecurityToken);
                }
                catch (Exception)
                {
                    _Logging.Warn(_Header + "malformed security token received");
                    req.Authentication.Result = AuthenticationResultEnum.Invalid;
                    return;
                }

                if (token.IsExpired)
                {
                    _Logging.Warn(_Header + "expired security token received");
                    req.Authentication.Result = AuthenticationResultEnum.Invalid;
                    return;
                }

                req.Authentication.User = await _Repo.User.ReadByGuid(token.TenantGUID.Value, token.UserGUID.Value, cancellationToken).ConfigureAwait(false);
                if (req.Authentication.User == null)
                {
                    _Logging.Warn(_Header + "user " + token.UserGUID + " not found");
                    req.Authentication.Result = AuthenticationResultEnum.NotFound;
                    return;
                }
                else
                {
                    req.Authentication.UserGUID = req.Authentication.User.GUID;
                }

                if (!req.Authentication.User.Active)
                {
                    _Logging.Warn(_Header + "user " + token.UserGUID + " is inactive");
                    req.Authentication.Result = AuthenticationResultEnum.Inactive;
                    return;
                }

                req.Authentication.Tenant = await _Repo.Tenant.ReadByGuid(req.Authentication.User.TenantGUID, cancellationToken).ConfigureAwait(false);
                if (req.Authentication.Tenant == null)
                {
                    _Logging.Warn(_Header + "tenant " + req.Authentication.User.TenantGUID + " not found");
                    req.Authentication.Result = AuthenticationResultEnum.NotFound;
                    return;
                }
                else
                {
                    req.Authentication.TenantGUID = req.Authentication.Tenant.GUID;
                }

                if (!req.Authentication.Tenant.Active)
                {
                    _Logging.Warn(_Header + "tenant " + req.Authentication.Tenant.GUID + " is inactive");
                    req.Authentication.Result = AuthenticationResultEnum.Inactive;
                    return;
                }

                req.Authentication.Result = AuthenticationResultEnum.Success;
                return;

                #endregion
            }
            else
            {
                _Logging.Warn(_Header + "no authentication material supplied from " + req.Http.Request.Source.IpAddress);
                req.Authentication.Result = AuthenticationResultEnum.NotFound;
                return;
            }
        }

        private string GenerateSecurityTokenString(AuthenticationToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            string json = _Serializer.SerializeJson(token, false);
            byte[] clear = Encoding.UTF8.GetBytes(json);
            byte[] cipher = Encrypt(clear);
            return Convert.ToBase64String(cipher);
        }

        private AuthenticationToken ParseSecurityTokenString(string token)
        {
            if (String.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            byte[] cipher = Convert.FromBase64String(token);
            byte[] clear = Decrypt(cipher);
            string json = Encoding.UTF8.GetString(clear);
            return _Serializer.DeserializeJson<AuthenticationToken>(json);
        }

        private byte[] Encrypt(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromHexString(_Settings.Encryption.Key);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = Convert.FromHexString(_Settings.Encryption.Iv);

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(data, 0, data.Length);
                            csEncrypt.FlushFinalBlock();
                        }

                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        private byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                throw new ArgumentNullException(nameof(encryptedData));

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromHexString(_Settings.Encryption.Key);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = Convert.FromHexString(_Settings.Encryption.Iv);

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    using (MemoryStream msDecrypt = new MemoryStream())
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(
                            new MemoryStream(encryptedData),
                            decryptor,
                            CryptoStreamMode.Read))
                        {
                            byte[] buffer = new byte[1024];
                            int count;
                            while ((count = csDecrypt.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                msDecrypt.Write(buffer, 0, count);
                            }
                        }

                        return msDecrypt.ToArray();
                    }
                }
            }
        }

        #endregion
    }
}
