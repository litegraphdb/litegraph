namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        #region Onboarding-Suite

        private static TestSuiteDescriptor CreateOnboardingSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Onboarding",
                displayName: "Tenant onboarding endpoint (tenant, users, credentials, graphs)",
                cases: new List<TestCaseDescriptor>
                {
                    Onboard("Onboarding.FullProvisioning", "Onboard a tenant with users, a credential, and a graph in one call", TestOnboardingFullProvisioning),
                    Onboard("Onboarding.TenantOnly", "Onboard with only a tenant and no subordinate objects", TestOnboardingTenantOnly),
                    Onboard("Onboarding.ServerGeneratedTenantGuid", "Onboard without a tenant GUID generates one server-side", TestOnboardingServerGeneratedTenantGuid),
                    Onboard("Onboarding.NonAdminDenied", "A non-administrator cannot onboard a tenant", TestOnboardingNonAdminDenied),
                    Onboard("Onboarding.NoBodyRejected", "Onboarding with no request body is rejected", TestOnboardingNoBodyRejected),
                    Onboard("Onboarding.MissingTenantRejected", "Onboarding without a tenant definition is rejected", TestOnboardingMissingTenantRejected),
                    Onboard("Onboarding.NullUserFailFast", "A malformed user is rejected and nothing is created", TestOnboardingNullUserFailFast),
                    Onboard("Onboarding.DuplicateTenantConflict", "Onboarding a tenant GUID that already exists conflicts", TestOnboardingDuplicateTenantConflict),
                    Onboard("Onboarding.DuplicateBearerTokenFailFast", "A conflicting credential bearer token is rejected and nothing is created", TestOnboardingDuplicateBearerTokenFailFast)
                });
        }

        private static TestCaseDescriptor Onboard(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: "Onboarding", caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region Onboarding-Cases

        private static async Task TestOnboardingFullProvisioning(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string bearerToken = "onboard-full-cred";
                string body = "{"
                    + "\"Tenant\":{\"Name\":\"Onboard Full\",\"Active\":true},"
                    + "\"Users\":["
                    + "{\"FirstName\":\"Ob\",\"LastName\":\"Admin\",\"Email\":\"ob-admin@onboard.test\",\"Password\":\"password\",\"Active\":true,\"IsTenantAdmin\":true},"
                    + "{\"FirstName\":\"Ob\",\"LastName\":\"Member\",\"Email\":\"ob-member@onboard.test\",\"Password\":\"password\",\"Active\":true}"
                    + "],"
                    + "\"Credentials\":[{\"Name\":\"Onboard credential\",\"BearerToken\":\"" + bearerToken + "\",\"Active\":true}],"
                    + "\"Graphs\":[{\"Name\":\"Onboard Graph\"}]"
                    + "}";

                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, onboard.Status, "Full onboarding succeeds (status " + onboard.Status + " body " + onboard.Body + ")");
                AssertEqual(2, OnboardArrayCount(onboard.Body, "Users"), "Two users were created");
                AssertEqual(1, OnboardArrayCount(onboard.Body, "Credentials"), "One credential was created");
                AssertEqual(1, OnboardArrayCount(onboard.Body, "Graphs"), "One graph was created");
                AssertFalse(onboard.Body.Contains("\"Password\":\"password\""), "The onboarding response redacts user passwords");

                string tenantGuid = OnboardTenantGuid(onboard.Body);
                AssertNotEmpty(Guid.Parse(tenantGuid), "Onboarding returned a tenant GUID");

                string? adminUserGuid = OnboardFindAdminUserGuid(onboard.Body);
                AssertNotNull(adminUserGuid, "The tenant-admin user is present in the response");

                HttpOutcome readAdmin = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenantGuid + "/users/" + adminUserGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readAdmin.Status, "The created tenant-admin user can be read back");
                AssertTrue(JsonBoolProperty(readAdmin.Body, "IsTenantAdmin"), "The onboarded user persisted IsTenantAdmin=true");

                // The onboarded administrator can authenticate with email/password against the new tenant.
                HttpOutcome tokenOutcome = await OnboardTokenAsync(endpoint, "ob-admin@onboard.test", "password", tenantGuid, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, tokenOutcome.Status, "The onboarded administrator can obtain a token (status " + tokenOutcome.Status + ")");

                // The onboarded credential resolves by its bearer token.
                HttpOutcome credLookup = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/credentials/bearer/" + bearerToken, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, credLookup.Status, "The onboarded credential resolves by bearer token");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingTenantOnly(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string body = "{\"Tenant\":{\"Name\":\"Onboard Tenant Only\",\"Active\":true}}";

                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, onboard.Status, "Tenant-only onboarding succeeds (status " + onboard.Status + " body " + onboard.Body + ")");
                AssertEqual(0, OnboardArrayCount(onboard.Body, "Users"), "No users were created");
                AssertEqual(0, OnboardArrayCount(onboard.Body, "Credentials"), "No credentials were created");
                AssertEqual(0, OnboardArrayCount(onboard.Body, "Graphs"), "No graphs were created");

                string tenantGuid = OnboardTenantGuid(onboard.Body);
                HttpOutcome readTenant = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenantGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, readTenant.Status, "The onboarded tenant exists");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingServerGeneratedTenantGuid(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string body = "{"
                    + "\"Tenant\":{\"Name\":\"Onboard Generated GUID\",\"Active\":true},"
                    + "\"Users\":[{\"FirstName\":\"Gen\",\"LastName\":\"Admin\",\"Email\":\"gen-admin@onboard.test\",\"Password\":\"password\",\"Active\":true,\"IsTenantAdmin\":true}]"
                    + "}";

                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, onboard.Status, "Onboarding without a tenant GUID succeeds (status " + onboard.Status + " body " + onboard.Body + ")");

                string tenantGuid = OnboardTenantGuid(onboard.Body);
                Guid parsed = Guid.Parse(tenantGuid);
                AssertNotEmpty(parsed, "A tenant GUID was generated server-side");
                AssertFalse(parsed.Equals(Guid.Parse(_DefaultTenantGuid)), "The generated tenant GUID is not the default tenant");
                AssertEqual(1, OnboardArrayCount(onboard.Body, "Users"), "The user was created in the generated tenant");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingNonAdminDenied(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string regularBearer = await ProvisionUserAsync(endpoint, _DefaultTenantGuid, "onboard-nonadmin@onboard.test", false, false, cancellationToken).ConfigureAwait(false);

                string body = "{\"Tenant\":{\"Name\":\"Should Not Exist\",\"Active\":true}}";
                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", regularBearer, body, cancellationToken).ConfigureAwait(false);
                AssertTrue(onboard.Status == 401 || onboard.Status == 403, "A non-administrator cannot onboard a tenant (status " + onboard.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingNoBodyRejected(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(400, onboard.Status, "Onboarding with no body returns 400 (status " + onboard.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingMissingTenantRejected(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string body = "{\"Users\":[{\"Email\":\"orphan@onboard.test\",\"Password\":\"password\",\"Active\":true}]}";
                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(400, onboard.Status, "Onboarding without a tenant returns 400 (status " + onboard.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingNullUserFailFast(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string tenantGuid = "1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a";
                string body = "{"
                    + "\"Tenant\":{\"GUID\":\"" + tenantGuid + "\",\"Name\":\"Fail Fast User\",\"Active\":true},"
                    + "\"Users\":[null]"
                    + "}";

                HttpOutcome onboard = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(400, onboard.Status, "A malformed (null) user returns 400 (status " + onboard.Status + ")");

                // Fail-fast: validation happens before creation, so the tenant must not exist.
                HttpOutcome readTenant = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + tenantGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(404, readTenant.Status, "The tenant was not created when onboarding failed validation");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingDuplicateTenantConflict(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string tenantGuid = "2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b";
                string body = "{\"Tenant\":{\"GUID\":\"" + tenantGuid + "\",\"Name\":\"Duplicate Tenant\",\"Active\":true}}";

                HttpOutcome first = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, first.Status, "First onboarding of the tenant succeeds (status " + first.Status + ")");

                HttpOutcome second = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, body, cancellationToken).ConfigureAwait(false);
                AssertEqual(409, second.Status, "Onboarding an existing tenant GUID returns 409 (status " + second.Status + ")");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestOnboardingDuplicateBearerTokenFailFast(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string endpoint = RequireEndpoint();
                string sharedToken = "onboard-dup-token";

                string firstBody = "{"
                    + "\"Tenant\":{\"Name\":\"Dup Token First\",\"Active\":true},"
                    + "\"Users\":[{\"Email\":\"dup-first@onboard.test\",\"Password\":\"password\",\"Active\":true}],"
                    + "\"Credentials\":[{\"Name\":\"First cred\",\"BearerToken\":\"" + sharedToken + "\",\"Active\":true}]"
                    + "}";
                HttpOutcome first = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, firstBody, cancellationToken).ConfigureAwait(false);
                AssertEqual(200, first.Status, "First onboarding with the bearer token succeeds (status " + first.Status + ")");

                string secondTenantGuid = "3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c";
                string secondBody = "{"
                    + "\"Tenant\":{\"GUID\":\"" + secondTenantGuid + "\",\"Name\":\"Dup Token Second\",\"Active\":true},"
                    + "\"Users\":[{\"Email\":\"dup-second@onboard.test\",\"Password\":\"password\",\"Active\":true}],"
                    + "\"Credentials\":[{\"Name\":\"Second cred\",\"BearerToken\":\"" + sharedToken + "\",\"Active\":true}]"
                    + "}";
                HttpOutcome second = await AuthRestAsync(HttpMethod.Put, endpoint + "/v1.0/tenants/onboarding", _AdminBearerToken, secondBody, cancellationToken).ConfigureAwait(false);
                AssertEqual(409, second.Status, "A conflicting bearer token returns 409 (status " + second.Status + ")");

                // Fail-fast: the bearer-token conflict is detected before creation, so the second tenant must not exist.
                HttpOutcome readTenant = await AuthRestAsync(HttpMethod.Get, endpoint + "/v1.0/tenants/" + secondTenantGuid, _AdminBearerToken, null, cancellationToken).ConfigureAwait(false);
                AssertEqual(404, readTenant.Status, "The second tenant was not created when the bearer token conflicted");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        #endregion

        #region Onboarding-Helpers

        private static async Task<HttpOutcome> OnboardTokenAsync(string endpoint, string email, string password, string tenantGuid, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint + "/v1.0/token"))
            {
                request.Headers.Add("x-email", email);
                request.Headers.Add("x-password", password);
                request.Headers.Add("x-tenant-guid", tenantGuid);

                using (HttpResponseMessage response = await _AuthorizationClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    HttpOutcome outcome = new HttpOutcome();
                    outcome.Status = (int)response.StatusCode;
                    outcome.Body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    return outcome;
                }
            }
        }

        private static int OnboardArrayCount(string body, string property)
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                if (document.RootElement.TryGetProperty(property, out JsonElement array) && array.ValueKind == JsonValueKind.Array)
                    return array.GetArrayLength();
                return -1;
            }
        }

        private static string OnboardTenantGuid(string body)
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                return document.RootElement.GetProperty("Tenant").GetProperty("GUID").GetString()
                    ?? throw new InvalidOperationException("Onboarding response did not contain a tenant GUID.");
            }
        }

        private static string? OnboardFindAdminUserGuid(string body)
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                foreach (JsonElement user in document.RootElement.GetProperty("Users").EnumerateArray())
                {
                    if (user.TryGetProperty("IsTenantAdmin", out JsonElement isTenantAdmin) && isTenantAdmin.ValueKind == JsonValueKind.True)
                        return user.GetProperty("GUID").GetString();
                }
            }
            return null;
        }

        private static bool JsonBoolProperty(string body, string property)
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                return document.RootElement.TryGetProperty(property, out JsonElement value)
                    && value.ValueKind == JsonValueKind.True;
            }
        }

        #endregion
    }
}
