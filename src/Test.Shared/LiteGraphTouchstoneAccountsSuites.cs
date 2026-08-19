namespace Test.Shared
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        #region Accounts-Suite

        private static TestSuiteDescriptor CreateAccountsSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Accounts",
                displayName: "v8 account flags and authentication context",
                cases: new System.Collections.Generic.List<TestCaseDescriptor>
                {
                    Acct("Accounts.FlagsDefaultFalse", "New user flags default to false", TestAccountsFlagsDefaultFalse),
                    Acct("Accounts.SystemAdminRoundTrip", "IsSystemAdmin round-trips", TestAccountsSystemAdminRoundTrip),
                    Acct("Accounts.TenantAdminRoundTrip", "IsTenantAdmin round-trips", TestAccountsTenantAdminRoundTrip),
                    Acct("Accounts.UpdateFlags", "Updating flags persists", TestAccountsUpdateFlags),
                    Acct("Accounts.ReadByEmailReturnsFlags", "ReadByEmail returns flags", TestAccountsReadByEmailReturnsFlags),
                    Acct("Accounts.RedactPreservesFlags", "Redact preserves flags", TestAccountsRedactPreservesFlags),
                    Acct("Auth.SystemAdminFromToken", "IsSystemAdmin derives from break-glass token", TestAuthSystemAdminFromToken),
                    Acct("Auth.SystemAdminFromUserFlag", "IsSystemAdmin derives from user flag", TestAuthSystemAdminFromUserFlag),
                    Acct("Auth.TenantAdminFromUserFlag", "IsTenantAdmin derives from user flag", TestAuthTenantAdminFromUserFlag),
                    Acct("Auth.NoFlagsNoAdmin", "No flags yields no admin", TestAuthNoFlagsNoAdmin)
                });
        }

        private static TestCaseDescriptor Acct(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: "Accounts", caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region Accounts-Cases

        private static async Task TestAccountsFlagsDefaultFalse(CancellationToken token)
        {
            string db = IeDbName("acct-defaults");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                UserMaster created = await client.User.Create(AcctUser(tenant, "a@x.com", false, false), token).ConfigureAwait(false);
                AssertFalse(created.IsSystemAdmin, "IsSystemAdmin defaults to false");
                AssertFalse(created.IsTenantAdmin, "IsTenantAdmin defaults to false");
            }
            IeCleanup(db);
        }

        private static async Task TestAccountsSystemAdminRoundTrip(CancellationToken token)
        {
            string db = IeDbName("acct-sysadmin");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                UserMaster created = await client.User.Create(AcctUser(tenant, "sys@x.com", true, false), token).ConfigureAwait(false);
                UserMaster read = await client.User.ReadByGuid(tenant, created.GUID, token).ConfigureAwait(false);
                AssertTrue(read.IsSystemAdmin, "IsSystemAdmin persists true");
                AssertFalse(read.IsTenantAdmin, "IsTenantAdmin remains false");
            }
            IeCleanup(db);
        }

        private static async Task TestAccountsTenantAdminRoundTrip(CancellationToken token)
        {
            string db = IeDbName("acct-tenadmin");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                UserMaster created = await client.User.Create(AcctUser(tenant, "ten@x.com", false, true), token).ConfigureAwait(false);
                UserMaster read = await client.User.ReadByGuid(tenant, created.GUID, token).ConfigureAwait(false);
                AssertTrue(read.IsTenantAdmin, "IsTenantAdmin persists true");
                AssertFalse(read.IsSystemAdmin, "IsSystemAdmin remains false");
            }
            IeCleanup(db);
        }

        private static async Task TestAccountsUpdateFlags(CancellationToken token)
        {
            string db = IeDbName("acct-update");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                UserMaster created = await client.User.Create(AcctUser(tenant, "u@x.com", false, false), token).ConfigureAwait(false);
                created.IsSystemAdmin = true;
                created.IsTenantAdmin = true;
                await client.User.Update(created, token).ConfigureAwait(false);
                UserMaster read = await client.User.ReadByGuid(tenant, created.GUID, token).ConfigureAwait(false);
                AssertTrue(read.IsSystemAdmin, "Updated IsSystemAdmin persists");
                AssertTrue(read.IsTenantAdmin, "Updated IsTenantAdmin persists");
            }
            IeCleanup(db);
        }

        private static async Task TestAccountsReadByEmailReturnsFlags(CancellationToken token)
        {
            string db = IeDbName("acct-email");
            using (LiteGraphClient client = IeNewClient(db))
            {
                Guid tenant = await IeSeedTenant(client).ConfigureAwait(false);
                await client.User.Create(AcctUser(tenant, "flag@x.com", true, true), token).ConfigureAwait(false);
                UserMaster read = await client.User.ReadByEmail(tenant, "flag@x.com", token).ConfigureAwait(false);
                AssertNotNull(read, "ReadByEmail finds the user");
                AssertTrue(read.IsSystemAdmin && read.IsTenantAdmin, "ReadByEmail returns both flags");
            }
            IeCleanup(db);
        }

        private static async Task TestAccountsRedactPreservesFlags(CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            LiteGraph.Serialization.Serializer serializer = new LiteGraph.Serialization.Serializer();
            UserMaster user = new UserMaster { Email = "r@x.com", Password = "longpassword", IsSystemAdmin = true, IsTenantAdmin = true };
            UserMaster redacted = UserMaster.Redact(serializer, user);
            AssertTrue(redacted.IsSystemAdmin, "Redact preserves IsSystemAdmin");
            AssertTrue(redacted.IsTenantAdmin, "Redact preserves IsTenantAdmin");
            AssertTrue(redacted.Password != "longpassword", "Redact masks the password");
        }

        private static async Task TestAuthSystemAdminFromToken(CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            AuthenticationContext ctx = new AuthenticationContext { IsAdmin = true };
            AssertTrue(ctx.IsSystemAdmin, "Break-glass token yields IsSystemAdmin");
            AssertFalse(ctx.IsTenantAdmin, "Token alone is not a tenant admin");
        }

        private static async Task TestAuthSystemAdminFromUserFlag(CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            AuthenticationContext ctx = new AuthenticationContext { User = new UserMaster { Email = "s@x.com", Password = "p", IsSystemAdmin = true } };
            AssertTrue(ctx.IsSystemAdmin, "User IsSystemAdmin flag yields IsSystemAdmin");
        }

        private static async Task TestAuthTenantAdminFromUserFlag(CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            AuthenticationContext ctx = new AuthenticationContext { User = new UserMaster { Email = "t@x.com", Password = "p", IsTenantAdmin = true } };
            AssertTrue(ctx.IsTenantAdmin, "User IsTenantAdmin flag yields IsTenantAdmin");
            AssertFalse(ctx.IsSystemAdmin, "Tenant admin is not a system admin");
        }

        private static async Task TestAuthNoFlagsNoAdmin(CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            AuthenticationContext ctx = new AuthenticationContext { User = new UserMaster { Email = "n@x.com", Password = "p" } };
            AssertFalse(ctx.IsSystemAdmin, "No flags: not a system admin");
            AssertFalse(ctx.IsTenantAdmin, "No flags: not a tenant admin");
        }

        private static UserMaster AcctUser(Guid tenantGuid, string email, bool sysAdmin, bool tenantAdmin)
        {
            return new UserMaster
            {
                GUID = Guid.NewGuid(),
                TenantGUID = tenantGuid,
                FirstName = "Test",
                LastName = "User",
                Email = email,
                Password = "password",
                Active = true,
                IsSystemAdmin = sysAdmin,
                IsTenantAdmin = tenantAdmin
            };
        }

        #endregion
    }
}
