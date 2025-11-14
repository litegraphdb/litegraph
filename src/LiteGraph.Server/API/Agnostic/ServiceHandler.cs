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

        internal async Task<ResponseContext> BackupExecute(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.BackupRequest == null) throw new ArgumentNullException(nameof(req.BackupRequest));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            await _LiteGraph.Admin.Backup(req.BackupRequest.Filename).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> BackupRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.BackupFilename)) throw new ArgumentNullException(nameof(req.BackupFilename));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            BackupFile data = await _LiteGraph.Admin.BackupRead(req.BackupFilename).ConfigureAwait(false);
            return new ResponseContext(req, data);
        }

        internal async Task<ResponseContext> BackupExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.BackupFilename)) throw new ArgumentNullException(nameof(req.BackupFilename));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            bool exists = await _LiteGraph.Admin.BackupExists(req.BackupFilename).ConfigureAwait(false);
            if (exists) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "The specified backup file was not found.");
        }

        internal async Task<ResponseContext> BackupReadAll(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            IEnumerable<BackupFile> backups = await _LiteGraph.Admin.BackupReadAll().ConfigureAwait(false);
            List<BackupFile> files = backups != null ? backups.ToList() : new List<BackupFile>();
            return new ResponseContext(req, files);
        }

        internal async Task<ResponseContext> BackupEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<BackupFile> er = await _LiteGraph.Admin.BackupEnumerate(req.EnumerationQuery).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> BackupDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.BackupFilename)) throw new ArgumentNullException(nameof(req.BackupFilename));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            await _LiteGraph.Admin.DeleteBackup(req.BackupFilename).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> FlushDatabase(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            _LiteGraph.Flush();
            return new ResponseContext(req);
        }

        #endregion

        #region Tenant-Routes

        internal async Task<ResponseContext> TenantCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tenant == null) throw new ArgumentNullException(nameof(req.Tenant));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            TenantMetadata obj = _LiteGraph.Tenant.Create(req.Tenant);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<TenantMetadata> objs = null;

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                objs = _LiteGraph.Tenant.ReadMany(req.Order, req.Skip).ToList();
            }
            else
            {
                objs = _LiteGraph.Tenant.ReadByGuids(req.GUIDs).ToList();
            }

            if (objs == null) objs = new List<TenantMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TenantEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<TenantMetadata> er = _LiteGraph.Tenant.Enumerate(req.EnumerationQuery);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> TenantRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            TenantMetadata obj = _LiteGraph.Tenant.ReadByGuid(req.TenantGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TenantStatistics(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin && req.TenantGUID == null) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            object obj = null;
            if (req.TenantGUID == null) obj = _LiteGraph.Tenant.GetStatistics();
            else obj = _LiteGraph.Tenant.GetStatistics(req.TenantGUID.Value);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (_LiteGraph.Tenant.ExistsByGuid(req.TenantGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TenantUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tenant == null) throw new ArgumentNullException(nameof(req.Tenant));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Tenant.GUID = req.TenantGUID.Value;
            TenantMetadata obj = _LiteGraph.Tenant.Update(req.Tenant);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            _LiteGraph.Tenant.DeleteByGuid(req.TenantGUID.Value, req.Force);
            return new ResponseContext(req);
        }

        #endregion

        #region User-Routes

        internal async Task<ResponseContext> UserCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.User == null) throw new ArgumentNullException(nameof(req.User));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.User.TenantGUID = req.TenantGUID.Value;
            UserMaster obj = _LiteGraph.User.Create(req.User);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> UserReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<UserMaster> objs = null;

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                objs = _LiteGraph.User.ReadMany(req.TenantGUID.Value, null, req.Order, req.Skip).ToList();
            }
            else
            {
                objs = _LiteGraph.User.ReadByGuids(req.TenantGUID.Value, req.GUIDs).ToList();
            }

            if (objs == null) objs = new List<UserMaster>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> UserEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<UserMaster> er = _LiteGraph.User.Enumerate(req.EnumerationQuery);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> UserRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            UserMaster obj = _LiteGraph.User.ReadByGuid(req.TenantGUID.Value, req.UserGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> UserExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (_LiteGraph.User.ExistsByGuid(req.TenantGUID.Value, req.UserGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> UserUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.User == null) throw new ArgumentNullException(nameof(req.User));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.User.TenantGUID = req.TenantGUID.Value;
            UserMaster obj = _LiteGraph.User.Update(req.User);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> UserDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            _LiteGraph.User.DeleteByGuid(req.TenantGUID.Value, req.UserGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> UserTenants(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<TenantMetadata> tenants = _LiteGraph.User.ReadTenantsByEmail(req.Authentication.Email);
            if (tenants != null && tenants.Count > 0) return new ResponseContext(req, tenants);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        #endregion

        #region Credential-Routes

        internal async Task<ResponseContext> CredentialCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Credential == null) throw new ArgumentNullException(nameof(req.Credential));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            Credential obj = await _LiteGraph.Credential.Create(req.Credential).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> CredentialReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<Credential> objs = null;

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                objs = (await _LiteGraph.Credential.ReadMany(req.TenantGUID.Value, null, null, req.Order, req.Skip).ConfigureAwait(false)).ToList();
            }
            else
            {
                objs = (await _LiteGraph.Credential.ReadByGuids(req.TenantGUID.Value, req.GUIDs).ConfigureAwait(false)).ToList();
            }

            if (objs == null) objs = new List<Credential>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> CredentialEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            EnumerationResult<Credential> er = await _LiteGraph.Credential.Enumerate(req.EnumerationQuery).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> CredentialRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            Credential obj = await _LiteGraph.Credential.ReadByGuid(req.TenantGUID.Value, req.CredentialGUID.Value).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (await _LiteGraph.Credential.ExistsByGuid(req.TenantGUID.Value, req.CredentialGUID.Value).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Credential == null) throw new ArgumentNullException(nameof(req.Credential));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            Credential obj = await _LiteGraph.Credential.Update(req.Credential).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> CredentialDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.Credential.DeleteByGuid(req.TenantGUID.Value, req.CredentialGUID.Value).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Label-Routes

        internal async Task<ResponseContext> LabelCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Label == null) throw new ArgumentNullException(nameof(req.Label));
            req.Label.TenantGUID = req.TenantGUID.Value;
            LabelMetadata obj = await _LiteGraph.Label.Create(req.Label, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelCreateMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Labels == null || req.Labels.Count < 1) throw new ArgumentNullException(nameof(req.Labels));
            foreach (LabelMetadata label in req.Labels) label.TenantGUID = req.TenantGUID.Value;
            List<LabelMetadata> obj = await _LiteGraph.Label.CreateMany(req.TenantGUID.Value, req.Labels, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<LabelMetadata> er = await _LiteGraph.Label.Enumerate(req.EnumerationQuery, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> LabelReadMany(RequestContext req)
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                {
                    objs.Add(label);
                }
            }
            else
            {
                objs = new List<LabelMetadata>();
                await foreach (LabelMetadata label in _LiteGraph.Label.ReadByGuids(req.TenantGUID.Value, req.GUIDs, CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                {
                    objs.Add(label);
                }
            }

            if (objs == null) objs = new List<LabelMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            LabelMetadata obj = await _LiteGraph.Label.ReadByGuid(req.TenantGUID.Value, req.LabelGUID.Value, CancellationToken.None).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> LabelExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (await _LiteGraph.Label.ExistsByGuid(req.TenantGUID.Value, req.LabelGUID.Value, CancellationToken.None).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> LabelUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Label == null) throw new ArgumentNullException(nameof(req.Label));
            req.Label.TenantGUID = req.TenantGUID.Value;
            LabelMetadata obj = await _LiteGraph.Label.Update(req.Label, CancellationToken.None).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Label.DeleteByGuid(req.TenantGUID.Value, req.LabelGUID.Value, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> LabelDeleteMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            await _LiteGraph.Label.DeleteMany(req.TenantGUID.Value, req.GUIDs, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Tag-Routes

        internal async Task<ResponseContext> TagCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tag == null) throw new ArgumentNullException(nameof(req.Tag));
            req.Tag.TenantGUID = req.TenantGUID.Value;
            TagMetadata obj = _LiteGraph.Tag.Create(req.Tag);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagCreateMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tags == null || req.Tags.Count < 1) throw new ArgumentNullException(nameof(req.Tags));
            foreach (TagMetadata tag in req.Tags) tag.TenantGUID = req.TenantGUID.Value;
            List<TagMetadata> obj = _LiteGraph.Tag.CreateMany(req.TenantGUID.Value, req.Tags);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<TagMetadata> er = _LiteGraph.Tag.Enumerate(req.EnumerationQuery);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> TagReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<TagMetadata> objs = null;

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                objs = _LiteGraph.Tag.ReadMany(req.TenantGUID.Value, null, null, null, null, null, req.Order, req.Skip).ToList();
            }
            else
            {
                objs = _LiteGraph.Tag.ReadByGuids(req.TenantGUID.Value, req.GUIDs).ToList();
            }

            if (objs == null) objs = new List<TagMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            TagMetadata obj = _LiteGraph.Tag.ReadByGuid(req.TenantGUID.Value, req.TagGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TagExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (_LiteGraph.Tag.ExistsByGuid(req.TenantGUID.Value, req.TagGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TagUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tag == null) throw new ArgumentNullException(nameof(req.Tag));
            req.Tag.TenantGUID = req.TenantGUID.Value;
            TagMetadata obj = _LiteGraph.Tag.Update(req.Tag);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.Tag.DeleteByGuid(req.TenantGUID.Value, req.TagGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> TagDeleteMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.Tag.DeleteMany(req.TenantGUID.Value, req.GUIDs);
            return new ResponseContext(req);
        }

        #endregion

        #region Vector-Routes

        internal async Task<ResponseContext> VectorCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vector == null) throw new ArgumentNullException(nameof(req.Vector));
            req.Vector.TenantGUID = req.TenantGUID.Value;
            VectorMetadata obj = _LiteGraph.Vector.Create(req.Vector);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorCreateMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vectors == null || req.Vectors.Count < 1) throw new ArgumentNullException(nameof(req.Vectors));
            List<VectorMetadata> obj = _LiteGraph.Vector.CreateMany(req.TenantGUID.Value, req.Vectors);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<VectorMetadata> er = _LiteGraph.Vector.Enumerate(req.EnumerationQuery);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> VectorReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            List<VectorMetadata> objs = null;

            if (req.GUIDs == null || req.GUIDs.Count < 1)
            {
                objs = _LiteGraph.Vector.ReadMany(req.TenantGUID.Value, null, null, null, req.Order, req.Skip).ToList();
            }
            else
            {
                objs = _LiteGraph.Vector.ReadByGuids(req.TenantGUID.Value, req.GUIDs).ToList();
            }

            if (objs == null) objs = new List<VectorMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            VectorMetadata obj = _LiteGraph.Vector.ReadByGuid(req.TenantGUID.Value, req.VectorGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> VectorExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (_LiteGraph.Vector.ExistsByGuid(req.TenantGUID.Value, req.VectorGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> VectorUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vector == null) throw new ArgumentNullException(nameof(req.Vector));
            req.Vector.TenantGUID = req.TenantGUID.Value;
            VectorMetadata obj = _LiteGraph.Vector.Update(req.Vector);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.Vector.DeleteByGuid(req.TenantGUID.Value, req.VectorGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorDeleteMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.Vector.DeleteMany(req.TenantGUID.Value, req.GUIDs);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorSearch(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.VectorSearchRequest == null) throw new ArgumentNullException(nameof(req.VectorSearchRequest));
            if (req.GraphGUID != null && !await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            IEnumerable<VectorSearchResult> results = _LiteGraph.Vector.Search(req.VectorSearchRequest).ToList();
            return new ResponseContext(req, results);
        }

        #endregion

        #region Graph-Routes

        internal async Task<ResponseContext> GraphCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Graph == null) throw new ArgumentNullException(nameof(req.Graph));
            req.Graph.TenantGUID = req.TenantGUID.Value;

            Graph graph = await _LiteGraph.Graph.Create(req.Graph, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, graph);
        }

        internal async Task<ResponseContext> GraphReadMany(RequestContext req)
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                {
                    objs.Add(graph);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> GraphEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            EnumerationResult<Graph> er = await _LiteGraph.Graph.Enumerate(req.EnumerationQuery, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> GraphExistence(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ExistenceRequest == null) throw new ArgumentNullException(nameof(req.ExistenceRequest));
            if (!req.ExistenceRequest.ContainsExistenceRequest()) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "No valid existence filters are present in the request.");
            ExistenceResult resp = await _LiteGraph.Batch.Existence(req.TenantGUID.Value, req.GraphGUID.Value, req.ExistenceRequest).ConfigureAwait(false);
            return new ResponseContext(req, resp);
        }

        internal async Task<ResponseContext> GraphSearch(RequestContext req)
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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                graphs.Add(graph);
            }
            sresp.Graphs = graphs;
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> GraphReadFirst(RequestContext req)
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
                CancellationToken.None).ConfigureAwait(false);

            if (graph != null) return new ResponseContext(req, graph);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "No matching records found.");
        }

        internal async Task<ResponseContext> GraphRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Graph graph = await _LiteGraph.Graph.ReadByGuid(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.IncludeData,
                req.IncludeSubordinates,
                CancellationToken.None).ConfigureAwait(false);
            if (graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, graph);
        }

        internal async Task<ResponseContext> GraphStatistics(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            object obj = null;
            if (req.GraphGUID == null) obj = await _LiteGraph.Graph.GetStatistics(req.TenantGUID.Value, CancellationToken.None).ConfigureAwait(false);
            else obj = await _LiteGraph.Graph.GetStatistics(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> GraphExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Graph graph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, false, false, CancellationToken.None).ConfigureAwait(false);
            if (graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req);
        }

        internal async Task<ResponseContext> GraphUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Graph == null) throw new ArgumentNullException(nameof(req.Graph));
            req.Graph.TenantGUID = req.TenantGUID.Value;
            req.Graph = await _LiteGraph.Graph.Update(req.Graph, CancellationToken.None).ConfigureAwait(false);
            if (req.Graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, req.Graph);
        }

        internal async Task<ResponseContext> GraphSubgraph(RequestContext req)
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
                CancellationToken.None).ConfigureAwait(false);

            return new ResponseContext(req, result);
        }

        internal async Task<ResponseContext> GraphSubgraphStatistics(RequestContext req)
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
                CancellationToken.None).ConfigureAwait(false);

            return new ResponseContext(req, stats);
        }

        internal async Task<ResponseContext> GraphDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                await _LiteGraph.Graph.DeleteByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.Force, CancellationToken.None).ConfigureAwait(false);
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

        internal async Task<ResponseContext> GraphGexfExport(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            try
            {
                string xml = _LiteGraph.RenderGraphAsGexf(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    req.IncludeData,
                    req.IncludeSubordinates);

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

        #endregion

        #region Node-Routes

        internal async Task<ResponseContext> NodeCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Node == null) throw new ArgumentNullException(nameof(req.Node));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node = await _LiteGraph.Node.Create(req.Node, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, req.Node);
        }

        internal async Task<ResponseContext> NodeCreateMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Nodes == null) throw new ArgumentNullException(nameof(req.Nodes));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                req.Nodes = await _LiteGraph.Node.CreateMany(req.TenantGUID.Value, req.GraphGUID.Value, req.Nodes, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext(req, req.Nodes);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
        }

        internal async Task<ResponseContext> NodeReadMany(RequestContext req)
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                {
                    objs.Add(node);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> NodeEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            req.EnumerationQuery.GraphGUID = req.GraphGUID;
            EnumerationResult<Node> er = await _LiteGraph.Node.Enumerate(req.EnumerationQuery, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> NodeSearch(RequestContext req)
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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                sresp.Nodes.Add(node);
            }
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> NodeReadFirst(RequestContext req)
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
                CancellationToken.None).ConfigureAwait(false);

            if (node != null) return new ResponseContext(req, node);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "No matching records found.");
        }

        internal async Task<ResponseContext> NodeRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            Node node = await _LiteGraph.Node.ReadByGuid(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.IncludeData,
                req.IncludeSubordinates,
                CancellationToken.None).ConfigureAwait(false);

            if (node == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, node);
        }

        internal async Task<ResponseContext> NodeExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            bool exists = await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.NodeGUID.Value, CancellationToken.None).ConfigureAwait(false);
            if (exists) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> NodeUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Node == null) throw new ArgumentNullException(nameof(req.Node));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node = await _LiteGraph.Node.Update(req.Node, CancellationToken.None).ConfigureAwait(false);
            if (req.Node != null) return new ResponseContext(req, req.Node);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> NodeDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.NodeGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Node.DeleteByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeDeleteAll(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Node.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeDeleteMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Node.DeleteMany(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Edge-Routes

        internal async Task<ResponseContext> EdgeCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edge == null) throw new ArgumentNullException(nameof(req.Edge));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge = await _LiteGraph.Edge.Create(req.Edge, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, req.Edge);
        }

        internal async Task<ResponseContext> EdgeCreateMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edges == null) throw new ArgumentNullException(nameof(req.Edges));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                req.Edges = await _LiteGraph.Edge.CreateMany(req.TenantGUID.Value, req.GraphGUID.Value, req.Edges, CancellationToken.None).ConfigureAwait(false);
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

        internal async Task<ResponseContext> EdgeReadMany(RequestContext req)
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
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
                    CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
                {
                    objs.Add(edge);
                }
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> EdgeEnumerate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.EnumerationQuery == null) req.EnumerationQuery = new EnumerationRequest();
            req.EnumerationQuery.TenantGUID = req.TenantGUID;
            req.EnumerationQuery.GraphGUID = req.GraphGUID;
            EnumerationResult<Edge> er = await _LiteGraph.Edge.Enumerate(req.EnumerationQuery, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, er);
        }

        internal async Task<ResponseContext> EdgesBetween(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            req.Edges = new List<Edge>();
            await foreach (Edge edge in _LiteGraph.Edge.ReadEdgesBetweenNodes(req.TenantGUID.Value, req.GraphGUID.Value, req.FromGUID.Value, req.ToGUID.Value, null, null, null, EnumerationOrderEnum.CreatedDescending, 0, false, false, CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                req.Edges.Add(edge);
            }
            return new ResponseContext(req, req.Edges);
        }

        internal async Task<ResponseContext> EdgeSearch(RequestContext req)
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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                sresp.Edges.Add(edge);
            }

            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> EdgeReadFirst(RequestContext req)
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
                CancellationToken.None).ConfigureAwait(false);

            if (edge != null) return new ResponseContext(req, edge);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "No matching records found.");
        }

        internal async Task<ResponseContext> EdgeRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Edge edge = await _LiteGraph.Edge.ReadByGuid(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.EdgeGUID.Value,
                req.IncludeData,
                req.IncludeSubordinates,
                CancellationToken.None).ConfigureAwait(false);
            if (edge == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, edge);
        }

        internal async Task<ResponseContext> EdgeExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (await _LiteGraph.Edge.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, CancellationToken.None).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> EdgeUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edge == null) throw new ArgumentNullException(nameof(req.Edge));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge = await _LiteGraph.Edge.Update(req.Edge, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req, req.Edge);
        }

        internal async Task<ResponseContext> EdgeDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Edge.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteByGuid(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteAll(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteAllInGraph(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.Edge.DeleteMany(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs, CancellationToken.None).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Routes-and-Traversal

        internal async Task<ResponseContext> EdgesFromNode(RequestContext req)
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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                edgesFrom.Add(edge);
            }
            return new ResponseContext(req, edgesFrom);
        }

        internal async Task<ResponseContext> EdgesToNode(RequestContext req)
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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                edgesTo.Add(edge);
            }
            return new ResponseContext(req, edgesTo);
        }

        internal async Task<ResponseContext> AllEdgesToNode(RequestContext req)
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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                allEdges.Add(edge);
            }
            return new ResponseContext(req, allEdges);
        }

        internal async Task<ResponseContext> NodeChildren(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> nodes = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadChildren(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                token: CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                nodes.Add(node);
            }
            return new ResponseContext(req, nodes);
        }

        internal async Task<ResponseContext> NodeParents(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> parents = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadParents(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                token: CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                parents.Add(node);
            }
            return new ResponseContext(req, parents);
        }

        internal async Task<ResponseContext> NodeNeighbors(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Node> neighbors = new List<Node>();
            await foreach (Node node in _LiteGraph.Node.ReadNeighbors(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.NodeGUID.Value,
                req.Order,
                req.Skip,
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
            {
                neighbors.Add(node);
            }
            return new ResponseContext(req, neighbors);
        }

        internal async Task<ResponseContext> GetRoutes(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.RouteRequest == null) throw new ArgumentNullException(nameof(req.RouteRequest));
            if (!await _LiteGraph.Graph.ExistsByGuid(req.TenantGUID.Value, req.GraphGUID.Value, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.RouteRequest.From, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!await _LiteGraph.Node.ExistsByGuid(req.TenantGUID.Value, req.RouteRequest.To, CancellationToken.None).ConfigureAwait(false)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

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
                CancellationToken.None).WithCancellation(CancellationToken.None).ConfigureAwait(false))
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
