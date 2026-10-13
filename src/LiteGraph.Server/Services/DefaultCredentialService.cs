namespace LiteGraph.Server.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Server.Classes;

    // This diagnostic reports availability, never the credential values themselves.
    internal static class DefaultCredentialService
    {
        internal const string Warning = "Default administrator credentials are active. Change them before exposing LiteGraph to an untrusted network. Change the administrator token, the seeded administrator password, and the seeded bearer credential; see the README production guidance.";

        internal static async Task<DefaultCredentialStatus> Read(Settings settings, GraphRepositoryBase repo, CancellationToken token = default)
        {
            DefaultCredentialStatus status = new DefaultCredentialStatus
            {
                AdministratorToken = settings.LiteGraph.AdminBearerToken == "litegraphadmin"
            };

            await foreach (UserMaster user in repo.User.ReadMany(null, "default@user.com", token: token).ConfigureAwait(false))
            {
                if (!user.Active || user.Password != "password") continue;
                TenantMetadata tenant = await repo.Tenant.ReadByGuid(user.TenantGUID, token).ConfigureAwait(false);
                if (tenant != null && tenant.Active) status.SeededUserPassword = true;
            }

            Credential credential = await repo.Credential.ReadByBearerToken("default", token).ConfigureAwait(false);
            if (credential != null && credential.Active)
            {
                UserMaster user = await repo.User.ReadByGuid(credential.TenantGUID, credential.UserGUID, token).ConfigureAwait(false);
                TenantMetadata tenant = await repo.Tenant.ReadByGuid(credential.TenantGUID, token).ConfigureAwait(false);
                status.SeededBearerToken = user != null && user.Active && tenant != null && tenant.Active;
            }
            return status;
        }
    }

    internal class DefaultCredentialStatus
    {
        public bool AdministratorToken { get; set; }
        public bool SeededUserPassword { get; set; }
        public bool SeededBearerToken { get; set; }
        public bool HasDefaultCredentials => AdministratorToken || SeededUserPassword || SeededBearerToken;
    }
}
