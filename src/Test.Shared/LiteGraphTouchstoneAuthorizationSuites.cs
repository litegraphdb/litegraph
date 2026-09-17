namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        #region Authorization-Suite

        private const string _AdminBearerToken = "litegraphadmin";
        private static readonly string _DefaultTenantGuid = "00000000-0000-0000-0000-000000000000";
        private static readonly string _DefaultUserGuid = "00000000-0000-0000-0000-000000000000";

        private static TestSuiteDescriptor CreateAuthorizationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Authorization",
                displayName: "v8 unified account authorization matrix and settings API",
                cases: new List<TestCaseDescriptor>
                {
                    Authz("Authorization.SystemAdminFullAccess", "System administrator can manage tenants, users, and settings", TestSystemAdminFullAccess),
                    Authz("Authorization.TenantAdminScope", "Tenant administrator manages its own tenant but not settings or other tenants", TestTenantAdminScope),
                    Authz("Authorization.RegularUserSelfService", "Regular user can self-service but cannot list or reach other users", TestRegularUserSelfService),
                    Authz("Authorization.UnauthenticatedDenied", "Unauthenticated requests are denied", TestUnauthenticatedDenied),
                    Authz("Authorization.SettingsRoundTrip", "System administrator can read, update, and read back settings", TestSettingsRoundTrip),
                    Authz("Authorization.SettingsDeniedForNonAdmin", "Settings endpoints deny tenant admins and regular users", TestSettingsDeniedForNonAdmin),
                    Authz("Authorization.AlgorithmScope", "Read-scoped credential can run algorithms and export but not write back or import", TestAlgorithmScope),
                    Authz("Authorization.SuccessfulPrivilegedActionAudited", "Permitted write/admin actions are audited; reads are not; denials remain audited", TestSuccessfulPrivilegedActionAudited),
                    Authz("Authorization.QueryScopeFailsClosed", "Query scope is parsed authoritatively; unparseable queries fail closed (400), not keyword-guessed", TestQueryScopeFailsClosed)
                });
        }

        private static TestCaseDescriptor Authz(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: "Authorization", caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region Authorization-Cases

        private static async Task TestAlgorithmScope(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = RequireEndpoint();

            HttpOutcome graphCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs", _AdminBearerToken,
                "{\"Name\":\"algo-authz-graph\"}", cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(graphCreated.Status), "Algorithm authz graph created (status " + graphCreated.Status + ")");
            string graphGuid = ExtractGuid(graphCreated.Body);

            string nodesUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/nodes";
            await AuthRestAsync(HttpMethod.Put, nodesUrl, _AdminBearerToken, "{\"Name\":\"A\"}", cancellationToken).ConfigureAwait(false);
            await AuthRestAsync(HttpMethod.Put, nodesUrl, _AdminBearerToken, "{\"Name\":\"B\"}", cancellationToken).ConfigureAwait(false);

            string? readerUserGuid = null;
            await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "algo-authz-reader@authz.test", isSystemAdmin: false, isTenantAdmin: false, cancellationToken, capturedGuid => readerUserGuid = capturedGuid).ConfigureAwait(false);
            AssertTrue(!String.IsNullOrEmpty(readerUserGuid), "Reader user provisioned");

            string readToken = "algo-authz-read-" + Guid.NewGuid().ToString("N");
            HttpOutcome credentialCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/credentials", _AdminBearerToken,
                "{\"UserGUID\":\"" + readerUserGuid + "\",\"Name\":\"Algorithm read-only\",\"BearerToken\":\"" + readToken + "\",\"Scopes\":[\"read\"],\"Active\":true}", cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(credentialCreated.Status), "Read-only credential created (status " + credentialCreated.Status + " body " + credentialCreated.Body + ")");

            string algoUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/algorithms";
            string exportUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/export/projection?format=NodeLinkJson&attributes=Meta";
            string importUrl = algoUrl + "/import";

            HttpOutcome compute = await AuthRestAsync(HttpMethod.Post, algoUrl, readToken, "{\"AlgorithmType\":\"PageRank\"}", cancellationToken).ConfigureAwait(false);
            AssertEqual(200, compute.Status, "Read scope permits algorithm compute (body " + compute.Body + ")");

            HttpOutcome export = await AuthRestAsync(HttpMethod.Get, exportUrl, readToken, null, cancellationToken).ConfigureAwait(false);
            AssertEqual(200, export.Status, "Read scope permits projection export");

            HttpOutcome writeBack = await AuthRestAsync(HttpMethod.Post, algoUrl, readToken, "{\"AlgorithmType\":\"PageRank\",\"WriteBack\":true}", cancellationToken).ConfigureAwait(false);
            AssertTrue(writeBack.Status == 401 || writeBack.Status == 403, "Read scope denies write-back (status " + writeBack.Status + ")");

            HttpOutcome import = await AuthRestAsync(HttpMethod.Post, importUrl, readToken, "{\"Values\":{}}", cancellationToken).ConfigureAwait(false);
            AssertTrue(import.Status == 401 || import.Status == 403, "Read scope denies results import (status " + import.Status + ")");

            HttpOutcome adminWriteBack = await AuthRestAsync(HttpMethod.Post, algoUrl, _AdminBearerToken, "{\"AlgorithmType\":\"PageRank\",\"WriteBack\":true}", cancellationToken).ConfigureAwait(false);
            AssertEqual(200, adminWriteBack.Status, "Admin permits write-back (body " + adminWriteBack.Body + ")");
        }

        private static async Task TestSuccessfulPrivilegedActionAudited(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

                // Positive case: a permitted write (graph create) must produce a 'Permitted' audit entry.
                HttpOutcome graphCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs", _AdminBearerToken,
                    "{\"Name\":\"audit-success-graph\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(graphCreated.Status), "Audit positive-case graph created (status " + graphCreated.Status + ")");

                // Negative case: a read (list graphs) must NOT be audited.
                HttpOutcome listGraphs = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, listGraphs.Status, "Audit negative-case graph list succeeds");

                // Denial case: a read-only credential attempting a write is denied and audited as 'Denied'.
                string? readerUserGuid = null;
                await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "audit-reader@authz.test", isSystemAdmin: false, isTenantAdmin: false, cancellationToken, capturedGuid => readerUserGuid = capturedGuid).ConfigureAwait(false);
                AssertTrue(!String.IsNullOrEmpty(readerUserGuid), "Audit reader user provisioned");

                string readToken = "audit-read-" + Guid.NewGuid().ToString("N");
                HttpOutcome credentialCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/credentials", _AdminBearerToken,
                    "{\"UserGUID\":\"" + readerUserGuid + "\",\"Name\":\"Audit read-only\",\"BearerToken\":\"" + readToken + "\",\"Scopes\":[\"read\"],\"Active\":true}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(credentialCreated.Status), "Audit read-only credential created (status " + credentialCreated.Status + ")");

                HttpOutcome deniedWrite = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs", readToken,
                    "{\"Name\":\"audit-denied-graph\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(deniedWrite.Status == 401 || deniedWrite.Status == 403, "Audit denial-case write is denied (status " + deniedWrite.Status + ")");

                // Audit records are written after the response is sent (PostRouting), so poll the shared store.
                List<AuthorizationAuditEntry> graphCreateEntries = await PollAuthorizationAuditAsync(
                    "GraphCreate",
                    entries => entries.Any(e => "Permitted".Equals(e.AuthorizationResult, StringComparison.OrdinalIgnoreCase))
                        && entries.Any(e => "Denied".Equals(e.AuthorizationResult, StringComparison.OrdinalIgnoreCase)),
                    cancellationToken).ConfigureAwait(false);

                AuthorizationAuditEntry? permitted = graphCreateEntries.FirstOrDefault(e =>
                    "Permitted".Equals(e.AuthorizationResult, StringComparison.OrdinalIgnoreCase));
                AssertTrue(permitted != null, "Permitted write action produced an audit entry");
                AssertEqual("write", permitted!.RequiredScope, "Permitted graph-create audit records write scope");
                AssertEqual(200, permitted.StatusCode, "Permitted graph-create audit records success status");

                AuthorizationAuditEntry? denied = graphCreateEntries.FirstOrDefault(e =>
                    "Denied".Equals(e.AuthorizationResult, StringComparison.OrdinalIgnoreCase));
                AssertTrue(denied != null, "Denied write action produced an audit entry");
                AssertTrue(denied!.StatusCode == 401 || denied.StatusCode == 403, "Denied graph-create audit records a denial status (status " + denied.StatusCode + ")");

                // Reads must never be audited: no entry, permitted or denied, may carry read scope.
                using (LiteGraphClient verifyClient = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings { Filename = _McpEnvironment.DatabasePath })))
                {
                    verifyClient.InitializeRepository();
                    AuthorizationAuditSearchResult all = await verifyClient.AuthorizationAudit.Search(
                        new AuthorizationAuditSearchRequest { PageSize = 1000 }, cancellationToken).ConfigureAwait(false);
                    AssertTrue(all.Objects.Count > 0, "Audit store contains records after privileged actions");
                    AssertTrue(all.Objects.All(e => !"read".Equals(e.RequiredScope, StringComparison.OrdinalIgnoreCase)),
                        "No read-scope request was audited (" + all.Objects.Count(e => "read".Equals(e.RequiredScope, StringComparison.OrdinalIgnoreCase)) + " read entries found)");
                }
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestQueryScopeFailsClosed(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();

                HttpOutcome graphCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs", _AdminBearerToken,
                    "{\"Name\":\"query-failsclosed-graph\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(graphCreated.Status), "Query fail-closed graph created (status " + graphCreated.Status + ")");
                string graphGuid = ExtractGuid(graphCreated.Body);

                await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/nodes", _AdminBearerToken,
                    "{\"Name\":\"A\"}", cancellationToken).ConfigureAwait(false);

                string? readerUserGuid = null;
                await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "query-reader@authz.test", isSystemAdmin: false, isTenantAdmin: false, cancellationToken, capturedGuid => readerUserGuid = capturedGuid).ConfigureAwait(false);
                AssertTrue(!String.IsNullOrEmpty(readerUserGuid), "Query reader user provisioned");

                string readToken = "query-read-" + Guid.NewGuid().ToString("N");
                HttpOutcome credentialCreated = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/credentials", _AdminBearerToken,
                    "{\"UserGUID\":\"" + readerUserGuid + "\",\"Name\":\"Query read-only\",\"BearerToken\":\"" + readToken + "\",\"Scopes\":[\"read\"],\"Active\":true}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(credentialCreated.Status), "Query read-only credential created (status " + credentialCreated.Status + ")");

                string queryUrl = endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/graphs/" + graphGuid + "/query";

                // Positive: read-only credential runs a valid read query.
                HttpOutcome readQuery = await AuthRestAsync(HttpMethod.Post, queryUrl, readToken,
                    "{\"Query\":\"MATCH (n) RETURN n\"}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readQuery.Status, "Read scope permits a valid read query (body " + readQuery.Body + ")");

                // Negative (authorization still enforced): read-only credential is denied a valid mutation query.
                HttpOutcome writeQuery = await AuthRestAsync(HttpMethod.Post, queryUrl, readToken,
                    "{\"Query\":\"CREATE (n:Person { name: 'Ada' }) RETURN n\"}", cancellationToken).ConfigureAwait(false);
                AssertTrue(writeQuery.Status == 401 || writeQuery.Status == 403, "Read scope denies a valid mutation query (status " + writeQuery.Status + ")");

                // Negative (fail closed): an unparseable query — even one containing a mutation substring ('SET') —
                // is rejected with a 400 decided before authorization, not keyword-classified and not executed into a 500.
                string bogusBody = "{\"Query\":\"MATCH (n) WHERE n.asset = 'SET' RETURN\"}";
                HttpOutcome bogusAsAdmin = await AuthRestAsync(HttpMethod.Post, queryUrl, _AdminBearerToken, bogusBody, cancellationToken).ConfigureAwait(false);
                AssertEqual(400, bogusAsAdmin.Status, "Unparseable query fails closed as 400 for admin (status " + bogusAsAdmin.Status + " body " + bogusAsAdmin.Body + ")");

                HttpOutcome bogusAsReader = await AuthRestAsync(HttpMethod.Post, queryUrl, readToken, bogusBody, cancellationToken).ConfigureAwait(false);
                AssertEqual(400, bogusAsReader.Status, "Unparseable query fails closed as 400 for read-only credential (status " + bogusAsReader.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task<List<AuthorizationAuditEntry>> PollAuthorizationAuditAsync(
            string requestType,
            Func<List<AuthorizationAuditEntry>, bool> predicate,
            CancellationToken cancellationToken)
        {
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

            List<AuthorizationAuditEntry> latest = new List<AuthorizationAuditEntry>();
            for (int attempt = 0; attempt < 40; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (LiteGraphClient verifyClient = new LiteGraphClient(GraphRepositoryFactory.Create(new DatabaseSettings { Filename = _McpEnvironment.DatabasePath })))
                {
                    verifyClient.InitializeRepository();
                    AuthorizationAuditSearchResult result = await verifyClient.AuthorizationAudit.Search(
                        new AuthorizationAuditSearchRequest { RequestType = requestType, PageSize = 1000 }, cancellationToken).ConfigureAwait(false);
                    latest = result.Objects;
                }

                if (predicate(latest)) return latest;
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }

            return latest;
        }

        private static async Task TestSystemAdminFullAccess(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();

                HttpOutcome listUsers = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, listUsers.Status, "System administrator can list users");

                HttpOutcome readSettings = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/settings", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readSettings.Status, "System administrator can read settings");

                HttpOutcome createTenant = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants", _AdminBearerToken, "{\"Name\":\"Authz second tenant\",\"Active\":true}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(createTenant.Status), "System administrator can create a tenant (status " + createTenant.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestTenantAdminScope(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string tenantAdminBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "tenant-admin@authz.test", isSystemAdmin: false, isTenantAdmin: true, cancellationToken).ConfigureAwait(false);
                string otherTenantGuid = await ProvisionTenantAsync(endpoint, "Authz other tenant", cancellationToken).ConfigureAwait(false);

                HttpOutcome listOwnUsers = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users", tenantAdminBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, listOwnUsers.Status, "Tenant administrator can list users in its own tenant");

                HttpOutcome createOwnUser = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users", tenantAdminBearer, "{\"FirstName\":\"Made\",\"LastName\":\"ByAdmin\",\"Email\":\"madebyadmin@authz.test\",\"Password\":\"password\",\"Active\":true}", cancellationToken).ConfigureAwait(false);
                AssertTrue(IsSuccess(createOwnUser.Status), "Tenant administrator can create a user in its own tenant (status " + createOwnUser.Status + ")");

                HttpOutcome readSettings = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/settings", tenantAdminBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(401, readSettings.Status, "Tenant administrator cannot read settings");

                HttpOutcome createTenant = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants", tenantAdminBearer, "{\"Name\":\"Should not exist\",\"Active\":true}", cancellationToken).ConfigureAwait(false);
                AssertEqual(401, createTenant.Status, "Tenant administrator cannot create a tenant");

                HttpOutcome listOtherUsers = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + otherTenantGuid + "/users", tenantAdminBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(401, listOtherUsers.Status, "Tenant administrator cannot list users in another tenant");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestRegularUserSelfService(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string? regularUserGuid = null;
                string regularBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "regular@authz.test", isSystemAdmin: false, isTenantAdmin: false, cancellationToken, capturedGuid => regularUserGuid = capturedGuid).ConfigureAwait(false);
                AssertTrue(!String.IsNullOrEmpty(regularUserGuid), "Regular user GUID was captured during provisioning");

                HttpOutcome listUsers = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users", regularBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(401, listUsers.Status, "Regular user cannot list users");

                HttpOutcome readSelf = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users/" + regularUserGuid, regularBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readSelf.Status, "Regular user can read its own record");

                HttpOutcome updateSelf = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users/" + regularUserGuid, regularBearer, "{\"FirstName\":\"Renamed\",\"LastName\":\"Self\",\"Email\":\"regular@authz.test\",\"Password\":\"password\",\"Active\":true}", cancellationToken).ConfigureAwait(false);
                AssertEqual(200, updateSelf.Status, "Regular user can update its own record");

                HttpOutcome readOther = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users/" + _DefaultUserGuid, regularBearer, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(401, readOther.Status, "Regular user cannot read another user");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestUnauthenticatedDenied(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();

                HttpOutcome listUsers = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + _DefaultTenantGuid + "/users", null, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(401, listUsers.Status, "Unauthenticated user listing is denied");

                HttpOutcome readSettings = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/settings", null, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(401, readSettings.Status, "Unauthenticated settings read is denied");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestSettingsRoundTrip(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();

                HttpOutcome read = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/settings", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, read.Status, "System administrator can read settings");

                JsonNode settings = JsonNode.Parse(read.Body) ?? throw new InvalidOperationException("Settings body was empty.");
                JsonNode? timeoutNode = settings["RequestTimeoutSeconds"];
                int currentTimeout = timeoutNode != null ? timeoutNode.GetValue<int>() : 60;
                int newTimeout = currentTimeout == 45 ? 46 : 45;
                settings["RequestTimeoutSeconds"] = newTimeout;

                HttpOutcome update = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/settings", _AdminBearerToken, settings.ToJsonString(), cancellationToken).ConfigureAwait(false);
                AssertEqual(200, update.Status, "System administrator can update settings");
                AssertTrue(update.Body.Contains("\"Success\":true") || update.Body.Contains("\"Success\": true"), "Settings update reports success");
                AssertTrue(update.Body.Contains("RequestTimeoutSeconds"), "Settings update lists RequestTimeoutSeconds as applied live");

                HttpOutcome readBack = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/settings", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readBack.Status, "Settings can be read back after update");
                JsonNode readBackSettings = JsonNode.Parse(readBack.Body) ?? throw new InvalidOperationException("Settings read-back body was empty.");
                JsonNode? persistedNode = readBackSettings["RequestTimeoutSeconds"] ?? throw new InvalidOperationException("Settings read-back did not contain RequestTimeoutSeconds.");
                int persistedTimeout = persistedNode.GetValue<int>();
                AssertEqual(newTimeout, persistedTimeout, "The updated RequestTimeoutSeconds was applied live and read back");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestSettingsDeniedForNonAdmin(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string tenantAdminBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "settings-tenantadmin@authz.test", isSystemAdmin: false, isTenantAdmin: true, cancellationToken).ConfigureAwait(false);
                string regularBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "settings-regular@authz.test", isSystemAdmin: false, isTenantAdmin: false, cancellationToken).ConfigureAwait(false);

                HttpOutcome tenantAdminUpdate = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/settings", tenantAdminBearer, "{\"RequestTimeoutSeconds\":30}", cancellationToken).ConfigureAwait(false);
                AssertEqual(401, tenantAdminUpdate.Status, "Tenant administrator cannot update settings");

                HttpOutcome regularUpdate = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/settings", regularBearer, "{\"RequestTimeoutSeconds\":30}", cancellationToken).ConfigureAwait(false);
                AssertEqual(401, regularUpdate.Status, "Regular user cannot update settings");

                HttpOutcome regularRestart = await AuthRestAsync(HttpMethod.Post, endpoint + "/v1.0/settings/restart", regularBearer, "", cancellationToken).ConfigureAwait(false);
                AssertEqual(401, regularRestart.Status, "Regular user cannot restart the server");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        #endregion

        #region Authorization-Helpers

        private static string RequireEndpoint()
        {
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");
            return _McpEnvironment.LiteGraphEndpoint;
        }

        private static bool IsSuccess(int status)
        {
            return status >= 200 && status < 300;
        }

        private static async Task<string> ProvisionTenantAsync(string endpoint, string name, CancellationToken cancellationToken)
        {
            HttpOutcome outcome = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants", _AdminBearerToken, "{\"Name\":\"" + name + "\",\"Active\":true}", cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(outcome.Status), "Provisioned tenant '" + name + "' (status " + outcome.Status + ")");
            return ExtractGuid(outcome.Body);
        }

        private static async Task<string> ProvisionUserAsync(
            string endpoint,
            string tenantGuid,
            string email,
            bool isSystemAdmin,
            bool isTenantAdmin,
            CancellationToken cancellationToken,
            Action<string>? capturedGuid = null)
        {
            string userBody = "{"
                + "\"FirstName\":\"Authz\","
                + "\"LastName\":\"Principal\","
                + "\"Email\":\"" + email + "\","
                + "\"Password\":\"password\","
                + "\"Active\":true,"
                + "\"IsSystemAdmin\":" + (isSystemAdmin ? "true" : "false") + ","
                + "\"IsTenantAdmin\":" + (isTenantAdmin ? "true" : "false")
                + "}";

            HttpOutcome userOutcome = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + tenantGuid + "/users", _AdminBearerToken, userBody, cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(userOutcome.Status), "Provisioned user '" + email + "' (status " + userOutcome.Status + ")");
            string userGuid = ExtractGuid(userOutcome.Body);
            if (capturedGuid != null) capturedGuid(userGuid);

            string bearerToken = "authz-" + userGuid;
            string credentialBody = "{"
                + "\"UserGUID\":\"" + userGuid + "\","
                + "\"Name\":\"Authz credential for " + email + "\","
                + "\"BearerToken\":\"" + bearerToken + "\","
                + "\"Active\":true"
                + "}";

            HttpOutcome credentialOutcome = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/" + tenantGuid + "/credentials", _AdminBearerToken, credentialBody, cancellationToken).ConfigureAwait(false);
            AssertTrue(IsSuccess(credentialOutcome.Status), "Provisioned credential for '" + email + "' (status " + credentialOutcome.Status + ")");

            return bearerToken;
        }

        private static string ExtractGuid(string body)
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                return document.RootElement.GetProperty("GUID").GetString() ?? throw new InvalidOperationException("Response did not contain a GUID.");
            }
        }

        private static async Task<HttpOutcome> AuthRestAsync(
            HttpMethod method,
            string url,
            string? bearerToken,
            string? jsonBody,
            CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                if (!String.IsNullOrEmpty(bearerToken)) request.Headers.Add("Authorization", "Bearer " + bearerToken);
                if (jsonBody != null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await _AuthorizationClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    HttpOutcome outcome = new HttpOutcome();
                    outcome.Status = (int)response.StatusCode;
                    outcome.Body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    return outcome;
                }
            }
        }

        private static readonly HttpClient _AuthorizationClient = CreateAuthorizationClient();

        private static HttpClient CreateAuthorizationClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        private sealed class HttpOutcome
        {
            public int Status { get; set; }
            public string Body { get; set; } = String.Empty;
        }

        #endregion
    }
}
