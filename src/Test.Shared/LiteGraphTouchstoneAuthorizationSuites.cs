namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
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
                    Authz("Authorization.SettingsDeniedForNonAdmin", "Settings endpoints deny tenant admins and regular users", TestSettingsDeniedForNonAdmin)
                });
        }

        private static TestCaseDescriptor Authz(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: "Authorization", caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region Authorization-Cases

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
