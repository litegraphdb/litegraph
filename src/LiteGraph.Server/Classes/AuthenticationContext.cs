namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Authentication context.
    /// </summary>
    public class AuthenticationContext
    {
        #region Public-Members

        /// <summary>
        /// Email address.
        /// </summary>
        public string Email { get; set; } = null;

        /// <summary>
        /// Password.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Bearer token.
        /// </summary>
        public string BearerToken { get; set; } = null;

        /// <summary>
        /// Security token.
        /// </summary>
        public string SecurityToken { get; set; } = null;

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Tenant.
        /// </summary>
        public TenantMetadata Tenant { get; set; } = null;

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// User.
        /// </summary>
        public UserMaster User { get; set; } = null;

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public Guid? CredentialGUID { get; set; } = null;

        /// <summary>
        /// Credential.
        /// </summary>
        public Credential Credential { get; set; } = null;

        /// <summary>
        /// Boolean indicating if the user is a system administrator using the administrator bearer token.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Boolean indicating if the principal is a system administrator.
        /// True when the break-glass administrator bearer token is used, or when the authenticated user record has the IsSystemAdmin flag set.
        /// A system administrator is a server-wide superuser.
        /// </summary>
        public bool IsSystemAdmin
        {
            get
            {
                return IsAdmin || (User != null && User.IsSystemAdmin);
            }
        }

        /// <summary>
        /// Boolean indicating if the principal is a tenant administrator within their own tenant.
        /// True when the authenticated user record has the IsTenantAdmin flag set.
        /// </summary>
        public bool IsTenantAdmin
        {
            get
            {
                return User != null && User.IsTenantAdmin;
            }
        }

        /// <summary>
        /// Authentication result.
        /// </summary>
        public AuthenticationResultEnum Result { get; set; } = AuthenticationResultEnum.NotFound;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthenticationContext()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
