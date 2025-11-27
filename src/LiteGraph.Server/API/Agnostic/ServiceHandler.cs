namespace LiteGraph.Server.API.Agnostic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using SyslogLogging;

    internal class ServiceHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Internal-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[ServiceHandler] ";
        static string _Hostname = Dns.GetHostName();
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private LiteGraphClient _LiteGraph = null;
        private Serializer _Serializer = null;
        private AuthenticationService _Authentication = null;

        #endregion

        #region Constructors-and-Factories

        internal ServiceHandler(
            Settings settings,
            LoggingModule logging,
            LiteGraphClient litegraph,
            Serializer serializer,
            AuthenticationService auth)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = litegraph ?? throw new ArgumentNullException(nameof(litegraph));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Authentication = auth ?? throw new ArgumentNullException(nameof(auth));

            _Logging.Debug(_Header + "initialized service handler");
        }

        #endregion

        #region Internal-Methods

        #region Admin-Routes

        internal async Task<ResponseContext> BackupExecute(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.BackupRequest == null) throw new ArgumentNullException(nameof(req.BackupRequest));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            await _LiteGraph.Admin.Backup(req.BackupRequest.Filename, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> BackupRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.BackupFilename)) throw new ArgumentNullException(nameof(req.BackupFilename));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            BackupFile data = await _LiteGraph.Admin.BackupRead(req.BackupFilename, token).ConfigureAwait(false);
            return new ResponseContext(req, data);
        }

        internal async Task<ResponseContext> BackupExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.BackupFilename)) throw new ArgumentNullException(nameof(req.BackupFilename));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            bool exists = await _LiteGraph.Admin.BackupExists(req.BackupFilename, token).ConfigureAwait(false);
            if (exists) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "The specified backup file was not found.");
        }

        internal async Task<ResponseContext> BackupReadAll(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<BackupFile> files = new List<BackupFile>();
            await foreach (BackupFile backup in _LiteGraph.Admin.BackupReadAll(token).WithCancellation(token).ConfigureAwait(false))
            {
                files.Add(backup);
            }

            return new ResponseContext(req, files);
        }

        internal async Task<ResponseContext> BackupEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<BackupFile> er = await _LiteGraph.Admin.BackupEnumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> BackupDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.BackupFilename)) throw new ArgumentNullException(nameof(req.BackupFilename));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            await _LiteGraph.Admin.DeleteBackup(req.BackupFilename, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> FlushDatabase(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            await Task.Run(() => _LiteGraph.Flush(), token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Tenant-Routes

        internal async Task<ResponseContext> TenantCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tenant == null) throw new ArgumentNullException(nameof(req.Tenant));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            TenantMetadata obj = await _LiteGraph.Tenant.Create(req.Tenant, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<TenantMetadata> objs = new List<TenantMetadata>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (TenantMetadata tenant in _LiteGraph.Tenant.ReadMany(req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(tenant);
                }
            }
            else
            {
                await foreach (TenantMetadata tenant in _LiteGraph.Tenant.ReadByGuids(req.GUIDs, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(tenant);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TenantEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<TenantMetadata> er = await _LiteGraph.Tenant.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> TenantRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            TenantMetadata obj = await _LiteGraph.Tenant.ReadByGuid(req.TenantGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TenantStatistics(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin && req.TenantGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            object obj = null;
            if (req.TenantGUID == null) obj = await _LiteGraph.Tenant.GetStatistics(token).ConfigureAwait(false);
            else obj = await _LiteGraph.Tenant.GetStatistics(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (await _LiteGraph.Tenant.ExistsByGuid(req.TenantGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TenantUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tenant == null) throw new ArgumentNullException(nameof(req.Tenant));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Tenant.GUID = req.TenantGUID.Value;
            TenantMetadata obj = await _LiteGraph.Tenant.Update(req.Tenant, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.Tenant.DeleteByGuid(req.TenantGUID.Value, req.Force, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region User-Routes

        internal async Task<ResponseContext> UserCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.User == null) throw new ArgumentNullException(nameof(req.User));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.User.TenantGUID = req.TenantGUID.Value;
            UserMaster obj = await _LiteGraph.User.Create(req.User, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> UserReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<UserMaster> objs = new List<UserMaster>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (UserMaster user in _LiteGraph.User.ReadMany(req.TenantGUID.Value, null, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(user);
                }
            }
            else
            {
                await foreach (UserMaster user in _LiteGraph.User.ReadByGuids(req.TenantGUID.Value, req.GUIDs, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(user);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> UserEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<UserMaster> er = await _LiteGraph.User.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> UserRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            UserMaster obj = await _LiteGraph.User.ReadByGuid(req.TenantGUID.Value, req.UserGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> UserExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (await _LiteGraph.User.ExistsByGuid(req.TenantGUID.Value, req.UserGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> UserUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.User == null) throw new ArgumentNullException(nameof(req.User));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.User.TenantGUID = req.TenantGUID.Value;
            UserMaster obj = await _LiteGraph.User.Update(req.User, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> UserDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.User.DeleteByGuid(req.TenantGUID.Value, req.UserGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> UserTenants(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<TenantMetadata> tenants = await _LiteGraph.User.ReadTenantsByEmail(req.Authentication.Email, token).ConfigureAwait(false);
            if (tenants != null && tenants.Count > 0) return new ResponseContext(req, tenants);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        #endregion

        #region Credential-Routes

        internal async Task<ResponseContext> CredentialCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Credential == null) throw new ArgumentNullException(nameof(req.Credential));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            Credential obj = await _LiteGraph.Credential.Create(req.Credential, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> CredentialReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<Credential> objs = new List<Credential>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (Credential credential in _LiteGraph.Credential.ReadMany(req.TenantGUID.Value, null, null, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(credential);
                }
            }
            else
            {
                await foreach (Credential credential in _LiteGraph.Credential.ReadByGuids(req.TenantGUID.Value, req.GUIDs, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(credential);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> CredentialEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<Credential> er = await _LiteGraph.Credential.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> CredentialRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            Credential obj = await _LiteGraph.Credential.ReadByGuid(req.TenantGUID.Value, req.CredentialGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (await _LiteGraph.Credential.ExistsByGuid(req.TenantGUID.Value, req.CredentialGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Credential == null) throw new ArgumentNullException(nameof(req.Credential));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            Credential obj = await _LiteGraph.Credential.Update(req.Credential, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> CredentialDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.Credential.DeleteByGuid(req.TenantGUID.Value, req.CredentialGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> CredentialReadByBearerToken(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (String.IsNullOrEmpty(req.BearerToken)) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest);
            Credential obj = await _LiteGraph.Credential.ReadByBearerToken(req.BearerToken, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.Credential.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> CredentialDeleteByUser(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.Credential.DeleteByUser(req.TenantGUID.Value, req.UserGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Label-Routes

        internal async Task<ResponseContext> LabelCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Label == null) throw new ArgumentNullException(nameof(req.Label));
            req.Label.TenantGUID = req.TenantGUID.Value;
            LabelMetadata obj = await _LiteGraph.Label.Create(req.Label, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelCreateMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Labels == null || req.Labels.Count < 1) throw new ArgumentNullException(nameof(req.Labels));
            foreach (LabelMetadata label in req.Labels) label.TenantGUID = req.TenantGUID.Value;
            List<LabelMetadata> obj = await _LiteGraph.Label.CreateMany(req.TenantGUID.Value, req.Labels, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<LabelMetadata> er = await _LiteGraph.Label.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> LabelReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<LabelMetadata> objs = null;

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                objs = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _LiteGraph.Label.ReadMany(
                    req.TenantGUID.Value,
                    req.GraphGUID,
                    req.NodeGUID,
                    req.EdgeGUID,
                    null,
                    req.Order,
                    req.Skip,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(label);
                }
            }
            else
            {
                objs = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _LiteGraph.Label.ReadByGuids(req.TenantGUID.Value, req.GUIDs, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(label);
                }
            }

            if (objs == null) objs = new List<LabelMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            LabelMetadata obj = await _LiteGraph.Label.ReadByGuid(req.TenantGUID.Value, req.LabelGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> LabelExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (await _LiteGraph.Label.ExistsByGuid(req.TenantGUID.Value, req.LabelGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> LabelUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Label == null) throw new ArgumentNullException(nameof(req.Label));
            req.Label.TenantGUID = req.TenantGUID.Value;
            LabelMetadata obj = await _LiteGraph.Label.Update(req.Label, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Label.DeleteByGuid(req.TenantGUID.Value, req.LabelGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelDeleteMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Label.DeleteMany(req.TenantGUID.Value, req.GUIDs, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelReadAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<LabelMetadata> objs = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _LiteGraph.Label.ReadAllInTenant(
                req.TenantGUID.Value,
                req.Order,
                req.Skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(label);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelReadAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<LabelMetadata> objs = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _LiteGraph.Label.ReadAllInGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(label);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelReadManyGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<LabelMetadata> objs = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _LiteGraph.Label.ReadManyGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(label);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelReadManyNode(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<LabelMetadata> objs = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _LiteGraph.Label.ReadManyNode(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.Order,
                req.Skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(label);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelReadManyEdge(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<LabelMetadata> objs = new List<LabelMetadata>();
            await foreach (LabelMetadata label in _LiteGraph.Label.ReadManyEdge(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.EdgeGUID.Value,
                req.Order,
                req.Skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(label);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Label.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelDeleteAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Label.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelDeleteGraphLabels(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Label.DeleteGraphLabels(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelDeleteNodeLabels(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Label.DeleteNodeLabels(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelDeleteEdgeLabels(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Label.DeleteEdgeLabels(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Tag-Routes

        internal async Task<ResponseContext> TagCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tag == null) throw new ArgumentNullException(nameof(req.Tag));
            req.Tag.TenantGUID = req.TenantGUID.Value;
            TagMetadata obj = await _LiteGraph.Tag.Create(req.Tag, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagCreateMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tags == null || req.Tags.Count < 1) throw new ArgumentNullException(nameof(req.Tags));
            foreach (TagMetadata tag in req.Tags) tag.TenantGUID = req.TenantGUID.Value;
            List<TagMetadata> obj = await _LiteGraph.Tag.CreateMany(req.TenantGUID.Value, req.Tags, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<TagMetadata> er = await _LiteGraph.Tag.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> TagReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = new List<TagMetadata>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (TagMetadata tag in _LiteGraph.Tag.ReadMany(req.TenantGUID.Value, null, null, null, null, null, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(tag);
                }
            }
            else
            {
                await foreach (TagMetadata tag in _LiteGraph.Tag.ReadByGuids(req.TenantGUID.Value, req.GUIDs, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(tag);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            TagMetadata obj = await _LiteGraph.Tag.ReadByGuid(req.TenantGUID.Value, req.TagGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TagExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (await _LiteGraph.Tag.ExistsByGuid(req.TenantGUID.Value, req.TagGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TagUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tag == null) throw new ArgumentNullException(nameof(req.Tag));
            req.Tag.TenantGUID = req.TenantGUID.Value;
            TagMetadata obj = await _LiteGraph.Tag.Update(req.Tag, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteByGuid(req.TenantGUID.Value, req.TagGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagDeleteMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteMany(req.TenantGUID.Value, req.GUIDs, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagReadAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = new List<TagMetadata>();

            await foreach (TagMetadata tag in _LiteGraph.Tag.ReadAllInTenant(
                req.TenantGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(tag);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagReadAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = new List<TagMetadata>();

            await foreach (TagMetadata tag in _LiteGraph.Tag.ReadAllInGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(tag);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagReadManyGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = new List<TagMetadata>();

            await foreach (TagMetadata tag in _LiteGraph.Tag.ReadManyGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(tag);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagReadManyNode(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = new List<TagMetadata>();

            await foreach (TagMetadata tag in _LiteGraph.Tag.ReadManyNode(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(tag);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagReadManyEdge(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = new List<TagMetadata>();

            await foreach (TagMetadata tag in _LiteGraph.Tag.ReadManyEdge(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.EdgeGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(tag);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagDeleteAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagDeleteGraphTags(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteGraphTags(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagDeleteNodeTags(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteNodeTags(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagDeleteEdgeTags(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Tag.DeleteEdgeTags(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Vector-Routes

        internal async Task<ResponseContext> VectorCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vector == null) throw new ArgumentNullException(nameof(req.Vector));
            req.Vector.TenantGUID = req.TenantGUID.Value;
            VectorMetadata obj = await _LiteGraph.Vector.Create(req.Vector, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorCreateMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vectors == null || req.Vectors.Count < 1) throw new ArgumentNullException(nameof(req.Vectors));
            List<VectorMetadata> obj = await _LiteGraph.Vector.CreateMany(req.TenantGUID.Value, req.Vectors, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<VectorMetadata> er = await _LiteGraph.Vector.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> VectorReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = new List<VectorMetadata>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadMany(req.TenantGUID.Value, null, null, null, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(vector);
                }
            }
            else
            {
                await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadByGuids(req.TenantGUID.Value, req.GUIDs, token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(vector);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            VectorMetadata obj = await _LiteGraph.Vector.ReadByGuid(req.TenantGUID.Value, req.VectorGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> VectorExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (await _LiteGraph.Vector.ExistsByGuid(req.TenantGUID.Value, req.VectorGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> VectorUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vector == null) throw new ArgumentNullException(nameof(req.Vector));
            req.Vector.TenantGUID = req.TenantGUID.Value;
            VectorMetadata obj = await _LiteGraph.Vector.Update(req.Vector, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteByGuid(req.TenantGUID.Value, req.VectorGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorDeleteMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteMany(req.TenantGUID.Value, req.GUIDs, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorSearch(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.VectorSearchRequest == null) throw new ArgumentNullException(nameof(req.VectorSearchRequest));
            if (req.GraphGUID != null && !await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<VectorSearchResult> results = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _LiteGraph.Vector.Search(req.VectorSearchRequest, token).WithCancellation(token).ConfigureAwait(false))
            {
                results.Add(result);
            }
            return new ResponseContext(req, results);
        }

        internal async Task<ResponseContext> VectorReadAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = new List<VectorMetadata>();

            await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadAllInTenant(
                req.TenantGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(vector);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorReadAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = new List<VectorMetadata>();

            await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadAllInGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(vector);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorReadManyGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = new List<VectorMetadata>();

            await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadManyGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(vector);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorReadManyNode(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = new List<VectorMetadata>();

            await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadManyNode(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(vector);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorReadManyEdge(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = new List<VectorMetadata>();

            await foreach (VectorMetadata vector in _LiteGraph.Vector.ReadManyEdge(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.EdgeGUID.Value,
                req.Order,
                req.Skip,
                token).ConfigureAwait(false))
            {
                objs.Add(vector);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorDeleteAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorDeleteGraphVectors(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteGraphVectors(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorDeleteNodeVectors(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteNodeVectors(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorDeleteEdgeVectors(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Vector.DeleteEdgeVectors(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Graph-Routes

        internal async Task<ResponseContext> GraphCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Graph == null) throw new ArgumentNullException(nameof(req.Graph));
            req.Graph.TenantGUID = req.TenantGUID.Value;

            Graph graph = await _LiteGraph.Graph.Create(req.Graph, token).ConfigureAwait(false);
            return new ResponseContext(req, graph);
        }

        internal async Task<ResponseContext> GraphReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<Graph> objs = new List<Graph>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (Graph graph in _LiteGraph.Graph.ReadMany(
                    req.TenantGUID.Value,
                    null,
                    null,
                    null,
                    null,
                    req.Order,
                    req.Skip,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(graph);
                }
            }
            else
            {
                await foreach (Graph graph in _LiteGraph.Graph.ReadByGuids(
                    req.TenantGUID.Value,
                    req.GUIDs,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(graph);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> GraphEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<Graph> er = await _LiteGraph.Graph.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> GraphExistence(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ExistenceRequest == null) throw new ArgumentNullException(nameof(req.ExistenceRequest));
            if (!req.ExistenceRequest.ContainsExistenceRequest()) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "No valid existence filters are present in the request.");
            ExistenceResult resp = await _LiteGraph.Batch.Existence(req.TenantGUID.Value, req.GraphGUID.Value, req.ExistenceRequest, token).ConfigureAwait(false);
            return new ResponseContext(req, resp);
        }

        internal async Task<ResponseContext> GraphSearch(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.ExistenceRequest));
            SearchResult sresp = new SearchResult();
            List<Graph> graphs = new List<Graph>();
            await foreach (Graph graph in _LiteGraph.Graph.ReadMany(
                req.TenantGUID.Value,
                req.SearchRequest.Name,
                req.SearchRequest.Labels,
                req.SearchRequest.Tags,
                req.SearchRequest.Expr,
                req.SearchRequest.Ordering,
                req.SearchRequest.Skip,
                req.SearchRequest.IncludeData,
                req.SearchRequest.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                graphs.Add(graph);
            }
            sresp.Graphs = graphs;
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> GraphReadFirst(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.ExistenceRequest));
            Graph graph = await _LiteGraph.Graph.ReadFirst(
                req.TenantGUID.Value,
                req.SearchRequest.Name,
                req.SearchRequest.Labels,
                req.SearchRequest.Tags,
                req.SearchRequest.Expr,
                req.SearchRequest.Ordering,
                req.SearchRequest.IncludeData,
                req.SearchRequest.IncludeSubordinates,
                token).ConfigureAwait(false);

            if (graph != null) return new ResponseContext(req, graph);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "No matching records found.");
        }

        internal async Task<ResponseContext> GraphRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Graph graph = await _LiteGraph.Graph.ReadByGuid(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.IncludeData,
                req.IncludeSubordinates,
                token).ConfigureAwait(false);
            if (graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, graph);
        }

        internal async Task<ResponseContext> GraphStatistics(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            object obj = null;
            if (req.GraphGUID == null) obj = await _LiteGraph.Graph.GetStatistics(req.TenantGUID.Value, token).ConfigureAwait(false);
            else obj = await _LiteGraph.Graph.GetStatistics(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> GraphExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Graph graph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, false, false, token).ConfigureAwait(false);
            if (graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req);
        }

        internal async Task<ResponseContext> GraphUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Graph == null) throw new ArgumentNullException(nameof(req.Graph));
            req.Graph.TenantGUID = req.TenantGUID.Value;
            req.Graph = await _LiteGraph.Graph.Update(req.Graph, token).ConfigureAwait(false);
            if (req.Graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, req.Graph);
        }

        internal async Task<ResponseContext> GraphSubgraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.TenantGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Tenant GUID is required.");
            if (req.GraphGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Graph GUID is required.");
            if (req.NodeGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Node GUID is required.");

            SearchResult result = await _LiteGraph.Graph.GetSubgraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.MaxDepth,
                req.MaxNodes,
                req.MaxEdges,
                req.IncludeData,
                req.IncludeSubordinates,
                token).ConfigureAwait(false);

            return new ResponseContext(req, result);
        }

        internal async Task<ResponseContext> GraphSubgraphStatistics(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.TenantGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Tenant GUID is required.");
            if (req.GraphGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Graph GUID is required.");
            if (req.NodeGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Node GUID is required.");

            GraphStatistics stats = await _LiteGraph.Graph.GetSubgraphStatistics(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.MaxDepth,
                req.MaxNodes,
                req.MaxEdges,
                token).ConfigureAwait(false);

            return new ResponseContext(req, stats);
        }

        internal async Task<ResponseContext> GraphDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                await _LiteGraph.Graph.DeleteByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.Force, token).ConfigureAwait(false);
                return new ResponseContext(req);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
            catch (ArgumentException e)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, e.Message);
            }
        }

        internal async Task<ResponseContext> GraphGexfExport(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            try
            {
                string xml = await _LiteGraph.RenderGraphAsGexf(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).ConfigureAwait(false);

                return new ResponseContext(req, xml);
            }
            catch (ArgumentException)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "GEXF export exception:" + Environment.NewLine + e.ToString());
                return ResponseContext.FromError(req, ApiErrorEnum.InternalError);
            }
        }

        internal async Task<ResponseContext> GraphReadAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<Graph> objs = new List<Graph>();

            await foreach (Graph graph in _LiteGraph.Graph.ReadAllInTenant(
                req.TenantGUID.Value,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).ConfigureAwait(false))
            {
                objs.Add(graph);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> GraphDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Graph.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Node-Routes

        internal async Task<ResponseContext> NodeCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Node == null) throw new ArgumentNullException(nameof(req.Node));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node = await _LiteGraph.Node.Create(req.Node, token).ConfigureAwait(false);
            return new ResponseContext(req, req.Node);
        }

        internal async Task<ResponseContext> NodeCreateMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Nodes == null) throw new ArgumentNullException(nameof(req.Nodes));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                req.Nodes = await _LiteGraph.Node.CreateMany(req.TenantGUID.Value, req.GraphGUID.Value, req.Nodes, token).ConfigureAwait(false);
                return new ResponseContext(req, req.Nodes);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
        }

        internal async Task<ResponseContext> NodeReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<Node> objs = new List<Node>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (Node node in _LiteGraph.Node.ReadMany(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    null,
                    null,
                    null,
                    null,
                    req.Order,
                    req.Skip,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(node);
                }
            }
            else
            {
                await foreach (Node node in _LiteGraph.Node.ReadByGuids(
                    req.TenantGUID.Value,
                    req.GUIDs,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(node);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> NodeEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            req.EnumerationQuery.GraphGUID = req.GraphGUID;
            EnumerationResult<Node> er = await _LiteGraph.Node.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> NodeSearch(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.SearchRequest));

            SearchResult sresp = new SearchResult();
            sresp.Nodes = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadMany(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.SearchRequest.Name,
                req.SearchRequest.Labels,
                req.SearchRequest.Tags,
                req.SearchRequest.Expr,
                req.SearchRequest.Ordering,
                req.SearchRequest.Skip,
                req.SearchRequest.IncludeData,
                req.SearchRequest.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                sresp.Nodes.Add(node);
            }
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> NodeReadFirst(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.SearchRequest));

            Node node = await _LiteGraph.Node.ReadFirst(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.SearchRequest.Name,
                req.SearchRequest.Labels,
                req.SearchRequest.Tags,
                req.SearchRequest.Expr,
                req.SearchRequest.Ordering,
                req.SearchRequest.IncludeData,
                req.SearchRequest.IncludeSubordinates,
                token).ConfigureAwait(false);

            if (node != null) return new ResponseContext(req, node);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "No matching records found.");
        }

        internal async Task<ResponseContext> NodeRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            Node node = await _LiteGraph.Node.ReadByGuid(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.IncludeData,
                req.IncludeSubordinates,
                token).ConfigureAwait(false);

            if (node == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, node);
        }

        internal async Task<ResponseContext> NodeExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            bool exists = await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false);
            if (exists) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> NodeUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Node == null) throw new ArgumentNullException(nameof(req.Node));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node = await _LiteGraph.Node.Update(req.Node, token).ConfigureAwait(false);
            if (req.Node != null) return new ResponseContext(req, req.Node);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> NodeDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Node.DeleteByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeDeleteAll(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Node.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeDeleteMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Node.DeleteMany(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeReadAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> objs = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadAllInTenant(
                req.TenantGUID.Value,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(node);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> NodeReadAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> objs = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadAllInGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(node);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> NodeReadMostConnected(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> objs = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadMostConnected(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                null,
                null,
                null,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(node);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> NodeReadLeastConnected(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> objs = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadLeastConnected(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                null,
                null,
                null,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(node);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> NodeDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Node.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Edge-Routes

        internal async Task<ResponseContext> EdgeCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edge == null) throw new ArgumentNullException(nameof(req.Edge));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge = await _LiteGraph.Edge.Create(req.Edge, token).ConfigureAwait(false);
            return new ResponseContext(req, req.Edge);
        }

        internal async Task<ResponseContext> EdgeCreateMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edges == null) throw new ArgumentNullException(nameof(req.Edges));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                req.Edges = await _LiteGraph.Edge.CreateMany(req.TenantGUID.Value, req.GraphGUID.Value, req.Edges, token).ConfigureAwait(false);
                return new ResponseContext(req, req.Edges);
            }
            catch (KeyNotFoundException knfe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, knfe.Message);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
        }

        internal async Task<ResponseContext> EdgeReadMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<Edge> objs = new List<Edge>();

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                await foreach (Edge edge in _LiteGraph.Edge.ReadMany(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    null,
                    null,
                    null,
                    null,
                    req.Order,
                    req.Skip,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(edge);
                }
            }
            else
            {
                await foreach (Edge edge in _LiteGraph.Edge.ReadByGuids(
                    req.TenantGUID.Value,
                    req.GUIDs,
                    req.IncludeData,
                    req.IncludeSubordinates,
                    token).WithCancellation(token).ConfigureAwait(false))
                {
                    objs.Add(edge);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> EdgeEnumerate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            req.EnumerationQuery.GraphGUID = req.GraphGUID;
            EnumerationResult<Edge> er = await _LiteGraph.Edge.Enumerate(req.EnumerationQuery, token).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> EdgesBetween(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            req.Edges = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadEdgesBetweenNodes(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.FromGUID.Value,
                req.ToGUID.Value,
                null,
                null,
                null,
                EnumerationOrderEnum.CreatedDescending,
                0,
                false,
                false,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                req.Edges.Add(edge);
            }
            return new ResponseContext(req, req.Edges);
        }

        internal async Task<ResponseContext> EdgeSearch(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.SearchRequest));

            SearchResult sresp = new SearchResult();
            sresp.Edges = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadMany(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.SearchRequest.Name,
                req.SearchRequest.Labels,
                req.SearchRequest.Tags,
                req.SearchRequest.Expr,
                req.SearchRequest.Ordering,
                req.SearchRequest.Skip,
                req.SearchRequest.IncludeData,
                req.SearchRequest.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                sresp.Edges.Add(edge);
            }

            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> EdgeReadFirst(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.SearchRequest));

            Edge edge = await _LiteGraph.Edge.ReadFirst(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.SearchRequest.Name,
                req.SearchRequest.Labels,
                req.SearchRequest.Tags,
                req.SearchRequest.Expr,
                req.SearchRequest.Ordering,
                req.SearchRequest.IncludeData,
                req.SearchRequest.IncludeSubordinates,
                token).ConfigureAwait(false);

            if (edge != null) return new ResponseContext(req, edge);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "No matching records found.");
        }

        internal async Task<ResponseContext> EdgeRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Edge edge = await _LiteGraph.Edge.ReadByGuid(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.EdgeGUID.Value,
                req.IncludeData,
                req.IncludeSubordinates,
                token).ConfigureAwait(false);
            if (edge == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, edge);
        }

        internal async Task<ResponseContext> EdgeExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (await _LiteGraph.Edge.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> EdgeUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edge == null) throw new ArgumentNullException(nameof(req.Edge));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge = await _LiteGraph.Edge.Update(req.Edge, token).ConfigureAwait(false);
            return new ResponseContext(req, req.Edge);
        }

        internal async Task<ResponseContext> EdgeDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Edge.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteAll(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteMany(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeReadAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Edge> objs = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadAllInTenant(
                req.TenantGUID.Value,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(edge);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> EdgeReadAllInGraph(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Edge> objs = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadAllInGraph(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(edge);
            }
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> EdgeDeleteAllInTenant(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Edge.DeleteAllInTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteNodeEdges(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteNodeEdges(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteNodeEdgesMany(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteNodeEdges(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Routes-and-Traversal

        internal async Task<ResponseContext> EdgesFromNode(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Edge> edgesFrom = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadEdgesFromNode(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                null,
                null,
                null,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                edgesFrom.Add(edge);
            }
            return new ResponseContext(req, edgesFrom);
        }

        internal async Task<ResponseContext> EdgesToNode(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Edge> edgesTo = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadEdgesToNode(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                null,
                null,
                null,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                edgesTo.Add(edge);
            }
            return new ResponseContext(req, edgesTo);
        }

        internal async Task<ResponseContext> AllEdgesToNode(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Edge> allEdges = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadNodeEdges(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                null,
                null,
                null,
                req.Order,
                req.Skip,
                req.IncludeData,
                req.IncludeSubordinates,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                allEdges.Add(edge);
            }
            return new ResponseContext(req, allEdges);
        }

        internal async Task<ResponseContext> NodeChildren(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> nodes = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadChildren(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                token: token).WithCancellation(token).ConfigureAwait(false))
            {
                nodes.Add(node);
            }
            return new ResponseContext(req, nodes);
        }

        internal async Task<ResponseContext> NodeParents(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> parents = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadParents(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                token: token).WithCancellation(token).ConfigureAwait(false))
            {
                parents.Add(node);
            }
            return new ResponseContext(req, parents);
        }

        internal async Task<ResponseContext> NodeNeighbors(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> neighbors = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadNeighbors(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.Order,
                req.Skip,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                neighbors.Add(node);
            }
            return new ResponseContext(req, neighbors);
        }

        internal async Task<ResponseContext> GetRoutes(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.RouteRequest == null) throw new ArgumentNullException(nameof(req.RouteRequest));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.RouteRequest.From, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.RouteRequest.To, token).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            RouteResponse sresp = new RouteResponse();
            List<RouteDetail> routes = new List<RouteDetail>();
            await foreach (RouteDetail route in _LiteGraph.Node.ReadRoutes(
                SearchTypeEnum.DepthFirstSearch,
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.RouteRequest.From,
                req.RouteRequest.To,
                req.RouteRequest.EdgeFilter,
                req.RouteRequest.NodeFilter,
                token).WithCancellation(token).ConfigureAwait(false))
            {
                routes.Add(route);
            }

            routes = routes.OrderBy(r => r.TotalCost).ToList();
            sresp.Routes = routes;
            sresp.Timestamp.End = DateTime.UtcNow;
            return new ResponseContext(req, sresp);
        }

        #endregion

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
