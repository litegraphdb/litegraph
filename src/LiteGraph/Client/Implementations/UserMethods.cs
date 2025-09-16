namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
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
        public UserMaster Create(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Client.ValidateTenantExists(user.TenantGUID);
            UserMaster created = _Repo.User.Create(user);
            _Client.Logging.Log(SeverityEnum.Info, "created user email " + created.Email + " GUID " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");

            foreach (UserMaster user in _Repo.User.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return user;
            }
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");

            foreach (UserMaster user in _Repo.User.ReadMany(tenantGuid, email, order, skip))
            {
                yield return user;
            }
        }

        /// <inheritdoc />
        public UserMaster ReadByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving user with GUID " + guid);

            return _Repo.User.ReadByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public UserMaster ReadByEmail(Guid tenantGuid, string email)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving user with email " + email);

            return _Repo.User.ReadByEmail(tenantGuid, email);
        }

        /// <inheritdoc />
        public List<TenantMetadata> ReadTenantsByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tenants associated with user " + email);
            return _Repo.User.ReadTenantsByEmail(email);
        }

        /// <inheritdoc />
        public IEnumerable<UserMaster> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving users");
            foreach (UserMaster obj in _Repo.User.ReadByGuids(tenantGuid, guids))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<UserMaster> Enumerate(EnumerationRequest query)
        {
            if (query == null) query = new EnumerationRequest();
            return _Repo.User.Enumerate(query);
        }

        /// <inheritdoc />
        public UserMaster Update(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Client.ValidateTenantExists(user.TenantGUID);
            user = _Repo.User.Update(user);
            _Client.Logging.Log(SeverityEnum.Debug, "updated user with email " + user.Email + " GUID " + user.GUID);
            return user;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            UserMaster user = _Repo.User.ReadByGuid(tenantGuid, guid);
            if (user == null) return;
            _Repo.User.DeleteByGuid(tenantGuid, guid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted user with email " + user.Email + " GUID " + user.GUID);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.User.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted users for tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            return _Repo.User.ExistsByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public bool ExistsByEmail(Guid tenantGuid, string email)
        {
            return _Repo.User.ExistsByEmail(tenantGuid, email);
        }

        /// <inheritdoc />
        public async Task<AuthenticationToken> CreateAuthToken(string email, string password, Guid tenantGuid, CancellationToken token = default)
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
        public async Task<AuthenticationToken> ReadTokenDetail(string authToken, CancellationToken token = default)
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
