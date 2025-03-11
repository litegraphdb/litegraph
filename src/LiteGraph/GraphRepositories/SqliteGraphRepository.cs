namespace LiteGraph.GraphRepositories
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Caching;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;
    using PrettyId;

    /// <summary>
    /// Sqlite graph repository.
    /// </summary>
    public class SqliteGraphRepository : GraphRepositoryBase
    {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

        // Helpful references for Sqlite JSON:
        // https://stackoverflow.com/questions/33432421/sqlite-json1-example-for-json-extract-set
        // https://www.sqlite.org/json1.html

        #region Public-Members

        /// <summary>
        /// Sqlite database filename.
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
        }

        /// <summary>
        /// Maximum supported statement length.
        /// Default for Sqlite is 1000000 (see https://www.sqlite.org/limits.html).
        /// </summary>
        public int MaxStatementLength
        {
            get
            {
                return _MaxStatementLength;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxStatementLength));
                _MaxStatementLength = value;
            }
        }

        /// <summary>
        /// Number of records to retrieve for object list retrieval.
        /// </summary>
        public int SelectBatchSize
        {
            get
            {
                return _SelectBatchSize;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(SelectBatchSize));
                _SelectBatchSize = value;
            }
        }

        /// <summary>
        /// Timestamp format.
        /// </summary>
        public string TimestampFormat
        {
            get
            {
                return _TimestampFormat;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(TimestampFormat));
                string test = DateTime.UtcNow.ToString(value);
                _TimestampFormat = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Filename = "litegraph.db";
        private string _ConnectionString = "Data Source=litegraph.db;Pooling=false";
        private int _MaxStatementLength = 1000000; // https://www.sqlite.org/limits.html
        private int _SelectBatchSize = 100;
        private string _TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
        private readonly object _QueryLock = new object();
        private readonly object _CreateLock = new object();
        private SerializationHelper _Serializer = new SerializationHelper();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="filename">Sqlite database filename.</param>
        public SqliteGraphRepository(string filename = "litegraph.db")
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            _Filename = filename;
            _ConnectionString = "Data Source=" + filename + ";Pooling=false";
        }

        #endregion

        #region Public-Methods

        #region General

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            foreach (string query in Setup.CreateTablesAndIndices())
                Query(query);
        }

        #endregion

        #region Tenants

        /// <inheritdoc />
        public override TenantMetadata CreateTenant(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            string createQuery = Tenants.InsertTenantQuery(tenant);
            DataTable createResult = null;
            TenantMetadata created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.TenantFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override TenantMetadata CreateTenant(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string createQuery = Tenants.InsertTenantQuery(new TenantMetadata
            {
                Name = name,
                CreatedUtc = DateTime.UtcNow
            });

            DataTable createResult = null;
            TenantMetadata created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.TenantFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override void DeleteTenant(Guid guid, bool force = false)
        {
            TenantMetadata tenant = ReadTenant(guid);
            if (tenant != null)
            {
                if (force)
                {
                    DeleteUsers(tenant.GUID);
                    DeleteGraphs(tenant.GUID);
                }

                if (ReadUsers(tenant.GUID, null).Any())
                {
                    Logging.Log(SeverityEnum.Warn, "specified tenant " + guid + " has linked users");
                    throw new InvalidOperationException("The specified tenant has linked users.");
                }

                if (ReadGraphs(tenant.GUID).Any())
                {
                    Logging.Log(SeverityEnum.Warn, "specified tenant " + guid + " has linked graphs");
                    throw new InvalidOperationException("The specified tenant has linked graphs.");
                }

                Query(Tenants.DeleteTenantQuery(guid), true);
            }
        }

        /// <inheritdoc />
        public override bool ExistsTenant(Guid guid)
        {
            if (ReadTenant(guid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override TenantMetadata ReadTenant(Guid guid)
        {
            DataTable result = Query(Tenants.SelectTenantQuery(guid));
            if (result != null && result.Rows.Count == 1) return Converters.TenantFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<TenantMetadata> ReadTenants(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Tenants.SelectTenantsQuery(SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TenantFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override TenantMetadata UpdateTenant(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            return Converters.TenantFromDataRow(Query(Tenants.UpdateTenantQuery(tenant), true).Rows[0]);
        }

        #endregion

        #region Users

        /// <inheritdoc />
        public override UserMaster CreateUser(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            ValidateTenantExists(user.TenantGUID);

            TenantMetadata tenant = ReadTenant(user.TenantGUID);
            if (tenant == null) throw new KeyNotFoundException("The specified tenant could not be found.");

            string createQuery = Users.InsertUserQuery(user);
            DataTable createResult = null;
            UserMaster created = null;
            UserMaster existing = null;

            lock (_CreateLock)
            {
                existing = ReadUserByEmail(user.TenantGUID, user.Email);
                if (existing != null) return existing;
                createResult = Query(createQuery, true);
            }

            created = Converters.UserFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override UserMaster CreateUser(Guid tenantGuid, string firstName, string lastName, string email, string password)
        {
            if (String.IsNullOrEmpty(firstName)) throw new ArgumentNullException(nameof(firstName));
            if (String.IsNullOrEmpty(lastName)) throw new ArgumentNullException(nameof(lastName));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            ValidateTenantExists(tenantGuid);

            TenantMetadata tenant = ReadTenant(tenantGuid);
            if (tenant == null) throw new KeyNotFoundException("The specified tenant could not be found.");

            string createQuery = Users.InsertUserQuery(new UserMaster
            {
                TenantGUID = tenantGuid,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                CreatedUtc = DateTime.UtcNow
            });

            DataTable createResult = null;
            UserMaster created = null;
            UserMaster existing = null;

            lock (_CreateLock)
            {
                existing = ReadUserByEmail(tenantGuid, email);
                if (existing != null) return existing;
                createResult = Query(createQuery, true);
            }

            created = Converters.UserFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override void DeleteUser(Guid tenantGuid, Guid guid)
        {
            UserMaster user = ReadUser(tenantGuid, guid);
            if (user != null)
            {
                DeleteUserCredentials(tenantGuid, guid);
                Query(Users.DeleteUserQuery(tenantGuid, guid), true);
            }
        }

        /// <inheritdoc />
        public override void DeleteUsers(Guid tenantGuid)
        {
            List<UserMaster> users = ReadUsers(tenantGuid, null).ToList();
            if (users != null && users.Count > 0)
            {
                foreach (UserMaster user in users)
                {
                    DeleteUser(user.TenantGUID, user.GUID);
                    DeleteUserCredentials(user.TenantGUID, user.GUID);
                }
            }
        }

        /// <inheritdoc />
        public override bool ExistsUser(Guid tenantGuid, Guid guid)
        {
            if (ReadUser(tenantGuid, guid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override bool ExistsUserByEmail(Guid tenantGuid, string email)
        {
            if (ReadUserByEmail(tenantGuid, email) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override UserMaster ReadUser(Guid tenantGuid, Guid guid)
        {
            DataTable result = Query(Users.SelectUserQuery(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override List<TenantMetadata> ReadUserTenants(string email)
        {
            DataTable result = Query(Users.SelectUserTenantsQuery(email));
            List<TenantMetadata> tenants = new List<TenantMetadata>();
            if (result != null && result.Rows.Count > 0)
                foreach (DataRow row in result.Rows) tenants.Add(Converters.TenantFromDataRow(row));
            return tenants;
        }

        /// <inheritdoc />
        public override UserMaster ReadUserByEmail(Guid tenantGuid, string email)
        {
            DataTable result = Query(Users.SelectUserQuery(tenantGuid, email));
            if (result != null && result.Rows.Count == 1) return Converters.UserFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<UserMaster> ReadUsers(
            Guid? tenantGuid,
            string email,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Users.SelectUsersQuery(tenantGuid, email, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.UserFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override UserMaster UpdateUser(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            ValidateTenantExists(user.TenantGUID);
            return Converters.UserFromDataRow(Query(Users.UpdateUserQuery(user), true).Rows[0]);
        }

        #endregion

        #region Credentials

        /// <inheritdoc />
        public override Credential CreateCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            ValidateTenantExists(cred.TenantGUID);
            ValidateUserExists(cred.TenantGUID, cred.UserGUID);

            TenantMetadata tenant = ReadTenant(cred.TenantGUID);
            if (tenant == null) throw new KeyNotFoundException("The specified tenant could not be found.");

            UserMaster user = ReadUser(cred.TenantGUID, cred.UserGUID);
            if (user == null) throw new KeyNotFoundException("The specified user could not be found.");

            string createQuery = Credentials.InsertCredentialQuery(cred);
            DataTable createResult = null;
            Credential created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.CredentialFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override Credential CreateCredential(
            Guid tenantGuid,
            Guid userGuid,
            string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            ValidateTenantExists(tenantGuid);
            ValidateUserExists(tenantGuid, userGuid);

            TenantMetadata tenant = ReadTenant(tenantGuid);
            if (tenant == null) throw new KeyNotFoundException("The specified tenant could not be found.");

            UserMaster user = ReadUser(tenantGuid, userGuid);
            if (user == null) throw new KeyNotFoundException("The specified user could not be found.");

            string createQuery = Credentials.InsertCredentialQuery(new Credential
            {
                TenantGUID = tenantGuid,
                UserGUID = userGuid,
                Name = name,
                BearerToken = IdGenerator.Generate(64),
                CreatedUtc = DateTime.UtcNow
            });

            DataTable createResult = null;
            Credential created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.CredentialFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override void DeleteCredential(Guid tenantGuid, Guid guid)
        {
            Credential cred = ReadCredential(tenantGuid, guid);
            if (cred != null)
            {
                Query(Credentials.DeleteCredentialQuery(tenantGuid, guid), true);
            }
        }

        /// <inheritdoc />
        public override void DeleteUserCredentials(Guid tenantGuid, Guid userGuid)
        {
            List<Credential> creds = ReadCredentials(tenantGuid, userGuid, null).ToList();
            if (creds != null && creds.Count > 0)
            {
                foreach (Credential cred in creds) DeleteCredential(cred.TenantGUID, cred.GUID);
            }
        }

        /// <inheritdoc />
        public override bool ExistsCredential(Guid tenantGuid, Guid guid)
        {
            if (ReadCredential(tenantGuid, guid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override Credential ReadCredential(Guid tenantGuid, Guid guid)
        {
            DataTable result = Query(Credentials.SelectCredentialQuery(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.CredentialFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override Credential ReadCredentialByBearerToken(string bearerToken)
        {
            DataTable result = Query(Credentials.SelectCredentialQuery(bearerToken));
            if (result != null && result.Rows.Count == 1) return Converters.CredentialFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<Credential> ReadCredentials(
            Guid? tenantGuid,
            Guid? userGuid,
            string bearerToken,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Credentials.SelectCredentialsQuery(tenantGuid, userGuid, bearerToken, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.CredentialFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override Credential UpdateCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            ValidateTenantExists(cred.TenantGUID);
            ValidateUserExists(cred.TenantGUID, cred.UserGUID);
            return Converters.CredentialFromDataRow(Query(Credentials.UpdateCredentialQuery(cred), true).Rows[0]);
        }

        #endregion

        #region Labels

        /// <inheritdoc />
        public override LabelMetadata CreateLabel(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (String.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");

            ValidateTenantExists(label.TenantGUID);
            ValidateGraphExists(label.TenantGUID, label.GraphGUID);
            if (label.NodeGUID != null) ValidateNodeExists(label.TenantGUID, label.GraphGUID, label.NodeGUID.Value);
            if (label.EdgeGUID != null) ValidateEdgeExists(label.TenantGUID, label.GraphGUID, label.EdgeGUID.Value);

            string createQuery = Labels.InsertLabelQuery(label);

            DataTable createResult = null;
            LabelMetadata created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.LabelFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override LabelMetadata CreateLabel(Guid tenantGuid, Guid graphGuid, Guid? nodeGuid, Guid? edgeGuid, string label)
        {
            if (String.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            return CreateLabel(new LabelMetadata { GUID = Guid.NewGuid(), TenantGUID = tenantGuid, GraphGUID = graphGuid, NodeGUID = nodeGuid, EdgeGUID = edgeGuid, Label = label });
        }

        /// <inheritdoc />
        public override List<LabelMetadata> CreateMultipleLabels(Guid tenantGuid, Guid graphGuid, List<LabelMetadata> labels)
        {
            if (labels == null || labels.Count < 1) return null;
            ValidateTenantExists(tenantGuid);
            ValidateGraphExists(tenantGuid, graphGuid);

            foreach (LabelMetadata label in labels)
            {
                if (String.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");

                label.TenantGUID = tenantGuid;
                label.GraphGUID = graphGuid;
                if (label.NodeGUID != null) ValidateNodeExists(label.TenantGUID, label.GraphGUID, label.NodeGUID.Value);
                if (label.EdgeGUID != null) ValidateEdgeExists(label.TenantGUID, label.GraphGUID, label.EdgeGUID.Value);
            }

            string createQuery = Labels.InsertMultipleLabelsQuery(tenantGuid, graphGuid, labels);
            string retrieveQuery = Labels.SelectMultipleLabelsQuery(tenantGuid, labels.Select(n => n.GUID).ToList());
            DataTable createResult = null;
            DataTable retrieveResult = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
                retrieveResult = Query(retrieveQuery, true);
            }

            return Converters.LabelsFromDataTable(retrieveResult);
        }

        /// <inheritdoc />
        public override void DeleteLabel(Guid tenantGuid, Guid guid)
        {
            LabelMetadata label = ReadLabel(tenantGuid, guid);
            if (label != null)
            {
                Query(Labels.DeleteLabelQuery(tenantGuid, guid), true);
            }
        }

        /// <inheritdoc />
        public override void DeleteLabels(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            Query(Labels.DeleteLabelsQuery(tenantGuid, graphGuid, nodeGuids, edgeGuids));
        }

        /// <inheritdoc />
        public override bool ExistsLabel(Guid tenantGuid, Guid guid)
        {
            if (ReadLabel(tenantGuid, guid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override LabelMetadata ReadLabel(Guid tenantGuid, Guid guid)
        {
            DataTable result = Query(Labels.SelectLabelQuery(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.LabelFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<LabelMetadata> ReadLabels(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = null;
                if (graphGuid == null)
                {
                    query = Labels.SelectTenantLabelsQuery(tenantGuid, label, SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null) query = Labels.SelectEdgeLabelsQuery(tenantGuid, graphGuid.Value, edgeGuid.Value, label, SelectBatchSize, skip, order);
                    else if (nodeGuid != null) query = Labels.SelectNodeLabelsQuery(tenantGuid, graphGuid.Value, nodeGuid.Value, label, SelectBatchSize, skip, order);
                    else query = Labels.SelectGraphLabelsQuery(tenantGuid, graphGuid.Value, label, SelectBatchSize, skip, order);
                }

                DataTable result = Query(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override LabelMetadata UpdateLabel(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (String.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");

            ValidateTenantExists(label.TenantGUID);
            ValidateGraphExists(label.TenantGUID, label.GraphGUID);
            if (label.NodeGUID != null) ValidateNodeExists(label.TenantGUID, label.GraphGUID, label.NodeGUID.Value);
            if (label.EdgeGUID != null) ValidateEdgeExists(label.TenantGUID, label.GraphGUID, label.EdgeGUID.Value);

            string updateQuery = Labels.UpdateLabelQuery(label);
            DataTable updateResult = null;
            LabelMetadata updated = null;

            lock (_CreateLock)
            {
                updateResult = Query(updateQuery, true);
            }

            updated = Converters.LabelFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        #endregion

        #region Tags

        /// <inheritdoc />
        public override TagMetadata CreateTag(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (String.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");

            ValidateTenantExists(tag.TenantGUID);
            ValidateGraphExists(tag.TenantGUID, tag.GraphGUID);
            if (tag.NodeGUID != null) ValidateNodeExists(tag.TenantGUID, tag.GraphGUID, tag.NodeGUID.Value);
            if (tag.EdgeGUID != null) ValidateEdgeExists(tag.TenantGUID, tag.GraphGUID, tag.EdgeGUID.Value);

            string createQuery = Tags.InsertTagQuery(tag);

            DataTable createResult = null;
            TagMetadata created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.TagFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override TagMetadata CreateTag(Guid tenantGuid, Guid graphGuid, Guid? nodeGuid, Guid? edgeGuid, string key, string value)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return CreateTag(new TagMetadata { GUID = Guid.NewGuid(), TenantGUID = tenantGuid, GraphGUID = graphGuid, NodeGUID = nodeGuid, EdgeGUID = edgeGuid, Key = key, Value = value });
        }

        /// <inheritdoc />
        public override List<TagMetadata> CreateMultipleTags(Guid tenantGuid, Guid graphGuid, List<TagMetadata> tags)
        {
            if (tags == null || tags.Count < 1) return null;
            ValidateTenantExists(tenantGuid);
            ValidateGraphExists(tenantGuid, graphGuid);

            foreach (TagMetadata tag in tags)
            {
                if (String.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");

                tag.TenantGUID = tenantGuid;
                tag.GraphGUID = graphGuid;
            }

            string createQuery = Tags.InsertMultipleTagsQuery(tenantGuid, graphGuid, tags);
            string retrieveQuery = Tags.SelectMultipleTagsQuery(tenantGuid, tags.Select(n => n.GUID).ToList());
            DataTable createResult = null;
            DataTable retrieveResult = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
                retrieveResult = Query(retrieveQuery, true);
            }

            return Converters.TagsFromDataTable(retrieveResult);
        }

        /// <inheritdoc />
        public override void DeleteTag(Guid tenantGuid, Guid guid)
        {
            TagMetadata tag = ReadTag(tenantGuid, guid);
            if (tag != null)
            {
                Query(Tags.DeleteTagQuery(tenantGuid, guid), true);
            }
        }

        /// <inheritdoc />
        public override void DeleteTags(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            Query(Tags.DeleteTagsQuery(tenantGuid, graphGuid, nodeGuids, edgeGuids));
        }

        /// <inheritdoc />
        public override bool ExistsTag(Guid tenantGuid, Guid guid)
        {
            if (ReadTag(tenantGuid, guid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override TagMetadata ReadTag(Guid tenantGuid, Guid guid)
        {
            DataTable result = Query(Tags.SelectTagQuery(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.TagFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<TagMetadata> ReadTags(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string key,
            string val,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = null;
                if (graphGuid == null)
                {
                    query = Tags.SelectTenantTagsQuery(tenantGuid, key, val, SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null) query = Tags.SelectEdgeTagsQuery(tenantGuid, graphGuid.Value, edgeGuid.Value, key, val, SelectBatchSize, skip, order);
                    else if (nodeGuid != null) query = Tags.SelectNodeTagsQuery(tenantGuid, graphGuid.Value, nodeGuid.Value, key, val, SelectBatchSize, skip, order);
                    else query = Tags.SelectGraphTagsQuery(tenantGuid, graphGuid.Value, key, val, SelectBatchSize, skip, order);
                }

                DataTable result = Query(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override TagMetadata UpdateTag(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (String.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");

            ValidateTenantExists(tag.TenantGUID);
            ValidateGraphExists(tag.TenantGUID, tag.GraphGUID);
            if (tag.NodeGUID != null) ValidateNodeExists(tag.TenantGUID, tag.GraphGUID, tag.NodeGUID.Value);
            if (tag.EdgeGUID != null) ValidateEdgeExists(tag.TenantGUID, tag.GraphGUID, tag.EdgeGUID.Value);

            string updateQuery = Tags.UpdateTagQuery(tag);
            DataTable updateResult = null;
            TagMetadata updated = null;

            lock (_CreateLock)
            {
                updateResult = Query(updateQuery, true);
            }

            updated = Converters.TagFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        #endregion

        #region Vectors

        /// <inheritdoc />
        public override VectorMetadata CreateVector(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (String.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
            if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
            if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

            ValidateTenantExists(vector.TenantGUID);
            ValidateGraphExists(vector.TenantGUID, vector.GraphGUID);
            if (vector.NodeGUID != null) ValidateNodeExists(vector.TenantGUID, vector.GraphGUID, vector.NodeGUID.Value);
            if (vector.EdgeGUID != null) ValidateEdgeExists(vector.TenantGUID, vector.GraphGUID, vector.EdgeGUID.Value);

            string createQuery = Vectors.InsertVectorQuery(vector);

            DataTable createResult = null;
            VectorMetadata created = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
            }

            created = Converters.VectorFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public override VectorMetadata CreateVector(
            Guid tenantGuid,
            Guid graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string model,
            int dimensionality,
            string content,
            List<float> vectors)
        {
            if (String.IsNullOrEmpty(model)) throw new ArgumentNullException(nameof(model));
            if (dimensionality < 0) throw new ArgumentOutOfRangeException(nameof(dimensionality));
            if (String.IsNullOrEmpty(content)) throw new ArgumentNullException(nameof(content));
            if (vectors == null || vectors.Count < 1) throw new ArgumentNullException(nameof(vectors));
            return CreateVector(new VectorMetadata { 
                GUID = Guid.NewGuid(), 
                TenantGUID = tenantGuid, 
                GraphGUID = graphGuid, 
                NodeGUID = nodeGuid, 
                EdgeGUID = edgeGuid, 
                Model = model,
                Dimensionality = dimensionality,
                Content = content,
                Vectors = vectors
            });
        }

        /// <inheritdoc />
        public override List<VectorMetadata> CreateMultipleVectors(Guid tenantGuid, Guid graphGuid, List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count < 1) return null;
            ValidateTenantExists(tenantGuid);
            ValidateGraphExists(tenantGuid, graphGuid);

            foreach (VectorMetadata vector in vectors)
            {
                if (String.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
                if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
                if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

                vector.TenantGUID = tenantGuid;
                vector.GraphGUID = graphGuid;
                if (vector.NodeGUID != null) ValidateNodeExists(vector.TenantGUID, vector.GraphGUID, vector.NodeGUID.Value);
                if (vector.EdgeGUID != null) ValidateEdgeExists(vector.TenantGUID, vector.GraphGUID, vector.EdgeGUID.Value);
            }

            string createQuery = Vectors.InsertMultipleVectorsQuery(tenantGuid, graphGuid, vectors);
            string retrieveQuery = Vectors.SelectMultipleVectorsQuery(tenantGuid, vectors.Select(n => n.GUID).ToList());
            DataTable createResult = null;
            DataTable retrieveResult = null;

            lock (_CreateLock)
            {
                createResult = Query(createQuery, true);
                retrieveResult = Query(retrieveQuery, true);
            }

            return Converters.VectorsFromDataTable(retrieveResult);
        }

        /// <inheritdoc />
        public override void DeleteVector(Guid tenantGuid, Guid guid)
        {
            VectorMetadata vector = ReadVector(tenantGuid, guid);
            if (vector != null)
            {
                Query(Vectors.DeleteVectorQuery(tenantGuid, guid), true);
            }
        }

        /// <inheritdoc />
        public override void DeleteVectors(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            Query(Vectors.DeleteVectorsQuery(tenantGuid, graphGuid, nodeGuids, edgeGuids));
        }

        /// <inheritdoc />
        public override bool ExistsVector(Guid tenantGuid, Guid guid)
        {
            if (ReadVector(tenantGuid, guid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override VectorMetadata ReadVector(Guid tenantGuid, Guid guid)
        {
            DataTable result = Query(Vectors.SelectVectorQuery(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.VectorFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<VectorMetadata> ReadVectors(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = null;
                if (graphGuid == null)
                {
                    query = Vectors.SelectTenantVectorsQuery(tenantGuid, SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null) query = Vectors.SelectEdgeVectorsQuery(tenantGuid, graphGuid.Value, edgeGuid.Value, SelectBatchSize, skip, order);
                    else if (nodeGuid != null) query = Vectors.SelectNodeVectorsQuery(tenantGuid, graphGuid.Value, nodeGuid.Value, SelectBatchSize, skip, order);
                    else query = Vectors.SelectGraphVectorsQuery(tenantGuid, graphGuid.Value, SelectBatchSize, skip, order);
                }

                DataTable result = Query(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<VectorMetadata> ReadGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            int skip = 0;
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending;

            while (true)
            {
                string query = Vectors.SelectGraphVectorsQuery(tenantGuid, graphGuid, SelectBatchSize, skip, order);
                DataTable result = Query(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<VectorMetadata> ReadNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            int skip = 0;
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending;

            while (true)
            {
                string query = Vectors.SelectNodeVectorsQuery(tenantGuid, graphGuid, nodeGuid, SelectBatchSize, skip, order);
                DataTable result = Query(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    VectorMetadata md = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return md;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<VectorMetadata> ReadEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            int skip = 0;
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending;

            while (true)
            {
                string query = Vectors.SelectEdgeVectorsQuery(tenantGuid, graphGuid, edgeGuid, SelectBatchSize, skip, order);
                DataTable result = Query(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override VectorMetadata UpdateVector(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (String.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
            if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
            if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

            ValidateTenantExists(vector.TenantGUID);
            ValidateGraphExists(vector.TenantGUID, vector.GraphGUID);
            if (vector.NodeGUID != null) ValidateNodeExists(vector.TenantGUID, vector.GraphGUID, vector.NodeGUID.Value);
            if (vector.EdgeGUID != null) ValidateEdgeExists(vector.TenantGUID, vector.GraphGUID, vector.EdgeGUID.Value);

            string updateQuery = Vectors.UpdateVectorQuery(vector);
            DataTable updateResult = null;
            VectorMetadata updated = null;

            lock (_CreateLock)
            {
                updateResult = Query(updateQuery, true);
            }

            updated = Converters.VectorFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public override IEnumerable<VectorSearchResult> SearchGraphVectors(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (Graph graph in ReadGraphs(tenantGuid, labels, tags, filter))
            {
                if (graph.Vectors == null || graph.Vectors.Count < 1 || graph.Vectors.Count != vectors.Count) continue;

                foreach (VectorMetadata vmd in graph.Vectors)
                {
                    if (vmd.Vectors == null || vmd.Vectors.Count < 1) continue;

                    float? score = null;
                    float? distance = null;
                    float? innerProduct = null;

                    CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                    yield return new VectorSearchResult
                    {
                        Graph = graph,
                        Score = score,
                        Distance = distance,
                        InnerProduct = innerProduct,
                    };
                }
            }

            yield break;
        }

        /// <inheritdoc />
        public override IEnumerable<VectorSearchResult> SearchNodeVectors(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (Node node in ReadNodes(tenantGuid, graphGuid, labels, tags, filter))
            {
                if (node.Vectors == null || node.Vectors.Count < 1) continue;

                foreach (VectorMetadata vmd in node.Vectors)
                {
                    if (vmd.Vectors == null || vmd.Vectors.Count < 1) continue;
                    if (vmd.Vectors.Count != vectors.Count) continue;

                    float? score = null;
                    float? distance = null;
                    float? innerProduct = null;

                    CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                    yield return new VectorSearchResult
                    {
                        Node = node,
                        Score = score,
                        Distance = distance,
                        InnerProduct = innerProduct,
                    };
                }
            }

            yield break;
        }

        /// <inheritdoc />
        public override IEnumerable<VectorSearchResult> SearchEdgeVectors(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (Edge edge in ReadEdges(tenantGuid, graphGuid, labels, tags, filter))
            {
                if (edge.Vectors == null || edge.Vectors.Count < 1 || edge.Vectors.Count != vectors.Count) continue;

                foreach (VectorMetadata vmd in edge.Vectors)
                {
                    if (vmd.Vectors == null || vmd.Vectors.Count < 1) continue;

                    float? score = null;
                    float? distance = null;
                    float? innerProduct = null;

                    CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                    yield return new VectorSearchResult
                    {
                        Edge = edge,
                        Score = score,
                        Distance = distance,
                        InnerProduct = innerProduct,
                    };
                }
            }

            yield break;
        }

        #endregion

        #region Graphs

        /// <inheritdoc />
        public override Graph CreateGraph(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            ValidateLabels(graph.Labels);
            ValidateTags(graph.Tags);
            ValidateVectors(graph.Vectors);
            ValidateTenantExists(graph.TenantGUID);

            string createQuery = Graphs.InsertGraphQuery(graph);
            DataTable createResult = null;
            Graph created = null;
            Graph existing = null;

            lock (_CreateLock)
            {
                existing = ReadGraph(graph.TenantGUID, graph.GUID);
                if (existing != null) return existing;
                createResult = Query(createQuery, true);
            }

            created = Converters.GraphFromDataRow(createResult.Rows[0]);

            if (graph.Labels != null && graph.Labels.Count > 0)
            {
                List<LabelMetadata> labels = new List<LabelMetadata>();
                foreach (string label in graph.Labels)
                {
                    labels.Add(new LabelMetadata
                    {
                        TenantGUID = graph.TenantGUID,
                        GraphGUID = graph.GUID,
                        Label = label
                    });
                }

                CreateMultipleLabels(graph.TenantGUID, graph.GUID, labels);
            }

            if (graph.Tags != null && graph.Tags is NameValueCollection nvc)
            {
                List<TagMetadata> tags = new List<TagMetadata>();
                foreach (string key in nvc.AllKeys)
                {
                    tags.Add(new TagMetadata
                    {
                        TenantGUID = graph.TenantGUID,
                        GraphGUID = graph.GUID,
                        Key = key,
                        Value = nvc.Get(key)
                    });
                }

                if (tags.Count > 0) CreateMultipleTags(graph.TenantGUID, graph.GUID, tags);
            }

            if (graph.Vectors != null && graph.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in graph.Vectors)
                {
                    vector.TenantGUID = graph.TenantGUID;
                    vector.GraphGUID = graph.GUID;
                }

                created.Vectors = CreateMultipleVectors(graph.TenantGUID, graph.GUID, graph.Vectors);
            }

            created.Labels = graph.Labels;
            created.Tags = graph.Tags;
            created.Vectors = graph.Vectors;
            return created;
        }

        /// <inheritdoc />
        public override Graph CreateGraph(
            Guid tenantGuid,
            Guid guid,
            string name,
            object data = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            List<VectorMetadata> vectors = null)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            return CreateGraph(new Graph
            {
                GUID = guid,
                TenantGUID = tenantGuid,
                Name = name,
                Labels = labels,
                Tags = tags,
                Data = data,
                Vectors = vectors
            });
        }

        /// <inheritdoc />
        public override IEnumerable<Graph> ReadGraphs(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Graphs.SelectGraphsQuery(tenantGuid, labels, tags, graphFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);

                    List<LabelMetadata> allLabels = ReadLabels(tenantGuid, graph.GUID, null, null, null).ToList();
                    if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);

                    List<TagMetadata> allTags = ReadTags(tenantGuid, graph.GUID, null, null, null, null).ToList();
                    if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);

                    graph.Vectors = ReadGraphVectors(tenantGuid, graph.GUID).ToList();

                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override Graph ReadGraph(Guid tenantGuid, Guid guid)
        {
            DataTable result = Query(Graphs.SelectGraphQuery(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1)
            {
                Graph graph = Converters.GraphFromDataRow(result.Rows[0]);

                List<LabelMetadata> allLabels = ReadLabels(tenantGuid, graph.GUID, null, null, null).ToList();
                if (allLabels != null) graph.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = ReadTags(tenantGuid, guid, null, null, null, null).ToList();
                if (allTags != null) graph.Tags = TagMetadata.ToNameValueCollection(allTags);

                graph.Vectors = ReadGraphVectors(tenantGuid, guid).ToList();

                return graph;
            }
            return null;
        }

        /// <inheritdoc />
        public override Graph UpdateGraph(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            ValidateLabels(graph.Labels);
            ValidateTags(graph.Tags);
            ValidateVectors(graph.Vectors);
            ValidateTenantExists(graph.TenantGUID);
            ValidateGraphExists(graph.TenantGUID, graph.GUID);
            Graph updated = Converters.GraphFromDataRow(Query(Graphs.UpdateGraphQuery(graph), true).Rows[0]);
            DeleteGraphLabels(graph.TenantGUID, graph.GUID);
            DeleteGraphTags(graph.TenantGUID, graph.GUID);
            DeleteGraphVectors(graph.TenantGUID, graph.GUID);

            if (graph.Labels != null)
            {
                CreateMultipleLabels(
                    graph.TenantGUID,
                    graph.GUID,
                    LabelMetadata.FromListString(
                        graph.TenantGUID,
                        graph.GUID,
                        null,
                        null,
                        graph.Labels));
                updated.Labels = graph.Labels;
            }

            if (graph.Tags != null)
            {
                CreateMultipleTags(
                    graph.TenantGUID,
                    graph.GUID,
                    TagMetadata.FromNameValueCollection(
                        graph.TenantGUID,
                        graph.GUID,
                        null,
                        null,
                        graph.Tags));
                updated.Tags = graph.Tags;
            }

            if (graph.Vectors != null)
            {
                foreach (VectorMetadata vector in graph.Vectors)
                {
                    vector.TenantGUID = graph.TenantGUID;
                    vector.GraphGUID = graph.GUID;
                }

                updated.Vectors = CreateMultipleVectors(graph.TenantGUID, graph.GUID, graph.Vectors);                
            }

            return updated;
        }

        /// <inheritdoc />
        public override void DeleteGraph(Guid tenantGuid, Guid graphGuid, bool force = false)
        {
            Graph graph = ReadGraph(tenantGuid, graphGuid);
            if (graph != null)
            {
                int nodeCount = ReadNodes(tenantGuid, graphGuid).Count();
                if (nodeCount > 0 && !force)
                    throw new InvalidOperationException("Unable to delete graph '" + graphGuid + "' as it contains nodes.  If you wish to force deletion, set 'force' to true.");

                DeleteNodes(tenantGuid, graphGuid);
                DeleteEdges(tenantGuid, graphGuid);
                DeleteAllGraphLabels(tenantGuid, graphGuid);
                DeleteAllGraphTags(tenantGuid, graphGuid);
                DeleteAllGraphVectors(tenantGuid, graphGuid);
                Query(Graphs.DeleteGraphQuery(tenantGuid, graphGuid), true);
            }
        }
        
        /// <inheritdoc />
        public override void DeleteGraphs(Guid tenantGuid)
        {
            List<Graph> graphs = ReadGraphs(tenantGuid).ToList();
            if (graphs != null && graphs.Count > 0)
            {
                foreach (Graph graph in graphs)
                {
                    DeleteGraph(graph.TenantGUID, graph.GUID, true);
                }
            }
        }

        /// <inheritdoc />
        public override void DeleteAllGraphLabels(Guid tenantGuid, Guid graphGuid)
        {
            Query(Labels.DeleteAllGraphLabelsQuery(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public override void DeleteGraphLabels(Guid tenantGuid, Guid graphGuid)
        {
            Query(Labels.DeleteGraphLabelsQuery(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public override void DeleteAllGraphTags(Guid tenantGuid, Guid graphGuid)
        {
            Query(Tags.DeleteAllGraphTagsQuery(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public override void DeleteGraphTags(Guid tenantGuid, Guid graphGuid)
        {
            Query(Tags.DeleteGraphTagsQuery(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public override void DeleteAllGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            Query(Vectors.DeleteAllGraphVectors(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public override void DeleteGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            Query(Vectors.DeleteGraphVectors(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public override bool ExistsGraph(Guid tenantGuid, Guid graphGuid)
        {
            if (ReadGraph(tenantGuid, graphGuid) != null) return true;
            return false;
        }

        #endregion

        #region Nodes

        /// <inheritdoc />
        public override Node CreateNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            ValidateTags(node.Tags);
            ValidateLabels(node.Labels);
            ValidateVectors(node.Vectors);
            ValidateTenantExists(node.TenantGUID);
            ValidateGraphExists(node.TenantGUID, node.GraphGUID);

            string createQuery = Nodes.InsertNodeQuery(node);
            DataTable createResult = null;
            Node created = null;
            Node existing = null;

            lock (_CreateLock)
            {
                existing = ReadNode(node.TenantGUID, node.GraphGUID, node.GUID);
                if (existing != null) return existing;
                createResult = Query(createQuery, true);
            }

            created = Converters.NodeFromDataRow(createResult.Rows[0]);

            if (node.Labels != null)
            {
                List<LabelMetadata> labels = new List<LabelMetadata>();
                foreach (string label in node.Labels)
                {
                    labels.Add(new LabelMetadata
                    {
                        TenantGUID = node.TenantGUID,
                        GraphGUID = node.GraphGUID,
                        NodeGUID = node.GUID,
                        Label = label
                    });
                }

                if (labels.Count > 0) CreateMultipleLabels(node.TenantGUID, node.GraphGUID, labels);
            }

            if (node.Tags != null)
            {
                List<TagMetadata> tags = new List<TagMetadata>();
                foreach (string key in node.Tags)
                {
                    tags.Add(new TagMetadata
                    {
                        TenantGUID = node.TenantGUID,
                        GraphGUID = node.GraphGUID,
                        NodeGUID = node.GUID,
                        Key = key,
                        Value = node.Tags.Get(key)
                    });
                }

                if (tags.Count > 0) CreateMultipleTags(node.TenantGUID, node.GraphGUID, tags);
            }

            if (node.Vectors != null && node.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in node.Vectors)
                {
                    vector.TenantGUID = node.TenantGUID;
                    vector.GraphGUID = node.GraphGUID;
                    vector.NodeGUID = node.GUID;
                }

                created.Vectors = CreateMultipleVectors(node.TenantGUID, node.GraphGUID, node.Vectors);
            }

            created.Labels = node.Labels;
            created.Tags = node.Tags;
            return created;
        }

        /// <inheritdoc />
        public override Node CreateNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid guid,
            string name,
            object data = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            List<VectorMetadata> vectors = null)
        {
            return CreateNode(new Node
            {
                GUID = guid,
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Name = name,
                Labels = labels,
                Data = data,
                Tags = tags,
                Vectors = vectors
            });
        }

        /// <inheritdoc />
        public override List<Node> CreateMultipleNodes(Guid tenantGuid, Guid graphGuid, List<Node> nodes)
        {
            if (nodes == null || nodes.Count < 1) return new List<Node>();
            List<Node> created = new List<Node>();

            foreach (Node node in nodes)
            {
                node.TenantGUID = tenantGuid;
                node.GraphGUID = graphGuid;
                created.Add(CreateNode(node));
            }

            return created;
        }

        /// <inheritdoc />
        public override IEnumerable<Node> ReadNodes(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Nodes.SelectNodesQuery(tenantGuid, graphGuid, labels, tags, nodeFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Node node = Converters.NodeFromDataRow(result.Rows[i]);

                    List<LabelMetadata> allLabels = ReadLabels(tenantGuid, graphGuid, node.GUID, null, null).ToList();
                    if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                    List<TagMetadata> allTags = ReadTags(tenantGuid, graphGuid, node.GUID, null, null, null).ToList();
                    if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                    node.Vectors = ReadNodeVectors(tenantGuid, graphGuid, node.GUID).ToList();

                    yield return node;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override Node ReadNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            DataTable result = Query(Nodes.SelectNodeQuery(tenantGuid, graphGuid, nodeGuid));
            if (result != null && result.Rows.Count == 1)
            {
                Node node = Converters.NodeFromDataRow(result.Rows[0]);

                List<LabelMetadata> allLabels = ReadLabels(tenantGuid, graphGuid, nodeGuid, null, null).ToList();
                if (allLabels != null) node.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = ReadTags(tenantGuid, graphGuid, nodeGuid, null, null, null).ToList();
                if (allTags != null) node.Tags = TagMetadata.ToNameValueCollection(allTags);

                node.Vectors = ReadNodeVectors(tenantGuid, graphGuid, nodeGuid).ToList();

                return node;
            }
            return null;
        }

        /// <inheritdoc />
        public override Node UpdateNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            ValidateLabels(node.Labels);
            ValidateTags(node.Tags);
            ValidateVectors(node.Vectors);
            ValidateTenantExists(node.TenantGUID);
            ValidateGraphExists(node.TenantGUID, node.GraphGUID);
            Node updated = Converters.NodeFromDataRow(Query(Nodes.UpdateNodeQuery(node), true).Rows[0]);
            DeleteNodeLabels(node.TenantGUID, node.GraphGUID, node.GUID);
            DeleteNodeTags(node.TenantGUID, node.GraphGUID, node.GUID);
            DeleteNodeVectors(node.TenantGUID, node.GraphGUID, node.GUID);

            if (node.Labels != null)
            {
                CreateMultipleLabels(
                    node.TenantGUID,
                    node.GraphGUID,
                    LabelMetadata.FromListString(node.TenantGUID, node.GraphGUID, node.GUID, null, node.Labels));
                updated.Labels = node.Labels;
            }

            if (node.Tags != null)
            {
                CreateMultipleTags(
                    node.TenantGUID,
                    node.GraphGUID,
                    TagMetadata.FromNameValueCollection(
                        node.TenantGUID,
                        node.GraphGUID,
                        node.GUID,
                        null,
                        node.Tags));
                updated.Tags = node.Tags;
            }

            if (node.Vectors != null)
            {
                foreach (VectorMetadata vector in node.Vectors)
                {
                    vector.TenantGUID = node.TenantGUID;
                    vector.GraphGUID = node.GraphGUID;
                    vector.NodeGUID = node.GUID;
                    vector.EdgeGUID = null;
                }

                updated.Vectors = CreateMultipleVectors(node.TenantGUID, node.GraphGUID, node.Vectors);
            }

            return updated;
        }

        /// <inheritdoc />
        public override void DeleteNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            Query(Nodes.DeleteNodeQuery(tenantGuid, graphGuid, nodeGuid), true);
            DeleteNodeEdges(tenantGuid, graphGuid, nodeGuid);
            DeleteLabels(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
            DeleteTags(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
            DeleteVectors(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public override void DeleteNodes(Guid tenantGuid, Guid graphGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            Query(Nodes.DeleteNodesQuery(tenantGuid, graphGuid), true);
            DeleteEdges(tenantGuid, graphGuid);
            DeleteLabels(tenantGuid, graphGuid, null, null);
            DeleteTags(tenantGuid, graphGuid, null, null);
            DeleteVectors(tenantGuid, graphGuid, null, null);
        }

        /// <inheritdoc />
        public override void DeleteNodes(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            ValidateGraphExists(tenantGuid, graphGuid);
            Query(Nodes.DeleteNodesQuery(tenantGuid, graphGuid, nodeGuids), true);
            DeleteNodeEdges(tenantGuid, graphGuid, nodeGuids);
            DeleteLabels(tenantGuid, graphGuid, nodeGuids, null);
            DeleteTags(tenantGuid, graphGuid, nodeGuids, null);
        }

        /// <inheritdoc />
        public override void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            DeleteNodeEdges(tenantGuid, graphGuid, new List<Guid> { nodeGuid });
        }

        /// <inheritdoc />
        public override void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            ValidateGraphExists(tenantGuid, graphGuid);
            string query = Nodes.DeleteNodeEdgesQuery(tenantGuid, graphGuid, nodeGuids);

            List<Edge> edges = new List<Edge>();
            foreach (Guid nodeGuid in nodeGuids)
            {
                List<Edge> nodeEdges = GetConnectedEdges(tenantGuid, graphGuid, nodeGuid).ToList();
                if (nodeEdges != null && nodeEdges.Count > 0) edges.AddRange(nodeEdges);
            }

            lock (_CreateLock)
                Query(query);

            if (edges.Count > 0)
            {
                DeleteLabels(tenantGuid, graphGuid, null, edges.Select(e => e.GUID).ToList());
                DeleteTags(tenantGuid, graphGuid, null, edges.Select(e => e.GUID).ToList());
            }
        }

        /// <inheritdoc />
        public override void DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            DeleteLabels(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public override void DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            DeleteTags(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public override void DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            DeleteVectors(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public override bool ExistsNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (ReadNode(tenantGuid, graphGuid, nodeGuid) != null) return true;
            return false;
        }

        /// <inheritdoc />
        public override IEnumerable<Node> GetParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectConnectedEdgesQuery(tenantGuid, graphGuid, nodeGuid, null, null, null, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.To.Equals(nodeGuid))
                    {
                        Node parent = ReadNode(tenantGuid, graphGuid, edge.From);
                        if (parent != null) yield return parent;
                        else Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                    }

                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Node> GetChildren(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectConnectedEdgesQuery(tenantGuid, graphGuid, nodeGuid, null, null, null, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.From.Equals(nodeGuid))
                    {
                        Node child = ReadNode(tenantGuid, graphGuid, edge.To);
                        if (child != null) yield return child;
                        else Logging.Log(SeverityEnum.Warn, "node " + edge.To + " referenced in graph " + graphGuid + " but does not exist");
                    }

                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Node> GetNeighbors(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            List<Guid> visited = new List<Guid>();

            while (true)
            {
                DataTable result = Query(Edges.SelectConnectedEdgesQuery(tenantGuid, graphGuid, nodeGuid, null, null, null, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    if (edge.From.Equals(nodeGuid))
                    {
                        if (visited.Contains(edge.To))
                        {
                            skip++;
                            continue;
                        }
                        else
                        {
                            Node neighbor = ReadNode(tenantGuid, graphGuid, edge.To);
                            if (neighbor != null)
                            {
                                visited.Add(edge.To);
                                yield return neighbor;
                            }
                            else Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                            skip++;
                        }
                    }
                    if (edge.To.Equals(nodeGuid))
                    {
                        if (visited.Contains(edge.From))
                        {
                            skip++;
                            continue;
                        }
                        else
                        {
                            Node neighbor = ReadNode(tenantGuid, graphGuid, edge.From);
                            if (neighbor != null)
                            {
                                visited.Add(edge.From);
                                yield return neighbor;
                            }
                            else Logging.Log(SeverityEnum.Warn, "node " + edge.From + " referenced in graph " + graphGuid + " but does not exist");
                            skip++;
                        }
                    }
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<RouteDetail> GetRoutes(
            SearchTypeEnum searchType,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr edgeFilter = null,
            Expr nodeFilter = null)
        {
            #region Retrieve-Objects

            Graph graph = ReadGraph(tenantGuid, graphGuid);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");

            Node fromNode = ReadNode(tenantGuid, graphGuid, fromNodeGuid);
            if (fromNode == null) throw new ArgumentException("No node with GUID '" + fromNodeGuid + "' exists in graph '" + graphGuid + "'");

            Node toNode = ReadNode(tenantGuid, graphGuid, toNodeGuid);
            if (toNode == null) throw new ArgumentException("No node with GUID '" + toNodeGuid + "' exists in graph '" + graphGuid + "'");

            #endregion

            #region Perform-Search

            RouteDetail routeDetail = new RouteDetail();

            if (searchType == SearchTypeEnum.DepthFirstSearch)
            {
                foreach (RouteDetail route in GetRoutesDfs(
                    tenantGuid,
                    graph,
                    fromNode,
                    toNode,
                    edgeFilter,
                    nodeFilter,
                    new List<Node> { fromNode },
                    new List<Edge>()))
                {
                    if (route != null) yield return route;
                }
            }
            else
            {
                throw new ArgumentException("Unknown search type '" + searchType.ToString() + ".");
            }

            #endregion
        }

        #endregion

        #region Batch

        /// <inheritdoc />
        public override ExistenceResult BatchExistence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.ContainsExistenceRequest()) throw new ArgumentException("Supplied existence request contains no valid existence filters.");

            ValidateTenantExists(tenantGuid);
            ValidateGraphExists(tenantGuid, graphGuid);

            Graph graph = ReadGraph(tenantGuid, graphGuid);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");

            ExistenceResult resp = new ExistenceResult();

            #region Nodes

            if (req.Nodes != null)
            {
                resp.ExistingNodes = new List<Guid>();
                resp.MissingNodes = new List<Guid>();

                string nodesQuery = Nodes.BatchExistsNodesQuery(tenantGuid, graphGuid, req.Nodes);
                DataTable nodesResult = Query(nodesQuery);
                if (nodesResult != null && nodesResult.Rows != null && nodesResult.Rows.Count > 0)
                {
                    foreach (DataRow row in nodesResult.Rows)
                    {
                        if (row["exists"] != null && row["exists"] != DBNull.Value)
                        {
                            int exists = Convert.ToInt32(row["exists"]);
                            if (exists == 1)
                                resp.ExistingNodes.Add(Guid.Parse(row["guid"].ToString()));
                            else
                                resp.MissingNodes.Add(Guid.Parse(row["guid"].ToString()));
                        }
                    }
                }
            }

            #endregion

            #region Edges

            if (req.Edges != null)
            {
                resp.ExistingEdges = new List<Guid>();
                resp.MissingEdges = new List<Guid>();

                string edgesQuery = Edges.BatchExistsEdgesQuery(tenantGuid, graphGuid, req.Edges);
                DataTable edgesResult = Query(edgesQuery);
                if (edgesResult != null && edgesResult.Rows != null && edgesResult.Rows.Count > 0)
                {
                    foreach (DataRow row in edgesResult.Rows)
                    {
                        if (row["exists"] != null && row["exists"] != DBNull.Value)
                        {
                            int exists = Convert.ToInt32(row["exists"]);
                            if (exists == 1)
                                resp.ExistingEdges.Add(Guid.Parse(row["guid"].ToString()));
                            else
                                resp.MissingEdges.Add(Guid.Parse(row["guid"].ToString()));
                        }
                    }
                }
            }

            #endregion

            #region Edges-Between

            if (req.EdgesBetween != null)
            {
                resp.ExistingEdgesBetween = new List<EdgeBetween>();
                resp.MissingEdgesBetween = new List<EdgeBetween>();

                string betweenQuery = Edges.BatchExistsEdgesBetweenQuery(tenantGuid, graphGuid, req.EdgesBetween);
                DataTable betweenResult = Query(betweenQuery);
                if (betweenResult != null && betweenResult.Rows != null && betweenResult.Rows.Count > 0)
                {
                    foreach (DataRow row in betweenResult.Rows)
                    {
                        if (row["exists"] != null && row["exists"] != DBNull.Value)
                        {
                            int exists = Convert.ToInt32(row["exists"]);
                            if (exists == 1)
                                resp.ExistingEdgesBetween.Add(new EdgeBetween
                                {
                                    From = Guid.Parse(row["fromguid"].ToString()),
                                    To = Guid.Parse(row["toguid"].ToString())
                                });
                            else
                                resp.MissingEdgesBetween.Add(new EdgeBetween
                                {
                                    From = Guid.Parse(row["fromguid"].ToString()),
                                    To = Guid.Parse(row["toguid"].ToString())
                                });
                        }
                    }
                }
            }

            #endregion

            return resp;
        }

        #endregion

        #region Edges

        /// <inheritdoc />
        public override IEnumerable<Edge> GetConnectedEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectConnectedEdgesQuery(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    edge.Labels = LabelMetadata.ToListString(ReadLabels(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                    edge.Tags = TagMetadata.ToNameValueCollection(ReadTags(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                    edge.Vectors = ReadEdgeVectors(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Edge> GetEdgesFrom(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectEdgesFromQuery(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    edge.Labels = LabelMetadata.ToListString(ReadLabels(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                    edge.Tags = TagMetadata.ToNameValueCollection(ReadTags(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                    edge.Vectors = ReadEdgeVectors(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Edge> GetEdgesTo(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectEdgesToQuery(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    edge.Labels = LabelMetadata.ToListString(ReadLabels(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                    edge.Tags = TagMetadata.ToNameValueCollection(ReadTags(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                    edge.Vectors = ReadEdgeVectors(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Edge> GetEdgesBetween(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectEdgesBetweenQuery(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, labels, tags, edgeFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    edge.Labels = LabelMetadata.ToListString(ReadLabels(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null).ToList());
                    edge.Tags = TagMetadata.ToNameValueCollection(ReadTags(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                    edge.Vectors = ReadEdgeVectors(edge.TenantGUID, edge.GraphGUID, edge.GUID).ToList();
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override Edge CreateEdge(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            ValidateLabels(edge.Labels);
            ValidateTags(edge.Tags);
            ValidateVectors(edge.Vectors);
            ValidateTenantExists(edge.TenantGUID);
            ValidateGraphExists(edge.TenantGUID, edge.GraphGUID);

            string insertQuery = Edges.InsertEdgeQuery(edge);
            DataTable createResult = null;
            Edge created = null;
            Edge existing = null;

            lock (_CreateLock)
            {
                existing = ReadEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID);
                if (existing != null) return existing;
                createResult = Query(insertQuery, true);
            }

            created = Converters.EdgeFromDataRow(createResult.Rows[0]);

            if (edge.Labels != null)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, edge.Labels);
                if (labels.Count > 0) CreateMultipleLabels(edge.TenantGUID, edge.GraphGUID, labels);
            }

            if (edge.Tags != null)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, edge.Tags);
                if (tags.Count > 0) CreateMultipleTags(edge.TenantGUID, edge.GraphGUID, tags);
            }

            if (edge.Vectors != null && edge.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in edge.Vectors)
                {
                    vector.TenantGUID = edge.TenantGUID;
                    vector.GraphGUID = edge.GraphGUID;
                    vector.EdgeGUID = edge.GUID;
                }

                created.Vectors = CreateMultipleVectors(edge.TenantGUID, edge.GraphGUID, edge.Vectors);
            }

            created.Labels = edge.Labels;
            created.Tags = edge.Tags;
            return created;
        }

        /// <inheritdoc />
        public override Edge CreateEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid guid,
            Guid fromGuid,
            Guid toGuid,
            string name,
            int cost = 0,
            object data = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            List<VectorMetadata> vectors = null)
        {
            return CreateEdge(new Edge
            {
                GUID = guid,
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                From = fromGuid,
                To = toGuid,
                Name = name,
                Cost = cost,
                Data = data,
                Labels = labels,
                Tags = tags,
                Vectors = vectors
            });
        }

        /// <inheritdoc />
        public override List<Edge> CreateMultipleEdges(Guid tenantGuid, Guid graphGuid, List<Edge> edges)
        {
            if (edges == null || edges.Count < 1) return new List<Edge>();
            List<Edge> created = new List<Edge>();

            foreach (Edge edge in edges)
            {
                edge.TenantGUID = tenantGuid;
                edge.GraphGUID = graphGuid;
                created.Add(CreateEdge(edge));
            }

            return created;
        }

        /// <inheritdoc />
        public override IEnumerable<Edge> ReadEdges(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = Query(Edges.SelectEdgesQuery(tenantGuid, graphGuid, labels, tags, edgeFilter, SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);

                    List<LabelMetadata> allLabels = ReadLabels(tenantGuid, graphGuid, null, edge.GUID, null).ToList();
                    if (allLabels != null) edge.Labels = LabelMetadata.ToListString(allLabels);

                    List<TagMetadata> allTags = ReadTags(tenantGuid, graphGuid, null, edge.GUID, null, null).ToList();
                    if (allTags != null) edge.Tags = TagMetadata.ToNameValueCollection(allTags);

                    edge.Vectors = ReadEdgeVectors(tenantGuid, graphGuid, edge.GUID).ToList();

                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public override Edge ReadEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            DataTable result = Query(Edges.SelectEdgeQuery(tenantGuid, graphGuid, edgeGuid));
            if (result != null && result.Rows.Count == 1)
            {
                Edge edge = Converters.EdgeFromDataRow(result.Rows[0]);

                List<LabelMetadata> allLabels = ReadLabels(tenantGuid, graphGuid, null, edgeGuid, null).ToList();
                if (allLabels != null) edge.Labels = LabelMetadata.ToListString(allLabels);

                List<TagMetadata> allTags = ReadTags(tenantGuid, graphGuid, null, edgeGuid, null, null).ToList();
                if (allTags != null) edge.Tags = TagMetadata.ToNameValueCollection(allTags);

                edge.Vectors = ReadEdgeVectors(tenantGuid, graphGuid, edge.GUID).ToList();

                return edge;
            }
            return null;
        }

        /// <inheritdoc />
        public override Edge UpdateEdge(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            ValidateLabels(edge.Labels);
            ValidateTags(edge.Tags);
            ValidateVectors(edge.Vectors);
            ValidateTenantExists(edge.TenantGUID);
            ValidateGraphExists(edge.TenantGUID, edge.GraphGUID);
            Edge updated = Converters.EdgeFromDataRow(Query(Edges.UpdateEdgeQuery(edge), true).Rows[0]);
            DeleteEdgeLabels(edge.TenantGUID, edge.GraphGUID, edge.GUID);
            DeleteEdgeTags(edge.TenantGUID, edge.GraphGUID, edge.GUID);
            DeleteEdgeVectors(edge.TenantGUID, edge.GraphGUID, edge.GUID);

            if (edge.Labels != null)
            {
                CreateMultipleLabels(
                    edge.TenantGUID,
                    edge.GraphGUID,
                    LabelMetadata.FromListString(
                        edge.TenantGUID,
                        edge.GraphGUID,
                        null,
                        edge.GUID,
                        edge.Labels));
                updated.Labels = edge.Labels;
            }

            if (edge.Tags != null)
            {
                CreateMultipleTags(
                    edge.TenantGUID,
                    edge.GraphGUID,
                    TagMetadata.FromNameValueCollection(
                        edge.TenantGUID,
                        edge.GraphGUID,
                        null,
                        edge.GUID,
                        edge.Tags));
                updated.Tags = edge.Tags;
            }

            if (edge.Vectors != null)
            {
                foreach (VectorMetadata vector in edge.Vectors)
                {
                    vector.TenantGUID = edge.TenantGUID;
                    vector.GraphGUID = edge.GraphGUID;
                    vector.NodeGUID = null;
                    vector.EdgeGUID = edge.GUID;
                }

                updated.Vectors = CreateMultipleVectors(edge.TenantGUID, edge.GraphGUID, edge.Vectors);
            }

            return updated;
        }

        /// <inheritdoc />
        public override void DeleteEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            Query(Edges.DeleteEdgeQuery(tenantGuid, graphGuid, edgeGuid), true);
            DeleteLabels(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
            DeleteTags(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
            DeleteVectors(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public override void DeleteEdges(Guid tenantGuid, Guid graphGuid)
        {
            ValidateGraphExists(tenantGuid, graphGuid);
            List<Edge> edges = ReadEdges(tenantGuid, graphGuid).ToList();
            if (edges != null && edges.Count > 0)
            {
                Query(Edges.DeleteEdgesQuery(tenantGuid, graphGuid), true);
                if (edges != null && edges.Count > 0)
                {
                    DeleteLabels(tenantGuid, graphGuid, null, edges.Select(e => e.GUID).ToList());
                    DeleteTags(tenantGuid, graphGuid, null, edges.Select(e => e.GUID).ToList());
                    DeleteVectors(tenantGuid, graphGuid, null, edges.Select(e => e.GUID).ToList());
                }
            }
        }

        /// <inheritdoc />
        public override void DeleteEdges(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            if (edgeGuids == null || edgeGuids.Count < 1) return;
            ValidateGraphExists(tenantGuid, graphGuid);
            Query(Edges.DeleteEdgesQuery(tenantGuid, graphGuid, edgeGuids), true);
            DeleteLabels(tenantGuid, graphGuid, null, edgeGuids);
            DeleteTags(tenantGuid, graphGuid, null, edgeGuids);
            DeleteVectors(tenantGuid, graphGuid, null, edgeGuids);
        }

        /// <inheritdoc />
        public override void DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            DeleteLabels(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public override void DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            DeleteTags(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public override void DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            DeleteVectors(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public override bool ExistsEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            if (ReadEdge(tenantGuid, graphGuid, edgeGuid) != null) return true;
            return false;
        }

        #endregion

        #endregion

        #region Private-Methods

        #region General

        private DataTable Query(string query, bool isTransaction = false)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(query);
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            DataTable result = new DataTable();

            if (isTransaction)
            {
                query = query.Trim();
                query = "BEGIN TRANSACTION; " + query + " END TRANSACTION;";
            }

            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

            lock (_QueryLock)
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    try
                    {
                        conn.Open();

                        using (SqliteCommand cmd = new SqliteCommand(query, conn))
                        {
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                result.Load(rdr);
                            }
                        }

                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        if (isTransaction)
                        {
                            using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", conn))
                                cmd.ExecuteNonQuery();
                        }

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Query", query);
                        throw;
                    }
                }
            }

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        #endregion

        #region Traversal-and-Routing

        private IEnumerable<RouteDetail> GetRoutesDfs(
            Guid tenantGuid,
            Graph graph,
            Node start,
            Node end,
            Expr edgeFilter,
            Expr nodeFilter,
            List<Node> visitedNodes,
            List<Edge> visitedEdges) 
        {
            #region Get-Edges

            List<Edge> edges = GetEdgesFrom(tenantGuid, graph.GUID, start.GUID, null, null, edgeFilter, EnumerationOrderEnum.CreatedDescending).ToList();

            #endregion

            #region Process-Each-Edge

            for (int i = 0; i < edges.Count; i++)
            {
                Edge nextEdge = edges[i];

                #region Retrieve-Next-Node

                Node nextNode = ReadNode(tenantGuid, graph.GUID, nextEdge.To);
                if (nextNode == null)
                {
                    Logging.Log(SeverityEnum.Warn, "node " + nextEdge.To + " referenced in graph " + graph.GUID + " but does not exist");
                    continue;
                }

                #endregion

                #region Check-for-End

                if (nextNode.GUID.Equals(end.GUID))
                {
                    RouteDetail routeDetail = new RouteDetail();
                    routeDetail.Edges = new List<Edge>(visitedEdges);
                    routeDetail.Edges.Add(nextEdge);
                    yield return routeDetail;
                    continue;
                }

                #endregion

                #region Check-for-Cycles

                if (visitedNodes.Any(n => n.GUID.Equals(nextEdge.To)))
                {
                    continue; // cycle
                }

                #endregion

                #region Recursion-and-Variables

                List<Node> childVisitedNodes = new List<Node>(visitedNodes);
                List<Edge> childVisitedEdges = new List<Edge>(visitedEdges);

                childVisitedNodes.Add(nextNode);
                childVisitedEdges.Add(nextEdge);

                IEnumerable<RouteDetail> recurse = GetRoutesDfs(tenantGuid, graph, nextNode, end, edgeFilter, nodeFilter, childVisitedNodes, childVisitedEdges);
                foreach (RouteDetail route in recurse)
                {
                    if (route != null) yield return route;
                }

                #endregion
            }

            #endregion
        }

        #endregion

        #region Validators

        private void ValidateLabels(List<string> labels)
        {
            if (labels == null) return;
            foreach (string label in labels)
                if (String.IsNullOrEmpty(label)) throw new ArgumentException("The supplied labels contains a null or empty label.");
        }

        private void ValidateTags(NameValueCollection tags)
        {
            if (tags == null) return;
            foreach (string key in tags.AllKeys)
                if (String.IsNullOrEmpty(key)) throw new ArgumentException("The supplied tags contains a null or empty key.");
        }

        private void ValidateVectors(List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count < 1) return;
            foreach (VectorMetadata vector in vectors)
            {
                if (String.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector object does not include a model.");
                if (vector.Dimensionality <= 0) throw new ArgumentException("The supplied vector object dimensionality must be greater than zero.");
                if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object does not include any vectors.");
                if (String.IsNullOrEmpty(vector.Content)) throw new ArgumentException("The supplied vector object does not contain any content.");
            }
        }

        private void ValidateTenantExists(Guid tenantGuid)
        {
            if (!ExistsTenant(tenantGuid))
                throw new ArgumentException("No tenant with GUID '" + tenantGuid + "' exists.");
        }

        private void ValidateUserExists(Guid tenantGuid, Guid userGuid)
        {
            if (!ExistsUser(tenantGuid, userGuid))
                throw new ArgumentException("No user with GUID '" + userGuid + "' exists.");
        }

        private void ValidateGraphExists(Guid tenantGuid, Guid? graphGuid)  
        {
            if (graphGuid == null) return;
            if (!ExistsGraph(tenantGuid, graphGuid.Value))
                throw new ArgumentException("No graph with GUID '" + graphGuid.Value + "' exists.");
        }

        private void ValidateNodeExists(Guid tenantGuid, Guid? graphGuid, Guid? nodeGuid)
        {
            if (graphGuid == null) return;
            if (nodeGuid == null) return;
            if (!ExistsNode(tenantGuid, graphGuid.Value, nodeGuid.Value))
                throw new ArgumentException("No node with GUID '" + nodeGuid.Value + "' exists.");
        }

        private void ValidateEdgeExists(Guid tenantGuid, Guid? graphGuid, Guid? edgeGuid)
        {
            if (graphGuid == null) return;
            if (edgeGuid == null) return;
            if (!ExistsEdge(tenantGuid, graphGuid.Value, edgeGuid.Value))
                throw new ArgumentException("No edge with GUID '" + edgeGuid.Value + "' exists.");
        }

        #endregion

        #region Vectors

        private void CompareVectors(
            VectorSearchTypeEnum searchType, 
            List<float> vectors1, 
            List<float> vectors2,
            out float? score,
            out float? distance,
            out float? innerProduct)
        {
            score = null;
            distance = null;
            innerProduct = null;

            if (searchType == VectorSearchTypeEnum.CosineDistance)
                distance = VectorHelper.CalculateCosineDistance(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.CosineSimilarity)
                score = VectorHelper.CalculateCosineSimilarity(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.DotProduct)
                innerProduct = VectorHelper.CalculateInnerProduct(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.EuclidianDistance)
                distance = VectorHelper.CalculateEuclidianDistance(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.EuclidianSimilarity)
                score = VectorHelper.CalculateEuclidianSimilarity(vectors1, vectors2);
            else
            {
                throw new ArgumentException("Unknown vector search type " + searchType.ToString() + ".");
            }
        }

        #endregion

        #endregion

#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
    }
}
