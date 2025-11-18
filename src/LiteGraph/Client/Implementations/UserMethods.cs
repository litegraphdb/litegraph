namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using RestWrapper;

    /// <summary>
    /// User methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class UserMethods : IUserMethods
    {
        #region Public-Members

        /// <summary>
        /// Timeout, in milliseconds.  Default is 600 seconds.
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

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        private int _TimeoutMs = 600 * 1000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// User methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public UserMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<UserMaster> Create(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            await _Client.ValidateTenantExists(user.TenantGUID, token).ConfigureAwait(false);
            UserMaster created = await _Repo.User.Create(user, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created user email " + created.Email + " GUID " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");

            await foreach (UserMaster user in _Repo.User.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return user;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");

            await foreach (UserMaster user in _Repo.User.ReadMany(tenantGuid, email, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return user;
            }
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving user with GUID " + guid);

            return await _Repo.User.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByEmail(Guid tenantGuid, string email, CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving user with email " + email);

            return await _Repo.User.ReadByEmail(tenantGuid, email, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TenantMetadata>> ReadTenantsByEmail(string email, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants associated with user " + email);
            return await _Repo.User.ReadTenantsByEmail(email, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<UserMaster> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");
            await foreach (UserMaster obj in _Repo.User.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<UserMaster>> Enumerate(EnumerationRequest query = null, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            return await _Repo.User.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<UserMaster> Update(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            await _Client.ValidateTenantExists(user.TenantGUID, token).ConfigureAwait(false);
            user = await _Repo.User.Update(user, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "updated user with email " + user.Email + " GUID " + user.GUID);
            return user;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            UserMaster user = await _Repo.User.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            if (user == null) return;
            await _Repo.User.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted user with email " + user.Email + " GUID " + user.GUID);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.User.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted users for tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            return await _Repo.User.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByEmail(Guid tenantGuid, string email, CancellationToken token = default)
        {
            return await _Repo.User.ExistsByEmail(tenantGuid, email, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationToken> GenerateAuthenticationToken(string email, string password, Guid tenantGuid, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            string url = _Client.Endpoint + "/v1.0/token";
            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                if (!string.IsNullOrWhiteSpace(email)) req.Headers.Add("x-email", email);
                if (!string.IsNullOrWhiteSpace(password)) req.Headers.Add("x-password", password);
                if (!string.IsNullOrWhiteSpace(tenantGuid.ToString())) req.Headers.Add("x-tenant-guid", tenantGuid.ToString());

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (_Client.Logging.ConsoleLogging) _Client.Logging.Log(SeverityEnum.Debug, "response (status " + resp.StatusCode + "): " + Environment.NewLine + resp.DataAsString);

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            _Client.Logging.Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                _Client.Logging.Log(SeverityEnum.Debug, "deserializing response body");
                                return _Client.Serializer.DeserializeJson<AuthenticationToken>(resp.DataAsString);
                            }
                            else
                            {
                                _Client.Logging.Log(SeverityEnum.Debug, "empty response body, returning null");
                                return null;
                            }
                        }
                        else
                        {
                            _Client.Logging.Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        _Client.Logging.Log(SeverityEnum.Warn, "no response from " + url);
                        return null;
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<AuthenticationToken> ReadAuthenticationToken(string authToken, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(authToken)) throw new ArgumentNullException(nameof(authToken));

            string url = _Client.Endpoint + "/v1.0/token/details";
            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = TimeoutMs;
                if (!string.IsNullOrWhiteSpace(authToken)) req.Headers.Add("x-token", authToken);

                using (RestResponse resp = await req.SendAsync(token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if (_Client.Logging.ConsoleLogging) _Client.Logging.Log(SeverityEnum.Debug, "response (status " + resp.StatusCode + "): " + Environment.NewLine + resp.DataAsString);

                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            _Client.Logging.Log(SeverityEnum.Debug, "success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                _Client.Logging.Log(SeverityEnum.Debug, "deserializing response body");
                                return _Client.Serializer.DeserializeJson<AuthenticationToken>(resp.DataAsString);
                            }
                            else
                            {
                                _Client.Logging.Log(SeverityEnum.Debug, "empty response body, returning null");
                                return null;
                            }
                        }
                        else
                        {
                            _Client.Logging.Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                            return null;
                        }
                    }
                    else
                    {
                        _Client.Logging.Log(SeverityEnum.Warn, "no response from " + url);
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
